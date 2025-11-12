using log4net;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using NoFences.Core.Util;
namespace NoFencesDataLayer.Services
{
    /// <summary>
    /// Steam game store detector implementing IGameStoreDetector
    /// Wraps SteamGameDetector for the new abstraction
    /// </summary>
    public class SteamStoreDetector : IGameStoreDetector
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SteamStoreDetector));

        public string PlatformName => "Steam";

        public List<GameInfo> GetInstalledGames()
        {
            var games = new List<GameInfo>();

            try
            {
                string steamPath = GetInstallPath();
                if (string.IsNullOrEmpty(steamPath))
                {
                    log.Info("Steam installation not found");
                    return games;
                }

                log.Info($"Found Steam at {steamPath}");

                // Get all Steam library folders
                var libraryFolders = GetLibraryFolders(steamPath);
                log.Info($"Found {libraryFolders.Count} library folders");

                // Scan each library for installed games
                foreach (var libraryPath in libraryFolders)
                {
                    var libraryGames = ScanLibraryFolder(libraryPath);
                    games.AddRange(libraryGames);
                    log.Debug($"Found {libraryGames.Count} games in {libraryPath}");
                }

                log.Info($"Total {games.Count} Steam games found");
            }
            catch (Exception ex)
            {
                log.Error($"Error detecting Steam games: {ex.Message}", ex);
            }

            return games;
        }

        public bool IsInstalled()
        {
            var installPath = GetInstallPath();
            return !string.IsNullOrEmpty(installPath);
        }

        public string GetInstallPath()
        {
            try
            {
                // Check 64-bit registry
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
                {
                    if (key != null)
                    {
                        var installPath = key.GetValue("InstallPath") as string;
                        if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                            return installPath;
                    }
                }

                // Check 32-bit registry
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam"))
                {
                    if (key != null)
                    {
                        var installPath = key.GetValue("InstallPath") as string;
                        if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                            return installPath;
                    }
                }

                // Check current user registry
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam"))
                {
                    if (key != null)
                    {
                        var installPath = key.GetValue("SteamPath") as string;
                        if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                            return installPath;
                    }
                }

                // Fallback to default paths
                string[] defaultPaths = new[]
                {
                    @"C:\Program Files (x86)\Steam",
                    @"C:\Program Files\Steam",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam")
                };

                foreach (var path in defaultPaths)
                {
                    if (Directory.Exists(path))
                        return path;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error finding Steam path: {ex.Message}", ex);
            }

            return null;
        }

        public string FindOrCreateGameShortcut(string gameId, string gameName, string outputDirectory, string iconPath = null)
        {
            if (!int.TryParse(gameId, out int appId))
                return null;

            try
            {
                if (!Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);

                // Sanitize filename
                string safeGameName = string.Join("", gameName.Split(Path.GetInvalidFileNameChars()));
                string shortcutPath = Path.Combine(outputDirectory, $"{safeGameName}.url");

                if (File.Exists(shortcutPath))
                {
                    log.Debug($"Shortcut already exists at {shortcutPath}");
                    return shortcutPath;
                }

                // Determine icon file
                string iconFile = iconPath;
                if (string.IsNullOrEmpty(iconFile))
                {
                    iconFile = Path.Combine(GetInstallPath() ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steam.exe");
                }

                // Create .url file with Steam protocol
                string urlContent = $@"[InternetShortcut]
URL=steam://rungameid/{appId}
IconIndex=0
IconFile={iconFile}
";

                File.WriteAllText(shortcutPath, urlContent);
                return shortcutPath;
            }
            catch (Exception ex)
            {
                log.Error($"Error creating shortcut for {gameName}: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Parses libraryfolders.vdf to get all Steam library locations
        /// </summary>
        private List<string> GetLibraryFolders(string steamPath)
        {
            var libraries = new List<string>();

            try
            {
                // Primary library is always the Steam install location
                string primaryLibrary = Path.Combine(steamPath, "steamapps");
                if (Directory.Exists(primaryLibrary))
                    libraries.Add(primaryLibrary);

                // Parse libraryfolders.vdf for additional libraries
                string vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(vdfPath))
                {
                    // Try alternative path (new VDF format)
                    vdfPath = Path.Combine(steamPath, "config", "libraryfolders.vdf");
                }

                if (File.Exists(vdfPath))
                {
                    string content = File.ReadAllText(vdfPath);

                    // Parse VDF format: "path"		"D:\\SteamLibrary"
                    var pathMatches = Regex.Matches(content, @"""path""\s+""([^""]+)""");
                    foreach (Match match in pathMatches)
                    {
                        if (match.Groups.Count > 1)
                        {
                            string libraryPath = match.Groups[1].Value.Replace("\\\\", "\\");
                            string steamappsPath = Path.Combine(libraryPath, "steamapps");

                            if (Directory.Exists(steamappsPath) && !libraries.Contains(steamappsPath))
                                libraries.Add(steamappsPath);
                        }
                    }

                    // Also try old VDF format with numbered keys
                    var oldFormatMatches = Regex.Matches(content, @"""(\d+)""\s+""([^""]+)""");
                    foreach (Match match in oldFormatMatches)
                    {
                        if (match.Groups.Count > 2)
                        {
                            string libraryPath = match.Groups[2].Value.Replace("\\\\", "\\");
                            string steamappsPath = Path.Combine(libraryPath, "steamapps");

                            if (Directory.Exists(steamappsPath) && !libraries.Contains(steamappsPath))
                                libraries.Add(steamappsPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error parsing library folders: {ex.Message}", ex);
            }

            return libraries;
        }

        /// <summary>
        /// Scans a Steam library folder for installed games
        /// Reads appmanifest_*.acf files
        /// </summary>
        private List<GameInfo> ScanLibraryFolder(string libraryPath)
        {
            var games = new List<GameInfo>();

            try
            {
                // Find all appmanifest files
                var manifestFiles = Directory.GetFiles(libraryPath, "appmanifest_*.acf");

                foreach (var manifestFile in manifestFiles)
                {
                    try
                    {
                        var gameInfo = ParseAppManifest(manifestFile, libraryPath);
                        if (gameInfo != null)
                            games.Add(gameInfo);
                    }
                    catch (Exception ex)
                    {
                        log.Debug($"Error parsing {manifestFile}: {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error scanning library {libraryPath}: {ex.Message}", ex);
            }

            return games;
        }

        /// <summary>
        /// Parses an appmanifest_*.acf file to extract game information
        /// </summary>
        private GameInfo ParseAppManifest(string manifestPath, string libraryPath)
        {
            try
            {
                string content = File.ReadAllText(manifestPath);
                log.Debug($"Parsing manifest: {manifestPath}");
                log.Debug($"Manifest content: {content}");

                // Extract AppID from filename: appmanifest_<appid>.acf
                string filename = Path.GetFileNameWithoutExtension(manifestPath);
                int appId = int.Parse(filename.Replace("appmanifest_", ""));

                dynamic manifestData = AcfParser.Parse(content);

                // Parse VDF format
                string name = manifestData.AppState.name;
                string installDir = manifestData.AppState.installdir;
                string sizeOnDiskStr = manifestData.AppState.SizeOnDisk;
                string lastUpdatedStr = manifestData.AppState.LastUpdated;

                if (string.IsNullOrEmpty(name))
                    return null;

                // Build full install path
                string fullInstallDir = null;
                if (!string.IsNullOrEmpty(installDir))
                {
                    fullInstallDir = Path.Combine(libraryPath, "common", installDir);
                }

                // Parse size
                long sizeOnDisk = 0;
                if (!string.IsNullOrEmpty(sizeOnDiskStr))
                    long.TryParse(sizeOnDiskStr, out sizeOnDisk);

                // Parse last updated (Unix timestamp)
                DateTime? lastUpdated = null;
                if (!string.IsNullOrEmpty(lastUpdatedStr))
                {
                    if (long.TryParse(lastUpdatedStr, out long timestamp))
                    {
                        lastUpdated = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                    }
                }

                // Find the game's executable
                string exePath = FindGameExecutable(fullInstallDir, name, appId);

                // For icon, we'll use the exe itself or check Steam's icon cache
                string iconPath = exePath;
                if (string.IsNullOrEmpty(iconPath))
                {
                    // Try Steam's icon cache as fallback
                    iconPath = GetSteamIconFromCache(appId);
                }

                var shortcutDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
                string shortcutPath = FindOrCreateGameShortcut(appId.ToString(), name, shortcutDir, iconPath);

                return new GameInfo
                {
                    GameId = appId.ToString(),
                    Name = name,
                    InstallDir = installDir,
                    ExecutablePath = exePath,
                    IconPath = iconPath,
                    SizeOnDisk = sizeOnDisk,
                    LastUpdated = lastUpdated,
                    ShortcutPath = shortcutPath,
                    Platform = PlatformName,
                    Metadata = new Dictionary<string, string>
                    {
                        ["AppID"] = appId.ToString(),
                        ["LibraryPath"] = libraryPath ?? ""
                    }
                };
            }
            catch (Exception ex)
            {
                log.Error($"Error parsing manifest {manifestPath}: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Extracts a value from VDF format: "key"		"value"
        /// </summary>
        private string ExtractVdfValue(string content, string key)
        {
            try
            {
                // Pattern: "key"		"value" or "key"	"value"
                var pattern = $@"""{key}""\s+""([^""]*)""";
                var match = Regex.Match(content, pattern, RegexOptions.IgnoreCase);

                if (match.Success && match.Groups.Count > 1)
                    return match.Groups[1].Value;
            }
            catch
            {
                log.Error($"Error extracting VDF value for key {key} - ignoring");
            }

            return null;
        }

        /// <summary>
        /// Finds the main executable for a Steam game in its install directory
        /// </summary>
        private string FindGameExecutable(string installDir, string gameName, int appId)
        {
            try
            {
                if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
                    return null;

                // Get all .exe files in the install directory (top level only)
                var exeFiles = Directory.GetFiles(installDir, "*.exe", SearchOption.TopDirectoryOnly);

                if (exeFiles.Length == 0)
                    return null;

                // Common launcher executables to skip (prefer actual game exe)
                var launcherNames = new[] { "unins", "crash", "report", "launcher", "setup", "install", "updater", "uninstall" };

                // Filter out likely launchers/installers
                var gameExes = exeFiles.Where(exe =>
                {
                    string fileName = Path.GetFileNameWithoutExtension(exe).ToLower();
                    return !launcherNames.Any(launcher => fileName.Contains(launcher));
                }).ToList();

                if (gameExes.Count == 0)
                    gameExes = exeFiles.ToList(); // Use all if filtering removed everything

                // Try to find exe matching game name
                string sanitizedGameName = SanitizeForComparison(gameName);
                foreach (var exe in gameExes)
                {
                    string exeName = SanitizeForComparison(Path.GetFileNameWithoutExtension(exe));
                    if (exeName.Contains(sanitizedGameName) || sanitizedGameName.Contains(exeName))
                    {
                        return exe;
                    }
                }

                // If no match, return the first non-launcher exe
                if (gameExes.Count > 0)
                    return gameExes[0];

                // Last resort: return first exe
                return exeFiles.FirstOrDefault();
            }
            catch (Exception ex)
            {
                log.Error($"Error finding executable for {gameName}: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Gets the icon from Steam's icon cache
        /// </summary>
        private string GetSteamIconFromCache(int appId)
        {
            try
            {
                string steamPath = GetInstallPath();
                if (string.IsNullOrEmpty(steamPath))
                    return null;

                // Steam stores icons in appcache/librarycache
                string iconCachePath = Path.Combine(steamPath, "appcache", "librarycache");
                if (!Directory.Exists(iconCachePath))
                    return null;

                // Check for various icon formats Steam uses
                string[] iconFormats = new[]
                {
                    $"{appId}_icon.jpg",
                    $"{appId}_logo.png",
                    $"{appId}.ico"
                };

                foreach (var format in iconFormats)
                {
                    string iconPath = Path.Combine(iconCachePath, format);
                    if (File.Exists(iconPath))
                        return iconPath;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error getting icon from cache for AppID {appId}: {ex.Message}", ex);
            }

            return null;
        }

        /// <summary>
        /// Sanitizes a string for comparison (removes special chars, spaces, etc.)
        /// </summary>
        private string SanitizeForComparison(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Remove special characters and spaces, convert to lowercase
            return Regex.Replace(input, @"[^\w]", "").ToLower();
        }
    }
}
