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
    /// Detects installed EA App (formerly Origin) games
    /// </summary>
    public class EAAppDetector : IGameStoreDetector
    {

        private static readonly ILog log = LogManager.GetLogger(typeof(EAAppDetector));

        public string PlatformName => "EA App";

        private const string LocalContentPath = @"Electronic Arts\EA Desktop\";
        private const string RegistryPath = @"SOFTWARE\WOW6432Node\EA Games";
        private const string RegistryPathNoWow = @"SOFTWARE\EA Games";
        private const string LauncherProtocol = "origin2://game/launch?offerIds={0}";
        private const string LegacyProtocol = "origin://launchgame/{0}";

        public List<GameInfo> GetInstalledGames()
        {
            var games = new List<GameInfo>();

            try
            {
                // Scan registry for EA games
                games.AddRange(ScanEARegistry(Registry.LocalMachine, RegistryPath));
                games.AddRange(ScanEARegistry(Registry.LocalMachine, RegistryPathNoWow));

                // Remove duplicates
                var uniqueGames = games
                    .GroupBy(g => g.GameId)
                    .Select(g => g.First())
                    .ToList();

                log.Debug($"Total {uniqueGames.Count} EA games found");
                return uniqueGames;
            }
            catch (Exception ex)
            {
                log.Error($"Error detecting EA games: {ex.Message}", ex);
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
                // Check for EA Desktop (new app)
                string eaDesktopPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Electronic Arts", "EA Desktop", "EA Desktop.exe");

                if (File.Exists(eaDesktopPath))
                    return Path.GetDirectoryName(eaDesktopPath);

                // Check for Origin (legacy)
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Origin"))
                {
                    if (key != null)
                    {
                        var clientPath = key.GetValue("ClientPath") as string;
                        if (!string.IsNullOrEmpty(clientPath) && File.Exists(clientPath))
                            return Path.GetDirectoryName(clientPath);
                    }
                }

                // Check default Origin paths
                string[] defaultPaths = new[]
                {
                    @"C:\Program Files (x86)\Origin",
                    @"C:\Program Files\Origin",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Origin")
                };

                foreach (var path in defaultPaths)
                {
                    string originExe = Path.Combine(path, "Origin.exe");
                    if (File.Exists(originExe))
                        return path;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error finding EA install path: {ex.Message}", ex);
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
                string safeGameName = string.Join("  ", gameName.Split(Path.GetInvalidFileNameChars()));
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
                    // Use EA Desktop or Origin icon as fallback
                    var launcherPath = GetInstallPath();
                    if (!string.IsNullOrEmpty(launcherPath))
                    {
                        iconFile = Path.Combine(launcherPath, "EA Desktop.exe");
                        if (!File.Exists(iconFile))
                            iconFile = Path.Combine(launcherPath, "Origin.exe");
                    }
                }

                // Create .url file with EA protocol (use new protocol)
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
        /// Scans EA Games registry for installed games
        /// </summary>
        private List<GameInfo> ScanEARegistry(RegistryKey root, string path)
        {
            var games = new List<GameInfo>();

            try
            {
                using (var eaGamesKey = root.OpenSubKey(path))
                {
                    if (eaGamesKey == null)
                        return games;

                    foreach (var gameKeyName in eaGamesKey.GetSubKeyNames())
                    {
                        try
                        {
                            using (var gameKey = eaGamesKey.OpenSubKey(gameKeyName))
                            {
                                if (gameKey == null)
                                    continue;

                                var gameInfo = ParseEARegistryEntry(gameKey, gameKeyName);
                                if (gameInfo != null)
                                    games.Add(gameInfo);
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Debug($"Error reading game {gameKeyName}: {ex.Message}");
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
        /// Parses an EA Games registry entry to extract game information
        /// </summary>
        private GameInfo ParseEARegistryEntry(RegistryKey gameKey, string gameId)
        {
            try
            {
                var displayName = gameKey.GetValue("DisplayName") as string;
                var installDir = gameKey.GetValue("Install Dir") as string;
                var installLocation = gameKey.GetValue("InstallLocation") as string;

                // Use either Install Dir or InstallLocation
                string gamePath = installDir ?? installLocation;

                // Game name
                string gameName = displayName ?? gameId;
                if (string.IsNullOrEmpty(gameName))
                    return null;

                // Verify installation exists
                if (string.IsNullOrEmpty(gamePath) || !Directory.Exists(gamePath))
                {
                    log.Debug($"Install path not found for {gameName}");
                    return null;
                }

                // Find executable
                string executablePath = FindGameExecutable(gamePath, gameName);
                string iconPath = executablePath;

                // Calculate install size
                long installSize = 0;
                try
                {
                    var dirInfo = new DirectoryInfo(gamePath);
                    installSize = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                        .Sum(f => f.Length);
                }
                catch
                {
                    // Size calculation can fail
                }

                // Create shortcut
                string shortcutDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "NoFences", "EAShortcuts");

                string shortcutPath = FindOrCreateGameShortcut(gameId, gameName, shortcutDir, iconPath);

                return new GameInfo
                {
                    GameId = gameId,
                    Name = gameName,
                    InstallDir = gamePath,
                    ExecutablePath = shortcutPath,
                    IconPath = iconPath,
                    SizeOnDisk = installSize,
                    LastUpdated = null,
                    ShortcutPath = shortcutPath,
                    Platform = PlatformName,
                    Metadata = new Dictionary<string, string>
                    {
                        ["GameId"] = gameId,
                        ["DisplayName"] = displayName ?? ""
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

                    // Filter out launchers, EA overlay, etc.
                    var launcherNames = new[] { "unins", "crash", "report", "launcher", "setup", "install", "updater", "uninstall", "eadesktop", "origin", "ealink", "activation" };
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
