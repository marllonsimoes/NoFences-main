using System;
using System.IO;
using System.Net;

namespace NoFencesDataLayer.Services
{
    /// <summary>
    /// Service for downloading the software catalog database from a remote server.
    /// Used for initial setup when no local catalog exists.
    /// </summary>
    public class CatalogDownloadService
    {
        private const string DEFAULT_CATALOG_URL = "https://example.com/catalogs/software_catalog.db";
        private const int DOWNLOAD_TIMEOUT_SECONDS = 300; // 5 minutes

        /// <summary>
        /// Download catalog database from remote URL
        /// </summary>
        /// <param name="url">Remote catalog URL (optional, uses default if not provided)</param>
        /// <param name="destinationPath">Local path to save the downloaded database</param>
        /// <param name="progressCallback">Optional callback for progress updates (0-100)</param>
        /// <returns>True if download succeeded, false otherwise</returns>
        public static bool DownloadCatalog(string url, string destinationPath, Action<int> progressCallback = null)
        {
            try
            {
                var downloadUrl = string.IsNullOrWhiteSpace(url) ? DEFAULT_CATALOG_URL : url;

                Console.WriteLine($"Downloading catalog from: {downloadUrl}");
                Console.WriteLine($"Destination: {destinationPath}");

                // Ensure destination directory exists
                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "NoFences/1.0");

                    // Set timeout
                    var timeout = TimeSpan.FromSeconds(DOWNLOAD_TIMEOUT_SECONDS);

                    // Progress reporting
                    if (progressCallback != null)
                    {
                        client.DownloadProgressChanged += (sender, e) =>
                        {
                            progressCallback(e.ProgressPercentage);
                        };
                    }

                    // Download synchronously
                    client.DownloadFile(downloadUrl, destinationPath);
                }

                // Verify file was downloaded
                if (File.Exists(destinationPath) && new FileInfo(destinationPath).Length > 0)
                {
                    Console.WriteLine($"✓ Catalog downloaded successfully ({new FileInfo(destinationPath).Length / 1024 / 1024:F2} MB)");
                    return true;
                }
                else
                {
                    Console.WriteLine("✗ Download failed: File is empty or missing");
                    return false;
                }
            }
            catch (WebException ex)
            {
                Console.WriteLine($"✗ Download failed: {ex.Message}");
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = ex.Response as HttpWebResponse;
                    if (response != null)
                    {
                        Console.WriteLine($"  HTTP Status: {(int)response.StatusCode} {response.StatusDescription}");
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Download failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if catalog exists at remote URL
        /// </summary>
        public static bool CheckCatalogAvailability(string url)
        {
            try
            {
                var downloadUrl = string.IsNullOrWhiteSpace(url) ? DEFAULT_CATALOG_URL : url;

                var request = (HttpWebRequest)WebRequest.Create(downloadUrl);
                request.Method = "HEAD";
                request.Timeout = 10000; // 10 seconds

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get remote catalog size in bytes
        /// </summary>
        public static long? GetRemoteCatalogSize(string url)
        {
            try
            {
                var downloadUrl = string.IsNullOrWhiteSpace(url) ? DEFAULT_CATALOG_URL : url;

                var request = (HttpWebRequest)WebRequest.Create(downloadUrl);
                request.Method = "HEAD";
                request.Timeout = 10000;

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return response.ContentLength;
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return null;
        }
    }
}
