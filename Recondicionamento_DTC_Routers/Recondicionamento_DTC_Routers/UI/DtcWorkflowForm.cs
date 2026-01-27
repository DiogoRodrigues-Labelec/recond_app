using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Recondicionamento_DTC_Routers.Domain;
using Recondicionamento_DTC_Routers.helpers;
using Recondicionamento_DTC_Routers.Infra;
using Recondicionamento_DTC_Routers.Workflow;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Recondicionamento_DTC_Routers.Workflow.DtcWorkflowRunner;

namespace Recondicionamento_DTC_Routers.UI
{
    public sealed class DtcWorkflowForm : Form
    {
        private DataGridView _grid;
        private RichTextBox _logBox;

        private TextBox _txtFab;
        private TextBox _txtId;
        private TextBox _txtFwOld;
        private TextBox _txtFwNew;
        private TextBox _txtComment;

        private Button _btnStart;
        private Button _btnCancel;
        private Button _btnNew;

        private Button _btnReportAppend;
        private Button _btnReportNew;
        private Label _lblReportPath;

        // ✅ novos botões
        private Button _btnOpenDtc;
        private Button _btnManualAdd;

        private BindingList<StepVm> _steps;
        private UiLogger _logger;
        private CancellationTokenSource _cts;

        private string _currentReportPath;

        public DtcWorkflowForm()
        {
            InitializeComponent();

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            BuildUiRuntime();
            BuildSteps();

            var resultados = Configuration.GetResultadosDir();
            _currentReportPath = Path.Combine(resultados, "report_dtc.html");

            _logger = new UiLogger(this, _logBox, Path.Combine(resultados, "logs_dtc.txt"));
            UpdateReportLabel();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1100, 650);
            Name = "DtcWorkflowForm";
            Text = "DTC - Sequencial Recondicionamento";
            ResumeLayout(false);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { _cts?.Cancel(); } catch { }
            _cts?.Dispose();
            _cts = null;
            base.OnFormClosing(e);
        }

        // ===================== UI =====================

        private void BuildUiRuntime()
        {
            Text = "DTC - Sequencial Recondicionamento";
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;
            MinimumSize = new Size(1100, 650);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(10)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var header = BuildHeaderPanel();
            root.Controls.Add(header, 0, 0);
            root.SetColumnSpan(header, 2);

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "#", DataPropertyName = "Order", Width = 40 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Passo", DataPropertyName = "Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Estado", DataPropertyName = "Status", Width = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Detalhe", DataPropertyName = "Detail", Width = 220 });

            _grid.RowPrePaint += Grid_RowPrePaint;

            _logBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9),
                WordWrap = false
            };

            root.Controls.Add(_grid, 0, 1);
            root.Controls.Add(_logBox, 1, 1);

            Controls.Clear();
            Controls.Add(root);
        }

        private void Grid_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (_steps == null || e.RowIndex < 0 || e.RowIndex >= _steps.Count) return;
            var s = _steps[e.RowIndex];

            var row = _grid.Rows[e.RowIndex];
            if (s.Status == StepStatus.Running) row.DefaultCellStyle.BackColor = Color.LightYellow;
            else if (s.Status == StepStatus.Ok) row.DefaultCellStyle.BackColor = Color.Honeydew;
            else if (s.Status == StepStatus.Fail) row.DefaultCellStyle.BackColor = Color.MistyRose;
            else if (s.Status == StepStatus.Skipped) row.DefaultCellStyle.BackColor = Color.Gainsboro;
            else row.DefaultCellStyle.BackColor = Color.White;
        }

        private Control BuildHeaderPanel()
        {
            var wrap = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 3,
                RowCount = 2,
                AutoSize = true
            };
            wrap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            wrap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            wrap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));

            var box = new GroupBox { Text = "Dados do DTC", Dock = DockStyle.Fill, Height = 110 };

            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 8,
                RowCount = 2,
                Padding = new Padding(8)
            };
            for (int i = 0; i < 8; i++)
                t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5f));

            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            t.Controls.Add(new Label { Text = "Fabricante", AutoSize = true }, 0, 0);
            _txtFab = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
            t.Controls.Add(_txtFab, 1, 0);
            t.SetColumnSpan(_txtFab, 3);

            t.Controls.Add(new Label { Text = "ID / Serial", AutoSize = true }, 4, 0);
            _txtId = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
            t.Controls.Add(_txtId, 5, 0);
            t.SetColumnSpan(_txtId, 3);

            t.Controls.Add(new Label { Text = "FW Old", AutoSize = true }, 0, 1);
            _txtFwOld = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
            t.Controls.Add(_txtFwOld, 1, 1);
            t.SetColumnSpan(_txtFwOld, 3);

            t.Controls.Add(new Label { Text = "FW New", AutoSize = true }, 4, 1);
            _txtFwNew = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
            t.Controls.Add(_txtFwNew, 5, 1);
            t.SetColumnSpan(_txtFwNew, 3);

            box.Controls.Add(t);

            var boxC = new GroupBox { Text = "Comentário", Dock = DockStyle.Fill, Height = 110 };
            _txtComment = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 70, ScrollBars = ScrollBars.Vertical };
            boxC.Controls.Add(_txtComment);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true
            };

            _lblReportPath = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(170, 0),
                Text = "Report atual:\nreport_dtc.html"
            };

            _btnReportAppend = new Button { Text = "Report: Adicionar", Width = 170, Height = 34 };
            _btnReportNew = new Button { Text = "Report: Novo", Width = 170, Height = 34 };

            _btnReportAppend.Click += (_, __) => UseExistingReport();
            _btnReportNew.Click += (_, __) => CreateNewReport();

            // ✅ novos botões
            _btnOpenDtc = new Button { Text = "Abrir DTC (manual)", Width = 170, Height = 34 };
            _btnOpenDtc.Click += (_, __) => OpenDtcInBrowser();

            _btnManualAdd = new Button { Text = "Adicionar DTC manual", Width = 170, Height = 34 };
            _btnManualAdd.Click += (_, __) => AddManualDtcToReport();

            _btnStart = new Button { Text = "Start Sequencial", Width = 170, Height = 40 };
            _btnCancel = new Button { Text = "Cancel", Width = 170, Height = 40, Enabled = false };
            _btnNew = new Button { Text = "Novo / Reset", Width = 170, Height = 40 };

            _btnStart.Click += async (_, __) => await StartAsync();
            _btnCancel.Click += (_, __) => _cts?.Cancel();
            _btnNew.Click += (_, __) => ResetUi();

            buttons.Controls.Add(_lblReportPath);
            buttons.Controls.Add(_btnReportAppend);
            buttons.Controls.Add(_btnReportNew);
            buttons.Controls.Add(_btnOpenDtc);
            buttons.Controls.Add(_btnManualAdd);
            buttons.Controls.Add(_btnStart);
            buttons.Controls.Add(_btnCancel);
            buttons.Controls.Add(_btnNew);

            wrap.Controls.Add(box, 0, 0);
            wrap.SetRowSpan(box, 2);

            wrap.Controls.Add(boxC, 1, 0);
            wrap.SetRowSpan(boxC, 2);

            wrap.Controls.Add(buttons, 2, 0);
            wrap.SetRowSpan(buttons, 2);

            return wrap;
        }

        private void BuildSteps()
        {
            _steps = new BindingList<StepVm>
            {
                new StepVm(1,  "Substituir DTC no setup (manual)"),
                new StepVm(2,  "Detetar fabricante (HTTP/HTTPS)"),
                new StepVm(3,  "Ler ID/Serial DTC (web)"),
                new StepVm(4,  "Ler FW atual"),
                new StepVm(5,  "Upgrade FW (se necessário)"),

                // ✅ NOVO step (releitura/validação do firmware depois do upgrade) — o runner tem de o implementar
                new StepVm(6,  "Reler FW após upgrade + validar"),

                // ⬇️ tudo a seguir sobe +1
                new StepVm(7,  "Upload configurações (E-REDES)"),
                new StepVm(8,  "s21 DTC: Tensões/Correntes (WS)"),
                new StepVm(9,  "s21 EMI PLC: Instantâneos (WS)"),
                new StepVm(10, "Adicionar ao report"),
            };

            _grid.DataSource = _steps;
        }

        private void ResetUi()
        {
            _logger?.Clear();

            if (_steps != null)
            {
                foreach (var s in _steps)
                {
                    s.Status = StepStatus.Pending;
                    s.Detail = "";
                }
            }

            _txtFab.Text = "";
            _txtId.Text = "";
            _txtFwOld.Text = "";
            _txtFwNew.Text = "";
            _txtComment.Text = "";
        }

        // ===================== Sequencial =====================

        private async Task StartAsync()
        {
            ResetUi();

            _btnStart.Enabled = false;
            _btnCancel.Enabled = true;

            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            try
            {
                var runner = BuildRunner();
                DtcRecord record = await runner.RunAsync(_cts.Token);

                _txtFab.Text = record.Fabricante ?? "";
                _txtId.Text = record.NumeroSerie ?? "";
                _txtFwOld.Text = record.FirmwareOld ?? "";
                _txtFwNew.Text = record.FirmwareNew ?? "";

                await _logger.LogAsync(
                    $"UI atualizada. Conformidade final: {(record.ConformidadeFinal ? "CONFORME" : "NÃO CONFORME")}",
                    toFile: true);
            }
            catch (OperationCanceledException)
            {
                await _logger.LogAsync("Sequencial cancelado.", toFile: true);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync($"Erro: {ex.Message}", toFile: true);
                ShowWarnLocal("Erro", ex.Message);
            }
            finally
            {
                _btnCancel.Enabled = false;
                _btnStart.Enabled = true;
            }
        }

        private DtcWorkflowRunner BuildRunner()
        {
            var sink = new LogSinkAdapter(_logger);

            // ✅ helper CIRCUTOR (reuse)
            var circ = new CircutorDtcHelper(sink);

            var runner = new DtcWorkflowRunner(_steps, sink)
            {
                AskYesNo = (title, message, defaultYes) => AskYesNoLocal(title, message, defaultYes),
                ShowInfo = (t, m) => ShowInfoLocal(t, m),
                ShowWarn = (t, m) => ShowWarnLocal(t, m),

                AskSwapDtcAsync = () =>
                {
                    var res = MessageBox.Show(
                        this,
                        "Substituir o DTC do laboratório pelo DTC de recondicionamento.\n\nQuando estiver pronto, clique OK.\nCancel = cancelar o sequencial.",
                        "Substituição DTC",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Information);

                    if (res != DialogResult.OK) throw new OperationCanceledException();
                    return Task.CompletedTask;
                },

                DetectFabricanteAsync = (ct) =>
                    ExecuteHttpProbeAsync(Configuration.configurationValues.dtcPort, ct, log: true),

                GetDtcIdAsync = async (fab, ct) => (await GetIdDtcAsync(fab, ct)) ?? "",
                GetFirmwareAsync = async (fab, ct) => (await GetFwDtcAsync(fab, ct)) ?? "",

                // ⚠️ mapping esperado (ZIV/CIRCUTOR)
                GetExpectedFirmware = (fab) => GetExpectedFwDtc(fab),

                DoUpgradeFirmwareAsync = async (fab, ct) =>
                {
                    ct.ThrowIfCancellationRequested();

                    bool fazer = AskYesNoLocal(
                        "Upgrade FW (DTC)",
                        $"Fabricante: {fab}\n\nQueres fazer upgrade de firmware agora?\n\nYes = fazer upgrade\nNo = skip",
                        defaultYes: false);

                    if (!fazer)
                        throw new StepSkippedException("SKIP (decisão do utilizador).");

                    if (string.Equals(fab, "CIRCUTOR", StringComparison.OrdinalIgnoreCase))
                    {
                        string tarPath = GetDtcFirmwareFile("CIRCUTOR");
                        await _logger.LogAsync($"[DTC][CIRCUTOR] FW tar: {tarPath}", toFile: true);

                        await circ.UpgradeFirmwareAsync(
                            Configuration.configurationValues.ip,
                            ParsePortOrDefault(Configuration.configurationValues.dtcPort, 80),
                            tarPath,
                            waitRebootSeconds: 600,
                            ct: ct);

                        return;
                    }

                    if (string.Equals(fab, "ZIV", StringComparison.OrdinalIgnoreCase))
                    {
                        string fwPath = GetDtcFirmwareFile("ZIV");
                        await _logger.LogAsync($"[DTC][ZIV] FW file: {fwPath}", toFile: true);

                        await UpgradeFw_ZivDtcAsync(fwPath, ct);
                        return;
                    }

                    // fallback
                    await UpgradeFwDtcManualAsync(fab, ct);
                },

                DoUploadConfigAsync = async (fab, id, ct) =>
                {
                    ct.ThrowIfCancellationRequested();

                    if (string.Equals(fab, "ZIV", StringComparison.OrdinalIgnoreCase))
                    {
                        var cfgPath = Path.Combine(
                            Configuration.configurationValues.Path_ConfigFW,
                            "Configuracao", "DTC", "ZIV", "config_ZIV.txt");

                        await _logger.LogAsync($"Upload config DTC ZIV: {cfgPath}", toFile: true);

                        if (!File.Exists(cfgPath))
                            throw new FileNotFoundException("Ficheiro config DTC ZIV não encontrado.", cfgPath);

                        await UploadConfig_ZivDtcAsync(cfgPath, ct);
                        return;
                    }

                    throw new StepSkippedException($"Upload config: fabricante '{fab}' não suportado (SKIP).");
                },

                // ✅ s21 DTC: usa IdDC do DTC (prefixado por fabricante do DTC)
                TestAnalogInputsAsync = async (fab, idDtc, ct) =>
                {
                    // 1) validar que temos ID do DTC
                    if (string.IsNullOrWhiteSpace(idDtc))
                        throw new Exception("s21(DTC): idDtc está vazio (step 3 falhou / não leu ID).");

                    // 2) construir ID com prefixo (CIR/ZIV/...)
                    string idDcDtc = BuildPrefixedIdDc(fab, idDtc);

                    // 3) aqui é o ponto crítico:
                    //    para o DTC, IdMeters = IdDC = id do DTC prefixado
                    var (okHttp, xml) = await RunWsDcRequestAsync(
                        idRpt: "s21",
                        idDcRaw: idDcDtc,
                        ct: ct,
                        idMetersOverride: idDcDtc
                    );

                    string preview = xml ?? "";
                    if (preview.Length > 3000) preview = preview.Substring(0, 3000) + "\n...\n(TRUNCADO)";

                    bool userOk = AskYesNoLocal(
                        "s21 - Valores Instantâneos (DTC)",
                        $"IdMeters: {idDcDtc}\nIdDC: {idDcDtc}\nHTTP: {(okHttp ? "OK" : "FAIL")}\n\nXML:\n\n{preview}\n\nEstá coerente?",
                        defaultYes: okHttp);

                    await _logger.LogAsync($"s21(DTC) idMeters={idDcDtc} idDc={idDcDtc} httpOk={okHttp} userOk={userOk}", toFile: true);
                    return okHttp && userOk;
                },



                // ✅ s21 EMI: usa IdDC do EMI vindo do config (podes já vir "CIR123" ou "123")
                TestEmiPlcAsync = async (fab, _ignored, ct) =>
                {
                    string emiIdRaw = TryGetEmiIdDc();

                    if (string.IsNullOrWhiteSpace(emiIdRaw))
                    {
                        bool userOk = AskYesNoLocal(
                            "s21 - Valores Instantâneos (EMI PLC)",
                            "emiPlcIdDc não está definido na configuração.\n\nQueres marcar este passo como OK (manual)?",
                            defaultYes: true);

                        await _logger.LogAsync($"s21(EMI) sem emiPlcIdDc -> userOk={userOk}", toFile: true);
                        return userOk; // <-- garante popup SEMPRE
                    }

                    string emiIdDc = BuildPrefixedIdDc("CIRCUTOR", emiIdRaw); // ou "ZIV" conforme o caso

                    var (okHttp, xml) = await RunWsDcRequestAsync(
                        idRpt: "s21",
                        idDcRaw: emiIdDc,
                        ct: ct,
                        idMetersOverride: (Configuration.configurationValues.ns_emi ?? "").Trim()
                    );

                    string preview = xml ?? "";
                    if (preview.Length > 3000) preview = preview.Substring(0, 3000) + "\n...\n(TRUNCADO)";

                    bool userOk2 = AskYesNoLocal(
                        "s21 - Valores Instantâneos (EMI PLC)",
                        $"IdMeters: {Configuration.configurationValues.ns_emi}\nIdDC: {emiIdDc}\nHTTP: {(okHttp ? "OK" : "FAIL")}\n\nXML:\n\n{preview}\n\nEstá coerente?",
                        defaultYes: okHttp);

                    await _logger.LogAsync($"s21(EMI) idMeters={Configuration.configurationValues.ns_emi} idDc={emiIdDc} httpOk={okHttp} userOk={userOk2}", toFile: true);
                    return okHttp && userOk2;
                },

                AddToReport = (r) =>
                {
                    r.Comentario = _txtComment.Text ?? "";

                    _currentReportPath ??= Path.Combine(Configuration.GetResultadosDir(), "report_dtc.html");
                    DtcHtmlReport.AppendRecord(_currentReportPath, r);

                    UpdateReportLabel();
                    ShowInfoLocal("Report", $"Report atualizado em:\n{_currentReportPath}");
                }
            };

            return runner;
        }

        // ===================== Botões manuais =====================

        private void OpenDtcInBrowser()
        {
            try
            {
                string ip = Configuration.configurationValues.ip;
                int port = ParsePortOrDefault(Configuration.configurationValues.dtcPort, 80);
                string url = $"http://{ip}:{port}/";

                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowWarnLocal("Abrir DTC", ex.Message);
            }
        }

        private void AddManualDtcToReport()
        {
            try
            {
                // assume que tens DtcManualEntryForm implementado
                var r = DtcManualEntryForm.Capture(this, GetExpectedFwDtc);
                if (r == null) return;

                _currentReportPath ??= Path.Combine(Configuration.GetResultadosDir(), "report_dtc.html");
                DtcHtmlReport.AppendRecord(_currentReportPath, r);

                UpdateReportLabel();
                _ = _logger.LogAsync($"[MANUAL] DTC adicionado ao report: {r.Fabricante} {r.NumeroSerie}", toFile: true);

                ShowInfoLocal("Report", $"Entrada manual adicionada em:\n{_currentReportPath}");
            }
            catch (Exception ex)
            {
                ShowWarnLocal("Manual DTC", ex.Message);
            }
        }

        private static int ParsePortOrDefault(string portStr, int def)
        {
            if (int.TryParse(portStr, out int p) && p > 0 && p < 65536) return p;
            return def;
        }

        // ===================== Report helpers =====================

        private void UseExistingReport()
        {
            var resultados = Configuration.GetResultadosDir();
            _currentReportPath = Path.Combine(resultados, "report_dtc.html");
            UpdateReportLabel();
            _ = _logger?.LogAsync($"📄 Report selecionado (existente): {_currentReportPath}", toFile: true);
        }

        private void CreateNewReport()
        {
            var resultados = Configuration.GetResultadosDir();
            _currentReportPath = GetNewReportPath(resultados, "report_dtc", ".html");

            DtcHtmlReport.CreateEmpty(_currentReportPath);

            UpdateReportLabel();
            _ = _logger?.LogAsync($"🆕 Novo report criado: {_currentReportPath}", toFile: true);
        }

        private void UpdateReportLabel()
        {
            if (_lblReportPath != null)
                _lblReportPath.Text = $"Report atual:\n{Path.GetFileName(_currentReportPath ?? "report_dtc.html")}";
        }

        private static string GetNewReportPath(string dir, string baseName, string ext)
        {
            string p0 = Path.Combine(dir, baseName + ext);
            if (!File.Exists(p0)) return p0;

            for (int i = 1; i < 10_000; i++)
            {
                string pi = Path.Combine(dir, $"{baseName} ({i}){ext}");
                if (!File.Exists(pi)) return pi;
            }

            throw new Exception("Não foi possível encontrar nome livre para o report.");
        }

        // ===================== Dialogs =====================

        private bool AskYesNoLocal(string title, string msg, bool defaultYes)
        {
            var defaultButton = defaultYes ? MessageBoxDefaultButton.Button1 : MessageBoxDefaultButton.Button2;
            var res = MessageBox.Show(this, msg, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, defaultButton);
            return res == DialogResult.Yes;
        }

        private void ShowInfoLocal(string title, string msg) =>
            MessageBox.Show(this, msg, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

        private void ShowWarnLocal(string title, string msg) =>
            MessageBox.Show(this, msg, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);

        // ===================== DTC: HTTP probe + ID/FW (ZIV/CIRCUTOR) + WS =====================

        private async Task<string> ExecuteHttpProbeAsync(string portStr, CancellationToken ct, bool log)
        {
            int port = ParsePortOrDefault(portStr, 80);

            string ip = Configuration.configurationValues.ip;
            string manufacturer = await ProbeDtcManufacturerAsync(ip, port, ct);

            if (log)
                await _logger.LogAsync($"HTTP probe DTC [{ip}:{port}] -> {manufacturer}", toFile: true);

            return manufacturer;
        }

        private static async Task<string> ProbeDtcManufacturerAsync(string ip, int port, CancellationToken ct)
        {
            static string Detect(string text)
            {
                string up = (text ?? "").ToUpperInvariant();
                if (up.Contains("ZIV")) return "ZIV";
                if (up.Contains("CIRCUTOR")) return "CIRCUTOR";
                return "UNKNOWN";
            }

            foreach (var scheme in new[] { "http", "https" })
            {
                try
                {
                    var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };

                    using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(4) };
                    string url = $"{scheme}://{ip}:{port}/";

                    using var resp = await http.GetAsync(url, ct);
                    string body = await resp.Content.ReadAsStringAsync(ct);

                    var headers = resp.Headers.ToString() + resp.Content.Headers.ToString();
                    string combined = headers + "\n" + body;

                    var d = Detect(combined);
                    if (d != "UNKNOWN") return d;
                }
                catch { }
            }

            // regra: se não deteta ZIV, assume CIRCUTOR (como tinhas antes)
            return "CIRCUTOR";
        }

        private async Task<string> GetIdDtcAsync(string fabricante, CancellationToken ct)
        {
            try
            {
                if (string.Equals(fabricante, "ZIV", StringComparison.OrdinalIgnoreCase))
                    return await GetId_ZivDtcAsync(ct);

                if (string.Equals(fabricante, "CIRCUTOR", StringComparison.OrdinalIgnoreCase))
                {
                    var circ = new CircutorDtcHelper(new LogSinkAdapter(_logger));
                    var s = await circ.GetSnapshotAsync(
                        Configuration.configurationValues.ip,
                        ParsePortOrDefault(Configuration.configurationValues.dtcPort, 80),
                        ct);

                    return s.Id ?? "";
                }

                await _logger.LogAsync($"GetIdDtcAsync: fabricante {fabricante} não implementado.", toFile: true);
                return "";
            }
            catch (Exception ex)
            {
                await _logger.LogAsync($"Erro ao ler ID DTC ({fabricante}): {ex.Message}", toFile: true);
                return "";
            }
        }

        private async Task<string> GetFwDtcAsync(string fabricante, CancellationToken ct)
        {
            if (string.Equals(fabricante, "ZIV", StringComparison.OrdinalIgnoreCase))
                return await GetFw_ZivDtcAsync(ct);

            if (string.Equals(fabricante, "CIRCUTOR", StringComparison.OrdinalIgnoreCase))
            {
                var circ = new CircutorDtcHelper(new LogSinkAdapter(_logger));
                var s = await circ.GetSnapshotAsync(
                    Configuration.configurationValues.ip,
                    ParsePortOrDefault(Configuration.configurationValues.dtcPort, 80),
                    ct);

                return s.Firmware ?? "";
            }

            await _logger.LogAsync($"GetFwDtcAsync: fabricante '{fabricante}' não implementado.", toFile: true);
            return "";
        }

        private Task<string> GetId_ZivDtcAsync(CancellationToken ct) => Task.Run(() =>
        {
            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            var options = new ChromeOptions();
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");

            using IWebDriver driver = new ChromeDriver(service, options);

            string url = $"http://{Configuration.configurationValues.ip}:{ParsePortOrDefault(Configuration.configurationValues.dtcPort, 80)}/";
            driver.Navigate().GoToUrl(url);
            Thread.Sleep(800);

            driver.FindElement(By.XPath("/html/body/div[2]/div/form/table/tbody/tr[1]/td[3]/input"))
                  .SendKeys(Configuration.configurationValues.dtcUser);
            Thread.Sleep(250);

            driver.FindElement(By.XPath("/html/body/div[2]/div/form/table/tbody/tr[2]/td[3]/input"))
                  .SendKeys(Configuration.configurationValues.dtcPass);
            Thread.Sleep(250);

            driver.FindElement(By.XPath("/html/body/div[2]/div/form/div/input")).Click();
            Thread.Sleep(600);

            var elemento = driver.FindElement(By.XPath("html/body/table[1]/tbody/tr/td[5]/table/tbody/tr[4]/td[2]"));
            return elemento.Text ?? "";
        }, ct);

        private Task<string> GetFw_ZivDtcAsync(CancellationToken ct) => Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            var options = new ChromeOptions();
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");

            using IWebDriver driver = new ChromeDriver(service, options);
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

            string baseUrl = $"http://{Configuration.configurationValues.ip}:{ParsePortOrDefault(Configuration.configurationValues.dtcPort, 80)}/";
            driver.Navigate().GoToUrl(baseUrl);

            TryLoginIfNeeded(driver, wait, Configuration.configurationValues.dtcUser, Configuration.configurationValues.dtcPass);

            string fw = ReadValueByLabel(driver, wait,
                "Firmware version",
                "Firmware Version",
                "Firmware");

            return fw?.Trim() ?? "";
        }, ct);

        private async Task UploadConfig_ZivDtcAsync(string configPath, CancellationToken ct)
        {
            if (!File.Exists(configPath))
                throw new FileNotFoundException("Ficheiro não existe.", configPath);

            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            var options = new ChromeOptions();
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");

            await Task.Run(async () =>
            {
                using var driver = new ChromeDriver(service, options);
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

                string baseUrl = $"http://{Configuration.configurationValues.ip}:{ParsePortOrDefault(Configuration.configurationValues.dtcPort, 80)}/";
                driver.Navigate().GoToUrl(baseUrl);

                TryLoginIfNeeded(driver, wait, Configuration.configurationValues.dtcUser, Configuration.configurationValues.dtcPass);
                ClickConfigFiles(driver, wait);

                var fileInput = wait.Until(d => d.FindElement(By.CssSelector("input[type='file']")));
                fileInput.SendKeys(configPath);

                var uploadBtn = wait.Until(d =>
                    TryFind(d, By.XPath("//input[@type='submit' and contains(@value,'Upload')]")) ??
                    TryFind(d, By.XPath("//button[contains(.,'Upload')]"))
                );

                uploadBtn.Click();
                await Task.Delay(TimeSpan.FromMinutes(2), ct);

            }, ct);

            await _logger.LogAsync($"Config DTC ZIV enviada: {Path.GetFileName(configPath)}", toFile: true);
        }

        private static void TryLoginIfNeeded(IWebDriver driver, WebDriverWait wait, string user, string pass)
        {
            var passBox = TryFind(driver, By.CssSelector("input[type='password']"));
            if (passBox == null) return;

            var form = passBox.FindElement(By.XPath("./ancestor::form[1]"));
            var userBox =
                TryFind(form, By.CssSelector("input[type='text']")) ??
                TryFind(form, By.XPath(".//input[not(@type) or @type='text']"));

            if (userBox == null)
                throw new Exception("Login: não encontrei textbox de user.");

            wait.Until(_ =>
            {
                try { return userBox.Displayed && userBox.Enabled && passBox.Displayed && passBox.Enabled; }
                catch { return false; }
            });

            userBox.Clear();
            userBox.SendKeys(user);

            passBox.Clear();
            passBox.SendKeys(pass);

            var btn =
                TryFind(form, By.CssSelector("input[type='submit']")) ??
                TryFind(form, By.CssSelector("button[type='submit']")) ??
                TryFind(form, By.XPath(".//button | .//input[@type='button' or @type='submit']"));

            if (btn != null)
            {
                try { btn.Click(); }
                catch { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", btn); }
            }
            else
            {
                passBox.SendKeys(OpenQA.Selenium.Keys.Enter);
            }

            wait.Until(d => d.PageSource.Length > 2000);
        }

        private static void ClickConfigFiles(IWebDriver driver, WebDriverWait wait)
        {
            var link =
                TryFind(driver, By.LinkText("Configuration files")) ??
                TryFind(driver, By.XPath("//a[normalize-space()='Configuration files']")) ??
                TryFind(driver, By.XPath("//a[contains(.,'Configuration files')]"));

            if (link == null)
                throw new Exception("Não encontrei o link 'Configuration files'.");

            link.Click();
            wait.Until(d => d.FindElements(By.CssSelector("input[type='file']")).Count > 0);
        }

        private static IWebElement TryFind(ISearchContext ctx, By by)
        {
            try { return ctx.FindElement(by); }
            catch { return null; }
        }

        private static string ReadValueByLabel(IWebDriver driver, WebDriverWait wait, params string[] labels)
        {
            wait.Until(d => d.PageSource.Length > 2000);
            const string NBSP = "\u00A0";

            foreach (var label in labels)
            {
                if (string.IsNullOrWhiteSpace(label)) continue;
                string wanted = label.Trim();

                var el = TryFind(driver, By.XPath(
                    $"//tr[td and normalize-space(translate(td[1], '{NBSP}', ' '))='{wanted}']" +
                    $"/td[normalize-space(translate(., '{NBSP}', ' '))!=''][last()]"
                ));

                if (el != null)
                {
                    var txt = (el.Text ?? "").Replace('\u00A0', ' ').Trim();
                    if (!string.IsNullOrWhiteSpace(txt))
                        return txt;
                }

                el = TryFind(driver, By.XPath(
                    $"//tr[td and contains(normalize-space(translate(td[1], '{NBSP}', ' ')), '{wanted}')]" +
                    $"/td[normalize-space(translate(., '{NBSP}', ' '))!=''][last()]"
                ));

                if (el != null)
                {
                    var txt = (el.Text ?? "").Replace('\u00A0', ' ').Trim();
                    if (!string.IsNullOrWhiteSpace(txt))
                        return txt;
                }
            }

            return "";
        }

        // ✅ mapping (o teu snippet)
        private static string GetExpectedFwDtc(string fabricante)
        {
            var map = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ZIV"] = "3.23.99.1.baac1e69",
                ["CIRCUTOR"] = "1.0.25s",
            };

            return map.TryGetValue(fabricante ?? "", out var v) ? (v ?? "") : "";
        }

        private async Task UpgradeFwDtcManualAsync(string fabricante, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            bool fazer = AskYesNoLocal(
                "Upgrade FW (DTC)",
                $"Fabricante: {fabricante}\n\nQueres fazer upgrade de firmware agora?\n\nYes = manual\nNo = skip",
                defaultYes: false);

            if (!fazer)
                throw new StepSkippedException("SKIP (decisão do utilizador).");

            var res = MessageBox.Show(
                this,
                "Faz o upgrade de firmware no DTC (manual).\n\nQuando terminares, clica OK.\nCancel = cancelar o sequencial.",
                "Upgrade FW (manual)",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);

            if (res != DialogResult.OK)
                throw new OperationCanceledException();

            await _logger.LogAsync("A aguardar DTC voltar a responder por HTTP...", toFile: true);

            string baseUrl = $"http://{Configuration.configurationValues.ip}:{ParsePortOrDefault(Configuration.configurationValues.dtcPort, 80)}/";
            await WaitHttpUpAsync(baseUrl, timeoutSeconds: 480, ct);

            await _logger.LogAsync("DTC voltou a responder por HTTP após upgrade.", toFile: true);
        }

        private static async Task WaitHttpUpAsync(string url, int timeoutSeconds, CancellationToken ct)
        {
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            await Task.Delay(5000, ct);

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

            while (DateTime.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    using var resp = await http.GetAsync(url, ct);
                    if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 500)
                        return;
                }
                catch { }

                await Task.Delay(2000, ct);
            }

            throw new Exception($"Timeout: DTC não voltou por HTTP em {timeoutSeconds}s.");
        }

        private async Task<(bool okHttp, string xml)> RunWsDcRequestAsync(
    string idRpt,
    string idDcRaw,
    CancellationToken ct,
    string idMetersOverride = null)
        {
            var url = $"http://{Configuration.configurationValues.ip}:8080/WS_DC/WS_DC.asmx";

            string idMeters = !string.IsNullOrWhiteSpace(idMetersOverride)
                ? idMetersOverride.Trim()
                : (Configuration.configurationValues.ns_emi ?? "").Trim();

            string idDc = NormalizeIdDc(idDcRaw);

            // SOAP (string original)
            string soap = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
  <s:Header/>
  <s:Body>
    <Request xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://www.asais.fr/ns/Saturne/DC/ws"">
      <IdPet>7190</IdPet>
      <IdRpt>{idRpt}</IdRpt>
      <tfStart/>
      <tfEnd/>
      <IdMeters>{idMeters}</IdMeters>
      <Priority>1</Priority>
      <IdDC>{idDc}</IdDC>
    </Request>
  </s:Body>
</s:Envelope>";

            // ----- log "bonito" -----
            static string Trunc(string s, int max)
            {
                if (string.IsNullOrEmpty(s)) return "";
                s = s.Replace("\r\n", "\n");
                return s.Length <= max ? s : s.Substring(0, max) + "\n...\n(TRUNCADO)";
            }

            static string PrettyXml(string xml)
            {
                if (string.IsNullOrWhiteSpace(xml)) return "";
                try
                {
                    var doc = new System.Xml.XmlDocument();
                    doc.LoadXml(xml);

                    var sb = new StringBuilder();
                    using (var xw = System.Xml.XmlWriter.Create(sb, new System.Xml.XmlWriterSettings
                    {
                        Indent = true,
                        IndentChars = "  ",
                        NewLineChars = "\n",
                        NewLineHandling = System.Xml.NewLineHandling.Replace,
                        OmitXmlDeclaration = false
                    }))
                    {
                        doc.Save(xw);
                    }
                    return sb.ToString();
                }
                catch
                {
                    // se não for XML válido (ou vier HTML), devolve como está
                    return xml;
                }
            }

            // log do request (no ficheiro) + resumo no UI
            string soapPretty = PrettyXml(soap);
            await _logger.LogAsync(
                $"[WS_DC][REQ]\n" +
                $"Url      : {url}\n" +
                $"IdRpt    : {idRpt}\n" +
                $"IdMeters : {idMeters}\n" +
                $"IdDC     : {idDc}\n" +
                $"SOAP:\n{Trunc(soapPretty, 4000)}\n",
                toFile: true);

            using var http = new HttpClient();
            using var content = new StringContent(soap, Encoding.UTF8, "text/xml");
            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            req.Headers.Add("SOAPAction", "\"http://www.asais.fr/ns/Saturne/DC/ws/Request\"");

            using var resp = await http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            // response bonito
            string rawPretty = PrettyXml(raw);

            await _logger.LogAsync(
                $"[WS_DC][RESP]\n" +
                $"HTTP     : {(int)resp.StatusCode} {resp.ReasonPhrase}\n" +
                $"Success  : {resp.IsSuccessStatusCode}\n" +
                $"Headers  :\n{Trunc(resp.Headers.ToString() + resp.Content.Headers.ToString(), 2000)}\n" +
                $"Body:\n{Trunc(rawPretty, 6000)}\n",
                toFile: true);

            // (opcional) também podes mandar um resumo curtinho para a UI (sem spam)
            await _logger.LogAsync(
                $"WS_DC {idRpt} | HTTP {(int)resp.StatusCode} | IdMeters={idMeters} | IdDC={idDc}",
                toFile: false);

            return (resp.IsSuccessStatusCode, raw);
        }

        private static string NormalizeIdDc(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            return Regex.Replace(raw.Trim(), @"\s+", "");
        }

        private static string BuildPrefixedIdDc(string fabricante, string id)
        {
            string core = NormalizeIdDc(id);
            if (string.IsNullOrWhiteSpace(core)) return "";

            if (Regex.IsMatch(core, @"^[A-Za-z]{2,4}"))
                return core;

            string pfx = (fabricante ?? "").Trim().ToUpperInvariant();
            pfx =
                pfx.Contains("CIRCUTOR") ? "CIR" :
                pfx.Contains("ZIV") ? "ZIV" :
                pfx.Contains("SAG") ? "SAG" :
                "";

            return string.IsNullOrWhiteSpace(pfx) ? core : (pfx + core);
        }

        private string TryGetEmiIdDc()
        {
            
            return Configuration.configurationValues.ns_emi;
        }

        private async Task UpgradeFw_ZivDtcAsync(string firmwarePath, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(firmwarePath))
                throw new ArgumentException(nameof(firmwarePath));

            firmwarePath = Path.GetFullPath(firmwarePath);

            if (!File.Exists(firmwarePath))
                throw new FileNotFoundException("Firmware ZIV DTC não encontrado.", firmwarePath);

            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            var options = new ChromeOptions();
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");

            string ip = Configuration.configurationValues.ip;
            int port = ParsePortOrDefault(Configuration.configurationValues.dtcPort, 80);
            string baseUrl = $"http://{ip}:{port}/";

            await _logger.LogAsync($"[ZIV DTC] Upgrade start | file='{firmwarePath}' | url={baseUrl}", toFile: true);

            await Task.Run(() =>
            {
                using var driver = new ChromeDriver(service, options);
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));

                driver.Navigate().GoToUrl(baseUrl);

                // Login (se necessário)
                TryLoginIfNeeded(driver, wait,
                    user: Configuration.configurationValues.dtcUser,
                    pass: Configuration.configurationValues.dtcPass);

                // Ir para a página onde existem os iframes do Reflash
                NavigateToReflashRoot_Ziv(driver, wait);

                // Procurar o iframe de upload (src contém /reflash/reflash_upload/)
                IWebElement uploadFrame = wait.Until(d =>
                {
                    try
                    {
                        // tenta por src
                        var fr = TryFind(d, By.CssSelector("iframe[src*='/reflash/reflash_upload']"));
                        if (fr != null) return fr;

                        // fallback: qualquer iframe cujo src contenha reflash_upload
                        var frames = d.FindElements(By.TagName("iframe"));
                        foreach (var f in frames)
                        {
                            var src = (f.GetAttribute("src") ?? "");
                            if (src.IndexOf("reflash_upload", StringComparison.OrdinalIgnoreCase) >= 0)
                                return f;
                        }
                        return null;
                    }
                    catch { return null; }
                });

                if (uploadFrame == null)
                    throw new Exception("[ZIV DTC] Não encontrei iframe de upload (/reflash/reflash_upload/).");

                // Entrar no iframe do upload
                driver.SwitchTo().Frame(uploadFrame);

                // Encontrar input file
                var fileInput = wait.Until(d =>
                {
                    try
                    {
                        var el = d.FindElement(By.CssSelector("input[type='file']"));
                        return el;
                    }
                    catch { return null; }
                });

                if (fileInput == null)
                    throw new Exception("[ZIV DTC] Reflash(upload): não encontrei input[type=file].");

                // Selecionar ficheiro (equivalente a "Escolher ficheiro")
                fileInput.SendKeys(firmwarePath);

                // Confirmar que ficou preenchido
                string picked = "";
                try { picked = (fileInput.GetAttribute("value") ?? "").Trim(); } catch { }

                if (string.IsNullOrWhiteSpace(picked))
                    throw new Exception("[ZIV DTC] Reflash(upload): input[type=file] ficou vazio após SendKeys (não selecionou ficheiro).");

                // Garantir checkbox "Only verify" desmarcada (se existir)
                try
                {
                    var onlyVerify = TryFind(driver, By.CssSelector("input[type='checkbox']"));
                    if (onlyVerify != null && onlyVerify.Enabled && onlyVerify.Selected)
                        SafeClick(driver, onlyVerify);
                }
                catch { /* ignore */ }

                // Clicar botão Reflash (normalmente é input submit/button com value "Reflash")
                var reflashBtn = wait.Until(d =>
                {
                    return
                        TryFind(d, By.XPath("//input[@type='submit' and contains(translate(@value,'REFLASH','reflash'),'reflash')]")) ??
                        TryFind(d, By.XPath("//input[@type='button' and contains(translate(@value,'REFLASH','reflash'),'reflash')]")) ??
                        TryFind(d, By.XPath("//button[contains(.,'Reflash')]"));
                });

                if (reflashBtn == null)
                    throw new Exception("[ZIV DTC] Reflash(upload): não encontrei botão 'Reflash'.");

                SafeClick(driver, reflashBtn);

                // Voltar ao default content
                driver.SwitchTo().DefaultContent();

                // Opcional: ler um preview do status iframe e logar (não falha se não existir)
                try
                {
                    var statusFrame =
                        TryFind(driver, By.CssSelector("iframe#statusdiv")) ??
                        TryFind(driver, By.CssSelector("iframe[src*='/reflash/reflash_status']"));

                    if (statusFrame != null)
                    {
                        driver.SwitchTo().Frame(statusFrame);
                        var txt = "";
                        try { txt = (driver.FindElement(By.TagName("body"))?.Text ?? "").Trim(); } catch { }
                        driver.SwitchTo().DefaultContent();

                        if (!string.IsNullOrWhiteSpace(txt))
                        {
                            if (txt.Length > 700) txt = txt.Substring(0, 700) + "\n...(TRUNCADO)";
                            _ = _logger.LogAsync($"[ZIV DTC] Reflash status (preview):\n{txt}", toFile: true);
                        }
                    }
                }
                catch { }

            }, ct);

            await _logger.LogAsync($"[ZIV DTC] Reflash submetido (upload+click). Aguardar ~90s...", toFile: true);

            // tal como no Circutor: dá tempo para começar a aplicar / reboot
            await Task.Delay(TimeSpan.FromSeconds(90), ct);

            await _logger.LogAsync("[ZIV DTC] A aguardar reboot/HTTP up...", toFile: true);
            await WaitHttpUpAsync(baseUrl, timeoutSeconds: 600, ct);
            await _logger.LogAsync("[ZIV DTC] HTTP up após upgrade.", toFile: true);
        }

        // ---------------- helpers mínimos ----------------

        private static void NavigateToReflashRoot_Ziv(IWebDriver driver, WebDriverWait wait)
        {
            // Se já estiver na página com os iframes, ok
            wait.Until(d => d.PageSource.Length > 1500);

            // tenta clicar no menu "Reflash"
            var reflashLink =
                TryFind(driver, By.XPath("//a[contains(.,'Reflash')]")) ??
                TryFind(driver, By.CssSelector("a[href*='/actions/reflash']"));

            if (reflashLink != null)
            {
                SafeClick(driver, reflashLink);
                wait.Until(d => (d.PageSource ?? "").IndexOf("reflash_upload", StringComparison.OrdinalIgnoreCase) >= 0);
                return;
            }

            // fallback: ir direto ao endpoint
            driver.Navigate().GoToUrl(new Uri(new Uri(driver.Url), "/actions/reflash/").ToString());
            wait.Until(d => (d.PageSource ?? "").IndexOf("reflash_upload", StringComparison.OrdinalIgnoreCase) >= 0);
        }

      

        private static void SafeClick(IWebDriver driver, IWebElement el)
        {
            if (el == null) return;

            try { el.Click(); }
            catch
            {
                try { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", el); }
                catch
                {
                    try { el.SendKeys(OpenQA.Selenium.Keys.Enter); }
                    catch { }
                }
            }
        }


        private static void ForceFileInputVisible(IWebDriver driver, IWebElement fileInput)
        {
            try
            {
                ((IJavaScriptExecutor)driver).ExecuteScript(@"
arguments[0].style.display='block';
arguments[0].style.visibility='visible';
arguments[0].style.opacity=1;
arguments[0].style.height='30px';
arguments[0].style.width='420px';
arguments[0].removeAttribute('hidden');
arguments[0].removeAttribute('disabled');
", fileInput);
            }
            catch { }
        }

        private static string SafeBodyText(IWebDriver driver)
        {
            try { return driver.FindElement(By.TagName("body"))?.Text ?? ""; }
            catch { return ""; }
        }

        private static bool ContainsAny(string text, params string[] needles)
        {
            var up = (text ?? "").ToUpperInvariant();
            foreach (var n in needles)
                if (!string.IsNullOrWhiteSpace(n) && up.Contains(n.ToUpperInvariant()))
                    return true;
            return false;
        }

        private static void TryAcceptAlert(IWebDriver driver, int timeoutSeconds)
        {
            var end = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (DateTime.UtcNow < end)
            {
                try
                {
                    var a = driver.SwitchTo().Alert();
                    a.Accept();
                    return;
                }
                catch { Thread.Sleep(150); }
            }
        }





        private string ResolveDtcFirmwarePath(string relativePathUnderRoot)
        {
            try
            {
                string base1 = Configuration.configurationValues.Path_ConfigFW ?? "";
                if (!string.IsNullOrWhiteSpace(base1))
                {
                    string p1 = Path.Combine(base1, relativePathUnderRoot);
                    if (File.Exists(p1)) return p1;
                }
            }
            catch { }

            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string p2 = Path.Combine(exeDir, relativePathUnderRoot);
            if (File.Exists(p2)) return p2;

            string p3 = Path.GetFullPath(Path.Combine(exeDir, "..", "..", relativePathUnderRoot));
            if (File.Exists(p3)) return p3;

            throw new FileNotFoundException("Firmware DTC não encontrado.", relativePathUnderRoot);
        }

        private string GetDtcFirmwareFile(string fabricante)
        {
            if (string.Equals(fabricante, "CIRCUTOR", StringComparison.OrdinalIgnoreCase))
                return ResolveDtcFirmwarePath(Path.Combine("Firmware", "DTC", "Circutor - 1.0.25s", "CIR_CDC_EDP_V1.0.25s.tar"));

            if (string.Equals(fabricante, "ZIV", StringComparison.OrdinalIgnoreCase))
                return ResolveDtcFirmwarePath(Path.Combine("Firmware", "DTC", "ZIV - 4WF01612020", "cct_xwing3_3_23_99_1_baac1e69_12020"));

            throw new NotSupportedException($"Firmware DTC: fabricante '{fabricante}' não suportado.");
        }
    }
}
