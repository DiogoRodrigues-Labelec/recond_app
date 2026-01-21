using System;
using System.Drawing;
using System.Windows.Forms;

namespace Recondicionamento_DTC_Routers.UI
{
    internal static class Dialogs
    {
        public static bool AskYesNo(IWin32Window owner, string title, string message, bool defaultYes = true)
        {
            var def = defaultYes ? MessageBoxDefaultButton.Button1 : MessageBoxDefaultButton.Button2;
            return MessageBox.Show(owner, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, def) == DialogResult.Yes;
        }

        public static void Info(IWin32Window owner, string title, string message)
        {
            MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void Warn(IWin32Window owner, string title, string message)
        {
            MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static int AskInt(IWin32Window owner, string title, string label, int min, int max, int defaultValue)
        {
            using var f = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false,
                MaximizeBox = false,
                Width = 420,
                Height = 170
            };

            var lbl = new Label { Left = 12, Top = 15, Width = 380, Text = label };
            var nud = new NumericUpDown { Left = 12, Top = 45, Width = 120, Minimum = min, Maximum = max, Value = defaultValue };
            var ok = new Button { Text = "OK", Left = 220, Top = 80, Width = 80, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "Cancelar", Left = 310, Top = 80, Width = 80, DialogResult = DialogResult.Cancel };

            f.Controls.AddRange(new Control[] { lbl, nud, ok, cancel });
            f.AcceptButton = ok;
            f.CancelButton = cancel;

            return f.ShowDialog(owner) == DialogResult.OK ? (int)nud.Value : defaultValue;
        }

        public static bool AskConformeNaoConforme(IWin32Window owner, string title, string question)
        {
            using var f = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false,
                MaximizeBox = false,
                Width = 460,
                Height = 190
            };

            var lbl = new Label
            {
                Left = 12,
                Top = 15,
                Width = 420,
                Height = 50,
                Text = question,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            bool result = false;

            var ok = new Button { Text = "Conforme", Left = 60, Top = 90, Width = 140, Height = 40, BackColor = Color.LightGreen };
            var nok = new Button { Text = "Não Conforme", Left = 240, Top = 90, Width = 140, Height = 40, BackColor = Color.LightCoral };

            ok.Click += (_, __) => { result = true; f.DialogResult = DialogResult.OK; f.Close(); };
            nok.Click += (_, __) => { result = false; f.DialogResult = DialogResult.OK; f.Close(); };

            f.Controls.AddRange(new Control[] { lbl, ok, nok });
            f.ShowDialog(owner);
            return result;
        }
    }
}
