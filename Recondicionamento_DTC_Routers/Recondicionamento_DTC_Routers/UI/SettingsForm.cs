using Recondicionamento_DTC_Routers.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Recondicionamento_DTC_Routers
{
    public partial class SettingsForm : Form
    {
        private StartForm _MenuPrincipal;
        public SettingsForm(StartForm MenuPrincipal)
        {
            InitializeComponent();
            _MenuPrincipal = MenuPrincipal;
        }
        SettingsForm MenuSettings;
        private void btn_sair_Click(object sender, EventArgs e)
        {
            MenuSettings = this;
            MenuSettings.Close();
            //Configuration.configurationValues.
            Configuration.SaveSettings();
            _MenuPrincipal.Show();
        }


        private void txt_router_fw_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Selecionar Ficheiro";
                openFileDialog.Filter = "Todos os ficheiros (*.*)|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txt_path_config_fw.Text = openFileDialog.FileName;
                }
            }
        }





        private void btn_guardar_Click(object sender, EventArgs e)
        {
            Configuration.configurationValues.Path_ConfigFW = txt_path_config_fw.Text;
            Configuration.configurationValues.Path_log = txt_path_log.Text;
            Configuration.configurationValues.Path_report = txt_caminho_report.Text;

            Configuration.configurationValues.routerPort = txt_router_port.Text;
            Configuration.configurationValues.dtcPort = txt_dtc_port.Text;



            Configuration.configurationValues.dtcPass = txt_dtc_pass.Text;
            Configuration.configurationValues.routerPass = txt_router_pass.Text;

            Configuration.configurationValues.dtcUser = txt_dtc_user.Text;
            Configuration.configurationValues.routerUser = txt_router_user.Text;

            Configuration.configurationValues.ip = txt_ip.Text;

            Configuration.configurationValues.ns_emi = txt_ns_emi.Text;

            Configuration.configurationValues.portRS232 = txt_router_port_RS232.Text;
            Configuration.configurationValues.portRS485 = txt_router_port_RS485.Text;


            Configuration.SaveSettings();

        }

        private void Form2_Load(object sender, EventArgs e)
        {
            txt_path_config_fw.Text = Configuration.configurationValues.Path_ConfigFW;
            txt_path_log.Text = Configuration.configurationValues.Path_log;
            txt_caminho_report.Text = Configuration.configurationValues.Path_report;


            txt_router_port.Text = Configuration.configurationValues.routerPort;
            txt_dtc_port.Text = Configuration.configurationValues.dtcPort;



            txt_dtc_pass.Text = Configuration.configurationValues.dtcPass;
            txt_router_pass.Text = Configuration.configurationValues.routerPass;

            txt_dtc_user.Text = Configuration.configurationValues.dtcUser;
            txt_router_user.Text = Configuration.configurationValues.routerUser;

            txt_ip.Text = Configuration.configurationValues.ip;

            txt_ns_emi.Text = Configuration.configurationValues.ns_emi;

            txt_router_port_RS232.Text = Configuration.configurationValues.portRS232;
            txt_router_port_RS485.Text = Configuration.configurationValues.portRS485;

        }

        private void txt_path_log_TextChanged(object sender, EventArgs e)
        {
           
        }

        private void txt_path_log_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Selecionar Ficheiro";
                openFileDialog.Filter = "Todos os ficheiros (*.*)|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txt_path_log.Text = openFileDialog.FileName;
                }
            }
        }

        private void txt_caminho_report_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Selecionar Ficheiro";
                openFileDialog.Filter = "Todos os ficheiros (*.*)|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txt_caminho_report.Text = openFileDialog.FileName;
                }
            }
        }
    }
}
