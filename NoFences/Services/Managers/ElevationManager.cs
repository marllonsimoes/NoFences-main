using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace NoFences.Services.Managers
{
    internal static class ElevationManager
    {
        [DllImport("libc")]
        private static extern uint geteuid();

        internal static bool IsCurrentProcessElevated()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }

            return geteuid() == 0;
        }

        internal static void StartElevatedAsync()
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
    }
}
