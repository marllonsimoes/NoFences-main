using ControlzEx.Theming;
using NoFences.Model;
using NoFences.View;
using NoFences.View.Modern;
using NoFencesService.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace NoFences.ApplicationLogic
{
    internal class TrayIconManager : IApplicationService
    {
        private NotifyIcon notifyIcon;
        private MenuItem toggleStartUpMenuItem;

        private System.Windows.Application wpfApp;
        private WhatsThat wpfWindow;

        public void Start()
        {
            toggleStartUpMenuItem = new MenuItem("Start on login", (s, e) => ToggleAutoStart())
            {
                Checked = StartupManager.IsAutoStartEnabled()
            };

            notifyIcon = new NotifyIcon
            {
                Text = "NoFences",
                Icon = new System.Drawing.Icon("fibonacci.ico"),
                ContextMenu = new ContextMenu(new[]
                {
                    toggleStartUpMenuItem,
                    new MenuItem("New Fence", (s, e) => FenceManager.Instance.CreateFence("New Fence")),
                    new MenuItem("Open local storage", (s, e) => OpenLocalStorage()),
                    new MenuItem("Open WPF", (s, e) => OpenWpfWindow()),
                    new MenuItem("Exit", (s, e) => ExitApplication())
                }),
                Visible = true
            };
        }

        public void Stop()
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                notifyIcon = null;
            }

            wpfWindow?.Close();
            wpfApp?.Shutdown();
        }

        private void ToggleAutoStart()
        {
            bool isEnabled = StartupManager.IsAutoStartEnabled();
            StartupManager.ToggleAutoStart(!isEnabled);
            toggleStartUpMenuItem.Checked = !isEnabled;
        }

        private void OpenLocalStorage()
        {
            string basePath = Path.Combine(AppEnvUtil.GetAppEnvironmentPath(), "Fences");
            var psi = new ProcessStartInfo() { FileName = basePath, UseShellExecute = true };
            Process.Start(psi);
        }

        private void OpenWpfWindow()
        {
            if (wpfApp == null)
            {
                wpfApp = new System.Windows.Application();
                DependencyInjectionSetup.InitializeIoCContainer();
            }

            if (wpfWindow == null)
            {
                wpfWindow = new WhatsThat();
                ElementHost.EnableModelessKeyboardInterop(wpfWindow);

                ResourceDictionary dictControls = new ResourceDictionary { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml") };
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(dictControls);

                ResourceDictionary dictFonts = new ResourceDictionary { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml") };
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(dictFonts);

                ThemeManager.Current.SyncTheme();
            }
            wpfWindow.Show();
        }

        private void ExitApplication()
        {
            System.Windows.Forms.Application.Exit();
        }
    }
}
