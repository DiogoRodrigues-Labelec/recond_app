using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Recondicionamento_DTC_Routers.Infra
{
    public sealed class UiLogger
    {
        private readonly RichTextBox _rtb;
        private readonly Control _invoker;
        private readonly string _logPath;

        public UiLogger(Control invoker, RichTextBox rtb, string logPath = "log.txt")
        {
            _invoker = invoker;
            _rtb = rtb;
            _logPath = logPath;
        }

        public void Clear()
        {
            if (_invoker.InvokeRequired) _invoker.Invoke(new Action(Clear));
            else _rtb.Clear();
        }

        public async Task LogAsync(string message, bool toFile = true)
        {
            string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string block = $"\n====================\n{ts}\n====================\n{message}\n";

            if (_invoker.InvokeRequired)
            {
                _invoker.Invoke(new Action(() =>
                {
                    _rtb.AppendText(block);
                    _rtb.SelectionStart = _rtb.TextLength;
                    _rtb.ScrollToCaret();
                }));
            }
            else
            {
                _rtb.AppendText(block);
                _rtb.SelectionStart = _rtb.TextLength;
                _rtb.ScrollToCaret();
            }

            if (!toFile) return;

            try
            {
                using var sw = new StreamWriter(_logPath, append: true, Encoding.UTF8);
                await sw.WriteLineAsync(block);
            }
            catch
            {
                // não bloqueia o fluxo por falhas de IO
            }
        }
    }
}
