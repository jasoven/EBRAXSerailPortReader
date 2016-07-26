namespace EBRAXRS232Service
{
    partial class EBRAXRS232SrvInstaller
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
            this.EBRAXRS232ReaderProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.EBRAXRS232ReaderServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // EBRAXRS232ReaderProcessInstaller
            // 
            this.EBRAXRS232ReaderProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.EBRAXRS232ReaderProcessInstaller.Password = null;
            this.EBRAXRS232ReaderProcessInstaller.Username = null;
            // 
            // EBRAXRS232ReaderServiceInstaller
            // 
            this.EBRAXRS232ReaderServiceInstaller.Description = "Service to Read status on Serial Port where an EBRAX Antiskiming kit is connected" +
                "";
            this.EBRAXRS232ReaderServiceInstaller.DisplayName = "EBRAXRS232ReaderService";
            this.EBRAXRS232ReaderServiceInstaller.ServiceName = "EBRAXRS232ReaderService";
            // 
            // EBRAXRS232SrvInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.EBRAXRS232ReaderProcessInstaller,
            this.EBRAXRS232ReaderServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller EBRAXRS232ReaderProcessInstaller;
        private System.ServiceProcess.ServiceInstaller EBRAXRS232ReaderServiceInstaller;
    }
}