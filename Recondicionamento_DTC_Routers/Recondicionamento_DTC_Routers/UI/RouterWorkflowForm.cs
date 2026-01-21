using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using Recondicionamento_DTC_Routers.Domain;
using Recondicionamento_DTC_Routers.helpers;
using Recondicionamento_DTC_Routers.Infra;
using Recondicionamento_DTC_Routers.Services;
using Recondicionamento_DTC_Routers.Workflow;
using Renci.SshNet;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Recondicionamento_DTC_Routers.UI
{
    public sealed class RouterWorkflowForm : Form
    {
        private DataGridView _grid;
        private RichTextBox _logBox;

        private TextBox _txtFab;
        private TextBox _txtNs;
        private TextBox _txtFwOld;
        private TextBox _txtFwNew;
        private TextBox _txtComment;

        private Button _btnStart;
        private Button _btnCancel;
        private Button _btnNew;

        private BindingList<StepVm> _steps;
        private UiLogger _logger;
        private CancellationTokenSource _cts;

        private Button _btnReportAppend;
        private Button _btnReportNew;
        private Label _lblReportPath;

        private string _currentReportPath;

        // Ethernet (porta-a-porta)
        private int? _ethPortsCount;     // nº de portas indicado pelo utilizador
        private bool[] _lastEthOk;       // resultados por porta (tamanho = _ethPortsCount)

        public RouterWorkflowForm()
        {
            InitializeComponent();

            // IMPORTANTÍSSIMO: o Designer instancia o Form.
            // Não corras lógica pesada nem UI dinâmica em design-time.
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            BuildUiRuntime();
            BuildSteps();

            var resultados = Configuration.GetResultadosDir();
            _currentReportPath = System.IO.Path.Combine(resultados, "report.html");

            _logger = new UiLogger(this, _logBox, System.IO.Path.Combine(Configuration.GetResultadosDir(), "log.txt"));
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 650);
            this.Name = "RouterWorkflowForm";
            this.Text = "Router - Sequencial Recondicionamento";
            this.ResumeLayout(false);
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
            Text = "Router - Sequencial Recondicionamento";
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

            var box = new GroupBox { Text = "Dados do Router", Dock = DockStyle.Fill, Height = 110 };

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

            t.Controls.Add(new Label { Text = "Nº Série", AutoSize = true }, 4, 0);
            _txtNs = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
            t.Controls.Add(_txtNs, 5, 0);
            t.SetColumnSpan(_txtNs, 3);

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

            _btnStart = new Button { Text = "Start Sequencial", Width = 170, Height = 40 };
            _btnCancel = new Button { Text = "Cancel", Width = 170, Height = 40, Enabled = false };
            _btnNew = new Button { Text = "Novo / Reset", Width = 170, Height = 40 };

            _btnStart.Click += async (_, __) => await StartAsync();
            _btnCancel.Click += (_, __) => _cts?.Cancel();
            _btnNew.Click += (_, __) => ResetUi();

            // --- Report UI (label + botões) ---
            _lblReportPath = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(170, 0),
                Text = $"Report atual:\n{System.IO.Path.GetFileName(_currentReportPath ?? "report.html")}"
            };

            _btnReportAppend = new Button { Text = "Report: Adicionar", Width = 170, Height = 34 };
            _btnReportNew = new Button { Text = "Report: Novo", Width = 170, Height = 34 };

            _btnReportAppend.Click += (_, __) => UseExistingReport();
            _btnReportNew.Click += (_, __) => CreateNewReport();

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
            // ✅ Ordem lógica pedida:
            // 1) Liga, 2) Ethernet (pergunta nº portas e testa uma-a-uma), só depois o resto (HTTP/SSH/etc.)
            _steps = new BindingList<StepVm>
            {
                new StepVm(1,  "Liga a 230V"),
                new StepVm(2,  "Teste Ethernet (porta a porta)"),
                new StepVm(3,  "Detetar fabricante (HTTP/HTTPS)"),
                new StepVm(4,  "Ler Nº Série"),
                new StepVm(5,  "Inspeção visual"),
                new StepVm(6,  "Ler FW inicial"),
                new StepVm(7,  "Upgrade de firmware"),
                new StepVm(8,  "Ler FW final"),
                new StepVm(9,  "Carregar configuração"),
                new StepVm(10, "Teste RS232"),
                new StepVm(11, "Teste RS485"),
                new StepVm(12, "Validação acessórios"),
                new StepVm(13, "Adicionar ao report"),
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
            _txtNs.Text = "";
            _txtFwOld.Text = "";
            _txtFwNew.Text = "";
            _txtComment.Text = "";

            // reset ethernet por router
            _ethPortsCount = null;
            _lastEthOk = null;
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

                RouterRecord record = await runner.RunAsync(_cts.Token);

                _txtFab.Text = record.Fabricante;
                _txtNs.Text = record.NumeroSerie;
                _txtFwOld.Text = record.FirmwareOld;
                _txtFwNew.Text = record.FirmwareNew;

                await _logger.LogAsync(
                    $"UI atualizada. Conformidade final: {(record.ConformidadeFinal ? "CONFORME" : "NÃO CONFORME")}",
                    toFile: true
                );
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

        private RouterWorkflowRunner BuildRunner()
        {
            var sink = new LogSinkAdapter(_logger);

            var runner = new RouterWorkflowRunner(_steps, sink)
            {
                ShowInfo = (t, m) => ShowInfoLocal(t, m),
                ShowWarn = (t, m) => ShowWarnLocal(t, m),

                // 1) Liga a 230V (pergunta ao utilizador)
                AskLiga230VAsync = () =>
                    Task.FromResult(AskConformeNaoConformeLocal("Liga a 230V", "O router liga quando submetido a 230V?")),

                // 2) Ethernet: pergunta nº portas (uma vez) e depois testa em sequência
                AskEthPortsAsync = () =>
                {
                    if (!_ethPortsCount.HasValue)
                        _ethPortsCount = AskIntLocal("Ethernet", "Quantas portas Ethernet existem no router?", 0, 8, 2);
                    return Task.FromResult(_ethPortsCount.Value);
                },

                TestETHAsync = async () => await TestEthPortsInteractiveAsync(log: true),

                // 3) Só depois disto é que faz sentido probe HTTP/HTTPS
                DetectFabricanteAsync = (ct) =>
                    ExecuteHttpRequestAsync(Configuration.configurationValues.routerPort, ct, log: true),

                AskInspecaoVisualAsync = () =>
                    Task.FromResult(AskConformeNaoConformeLocal("Inspeção Visual", "Resultado da inspeção visual?")),

                GetNumeroSerieAsync = async (fab) =>
                {
                    var ns = await GetIdRouterAsync(fab, _cts.Token);
                    return ns ?? "";
                },

                GetFirmwareAsync = async (fab) =>
                {
                    var fw = await GetFwRouterAsync(fab, _cts.Token);
                    return fw ?? "";
                },

                DoUpgradeFirmwareAsync = async (fab, ct) =>
                {
                    // Pergunta se queres fazer upgrade ou saltar
                    bool fazer = AskYesNoLocal(
                        "Upgrade de firmware",
                        "Queres fazer upgrade de firmware agora?\n\nYes = faz upgrade\nNo = skip",
                        $"Fabricante: {fab}"
                    );

                    if (!fazer)
                    {
                        await _logger.LogAsync("Upgrade de firmware: SKIPPED pelo utilizador.", toFile: true);
                        return;
                    }

                    var up = new UpgradeFirmware(sink);
                    await up.ROUTER(fab, ct);

                    await WaitPingBackAsync(
                        Configuration.configurationValues.ip,
                        initialDelaySeconds: 60,
                        timeoutSeconds: 480,
                        ct: ct
                    );
                },


                DoUploadConfigAsync = async (fab, ct) =>
                {
                    var uploader = new UploadConfiguration(sink);
                    return await uploader.ROUTER(fab, ct);
                },

                TestRS232Async = async () => await TestRS232(log: true),
                TestRS485Async = async () => await TestRS485(log: true),

                ValidateAcessorios = (r) =>
                {
                    // ✅ garante que o record fica com EthOk antes do report/conformidade
                    if (_lastEthOk != null && _lastEthOk.Length > 0)
                        r.EthOk = _lastEthOk;

                    // mete aqui a tua UI real depois
                    r.Antena = true;
                    r.CaboAlimentacao = true;
                    r.CaboRS232 = true;
                    r.CaboRS485 = true;
                },

                AddToReport = (r) =>
                {
                    try
                    {
                        // ✅ reforço: EthOk no report
                        if (_lastEthOk != null && _lastEthOk.Length > 0)
                            r.EthOk = _lastEthOk;

                        var resultados = Configuration.GetResultadosDir();

                        // pergunta sempre (mantive como tinhas)
                        var append = MessageBox.Show(this,
                            "Adicionar ao report existente?\n\nYes = adiciona ao atual/existente\nNo = cria novo report",
                            "Report",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) == DialogResult.Yes;

                        if (!append || string.IsNullOrWhiteSpace(_currentReportPath))
                        {
                            _currentReportPath = GetNewReportPath(resultados, "report", ".html");
                        }
                        else
                        {
                            _currentReportPath ??= System.IO.Path.Combine(resultados, "report.html");
                        }

                        var lista = RouterHtmlHelper.LerHtmlParaLista(_currentReportPath)
                                    ?? new System.Collections.Generic.List<RouterReportEntry>();

                        lista.Add(new RouterReportEntry
                        {
                            Timestamp = DateTime.Now,
                            Record = r
                        });

                        RouterHtmlHelper.EscreverHtmlInterativo(lista, _currentReportPath);

                        UpdateReportLabel();
                        ShowInfoLocal("Report", $"Report atualizado em:\n{_currentReportPath}");
                    }
                    catch (Exception ex)
                    {
                        ShowWarnLocal("Report", $"Erro a escrever report: {ex.Message}");
                    }
                }
            };

            return runner;
        }

        #endregion

        #region Ethernet (porta a porta)

        private async Task<bool> TestEthPortsInteractiveAsync(bool log = true)
        {
            // pergunta nº portas só uma vez
            if (!_ethPortsCount.HasValue)
                _ethPortsCount = AskIntLocal("Ethernet", "Quantas portas Ethernet existem no router?", 0, 8, 2);

            int n = _ethPortsCount.Value;

            if (n <= 0)
            {
                await LogMensagemAsync("Ethernet: 0 portas indicadas.", log);
                _lastEthOk = null;
                return false;
            }

            _lastEthOk = new bool[n];

            bool allOk = true;

            for (int i = 0; i < n; i++)
            {
                int portNum = i + 1;

                var res = MessageBox.Show(this,
                    $"Ligue o cabo à porta ETH{portNum} e carregue OK para testar.\n\nCancel = abortar teste Ethernet.",
                    "Teste Ethernet",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Information);

                if (res != DialogResult.OK)
                {
                    await LogMensagemAsync("Ethernet: teste abortado pelo utilizador.", log);
                    return false;
                }

                bool ok = await PingOnceAsync(Configuration.configurationValues.ip, 1200, _cts.Token);
                _lastEthOk[i] = ok;

                if (!ok) allOk = false;

                await LogMensagemAsync(ok ? $"✅ ETH{portNum}: OK" : $"❌ ETH{portNum}: FAIL", log);
            }

            await LogMensagemAsync(allOk
                ? "Ethernet: OK (todas as portas)"
                : "Ethernet: FAIL (há portas NOK)", log);

            return allOk;
        }

        private static async Task<bool> PingOnceAsync(string ip, int timeoutMs, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, timeoutMs);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Dialog helpers (sem dependências do Dialogs)

        private void UseExistingReport()
        {
            var resultados = Configuration.GetResultadosDir();
            _currentReportPath = System.IO.Path.Combine(resultados, "report.html");
            UpdateReportLabel();
            _ = _logger?.LogAsync($"📄 Report selecionado (existente): {_currentReportPath}", toFile: true);
        }

        private void CreateNewReport()
        {
            var resultados = Configuration.GetResultadosDir();
            _currentReportPath = GetNewReportPath(resultados, "report", ".html");

            RouterHtmlHelper.EscreverHtmlInterativo(
                new System.Collections.Generic.List<RouterReportEntry>(),
                _currentReportPath
            );

            UpdateReportLabel();
            _ = _logger?.LogAsync($"🆕 Novo report criado: {_currentReportPath}", toFile: true);
        }

        private void UpdateReportLabel()
        {
            if (_lblReportPath != null)
                _lblReportPath.Text = $"Report atual:\n{System.IO.Path.GetFileName(_currentReportPath ?? "report.html")}";
        }

        private static string GetNewReportPath(string dir, string baseName, string ext)
        {
            string p0 = System.IO.Path.Combine(dir, baseName + ext);
            if (!System.IO.File.Exists(p0)) return p0;

            for (int i = 1; i < 10_000; i++)
            {
                string pi = System.IO.Path.Combine(dir, $"{baseName} ({i}){ext}");
                if (!System.IO.File.Exists(pi)) return pi;
            }

            throw new Exception("Não foi possível encontrar nome livre para o report.");
        }

        private bool AskYesNoLocal(string title, string msg, string details)
        {
            string full = msg;
            if (!string.IsNullOrWhiteSpace(details))
                full += "\n\n" + details;

            var res = MessageBox.Show(this, full, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            return res == DialogResult.Yes;
        }

        private bool AskConformeNaoConformeLocal(string title, string msg)
        {
            var res = MessageBox.Show(this, msg, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            // Yes = Conforme | No = Não Conforme
            return res == DialogResult.Yes;
        }

        private int AskIntLocal(string title, string msg, int min, int max, int initial)
        {
            using var f = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                Width = 420,
                Height = 180,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var lbl = new Label { Left = 12, Top = 12, Width = 380, Text = msg };
            var num = new NumericUpDown
            {
                Left = 12,
                Top = 45,
                Width = 150,
                Minimum = min,
                Maximum = max,
                Value = Math.Clamp(initial, min, max)
            };

            var ok = new Button { Text = "OK", Left = 220, Width = 80, Top = 90, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "Cancel", Left = 310, Width = 80, Top = 90, DialogResult = DialogResult.Cancel };

            f.Controls.Add(lbl);
            f.Controls.Add(num);
            f.Controls.Add(ok);
            f.Controls.Add(cancel);

            f.AcceptButton = ok;
            f.CancelButton = cancel;

            return f.ShowDialog(this) == DialogResult.OK ? (int)num.Value : initial;
        }

        private void ShowInfoLocal(string title, string msg) =>
            MessageBox.Show(this, msg, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

        private void ShowWarnLocal(string title, string msg) =>
            MessageBox.Show(this, msg, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);

        #endregion

        #region Testes (RS232 / RS485)

        public async Task<int> TestRS232(bool log = true)
        {
            int n = 0;

            string ip = Configuration.configurationValues.ip;
            int port = int.Parse(Configuration.configurationValues.portRS232);

            byte[] request1 = new byte[]
            {
                0x7E, 0xA0, 0x21, 0x00, 0x02, 0x00, 0x23, 0x03, 0x93, 0x9A,
                0x74, 0x81, 0x80, 0x12, 0x05, 0x01, 0x80, 0x06, 0x01, 0x7D,
                0x07, 0x04, 0x00, 0x00, 0x00, 0x01, 0x08, 0x04, 0x00, 0x00,
                0x00, 0x07, 0x97, 0x2A, 0x7E
            };

            byte[] expectedResponse1 = new byte[]
            {
                0x7E, 0xA0, 0x23, 0x03, 0x00, 0x02, 0x00, 0x23, 0x73, 0xC0,
                0x48, 0x81, 0x80, 0x14, 0x05, 0x02, 0x00, 0x7D, 0x06, 0x02,
                0x00, 0x80, 0x07, 0x04, 0x00, 0x00, 0x00, 0x07, 0x08, 0x04,
                0x00, 0x00, 0x00, 0x01, 0xE7, 0x9F, 0x7E
            };

            byte[] request2 = new byte[]
            {
                0x7E, 0xA0, 0x47, 0x00, 0x02, 0x00, 0x23, 0x03, 0x10, 0x41,
                0x3E, 0xE6, 0xE6, 0x00, 0x60, 0x36, 0xA1, 0x09, 0x06, 0x07,
                0x60, 0x85, 0x74, 0x05, 0x08, 0x01, 0x01, 0x8A, 0x02, 0x07,
                0x80, 0x8B, 0x07, 0x60, 0x85, 0x74, 0x05, 0x08, 0x02, 0x01,
                0xAC, 0x0A, 0x80, 0x08, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46,
                0x47, 0x48, 0xBE, 0x10, 0x04, 0x0E, 0x01, 0x00, 0x00, 0x00,
                0x06, 0x5F, 0x1F, 0x04, 0x00, 0x00, 0x1E, 0x1D, 0xFF, 0xFF,
                0x19, 0x02, 0x7E
            };

            byte[] expectedResponse2 = new byte[]
            {
                0x7E, 0xA0, 0x53, 0x03, 0x00, 0x02, 0x00, 0x23, 0x30, 0x13,
                0x29, 0xE6, 0xE7, 0x00, 0x61, 0x42, 0xA1, 0x09, 0x06, 0x07,
                0x60, 0x85, 0x74, 0x05, 0x08, 0x01, 0x01, 0xA2, 0x03, 0x02,
                0x01, 0x00, 0xA3, 0x05, 0xA1, 0x03, 0x02, 0x01, 0x00, 0x88,
                0x02, 0x07, 0x80, 0x89, 0x07, 0x60, 0x85, 0x74, 0x05, 0x08,
                0x02, 0x01, 0xAA, 0x0A, 0x80, 0x08, 0x41, 0x42, 0x43, 0x44,
                0x45, 0x46, 0x47, 0x48, 0xBE, 0x10, 0x04, 0x0E, 0x08, 0x00,
                0x06, 0x5F, 0x1F, 0x04, 0x00, 0x00, 0x10, 0x1D, 0x21, 0x34,
                0x00, 0x07, 0x9A, 0xC7, 0x7E
            };

            try
            {
                using (TcpClient client = new TcpClient())
                {
                    client.Connect(ip, port);
                    using (NetworkStream stream = client.GetStream())
                    {
                        stream.ReadTimeout = 4000;

                        await LogMensagemAsync("Send SNRM request", log);
                        stream.Write(request1, 0, request1.Length);
                        Thread.Sleep(500);

                        byte[] response1 = new byte[1024];
                        int bytesRead1 = stream.Read(response1, 0, response1.Length);
                        await LogMensagemAsync("Received SNRM response", log);

                        bool match1 = CompareArrays(response1, bytesRead1, expectedResponse1);
                        await LogMensagemAsync(match1 ? "✅ Resposta corresponde à esperada." : "❌ Resposta diferente da esperada.", log);
                        if (match1) n++;

                        await LogMensagemAsync("Send AARQ request", log);
                        stream.Write(request2, 0, request2.Length);
                        Thread.Sleep(500);

                        byte[] response2 = new byte[1024];
                        int bytesRead2 = stream.Read(response2, 0, response2.Length);
                        await LogMensagemAsync("Received AARE response", log);

                        bool match2 = CompareArrays(response2, bytesRead2, expectedResponse2);
                        await LogMensagemAsync(match2 ? "✅ Resposta corresponde à esperada." : "❌ Resposta diferente da esperada.", log);
                        if (match2) n++;
                    }
                }
            }
            catch (Exception ex)
            {
                await LogMensagemAsync($"Erro: {ex.Message}", log);
            }

            return n;
        }

        public async Task<bool> TestRS485(bool log = true)
        {
            bool result = false;

            string ip = Configuration.configurationValues.ip;
            int port = int.Parse(Configuration.configurationValues.portRS485);

            byte[] modbusRequest = new byte[] { 0x01, 0x04, 0x00, 0x01, 0x00, 0x01, 0x60, 0x0A };

            try
            {
                using (TcpClient client = new TcpClient(ip, port))
                using (NetworkStream stream = client.GetStream())
                {
                    stream.ReadTimeout = 4000;
                    stream.Write(modbusRequest, 0, modbusRequest.Length);

                    byte[] response = new byte[12];
                    Thread.Sleep(500);
                    int bytesRead = stream.Read(response, 0, response.Length);

                    int year = response[4] + (response[3] << 8);
                    int month = response[5];
                    int day = response[6];
                    int hour = response[8];
                    int minute = response[9];
                    int second = response[10];

                    DateTime dateTime = new DateTime(year, month, day, hour, minute, second);

                    var sb = new StringBuilder();
                    for (int i = 0; i < bytesRead; i++)
                        sb.AppendFormat("{0:X2} ", response[i]);

                    await LogMensagemAsync($"{sb} ----> {dateTime:yyyy/MM/dd HH:mm:ss}", log);
                    result = true;
                }
            }
            catch (Exception ex)
            {
                await LogMensagemAsync($"Erro: {ex.Message}", log);
                result = false;
            }

            return result;
        }

        #endregion

        #region Logger + compare + probe HTTP

        private Task LogMensagemAsync(string msg, bool log = true, bool toFile = false)
        {
            if (!log) return Task.CompletedTask;
            return _logger.LogAsync(msg, toFile);
        }

        private static bool CompareArrays(byte[] actual, int actualLen, byte[] expected)
        {
            if (actualLen != expected.Length) return false;
            for (int i = 0; i < expected.Length; i++)
                if (actual[i] != expected[i]) return false;
            return true;
        }

        private async Task<string> ExecuteHttpRequestAsync(string portStr, CancellationToken ct, bool log)
        {
            if (!int.TryParse(portStr, out int port)) port = 80;

            string ip = Configuration.configurationValues.ip;
            string manufacturer = await ProbeManufacturerAsync(ip, port, ct);

            if (log)
                await LogMensagemAsync($"HTTP probe [{ip}:{port}] -> {manufacturer}", true);

            return manufacturer;
        }

        private static async Task<string> ProbeManufacturerAsync(string ip, int port, CancellationToken ct)
        {
            static string Detect(string text)
            {
                if (string.IsNullOrWhiteSpace(text)) return "UNKNOWN";
                text = text.ToUpperInvariant();

                if (text.Contains("ZIV")) return "ZIV";
                if (text.Contains("TELDAT")) return "TELDAT";
                if (text.Contains("ANDRA") || text.Contains("ANDRASE") || text.Contains("ANDRA S")) return "ANDRA";
                if (text.Contains("VIRTUAL ACCESS") || text.Contains("VIA")) return "VA";
                if (text.Contains("WESTERMO")) return "VA";
                if (text.Contains("CIRCUTOR")) return "CIRCUTOR";

                return "UNKNOWN";
            }

            foreach (var scheme in new[] { "https", "http" })
            {
                try
                {
                    var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };

                    using (var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(3) })
                    {
                        string url = $"{scheme}://{ip}:{port}/";
                        using var resp = await http.GetAsync(url, ct);
                        string body = await resp.Content.ReadAsStringAsync(ct);

                        var headers = resp.Headers.ToString() + resp.Content.Headers.ToString();
                        string combined = headers + "\n" + body;

                        var d = Detect(combined);
                        if (d != "UNKNOWN") return d;
                    }
                }
                catch
                {
                    // ignora e tenta o próximo
                }
            }

            return "UNKNOWN";
        }

        #endregion

        #region Get NS / FW por fabricante (Selenium + SSH)

        private async Task<string> GetIdRouterAsync(string fabricante, CancellationToken ct)
        {
            string ns = "";

            try
            {
                if (fabricante.Equals("ZIV", StringComparison.OrdinalIgnoreCase))
                {
                    ns = await GetId_ZivAsync(ct);
                }
                else if (fabricante.Equals("VA", StringComparison.OrdinalIgnoreCase))
                    ns = await GetId_VaAsync(ct);
                else if (fabricante.Equals("ANDRA", StringComparison.OrdinalIgnoreCase))
                    ns = await GetId_AndraAsync(ct);
                else if (fabricante.Equals("TELDAT", StringComparison.OrdinalIgnoreCase))
                    ns = await GetId_TeldatAsync(ct);
            }
            catch (Exception ex)
            {
                await LogMensagemAsync($"Erro ao ler NS ({fabricante}): {ex.Message}", true, true);
            }

            await LogMensagemAsync($"NS ({fabricante}): {ns}", true);
            return ns;
        }

        private async Task<string> GetFwRouterAsync(string fabricante, CancellationToken ct)
        {
            string fw = "";

            try
            {
                if (fabricante.Equals("ZIV", StringComparison.OrdinalIgnoreCase))
                    fw = await GetFw_ZivAsync(ct);
                else if (fabricante.Equals("VA", StringComparison.OrdinalIgnoreCase))
                    fw = await GetFw_VaAsync(ct);
                else if (fabricante.Equals("ANDRA", StringComparison.OrdinalIgnoreCase))
                    fw = await GetFw_AndraAsync(ct);
                else if (fabricante.Equals("TELDAT", StringComparison.OrdinalIgnoreCase))
                    fw = await GetFw_TeldatAsync(ct);
            }
            catch (Exception ex)
            {
                await LogMensagemAsync($"Erro ao ler FW ({fabricante}): {ex.Message}", true, true);
            }

            await LogMensagemAsync($"FW ({fabricante}): {fw}", true);
            return fw;
        }

        private Task<string> GetId_ZivAsync(CancellationToken ct) => Task.Run(() =>
        {
            string ns;

            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            var options = new ChromeOptions();
            options.AddArgument("--ignore-certificate-errors");

            using (IWebDriver driver = new ChromeDriver(service, options))
            {
                string url = "http://" + Configuration.configurationValues.ip + ":" + Configuration.configurationValues.routerPort;
                driver.Navigate().GoToUrl(url);
                Thread.Sleep(800);

                driver.FindElement(By.XPath("/html/body/div[2]/div/form/table/tbody/tr[1]/td[3]/input"))
                      .SendKeys(Configuration.configurationValues.routerUser);
                Thread.Sleep(300);

                driver.FindElement(By.XPath("/html/body/div[2]/div/form/table/tbody/tr[2]/td[3]/input"))
                      .SendKeys(Configuration.configurationValues.routerPass);
                Thread.Sleep(300);

                driver.FindElement(By.XPath("/html/body/div[2]/div/form/div/input")).Click();
                Thread.Sleep(500);

                var elemento = driver.FindElement(By.XPath("/html/body/table[2]/tbody/tr/td[2]/table/tbody/tr/td[2]/form/table[2]/tbody/tr[10]/td[3]"));
                ns = elemento.Text;
            }

            return ns;
        }, ct);

        private Task<string> GetFw_ZivAsync(CancellationToken ct) => Task.Run(() =>
        {
            string fw;

            var options = new ChromeOptions();
            options.AddArgument("--ignore-certificate-errors");

            using (IWebDriver driver = new ChromeDriver(options))
            {
                string url = "http://" + Configuration.configurationValues.ip + ":" + Configuration.configurationValues.routerPort;
                driver.Navigate().GoToUrl(url);
                Thread.Sleep(800);

                driver.FindElement(By.XPath("/html/body/div[2]/div/form/table/tbody/tr[1]/td[3]/input"))
                      .SendKeys(Configuration.configurationValues.routerUser);
                Thread.Sleep(300);

                driver.FindElement(By.XPath("/html/body/div[2]/div/form/table/tbody/tr[2]/td[3]/input"))
                      .SendKeys(Configuration.configurationValues.routerPass);
                Thread.Sleep(300);

                driver.FindElement(By.XPath("/html/body/div[2]/div/form/div/input")).Click();
                Thread.Sleep(500);

                var elemento = driver.FindElement(By.XPath("/html/body/table[2]/tbody/tr/td[2]/table/tbody/tr/td[2]/form/table[2]/tbody/tr[7]/td[3]"));
                fw = elemento.Text;
            }

            return fw;
        }, ct);

        private Task<string> GetId_VaAsync(CancellationToken ct) => Task.Run(() =>
        {
            string ns = "";

            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            var options = new ChromeOptions();
            options.AddArgument("--ignore-certificate-errors");

            using (IWebDriver driver = new ChromeDriver(service, options))
            {
                bool hmi_novo = false;

                string url = "http://" + Configuration.configurationValues.ip + ":" + Configuration.configurationValues.routerPort;
                driver.Navigate().GoToUrl(url);
                Thread.Sleep(800);

                var elementos = driver.FindElements(By.XPath("/html/body/div/div[1]/form/div[1]/fieldset/fieldset/div[1]/div/input"));
                if (!elementos.Any()) hmi_novo = true;

                if (!hmi_novo)
                {
                    driver.FindElement(By.XPath("/html/body/div/div[1]/form/div[1]/fieldset/fieldset/div[1]/div/input"))
                          .SendKeys(Configuration.configurationValues.routerUser);
                    Thread.Sleep(300);

                    driver.FindElement(By.XPath("/html/body/div/div[1]/form/div[1]/fieldset/fieldset/div[2]/div/input"))
                          .SendKeys(Configuration.configurationValues.routerPass);
                    Thread.Sleep(300);

                    driver.FindElement(By.XPath("/html/body/div/div[1]/form/div[2]/input[1]")).Click();
                    Thread.Sleep(300);

                    try { driver.SwitchTo().Alert().Accept(); driver.SwitchTo().DefaultContent(); } catch { }

                    var elemento = driver.FindElement(By.XPath("/html/body/header/div/div/div/small"));
                    ns = elemento.Text.Split('\n')[0];
                }
                else
                {
                    driver.FindElement(By.XPath("/html/body/div/div/div[1]/div/form/div[1]/div[1]/input"))
                          .SendKeys(Configuration.configurationValues.routerUser);
                    Thread.Sleep(300);

                    driver.FindElement(By.XPath("/html/body/div/div/div[1]/div/form/div[1]/div[2]/input"))
                          .SendKeys(Configuration.configurationValues.routerPass);
                    Thread.Sleep(300);

                    driver.FindElement(By.XPath("/html/body/div/div/div[1]/div/form/div[2]/input")).Click();
                    Thread.Sleep(300);

                    try { driver.SwitchTo().Alert().Accept(); driver.SwitchTo().DefaultContent(); } catch { }

                    var elemento = driver.FindElement(By.XPath("/html/body/div/div[2]/header/div/div/div/small"));
                    ns = elemento.Text.Split('/')[1];
                    ns = ns.Split('\n')[1].Trim();
                }
            }

            return ns;
        }, ct);

        private Task<string> GetFw_VaAsync(CancellationToken ct) => Task.Run(() =>
        {
            string fw = "";

            var options = new ChromeOptions();
            options.AddArgument("--ignore-certificate-errors");

            using (IWebDriver driver = new ChromeDriver(options))
            {
                bool hmi_novo = false;

                string url = "http://" + Configuration.configurationValues.ip + ":" + Configuration.configurationValues.routerPort;
                driver.Navigate().GoToUrl(url);
                Thread.Sleep(800);

                var elementos = driver.FindElements(By.XPath("/html/body/div/div[1]/form/div[1]/fieldset/fieldset/div[1]/div/input"));
                if (!elementos.Any()) hmi_novo = true;

                if (!hmi_novo)
                {
                    driver.FindElement(By.XPath("/html/body/div/div[1]/form/div[1]/fieldset/fieldset/div[1]/div/input"))
                          .SendKeys(Configuration.configurationValues.routerUser);
                    Thread.Sleep(300);

                    driver.FindElement(By.XPath("/html/body/div/div[1]/form/div[1]/fieldset/fieldset/div[2]/div/input"))
                          .SendKeys(Configuration.configurationValues.routerPass);
                    Thread.Sleep(300);

                    driver.FindElement(By.XPath("/html/body/div/div[1]/form/div[2]/input[1]")).Click();
                    Thread.Sleep(300);

                    try { driver.SwitchTo().Alert().Accept(); driver.SwitchTo().DefaultContent(); } catch { }

                    var elemento = driver.FindElement(By.XPath("/html/body/header/div/div/div/small"));
                    fw = elemento.Text.Split('\n')[1];
                }
                else
                {
                    driver.FindElement(By.XPath("/html/body/div/div/div[1]/div/form/div[1]/div[1]/input"))
                          .SendKeys(Configuration.configurationValues.routerUser);
                    Thread.Sleep(300);

                    driver.FindElement(By.XPath("/html/body/div/div/div[1]/div/form/div[1]/div[2]/input"))
                          .SendKeys(Configuration.configurationValues.routerPass);
                    Thread.Sleep(300);

                    driver.FindElement(By.XPath("/html/body/div/div/div[1]/div/form/div[2]/input")).Click();
                    Thread.Sleep(300);

                    try { driver.SwitchTo().Alert().Accept(); driver.SwitchTo().DefaultContent(); } catch { }

                    var elemento = driver.FindElement(By.XPath("/html/body/div/div[2]/header/div/div/div/small"));
                    fw = elemento.Text.Split('/')[2];
                }
            }

            return fw;
        }, ct);

        private Task<string> GetId_AndraAsync(CancellationToken ct) => Task.Run(() =>
        {
            string ns = "";

            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            var options = new ChromeOptions();
            options.AddArgument("--ignore-certificate-errors");

            using (IWebDriver driver = new ChromeDriver(service, options))
            {
                string url = "https://" + Configuration.configurationValues.ip + ":" + Configuration.configurationValues.routerPort;
                driver.Navigate().GoToUrl(url);
                Thread.Sleep(1200);

                driver.FindElement(By.XPath("/html/body/div[2]/form/input[1]")).SendKeys(Configuration.configurationValues.routerUser);
                Thread.Sleep(300);
                driver.FindElement(By.XPath("/html/body/div[2]/form/input[2]")).SendKeys(Configuration.configurationValues.routerPass);
                Thread.Sleep(300);
                driver.FindElement(By.XPath("/html/body/div[2]/form/button")).Click();
                Thread.Sleep(1200);

                var iframe = driver.FindElement(By.XPath("/html/body/table/tbody/tr[2]/td[2]/iframe"));
                driver.SwitchTo().Frame(iframe);

                var elemento = driver.FindElement(By.XPath("/html/body/div/div[2]/form/table/tbody/tr[1]/td[2]/input"));
                ns = elemento.GetAttribute("value");
            }

            return ns;
        }, ct);

        private Task<string> GetFw_AndraAsync(CancellationToken ct) => Task.Run(() =>
        {
            string fw = "";

            var options = new ChromeOptions();
            options.AddArgument("--ignore-certificate-errors");

            using (IWebDriver driver = new ChromeDriver(options))
            {
                string url = "https://" + Configuration.configurationValues.ip + ":" + Configuration.configurationValues.routerPort;
                driver.Navigate().GoToUrl(url);
                Thread.Sleep(1200);

                driver.FindElement(By.XPath("/html/body/div[2]/form/input[1]")).SendKeys(Configuration.configurationValues.routerUser);
                Thread.Sleep(300);
                driver.FindElement(By.XPath("/html/body/div[2]/form/input[2]")).SendKeys(Configuration.configurationValues.routerPass);
                Thread.Sleep(300);
                driver.FindElement(By.XPath("/html/body/div[2]/form/button")).Click();
                Thread.Sleep(1200);

                var elemento = driver.FindElement(By.XPath("/html/body/table/tbody/tr[3]/td/div"));
                fw = elemento.Text;
            }

            return fw;
        }, ct);

        private async Task<string> GetId_TeldatAsync(CancellationToken ct)
        {
            var host = Configuration.configurationValues.ip;
            var port = 2211;
            var username = Configuration.configurationValues.routerUser;
            var password = Configuration.configurationValues.routerPass;

            return await Task.Run(() =>
            {
                var connectionInfo = new ConnectionInfo(host, port, username,
                    new PasswordAuthenticationMethod(username, password));

                using (var client = new SshClient(connectionInfo))
                {
                    client.Connect();
                    if (!client.IsConnected) return "";

                    var shellStream = client.CreateShellStream("xterm", 80, 24, 800, 600, 1024);

                    var sb = new StringBuilder();
                    var buffer = new byte[4096];

                    var deadline = DateTime.UtcNow.AddSeconds(2);
                    while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
                    {
                        if (shellStream.DataAvailable)
                        {
                            int read = shellStream.Read(buffer, 0, buffer.Length);
                            if (read > 0) sb.Append(Encoding.UTF8.GetString(buffer, 0, read));
                        }
                        else
                        {
                            Thread.Sleep(50);
                        }
                    }

                    string textoPosLogin = sb.ToString();
                    var match = Regex.Match(textoPosLogin, @"S/N:\s*([^\r\n]+)", RegexOptions.IgnoreCase);

                    client.Disconnect();
                    return match.Success ? match.Groups[1].Value.Trim() : "";
                }
            }, ct);
        }

        private async Task<string> GetFw_TeldatAsync(CancellationToken ct)
        {
            var host = Configuration.configurationValues.ip;
            var port = 2211;
            var username = Configuration.configurationValues.routerUser;
            var password = Configuration.configurationValues.routerPass;

            return await Task.Run(() =>
            {
                var connectionInfo = new ConnectionInfo(host, port, username,
                    new PasswordAuthenticationMethod(username, password));

                using (var client = new SshClient(connectionInfo))
                {
                    client.Connect();
                    if (!client.IsConnected) return "";

                    var shellStream = client.CreateShellStream("xterm", 80, 24, 800, 600, 1024);

                    var sb = new StringBuilder();
                    var buffer = new byte[4096];

                    var deadline = DateTime.UtcNow.AddSeconds(2);
                    while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
                    {
                        if (shellStream.DataAvailable)
                        {
                            int read = shellStream.Read(buffer, 0, buffer.Length);
                            if (read > 0) sb.Append(Encoding.UTF8.GetString(buffer, 0, read));
                        }
                        else
                        {
                            Thread.Sleep(50);
                        }
                    }

                    string textoPosLogin = sb.ToString();
                    var matchLinha = Regex.Match(textoPosLogin, @"CIT software version:\s*(.+)", RegexOptions.IgnoreCase);

                    client.Disconnect();
                    return matchLinha.Success ? matchLinha.Groups[1].Value.Trim() : "";
                }
            }, ct);
        }

        private async Task WaitPingBackAsync(string ip, int initialDelaySeconds, int timeoutSeconds, CancellationToken ct)
        {
            if (initialDelaySeconds > 0)
                await Task.Delay(TimeSpan.FromSeconds(initialDelaySeconds), ct);

            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            int tent = 0;

            while (DateTime.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();
                tent++;

                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(ip, 1200);
                    if (reply.Status == IPStatus.Success)
                    {
                        await _logger.LogAsync($"✅ Ping OK ({ip}) após upgrade. Tentativas: {tent}", toFile: true);
                        return;
                    }
                }
                catch
                {
                    // ignora e continua
                }

                if (tent == 1 || tent % 10 == 0)
                    await _logger.LogAsync($"... à espera de ping ({ip}) (tentativa {tent})", toFile: true);

                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }

            await _logger.LogAsync($"⚠️ Timeout: ping não voltou em {timeoutSeconds}s ({ip}).", toFile: true);
        }

        #endregion
    }
}
