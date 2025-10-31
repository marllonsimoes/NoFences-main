using IWshRuntimeLibrary;
using System;
using System.IO;
using System.Windows.Forms;

namespace NoFences.ApplicationLogic
{
    internal static class StartupManager
    {
        internal static bool IsAutoStartEnabled()
        {
            string startUpFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            return System.IO.File.Exists(Path.Combine(startUpFolderPath, Application.ProductName + ".lnk"));
        }

        internal static void ToggleAutoStart(bool enable)
        {
            string startUpFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcutPath = Path.Combine(startUpFolderPath, Application.ProductName + ".lnk");

            if (enable)
            {
                WshShell wshShell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)wshShell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = Application.ExecutablePath;
                shortcut.WorkingDirectory = Application.StartupPath;
                shortcut.Description = "Launch NoFences";
                shortcut.IconLocation = Path.Combine(Application.StartupPath, @"fibonacci _1_.ico");
                shortcut.Save();
            }
            else
            {
                if (System.IO.File.Exists(shortcutPath))
                {
                    System.IO.File.Delete(shortcutPath);
                }
            }
        }
    }
}
