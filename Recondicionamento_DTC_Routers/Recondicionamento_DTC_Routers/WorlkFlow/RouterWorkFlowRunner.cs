using Recondicionamento_DTC_Routers.Domain;
using Recondicionamento_DTC_Routers.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Recondicionamento_DTC_Routers.Workflow
{
    public sealed class RouterWorkflowRunner
    {
        private readonly IList<StepVm> _steps;
        private readonly ILogSink _log;

        public RouterWorkflowRunner(IList<StepVm> steps, ILogSink log)
        {
            _steps = steps;
            _log = log;
        }

        // UI hooks
        public Func<string, string, string, Task<bool>> AskYesNo { get; set; } =
            (_, __, ___) => Task.FromResult(true);

        public Action<string, string> ShowInfo { get; set; } = (_, __) => { };
        public Action<string, string> ShowWarn { get; set; } = (_, __) => { };

        // Steps providers
        public Func<CancellationToken, Task<string>> DetectFabricanteAsync { get; set; } =
            _ => Task.FromResult("UNKNOWN");

        public Func<string, Task<string>> GetNumeroSerieAsync { get; set; } =
            _ => Task.FromResult("");

        public Func<string, Task<string>> GetFirmwareAsync { get; set; } =
            _ => Task.FromResult("");

        public Func<Task<bool>> AskInspecaoVisualAsync { get; set; } =
            () => Task.FromResult(false);

        public Func<Task<bool>> AskLiga230VAsync { get; set; } =
            () => Task.FromResult(false);

        public Func<Task<int>> AskEthPortsAsync { get; set; } =
            () => Task.FromResult(0);

        public Func<Task<bool>> TestETHAsync { get; set; } =
            () => Task.FromResult(true);

        // ✅ novo: pergunta se quer fazer upgrade
        public Func<string, Task<bool>> AskDoUpgradeAsync { get; set; } =
            _ => Task.FromResult(true);

        public Func<string, CancellationToken, Task> DoUpgradeFirmwareAsync { get; set; } =
            (_, __) => Task.CompletedTask;

        public Func<string, CancellationToken, Task<bool>> DoUploadConfigAsync { get; set; } =
            (_, __) => Task.FromResult(false);

        public Func<Task<int>> TestRS232Async { get; set; } =
            () => Task.FromResult(0);

        public Func<Task<bool>> TestRS485Async { get; set; } =
            () => Task.FromResult(false);

        public Action<RouterRecord> ValidateAcessorios { get; set; } = _ => { };
        public Action<RouterRecord> AddToReport { get; set; } = _ => { };

        public async Task<RouterRecord> RunAsync(CancellationToken ct)
        {
            var r = new RouterRecord();
            ResetSteps();

            await RunStepAsync(1, ct, async () =>
            {
                r.Liga230V = await AskLiga230VAsync();
                SetDetail(1, r.Liga230V ? "LIGA" : "NÃO LIGA");
                if (!r.Liga230V) Fail(1, "Router não liga a 230V.");
            });

            await RunStepAsync(2, ct, async () =>
            {
                int ports = await AskEthPortsAsync();
                if (ports < 0) ports = 0;
                if (ports > 8) ports = 8;

                r.EthOk = new bool[8];

                if (ports == 0)
                {
                    SetDetail(2, "0 portas");
                    return;
                }

                int okCount = 0;
                for (int i = 0; i < ports; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    bool ready = await AskYesNo(
                        "Ethernet",
                        $"Ligar cabo à porta ETH{i + 1} e clicar YES para testar.",
                        "YES = testar\nNO = marcar FAIL"
                    );

                    if (!ready)
                    {
                        r.EthOk[i] = false;
                        continue;
                    }

                    bool ok = await TestETHAsync();
                    r.EthOk[i] = ok;
                    if (ok) okCount++;
                }

                SetDetail(2, $"{okCount}/{ports} OK");

                // se falhou alguma das portas existentes, marca fail mas NÃO pára o workflow
                bool allOk = true;
                for (int i = 0; i < ports; i++) allOk &= r.EthOk[i];
                if (!allOk) Fail(2, $"Ethernet falhou ({okCount}/{ports} OK).");
            });

            await RunStepAsync(3, ct, async () =>
            {
                r.Fabricante = (await DetectFabricanteAsync(ct))?.Trim() ?? "UNKNOWN";
                SetDetail(3, r.Fabricante);
                if (string.IsNullOrWhiteSpace(r.Fabricante) || r.Fabricante == "UNKNOWN")
                    Fail(3, "Fabricante não detetado.");
            });

            await RunStepAsync(4, ct, async () =>
            {
                r.NumeroSerie = (await GetNumeroSerieAsync(r.Fabricante))?.Trim() ?? "";
                SetDetail(4, r.NumeroSerie);
                if (string.IsNullOrWhiteSpace(r.NumeroSerie))
                    Fail(4, "Nº Série vazio.");
            });

            await RunStepAsync(5, ct, async () =>
            {
                r.InspecaoVisual = await AskInspecaoVisualAsync();
                SetDetail(5, r.InspecaoVisual ? "CONFORME" : "NÃO CONFORME");
                if (!r.InspecaoVisual) Fail(5, "Inspeção visual NÃO conforme.");
            });

            // 6) Ler FW inicial
            await RunStepAsync(6, ct, async () =>
            {
                r.FirmwareOld = (await GetFirmwareAsync(r.Fabricante))?.Trim() ?? "";
                SetDetail(6, r.FirmwareOld);
            });

            // 7) Upgrade de firmware (pergunta se quer skip)
            await RunStepAsync(7, ct, async () =>
            {
                bool fazerUpgrade = await AskYesNo(
                    "Upgrade de firmware",
                    "Queres fazer upgrade de firmware agora?\n\nYes = faz upgrade\nNo = skip",
                    $"Fabricante: {r.Fabricante}"
                );

                if (!fazerUpgrade)
                    throw new StepSkippedException("SKIP pelo utilizador");

                await DoUpgradeFirmwareAsync(r.Fabricante, ct);
                SetDetail(7, "OK");
            });

            // 8) Ler FW final
            await RunStepAsync(8, ct, async () =>
            {
                r.FirmwareNew = (await GetFirmwareAsync(r.Fabricante))?.Trim() ?? "";
                SetDetail(8, r.FirmwareNew);
            });

            await RunStepAsync(9, ct, async () =>
            {
                r.ConfigUploaded = await DoUploadConfigAsync(r.Fabricante, ct);
                SetDetail(9, r.ConfigUploaded ? "OK" : "FAIL");
                if (!r.ConfigUploaded) Fail(9, "Upload config falhou.");
            });

            await RunStepAsync(10, ct, async () =>
            {
                r.Rs232Score = await TestRS232Async();
                SetDetail(10, $"Score={r.Rs232Score}");

                if (r.Rs232Score <= 0)
                    throw new Exception("RS232 falhou.");
            });

            await RunStepAsync(11, ct, async () =>
            {
                r.Rs485Ok = await TestRS485Async();
                SetDetail(11, r.Rs485Ok ? "OK" : "FAIL");

                if (!r.Rs485Ok)
                    throw new Exception("RS485 falhou.");
            });


            await RunStepAsync(12, ct, async () =>
            {
                ValidateAcessorios(r);
                SetDetail(12, "OK");
                await Task.CompletedTask;
            });

            r.ConformidadeFinal = ComputeConformidade(r);
            await _log.LogAsync($"Conformidade final: {(r.ConformidadeFinal ? "CONFORME" : "NÃO CONFORME")}");

            // ✅ Step 13 corre SEMPRE (a menos que Cancel)
            await RunStepAsync(13, ct, async () =>
            {
                AddToReport(r);
                SetDetail(13, "OK");
                await Task.CompletedTask;
            });

            return r;
        }

        private bool ComputeConformidade(RouterRecord r)
        {
            if (!r.InspecaoVisual) return false;
            if (!r.Liga230V) return false;
            if (string.IsNullOrWhiteSpace(r.Fabricante) || r.Fabricante == "UNKNOWN") return false;
            if (string.IsNullOrWhiteSpace(r.NumeroSerie)) return false;
            if (string.IsNullOrWhiteSpace(r.FirmwareOld)) return false;
            if (string.IsNullOrWhiteSpace(r.FirmwareNew)) return false;
            if (!r.ConfigUploaded) return false;
            if (r.Rs232Score <= 0) return false;
            if (!r.Rs485Ok) return false;
            if (r.EthOk == null) return false;
            return true;
        }

        private void ResetSteps()
        {
            foreach (var s in _steps)
            {
                s.Status = StepStatus.Pending;
                s.Detail = "";
            }
        }

        private void SetDetail(int order, string detail)
        {
            var s = _steps.FirstOrDefault(x => x.Order == order);
            if (s != null) s.Detail = detail ?? "";
        }

        private void Fail(int order, string msg)
        {
            var s = _steps.FirstOrDefault(x => x.Order == order);
            if (s != null)
            {
                s.Status = StepStatus.Fail;
                s.Detail = msg ?? "FAIL";
            }
        }

        private void Skip(int order, string msg)
        {
            var s = _steps.FirstOrDefault(x => x.Order == order);
            if (s != null)
            {
                s.Status = StepStatus.Skipped;
                s.Detail = msg ?? "SKIP";
            }
        }

        // ✅ NÃO faz throw em FAIL; só em Cancel
        private async Task RunStepAsync(int order, CancellationToken ct, Func<Task> action)
        {
            ct.ThrowIfCancellationRequested();

            var step = _steps.FirstOrDefault(s => s.Order == order);
            if (step == null) return;

            step.Status = StepStatus.Running;
            step.Detail = "";

            await _log.LogAsync($"[{order:00}] {step.Name} - START");
            


            try
            {
                await action();
                step.Status = StepStatus.Ok;
                await _log.LogAsync($"[{order:00}] {step.Name} - OK");
            }
            catch (OperationCanceledException)
            {
                step.Status = StepStatus.Skipped;
                step.Detail = "CANCEL";
                await _log.LogAsync($"[{order:00}] {step.Name} - CANCEL");
                throw; // cancel continua a abortar
            }
            catch (StepSkippedException ex)
            {
                step.Status = StepStatus.Skipped;
                step.Detail = ex.Message;
                await _log.LogAsync($"[{order:00}] {step.Name} - SKIP: {ex.Message}");
                // NÃO faz throw
            }
            catch (Exception ex)
            {
                step.Status = StepStatus.Fail;
                step.Detail = ex.Message;
                await _log.LogAsync($"[{order:00}] {step.Name} - FAIL: {ex.Message}");
                // NÃO faz throw -> continua o workflow
            }
        }
        public sealed class StepSkippedException : Exception
        {
            public StepSkippedException(string message) : base(message) { }
        }

    }
}
