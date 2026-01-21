using System;
using System.Drawing;
using System.Windows.Forms;

namespace Recondicionamento_DTC_Routers.UI
{
    public sealed class StartForm : Form
    {
        private Button _btnRouter;
        private Button _btnDtc;
        private Button _btnSettings;
        private Label _lbl;

        public StartForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Recondicionamento - Start";
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Font;
            MinimumSize = new Size(520, 280);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14),
                ColumnCount = 1,
                RowCount = 4
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _lbl = new Label
            {
                Text = "O que vais processar agora?",
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Dock = DockStyle.Top
            };

            var buttons = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            _btnRouter = new Button { Text = "ROUTER", Dock = DockStyle.Fill, Height = 80, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            _btnDtc = new Button { Text = "DTC", Dock = DockStyle.Fill, Height = 80, Font = new Font("Segoe UI", 11, FontStyle.Bold) };

            _btnRouter.Click += (_, __) =>
            {
                Hide();
                using var f = new RouterWorkflowForm();
                f.ShowDialog(this);
                Show();
            };

            _btnDtc.Click += (_, __) =>
            {
                Hide();
                using var f = new DtcWorkflowForm();
                f.ShowDialog(this);
                Show();
            };


            buttons.Controls.Add(_btnRouter, 0, 0);
            buttons.Controls.Add(_btnDtc, 1, 0);

            _btnSettings = new Button { Text = "Settings", Dock = DockStyle.Right, Width = 120 };
            _btnSettings.Click += (_, __) =>
            {
                Dialogs.Info(this, "Settings", "Liga aqui o teu Form2 (settings) se quiseres.");
            };

            var tip = new Label
            {
                Text = $"IP atual: {Configuration.configurationValues.ip} | RouterPort: {Configuration.configurationValues.routerPort} | DtcPort: {Configuration.configurationValues.dtcPort}",
                AutoSize = true,
                ForeColor = Color.DimGray,
                Dock = DockStyle.Top
            };

            root.Controls.Add(_lbl, 0, 0);
            root.Controls.Add(buttons, 0, 1);
            root.Controls.Add(tip, 0, 2);
            root.Controls.Add(_btnSettings, 0, 3);

            Controls.Add(root);
        }
    }
}
