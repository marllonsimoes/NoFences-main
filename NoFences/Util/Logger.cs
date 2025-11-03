using System;
using System.IO;

namespace NoFences.Util
{
    internal static class Logger
    {
        private static readonly string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NoFences_Log.txt");

        static Logger()
        {
            try
            {
                File.WriteAllText(logPath, string.Empty);
            }
            catch
            {
                // Ignore errors if the file can't be written
            }
        }

        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }
}
