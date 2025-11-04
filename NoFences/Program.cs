using CommunityToolkit.Mvvm.DependencyInjection;
using log4net;
using log4net.Config;
using NoFences.Model;
using NoFences.Model.Canvas;
using NoFences.Services;
using NoFences.Services.Managers;
using NoFencesDataLayer.MasterCatalog.Tools;
using NoFencesDataLayer.Services;
using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using WpfApp = System.Windows.Application;

namespace NoFences
{
    class Program
    {
        private static ILog log;

        // P/Invoke for console allocation
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            log = LogManager.GetLogger(typeof(Program));

            // Check for command-line arguments to run catalog importer
            if (args.Length > 0 && args[0] == "--import-catalog")
            {
                AllocConsole();
                var exitCode = CatalogImportCommand.Execute(args);
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(exitCode);
                return;
            }

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

                // Initialize WPF Application for WPF controls (H.NotifyIcon, ContextMenus, etc.)
                // This must be created before any WPF controls are instantiated
                if (WpfApp.Current == null)
                {
                    var wpfApp = new WpfApp { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };

                    // Load MahApps.Metro resource dictionaries for theming support
                    wpfApp.Resources.MergedDictionaries.Add(
                        new System.Windows.ResourceDictionary
                        {
                            Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml")
                        });
                    wpfApp.Resources.MergedDictionaries.Add(
                        new System.Windows.ResourceDictionary
                        {
                            Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml")
                        });
                    wpfApp.Resources.MergedDictionaries.Add(
                        new System.Windows.ResourceDictionary
                        {
                            Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Themes/Light.Blue.xaml")
                        });
                }

                DependencyInjectionSetup.InitializeIoCContainerWithToolkit(useWorkerW: true);

                // Initialize software catalog database if not already done
                if (!SoftwareCatalogInitializer.IsCatalogInitialized())
                {
                    log.Info("Software catalog not initialized - attempting to build from local CSV or download from remote...");
                    bool success = SoftwareCatalogInitializer.InitializeFromLocalOrRemote();
                    if (success)
                    {
                        var stats = SoftwareCatalogInitializer.GetCatalogStatistics();
                        log.Info($"Software catalog initialized successfully: {stats}");
                    }
                    else
                    {
                        log.Warn("Failed to initialize software catalog - categorization will use heuristics only");
                    }
                }
                else
                {
                    log.Info("Software catalog already initialized");
                }

                var services = new List<IApplicationService>
                {
                    new TrayIconManager(),
                    //new WindowsServiceManager(),
                    new PipeService()
                    // Add new services here
                };

                Application.ApplicationExit += (sender, e) =>
                {
                    foreach (var service in services)
                    {
                        service.Stop();
                    }

                    // Shutdown WPF Application if it exists
                    if (WpfApp.Current != null)
                    {
                        WpfApp.Current.Dispatcher.Invoke(() => WpfApp.Current.Shutdown());
                    }
                };

                foreach (var service in services)
                {
                    service.Start();
                }

                log.Debug("Getting FenceManager from DI ===");
                var fenceManager = Ioc.Default.GetRequiredService<FenceManager>();

                log.Debug("Loading fences from disk ===");
                fenceManager.LoadFences();

                log.Debug("Getting canvas reference ===");
                var canvas = fenceManager.Canvas;
                log.Debug($"Canvas created: Handle={canvas.Handle}, Visible={canvas.Visible}, Bounds={canvas.Bounds}");

                log.Info("Showing canvas ===");
                fenceManager.ShowCanvas();
                log.Debug($"After ShowCanvas: Visible={canvas.Visible}, IsHandleCreated={canvas.IsHandleCreated}");

                if (fenceManager.FenceCount == 0)
                {
                    log.Info("No fences loaded, creating default fence ===");
                    fenceManager.CreateFence("New Fence (WPF)");
                }

                log.Info($"Starting Application.Run with Canvas ===");
                log.Debug($"Canvas state before Run: Visible={canvas.Visible}, TopMost={canvas.TopMost}, Opacity={canvas.Opacity}");
                Application.Run(canvas);
            }
        }
    }
}
