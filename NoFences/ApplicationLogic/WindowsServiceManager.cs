using System.ServiceProcess;

namespace NoFences.ApplicationLogic
{
    internal class WindowsServiceManager : IApplicationService
    {
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
                    // TODO add error handler and logging
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
