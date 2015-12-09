using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ConvertSysLogToCEF
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 
                { 
                    new TaniumSyslogToCEFConverter() 
                };
                ServiceBase.Run(ServicesToRun);
            }
            else if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "-install":
                        TaniumSyslogToCEFConverter.InstallService();
                        TaniumSyslogToCEFConverter.StartService();
                        break;
                    case "-uninstall":
                        TaniumSyslogToCEFConverter.StopService();
                        TaniumSyslogToCEFConverter.UninstallService();
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}
