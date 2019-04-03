using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Copying_Solution
{
    static class Program
    {
        static void Main(string[] args)
        {
            // This is meant to debug things while still on the IDE
#if DEBUG
            Service service = new Service();
            service.DebugStart();
            // Acts as a while true
            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            return;
#endif
            // This allows us to give the .exe parameters that allow it to install and uninstall itself without much trouble.
            // (So long as the command line is run as administrator of course).
            if (Environment.UserInteractive)
            {
                string parameter = string.Concat(args);
                switch (parameter)
                {
                    case "--install":
                        ManagedInstallerClass.InstallHelper(new[] { Assembly.GetExecutingAssembly().Location });
                        break;
                    case "--uninstall":
                        ManagedInstallerClass.InstallHelper(new[] { "/u", Assembly.GetExecutingAssembly().Location });
                        break;
                }
                return;
            }

            ServiceBase[] servicesToRun = new ServiceBase[]
            {
                new Service()
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}
