using System.ServiceProcess;

namespace NoFencesService
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new NoFencesService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
