using CommunityToolkit.Mvvm.DependencyInjection;
using ControlzEx.Theming;
using IWshRuntimeLibrary;
using Microsoft.Extensions.DependencyInjection;
using NoFences.Model;
using NoFences.View;
using NoFences.View.Modern;
using NoFences.View.Service;
using NoFencesService.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Xml.Serialization;
using File = System.IO.File;

namespace NoFences
{

    class Program
    {

        static NotifyIcon NotifyIcon;
        static MenuItem toggleStartUp;
        static NamedPipeServerStream pipeServer;
        static System.Windows.Forms.Timer timer;
        static ServiceController sc;
        static System.Windows.Application WpfApp;
        static WhatsThat wpfWindow;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (var mutex = new Mutex(true, "No_fences", out var createdNew))
            {
                if (createdNew)
                {
                    System.Windows.Forms.Application.EnableVisualStyles();
                    System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

                    toggleStartUp = new MenuItem("Start on login", StartOnLogin)
                    {
                        Checked = IsAutoStart()
                    };

                    NotifyIcon = new NotifyIcon
                    {
                        Text = "NoFences",
                        Icon = new System.Drawing.Icon("fibonacci.ico"),

                        ContextMenu = new ContextMenu(new[]
                        {
                            toggleStartUp,
                            new MenuItem("New Fence", NewFence),
                            new MenuItem("Open local storage",  (sender, e) => {
                                    string basePath = Path.Combine(AppEnvUtil.GetAppEnvironmentPath(), "Fences");
                                    var psi = new ProcessStartInfo() { FileName = basePath, UseShellExecute = true };
                                    Process.Start(psi);
                                }),
                            new MenuItem("Open WPF", (sends, args) => {

                                if (WpfApp == null) {
                                    new System.Windows.Application();
                                    WpfApp = System.Windows.Application.Current;
                                    // TODO initialize IOC container
                                    InitializeIoCContainer();
                                }
                                if (wpfWindow == null) 
                                {
                                    wpfWindow = new WhatsThat();
                                    ElementHost.EnableModelessKeyboardInterop(wpfWindow);

                                    //MahApps.Metro resource dictionaries. Make sure that all file names are Case Sensitive! 
                                    ResourceDictionary dictControls = new ResourceDictionary();
                                    dictControls.Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml");
                                    System.Windows.Application.Current.Resources.MergedDictionaries.Add(dictControls);

                                    ResourceDictionary dictFonts = new ResourceDictionary();
                                    dictFonts.Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml");
                                    System.Windows.Application.Current.Resources.MergedDictionaries.Add(dictFonts);

                                    //ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
                                    ThemeManager.Current.SyncTheme();
                                }
                                wpfWindow.Show();
                            }),
                            new MenuItem("Exit", ExitHandler)
                        }),
                        Visible = true
                    };

                    FenceManager.Instance.LoadFences();

                    if (System.Windows.Forms.Application.OpenForms.Count == 0)
                    {
                        FenceManager.Instance.CreateFence("First fence");
                    }

                    StopStartPipeAndTimer();
                    if (!IsCurrentProcessElevated())
                    {
                        StartElevatedAsync();
                    }

                    StartService();

                    System.Windows.Forms.Application.ApplicationExit += Application_ApplicationExit;
                    System.Windows.Forms.Application.Run();
                }
            }
        }

        private static void InitializeIoCContainer()
        {
            Ioc.Default.ConfigureServices(
               new ServiceCollection()
               .AddSingleton<ISettingsService, SettingsService>()
               .AddSingleton<IMonitoredPathService, MonitoredPathService>()
               .AddSingleton<IFoldersConfigurationService, FolderConfigurationService>()
               .AddSingleton<IDeviceInfoService, DeviceInfoService>()
               //.AddSingleton(RestService.For<IRedditService>("https://www.reddit.com/"))
               //.AddSingleton(RestService.For<IContactsService>("https://randomuser.me/"))
               .AddTransient<MonitoredPathsViewModel>() //ViewModels
               .AddTransient<MonitoredPathViewModel>()
               .AddTransient<FolderConfigurationViewModel>()
               //.AddTransient<ContactsListWidgetViewModel>()
               //.AddTransient<AsyncRelayCommandPageViewModel>()
               //.AddTransient<IocPageViewModel>()
               //.AddTransient<MessengerPageViewModel>()
               //.AddTransient<ObservableObjectPageViewModel>()
               //.AddTransient<ObservableValidatorPageViewModel>()
               //.AddTransient<ValidationFormWidgetViewModel>()
               //.AddTransient<RelayCommandPageViewModel>()
               //.AddTransient<CollectionsPageViewModel>()
               //.AddTransient<SamplePageViewModel>()
               .BuildServiceProvider());
        }

        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            StopService();
        }

        #region admin privilegdes

        [DllImport("libc")]
        private static extern uint geteuid();

        public static bool IsCurrentProcessElevated()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // https://github.com/dotnet/sdk/blob/v6.0.100/src/Cli/dotnet/Installer/Windows/WindowsUtils.cs#L38
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }

            // https://github.com/dotnet/maintenance-packages/blob/62823150914410d43a3fd9de246d882f2a21d5ef/src/Common/tests/TestUtilities/System/PlatformDetection.Unix.cs#L58
            // 0 is the ID of the root user
            return geteuid() == 0;
        }


        public static void StartElevatedAsync()
        {
            var currentProcessPath = Assembly.GetExecutingAssembly().Location ?? (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.ChangeExtension(typeof(Program).Assembly.Location, "exe")
                : Path.ChangeExtension(typeof(Program).Assembly.Location, null));

            var processStartInfo = CreateProcessStartInfo(currentProcessPath);

            var process = Process.Start(processStartInfo)
                ?? throw new InvalidOperationException("Could not start process.");

            process.WaitForExit(0);
        }

        private static ProcessStartInfo CreateProcessStartInfo(string processPath)
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ConfigureProcessStartInfoForWindows(ref startInfo, processPath);
            }

            return startInfo;
        }

        private static void ConfigureProcessStartInfoForWindows(ref ProcessStartInfo startInfo, string processPath)
        {
            startInfo.Verb = "runas";
            startInfo.FileName = processPath;
        }

        #endregion

        private static void StartService()
        {
            if (sc == null)
            {
                sc = new ServiceController("NoFencesService");
            }

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

        private static void StopService()
        {
            if (sc != null)
            {
                if (sc.Status == ServiceControllerStatus.Running)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped);
                }
            }
        }

        private static void StopStartPipeAndTimer()
        {
            if (pipeServer != null)
            {
                if (pipeServer.IsConnected)
                {
                    pipeServer.Disconnect();
                    pipeServer.Dispose();
                    pipeServer = null;
                }
            }
            pipeServer = new NamedPipeServerStream("NoFencesPipeServer", PipeDirection.In);
            Task t = pipeServer.WaitForConnectionAsync();

            if (timer != null && timer.Enabled)
            {
                timer.Stop();
                timer.Dispose();
            }
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 500;
            timer.Enabled = true;
            timer.Tick += Timer_Tick; ;
            timer.Start();
        }

        private static void Timer_Tick(object sender, EventArgs ev)
        {
            try
            {
                if (pipeServer.IsConnected)
                {
                    using (StreamReader sr = new StreamReader(pipeServer))
                    {
                        var result = sr.ReadToEnd();
                        Console.WriteLine(result);
                        if (result != null && result.Length > 0)
                        {
                            var serializer = new XmlSerializer(typeof(FenceInfo));
                            var stringReader = new StringReader(result);
                            var fence = serializer.Deserialize(stringReader) as FenceInfo;

                            System.Windows.Forms.Application.OpenForms[0].Invoke(new ShowFenceWindowDelegate(ShowFenceWindow), fence);
                            
                        }
                    }
                    StopStartPipeAndTimer();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading pipe: {0}", e);
            }
        }

        private delegate void ShowFenceWindowDelegate(FenceInfo fenceInfo);

        private static void ShowFenceWindow(FenceInfo fenceInfo)
        {
            new FenceWindow(fenceInfo).Show();
        }

        private static void ExitHandler(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
            Environment.Exit(0);
        }

        private static void NewFence(object sender, EventArgs e)
        {
            FenceManager.Instance.CreateFence("New Fence");
        }

        private static void StartOnLogin(object sender, EventArgs e)
        {
            bool toggle = IsAutoStart();
            ToggleAutoStart(!toggle);
            toggleStartUp.Checked = !toggle;
        }

        private static void ToggleAutoStart(bool toggle)
        {
            WshShell wshShell = new WshShell();
            IWshRuntimeLibrary.IWshShortcut shortcut;
            string startUpFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

            if (!toggle)
            {
                File.Delete(startUpFolderPath + "\\" + System.Windows.Forms.Application.ProductName + ".lnk");
            }
            else
            {
                shortcut = (IWshRuntimeLibrary.IWshShortcut)wshShell.CreateShortcut(startUpFolderPath + "\\" + System.Windows.Forms.Application.ProductName + ".lnk");
                shortcut.TargetPath = System.Windows.Forms.Application.ExecutablePath;
                shortcut.WorkingDirectory = System.Windows.Forms.Application.StartupPath;
                shortcut.Description = "Launch NoFences";
                shortcut.IconLocation = System.Windows.Forms.Application.StartupPath + @"\fibonacci _1_.ico";
                shortcut.Save();
            }
        }

        private static bool IsAutoStart()
        {
            string startUpFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            return File.Exists(startUpFolderPath + "\\" + System.Windows.Forms.Application.ProductName + ".lnk");
        }
    }
}
