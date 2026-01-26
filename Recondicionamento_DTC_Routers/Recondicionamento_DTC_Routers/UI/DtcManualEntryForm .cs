using Recondicionamento_DTC_Routers.Domain;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Recondicionamento_DTC_Routers.UI
{
    public sealed partial class DtcManualEntryForm : Form
    {
        public DtcRecord Result { get; private set; }

        private TextBox _txtFab, _txtId, _txtFwOld, _txtFwNew, _txtComment;
        private CheckBox _chkConfig, _chkS01Dtc, _chkS01Emi, _chkConforme;
        private Label _lblExpected;

        private Button _btnOk, _btnCancel;

        private readonly Func<string, string> _getExpectedFirmware;

        public DtcManualEntryForm(Func<string, string> getExpectedFirmware)
        {
            _getExpectedFirmware = getExpectedFirmware;

            Text = "Adicionar DTC manual ao report";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimumSize = new Size(900, 500);
            ClientSize = new Size(900, 520);

            BuildUi();
            Wire();
            RecalcConformidade();
        }

        public static DtcRecord Capture(IWin32Window owner, Func<string, string> getExpectedFirmware)
        {
            using var f = new DtcManualEntryForm(getExpectedFirmware);
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
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // topo
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // meio
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 140)); // bottom
            Controls.Add(root);

            // --- Dados (top-left) ---
            var gbDados = new GroupBox { Text = "Dados", Dock = DockStyle.Fill };
            var t = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), ColumnCount = 4, RowCount = 2 };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            _txtFab = new TextBox { Dock = DockStyle.Fill };
            _txtId = new TextBox { Dock = DockStyle.Fill };
            _txtFwOld = new TextBox { Dock = DockStyle.Fill };
            _txtFwNew = new TextBox { Dock = DockStyle.Fill };

            t.Controls.Add(new Label { Text = "Fabricante", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 0);
            t.Controls.Add(_txtFab, 1, 0);
            t.Controls.Add(new Label { Text = "ID / Serial", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 2, 0);
            t.Controls.Add(_txtId, 3, 0);

            t.Controls.Add(new Label { Text = "FW Old", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 1);
            t.Controls.Add(_txtFwOld, 1, 1);
            t.Controls.Add(new Label { Text = "FW New", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 2, 1);
            t.Controls.Add(_txtFwNew, 3, 1);

            gbDados.Controls.Add(t);
            root.Controls.Add(gbDados, 0, 0);

            // --- Resultados (top-right) ---
            var gbRes = new GroupBox { Text = "Resultados", Dock = DockStyle.Fill };
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10)
            };

            _chkConfig = new CheckBox { Text = "Config carregada (E-REDES)", AutoSize = true, Checked = true };
            _chkS01Dtc = new CheckBox { Text = "S01 DTC (Tensões/Correntes) OK", AutoSize = true, Checked = true };
            _chkS01Emi = new CheckBox { Text = "S01 EMI PLC OK", AutoSize = true, Checked = true };

            _chkConforme = new CheckBox { Text = "Conformidade final (auto)", AutoSize = true, Enabled = false };

            flow.Controls.Add(_chkConfig);
            flow.Controls.Add(_chkS01Dtc);
            flow.Controls.Add(_chkS01Emi);
            flow.Controls.Add(new Label { Height = 8 });
            flow.Controls.Add(_chkConforme);

            gbRes.Controls.Add(flow);
            root.Controls.Add(gbRes, 1, 0);

            // --- Firmware esperado (meio-left) ---
            var gbExp = new GroupBox { Text = "Firmware esperado (info)", Dock = DockStyle.Fill };
            _lblExpected = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextAlign = ContentAlignment.TopLeft,
                Padding = new Padding(10),
                Text = "Sem mapping de firmware esperado.\nTip: Se FW New estiver vazio, assumimos que ficou igual ao FW Old."
            };
            gbExp.Controls.Add(_lblExpected);
            root.Controls.Add(gbExp, 0, 1);

            // --- Notas (meio-right) ---
            var gbNotes = new GroupBox { Text = "Notas", Dock = DockStyle.Fill };
            var lbl = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Padding = new Padding(10),
                Text =
@"Checklist manual (resumo):
1) Identificar fabricante / ID
2) Confirmar FW old/new
3) Assinalar config + S01 DTC + S01 EMI
4) Adicionar comentário"
            };
            gbNotes.Controls.Add(lbl);
            root.Controls.Add(gbNotes, 1, 1);

            // --- Bottom: comentário + botões ---
            var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var gbComment = new GroupBox { Text = "Comentário", Dock = DockStyle.Fill };
            _txtComment = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };
            gbComment.Controls.Add(_txtComment);

            var btns = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true };
            _btnOk = new Button { Text = "Adicionar ao report", Width = 190, Height = 40 };
            _btnCancel = new Button { Text = "Cancelar", Width = 190, Height = 40 };
            btns.Controls.Add(_btnOk);
            btns.Controls.Add(_btnCancel);

            bottom.Controls.Add(gbComment, 0, 0);
            bottom.Controls.Add(btns, 1, 0);

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
                Result = BuildRecordFromUi();
                DialogResult = DialogResult.OK;
                Close();
            };

            _txtFab.TextChanged += (_, __) => UpdateExpectedFwBox();
            _txtFwOld.TextChanged += (_, __) => RecalcConformidade();
            _txtFwNew.TextChanged += (_, __) => RecalcConformidade();

            _chkConfig.CheckedChanged += (_, __) => RecalcConformidade();
            _chkS01Dtc.CheckedChanged += (_, __) => RecalcConformidade();
            _chkS01Emi.CheckedChanged += (_, __) => RecalcConformidade();

            UpdateExpectedFwBox();
        }

        private void UpdateExpectedFwBox()
        {
            string fab = (_txtFab.Text ?? "").Trim();
            string expected = _getExpectedFirmware?.Invoke(fab) ?? "";

            if (string.IsNullOrWhiteSpace(expected))
            {
                _lblExpected.Text = "Sem mapping de firmware esperado.\nTip: Se FW New estiver vazio, assumimos que ficou igual ao FW Old.";
            }
            else
            {
                _lblExpected.Text =
                    $"Firmware esperado para {fab}:\n  {expected}\n\n" +
                    "Tip: Marca conformidade apenas se FW final coincidir (ou se validação não for necessária).";
            }

            RecalcConformidade();
        }

        private void RecalcConformidade()
        {
            string fab = (_txtFab.Text ?? "").Trim();
            string expected = _getExpectedFirmware?.Invoke(fab) ?? "";

            string fwOld = (_txtFwOld.Text ?? "").Trim();
            string fwNew = (_txtFwNew.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(fwNew)) fwNew = fwOld;

            bool fwOk = true;
            if (!string.IsNullOrWhiteSpace(expected))
                fwOk = string.Equals(fwNew, expected, StringComparison.OrdinalIgnoreCase);

            bool conforme = _chkConfig.Checked && _chkS01Dtc.Checked && _chkS01Emi.Checked && fwOk;
            _chkConforme.Checked = conforme;
        }

        private DtcRecord BuildRecordFromUi()
        {
            string fwOld = (_txtFwOld.Text ?? "").Trim();
            string fwNew = (_txtFwNew.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(fwNew)) fwNew = fwOld;

            return new DtcRecord
            {
                Fabricante = (_txtFab.Text ?? "").Trim(),
                NumeroSerie = (_txtId.Text ?? "").Trim(),

                FirmwareOld = fwOld,
                FirmwareNew = fwNew,

                ConfigUploaded = _chkConfig.Checked,
                AnalogOk = _chkS01Dtc.Checked,
                EmiPlcOk = _chkS01Emi.Checked,

                Comentario = _txtComment.Text ?? "",
                ConformidadeFinal = _chkConforme.Checked
            };
        }
    }
}
