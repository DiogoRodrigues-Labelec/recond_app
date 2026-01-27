using Recondicionamento_DTC_Routers.Domain;
using Recondicionamento_DTC_Routers.Infra;
using Recondicionamento_DTC_Routers.Services;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
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
        public Func<string, string, bool, bool> AskYesNo { get; set; }
        public Action<string, string> ShowInfo { get; set; }
        public Action<string, string> ShowWarn { get; set; }

        // Workflow hooks
        public Func<CancellationToken, Task> VerifyTestSetupAsync { get; set; } // opcional
        public Func<Task> AskSwapDtcAsync { get; set; }                        // obrigatório
        public Func<CancellationToken, Task<string>> DetectFabricanteAsync { get; set; } // obrigatório

        // Snapshot único (ID + FW)
        public Func<string, CancellationToken, Task<DtcDeviceSnapshot>> GetDeviceSnapshotAsync { get; set; }

        // Fallbacks
        public Func<string, CancellationToken, Task<string>> GetDtcIdAsync { get; set; }
        public Func<string, CancellationToken, Task<string>> GetFirmwareAsync { get; set; }

        public Func<string, string> GetExpectedFirmware { get; set; } // mapping (pode devolver "")
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
                // defaults para SKIP não bloquear conformidade
                ConfigUploaded = true,
                AnalogOk = true,
                EmiPlcOk = true,
                ConformidadeFinal = false
            };

            bool didUpgrade = false;
            string expected = "";

            await _log.LogAsync("=== INÍCIO WORKFLOW DTC ===");

            // Step 0 (opcional)
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

            // 3) Ler ID/Serial (+ FW se vier no snapshot)
            DtcDeviceSnapshot snap = null;
            await RunStepAsync(3, "Ler ID/Serial DTC (web)", ct, async () =>
            {
                if (GetDeviceSnapshotAsync != null)
                {
                    snap = await GetDeviceSnapshotAsync(r.Fabricante, ct);
                    r.NumeroSerie = (snap?.Id ?? "").Trim();
                    r.FirmwareOld = (snap?.Firmware ?? "").Trim();
                }
                else
                {
                    if (GetDtcIdAsync == null)
                        throw new InvalidOperationException("Nem GetDeviceSnapshotAsync nem GetDtcIdAsync estão definidos.");

                    r.NumeroSerie = (await GetDtcIdAsync(r.Fabricante, ct))?.Trim() ?? "";
                }

                SetDetail(3, string.IsNullOrWhiteSpace(r.NumeroSerie) ? "vazio" : r.NumeroSerie);
                await _log.LogAsync($"ID/Serial: {r.NumeroSerie}");
            });

            // 4) Ler FW atual (se não veio no snapshot)
            await RunStepAsync(4, "Ler FW atual", ct, async () =>
            {
                if (string.IsNullOrWhiteSpace(r.FirmwareOld))
                {
                    if (GetFirmwareAsync == null)
                        throw new StepSkippedException("GetFirmwareAsync não definido (sem snapshot).");

                    r.FirmwareOld = (await GetFirmwareAsync(r.Fabricante, ct))?.Trim() ?? "";
                }

                // 🔧 TESTE: descomenta para forçar upgrade mesmo quando já está igual ao esperado
                 r.FirmwareOld = "0.0.0-test";

                SetDetail(4, string.IsNullOrWhiteSpace(r.FirmwareOld) ? "vazio" : r.FirmwareOld);
                await _log.LogAsync($"FW old: {r.FirmwareOld}");
            },
            onSkip: () => { r.FirmwareOld ??= ""; });

            // expected (mapping) — usamos a partir daqui
            expected = (GetExpectedFirmware?.Invoke(r.Fabricante) ?? "").Trim();

            // 5) Upgrade FW (se necessário) — pergunta ao utilizador (SEM validar aqui)
            await RunStepAsync(5, "Upgrade FW (se necessário)", ct, async () =>
            {
                if (string.IsNullOrWhiteSpace(expected))
                    throw new StepSkippedException("Sem firmware esperado (mapping vazio).");

                if (string.Equals(r.FirmwareOld ?? "", expected, StringComparison.OrdinalIgnoreCase))
                {
                    r.FirmwareNew = r.FirmwareOld;
                    SetDetail(5, $"SKIP (já OK) -> {r.FirmwareNew}");
                    throw new StepSkippedException("FW já é o esperado.");
                }

                if (AskYesNo == null)
                    throw new InvalidOperationException("AskYesNo não definido (necessário para confirmar upgrade).");

                bool doIt = AskYesNo(
                    "Upgrade de Firmware",
                    $"FW atual: {r.FirmwareOld}\nFW esperado: {expected}\n\nQueres realizar o upgrade agora?",
                    true);

                if (!doIt)
                {
                    r.FirmwareNew = r.FirmwareOld;
                    SetDetail(5, $"SKIP (user) -> {r.FirmwareNew}");
                    throw new StepSkippedException("Utilizador optou por não fazer upgrade.");
                }

                if (DoUpgradeFirmwareAsync == null)
                    throw new InvalidOperationException($"Precisa upgrade para {expected} mas DoUpgradeFirmwareAsync é null.");

                await DoUpgradeFirmwareAsync(r.Fabricante, ct);
                didUpgrade = true;

                SetDetail(5, "OK (upgrade executado)");
                await _log.LogAsync("Upgrade executado. Vai reler FW no passo seguinte.");
            },
            onSkip: () => { /* nada */ });

            // 6) Relê FW após upgrade + valida (ESTE É O STEP QUE FALTAVA)
            await RunStepAsync(6, "Reler FW após upgrade + validar", ct, async () =>
            {
                if (string.IsNullOrWhiteSpace(expected))
                    throw new StepSkippedException("Sem firmware esperado (mapping vazio).");

                if (!didUpgrade)
                    throw new StepSkippedException("Sem upgrade (não é necessário / user skip).");

                string fwAfter = "";

                if (GetDeviceSnapshotAsync != null)
                    fwAfter = (await GetDeviceSnapshotAsync(r.Fabricante, ct))?.Firmware ?? "";
                else if (GetFirmwareAsync != null)
                    fwAfter = (await GetFirmwareAsync(r.Fabricante, ct)) ?? "";

                r.FirmwareNew = string.IsNullOrWhiteSpace(fwAfter) ? (r.FirmwareOld ?? "") : fwAfter.Trim();

                // Normalização “robusta”
                static string N(string s) => Regex.Replace((s ?? "").ToLowerInvariant(), @"[^a-z0-9]+", "");

                bool match = !string.IsNullOrWhiteSpace(r.FirmwareNew)
                             && (N(r.FirmwareNew).Contains(N(expected)) || N(expected).Contains(N(r.FirmwareNew)));

                if (!match)
                {
                    SetDetail(6, $"FAIL -> {r.FirmwareNew} (esp {expected})");
                    await _log.LogAsync($"FW após upgrade NÃO bate. new={r.FirmwareNew} expected={expected}");
                    throw new Exception($"Firmware após upgrade não é o esperado. Atual={r.FirmwareNew} Esperado={expected}");
                }

                SetDetail(6, $"OK -> {r.FirmwareNew}");
                await _log.LogAsync($"FW após upgrade OK: {r.FirmwareNew}");
            },
            onSkip: () =>
            {
                // se não houve upgrade, mantém coerente
                if (string.IsNullOrWhiteSpace(r.FirmwareNew))
                    r.FirmwareNew = r.FirmwareOld;
            });

            // garante FWNew sempre preenchido
            if (string.IsNullOrWhiteSpace(r.FirmwareNew))
                r.FirmwareNew = r.FirmwareOld;

            // 7) Upload configurações
            await RunStepAsync(7, "Upload configurações (E-REDES)", ct, async () =>
            {
                if (DoUploadConfigAsync == null)
                    throw new StepSkippedException("DoUploadConfigAsync não definido (SKIP).");

                await DoUploadConfigAsync(r.Fabricante, r.NumeroSerie, ct);
                r.ConfigUploaded = true;

                SetDetail(7, "OK");
                await _log.LogAsync("Upload config OK");
            },
            onSkip: () => r.ConfigUploaded = true,
            onFail: () => r.ConfigUploaded = false);

            // 8) s21 DTC  -> IdDC = ID do DTC (ajustado p/ SVM no CIRCUTOR)
            await RunStepAsync(8, "s21 DTC: Tensões/Correntes (WS)", ct, async () =>
            {
                if (TestAnalogInputsAsync == null)
                    throw new StepSkippedException("TestAnalogInputsAsync não definido (SKIP).");

                string idDcDtc = FixDtcIdFors21(r.Fabricante, r.NumeroSerie);

                bool ok = await TestAnalogInputsAsync(r.Fabricante, idDcDtc, ct);
                r.AnalogOk = ok;

                SetDetail(8, ok ? "OK" : "FAIL");
                await _log.LogAsync($"s21(DTC) idDc={idDcDtc} (raw={r.NumeroSerie}) -> {ok}");

                if (!ok) throw new Exception("s21(DTC) FAIL");
            },
            onSkip: () => r.AnalogOk = true,
            onFail: () => r.AnalogOk = false);


            // 9) s21 EMI PLC -> IdDC = emiPlcIdDc (ID do meter vindo do config)
            await RunStepAsync(9, "s21 EMI PLC: Instantâneos (WS)", ct, async () =>
            {
                if (TestEmiPlcAsync == null)
                    throw new StepSkippedException("TestEmiPlcAsync não definido (SKIP).");

                string emiIdDc = Configuration.TryGetEmiIdDc();
                if (string.IsNullOrWhiteSpace(emiIdDc))
                    throw new StepSkippedException("EMI PLC: emiPlcIdDc não definido em config.");

                bool ok = await TestEmiPlcAsync(r.Fabricante, emiIdDc, ct);
                r.EmiPlcOk = ok;

                SetDetail(9, ok ? "OK" : "FAIL");
                await _log.LogAsync($"s21(EMI) idDc={emiIdDc} -> {ok}");

                if (!ok) throw new Exception("s21(EMI) FAIL");
            },
            onSkip: () => r.EmiPlcOk = true,
            onFail: () => r.EmiPlcOk = false);


            static string N(string s) => Regex.Replace((s ?? "").ToLowerInvariant(), @"[^a-z0-9]+", "");

            string expectedFw = (expected ?? "").Trim();
            string actualFw = (r.FirmwareNew ?? r.FirmwareOld ?? "").Trim();

            bool fwOk = string.IsNullOrWhiteSpace(expectedFw)
                        || (!string.IsNullOrWhiteSpace(actualFw) &&
                            (N(actualFw).Contains(N(expectedFw)) || N(expectedFw).Contains(N(actualFw))));

            r.ConformidadeFinal = r.ConfigUploaded && r.AnalogOk && r.EmiPlcOk && fwOk;

            // 10) Report
            await RunStepAsync(10, "Adicionar ao report", ct, async () =>
            {
                if (AddToReport == null)
                    throw new StepSkippedException("AddToReport não definido (SKIP).");

                AddToReport(r);
                SetDetail(10, "OK");
                await _log.LogAsync("Report atualizado.");
            });


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
            }
            catch (Exception ex)
            {
                onFail?.Invoke();
                step.Status = StepStatus.Fail;
                step.Detail = ex.Message;
                await _log.LogAsync($"[{order:00}] {name} - FAIL: {ex.Message}");
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

        public sealed class StepSkippedException : Exception
        {
            public StepSkippedException(string msg) : base(msg) { }
        }


        private static string FixDtcIdFors21(string fabricante, string dtcId)
        {
            if (string.IsNullOrWhiteSpace(dtcId)) return "";

            string id = Regex.Replace(dtcId.Trim(), @"\s+", "");
            string fab = (fabricante ?? "").ToUpperInvariant();
            string up = id.ToUpperInvariant();

            // já está em formato supervisor -> não mexe
            if (up.StartsWith("CIRS") || up.StartsWith("ZIVS"))
                return up;

            // decide prefixo supervisor e prefixo normal
            string pfx = fab.Contains("CIRCUTOR") ? "CIR" :
                         fab.Contains("ZIV") ? "ZIV" : "";

            string spfx = fab.Contains("CIRCUTOR") ? "CIRS" :
                          fab.Contains("ZIV") ? "ZIVS" : "";

            if (string.IsNullOrWhiteSpace(pfx) || string.IsNullOrWhiteSpace(spfx))
                return id; // desconhecido -> não inventa

            // caso "CIR2200..." / "ZIV2200..."
            if (up.StartsWith(pfx))
            {
                string digits = up.Substring(pfx.Length);          // parte após CIR/ZIV
                if (digits.Length > 0) digits = digits.Substring(1); // remove 1º dígito
                return spfx + digits;
            }

            // caso só dígitos (ou mistura) -> extrai dígitos e aplica regra
            string onlyDigits = Regex.Replace(up, @"\D+", "");
            if (onlyDigits.Length > 0) onlyDigits = onlyDigits.Substring(1);
            return spfx + onlyDigits;
        }

    }

    public sealed class DtcDeviceSnapshot
    {
        public string Id { get; set; }
        public string Firmware { get; set; }
    }
}
