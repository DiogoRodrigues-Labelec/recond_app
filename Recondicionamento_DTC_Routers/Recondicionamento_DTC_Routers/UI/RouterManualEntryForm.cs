using Recondicionamento_DTC_Routers.Domain;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Recondicionamento_DTC_Routers.UI
{
    public sealed partial class RouterManualEntryForm : Form
    {
        public RouterRecord Result { get; private set; }

        private TextBox _txtFab, _txtNs, _txtFwOld, _txtFwNew, _txtComment;
        private CheckBox _chkLiga230, _chkInspecao, _chkConfig, _chkRs485;
        private NumericUpDown _numRs232;
        private NumericUpDown _numEthPorts;
        private CheckBox[] _eth;
        private CheckBox _chkAntena, _chkCaboAlim, _chkCabo232, _chkCabo485;
        private CheckBox _chkConforme;

        private Button _btnOk, _btnCancel;

        public RouterManualEntryForm()
        {
            Text = "Adicionar Router manual ao report";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(820, 520);

            BuildUi();
            Wire();
            UpdateEthEnabled();
            RecalcConformidade();
        }

        public static RouterRecord Capture(IWin32Window owner)
        {
            using var f = new RouterManualEntryForm();
            return f.ShowDialog(owner) == DialogResult.OK ? f.Result : null;
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 3
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            // --- Top: dados base ---
            var gbDados = new GroupBox { Text = "Dados", Dock = DockStyle.Fill, Height = 120 };
            var tDados = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, Padding = new Padding(10) };
            tDados.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tDados.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tDados.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tDados.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            _txtFab = new TextBox { Dock = DockStyle.Fill };
            _txtNs = new TextBox { Dock = DockStyle.Fill };
            _txtFwOld = new TextBox { Dock = DockStyle.Fill };
            _txtFwNew = new TextBox { Dock = DockStyle.Fill };

            tDados.Controls.Add(new Label { Text = "Fabricante", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 0);
            tDados.Controls.Add(_txtFab, 1, 0);
            tDados.Controls.Add(new Label { Text = "Nº Série", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 2, 0);
            tDados.Controls.Add(_txtNs, 3, 0);

            tDados.Controls.Add(new Label { Text = "FW Old", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 1);
            tDados.Controls.Add(_txtFwOld, 1, 1);
            tDados.Controls.Add(new Label { Text = "FW New", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 2, 1);
            tDados.Controls.Add(_txtFwNew, 3, 1);

            gbDados.Controls.Add(tDados);

            // --- Right top: estados ---
            var gbChecks = new GroupBox { Text = "Resultados", Dock = DockStyle.Fill, Height = 120 };
            var flowChecks = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(10) };

            _chkLiga230 = new CheckBox { Text = "Liga a 230V", AutoSize = true };
            _chkInspecao = new CheckBox { Text = "Inspeção visual OK", AutoSize = true };
            _chkConfig = new CheckBox { Text = "Config carregada", AutoSize = true };
            _chkRs485 = new CheckBox { Text = "RS485 OK", AutoSize = true };

            flowChecks.Controls.Add(_chkLiga230);
            flowChecks.Controls.Add(_chkInspecao);
            flowChecks.Controls.Add(_chkConfig);
            flowChecks.Controls.Add(_chkRs485);

            gbChecks.Controls.Add(flowChecks);

            root.Controls.Add(gbDados, 0, 0);
            root.Controls.Add(gbChecks, 1, 0);

            // --- Middle left: ethernet ---
            var gbEth = new GroupBox { Text = "Ethernet", Dock = DockStyle.Fill };
            var ethPanel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), ColumnCount = 4, RowCount = 3 };
            ethPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            ethPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            ethPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            ethPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            ethPanel.Controls.Add(new Label { Text = "Nº portas", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 0);
            _numEthPorts = new NumericUpDown { Minimum = 0, Maximum = 8, Value = 2, Width = 60 };
            ethPanel.Controls.Add(_numEthPorts, 1, 0);

            _eth = new CheckBox[8];
            var ethFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoSize = true };
            for (int i = 0; i < 8; i++)
            {
                _eth[i] = new CheckBox { Text = $"ETH{i + 1}", AutoSize = true };
                ethFlow.Controls.Add(_eth[i]);
            }
            ethPanel.Controls.Add(ethFlow, 0, 1);
            ethPanel.SetColumnSpan(ethFlow, 4);

            gbEth.Controls.Add(ethPanel);

            // --- Middle right: rs232 + acessórios + conformidade ---
            var gbMisc = new GroupBox { Text = "RS232 / Acessórios", Dock = DockStyle.Fill };
            var misc = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), ColumnCount = 2, RowCount = 5 };
            misc.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            misc.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            misc.Controls.Add(new Label { Text = "RS232 score (0..2)", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 0);
            _numRs232 = new NumericUpDown { Minimum = 0, Maximum = 2, Value = 2, Width = 60 };
            misc.Controls.Add(_numRs232, 1, 0);

            var acc = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoSize = true };
            _chkAntena = new CheckBox { Text = "Antena", AutoSize = true, Checked = true };
            _chkCaboAlim = new CheckBox { Text = "Cabo Alimentação", AutoSize = true, Checked = true };
            _chkCabo232 = new CheckBox { Text = "Cabo RS232", AutoSize = true, Checked = true };
            _chkCabo485 = new CheckBox { Text = "Cabo RS485", AutoSize = true, Checked = true };
            acc.Controls.AddRange(new Control[] { _chkAntena, _chkCaboAlim, _chkCabo232, _chkCabo485 });

            misc.Controls.Add(new Label { Text = "Acessórios", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 1);
            misc.Controls.Add(acc, 1, 1);

            _chkConforme = new CheckBox { Text = "Conformidade final (auto)", AutoSize = true, Enabled = false };
            misc.Controls.Add(_chkConforme, 0, 2);
            misc.SetColumnSpan(_chkConforme, 2);

            gbMisc.Controls.Add(misc);

            root.Controls.Add(gbEth, 0, 1);
            root.Controls.Add(gbMisc, 1, 1);

            // --- Bottom: comentário + botões ---
            var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var gbComment = new GroupBox { Text = "Comentário", Dock = DockStyle.Fill, Height = 110 };
            _txtComment = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };
            gbComment.Controls.Add(_txtComment);

            var flowBtns = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true };
            _btnOk = new Button { Text = "Adicionar ao report", Width = 180, Height = 38 };
            _btnCancel = new Button { Text = "Cancelar", Width = 180, Height = 38 };
            flowBtns.Controls.Add(_btnOk);
            flowBtns.Controls.Add(_btnCancel);

            bottom.Controls.Add(gbComment, 0, 0);
            bottom.Controls.Add(flowBtns, 1, 0);

            root.Controls.Add(bottom, 0, 2);
            root.SetColumnSpan(bottom, 2);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        private void Wire()
        {
            _btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };
            _btnOk.Click += (_, __) =>
            {
                var r = BuildRecordFromUi();
                Result = r;
                DialogResult = DialogResult.OK;
                Close();
            };

            _numEthPorts.ValueChanged += (_, __) => { UpdateEthEnabled(); RecalcConformidade(); };

            Action hook = RecalcConformidade;
            _chkLiga230.CheckedChanged += (_, __) => hook();
            _chkInspecao.CheckedChanged += (_, __) => hook();
            _chkConfig.CheckedChanged += (_, __) => hook();
            _chkRs485.CheckedChanged += (_, __) => hook();
            _numRs232.ValueChanged += (_, __) => hook();
            _chkAntena.CheckedChanged += (_, __) => hook();
            _chkCaboAlim.CheckedChanged += (_, __) => hook();
            _chkCabo232.CheckedChanged += (_, __) => hook();
            _chkCabo485.CheckedChanged += (_, __) => hook();

            foreach (var cb in _eth) cb.CheckedChanged += (_, __) => hook();
        }

        private void UpdateEthEnabled()
        {
            int n = (int)_numEthPorts.Value;
            for (int i = 0; i < 8; i++)
            {
                _eth[i].Enabled = i < n;
                if (i >= n) _eth[i].Checked = false;
            }
        }

        private void RecalcConformidade()
        {
            int n = (int)_numEthPorts.Value;

            bool ethOk = true;
            for (int i = 0; i < n; i++)
                ethOk &= _eth[i].Checked;

            bool accOk = _chkAntena.Checked && _chkCaboAlim.Checked && _chkCabo232.Checked && _chkCabo485.Checked;

            bool rs232Ok = ((int)_numRs232.Value) >= 2;

            bool conforme =
                _chkLiga230.Checked &&
                _chkInspecao.Checked &&
                _chkConfig.Checked &&
                ethOk &&
                rs232Ok &&
                _chkRs485.Checked &&
                accOk;

            _chkConforme.Checked = conforme;
        }

        private RouterRecord BuildRecordFromUi()
        {
            int n = (int)_numEthPorts.Value;
            var eth = new bool[8];
            for (int i = 0; i < 8; i++)
                eth[i] = _eth[i].Checked;

            var r = new RouterRecord
            {
                Fabricante = (_txtFab.Text ?? "").Trim(),
                NumeroSerie = (_txtNs.Text ?? "").Trim(),
                FirmwareOld = (_txtFwOld.Text ?? "").Trim(),
                FirmwareNew = (_txtFwNew.Text ?? "").Trim(),

                Liga230V = _chkLiga230.Checked,
                InspecaoVisual = _chkInspecao.Checked,
                ConfigUploaded = _chkConfig.Checked,

                Rs232Score = (int)_numRs232.Value,
                Rs485Ok = _chkRs485.Checked,

                NumeroPortasEth = n,
                EthOk = eth,

                Antena = _chkAntena.Checked,
                CaboAlimentacao = _chkCaboAlim.Checked,
                CaboRS232 = _chkCabo232.Checked,
                CaboRS485 = _chkCabo485.Checked,

                Comentario = _txtComment.Text ?? "",
                ConformidadeFinal = _chkConforme.Checked
            };

            return r;
        }
    }
}
