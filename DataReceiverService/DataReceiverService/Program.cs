using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace DataReceiverService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                if (args[0] == "/startdebug") // simplify debugging.
                {
                    WebServiceMgr mgr = new WebServiceMgr();
                    mgr.Start(true);
                    while (true)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            }

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
            { 
                new Service1() 
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
