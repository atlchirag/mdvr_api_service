using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace mdvr_api_service
{
    [RunInstaller(true)]
    public partial class CustomServiceInstaller : System.Configuration.Install.Installer
    {
        private ServiceProcessInstaller process;
        private ServiceInstaller service;
        public CustomServiceInstaller()
        {


            DateTime d1 = DateTime.Now;
            DateTime d2 = Convert.ToDateTime("2023 - 01 - 02 12:27:29.000");

            TimeSpan t = d1 - d2;
            double Nrofminutes = t.TotalMinutes;

            process = new ServiceProcessInstaller();
            process.Account = ServiceAccount.LocalSystem;

            service = new ServiceInstaller();
            service.ServiceName = "MdvrApiService";
            service.DisplayName = "MdvrApiService";
            service.StartType = ServiceStartMode.Automatic;

            Installers.Add(process);
            Installers.Add(service);
            InitializeComponent();
        }
    }
}
