using log4net;
using Newtonsoft.Json;
using NoFences.Core.Util;
using NoFencesDataLayer.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NoFencesDataLayer.Services
{
    /// <summary>
    /// Detects installed Amazon Games (Prime Gaming) games.
    /// Session 11: Refactored to use repository pattern for data access.
    /// Business logic only - data access delegated to AmazonGamesRepository.
    /// </summary>
    public class AmazonGamesDetector : IGameStoreDetector
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AmazonGamesDetector));
        private readonly IAmazonGamesRepository repository;

        public string PlatformName => "Amazon Games";
        private const string LauncherProtocol = "amazon-games://play/{0}";

        /// <summary>
        /// Default constructor - creates own repository instance.
        /// </summary>
        public AmazonGamesDetector() : this(new AmazonGamesRepository())
        {
        }

        /// <summary>
        /// Constructor with dependency injection for testing.
        /// </summary>
        public AmazonGamesDetector(IAmazonGamesRepository repository)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public List<GameInfo> GetInstalledGames()
        {
            var games = new List<GameInfo>();

            try
            {
                // Session 11: Use repository for data access
                // Try repository (SQLite database) first - more reliable
                if (repository.IsAvailable())
                {
                    log.Debug("Using Amazon Games repository (SQLite database)");
                    games = repository.GetInstalledGames();

                    // Enhance games with executable paths, icons, and shortcuts
                    foreach (var game in games)
                    {
                        EnhanceGameInfo(game);
                    }

                    log.Info($"Found {games.Count} Amazon games from repository");
                    return games;
                }

                // Fallback to legacy Fuel.json method
                log.Info("Repository not available, falling back to Fuel.json parsing");
                games = GetInstalledGamesFromFuelJson();
            }
            catch (Exception ex)
            {
                log.Error($"Error detecting Amazon games: {ex.Message}", ex);
            }

            return games;
        }

        /// <summary>
        /// Enhances a GameInfo object with executable path, icon, and shortcut.
        /// Session 11: Separated enhancement logic from data access.
        /// </summary>
        private void EnhanceGameInfo(GameInfo game)
        {
            try
            {
                // Find executable in install directory
                if (!string.IsNullOrEmpty(game.InstallDir) && Directory.Exists(game.InstallDir))
                {
                    string executablePath = FindGameExecutable(game.InstallDir, game.Name);
                    if (!string.IsNullOrEmpty(executablePath))
                    {
                        game.IconPath = executablePath;
                    }
                }

                // Create shortcut for launching via Amazon Games protocol
                string shortcutDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
                string shortcutPath = FindOrCreateGameShortcut(game.GameId, game.Name, shortcutDir, game.IconPath);

                game.ExecutablePath = shortcutPath;
                game.ShortcutPath = shortcutPath;
            }
            catch (Exception ex)
            {
                log.Error($"Error enhancing game info for {game.Name}: {ex.Message}", ex);
            }
        }


        /// <summary>
        /// Legacy method: Reads games from individual Fuel.json files.
        /// Used as fallback when SQLite database is not available.
        /// </summary>
        private List<GameInfo> GetInstalledGamesFromFuelJson()
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
                    log.Debug($"Amazon Games data folder not found at {amazonGamesDataPath}");
                    return games;
                }

                // Each game has its own folder with a Fuel.json file
                var gameFolders = Directory.GetDirectories(amazonGamesDataPath);
                log.Debug($"Found {gameFolders.Length} game folders");

                foreach (var gameFolder in gameFolders)
                {
                    try
                    {
                        string fuelJsonPath = Path.Combine(gameFolder, "fuel.json");
                        if (File.Exists(fuelJsonPath))
                        {
                            var gameInfo = ParseFuelJson(fuelJsonPath, gameFolder);
                            if (gameInfo != null)
                                games.Add(gameInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Error parsing {gameFolder}: {ex.Message}", ex);
                    }
                }

                log.Debug($"Total {games.Count} Amazon games found from Fuel.json files");
            }
            catch (Exception ex)
            {
                log.Error($"Error reading Fuel.json files: {ex.Message}", ex);
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
                log.Error($"Error finding Amazon install path: {ex.Message}", ex);
            }

            return null;
        }

        public string FindOrCreateGameShortcut(string gameId, string gameName, string outputDirectory, string iconPath = null)
        {
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
                log.Debug($"Created shortcut at {shortcutPath}");
                return shortcutPath;
            }
            catch (Exception ex)
            {
                log.Error($"Error creating shortcut for {gameName}: {ex.Message}", ex);
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
                log.Debug($"Parsing manifest: {fuelJsonPath}");
                log.Debug($"Manifest content: {content}");

                dynamic manifestJson = JsonConvert.DeserializeObject(content);
                // Extract key fields using regex
                string productId = manifestJson.Id;
                string productTitle = manifestJson.ProductTitle;
                string installDirectory = manifestJson.InstallDirectory;

                if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(productTitle))
                {
                    log.Debug($"Missing required fields in {fuelJsonPath}");
                    return null;
                }

                // Verify install directory exists
                if (string.IsNullOrEmpty(installDirectory) || !Directory.Exists(installDirectory))
                {
                    log.Debug($"Install directory not found for {productTitle}");
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
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

                string shortcutPath = FindOrCreateGameShortcut(productId, productTitle, shortcutDir, iconPath);

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
                log.Error($"Error parsing Fuel.json {fuelJsonPath}: {ex.Message}", ex);
                return null;
            }
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
                log.Error($"Error finding executable for {gameName}: {ex.Message}", ex);
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
