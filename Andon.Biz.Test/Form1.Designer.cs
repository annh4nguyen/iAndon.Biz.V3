namespace iAndon.Biz.Test
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txtWebSocketURL = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtCustomerID = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.txtRabbitInterval = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtRabbitPassword = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtRabbitUser = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txtRabbitVHost = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtRabbitHost = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label8 = new System.Windows.Forms.Label();
            this.txtArchiveInterval = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.txtLiveInterval = new System.Windows.Forms.TextBox();
            this.txtLiveTime = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.label13 = new System.Windows.Forms.Label();
            this.txtLLogPath = new System.Windows.Forms.TextBox();
            this.txtLogLevel = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.txtWebSocketURL);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txtCustomerID);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(17, 16);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox1.Size = new System.Drawing.Size(479, 94);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Customer";
            // 
            // txtWebSocketURL
            // 
            this.txtWebSocketURL.Enabled = false;
            this.txtWebSocketURL.Location = new System.Drawing.Point(128, 57);
            this.txtWebSocketURL.Margin = new System.Windows.Forms.Padding(4);
            this.txtWebSocketURL.Name = "txtWebSocketURL";
            this.txtWebSocketURL.Size = new System.Drawing.Size(341, 22);
            this.txtWebSocketURL.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 62);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(108, 16);
            this.label2.TabIndex = 2;
            this.label2.Text = "WebSocket URL";
            // 
            // txtCustomerID
            // 
            this.txtCustomerID.Enabled = false;
            this.txtCustomerID.Location = new System.Drawing.Point(128, 22);
            this.txtCustomerID.Margin = new System.Windows.Forms.Padding(4);
            this.txtCustomerID.Name = "txtCustomerID";
            this.txtCustomerID.Size = new System.Drawing.Size(341, 22);
            this.txtCustomerID.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 27);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Customer ID";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.txtRabbitInterval);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.txtRabbitPassword);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.txtRabbitUser);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.txtRabbitVHost);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.txtRabbitHost);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Location = new System.Drawing.Point(17, 117);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox2.Size = new System.Drawing.Size(479, 188);
            this.groupBox2.TabIndex = 0;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "EMQX";
            this.groupBox2.Enter += new System.EventHandler(this.groupBox2_Enter);
            // 
            // txtRabbitInterval
            // 
            this.txtRabbitInterval.Enabled = false;
            this.txtRabbitInterval.Location = new System.Drawing.Point(128, 155);
            this.txtRabbitInterval.Margin = new System.Windows.Forms.Padding(4);
            this.txtRabbitInterval.Name = "txtRabbitInterval";
            this.txtRabbitInterval.Size = new System.Drawing.Size(341, 22);
            this.txtRabbitInterval.TabIndex = 9;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(9, 160);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(49, 16);
            this.label7.TabIndex = 8;
            this.label7.Text = "Topics";
            // 
            // txtRabbitPassword
            // 
            this.txtRabbitPassword.Enabled = false;
            this.txtRabbitPassword.Location = new System.Drawing.Point(128, 122);
            this.txtRabbitPassword.Margin = new System.Windows.Forms.Padding(4);
            this.txtRabbitPassword.Name = "txtRabbitPassword";
            this.txtRabbitPassword.Size = new System.Drawing.Size(341, 22);
            this.txtRabbitPassword.TabIndex = 7;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(9, 127);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(67, 16);
            this.label6.TabIndex = 6;
            this.label6.Text = "Gateways";
            // 
            // txtRabbitUser
            // 
            this.txtRabbitUser.Enabled = false;
            this.txtRabbitUser.Location = new System.Drawing.Point(128, 87);
            this.txtRabbitUser.Margin = new System.Windows.Forms.Padding(4);
            this.txtRabbitUser.Name = "txtRabbitUser";
            this.txtRabbitUser.Size = new System.Drawing.Size(341, 22);
            this.txtRabbitUser.TabIndex = 5;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(9, 92);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(36, 16);
            this.label5.TabIndex = 4;
            this.label5.Text = "User";
            // 
            // txtRabbitVHost
            // 
            this.txtRabbitVHost.Enabled = false;
            this.txtRabbitVHost.Location = new System.Drawing.Point(128, 54);
            this.txtRabbitVHost.Margin = new System.Windows.Forms.Padding(4);
            this.txtRabbitVHost.Name = "txtRabbitVHost";
            this.txtRabbitVHost.Size = new System.Drawing.Size(341, 22);
            this.txtRabbitVHost.TabIndex = 3;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(9, 59);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(31, 16);
            this.label4.TabIndex = 2;
            this.label4.Text = "Port";
            // 
            // txtRabbitHost
            // 
            this.txtRabbitHost.Enabled = false;
            this.txtRabbitHost.Location = new System.Drawing.Point(128, 20);
            this.txtRabbitHost.Margin = new System.Windows.Forms.Padding(4);
            this.txtRabbitHost.Name = "txtRabbitHost";
            this.txtRabbitHost.Size = new System.Drawing.Size(341, 22);
            this.txtRabbitHost.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 25);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 16);
            this.label3.TabIndex = 0;
            this.label3.Text = "Host";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label8);
            this.groupBox3.Controls.Add(this.txtArchiveInterval);
            this.groupBox3.Controls.Add(this.label9);
            this.groupBox3.Controls.Add(this.txtLiveInterval);
            this.groupBox3.Controls.Add(this.txtLiveTime);
            this.groupBox3.Controls.Add(this.label10);
            this.groupBox3.Location = new System.Drawing.Point(17, 313);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox3.Size = new System.Drawing.Size(479, 123);
            this.groupBox3.TabIndex = 0;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Archive";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(9, 26);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(98, 16);
            this.label8.TabIndex = 0;
            this.label8.Text = "Archive Interval";
            // 
            // txtArchiveInterval
            // 
            this.txtArchiveInterval.Enabled = false;
            this.txtArchiveInterval.Location = new System.Drawing.Point(128, 21);
            this.txtArchiveInterval.Margin = new System.Windows.Forms.Padding(4);
            this.txtArchiveInterval.Name = "txtArchiveInterval";
            this.txtArchiveInterval.Size = new System.Drawing.Size(341, 22);
            this.txtArchiveInterval.TabIndex = 1;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(9, 60);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(78, 16);
            this.label9.TabIndex = 2;
            this.label9.Text = "Live Interval";
            // 
            // txtLiveInterval
            // 
            this.txtLiveInterval.Enabled = false;
            this.txtLiveInterval.Location = new System.Drawing.Point(128, 55);
            this.txtLiveInterval.Margin = new System.Windows.Forms.Padding(4);
            this.txtLiveInterval.Name = "txtLiveInterval";
            this.txtLiveInterval.Size = new System.Drawing.Size(341, 22);
            this.txtLiveInterval.TabIndex = 3;
            // 
            // txtLiveTime
            // 
            this.txtLiveTime.Enabled = false;
            this.txtLiveTime.Location = new System.Drawing.Point(128, 89);
            this.txtLiveTime.Margin = new System.Windows.Forms.Padding(4);
            this.txtLiveTime.Name = "txtLiveTime";
            this.txtLiveTime.Size = new System.Drawing.Size(341, 22);
            this.txtLiveTime.TabIndex = 5;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(9, 94);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(66, 16);
            this.label10.TabIndex = 4;
            this.label10.Text = "Live Time";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.label13);
            this.groupBox4.Controls.Add(this.txtLLogPath);
            this.groupBox4.Controls.Add(this.txtLogLevel);
            this.groupBox4.Controls.Add(this.label12);
            this.groupBox4.Location = new System.Drawing.Point(17, 443);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox4.Size = new System.Drawing.Size(479, 90);
            this.groupBox4.TabIndex = 0;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "System";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(11, 25);
            this.label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(60, 16);
            this.label13.TabIndex = 0;
            this.label13.Text = "Log Path";
            // 
            // txtLLogPath
            // 
            this.txtLLogPath.Enabled = false;
            this.txtLLogPath.Location = new System.Drawing.Point(129, 20);
            this.txtLLogPath.Margin = new System.Windows.Forms.Padding(4);
            this.txtLLogPath.Name = "txtLLogPath";
            this.txtLLogPath.Size = new System.Drawing.Size(340, 22);
            this.txtLLogPath.TabIndex = 1;
            // 
            // txtLogLevel
            // 
            this.txtLogLevel.Enabled = false;
            this.txtLogLevel.Location = new System.Drawing.Point(129, 54);
            this.txtLogLevel.Margin = new System.Windows.Forms.Padding(4);
            this.txtLogLevel.Name = "txtLogLevel";
            this.txtLogLevel.Size = new System.Drawing.Size(340, 22);
            this.txtLogLevel.TabIndex = 3;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(11, 59);
            this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(66, 16);
            this.label12.TabIndex = 2;
            this.label12.Text = "Log Level";
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(388, 540);
            this.btnStart.Margin = new System.Windows.Forms.Padding(4);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(100, 28);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(388, 540);
            this.btnStop.Margin = new System.Windows.Forms.Padding(4);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(100, 28);
            this.btnStop.TabIndex = 1;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Visible = false;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // txtMessage
            // 
            this.txtMessage.Enabled = false;
            this.txtMessage.Location = new System.Drawing.Point(504, 23);
            this.txtMessage.Margin = new System.Windows.Forms.Padding(4);
            this.txtMessage.Multiline = true;
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtMessage.Size = new System.Drawing.Size(780, 509);
            this.txtMessage.TabIndex = 2;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1301, 577);
            this.Controls.Add(this.txtMessage);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnStop);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form1";
            this.Text = "Avani iAndon Biz";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.TextBox txtWebSocketURL;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtCustomerID;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtRabbitInterval;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtRabbitPassword;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtRabbitUser;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtRabbitVHost;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtRabbitHost;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtArchiveInterval;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtLiveInterval;
        private System.Windows.Forms.TextBox txtLiveTime;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtLLogPath;
        private System.Windows.Forms.TextBox txtLogLevel;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.TextBox txtMessage;
    }
}

