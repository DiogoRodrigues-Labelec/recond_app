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

        // Workflow hooks
        public Func<CancellationToken, Task> VerifyTestSetupAsync { get; set; } // opcional
        public Func<Task> AskSwapDtcAsync { get; set; }                        // obrigatório
        public Func<CancellationToken, Task<string>> DetectFabricanteAsync { get; set; } // obrigatório

        // ✅ NOVO (opcional): Snapshot único (ID + FW) numa só sessão Selenium
        public Func<string, CancellationToken, Task<DtcDeviceSnapshot>> GetDeviceSnapshotAsync { get; set; }

        // ✅ COMPAT com o teu Form atual:
        // O teu Form já faz cache internamente, por isso chamar os 2 não cria 2 sessões Selenium.
        public Func<string, CancellationToken, Task<string>> GetDtcIdAsync { get; set; }      // opcional (fallback)
        public Func<string, CancellationToken, Task<string>> GetFirmwareAsync { get; set; }   // opcional (fallback)

        public Func<string, string> GetExpectedFirmware { get; set; } // pode devolver "" (sem mapping)
        public Func<string, CancellationToken, Task> DoUpgradeFirmwareAsync { get; set; } // opcional (manual)

        public Func<string, string, CancellationToken, Task> DoUploadConfigAsync { get; set; } // opcional
        public Func<string, string, CancellationToken, Task<bool>> TestAnalogInputsAsync { get; set; } // opcional
        public Func<string, string, CancellationToken, Task<bool>> TestEmiPlcAsync { get; set; }       // opcional

        public Action<DtcRecord> AddToReport { get; set; } // opcional

        public async Task<DtcRecord> RunAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var r = new DtcRecord
            {
                // defaults para SKIP não bloquear conformidade
                ConfigUploaded = true,
                AnalogOk = true,
                EmiPlcOk = true,
                ConformidadeFinal = false
            };

            await _log.LogAsync("=== INÍCIO WORKFLOW DTC ===");

            // Step 0 (opcional) — se não existir na grid, RunStepAsync ignora
            if (VerifyTestSetupAsync != null)
            {
                await RunStepAsync(0, "Validar setup", ct, async () =>
                {
                    await VerifyTestSetupAsync(ct);
                });
            }

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

            // 3) Ler ID/Serial (web) + (se existir) FW no snapshot
            DtcDeviceSnapshot snap = null;
            await RunStepAsync(3, "Ler ID/Serial DTC (web)", ct, async () =>
            {
                // Preferência: snapshot (ID+FW)
                if (GetDeviceSnapshotAsync != null)
                {
                    snap = await GetDeviceSnapshotAsync(r.Fabricante, ct);
                    r.NumeroSerie = (snap?.Id ?? "").Trim();
                    r.FirmwareOld = (snap?.Firmware ?? "").Trim();
                }
                else
                {
                    // Fallback: API antiga do Form (ID separado)
                    if (GetDtcIdAsync == null)
                        throw new InvalidOperationException("Nem GetDeviceSnapshotAsync nem GetDtcIdAsync estão definidos.");

                    r.NumeroSerie = (await GetDtcIdAsync(r.Fabricante, ct))?.Trim() ?? "";
                }

                SetDetail(3, string.IsNullOrWhiteSpace(r.NumeroSerie) ? "vazio" : r.NumeroSerie);
                await _log.LogAsync($"ID/Serial: {r.NumeroSerie}");
            });

            // 4) Ler FW atual
            await RunStepAsync(4, "Ler FW atual", ct, async () =>
            {
                // Se veio no snapshot do step 3, ótimo
                if (string.IsNullOrWhiteSpace(r.FirmwareOld))
                {
                    if (GetFirmwareAsync == null)
                        throw new StepSkippedException("GetFirmwareAsync não definido (sem snapshot).");

                    r.FirmwareOld = (await GetFirmwareAsync(r.Fabricante, ct))?.Trim() ?? "";
                }

                SetDetail(4, string.IsNullOrWhiteSpace(r.FirmwareOld) ? "vazio" : r.FirmwareOld);
                await _log.LogAsync($"FW old: {r.FirmwareOld}");
            },
            onSkip: () =>
            {
                // se não conseguimos ler FW, mantém vazio e deixa conformidade depender do mapping
                r.FirmwareOld ??= "";
            });

            // 5) Upgrade FW (se necessário) — AGORA pergunta ao utilizador
            await RunStepAsync(5, "Upgrade FW (se necessário)", ct, async () =>
            {
                string expected = (GetExpectedFirmware?.Invoke(r.Fabricante) ?? "").Trim();

                // Sem mapping -> SKIP
                if (string.IsNullOrWhiteSpace(expected))
                    throw new StepSkippedException("Sem firmware esperado (mapping vazio).");

                // Já OK -> SKIP
                if (string.Equals(r.FirmwareOld ?? "", expected, StringComparison.OrdinalIgnoreCase))
                {
                    r.FirmwareNew = r.FirmwareOld;
                    throw new StepSkippedException("FW já é o esperado.");
                }

                // ✅ Pergunta se quer fazer upgrade
                if (AskYesNo == null)
                    throw new InvalidOperationException("AskYesNo não definido (necessário para confirmar upgrade).");

                bool doIt = AskYesNo(
                    "Upgrade de Firmware",
                    $"FW atual: {r.FirmwareOld}\nFW esperado: {expected}\n\nQueres realizar o upgrade agora (manual)?",
                    true);


                if (!doIt)
                    throw new StepSkippedException("Utilizador optou por não fazer upgrade.");

                if (DoUpgradeFirmwareAsync == null)
                    throw new InvalidOperationException($"Precisa upgrade para {expected} mas DoUpgradeFirmwareAsync é null.");

                // ✅ Executa upgrade manual (o teu método já espera reboot)
                await DoUpgradeFirmwareAsync(r.Fabricante, ct);

                // ✅ Depois do upgrade: lê firmware novamente
                string fwAfter = "";

                if (GetDeviceSnapshotAsync != null)
                {
                    var snapAfter = await GetDeviceSnapshotAsync(r.Fabricante, ct);
                    fwAfter = (snapAfter?.Firmware ?? "").Trim();
                }
                else if (GetFirmwareAsync != null)
                {
                    fwAfter = (await GetFirmwareAsync(r.Fabricante, ct))?.Trim() ?? "";
                }

                r.FirmwareNew = string.IsNullOrWhiteSpace(fwAfter) ? (r.FirmwareOld ?? "") : fwAfter;

                SetDetail(5, $"OK -> {r.FirmwareNew}");
                await _log.LogAsync($"FW após upgrade: {r.FirmwareNew}");
            });

            // Se step 5 foi SKIP/FAIL, garante FW new preenchido
            if (string.IsNullOrWhiteSpace(r.FirmwareNew))
                r.FirmwareNew = r.FirmwareOld;

            // 6) Upload configurações
            await RunStepAsync(6, "Upload configurações (E-REDES)", ct, async () =>
            {
                if (DoUploadConfigAsync == null)
                    throw new StepSkippedException("DoUploadConfigAsync não definido (SKIP).");

                await DoUploadConfigAsync(r.Fabricante, r.NumeroSerie, ct);
                r.ConfigUploaded = true;

                SetDetail(6, "OK");
                await _log.LogAsync("Upload config OK");
            },
            onSkip: () => r.ConfigUploaded = true,
            onFail: () => r.ConfigUploaded = false);

            // 7) S01 DTC
            await RunStepAsync(7, "S01 DTC: Tensões/Correntes (WS)", ct, async () =>
            {
                if (TestAnalogInputsAsync == null)
                    throw new StepSkippedException("TestAnalogInputsAsync não definido (SKIP).");

                bool ok = await TestAnalogInputsAsync(r.Fabricante, r.NumeroSerie, ct);
                r.AnalogOk = ok;

                SetDetail(7, ok ? "OK" : "FAIL");
                await _log.LogAsync($"S01(DTC) -> {ok}");

                if (!ok) throw new Exception("S01(DTC) FAIL");
            },
            onSkip: () => r.AnalogOk = true,
            onFail: () => r.AnalogOk = false);

            // 8) S01 EMI PLC
            await RunStepAsync(8, "S01 EMI PLC: Instantâneos (WS)", ct, async () =>
            {
                if (TestEmiPlcAsync == null)
                    throw new StepSkippedException("TestEmiPlcAsync não definido (SKIP).");

                bool ok = await TestEmiPlcAsync(r.Fabricante, r.NumeroSerie, ct);
                r.EmiPlcOk = ok;

                SetDetail(8, ok ? "OK" : "FAIL");
                await _log.LogAsync($"S01(EMI) -> {ok}");

                if (!ok) throw new Exception("S01(EMI) FAIL");
            },
            onSkip: () => r.EmiPlcOk = true,
            onFail: () => r.EmiPlcOk = false);

            // 9) Report
            await RunStepAsync(9, "Adicionar ao report", ct, async () =>
            {
                if (AddToReport == null)
                    throw new StepSkippedException("AddToReport não definido (SKIP).");

                AddToReport(r);
                SetDetail(9, "OK");
                await _log.LogAsync("Report atualizado.");
            });

            // Conformidade final
            string expectedFw = (GetExpectedFirmware?.Invoke(r.Fabricante) ?? "").Trim();
            bool fwOk = string.IsNullOrWhiteSpace(expectedFw)
                        || string.Equals(r.FirmwareNew ?? r.FirmwareOld ?? "", expectedFw, StringComparison.OrdinalIgnoreCase);

            r.ConformidadeFinal = r.ConfigUploaded && r.AnalogOk && r.EmiPlcOk && fwOk;

            await _log.LogAsync($"=== FIM WORKFLOW DTC | Conformidade: {(r.ConformidadeFinal ? "CONFORME" : "NÃO CONFORME")} ===");
            return r;
        }

        // ---------------- helpers ----------------

        private async Task RunStepAsync(
            int order,
            string name,
            CancellationToken ct,
            Func<Task> action,
            Action onSkip = null,
            Action onFail = null)
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
                step.Status = StepStatus.Ok;
                await _log.LogAsync($"[{order:00}] {name} - OK");
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
                // não faz throw -> continua workflow
            }
            catch (Exception ex)
            {
                onFail?.Invoke();
                step.Status = StepStatus.Fail;
                step.Detail = ex.Message;
                await _log.LogAsync($"[{order:00}] {name} - FAIL: {ex.Message}");
                // não faz throw -> continua workflow
            }
        }

        private void SetDetail(int order, string detail)
        {
            foreach (var s in _steps)
            {
                if (s.Order == order)
                {
                    s.Detail = detail ?? "";
                    return;
                }
            }
        }

        // Exceção pública para poderes usar no Form (upgrade manual -> SKIP)
        public sealed class StepSkippedException : Exception
        {
            public StepSkippedException(string msg) : base(msg) { }
        }
    }

    // Snapshot simples (ID + FW)
    public sealed class DtcDeviceSnapshot
    {
        public string Id { get; set; }
        public string Firmware { get; set; }
    }
}
