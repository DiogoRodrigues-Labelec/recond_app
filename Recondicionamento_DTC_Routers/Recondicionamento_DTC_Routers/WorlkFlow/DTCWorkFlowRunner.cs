using Recondicionamento_DTC_Routers.Domain;
using Recondicionamento_DTC_Routers.Infra;
using Recondicionamento_DTC_Routers.Services;
using System;
using System.ComponentModel;
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
        public Func<CancellationToken, Task> VerifyTestSetupAsync { get; set; } // opcional (se não usares, mete null)
        public Func<Task> AskSwapDtcAsync { get; set; }

        public Func<CancellationToken, Task<string>> DetectFabricanteAsync { get; set; }
        public Func<string, CancellationToken, Task<string>> GetDtcIdAsync { get; set; }
        public Func<string, CancellationToken, Task<string>> GetFirmwareAsync { get; set; }

        public Func<string, string> GetExpectedFirmware { get; set; } // pode devolver "" se não tiveres mapping
        public Func<string, CancellationToken, Task> DoUpgradeFirmwareAsync { get; set; } // opcional

        public Func<string, string, CancellationToken, Task> DoUploadConfigAsync { get; set; } // opcional

        public Func<string, string, CancellationToken, Task<bool>> TestAnalogInputsAsync { get; set; } // S01 DTC
        public Func<string, string, CancellationToken, Task<bool>> TestEmiPlcAsync { get; set; }       // S01 EMI

        public Action<DtcRecord> AddToReport { get; set; } // opcional

        public async Task<DtcRecord> RunAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var r = new DtcRecord
            {
                ConfigUploaded = true,  // se um passo for SKIP, não queremos bloquear conformidade
                AnalogOk = true,
                EmiPlcOk = true,
                ConformidadeFinal = false
            };

            await _log.LogAsync("DTC workflow started.");

            // (Opcional) Step 0: setup
            if (VerifyTestSetupAsync != null)
            {
                await RunStepAsync(0, "Validar setup", async () =>
                {
                    await VerifyTestSetupAsync(ct);
                }, ct);
            }

            // 1) Manual swap
            await RunStepAsync(1, "Substituir DTC no setup (manual)", async () =>
            {
                if (AskSwapDtcAsync == null) throw new InvalidOperationException("AskSwapDtcAsync não definido.");
                await AskSwapDtcAsync();
            }, ct);

            // 2) Detect fabricante
            await RunStepAsync(2, "Detetar fabricante (HTTP/HTTPS)", async () =>
            {
                if (DetectFabricanteAsync == null) throw new InvalidOperationException("DetectFabricanteAsync não definido.");
                r.Fabricante = (await DetectFabricanteAsync(ct))?.Trim();
                if (string.IsNullOrWhiteSpace(r.Fabricante)) r.Fabricante = "UNKNOWN";
                SetDetail(2, r.Fabricante);
                await _log.LogAsync($"Fabricante: {r.Fabricante}");
            }, ct);

            // 3) Ler ID/Serial
            await RunStepAsync(3, "Ler ID/Serial DTC (web)", async () =>
            {
                if (GetDtcIdAsync == null) throw new InvalidOperationException("GetDtcIdAsync não definido.");
                r.NumeroSerie = (await GetDtcIdAsync(r.Fabricante, ct))?.Trim();
                SetDetail(3, r.NumeroSerie ?? "");
                await _log.LogAsync($"ID/Serial: {r.NumeroSerie}");
            }, ct);

            // 4) Ler FW atual
            await RunStepAsync(4, "Ler FW atual", async () =>
            {
                if (GetFirmwareAsync == null) throw new InvalidOperationException("GetFirmwareAsync não definido.");
                r.FirmwareOld = (await GetFirmwareAsync(r.Fabricante, ct))?.Trim();
                SetDetail(4, r.FirmwareOld ?? "");
                await _log.LogAsync($"FW old: {r.FirmwareOld}");
            }, ct);

            // 5) Upgrade FW (se necessário)
            await RunStepMaybeSkipAsync(5, "Upgrade FW (se necessário)", async () =>
            {
                var expected = GetExpectedFirmware?.Invoke(r.Fabricante) ?? "";
                expected = expected.Trim();

                if (string.IsNullOrWhiteSpace(expected))
                {
                    // Sem mapping -> SKIP
                    throw new SkipStepException("Sem firmware esperado (mapping vazio).");
                }

                // Se já está OK -> SKIP
                if (string.Equals(r.FirmwareOld ?? "", expected, StringComparison.OrdinalIgnoreCase))
                {
                    r.FirmwareNew = r.FirmwareOld;
                    throw new SkipStepException("FW já é o esperado.");
                }

                // Precisa upgrade
                if (DoUpgradeFirmwareAsync == null)
                    throw new InvalidOperationException($"Precisa upgrade para {expected} mas DoUpgradeFirmwareAsync é null.");

                await DoUpgradeFirmwareAsync(r.Fabricante, ct);
                r.FirmwareNew = expected;
                SetDetail(5, $"OK -> {expected}");
                await _log.LogAsync($"FW upgrade -> {expected}");
            }, ct);

            // Se não houve upgrade, define FW new = old (para report)
            if (string.IsNullOrWhiteSpace(r.FirmwareNew))
                r.FirmwareNew = r.FirmwareOld;

            // 6) Upload configurações
            await RunStepMaybeSkipAsync(6, "Upload configurações (E-REDES)", async () =>
            {
                if (DoUploadConfigAsync == null)
                    throw new SkipStepException("DoUploadConfigAsync não definido (SKIP).");

                await DoUploadConfigAsync(r.Fabricante, r.NumeroSerie, ct);
                r.ConfigUploaded = true;
                SetDetail(6, "OK");
                await _log.LogAsync("Upload config OK");
            }, ct,
            onFail: () => r.ConfigUploaded = false,
            onSkip: () => r.ConfigUploaded = true);

            // 7) S01 DTC (tensões/correntes)
            await RunStepMaybeSkipAsync(7, "S01 DTC: Tensões/Correntes (WS)", async () =>
            {
                if (TestAnalogInputsAsync == null)
                    throw new SkipStepException("TestAnalogInputsAsync não definido (SKIP).");

                bool ok = await TestAnalogInputsAsync(r.Fabricante, r.NumeroSerie, ct);
                r.AnalogOk = ok;
                SetDetail(7, ok ? "OK" : "FAIL");
                await _log.LogAsync($"S01(DTC) -> {ok}");
                if (!ok) throw new Exception("S01(DTC) FAIL");
            }, ct,
            onFail: () => r.AnalogOk = false,
            onSkip: () => r.AnalogOk = true);

            // 8) S01 EMI PLC
            await RunStepMaybeSkipAsync(8, "S01 EMI PLC: Instantâneos (WS)", async () =>
            {
                if (TestEmiPlcAsync == null)
                    throw new SkipStepException("TestEmiPlcAsync não definido (SKIP).");

                bool ok = await TestEmiPlcAsync(r.Fabricante, r.NumeroSerie, ct);
                r.EmiPlcOk = ok;
                SetDetail(8, ok ? "OK" : "FAIL");
                await _log.LogAsync($"S01(EMI) -> {ok}");
                if (!ok) throw new Exception("S01(EMI) FAIL");
            }, ct,
            onFail: () => r.EmiPlcOk = false,
            onSkip: () => r.EmiPlcOk = true);

            // 9) Report
            await RunStepMaybeSkipAsync(9, "Adicionar ao report", () =>
            {
                if (AddToReport == null)
                    throw new SkipStepException("AddToReport não definido (SKIP).");

                AddToReport(r);
                SetDetail(9, "OK");
                return Task.CompletedTask;
            }, ct);

            // conformidade final
            string expectedFw = (GetExpectedFirmware?.Invoke(r.Fabricante) ?? "").Trim();
            bool fwOk = string.IsNullOrWhiteSpace(expectedFw)
                        || string.Equals(r.FirmwareNew ?? r.FirmwareOld ?? "", expectedFw, StringComparison.OrdinalIgnoreCase);

            r.ConformidadeFinal = r.ConfigUploaded && r.AnalogOk && r.EmiPlcOk && fwOk;

            await _log.LogAsync($"Workflow finished. ConformidadeFinal={r.ConformidadeFinal}");
            return r;
        }

        // ---------------- helpers ----------------

        private async Task RunStepAsync(int order, string name, Func<Task> body, CancellationToken ct)
        {
            var step = FindStep(order, name);
            SetStep(step, StepStatus.Running, "");

            try
            {
                ct.ThrowIfCancellationRequested();
                await body();
                SetStep(step, StepStatus.Ok, step.Detail);
            }
            catch
            {
                SetStep(step, StepStatus.Fail, step.Detail);
                throw;
            }
        }

        private async Task RunStepMaybeSkipAsync(int order, string name, Func<Task> body, CancellationToken ct,
            Action onFail = null, Action onSkip = null)
        {
            var step = FindStep(order, name);
            SetStep(step, StepStatus.Running, "");

            try
            {
                ct.ThrowIfCancellationRequested();
                await body();
                SetStep(step, StepStatus.Ok, step.Detail);
            }
            catch (SkipStepException ex)
            {
                onSkip?.Invoke();
                SetStep(step, StepStatus.Skipped, ex.Message);
            }
            catch
            {
                onFail?.Invoke();
                SetStep(step, StepStatus.Fail, step.Detail);
                throw;
            }
        }

        private StepVm FindStep(int order, string fallbackName)
        {
            foreach (var s in _steps)
                if (s.Order == order) return s;

            // se não existir (por exemplo Step 0), cria um “fantasma”
            var vm = new StepVm(order, fallbackName);
            _steps.Add(vm);
            return vm;
        }

        private void SetStep(StepVm s, StepStatus st, string detail)
        {
            s.Status = st;
            if (detail != null) s.Detail = detail;
        }

        private void SetDetail(int order, string detail)
        {
            foreach (var s in _steps)
                if (s.Order == order)
                {
                    s.Detail = detail ?? "";
                    return;
                }
        }

        private sealed class SkipStepException : Exception
        {
            public SkipStepException(string msg) : base(msg) { }
        }
    }
}
