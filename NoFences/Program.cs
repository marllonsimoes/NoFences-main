using CommunityToolkit.Mvvm.DependencyInjection;
using log4net;
using log4net.Config;
using log4net.Core;
using log4net.Repository.Hierarchy;
using NoFences.Model;
using NoFences.Model.Canvas;
using NoFences.Services;
using NoFences.Services.Managers;
using NoFencesDataLayer.MasterCatalog.Tools;
using NoFencesDataLayer.Services;
using System;
using System.Collections.Generic;
using System.IO;
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

#if DEBUG
            var repository = log4net.LogManager.GetRepository();
            var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)repository;

            log4net.Core.Level log4netLevel = log4net.Core.Level.Debug;
            ((Logger)hierarchy.GetLogger("NoFencesDataLayer.Services.SteamStoreDetector")).Level = log4netLevel;
            ((Logger)hierarchy.GetLogger("NoFencesDataLayer.Services.AmazonGamesDetector")).Level = log4netLevel;
            ((Logger)hierarchy.GetLogger("NoFencesDataLayer.Services.EpicGamesDetector")).Level = log4netLevel;
            ((Logger)hierarchy.GetLogger("NoFencesDataLayer.Services.UbisoftConnectDetector")).Level = log4netLevel;
            ((Logger)hierarchy.GetLogger("NoFencesDataLayer.Services.GOGGalaxyDetector")).Level = log4netLevel;
            ((Logger)hierarchy.GetLogger("NoFencesDataLayer.Services.EAAppDetector")).Level = log4netLevel;
            hierarchy.RaiseConfigurationChanged(EventArgs.Empty);
#endif

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

                // Initialize installed software database automatically if empty
                InitializeInstalledSoftwareDatabase();

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

        /// <summary>
        /// Automatically initializes the installed software database if it's empty.
        /// Runs on background thread to avoid blocking UI startup.
        /// </summary>
        private static void InitializeInstalledSoftwareDatabase()
        {
            try
            {
                log.Info("Checking if installed software database needs initialization...");

                // Check if database is empty
                var service = new InstalledSoftwareService();
                var existingCount = service.GetInstalledSoftwareCount();

                if (existingCount > 0)
                {
                    log.Info($"Installed software database already populated with {existingCount} entries");
                    return;
                }

                log.Info("Installed software database is empty - starting automatic population in background");

                // Run database population on background thread to avoid blocking UI
                var populationThread = new Thread(() =>
                {
                    try
                    {
                        log.Info("Background thread: Starting installed software detection and database population");

                        int entriesWritten = service.RefreshInstalledSoftware();

                        if (entriesWritten > 0)
                        {
                            log.Info($"Background thread: Successfully populated database with {entriesWritten} software entries");
                        }
                        else
                        {
                            log.Warn("Background thread: No software entries were written to database");
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Background thread: Error during automatic database population: {ex.Message}", ex);
                    }
                })
                {
                    IsBackground = true,
                    Name = "InstalledSoftwareDBPopulation"
                };

                populationThread.Start();
                log.Info("Background database population thread started");
            }
            catch (Exception ex)
            {
                log.Error($"Error checking/initializing installed software database: {ex.Message}", ex);
            }
        }
    }
}
