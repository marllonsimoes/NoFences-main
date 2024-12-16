using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NoFencesService.Util
{
    public sealed class AppEnvUtil
    {

        private static BackgroundWorker backgroundWorker = new BackgroundWorker();

        private static readonly string BaseAppEnvPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).CompanyName,
                    FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductName);

        public static string BasePath
        {
            get
            {
                return BaseAppEnvPath;
            }
        }

        public static void EnsureDirectoryExists(string path)
        {
            var di = new DirectoryInfo(Path.Combine(BaseAppEnvPath, path));
            if (!di.Exists)
                di.Create();
        }

        public static string GetAppEnvironmentPath()
        {
            return BaseAppEnvPath;
        }

        public static void EnsureAppEnvironmentPathExists()
        {
            EnsureDirectoryExists("");
        }

        public static bool EnsureFileExists(string fileName)
        {
            var file = new FileInfo(Path.Combine(BaseAppEnvPath, fileName));
            if (!file.Exists)
            {
                file.Create();
                return true;
            }
            return false;
        }

        public static void EnsureAllDataRequirementsAreAvaiable()
        {
            var refDb = EnsureFileExists("ref.db");
            if (refDb)
            {
                backgroundWorker.DoWork += BackgroundWorker_DoWork;
                backgroundWorker.RunWorkerAsync(Path.Combine(BaseAppEnvPath, "ref.db"));
            }
        }

        private static void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
           // TODO - start service to provide all the data needed
           // DownloadReferenceDatabase(e.Argument as string);
        }

        private static async void DownloadReferenceDatabase(string refDbPath)
        {
            HttpClient httpClient = new HttpClient()
            {
                BaseAddress = new Uri("http://localhost/living"),
            };

            using (HttpResponseMessage response = await httpClient.GetAsync("/management/services/ref"))
            {
                byte[] result = await response.EnsureSuccessStatusCode().Content.ReadAsByteArrayAsync();
                File.WriteAllBytes(refDbPath, result);
            }
        }
    }
}
