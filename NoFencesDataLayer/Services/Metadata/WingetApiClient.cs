using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace NoFencesDataLayer.Services.Metadata
{
    /// <summary>
    /// Winget (Windows Package Manager) API client for software metadata.
    /// Uses the winget REST API to search for software information.
    /// API: https://github.com/microsoft/winget-cli-restsource
    /// Session 11: Software metadata collection.
    /// </summary>
    public class WingetApiClient : ISoftwareMetadataProvider
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(WingetApiClient));
        private const string BASE_URL = "https://winget.azureedge.net/cache";
        private static readonly HttpClient httpClient = new HttpClient();

        public string ProviderName => "Winget";
        public int Priority => 1; // Highest priority for software (official MS source)

        public WingetApiClient()
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NoFences/1.6.2");
        }

        public bool IsAvailable()
        {
            // Winget API is public, no API key required
            return true;
        }

        /// <summary>
        /// Searches for software by name.
        /// </summary>
        public async Task<MetadataResult> SearchByNameAsync(string softwareName)
        {
            if (string.IsNullOrWhiteSpace(softwareName))
                return null;

            try
            {
                // Clean software name
                string cleanName = CleanSoftwareName(softwareName);

                log.Debug($"Winget: Searching for software '{cleanName}'");

                // Winget uses a manifests API
                // Note: The public API structure may change, this uses the known community repo
                string searchUrl = $"https://api.github.com/repos/microsoft/winget-pkgs/contents/manifests";

                // Alternative: Use winget command line locally
                // For now, we'll try a direct search approach
                var metadata = await SearchViaLocalWinget(cleanName);

                if (metadata != null)
                {
                    log.Info($"Winget: Found metadata for '{softwareName}' -> '{metadata.Name}'");
                }
                else
                {
                    log.Debug($"Winget: No results found for '{cleanName}'");
                }

                return metadata;
            }
            catch (Exception ex)
            {
                log.Error($"Winget: Error searching for software '{softwareName}': {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Searches for software by name and publisher.
        /// </summary>
        public async Task<MetadataResult> SearchByNameAndPublisherAsync(string softwareName, string publisher)
        {
            if (string.IsNullOrWhiteSpace(softwareName))
                return null;

            try
            {
                log.Debug($"Winget: Searching for '{softwareName}' by '{publisher}'");

                // Use local winget command with publisher filter
                var metadata = await SearchViaLocalWinget(softwareName, publisher);

                if (metadata != null)
                {
                    log.Info($"Winget: Found metadata for '{softwareName}' by '{publisher}'");
                }

                return metadata;
            }
            catch (Exception ex)
            {
                log.Error($"Winget: Error searching for '{softwareName}' by '{publisher}': {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Searches via local winget CLI (if available).
        /// This is more reliable than trying to parse the GitHub repo.
        /// </summary>
        private async Task<MetadataResult> SearchViaLocalWinget(string softwareName, string publisher = null)
        {
            try
            {
                // Check if winget is installed
                var wingetPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "WindowsApps", "winget.exe"
                );

                if (!System.IO.File.Exists(wingetPath))
                {
                    log.Debug("Winget CLI not found, skipping local search");
                    return null;
                }

                // Build winget search command
                string arguments = $"search \"{softwareName}\" --accept-source-agreements";
                if (!string.IsNullOrEmpty(publisher))
                {
                    arguments += $" --source winget --publisher \"{publisher}\"";
                }

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = wingetPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(startInfo))
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    process.WaitForExit();

                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        return ParseWingetOutput(output, softwareName);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                log.Warn($"Winget: Error executing local winget CLI: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parses winget CLI output to extract metadata.
        /// </summary>
        private MetadataResult ParseWingetOutput(string output, string searchTerm)
        {
            try
            {
                // Winget output format:
                // Name              Id                    Version    Source
                // -------------------------------------------------------
                // Google Chrome     Google.Chrome         119.0.6045 winget

                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // Skip header lines
                var dataLines = lines.Skip(2).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

                if (dataLines.Count == 0)
                    return null;

                // Take first result (most relevant)
                var firstLine = dataLines[0];
                var parts = firstLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 3)
                    return null;

                var metadata = new MetadataResult
                {
                    Source = ProviderName,
                    Name = parts[0],
                    Confidence = CalculateConfidence(parts[0], searchTerm)
                };

                // Package ID
                if (parts.Length > 1)
                {
                    metadata.AdditionalData["package_id"] = parts[1];
                }

                // Version
                if (parts.Length > 2)
                {
                    metadata.AdditionalData["version"] = parts[2];
                }

                // Try to get more details using winget show
                string packageId = null;
                metadata.AdditionalData.TryGetValue("package_id", out packageId);
                EnrichWithShowCommand(packageId, metadata);

                return metadata;
            }
            catch (Exception ex)
            {
                log.Warn($"Winget: Error parsing CLI output: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Enriches metadata using 'winget show' command.
        /// </summary>
        private void EnrichWithShowCommand(string packageId, MetadataResult metadata)
        {
            if (string.IsNullOrEmpty(packageId))
                return;

            try
            {
                var wingetPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "WindowsApps", "winget.exe"
                );

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = wingetPath,
                    Arguments = $"show {packageId} --accept-source-agreements",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        ParseShowOutput(output, metadata);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Warn($"Winget: Error enriching with show command: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses 'winget show' output to extract detailed metadata.
        /// </summary>
        private void ParseShowOutput(string output, MetadataResult metadata)
        {
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (line.Contains(":"))
                {
                    var parts = line.Split(new[] { ':' }, 2);
                    if (parts.Length != 2)
                        continue;

                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    switch (key.ToLower())
                    {
                        case "publisher":
                            metadata.Publisher = value;
                            break;
                        case "description":
                            metadata.Description = value;
                            break;
                        case "homepage":
                            metadata.WebsiteUrl = value;
                            break;
                        case "version":
                            metadata.AdditionalData["latest_version"] = value;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Cleans software name for better search results.
        /// </summary>
        private string CleanSoftwareName(string softwareName)
        {
            if (string.IsNullOrEmpty(softwareName))
                return softwareName;

            // Remove version numbers
            string cleaned = System.Text.RegularExpressions.Regex.Replace(
                softwareName,
                @"\s+\d+(\.\d+)*$",
                ""
            );

            // Remove common suffixes
            string[] suffixesToRemove = {
                " (x64)",
                " (x86)",
                " 64-bit",
                " 32-bit"
            };

            foreach (var suffix in suffixesToRemove)
            {
                if (cleaned.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned.Substring(0, cleaned.Length - suffix.Length).Trim();
                }
            }

            return cleaned;
        }

        /// <summary>
        /// Calculates confidence score based on name matching.
        /// </summary>
        private double CalculateConfidence(string foundName, string searchTerm)
        {
            if (string.IsNullOrEmpty(foundName) || string.IsNullOrEmpty(searchTerm))
                return 0.5;

            // Exact match
            if (string.Equals(foundName, searchTerm, StringComparison.OrdinalIgnoreCase))
                return 1.0;

            // Contains match
            if (foundName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                return 0.8;

            // Fuzzy match
            return 0.6;
        }
    }
}
