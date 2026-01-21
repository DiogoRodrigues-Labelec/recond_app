using Recondicionamento_DTC_Routers.Domain;
using Recondicionamento_DTC_Routers.Infra;
using Recondicionamento_DTC_Routers.Services;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Recondicionamento_DTC_Routers.Workflow
{
    public sealed class DtcWorkflowRunner
    {
        private readonly BindingList<StepVm> _steps;
        private readonly ILogSink _log;

        public DtcWorkflowRunner(BindingList<StepVm> steps, ILogSink log)
        {
            _steps = steps ?? throw new ArgumentNullException(nameof(steps));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        // UI delegates
        public Func<string, string, bool, bool> AskYesNo { get; set; }  // (title, msg, defaultYes) -> bool
        public Action<string, string> ShowInfo { get; set; }
        public Action<string, string> ShowWarn { get; set; }

        // Workflow hooks (null => SKIP)
        public Func<CancellationToken, Task> VerifyTestSetupAsync { get; set; } // opcional
        public Func<Task> AskSwapDtcAsync { get; set; }

        public Func<CancellationToken, Task<string>> DetectFabricanteAsync { get; set; }
        public Func<string, CancellationToken, Task<string>> GetDtcIdAsync { get; set; }
        public Func<string, CancellationToken, Task<string>> GetFirmwareAsync { get; set; }

        public Func<string, string> GetExpectedFirmware { get; set; } // pode devolver "" se não tiveres mapping
        public Func<string, CancellationToken, Task> DoUpgradeFirmwareAsync { get; set; } // manual/auto

        public Func<string, string, CancellationToken, Task> DoUploadConfigAsync { get; set; }
        public Func<string, string, CancellationToken, Task<bool>> TestAnalogInputsAsync { get; set; }
        public Func<string, string, CancellationToken, Task<bool>> TestEmiPlcAsync { get; set; }

        public Action<DtcRecord> AddToReport { get; set; }

        public async Task<DtcRecord> RunAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var r = new DtcRecord
            {
                ConfigUploaded = true,  // SKIP não bloqueia
                AnalogOk = true,
                EmiPlcOk = true,
                ConformidadeFinal = false
            };

            await _log.LogAsync("=== INÍCIO WORKFLOW DTC ===");

            // (Opcional) 0) setup
            await RunStepMaybeSkipAsync(0, "Validar setup", ct, async () =>
            {
                if (VerifyTestSetupAsync == null)
                    throw new StepSkippedException("VerifyTestSetupAsync não definido (SKIP).");

                await VerifyTestSetupAsync(ct);
            });

            // 1) Manual swap
            await RunStepAsync(1, "Substituir DTC no setup (manual)", ct, async () =>
            {
                if (AskSwapDtcAsync == null)
                    throw new InvalidOperationException("AskSwapDtcAsync não definido.");

                await AskSwapDtcAsync();
            });

            // 2) Fabricante
            await RunStepAsync(2, "Detetar fabricante (HTTP/HTTPS)", ct, async () =>
            {
                if (DetectFabricanteAsync == null)
                    throw new InvalidOperationException("DetectFabricanteAsync não definido.");

                r.Fabricante = (await DetectFabricanteAsync(ct))?.Trim();
                if (string.IsNullOrWhiteSpace(r.Fabricante)) r.Fabricante = "UNKNOWN";

                SetDetail(2, r.Fabricante);
                await _log.LogAsync($"Fabricante: {r.Fabricante}");
            });

            // 3) ID/Serial
            await RunStepAsync(3, "Ler ID/Serial DTC (web)", ct, async () =>
            {
                if (GetDtcIdAsync == null)
                    throw new InvalidOperationException("GetDtcIdAsync não definido.");

                r.NumeroSerie = (await GetDtcIdAsync(r.Fabricante, ct))?.Trim() ?? "";
                SetDetail(3, r.NumeroSerie);
                await _log.LogAsync($"ID/Serial: {r.NumeroSerie}");
            });

            // 4) FW atual
            await RunStepAsync(4, "Ler FW atual", ct, async () =>
            {
                if (GetFirmwareAsync == null)
                    throw new InvalidOperationException("GetFirmwareAsync não definido.");

                r.FirmwareOld = (await GetFirmwareAsync(r.Fabricante, ct))?.Trim() ?? "";
                SetDetail(4, string.IsNullOrWhiteSpace(r.FirmwareOld) ? "vazio" : r.FirmwareOld);
                await _log.LogAsync($"FW old: {r.FirmwareOld}");
            });

            // 5) Upgrade FW (manual/auto)
            await RunStepMaybeSkipAsync(5, "Upgrade FW (se necessário)", ct, async () =>
            {
                string expected = (GetExpectedFirmware?.Invoke(r.Fabricante) ?? "").Trim();

                if (string.IsNullOrWhiteSpace(expected))
                    throw new StepSkippedException("Sem firmware esperado (mapping vazio).");

                // se não conseguiu ler FW, pergunta se quer tentar (manual)
                if (string.IsNullOrWhiteSpace(r.FirmwareOld))
                {
                    bool tentar = AskYesNo?.Invoke(
                        "Firmware DTC",
                        $"FW atual: (desconhecido)\nFW esperado: {expected}\n\nQueres tentar executar upgrade de firmware?",
                        true) ?? false;

                    if (!tentar)
                        throw new StepSkippedException("Decisão: não tentar upgrade (FW atual desconhecido).");
                }
                else
                {
                    // já está ok?
                    if (string.Equals(r.FirmwareOld, expected, StringComparison.OrdinalIgnoreCase))
                    {
                        r.FirmwareNew = r.FirmwareOld;
                        throw new StepSkippedException("FW já é o esperado.");
                    }

                    // pergunta se queres mesmo fazer upgrade
                    bool fazer = AskYesNo?.Invoke(
                        "Firmware DTC",
                        $"FW atual: {r.FirmwareOld}\nFW esperado: {expected}\n\nQueres fazer upgrade de firmware agora?",
                        false) ?? false;

                    if (!fazer)
                    {
                        r.FirmwareNew = r.FirmwareOld;
                        throw new StepSkippedException("Decisão: SKIP upgrade.");
                    }
                }

                if (DoUpgradeFirmwareAsync == null)
                    throw new InvalidOperationException($"Precisa upgrade para {expected} mas DoUpgradeFirmwareAsync é null.");

                await _log.LogAsync("A executar upgrade (manual/auto)...");
                await DoUpgradeFirmwareAsync(r.Fabricante, ct);

                // Após upgrade: re-lê FW (isto resolve o teu “não lê antes/depois”)
                if (GetFirmwareAsync != null)
                    r.FirmwareNew = (await GetFirmwareAsync(r.Fabricante, ct))?.Trim() ?? "";
                else
                    r.FirmwareNew = expected;

                SetDetail(5, $"OK -> {r.FirmwareNew}");

                // valida se bate no esperado (se não bater, falha o passo)
                if (!string.IsNullOrWhiteSpace(expected) &&
                    !string.Equals(r.FirmwareNew ?? "", expected, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception($"FW após upgrade ({r.FirmwareNew}) != esperado ({expected})");
                }

                await _log.LogAsync($"FW upgrade OK -> {r.FirmwareNew}");
            },
            onSkip: () =>
            {
                // se skip e ainda não tens firmwareNew, alinha
                if (string.IsNullOrWhiteSpace(r.FirmwareNew))
                    r.FirmwareNew = r.FirmwareOld;
            },
            onFail: () =>
            {
                // se falha e firmwareNew vazio, ao menos guarda o old
                if (string.IsNullOrWhiteSpace(r.FirmwareNew))
                    r.FirmwareNew = r.FirmwareOld;
            });

            // garante sempre FW New preenchido para report
            if (string.IsNullOrWhiteSpace(r.FirmwareNew))
                r.FirmwareNew = r.FirmwareOld;

            // 6) Upload config
            await RunStepMaybeSkipAsync(6, "Upload configurações (E-REDES)", ct, async () =>
            {
                if (DoUploadConfigAsync == null)
                    throw new StepSkippedException("DoUploadConfigAsync não definido (SKIP).");

                await DoUploadConfigAsync(r.Fabricante, r.NumeroSerie, ct);
                r.ConfigUploaded = true;
                SetDetail(6, "OK");
                await _log.LogAsync("Upload config OK");
            },
            onFail: () => r.ConfigUploaded = false,
            onSkip: () => r.ConfigUploaded = true);

            // 7) S01 DTC
            await RunStepMaybeSkipAsync(7, "S01 DTC: Tensões/Correntes (WS)", ct, async () =>
            {
                if (TestAnalogInputsAsync == null)
                    throw new StepSkippedException("TestAnalogInputsAsync não definido (SKIP).");

                bool ok = await TestAnalogInputsAsync(r.Fabricante, r.NumeroSerie, ct);
                r.AnalogOk = ok;
                SetDetail(7, ok ? "OK" : "FAIL");
                await _log.LogAsync($"S01(DTC) -> {ok}");

                if (!ok) throw new Exception("S01(DTC) FAIL");
            },
            onFail: () => r.AnalogOk = false,
            onSkip: () => r.AnalogOk = true);

            // 8) S01 EMI
            await RunStepMaybeSkipAsync(8, "S01 EMI PLC: Instantâneos (WS)", ct, async () =>
            {
                if (TestEmiPlcAsync == null)
                    throw new StepSkippedException("TestEmiPlcAsync não definido (SKIP).");

                bool ok = await TestEmiPlcAsync(r.Fabricante, r.NumeroSerie, ct);
                r.EmiPlcOk = ok;
                SetDetail(8, ok ? "OK" : "FAIL");
                await _log.LogAsync($"S01(EMI) -> {ok}");

                if (!ok) throw new Exception("S01(EMI) FAIL");
            },
            onFail: () => r.EmiPlcOk = false,
            onSkip: () => r.EmiPlcOk = true);

            // 9) Report
            await RunStepMaybeSkipAsync(9, "Adicionar ao report", ct, () =>
            {
                if (AddToReport == null)
                    throw new StepSkippedException("AddToReport não definido (SKIP).");

                AddToReport(r);
                SetDetail(9, "OK");
                return Task.CompletedTask;
            });

            // conformidade final (FW só conta se houver expected)
            string expectedFw = (GetExpectedFirmware?.Invoke(r.Fabricante) ?? "").Trim();
            bool fwOk = string.IsNullOrWhiteSpace(expectedFw)
                        || string.Equals((r.FirmwareNew ?? r.FirmwareOld ?? "").Trim(), expectedFw, StringComparison.OrdinalIgnoreCase);

            r.ConformidadeFinal = r.ConfigUploaded && r.AnalogOk && r.EmiPlcOk && fwOk;

            await _log.LogAsync($"=== FIM WORKFLOW DTC | Conformidade: {(r.ConformidadeFinal ? "CONFORME" : "NÃO CONFORME")} ===");
            return r;
        }

        // ---------------- Step runners ----------------

        private async Task RunStepAsync(int order, string name, CancellationToken ct, Func<Task> action)
        {
            ct.ThrowIfCancellationRequested();

            var step = _steps.FirstOrDefault(s => s.Order == order);
            if (step == null) return;

            step.Status = StepStatus.Running;
            step.Detail = "";

            await _log.LogAsync($"[{order:00}] {name} - START");

            try
            {
                await action();

                // se ninguém mexeu no status dentro do action, fica OK
                if (step.Status == StepStatus.Running)
                    step.Status = StepStatus.Ok;

                await _log.LogAsync($"[{order:00}] {name} - {step.Status}");
            }
            catch (OperationCanceledException)
            {
                step.Status = StepStatus.Skipped;
                step.Detail = "CANCEL";
                await _log.LogAsync($"[{order:00}] {name} - CANCEL");
                throw; // cancel aborta
            }
            catch (StepSkippedException ex)
            {
                step.Status = StepStatus.Skipped;
                step.Detail = ex.Message;
                await _log.LogAsync($"[{order:00}] {name} - SKIP: {ex.Message}");
            }
            catch (Exception ex)
            {
                step.Status = StepStatus.Fail;
                step.Detail = ex.Message;
                await _log.LogAsync($"[{order:00}] {name} - FAIL: {ex.Message}");
                // NÃO faz throw -> continua
            }
        }

        private async Task RunStepMaybeSkipAsync(int order, string name, CancellationToken ct, Func<Task> body,
            Action onFail = null, Action onSkip = null)
        {
            ct.ThrowIfCancellationRequested();

            var step = _steps.FirstOrDefault(s => s.Order == order);
            if (step == null)
            {
                // se não existir (ex. step 0), cria
                step = new StepVm(order, name);
                _steps.Add(step);
            }

            step.Status = StepStatus.Running;
            if (step.Detail == null) step.Detail = "";

            await _log.LogAsync($"[{order:00}] {name} - START");

            try
            {
                await body();

                // se body não alterou status, fica OK
                if (step.Status == StepStatus.Running)
                    step.Status = StepStatus.Ok;

                await _log.LogAsync($"[{order:00}] {name} - {step.Status}");
            }
            catch (OperationCanceledException)
            {
                step.Status = StepStatus.Skipped;
                step.Detail = "CANCEL";
                await _log.LogAsync($"[{order:00}] {name} - CANCEL");
                throw;
            }
            catch (StepSkippedException ex)
            {
                onSkip?.Invoke();
                step.Status = StepStatus.Skipped;
                step.Detail = ex.Message;
                await _log.LogAsync($"[{order:00}] {name} - SKIP: {ex.Message}");
            }
            catch (Exception ex)
            {
                onFail?.Invoke();
                step.Status = StepStatus.Fail;
                step.Detail = ex.Message;
                await _log.LogAsync($"[{order:00}] {name} - FAIL: {ex.Message}");
                // ✅ NÃO throw -> continua workflow
            }
        }

        private void SetDetail(int order, string detail)
        {
            var s = _steps.FirstOrDefault(x => x.Order == order);
            if (s != null) s.Detail = detail ?? "";
        }

        private sealed class StepSkippedException : Exception
        {
            public StepSkippedException(string msg) : base(msg) { }
        }
    }
}
