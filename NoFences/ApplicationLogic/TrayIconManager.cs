using ControlzEx.Theming;
using NoFences.Model;
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
                    new MenuItem("New Fence", (s, e) => CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<FenceManager>().CreateFence("New Fence")),
                    new MenuItem("Open local storage", (s, e) => OpenLocalStorage()),
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

        private void ExitApplication()
        {
            System.Windows.Forms.Application.Exit();
        }
    }
}
