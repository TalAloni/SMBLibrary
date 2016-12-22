namespace SMBServer
{
    partial class ServerUI
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
            this.comboIPAddress = new System.Windows.Forms.ComboBox();
            this.rbtNetBiosOverTCP = new System.Windows.Forms.RadioButton();
            this.rbtDirectTCPTransport = new System.Windows.Forms.RadioButton();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.lblTransport = new System.Windows.Forms.Label();
            this.lblAddress = new System.Windows.Forms.Label();
            this.chkIntegratedWindowsAuthentication = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // comboIPAddress
            // 
            this.comboIPAddress.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboIPAddress.FormattingEnabled = true;
            this.comboIPAddress.Location = new System.Drawing.Point(79, 6);
            this.comboIPAddress.Name = "comboIPAddress";
            this.comboIPAddress.Size = new System.Drawing.Size(121, 21);
            this.comboIPAddress.TabIndex = 0;
            // 
            // rbtNetBiosOverTCP
            // 
            this.rbtNetBiosOverTCP.AutoSize = true;
            this.rbtNetBiosOverTCP.Checked = true;
            this.rbtNetBiosOverTCP.Location = new System.Drawing.Point(79, 41);
            this.rbtNetBiosOverTCP.Name = "rbtNetBiosOverTCP";
            this.rbtNetBiosOverTCP.Size = new System.Drawing.Size(164, 17);
            this.rbtNetBiosOverTCP.TabIndex = 2;
            this.rbtNetBiosOverTCP.TabStop = true;
            this.rbtNetBiosOverTCP.Text = "NetBIOS over TCP (Port 139)";
            this.rbtNetBiosOverTCP.UseVisualStyleBackColor = true;
            // 
            // rbtDirectTCPTransport
            // 
            this.rbtDirectTCPTransport.AutoSize = true;
            this.rbtDirectTCPTransport.Location = new System.Drawing.Point(79, 64);
            this.rbtDirectTCPTransport.Name = "rbtDirectTCPTransport";
            this.rbtDirectTCPTransport.Size = new System.Drawing.Size(174, 17);
            this.rbtDirectTCPTransport.TabIndex = 3;
            this.rbtDirectTCPTransport.Text = "Direct TCP Transport (Port 445)";
            this.rbtDirectTCPTransport.UseVisualStyleBackColor = true;
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(339, 9);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(91, 23);
            this.btnStart.TabIndex = 4;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnStop
            // 
            this.btnStop.Enabled = false;
            this.btnStop.Location = new System.Drawing.Point(339, 38);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(91, 23);
            this.btnStop.TabIndex = 5;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // lblTransport
            // 
            this.lblTransport.AutoSize = true;
            this.lblTransport.Location = new System.Drawing.Point(12, 43);
            this.lblTransport.Name = "lblTransport";
            this.lblTransport.Size = new System.Drawing.Size(55, 13);
            this.lblTransport.TabIndex = 14;
            this.lblTransport.Text = "Transport:";
            // 
            // lblAddress
            // 
            this.lblAddress.AutoSize = true;
            this.lblAddress.Location = new System.Drawing.Point(12, 9);
            this.lblAddress.Name = "lblAddress";
            this.lblAddress.Size = new System.Drawing.Size(61, 13);
            this.lblAddress.TabIndex = 13;
            this.lblAddress.Text = "IP Address:";
            // 
            // chkIntegratedWindowsAuthentication
            // 
            this.chkIntegratedWindowsAuthentication.AutoSize = true;
            this.chkIntegratedWindowsAuthentication.Checked = true;
            this.chkIntegratedWindowsAuthentication.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkIntegratedWindowsAuthentication.Location = new System.Drawing.Point(79, 94);
            this.chkIntegratedWindowsAuthentication.Name = "chkIntegratedWindowsAuthentication";
            this.chkIntegratedWindowsAuthentication.Size = new System.Drawing.Size(192, 17);
            this.chkIntegratedWindowsAuthentication.TabIndex = 15;
            this.chkIntegratedWindowsAuthentication.Text = "Integrated Windows Authentication";
            this.chkIntegratedWindowsAuthentication.UseVisualStyleBackColor = true;
            // 
            // ServerUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(442, 123);
            this.Controls.Add(this.chkIntegratedWindowsAuthentication);
            this.Controls.Add(this.lblTransport);
            this.Controls.Add(this.lblAddress);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.rbtDirectTCPTransport);
            this.Controls.Add(this.rbtNetBiosOverTCP);
            this.Controls.Add(this.comboIPAddress);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(450, 150);
            this.MinimumSize = new System.Drawing.Size(450, 150);
            this.Name = "ServerUI";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "SMB Server";
            this.Load += new System.EventHandler(this.ServerUI_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboIPAddress;
        private System.Windows.Forms.RadioButton rbtNetBiosOverTCP;
        private System.Windows.Forms.RadioButton rbtDirectTCPTransport;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Label lblTransport;
        private System.Windows.Forms.Label lblAddress;
        private System.Windows.Forms.CheckBox chkIntegratedWindowsAuthentication;
    }
}

