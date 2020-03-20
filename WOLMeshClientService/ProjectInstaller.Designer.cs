namespace WOLMeshClientService
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
            this.spi = new System.ServiceProcess.ServiceProcessInstaller();
            this.si = new System.ServiceProcess.ServiceInstaller();
            // 
            // spi
            // 
            this.spi.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.spi.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.si});
            this.spi.Password = null;
            this.spi.Username = null;
            // 
            // si
            // 
            this.si.Description = "Wake On Lan Mesh Client Service";
            this.si.DisplayName = "Wake On Lan Mesh Client";
            this.si.ServiceName = "WOLMeshClient";
            this.si.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.spi});

        }

        #endregion
        private System.ServiceProcess.ServiceInstaller si;
        public System.ServiceProcess.ServiceProcessInstaller spi;
    }
}