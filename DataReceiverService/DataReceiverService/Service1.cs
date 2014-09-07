using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace DataReceiverService
{
    public partial class Service1 : ServiceBase
    {
        public static string ServiceNameValue = "Temperature Data Receiver";
        public Service1()
        {
            InitializeComponent();
            ServiceName = ServiceNameValue;
            mgr = new WebServiceMgr();
        }
        WebServiceMgr mgr;
        protected override void OnStart(string[] args)
        {
            mgr.Start();
        }

        protected override void OnStop()
        {
            mgr.Stop();
        }
    }

    // Provide the ProjectInstaller class which allows 
    // the service to be installed by the Installutil.exe tool
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller process;
        private ServiceInstaller service;

        public ProjectInstaller()
        {
            process = new ServiceProcessInstaller();
            process.Account = ServiceAccount.LocalSystem;
            service = new ServiceInstaller();
            service.ServiceName = Service1.ServiceNameValue;
            Installers.Add(process);
            Installers.Add(service);
        }
    }

}
