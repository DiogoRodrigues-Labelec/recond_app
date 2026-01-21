//namespace Recondicionamento_DTC_Routers
//{
//    partial class Form2
//    {
//        /// <summary>
//        /// Required designer variable.
//        /// </summary>
//        private System.ComponentModel.IContainer components = null;

//        /// <summary>
//        /// Clean up any resources being used.
//        /// </summary>
//        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
//        protected override void Dispose(bool disposing)
//        {
//            if (disposing && (components != null))
//            {
//                components.Dispose();
//            }
//            base.Dispose(disposing);
//        }

//        #region Windows Form Designer generated code

//        /// <summary>
//        /// Required method for Designer support - do not modify
//        /// the contents of this method with the code editor.
//        /// </summary>
//        private void InitializeComponent()
//        {
//            this.btn_sair = new System.Windows.Forms.Button();
//            this.label1 = new System.Windows.Forms.Label();
//            this.txt_ip = new System.Windows.Forms.TextBox();
//            this.label2 = new System.Windows.Forms.Label();
//            this.txt_router_port = new System.Windows.Forms.TextBox();
//            this.label3 = new System.Windows.Forms.Label();
//            this.label4 = new System.Windows.Forms.Label();
//            this.txt_router_user = new System.Windows.Forms.TextBox();
//            this.label5 = new System.Windows.Forms.Label();
//            this.txt_router_pass = new System.Windows.Forms.TextBox();
//            this.label6 = new System.Windows.Forms.Label();
//            this.label7 = new System.Windows.Forms.Label();
//            this.label8 = new System.Windows.Forms.Label();
//            this.label9 = new System.Windows.Forms.Label();
//            this.txt_dtc_pass = new System.Windows.Forms.TextBox();
//            this.label10 = new System.Windows.Forms.Label();
//            this.txt_dtc_user = new System.Windows.Forms.TextBox();
//            this.label11 = new System.Windows.Forms.Label();
//            this.label12 = new System.Windows.Forms.Label();
//            this.txt_dtc_port = new System.Windows.Forms.TextBox();
//            this.label13 = new System.Windows.Forms.Label();
//            this.txt_router_config = new System.Windows.Forms.TextBox();
//            this.txt_dtc_config = new System.Windows.Forms.TextBox();
//            this.txt_router_fw = new System.Windows.Forms.TextBox();
//            this.txt_dtc_fw = new System.Windows.Forms.TextBox();
//            this.btn_guardar = new System.Windows.Forms.Button();
//            this.SuspendLayout();
//            // 
//            // btn_sair
//            // 
//            this.btn_sair.Location = new System.Drawing.Point(1131, 624);
//            this.btn_sair.Name = "btn_sair";
//            this.btn_sair.Size = new System.Drawing.Size(152, 55);
//            this.btn_sair.TabIndex = 8;
//            this.btn_sair.Text = "Sair";
//            this.btn_sair.UseVisualStyleBackColor = true;
//            this.btn_sair.Click += new System.EventHandler(this.btn_sair_Click);
//            // 
//            // label1
//            // 
//            this.label1.AutoSize = true;
//            this.label1.Font = new System.Drawing.Font("Malgun Gothic", 26.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.label1.Location = new System.Drawing.Point(47, 29);
//            this.label1.Name = "label1";
//            this.label1.Size = new System.Drawing.Size(53, 47);
//            this.label1.TabIndex = 10;
//            this.label1.Text = "IP";
//            // 
//            // txt_ip
//            // 
//            this.txt_ip.Font = new System.Drawing.Font("Malgun Gothic", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.txt_ip.Location = new System.Drawing.Point(106, 29);
//            this.txt_ip.Name = "txt_ip";
//            this.txt_ip.Size = new System.Drawing.Size(352, 54);
//            this.txt_ip.TabIndex = 11;
//            // 
//            // label2
//            // 
//            this.label2.AutoSize = true;
//            this.label2.Font = new System.Drawing.Font("Malgun Gothic", 26.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.label2.Location = new System.Drawing.Point(47, 105);
//            this.label2.Name = "label2";
//            this.label2.Size = new System.Drawing.Size(132, 47);
//            this.label2.TabIndex = 12;
//            this.label2.Text = "Router";
//            // 
//            // txt_router_port
//            // 
//            this.txt_router_port.Font = new System.Drawing.Font("Malgun Gothic", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.txt_router_port.Location = new System.Drawing.Point(303, 169);
//            this.txt_router_port.Name = "txt_router_port";
//            this.txt_router_port.Size = new System.Drawing.Size(238, 54);
//            this.txt_router_port.TabIndex = 13;
//            // 
//            // label3
//            // 
//            this.label3.AutoSize = true;
//            this.label3.Font = new System.Drawing.Font("Malgun Gothic", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.label3.Location = new System.Drawing.Point(111, 172);
//            this.label3.Name = "label3";
//            this.label3.Size = new System.Drawing.Size(103, 47);
//            this.label3.TabIndex = 14;
//            this.label3.Text = "Porta";
//            // 
//            // label4
//            // 
//            this.label4.AutoSize = true;
//            this.label4.Font = new System.Drawing.Font("Malgun Gothic", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.label4.Location = new System.Drawing.Point(111, 247);
//            this.label4.Name = "label4";
//            this.label4.Size = new System.Drawing.Size(186, 47);
//            this.label4.TabIndex = 15;
//            this.label4.Text = "UserName";
//            // 
//            // txt_router_user
//            // 
//            this.txt_router_user.Font = new System.Drawing.Font("Malgun Gothic", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.txt_router_user.Location = new System.Drawing.Point(303, 240);
//            this.txt_router_user.Name = "txt_router_user";
//            this.txt_router_user.Size = new System.Drawing.Size(238, 54);
//            this.txt_router_user.TabIndex = 16;
//            // 
//            // label5
//            // 
//            this.label5.AutoSize = true;
//            this.label5.Font = new System.Drawing.Font("Malgun Gothic", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.label5.Location = new System.Drawing.Point(111, 324);
//            this.label5.Name = "label5";
//            this.label5.Size = new System.Drawing.Size(175, 47);
//            this.label5.TabIndex = 17;
//            this.label5.Text = "PassWord";
//            // 
//            // txt_router_pass
//            // 
//            this.txt_router_pass.Font = new System.Drawing.Font("Malgun Gothic", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.txt_router_pass.Location = new System.Drawing.Point(303, 317);
//            this.txt_router_pass.Name = "txt_router_pass";
//            this.txt_router_pass.Size = new System.Drawing.Size(238, 54);
//            this.txt_router_pass.TabIndex = 18;
//            // 
//            // label6
//            // 
//            this.label6.AutoSize = true;
//            this.label6.Font = new System.Drawing.Font("Malgun Gothic", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.label6.Location = new System.Drawing.Point(111, 403);
//            this.label6.Name = "label6";
//            this.label6.Size = new System.Drawing.Size(187, 47);
//            this.label6.TabIndex = 19;
//            this.label6.Text = "FichConfig";
//            // 
//            // label7
//            // 
//            this.label7.AutoSize = true;
//            this.label7.Font = new System.Drawing.Font("Malgun Gothic", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.label7.Location = new System.Drawing.Point(111, 516);
//            this.label7.Name = "label7";
//            this.label7.Size = new System.Drawing.Size(133, 47);
//            this.label7.TabIndex = 21;
//            this.label7.Text = "FichFW";
//            // 
//            // label8
//            // 
//            this.label8.AutoSize = true;
//            this.label8.Font = new System.Drawing.Font("Malgun Gothic", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.label8.Location = new System.Drawing.Point(738, 516);
//            this.label8.Name = "label8";
//            this.label8.Size = new System.Drawing.Size(133, 47);
//            this.label8.TabIndex = 32;
//            this.label8.Text = "FichFW";
//            // 
//            // label9
//            // 
//            this.label9.AutoSize = true;
//            this.label9.Font = new System.Drawing.Font("Malgun Gothic", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.label9.Location = new System.Drawing.Point(738, 403);
//            this.label9.Name = "label9";
//            this.label9.Size = new System.Drawing.Size(187, 47);
//            this.label9.TabIndex = 30;
//            this.label9.Text = "FichConfig";
//            // 
//            // txt_dtc_pass
//            // 
//            this.txt_dtc_pass.Font = new System.Drawing.Font("Malgun Gothic", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.txt_dtc_pass.Location = new System.Drawing.Point(930, 317);
//            this.txt_dtc_pass.Name = "txt_dtc_pass";
//            this.txt_dtc_pass.Size = new System.Drawing.Size(238, 54);
//            this.txt_dtc_pass.TabIndex = 29;
//            // 
//            // label10
//            // 
//            this.label10.AutoSize = true;
//            this.label10.Font = new System.Drawing.Font("Malgun Gothic", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.label10.Location = new System.Drawing.Point(738, 324);
//            this.label10.Name = "label10";
//            this.label10.Size = new System.Drawing.Size(175, 47);
//            this.label10.TabIndex = 28;
//            this.label10.Text = "PassWord";
//            // 
//            // txt_dtc_user
//            // 
//            this.txt_dtc_user.Font = new System.Drawing.Font("Malgun Gothic", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.txt_dtc_user.Location = new System.Drawing.Point(930, 240);
//            this.txt_dtc_user.Name = "txt_dtc_user";
//            this.txt_dtc_user.Size = new System.Drawing.Size(238, 54);
//            this.txt_dtc_user.TabIndex = 27;
//            // 
//            // label11
//            // 
//            this.label11.AutoSize = true;
//            this.label11.Font = new System.Drawing.Font("Malgun Gothic", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.label11.Location = new System.Drawing.Point(738, 247);
//            this.label11.Name = "label11";
//            this.label11.Size = new System.Drawing.Size(186, 47);
//            this.label11.TabIndex = 26;
//            this.label11.Text = "UserName";
//            // 
//            // label12
//            // 
//            this.label12.AutoSize = true;
//            this.label12.Font = new System.Drawing.Font("Malgun Gothic", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.label12.Location = new System.Drawing.Point(738, 172);
//            this.label12.Name = "label12";
//            this.label12.Size = new System.Drawing.Size(103, 47);
//            this.label12.TabIndex = 25;
//            this.label12.Text = "Porta";
//            // 
//            // txt_dtc_port
//            // 
//            this.txt_dtc_port.Font = new System.Drawing.Font("Malgun Gothic", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.txt_dtc_port.Location = new System.Drawing.Point(930, 169);
//            this.txt_dtc_port.Name = "txt_dtc_port";
//            this.txt_dtc_port.Size = new System.Drawing.Size(238, 54);
//            this.txt_dtc_port.TabIndex = 24;
//            // 
//            // label13
//            // 
//            this.label13.AutoSize = true;
//            this.label13.Font = new System.Drawing.Font("Malgun Gothic", 26.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.label13.Location = new System.Drawing.Point(674, 105);
//            this.label13.Name = "label13";
//            this.label13.Size = new System.Drawing.Size(88, 47);
//            this.label13.TabIndex = 23;
//            this.label13.Text = "DTC";
//            // 
//            // txt_router_config
//            // 
//            this.txt_router_config.Font = new System.Drawing.Font("Footlight MT Light", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.txt_router_config.Location = new System.Drawing.Point(119, 465);
//            this.txt_router_config.Name = "txt_router_config";
//            this.txt_router_config.Size = new System.Drawing.Size(541, 24);
//            this.txt_router_config.TabIndex = 33;
//            this.txt_router_config.Text = "c:\\";
//            this.txt_router_config.Click += new System.EventHandler(this.txt_router_config_Click);
//            // 
//            // txt_dtc_config
//            // 
//            this.txt_dtc_config.Font = new System.Drawing.Font("Footlight MT Light", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.txt_dtc_config.Location = new System.Drawing.Point(746, 465);
//            this.txt_dtc_config.Name = "txt_dtc_config";
//            this.txt_dtc_config.Size = new System.Drawing.Size(541, 24);
//            this.txt_dtc_config.TabIndex = 35;
//            this.txt_dtc_config.Text = "c:\\";
//            this.txt_dtc_config.Click += new System.EventHandler(this.txt_dtc_config_Click);
//            // 
//            // txt_router_fw
//            // 
//            this.txt_router_fw.Font = new System.Drawing.Font("Footlight MT Light", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.txt_router_fw.Location = new System.Drawing.Point(119, 582);
//            this.txt_router_fw.Name = "txt_router_fw";
//            this.txt_router_fw.Size = new System.Drawing.Size(541, 24);
//            this.txt_router_fw.TabIndex = 36;
//            this.txt_router_fw.Text = "c:\\";
//            this.txt_router_fw.Click += new System.EventHandler(this.txt_router_fw_Click);
//            // 
//            // txt_dtc_fw
//            // 
//            this.txt_dtc_fw.Font = new System.Drawing.Font("Footlight MT Light", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
//            this.txt_dtc_fw.Location = new System.Drawing.Point(746, 582);
//            this.txt_dtc_fw.Name = "txt_dtc_fw";
//            this.txt_dtc_fw.Size = new System.Drawing.Size(541, 24);
//            this.txt_dtc_fw.TabIndex = 37;
//            this.txt_dtc_fw.Text = "c:\\";
//            this.txt_dtc_fw.Click += new System.EventHandler(this.txt_dtc_fw_Click);
//            // 
//            // btn_guardar
//            // 
//            this.btn_guardar.Location = new System.Drawing.Point(958, 624);
//            this.btn_guardar.Name = "btn_guardar";
//            this.btn_guardar.Size = new System.Drawing.Size(152, 55);
//            this.btn_guardar.TabIndex = 38;
//            this.btn_guardar.Text = "Guardar";
//            this.btn_guardar.UseVisualStyleBackColor = true;
//            this.btn_guardar.Click += new System.EventHandler(this.btn_guardar_Click);
//            // 
//            // Form2
//            // 
//            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
//            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
//            this.ClientSize = new System.Drawing.Size(1322, 707);
//            this.Controls.Add(this.btn_guardar);
//            this.Controls.Add(this.txt_dtc_fw);
//            this.Controls.Add(this.txt_router_fw);
//            this.Controls.Add(this.txt_dtc_config);
//            this.Controls.Add(this.txt_router_config);
//            this.Controls.Add(this.label8);
//            this.Controls.Add(this.label9);
//            this.Controls.Add(this.txt_dtc_pass);
//            this.Controls.Add(this.label10);
//            this.Controls.Add(this.txt_dtc_user);
//            this.Controls.Add(this.label11);
//            this.Controls.Add(this.label12);
//            this.Controls.Add(this.txt_dtc_port);
//            this.Controls.Add(this.label13);
//            this.Controls.Add(this.label7);
//            this.Controls.Add(this.label6);
//            this.Controls.Add(this.txt_router_pass);
//            this.Controls.Add(this.label5);
//            this.Controls.Add(this.txt_router_user);
//            this.Controls.Add(this.label4);
//            this.Controls.Add(this.label3);
//            this.Controls.Add(this.txt_router_port);
//            this.Controls.Add(this.label2);
//            this.Controls.Add(this.txt_ip);
//            this.Controls.Add(this.label1);
//            this.Controls.Add(this.btn_sair);
//            this.Name = "Form2";
//            this.Text = "Form2";
//            this.Load += new System.EventHandler(this.Form2_Load);
//            this.ResumeLayout(false);
//            this.PerformLayout();

//        }

//        #endregion

//        private System.Windows.Forms.Button btn_sair;
//        private System.Windows.Forms.Label label1;
//        private System.Windows.Forms.TextBox txt_ip;
//        private System.Windows.Forms.Label label2;
//        private System.Windows.Forms.TextBox txt_router_port;
//        private System.Windows.Forms.Label label3;
//        private System.Windows.Forms.Label label4;
//        private System.Windows.Forms.TextBox txt_router_user;
//        private System.Windows.Forms.Label label5;
//        private System.Windows.Forms.TextBox txt_router_pass;
//        private System.Windows.Forms.Label label6;
//        private System.Windows.Forms.Label label7;
//        private System.Windows.Forms.Label label8;
//        private System.Windows.Forms.Label label9;
//        private System.Windows.Forms.TextBox txt_dtc_pass;
//        private System.Windows.Forms.Label label10;
//        private System.Windows.Forms.TextBox txt_dtc_user;
//        private System.Windows.Forms.Label label11;
//        private System.Windows.Forms.Label label12;
//        private System.Windows.Forms.TextBox txt_dtc_port;
//        private System.Windows.Forms.Label label13;
//        private System.Windows.Forms.TextBox txt_router_config;
//        private System.Windows.Forms.TextBox txt_dtc_config;
//        private System.Windows.Forms.TextBox txt_router_fw;
//        private System.Windows.Forms.TextBox txt_dtc_fw;
//        private System.Windows.Forms.Button btn_guardar;
//    }
//}


namespace Recondicionamento_DTC_Routers
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btn_sair = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.txt_ip = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txt_router_port = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txt_router_user = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txt_router_pass = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txt_dtc_pass = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.txt_dtc_user = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.txt_dtc_port = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.txt_path_config_fw = new System.Windows.Forms.TextBox();
            this.btn_guardar = new System.Windows.Forms.Button();
            this.label14 = new System.Windows.Forms.Label();
            this.txt_ns_emi = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txt_router_port_RS232 = new System.Windows.Forms.TextBox();
            this.txt_path_log = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.txt_router_port_RS485 = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.txt_caminho_report = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btn_sair
            // 
            this.btn_sair.BackColor = System.Drawing.Color.Firebrick;
            this.btn_sair.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_sair.ForeColor = System.Drawing.Color.White;
            this.btn_sair.Location = new System.Drawing.Point(1165, 721);
            this.btn_sair.Name = "btn_sair";
            this.btn_sair.Size = new System.Drawing.Size(152, 55);
            this.btn_sair.TabIndex = 8;
            this.btn_sair.Text = "Sair";
            this.btn_sair.UseVisualStyleBackColor = false;
            this.btn_sair.Click += new System.EventHandler(this.btn_sair_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(47, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 47);
            this.label1.TabIndex = 10;
            this.label1.Text = "IP";
            // 
            // txt_ip
            // 
            this.txt_ip.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_ip.Location = new System.Drawing.Point(106, 29);
            this.txt_ip.Name = "txt_ip";
            this.txt_ip.Size = new System.Drawing.Size(352, 54);
            this.txt_ip.TabIndex = 11;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(38, 324);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(131, 47);
            this.label2.TabIndex = 12;
            this.label2.Text = "Router";
            // 
            // txt_router_port
            // 
            this.txt_router_port.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_router_port.Location = new System.Drawing.Point(294, 383);
            this.txt_router_port.Name = "txt_router_port";
            this.txt_router_port.Size = new System.Drawing.Size(238, 54);
            this.txt_router_port.TabIndex = 13;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(56, 383);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(194, 47);
            this.label3.TabIndex = 14;
            this.label3.Text = "Porta HTTP";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(102, 594);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(181, 47);
            this.label4.TabIndex = 15;
            this.label4.Text = "UserName";
            // 
            // txt_router_user
            // 
            this.txt_router_user.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_router_user.Location = new System.Drawing.Point(294, 587);
            this.txt_router_user.Name = "txt_router_user";
            this.txt_router_user.Size = new System.Drawing.Size(238, 54);
            this.txt_router_user.TabIndex = 16;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(102, 671);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(173, 47);
            this.label5.TabIndex = 17;
            this.label5.Text = "PassWord";
            // 
            // txt_router_pass
            // 
            this.txt_router_pass.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_router_pass.Location = new System.Drawing.Point(294, 664);
            this.txt_router_pass.Name = "txt_router_pass";
            this.txt_router_pass.Size = new System.Drawing.Size(238, 54);
            this.txt_router_pass.TabIndex = 18;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Segoe UI Semibold", 26.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(47, 100);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(381, 47);
            this.label7.TabIndex = 21;
            this.label7.Text = "CaminhoFich_Conf_FW";
            // 
            // txt_dtc_pass
            // 
            this.txt_dtc_pass.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_dtc_pass.Location = new System.Drawing.Point(921, 566);
            this.txt_dtc_pass.Name = "txt_dtc_pass";
            this.txt_dtc_pass.Size = new System.Drawing.Size(238, 54);
            this.txt_dtc_pass.TabIndex = 29;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(729, 573);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(173, 47);
            this.label10.TabIndex = 28;
            this.label10.Text = "PassWord";
            // 
            // txt_dtc_user
            // 
            this.txt_dtc_user.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_dtc_user.Location = new System.Drawing.Point(921, 489);
            this.txt_dtc_user.Name = "txt_dtc_user";
            this.txt_dtc_user.Size = new System.Drawing.Size(238, 54);
            this.txt_dtc_user.TabIndex = 27;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(729, 496);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(181, 47);
            this.label11.TabIndex = 26;
            this.label11.Text = "UserName";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.Location = new System.Drawing.Point(729, 421);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(102, 47);
            this.label12.TabIndex = 25;
            this.label12.Text = "Porta";
            // 
            // txt_dtc_port
            // 
            this.txt_dtc_port.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_dtc_port.Location = new System.Drawing.Point(921, 418);
            this.txt_dtc_port.Name = "txt_dtc_port";
            this.txt_dtc_port.Size = new System.Drawing.Size(238, 54);
            this.txt_dtc_port.TabIndex = 24;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.Location = new System.Drawing.Point(665, 354);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(87, 47);
            this.label13.TabIndex = 23;
            this.label13.Text = "DTC";
            // 
            // txt_path_config_fw
            // 
            this.txt_path_config_fw.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_path_config_fw.Location = new System.Drawing.Point(434, 118);
            this.txt_path_config_fw.Name = "txt_path_config_fw";
            this.txt_path_config_fw.Size = new System.Drawing.Size(544, 29);
            this.txt_path_config_fw.TabIndex = 36;
            this.txt_path_config_fw.Text = "c:\\";
            this.txt_path_config_fw.Click += new System.EventHandler(this.txt_router_fw_Click);
            // 
            // btn_guardar
            // 
            this.btn_guardar.BackColor = System.Drawing.Color.ForestGreen;
            this.btn_guardar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_guardar.ForeColor = System.Drawing.Color.White;
            this.btn_guardar.Location = new System.Drawing.Point(1007, 721);
            this.btn_guardar.Name = "btn_guardar";
            this.btn_guardar.Size = new System.Drawing.Size(152, 55);
            this.btn_guardar.TabIndex = 38;
            this.btn_guardar.Text = "Guardar";
            this.btn_guardar.UseVisualStyleBackColor = false;
            this.btn_guardar.Click += new System.EventHandler(this.btn_guardar_Click);
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label14.Location = new System.Drawing.Point(674, 32);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(158, 47);
            this.label14.TabIndex = 39;
            this.label14.Text = "N/S EMI";
            // 
            // txt_ns_emi
            // 
            this.txt_ns_emi.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_ns_emi.Location = new System.Drawing.Point(838, 32);
            this.txt_ns_emi.Name = "txt_ns_emi";
            this.txt_ns_emi.Size = new System.Drawing.Size(330, 54);
            this.txt_ns_emi.TabIndex = 40;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(56, 447);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(209, 47);
            this.label6.TabIndex = 42;
            this.label6.Text = "Porta RS232";
            // 
            // txt_router_port_RS232
            // 
            this.txt_router_port_RS232.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_router_port_RS232.Location = new System.Drawing.Point(294, 447);
            this.txt_router_port_RS232.Name = "txt_router_port_RS232";
            this.txt_router_port_RS232.Size = new System.Drawing.Size(238, 54);
            this.txt_router_port_RS232.TabIndex = 41;
            // 
            // txt_path_log
            // 
            this.txt_path_log.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_path_log.Location = new System.Drawing.Point(434, 177);
            this.txt_path_log.Name = "txt_path_log";
            this.txt_path_log.Size = new System.Drawing.Size(544, 29);
            this.txt_path_log.TabIndex = 44;
            this.txt_path_log.Text = "c:\\";
            this.txt_path_log.Click += new System.EventHandler(this.txt_path_log_Click);
            this.txt_path_log.TextChanged += new System.EventHandler(this.txt_path_log_TextChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Segoe UI Semibold", 26.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(47, 159);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(290, 47);
            this.label8.TabIndex = 43;
            this.label8.Text = "CaminhoFich_log";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(56, 522);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(209, 47);
            this.label9.TabIndex = 46;
            this.label9.Text = "Porta RS485";
            // 
            // txt_router_port_RS485
            // 
            this.txt_router_port_RS485.Font = new System.Drawing.Font("Segoe UI", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_router_port_RS485.Location = new System.Drawing.Point(294, 522);
            this.txt_router_port_RS485.Name = "txt_router_port_RS485";
            this.txt_router_port_RS485.Size = new System.Drawing.Size(238, 54);
            this.txt_router_port_RS485.TabIndex = 45;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("Segoe UI Semibold", 26.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label15.Location = new System.Drawing.Point(47, 227);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(340, 47);
            this.label15.TabIndex = 47;
            this.label15.Text = "CaminhoFich_report";
            // 
            // txt_caminho_report
            // 
            this.txt_caminho_report.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_caminho_report.Location = new System.Drawing.Point(434, 245);
            this.txt_caminho_report.Name = "txt_caminho_report";
            this.txt_caminho_report.Size = new System.Drawing.Size(544, 29);
            this.txt_caminho_report.TabIndex = 48;
            this.txt_caminho_report.Text = "c:\\";
            this.txt_caminho_report.Click += new System.EventHandler(this.txt_caminho_report_Click);
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1329, 802);
            this.Controls.Add(this.txt_caminho_report);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.txt_router_port_RS485);
            this.Controls.Add(this.txt_path_log);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.txt_router_port_RS232);
            this.Controls.Add(this.txt_ns_emi);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.btn_guardar);
            this.Controls.Add(this.txt_path_config_fw);
            this.Controls.Add(this.txt_dtc_pass);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.txt_dtc_user);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.txt_dtc_port);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.txt_router_pass);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txt_router_user);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txt_router_port);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txt_ip);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btn_sair);
            this.Name = "Form2";
            this.Text = "Form2";
            this.Load += new System.EventHandler(this.Form2_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_sair;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txt_ip;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txt_router_port;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txt_router_user;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txt_router_pass;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txt_dtc_pass;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txt_dtc_user;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox txt_dtc_port;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txt_path_config_fw;
        private System.Windows.Forms.Button btn_guardar;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox txt_ns_emi;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txt_router_port_RS232;
        private System.Windows.Forms.TextBox txt_path_log;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txt_router_port_RS485;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox txt_caminho_report;
    }
}