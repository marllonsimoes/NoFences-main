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
    /// Detects installed GOG Galaxy games
    /// GOG stores game info in registry and local game folders
    /// </summary>
    public class GOGGalaxyDetector : IGameStoreDetector
    {

        private static readonly ILog log = LogManager.GetLogger(typeof(GOGGalaxyDetector));
        public string PlatformName => "GOG Galaxy";

        private const string RegistryPath = @"SOFTWARE\WOW6432Node\GOG.com\Games";
        private const string RegistryPathNoWow = @"SOFTWARE\GOG.com\Games";
        private const string LauncherProtocol = "goggalaxy://openGameView/{0}";

        public List<GameInfo> GetInstalledGames()
        {
            var games = new List<GameInfo>();

            try
            {
                // Scan HKLM registry
                games.AddRange(ScanGOGRegistry(Registry.LocalMachine, RegistryPath));
                games.AddRange(ScanGOGRegistry(Registry.LocalMachine, RegistryPathNoWow));

                // Remove duplicates
                var uniqueGames = games
                    .GroupBy(g => g.GameId)
                    .Select(g => g.First())
                    .ToList();

                log.Debug($"Total {uniqueGames.Count} GOG games found");
                return uniqueGames;
            }
            catch (Exception ex)
            {
                log.Error($"Error detecting GOG games: {ex.Message}", ex);
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
                // Check registry for GOG Galaxy
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\GalaxyClient\paths"))
                {
                    if (key != null)
                    {
                        var clientPath = key.GetValue("client") as string;
                        if (!string.IsNullOrEmpty(clientPath) && Directory.Exists(clientPath))
                            return clientPath;
                    }
                }

                // Check 32-bit registry
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\GOG.com\GalaxyClient\paths"))
                {
                    if (key != null)
                    {
                        var clientPath = key.GetValue("client") as string;
                        if (!string.IsNullOrEmpty(clientPath) && Directory.Exists(clientPath))
                            return clientPath;
                    }
                }

                // Check default paths
                string[] defaultPaths = new[]
                {
                    @"C:\Program Files (x86)\GOG Galaxy",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "GOG Galaxy")
                };

                foreach (var path in defaultPaths)
                {
                    if (Directory.Exists(path))
                        return path;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error finding GOG install path: {ex.Message}", ex);
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
                    // Use GOG Galaxy icon as fallback
                    var galaxyPath = GetInstallPath();
                    if (!string.IsNullOrEmpty(galaxyPath))
                    {
                        iconFile = Path.Combine(galaxyPath, "GalaxyClient.exe");
                    }
                }

                // Create .url file with GOG protocol
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
        /// Scans GOG registry for installed games
        /// </summary>
        private List<GameInfo> ScanGOGRegistry(RegistryKey root, string path)
        {
            var games = new List<GameInfo>();

            try
            {
                using (var gamesKey = root.OpenSubKey(path))
                {
                    if (gamesKey == null)
                        return games;

                    foreach (var gameKeyName in gamesKey.GetSubKeyNames())
                    {
                        try
                        {
                            using (var gameKey = gamesKey.OpenSubKey(gameKeyName))
                            {
                                if (gameKey == null)
                                    continue;

                                var gameInfo = ParseGOGRegistryEntry(gameKey, gameKeyName);
                                if (gameInfo != null)
                                    games.Add(gameInfo);
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error($"Error reading game key {gameKeyName}: {ex.Message}", ex);
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
        /// Parses a GOG registry entry to extract game information
        /// </summary>
        private GameInfo ParseGOGRegistryEntry(RegistryKey gameKey, string gameId)
        {
            try
            {
                var gameName = gameKey.GetValue("gameName") as string;
                var path = gameKey.GetValue("path") as string;
                var exe = gameKey.GetValue("exe") as string;
                var workingDir = gameKey.GetValue("workingDir") as string;

                if (string.IsNullOrEmpty(gameName))
                    return null;

                // Verify installation exists
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    log.Debug($"Install path not found for {gameName}");
                    return null;
                }

                // Build full executable path
                string executablePath = null;
                string iconPath = null;

                if (!string.IsNullOrEmpty(exe))
                {
                    // exe can be relative or absolute
                    if (Path.IsPathRooted(exe))
                    {
                        executablePath = exe;
                    }
                    else if (!string.IsNullOrEmpty(workingDir))
                    {
                        executablePath = Path.Combine(workingDir, exe);
                    }
                    else
                    {
                        executablePath = Path.Combine(path, exe);
                    }

                    if (File.Exists(executablePath))
                    {
                        iconPath = executablePath;
                    }
                    else
                    {
                        log.Debug($"Executable not found: {executablePath}");
                        executablePath = null;
                    }
                }

                // If no executable found, try to find one
                if (string.IsNullOrEmpty(executablePath))
                {
                    executablePath = FindGameExecutable(path, gameName);
                    if (!string.IsNullOrEmpty(executablePath))
                        iconPath = executablePath;
                }

                // Calculate install size
                long installSize = 0;
                try
                {
                    if (Directory.Exists(path))
                    {
                        var dirInfo = new DirectoryInfo(path);
                        installSize = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                            .Sum(f => f.Length);
                    }
                }
                catch
                {
                    // Size calculation can fail for large directories
                }

                // Create shortcut
                string shortcutDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

                string shortcutPath = FindOrCreateGameShortcut(gameId, gameName, shortcutDir, iconPath);

                return new GameInfo
                {
                    GameId = gameId,
                    Name = gameName,
                    InstallDir = path,
                    ExecutablePath = shortcutPath, // Use shortcut for launching
                    IconPath = iconPath, // Use game exe for icon
                    SizeOnDisk = installSize,
                    LastUpdated = null, // GOG doesn't store this in registry
                    ShortcutPath = shortcutPath,
                    Platform = PlatformName,
                    Metadata = new Dictionary<string, string>
                    {
                        ["GameId"] = gameId,
                        ["Exe"] = exe ?? "",
                        ["WorkingDir"] = workingDir ?? ""
                    }
                };
            }
            catch (Exception ex)
            {
                log.Error($"Error parsing registry entry for {gameId}: {ex.Message}", ex);
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

                // Get all executables in the install directory
                var exeFiles = Directory.GetFiles(installDir, "*.exe", SearchOption.TopDirectoryOnly);
                if (exeFiles.Length == 0)
                    return null;

                // Filter out launchers, uninstallers, etc.
                var launcherNames = new[] { "unins", "crash", "report", "launcher", "setup", "install", "updater", "uninstall", "galaxy", "support" };
                var gameExes = exeFiles.Where(exe =>
                {
                    string fileName = Path.GetFileNameWithoutExtension(exe).ToLower();
                    return !launcherNames.Any(launcher => fileName.Contains(launcher));
                }).ToList();

                if (gameExes.Count == 0)
                    gameExes = exeFiles.ToList(); // Use all if filtering removed everything

                // Try to match game name
                string sanitizedGameName = SanitizeForComparison(gameName);
                foreach (var exe in gameExes)
                {
                    string exeName = SanitizeForComparison(Path.GetFileNameWithoutExtension(exe));
                    if (exeName.Contains(sanitizedGameName) || sanitizedGameName.Contains(exeName))
                        return exe;
                }

                // Return first non-launcher exe
                return gameExes.FirstOrDefault();
            }
            catch (Exception ex)
            {
                log.Error($"Error finding executable for {gameName}: {ex.Message}", ex);
                return null;
            }
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
