namespace Avani.Andon.Energy.Biz.Service
{
    partial class ProjectInstaller
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.iAndonBizServiceV3Installer = new System.ServiceProcess.ServiceInstaller();
            // 
            // ServiceProcessInstaller
            // 
            this.ServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.ServiceProcessInstaller.Password = null;
            this.ServiceProcessInstaller.Username = null;
            // 
            // iAndonBizServiceV3Installer
            // 
            this.iAndonBizServiceV3Installer.DelayedAutoStart = true;
            this.iAndonBizServiceV3Installer.Description = "The iAndon Biz Service Enables you to proccess bussiness with data from processes" +
    ".";
            this.iAndonBizServiceV3Installer.DisplayName = "Avani iAndon Biz Service V3";
            this.iAndonBizServiceV3Installer.ServiceName = "Service";
            this.iAndonBizServiceV3Installer.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.ServiceProcessInstaller,
            this.iAndonBizServiceV3Installer});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller ServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller iAndonBizServiceV3Installer;
    }
}