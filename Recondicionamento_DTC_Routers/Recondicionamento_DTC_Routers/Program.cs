using System;
using System.Windows.Forms;

namespace Recondicionamento_DTC_Routers
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Configuration.LoadSettings(); // mantém o teu config

            Application.Run(new UI.StartForm());
        }
    }
}
