using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NoFences.Core.Util
{
    /// <summary>
    /// Detects installed Epic Games Store games via manifest files
    /// </summary>
    public class EpicGamesStoreDetector : IGameStoreDetector
    {
        public string PlatformName => "Epic Games Store";

        private const string ManifestFolder = @"C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests";
        private const string LauncherProtocol = "com.epicgames.launcher://apps/{0}?action=launch&silent=true";

        public List<GameInfo> GetInstalledGames()
        {
            var games = new List<GameInfo>();

            try
            {
                if (!Directory.Exists(ManifestFolder))
                {
                    Debug.WriteLine($"EpicGamesStoreDetector: Manifest folder not found at {ManifestFolder}");
                    return games;
                }

                Debug.WriteLine($"EpicGamesStoreDetector: Scanning {ManifestFolder}");

                // Epic uses .item files for game manifests
                var manifestFiles = Directory.GetFiles(ManifestFolder, "*.item");
                Debug.WriteLine($"EpicGamesStoreDetector: Found {manifestFiles.Length} manifest files");

                foreach (var manifestFile in manifestFiles)
                {
                    try
                    {
                        var gameInfo = ParseManifest(manifestFile);
                        if (gameInfo != null)
                            games.Add(gameInfo);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"EpicGamesStoreDetector: Error parsing {manifestFile}: {ex.Message}");
                    }
                }

                Debug.WriteLine($"EpicGamesStoreDetector: Total {games.Count} Epic games found");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EpicGamesStoreDetector: Error detecting Epic games: {ex.Message}");
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
                // Check registry for Epic Games Launcher
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Epic Games\EpicGamesLauncher"))
                {
                    if (key != null)
                    {
                        var installPath = key.GetValue("AppDataPath") as string;
                        if (!string.IsNullOrEmpty(installPath))
                            return installPath;
                    }
                }

                // Try alternate registry location
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Epic Games\EOS"))
                {
                    if (key != null)
                    {
                        var installPath = key.GetValue("ModSdkMetadataDir") as string;
                        if (!string.IsNullOrEmpty(installPath))
                        {
                            // Navigate up to launcher directory
                            var launcherPath = Path.GetDirectoryName(Path.GetDirectoryName(installPath));
                            return launcherPath;
                        }
                    }
                }

                // Check default installation paths
                string[] defaultPaths = new[]
                {
                    @"C:\Program Files\Epic Games\Launcher",
                    @"C:\Program Files (x86)\Epic Games\Launcher"
                };

                foreach (var path in defaultPaths)
                {
                    if (Directory.Exists(path))
                        return path;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EpicGamesStoreDetector: Error finding Epic install path: {ex.Message}");
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
                    // Use Epic launcher icon as fallback
                    var launcherPath = GetInstallPath();
                    if (!string.IsNullOrEmpty(launcherPath))
                    {
                        iconFile = Path.Combine(launcherPath, "Portal", "Binaries", "Win64", "EpicGamesLauncher.exe");
                        if (!File.Exists(iconFile))
                            iconFile = Path.Combine(launcherPath, "Launcher", "Portal", "Binaries", "Win64", "EpicGamesLauncher.exe");
                    }
                }

                // Create .url file with Epic protocol
                string launchUrl = string.Format(LauncherProtocol, gameId);
                string urlContent = $@"[InternetShortcut]
URL={launchUrl}
IconIndex=0
IconFile={iconFile ?? ""}
";

                File.WriteAllText(shortcutPath, urlContent);
                Debug.WriteLine($"EpicGamesStoreDetector: Created shortcut at {shortcutPath}");
                return shortcutPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EpicGamesStoreDetector: Error creating shortcut for {gameName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parses an Epic Games manifest file (.item)
        /// Format is JSON but we'll use regex for simplicity (no JSON dependency)
        /// </summary>
        private GameInfo ParseManifest(string manifestPath)
        {
            try
            {
                string content = File.ReadAllText(manifestPath);

                // Extract key fields using regex (Epic manifests are JSON)
                string appName = ExtractJsonValue(content, "AppName");
                string displayName = ExtractJsonValue(content, "DisplayName");
                string installLocation = ExtractJsonValue(content, "InstallLocation");
                string launchExecutable = ExtractJsonValue(content, "LaunchExecutable");
                string installSizeStr = ExtractJsonValue(content, "InstallSize");

                if (string.IsNullOrEmpty(appName) || string.IsNullOrEmpty(displayName))
                    return null;

                // Verify install location exists
                if (string.IsNullOrEmpty(installLocation) || !Directory.Exists(installLocation))
                {
                    Debug.WriteLine($"EpicGamesStoreDetector: Install location not found for {displayName}");
                    return null;
                }

                // Build full executable path
                string executablePath = null;
                string iconPath = null;
                if (!string.IsNullOrEmpty(launchExecutable))
                {
                    executablePath = Path.Combine(installLocation, launchExecutable);
                    if (File.Exists(executablePath))
                    {
                        iconPath = executablePath; // Use game exe for icon
                    }
                    else
                    {
                        Debug.WriteLine($"EpicGamesStoreDetector: Executable not found: {executablePath}");
                        executablePath = null;
                    }
                }

                // If no executable found, try to find one
                if (string.IsNullOrEmpty(executablePath))
                {
                    executablePath = FindGameExecutable(installLocation, displayName);
                    if (!string.IsNullOrEmpty(executablePath))
                        iconPath = executablePath;
                }

                // Parse install size
                long installSize = 0;
                if (!string.IsNullOrEmpty(installSizeStr))
                    long.TryParse(installSizeStr, out installSize);

                // Create shortcut
                string shortcutDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "NoFences", "EpicShortcuts");

                string shortcutPath = CreateGameShortcut(appName, displayName, shortcutDir, iconPath);

                return new GameInfo
                {
                    GameId = appName,
                    Name = displayName,
                    InstallDir = installLocation,
                    ExecutablePath = shortcutPath, // Use shortcut as executable
                    IconPath = iconPath, // Use game exe for icon extraction
                    SizeOnDisk = installSize,
                    LastUpdated = File.GetLastWriteTime(manifestPath),
                    ShortcutPath = shortcutPath,
                    Platform = PlatformName,
                    Metadata = new Dictionary<string, string>
                    {
                        ["AppName"] = appName,
                        ["LaunchExecutable"] = launchExecutable ?? ""
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EpicGamesStoreDetector: Error parsing manifest {manifestPath}: {ex.Message}");
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
                    return match.Groups[1].Value.Replace("\\\\", "\\");
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

                // Look for executables in common subdirectories
                string[] searchPaths = new[]
                {
                    installDir,
                    Path.Combine(installDir, "Binaries"),
                    Path.Combine(installDir, "Binaries", "Win64"),
                    Path.Combine(installDir, "Binaries", "Win32"),
                    Path.Combine(installDir, "Bin"),
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
                    var launcherNames = new[] { "unins", "crash", "report", "launcher", "setup", "install", "updater", "uninstall", "easyanticheat", "battleye" };
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
                Debug.WriteLine($"EpicGamesStoreDetector: Error finding executable for {gameName}: {ex.Message}");
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
