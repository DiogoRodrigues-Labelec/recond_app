using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Recondicionamento_DTC_Routers.Domain;
using Recondicionamento_DTC_Routers.helpers;
using Recondicionamento_DTC_Routers.Infra;
using Recondicionamento_DTC_Routers.Workflow;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Recondicionamento_DTC_Routers.Workflow.RouterWorkflowRunner;

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

        private BindingList<StepVm> _steps;
        private UiLogger _logger;
        private CancellationTokenSource _cts;

        private string _currentReportPath;

        private static Task Delay(int ms, CancellationToken ct) => Task.Delay(ms, ct);


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

        #region UI

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

            _btnStart = new Button { Text = "Start Sequencial", Width = 170, Height = 40 };
            _btnCancel = new Button { Text = "Cancel", Width = 170, Height = 40, Enabled = false };
            _btnNew = new Button { Text = "Novo / Reset", Width = 170, Height = 40 };

            _btnStart.Click += async (_, __) => await StartAsync();
            _btnCancel.Click += (_, __) => _cts?.Cancel();
            _btnNew.Click += (_, __) => ResetUi();

            buttons.Controls.Add(_lblReportPath);
            buttons.Controls.Add(_btnReportAppend);
            buttons.Controls.Add(_btnReportNew);
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
            // Alinhado com o teu procedimento DTC:
            // Web UI -> FW -> upload config -> S01 DTC -> S01 EMI -> report
            _steps = new BindingList<StepVm>
            {
                new StepVm(1,  "Substituir DTC no setup (manual)"),
                new StepVm(2,  "Detetar fabricante (HTTP/HTTPS)"),
                new StepVm(3,  "Ler ID/Serial DTC (web)"),
                new StepVm(4,  "Ler FW atual"),
                new StepVm(5,  "Upgrade FW (se necessário)"),
                new StepVm(6,  "Upload configurações (E-REDES)"),
                new StepVm(7,  "S01 DTC: Tensões/Correntes (WS)"),
                new StepVm(8,  "S01 EMI PLC: Instantâneos (WS)"),
                new StepVm(9,  "Adicionar ao report"),
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

        #endregion

        #region Sequencial

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

                // UI
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

            var runner = new DtcWorkflowRunner(_steps, sink)
            {
                AskYesNo = (title, message, defaultYes) => AskYesNoLocal(title, message, defaultYes),
                ShowInfo = (t, m) => ShowInfoLocal(t, m),
                ShowWarn = (t, m) => ShowWarnLocal(t, m),

                // 1) Manual swap
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

                // 2) Fabricante
                DetectFabricanteAsync = (ct) =>
                    ExecuteHttpProbeAsync(Configuration.configurationValues.dtcPort, ct, log: true),

                // 3) ID/Serial
                GetDtcIdAsync = async (fab, ct) =>
                    (await GetIdDtcAsync(fab, ct)) ?? "",

                // 4) FW
                GetFirmwareAsync = async (fab, ct) =>
                    (await GetFwDtcAsync(fab, ct)) ?? "",

                // 5) Firmware esperado
                GetExpectedFirmware = (fab) => GetExpectedFwDtc(fab),

                // 5) Upgrade: manual (popup)
                DoUpgradeFirmwareAsync = async (fab, ct) =>
                    await UpgradeFwDtcManualAsync(fab, ct),


                // 6) Upload Config (AQUI é onde entra o ZIV/CIRCUTOR)
                DoUploadConfigAsync = async (fab, id, ct) =>
                {
                    ct.ThrowIfCancellationRequested();

                    if (string.Equals(fab, "ZIV", StringComparison.OrdinalIgnoreCase))
                    {
                        // Disseste: configuracao/dtc/config_ZIV.txt
                        var cfgPath = Path.Combine(
                            Configuration.configurationValues.Path_ConfigFW,
                            "Configuracao", "DTC","ZIV", "config_ZIV.txt");

                        await _logger.LogAsync($"Upload config DTC ZIV: {cfgPath}", toFile: true);

                        if (!File.Exists(cfgPath))
                            throw new FileNotFoundException("Ficheiro config DTC ZIV não encontrado.", cfgPath);

                        await UploadConfig_ZivDtcAsync(cfgPath, ct);
                        return;
                    }

                    if (string.Equals(fab, "CIRCUTOR", StringComparison.OrdinalIgnoreCase))
                    {
                        // Placeholder: ajusta o nome real do ficheiro quando o tiveres
                        var cfgPath = Path.Combine(
                            Configuration.configurationValues.Path_ConfigFW,
                            "Configuracao", "DTC","CIRCUTO", "config_CIRCUTOR.txt");

                        await _logger.LogAsync($"Upload config DTC CIRCUTOR: {cfgPath}", toFile: true);

                        if (!File.Exists(cfgPath))
                            throw new FileNotFoundException("Ficheiro config DTC CIRCUTOR não encontrado.", cfgPath);

                        await UploadConfig_CircutorDtcAsync(cfgPath, ct); // por agora NotImplemented
                        return;
                    }

                    throw new NotSupportedException($"Upload config: fabricante '{fab}' não suportado.");
                },

                // 7) S01 DTC
                TestAnalogInputsAsync = async (fab, idDtc, ct) =>
                {
                    var (okHttp, xml) = await RunWsDcRequestAsync(idRpt: "S01", idDc: idDtc, ct: ct);

                    string preview = xml ?? "";
                    if (preview.Length > 3000) preview = preview.Substring(0, 3000) + "\n...\n(TRUNCADO)";

                    bool userOk = AskYesNoLocal(
                        "S01 - Valores Instantâneos (DTC)",
                        $"HTTP: {(okHttp ? "OK" : "FAIL")}\n\nXML recebido:\n\n{preview}\n\nEstá coerente com os valores aplicados?",
                        defaultYes: okHttp);

                    await _logger.LogAsync($"S01(DTC) idDc={idDtc} httpOk={okHttp} userOk={userOk}", toFile: true);
                    return okHttp && userOk;
                },

                // 8) S01 EMI PLC
                TestEmiPlcAsync = async (fab, idDtc, ct) =>
                {
                    string emiIdDc = TryGetEmiIdDc();
                    if (string.IsNullOrWhiteSpace(emiIdDc))
                    {
                        await _logger.LogAsync("EMI PLC: emiIdDc não definido em config -> SKIP lógico.", toFile: true);
                        return true;
                    }

                    var (okHttp, xml) = await RunWsDcRequestAsync(idRpt: "S01", idDc: emiIdDc, ct: ct);

                    string preview = xml ?? "";
                    if (preview.Length > 3000) preview = preview.Substring(0, 3000) + "\n...\n(TRUNCADO)";

                    bool userOk = AskYesNoLocal(
                        "S01 - Valores Instantâneos (EMI PLC)",
                        $"EMI IdDC: {emiIdDc}\nHTTP: {(okHttp ? "OK" : "FAIL")}\n\nXML recebido:\n\n{preview}\n\nEstá coerente?",
                        defaultYes: okHttp);

                    await _logger.LogAsync($"S01(EMI) idDc={emiIdDc} httpOk={okHttp} userOk={userOk}", toFile: true);
                    return okHttp && userOk;
                },

                // 9) Report
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


        #endregion

        #region Report helpers

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

        #endregion

        #region Dialogs

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

        #endregion

        #region DTC: HTTP probe + ID (ZIV) + FW + WS

        private async Task<string> ExecuteHttpProbeAsync(string portStr, CancellationToken ct, bool log)
        {
            if (!int.TryParse(portStr, out int port)) port = 80;

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
                if (string.IsNullOrWhiteSpace(text)) return "UNKNOWN";
                string up = text.ToUpperInvariant();

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
                catch
                {
                    // tenta o próximo
                }
            }

            return "UNKNOWN";
        }

        private async Task<string> GetIdDtcAsync(string fabricante, CancellationToken ct)
        {
            try
            {
                if (string.Equals(fabricante, "ZIV", StringComparison.OrdinalIgnoreCase))
                    return await GetId_ZivDtcAsync(ct);

                await _logger.LogAsync($"GetIdDtcAsync: fabricante {fabricante} não implementado.", toFile: true);
                return "";
            }
            catch (Exception ex)
            {
                await _logger.LogAsync($"Erro ao ler ID DTC ({fabricante}): {ex.Message}", toFile: true);
                return "";
            }
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

            string url = "http://" + Configuration.configurationValues.ip + ":" + Configuration.configurationValues.dtcPort;
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


        private async Task UploadConfig_ZivDtcAsync(string configPath, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(configPath)) throw new ArgumentException(nameof(configPath));
            if (!File.Exists(configPath)) throw new FileNotFoundException("Ficheiro não existe.", configPath);

            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            var options = new ChromeOptions();
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");

            // (Opcional mas recomendado no WinForms) não congelar UI
            await Task.Run(async () =>
            {
                using var driver = new ChromeDriver(service, options);
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

                string baseUrl = $"http://{Configuration.configurationValues.ip}:{Configuration.configurationValues.dtcPort}/";
                driver.Navigate().GoToUrl(baseUrl);

                TryLoginIfNeeded(driver, wait);

                ClickConfigFiles(driver, wait);

                var fileInput = wait.Until(d => d.FindElement(By.CssSelector("input[type='file']")));
                fileInput.SendKeys(configPath);

                // Only verify (se existir)
                var onlyVerify = TryFind(driver, By.CssSelector("input[type='checkbox']"));
                if (onlyVerify != null && onlyVerify.Selected)
                    onlyVerify.Click();

                // Botão Upload configuration (no print é um <input value="Upload configuration">)
                var uploadBtn = wait.Until(d =>
                    TryFind(d, By.XPath("//input[@type='submit' and contains(@value,'Upload')]")) ??
                    TryFind(d, By.XPath("//button[contains(.,'Upload')]"))
                );

                uploadBtn.Click();

                // Aqui é o que disseste: "carregar em upload e esperar uns minutos"
                // (sem inventar parsing)
                await Task.Delay(TimeSpan.FromMinutes(2), ct);

            }, ct);

            await _logger.LogAsync($"Config DTC ZIV enviada: {Path.GetFileName(configPath)}", toFile: true);
        }

        private static void TryLoginIfNeeded(IWebDriver driver, WebDriverWait wait)
        {
            // Só faz login se houver password box (evita confundir com 'Hostname' etc.)
            var passBox = TryFind(driver, By.CssSelector("input[type='password']"));
            if (passBox == null) return;

            // tenta achar o username no mesmo form
            var form = passBox.FindElement(By.XPath("./ancestor::form[1]"));
            var userBox =
                TryFind(form, By.CssSelector("input[type='text']")) ??
                TryFind(form, By.XPath(".//input[not(@type) or @type='text' or @type='username']"));

            if (userBox == null) return;

            userBox.Clear();
            userBox.SendKeys(Configuration.configurationValues.dtcUser);

            passBox.Clear();
            passBox.SendKeys(Configuration.configurationValues.dtcPass);

            var btn =
                TryFind(form, By.CssSelector("input[type='submit']")) ??
                TryFind(form, By.XPath(".//input[@type='button' or @type='submit'] | .//button"));

            btn?.Click();

            // espera carregar página seguinte (menu aparecer / DOM crescer)
            wait.Until(d => d.PageSource.Length > 2000);
        }

        private static void ClickConfigFiles(IWebDriver driver, WebDriverWait wait)
        {
            // No layout do DTC aparece mesmo como "Configuration files" no menu da esquerda
            var link =
                TryFind(driver, By.LinkText("Configuration files")) ??
                TryFind(driver, By.XPath("//a[normalize-space()='Configuration files']")) ??
                TryFind(driver, By.XPath("//a[contains(.,'Configuration files')]"));

            if (link == null)
                throw new Exception("Não encontrei o link 'Configuration files' no menu.");

            link.Click();

            // espera aparecer o input file
            wait.Until(d => d.FindElements(By.CssSelector("input[type='file']")).Count > 0);
        }

        private static IWebElement TryFind(ISearchContext ctx, By by)
        {
            try { return ctx.FindElement(by); } catch { return null; }
        }
        private Task UploadConfig_CircutorDtcAsync(string configPath, CancellationToken ct)
        {
            throw new NotImplementedException("Upload config CIRCUTOR ainda não implementado (ajustar XPaths/flow).");
        }


        private async Task<(bool okHttp, string xml)> RunWsDcRequestAsync(string idRpt, string idDc, CancellationToken ct)
        {
            var url = $"http://{Configuration.configurationValues.ip}:8080/WS_DC/WS_DC.asmx";

            // Mantive isto igual ao que tinhas (ns_emi).
            // Se para o DTC tiver de ser outro, cria ns_dtc e troca aqui.
            string idMeters = Configuration.configurationValues.ns_emi;

            var soap = $@"<?xml version=""1.0"" encoding=""utf-8""?>
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

            using var http = new HttpClient();
            using var content = new StringContent(soap, Encoding.UTF8, "text/xml");
            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            req.Headers.Add("SOAPAction", "\"http://www.asais.fr/ns/Saturne/DC/ws/Request\"");

            using var resp = await http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            await _logger.LogAsync($"WS_DC idRpt={idRpt} idDc={idDc} status={(int)resp.StatusCode}", toFile: true);
            return (resp.IsSuccessStatusCode, raw);
        }

        private string TryGetEmiIdDc()
        {
            // Ideal: adicionas Configuration.configurationValues.emiPlcIdDc
            // Enquanto não existe, tenta procurar por reflection para não partir builds:
            try
            {
                var cv = Configuration.configurationValues;
                var p = cv.GetType().GetProperty("emiPlcIdDc");
                if (p != null)
                {
                    var v = p.GetValue(cv) as string;
                    return v ?? "";
                }
            }
            catch { }
            return "";
        }


        private async Task<string> GetFwDtcAsync(string fabricante, CancellationToken ct)
        {
            try
            {
                if (string.Equals(fabricante, "ZIV", StringComparison.OrdinalIgnoreCase))
                    return await GetFw_ZivDtcAsync(ct);

                // se quiseres implementar outros depois:
                // if (string.Equals(fabricante, "CIRCUTOR", StringComparison.OrdinalIgnoreCase))
                //     return await GetFw_CircutorDtcAsync(ct);

                await _logger.LogAsync($"GetFwDtcAsync: fabricante '{fabricante}' não implementado.", toFile: true);
                return "";
            }
            catch (Exception ex)
            {
                await _logger.LogAsync($"GetFwDtcAsync erro ({fabricante}): {ex.Message}", toFile: true);
                return "";
            }
        }

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

            string baseUrl = $"http://{Configuration.configurationValues.ip}:{Configuration.configurationValues.dtcPort}/";
            driver.Navigate().GoToUrl(baseUrl);

            // login se necessário
            TryLoginIfNeeded(driver, wait);

            // na página “Identification” tens:
            // Firmware version  <valor>
            // Vamos procurar pelo label, para não depender de layout fixo
            string fw = ReadValueByLabel(driver, wait,
                "Firmware version",
                "Firmware Version",
                "Firmware");

            return fw?.Trim() ?? "";
        }, ct);

        private static string GetExpectedFwDtc(string fabricante)
        {
            // mete aqui o mapping quando quiseres.
            // Se não souberes, devolve "" e o runner vai perguntar.
            var map = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Exemplo (da tua imagem):
                ["ZIV"] = "3.23.99.1.baac1e69",

                // ["CIRCUTOR"] = "x.y.z",
            };

            return map.TryGetValue(fabricante ?? "", out var v) ? (v ?? "") : "";
        }

        private async Task UpgradeFwDtcManualAsync(string fabricante, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            bool fazer = AskYesNoLocal(
                "Upgrade FW (DTC)",
                $"Fabricante: {fabricante}\n\nQueres fazer upgrade de firmware agora?\n\nYes = vou fazer upgrade (manual)\nNo = skip",
                defaultYes: false
            );

            if (!fazer)
                throw new StepSkippedException("SKIP (decisão do utilizador).");  // ✅ em vez de return

            var res = MessageBox.Show(
                this,
                "Faz o upgrade de firmware no DTC (manual).\n\nQuando terminares, clica OK.\nCancel = cancelar o sequencial.",
                "Upgrade FW (manual)",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information
            );

            if (res != DialogResult.OK)
                throw new OperationCanceledException();

            await _logger.LogAsync("A aguardar DTC voltar a responder por HTTP...", toFile: true);

            string baseUrl = $"http://{Configuration.configurationValues.ip}:{Configuration.configurationValues.dtcPort}/";
            await WaitHttpUpAsync(baseUrl, timeoutSeconds: 480, ct);

            await _logger.LogAsync("DTC voltou a responder por HTTP após upgrade.", toFile: true);
        }

        private static async Task WaitHttpUpAsync(string url, int timeoutSeconds, CancellationToken ct)
        {
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

            // pequeno “delay” inicial para evitar martelar logo no reboot
            await Task.Delay(5000, ct);

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

            while (DateTime.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    using var resp = await http.GetAsync(url, ct);
                    if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 500)
                        return; // está “up” (mesmo que peça login)
                }
                catch
                {
                    // ignora e tenta novamente
                }

                await Task.Delay(2000, ct);
            }

            throw new Exception($"Timeout: DTC não voltou por HTTP em {timeoutSeconds}s.");
        }

        private static string ReadValueByLabel(IWebDriver driver, WebDriverWait wait, params string[] labels)
        {
            wait.Until(d => d.PageSource.Length > 2000);

            const string NBSP = "\u00A0";

            foreach (var label in labels)
            {
                if (string.IsNullOrWhiteSpace(label)) continue;

                string wanted = label.Trim();

                // 1) Procura a linha cujo 1º td (label) coincide, tratando NBSP como espaço
                // 2) Devolve a última td não vazia dessa linha (no teu caso é td[3])
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

                // fallback: match por "contém" (para casos tipo "Firmware version" / variações)
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



        #endregion
    }




    // ============================================================================================
    // Report HTML DTC (create/append)
    // ============================================================================================
    public static class DtcHtmlReport
    {
        public static void CreateEmpty(string reportPath)
        {
            var html = new StringBuilder();
            html.AppendLine("<!doctype html>");
            html.AppendLine("<html><head><meta charset=\"utf-8\"/>");
            html.AppendLine("<title>Report DTC</title>");
            html.AppendLine("<style>body{font-family:Arial} table{border-collapse:collapse;width:100%} td,th{border:1px solid #ccc;padding:6px} th{background:#f3f3f3}</style>");
            html.AppendLine("</head><body>");
            html.AppendLine("<h2>Report DTC</h2>");
            html.AppendLine("<table>");
            html.AppendLine("<thead><tr>");
            html.AppendLine("<th>Fabricante</th><th>ID/Serial</th><th>FW inicial</th><th>FW final</th><th>Config</th><th>Analógicas</th><th>EMI PLC</th><th>Conformidade</th><th>Comentário</th><th>Data</th>");
            html.AppendLine("</tr></thead>");
            html.AppendLine("<tbody>");
            html.AppendLine("</tbody></table>");
            html.AppendLine("</body></html>");

            File.WriteAllText(reportPath, html.ToString(), Encoding.UTF8);
        }

        public static void AppendRecord(string reportPath, DtcRecord r)
        {
            if (r == null) return;

            if (!File.Exists(reportPath))
                CreateEmpty(reportPath);

            string comentario = "";
            try { comentario = r.Comentario ?? ""; } catch { }

            var row = new StringBuilder();
            row.AppendLine("<tr>");
            row.AppendLine($"  <td>{Html(r.Fabricante)}</td>");
            row.AppendLine($"  <td>{Html(r.NumeroSerie)}</td>");
            row.AppendLine($"  <td>{Html(r.FirmwareOld)}</td>");
            row.AppendLine($"  <td>{Html(r.FirmwareNew)}</td>");
            row.AppendLine($"  <td>{(r.ConfigUploaded ? "OK" : "FAIL")}</td>");
            row.AppendLine($"  <td>{(r.AnalogOk ? "OK" : "FAIL")}</td>");
            row.AppendLine($"  <td>{(r.EmiPlcOk ? "OK" : "FAIL")}</td>");
            row.AppendLine($"  <td>{(r.ConformidadeFinal ? "CONFORME" : "NÃO CONFORME")}</td>");
            row.AppendLine($"  <td>{Html(comentario)}</td>");
            row.AppendLine($"  <td>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</td>");
            row.AppendLine("</tr>");

            string content = File.ReadAllText(reportPath, Encoding.UTF8);
            int idx = content.LastIndexOf("</tbody>", StringComparison.OrdinalIgnoreCase);

            if (idx < 0)
            {
                File.AppendAllText(reportPath, row.ToString(), Encoding.UTF8);
                return;
            }

            string updated = content.Insert(idx, row.ToString());
            File.WriteAllText(reportPath, updated, Encoding.UTF8);
        }

        private static string Html(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }
}
