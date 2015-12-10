using System;
using System.Collections;
using System.ServiceProcess;
using System.Threading;
using System.IO;
using System.Configuration.Install;

//Service and Thread Handling Code
namespace ConvertSysLogToCEF
{
    public partial class TaniumSyslogToCEFConverter : ServiceBase
    {
        private ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
        private Thread _thread;
        private static String LogFile = AppDomain.CurrentDomain.BaseDirectory + "\\ConvertSysLogToCEFService.log";
        private static String ConverterServiceName = "Tanium Syslog to CEF Converter";

        public TaniumSyslogToCEFConverter()
        {
            InitializeComponent();
        }

        //Service Handling Functions
        private static bool IsInstalled()
        {
            using (ServiceController controller =
                new ServiceController(ConverterServiceName))
            {
                try
                {
                    ServiceControllerStatus status = controller.Status;       
                }
                catch
                {
                    return false;
                }
                return true;
            }
        }
        private static bool IsRunning()
        {
            using (ServiceController controller =
                new ServiceController(ConverterServiceName))
            {
                if (!IsInstalled()) return false;
                return (controller.Status == ServiceControllerStatus.Running);
            }
        }
        private static AssemblyInstaller GetInstaller()
        {
            AssemblyInstaller installer = new AssemblyInstaller(
                typeof(ProjectInstaller).Assembly, null);
            installer.UseNewContext = true;
            return installer;
        }
        public static void InstallService()
        {
            if (IsInstalled()) return;

            try
            {
                using (AssemblyInstaller installer = GetInstaller())
                {
                    IDictionary state = new Hashtable();
                    try
                    {
                        WriteErrorLog("Installing service...");
                        installer.Install(state);
                        installer.Commit(state);
                    }
                    catch (Exception e)
                    {
                        WriteErrorLog(e);
                        WriteErrorLog("Attempting rollback...");
                        try
                        {
                            installer.Rollback(state);
                        }
                        catch (Exception ex)
                        {
                            WriteErrorLog(ex);
                        }
                        throw;
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static void UninstallService()
        {
            if (!IsInstalled()) return;
            try
            {
                using (AssemblyInstaller installer = GetInstaller())
                {
                    IDictionary state = new Hashtable();
                    try
                    {
                        WriteErrorLog("Uninstalling service...");
                        installer.Uninstall(state);
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
            catch
            {
                throw;
            }
        }
        public static void StartService()
        {
            if (!IsInstalled()) return;

            using (ServiceController controller =
                new ServiceController(ConverterServiceName))
            {
                try
                {
                    if (controller.Status != ServiceControllerStatus.Running)
                    {
                        WriteErrorLog("Starting service...");
                        controller.Start();
                        controller.WaitForStatus(ServiceControllerStatus.Running,
                            TimeSpan.FromSeconds(10));
                    }
                }
                catch
                {
                    throw;
                }
            }
        }
        public static void StopService()
        {
            if (!IsInstalled()) return;
            using (ServiceController controller =
                new ServiceController(ConverterServiceName))
            {
                try
                {
                    if (controller.Status != ServiceControllerStatus.Stopped)
                    {
                        WriteErrorLog("Stopping service...");
                        controller.Stop();
                        controller.WaitForStatus(ServiceControllerStatus.Stopped,
                             TimeSpan.FromSeconds(10));
                    }
                }
                catch
                {
                    //The service throws an exception here, but stops anyway
                }
            }
        }

        //Thread Handling Functions
        protected override void OnStart(string[] args)
        {
            WriteErrorLog("The service has started");
            _thread = new Thread(new ThreadStart(WorkerThread));
            _thread.Name = "CEF Conversion Worker Thread";
            _thread.IsBackground = true;
            _thread.Start();
        }
        protected override void OnStop()
        {
            WriteErrorLog("Stopping TCP listener");
            _shutdownEvent.Set();
            CEF.Running = false;
            if (!_thread.Join(10000))
            {
                WriteErrorLog("Aborting TCP listener");
                _thread.Abort();
            }
            WriteErrorLog("The TCP listener has stopped");
            StopService();
        }
        public void WorkerThread()
        {
            try
            {
                while (!_shutdownEvent.WaitOne(0))
                {
                    WriteErrorLog("Starting TCP listener");
                    CEF.Running = true;
                    CEF.ConvertSysLogMessages();
                }
            }
            catch(Exception e)
            {
                WriteErrorLog(e);
            }
        }

        //Logging Functions
        public static void WriteErrorLog(Exception ex)
        {
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(LogFile, true);
                sw.WriteLine(DateTime.Now.ToString() + ": " + ex.Source.ToString().Trim() + "; " + ex.Message.ToString().Trim());
                sw.Flush();
                sw.Close();
            }
            catch { }
        }
        public static void WriteErrorLog(string Message)
        {
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(LogFile, true);
                sw.WriteLine(DateTime.Now.ToString() + ": " + Message);
                sw.Flush();
                sw.Close();
            }
            catch { }
        }
    }
}
