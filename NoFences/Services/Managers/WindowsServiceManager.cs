using log4net;
using NoFences.Util;
using System.ServiceProcess;

namespace NoFences.Services.Managers
{
    internal class WindowsServiceManager : IApplicationService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(WindowsServiceManager));

        private ServiceController sc;

        public WindowsServiceManager()
        {
            sc = new ServiceController("NoFencesService");
        }

        public void Start()
        {
            if (sc.Status != ServiceControllerStatus.Running)
            {
                try
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running);
                }
                catch
                {
                    log.Error($"Error while checking status");// TODO add error handler and logging
                }
            }
        }

        public void Stop()
        {
            if (sc != null && sc.Status == ServiceControllerStatus.Running)
            {
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped);
            }
        }
    }
}
