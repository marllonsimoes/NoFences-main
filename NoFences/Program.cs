using NoFences.ApplicationLogic;
using NoFences.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace NoFences
{
    class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (var mutex = new Mutex(true, "No_fences", out bool createdNew))
            {
                if (!createdNew)
                {
                    // Another instance is already running
                    return;
                }

                // It's recommended to run with elevated privileges to manage Windows Services
                // and avoid other potential permission issues.
                //if (!ElevationManager.IsCurrentProcessElevated())
                //{
                //    ElevationManager.StartElevatedAsync();
                //    // The new elevated process will take over. This instance should exit.
                //    return;
                //}

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                DependencyInjectionSetup.InitializeIoCContainer();

                // The application is composed of several services that manage different aspects.
                // To add new functionality, create a class that implements IApplicationService
                // and add it to this list.
                var services = new List<IApplicationService>
                {
                    new TrayIconManager(),
                    //new WindowsServiceManager(),
                    new PipeService()
                    // Add new services here
                };

                // Register an event handler to stop all services on application exit.
                Application.ApplicationExit += (sender, e) =>
                {
                    foreach (var service in services)
                    {
                        service.Stop();
                    }
                };

                // Start all registered services.
                foreach (var service in services)
                {
                    service.Start();
                }

                var fenceManager = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<FenceManager>();

                // Load existing fences from storage.
                fenceManager.LoadFences();

                // If no fences are loaded, create a default one to guide the user.
                if (Application.OpenForms.Count == 0)
                {
                    fenceManager.CreateFence("First fence");
                }

                // Start the main application message loop.
                Application.Run();
            }
        }
    }
}