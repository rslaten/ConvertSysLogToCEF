namespace ConvertSysLogToCEF
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
            this.TaniumSysLogToCEFConverterProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.TaniumSysLogToCEFConverterInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // TaniumSysLogToCEFConverterProcessInstaller
            // 
            this.TaniumSysLogToCEFConverterProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.TaniumSysLogToCEFConverterProcessInstaller.Password = null;
            this.TaniumSysLogToCEFConverterProcessInstaller.Username = null;
            // 
            // TaniumSysLogToCEFConverterInstaller
            // 
            this.TaniumSysLogToCEFConverterInstaller.ServiceName = "Tanium Syslog to CEF Converter";
            this.TaniumSysLogToCEFConverterInstaller.Description = "Converts SysLog streamed over TCP to CEF and restreams it over TCP";
            this.TaniumSysLogToCEFConverterInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.TaniumSysLogToCEFConverterProcessInstaller,
            this.TaniumSysLogToCEFConverterInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller TaniumSysLogToCEFConverterProcessInstaller;
        private System.ServiceProcess.ServiceInstaller TaniumSysLogToCEFConverterInstaller;
    }
}