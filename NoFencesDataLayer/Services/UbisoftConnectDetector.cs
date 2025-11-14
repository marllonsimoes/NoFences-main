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
    /// Detects installed Ubisoft Connect (formerly Uplay) games
    /// </summary>
    public class UbisoftConnectDetector : IGameStoreDetector
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(UbisoftConnectDetector));

        public string PlatformName => "Ubisoft Connect";

        private const string RegistryPath = @"SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs";
        private const string RegistryPathNoWow = @"SOFTWARE\Ubisoft\Launcher\Installs";
        private const string LauncherProtocol = "uplay://launch/{0}";

        public List<GameInfo> GetInstalledGames()
        {
            var games = new List<GameInfo>();

            try
            {
                // Scan registry for installed games
                games.AddRange(ScanUbisoftRegistry(Registry.LocalMachine, RegistryPath));
                games.AddRange(ScanUbisoftRegistry(Registry.LocalMachine, RegistryPathNoWow));

                // Remove duplicates
                var uniqueGames = games
                    .GroupBy(g => g.GameId)
                    .Select(g => g.First())
                    .ToList();

                log.Debug($"Total {uniqueGames.Count} Ubisoft games found");
                return uniqueGames;
            }
            catch (Exception ex)
            {
                log.Error($"Error detecting Ubisoft games: {ex.Message}", ex);
                return games;
            }
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
                // Check registry for Ubisoft Connect
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Ubisoft\Launcher"))
                {
                    if (key != null)
                    {
                        var installDir = key.GetValue("InstallDir") as string;
                        if (!string.IsNullOrEmpty(installDir) && Directory.Exists(installDir))
                            return installDir;
                    }
                }

                // Check 32-bit registry
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Ubisoft\Launcher"))
                {
                    if (key != null)
                    {
                        var installDir = key.GetValue("InstallDir") as string;
                        if (!string.IsNullOrEmpty(installDir) && Directory.Exists(installDir))
                            return installDir;
                    }
                }

                // Check default paths
                string[] defaultPaths = new[]
                {
                    @"C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Ubisoft", "Ubisoft Game Launcher")
                };

                foreach (var path in defaultPaths)
                {
                    if (Directory.Exists(path))
                        return path;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error finding Ubisoft install path: {ex.Message}", ex);
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
                    // Use Ubisoft Connect icon as fallback
                    var launcherPath = GetInstallPath();
                    if (!string.IsNullOrEmpty(launcherPath))
                    {
                        iconFile = Path.Combine(launcherPath, "UbisoftConnect.exe");
                        if (!File.Exists(iconFile))
                            iconFile = Path.Combine(launcherPath, "Uplay.exe"); // Legacy name
                    }
                }

                // Create .url file with Ubisoft protocol
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
        /// Scans Ubisoft registry for installed games
        /// </summary>
        private List<GameInfo> ScanUbisoftRegistry(RegistryKey root, string path)
        {
            var games = new List<GameInfo>();

            try
            {
                using (var installsKey = root.OpenSubKey(path))
                {
                    if (installsKey == null)
                        return games;

                    foreach (var gameId in installsKey.GetSubKeyNames())
                    {
                        try
                        {
                            using (var gameKey = installsKey.OpenSubKey(gameId))
                            {
                                if (gameKey == null)
                                    continue;

                                var gameInfo = ParseUbisoftRegistryEntry(gameKey, gameId);
                                if (gameInfo != null)
                                    games.Add(gameInfo);
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Debug($"Error reading game {gameId}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error scanning registry {path}: {ex.Message}", ex);
            }

            return games;
        }

        /// <summary>
        /// Parses a Ubisoft registry entry to extract game information
        /// </summary>
        private GameInfo ParseUbisoftRegistryEntry(RegistryKey gameKey, string gameId)
        {
            try
            {
                var installDir = gameKey.GetValue("InstallDir") as string;

                // Verify installation exists
                if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
                {
                    log.Debug($"Install dir not found for game {gameId}");
                    return null;
                }

                // Extract game name from install directory or use game ID
                string gameName = Path.GetFileName(installDir.TrimEnd('\\', '/'));
                if (string.IsNullOrEmpty(gameName))
                    gameName = $"Ubisoft Game {gameId}";

                // Try to clean up the name (remove version numbers, etc.)
                gameName = CleanGameName(gameName);

                // Find executable
                string executablePath = FindGameExecutable(installDir, gameName);
                string iconPath = executablePath;

                // Calculate install size
                long installSize = 0;
                try
                {
                    var dirInfo = new DirectoryInfo(installDir);
                    installSize = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                        .Sum(f => f.Length);
                }
                catch
                {
                    // Size calculation can fail
                }

                // Create shortcut
                string shortcutDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
                string shortcutPath = FindOrCreateGameShortcut(gameId, gameName, shortcutDir, iconPath);

                return new GameInfo
                {
                    GameId = gameId,
                    Name = gameName,
                    InstallDir = installDir,
                    ExecutablePath = shortcutPath,
                    IconPath = iconPath,
                    SizeOnDisk = installSize,
                    LastUpdated = null,
                    ShortcutPath = shortcutPath,
                    Platform = PlatformName,
                    Metadata = new Dictionary<string, string>
                    {
                        ["GameId"] = gameId
                    }
                };
            }
            catch (Exception ex)
            {
                log.Error($"Error parsing entry for {gameId}: {ex.Message}", ex);
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

                // Check common subdirectories first
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

                    // Filter out launchers, installers, Ubisoft overlay
                    var launcherNames = new[] { "unins", "crash", "report", "launcher", "setup", "install", "updater", "uninstall", "ubisoft", "uplay", "overlay" };
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
        /// Cleans up game name (removes version numbers, extra info)
        /// </summary>
        private string CleanGameName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            // Remove common suffixes
            name = Regex.Replace(name, @"\s*\(.*?\)\s*", " "); // Remove parentheses content
            name = Regex.Replace(name, @"\s*\[.*?\]\s*", " "); // Remove brackets content
            name = Regex.Replace(name, @"\s+v?\d+(\.\d+)*\s*$", ""); // Remove version numbers at end

            return name.Trim();
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

        /// <summary>
        /// Checks if the given path contains a game for this platform.
        /// Currently only implemented for EA App detector.
        /// </summary>
        public bool CanDetectFromPath(string installPath)
        {
            // Pattern-based detection not implemented for this platform
            return false;
        }

        /// <summary>
        /// Extracts game information from installation path.
        /// Currently only implemented for EA App detector.
        /// </summary>
        public GameInfo GetGameInfoFromPath(string installPath)
        {
            // Pattern-based detection not implemented for this platform
            return null;
        }
    }
}
