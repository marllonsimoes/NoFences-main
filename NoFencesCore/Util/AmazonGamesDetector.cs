using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NoFences.Core.Util
{
    /// <summary>
    /// Detects installed Amazon Games (Prime Gaming) games
    /// </summary>
    public class AmazonGamesDetector : IGameStoreDetector
    {
        public string PlatformName => "Amazon Games";

        private const string LauncherProtocol = "amazon-games://play/{0}";

        public List<GameInfo> GetInstalledGames()
        {
            var games = new List<GameInfo>();

            try
            {
                // Amazon Games stores data in Local AppData
                string amazonGamesDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Amazon Games", "Data", "Games");

                if (!Directory.Exists(amazonGamesDataPath))
                {
                    Debug.WriteLine($"AmazonGamesDetector: Amazon Games data folder not found at {amazonGamesDataPath}");
                    return games;
                }

                // Each game has its own folder with a Fuel.json file
                var gameFolders = Directory.GetDirectories(amazonGamesDataPath);
                Debug.WriteLine($"AmazonGamesDetector: Found {gameFolders.Length} game folders");

                foreach (var gameFolder in gameFolders)
                {
                    try
                    {
                        string fuelJsonPath = Path.Combine(gameFolder, "Fuel.json");
                        if (File.Exists(fuelJsonPath))
                        {
                            var gameInfo = ParseFuelJson(fuelJsonPath, gameFolder);
                            if (gameInfo != null)
                                games.Add(gameInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"AmazonGamesDetector: Error parsing {gameFolder}: {ex.Message}");
                    }
                }

                Debug.WriteLine($"AmazonGamesDetector: Total {games.Count} Amazon games found");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AmazonGamesDetector: Error detecting Amazon games: {ex.Message}");
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
                // Check Local AppData for Amazon Games
                string amazonGamesPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Amazon Games", "App", "Amazon Games.exe");

                if (File.Exists(amazonGamesPath))
                    return Path.GetDirectoryName(amazonGamesPath);

                // Check default installation paths
                string[] defaultPaths = new[]
                {
                    @"C:\Program Files\Amazon Games\App",
                    @"C:\Program Files (x86)\Amazon Games\App"
                };

                foreach (var path in defaultPaths)
                {
                    string exePath = Path.Combine(path, "Amazon Games.exe");
                    if (File.Exists(exePath))
                        return path;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AmazonGamesDetector: Error finding Amazon install path: {ex.Message}");
            }

            return null;
        }

        public string CreateGameShortcut(string gameId, string gameName, string outputDirectory, string iconPath = null)
        {
            try
            {
                if (!Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);

                // Sanitize filename
                string safeGameName = string.Join("_", gameName.Split(Path.GetInvalidFileNameChars()));
                string shortcutPath = Path.Combine(outputDirectory, $"{safeGameName}.url");

                // Determine icon file
                string iconFile = iconPath;
                if (string.IsNullOrEmpty(iconFile))
                {
                    // Use Amazon Games icon as fallback
                    var launcherPath = GetInstallPath();
                    if (!string.IsNullOrEmpty(launcherPath))
                    {
                        iconFile = Path.Combine(launcherPath, "Amazon Games.exe");
                    }
                }

                // Create .url file with Amazon Games protocol
                string launchUrl = string.Format(LauncherProtocol, gameId);
                string urlContent = $@"[InternetShortcut]
URL={launchUrl}
IconIndex=0
IconFile={iconFile ?? ""}
";

                File.WriteAllText(shortcutPath, urlContent);
                Debug.WriteLine($"AmazonGamesDetector: Created shortcut at {shortcutPath}");
                return shortcutPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AmazonGamesDetector: Error creating shortcut for {gameName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parses a Fuel.json file to extract game information
        /// Uses regex instead of JSON library to avoid dependencies
        /// </summary>
        private GameInfo ParseFuelJson(string fuelJsonPath, string gameFolder)
        {
            try
            {
                string content = File.ReadAllText(fuelJsonPath);

                // Extract key fields using regex
                string productId = ExtractJsonValue(content, "Id");
                string productTitle = ExtractJsonValue(content, "ProductTitle");
                string installDirectory = ExtractJsonValue(content, "InstallDirectory");

                if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(productTitle))
                {
                    Debug.WriteLine($"AmazonGamesDetector: Missing required fields in {fuelJsonPath}");
                    return null;
                }

                // Verify install directory exists
                if (string.IsNullOrEmpty(installDirectory) || !Directory.Exists(installDirectory))
                {
                    Debug.WriteLine($"AmazonGamesDetector: Install directory not found for {productTitle}");
                    return null;
                }

                // Find executable
                string executablePath = FindGameExecutable(installDirectory, productTitle);
                string iconPath = executablePath;

                // Calculate install size
                long installSize = 0;
                try
                {
                    var dirInfo = new DirectoryInfo(installDirectory);
                    installSize = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                        .Sum(f => f.Length);
                }
                catch
                {
                    // Size calculation can fail
                }

                // Get last updated time from Fuel.json
                DateTime? lastUpdated = null;
                try
                {
                    lastUpdated = File.GetLastWriteTime(fuelJsonPath);
                }
                catch { }

                // Create shortcut
                string shortcutDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "NoFences", "AmazonShortcuts");

                string shortcutPath = CreateGameShortcut(productId, productTitle, shortcutDir, iconPath);

                return new GameInfo
                {
                    GameId = productId,
                    Name = productTitle,
                    InstallDir = installDirectory,
                    ExecutablePath = shortcutPath,
                    IconPath = iconPath,
                    SizeOnDisk = installSize,
                    LastUpdated = lastUpdated,
                    ShortcutPath = shortcutPath,
                    Platform = PlatformName,
                    Metadata = new Dictionary<string, string>
                    {
                        ["ProductId"] = productId,
                        ["FuelJsonPath"] = fuelJsonPath
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AmazonGamesDetector: Error parsing Fuel.json {fuelJsonPath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extracts a value from JSON content using regex
        /// </summary>
        private string ExtractJsonValue(string json, string key)
        {
            try
            {
                // Pattern: "key": "value" or "key":"value"
                var pattern = $@"""{key}""\s*:\s*""([^""]*)""";
                var match = Regex.Match(json, pattern, RegexOptions.IgnoreCase);

                if (match.Success && match.Groups.Count > 1)
                {
                    // Unescape JSON strings
                    return match.Groups[1].Value
                        .Replace("\\\\", "\\")
                        .Replace("\\/", "/")
                        .Replace("\\\"", "\"");
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return null;
        }

        /// <summary>
        /// Attempts to find the main executable in a game's install directory
        /// </summary>
        private string FindGameExecutable(string installDir, string gameName)
        {
            try
            {
                if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
                    return null;

                // Check common subdirectories
                string[] searchPaths = new[]
                {
                    installDir,
                    Path.Combine(installDir, "bin"),
                    Path.Combine(installDir, "Binaries"),
                    Path.Combine(installDir, "Binaries", "Win64"),
                    Path.Combine(installDir, "Game", "Binaries", "Win64")
                };

                foreach (var searchPath in searchPaths)
                {
                    if (!Directory.Exists(searchPath))
                        continue;

                    var exeFiles = Directory.GetFiles(searchPath, "*.exe", SearchOption.TopDirectoryOnly);
                    if (exeFiles.Length == 0)
                        continue;

                    // Filter out launchers, crash reporters, etc.
                    var launcherNames = new[] { "unins", "crash", "report", "launcher", "setup", "install", "updater", "uninstall", "amazon" };
                    var gameExes = exeFiles.Where(exe =>
                    {
                        string fileName = Path.GetFileNameWithoutExtension(exe).ToLower();
                        return !launcherNames.Any(launcher => fileName.Contains(launcher));
                    }).ToList();

                    if (gameExes.Count > 0)
                    {
                        // Try to match game name
                        string sanitizedGameName = SanitizeForComparison(gameName);
                        foreach (var exe in gameExes)
                        {
                            string exeName = SanitizeForComparison(Path.GetFileNameWithoutExtension(exe));
                            if (exeName.Contains(sanitizedGameName) || sanitizedGameName.Contains(exeName))
                                return exe;
                        }

                        // Return first non-launcher exe
                        return gameExes[0];
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AmazonGamesDetector: Error finding executable for {gameName}: {ex.Message}");
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
