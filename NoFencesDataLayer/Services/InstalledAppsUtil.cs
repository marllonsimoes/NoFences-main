using log4net;
using Microsoft.Win32;
using NoFences.Core.Model;
using NoFences.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NoFencesDataLayer.Services
{
    /// <summary>
    /// Utility for detecting and categorizing installed software on Windows
    /// </summary>
    public static class InstalledAppsUtil
    {

        private static readonly ILog log = LogManager.GetLogger(typeof(InstalledAppsUtil));

        // Registry paths for installed software
        private static readonly string[] RegistryPaths = new[]
        {
            // Machine-wide installs (64-bit)
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            // Machine-wide installs (32-bit on 64-bit Windows)
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        private static readonly string[] UserRegistryPaths = new[]
        {
            // Current user installs
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        /// <summary>
        /// Gets all installed software from registry (both machine and user installs)
        /// Includes Steam games detected via VDF parsing
        /// </summary>
        public static List<InstalledSoftware> GetAllInstalled()
        {
            var software = new List<InstalledSoftware>();

            // Scan HKLM (machine-wide installs)
            foreach (var path in RegistryPaths)
            {
                software.AddRange(ScanRegistryKey(Registry.LocalMachine, path, path.Contains("WOW6432Node")));
            }

            // Scan HKCU (user installs)
            foreach (var path in UserRegistryPaths)
            {
                software.AddRange(ScanRegistryKey(Registry.CurrentUser, path, false));
            }

            log.Info($"Found {software.Count} software entries from registry");
            foreach (var soft in software) {
                log.Debug($"\t - {soft}");
            }

            // Pattern-based detector claiming: Allow specialized detectors to enhance registry entries
            // This enables platform-specific detection (e.g., EA App's __Installer/installerdata.xml)
            var patternDetectors = new List<IGameStoreDetector>
            {
                new EAAppDetector(),
                new SteamStoreDetector(),
                new EpicGamesStoreDetector(),
                new GOGGalaxyDetector(),
                new UbisoftConnectDetector(),
                new AmazonGamesDetector()
            };

            log.Info($"Checking {software.Count} registry entries against pattern detectors");
            for (int i = 0; i < software.Count; i++)
            {
                var softwareEntry = software[i];

                // Skip entries without install location
                if (string.IsNullOrEmpty(softwareEntry.InstallLocation))
                    continue;

                // Check if any detector can identify this software by its installation structure
                foreach (var detector in patternDetectors)
                {
                    try
                    {
                        if (detector.CanDetectFromPath(softwareEntry.InstallLocation))
                        {
                            log.Debug($"Pattern match: {detector.PlatformName} detected at {softwareEntry.InstallLocation}");

                            var gameInfo = detector.GetGameInfoFromPath(softwareEntry.InstallLocation);
                            if (gameInfo != null)
                            {
                                // Convert GameInfo to InstalledSoftware, preserving useful registry data
                                var enhancedEntry = new InstalledSoftware
                                {
                                    Name = gameInfo.Name,
                                    Publisher = detector.PlatformName,
                                    InstallLocation = gameInfo.InstallDir,
                                    ExecutablePath = gameInfo.ExecutablePath ?? gameInfo.ShortcutPath,
                                    IconPath = gameInfo.IconPath,
                                    Category = SoftwareCategory.Games, // Keep enum for backward compatibility
                                    Version = softwareEntry.Version, // Preserve registry version if available
                                    InstallDate = gameInfo.LastUpdated ?? softwareEntry.InstallDate,
                                    RegistryKey = $"{detector.PlatformName}:{gameInfo.GameId}",
                                    IsWow64 = softwareEntry.IsWow64,
                                    Source = detector.PlatformName
                                };

                                // Replace generic registry entry with enhanced game info
                                software[i] = enhancedEntry;
                                log.Info($"Enhanced registry entry '{softwareEntry.Name}' with {detector.PlatformName} data");
                                break; // First detector that matches wins
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Debug($"Error checking {detector.PlatformName} pattern at {softwareEntry.InstallLocation}: {ex.Message}");
                    }
                }
            }

            // Add games from all supported game stores
            var games = GetAllGames();
            log.Info($"Found {games.Count} games entries from registry");
            foreach (var game in games)
            {
                log.Debug($"\t - {game}");
            }

            software.AddRange(games);
            software.Sort((a, b) => a.Name.CompareTo(b.Name));

            // Remove duplicates with priority-based deduplication
            // Priority: Specialized detectors (Steam, GOG, etc.) > Registry
            // This ensures game metadata from specialized detectors isn't overwritten by generic registry data
            var uniqueSoftware = software
                .GroupBy(s => s.Name?.ToLower())
                .Select(g => {
                    if (g.Count() > 1)
                    {
                        log.Debug($"Found {g.Count()} duplicates for '{g.First().Name}' - selecting best source");

                        // Priority 1: Entries from specialized detectors (non-Registry source)
                        var specializedEntry = g.FirstOrDefault(item =>
                            !string.IsNullOrEmpty(item.Source) &&
                            item.Source != "Registry");
                        if (specializedEntry != null)
                        {
                            log.Debug($"  → Using specialized detector source: {specializedEntry.Source}");
                            return specializedEntry;
                        }

                        // Priority 2: Entries with non-Other category (better categorization)
                        var categorizedEntry = g.FirstOrDefault(item => item.Category != SoftwareCategory.Other);
                        if (categorizedEntry != null)
                        {
                            log.Debug($"  → Using categorized entry: {categorizedEntry.Category}");
                            return categorizedEntry;
                        }

                        // Priority 3: First entry as fallback
                        log.Debug($"  → Using first entry as fallback");
                        return g.First();
                    }
                    return g.First();
                })
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .OrderBy(s => s.Name)
                .ToList();

            log.Info($"Found {uniqueSoftware.Count} unique software installations");

            return uniqueSoftware;
        }

        /// <summary>
        /// Gets installed software filtered by category
        /// </summary>
        public static List<InstalledSoftware> GetByCategory(SoftwareCategory category)
        {
            var allSoftware = GetAllInstalled();

            if (category == SoftwareCategory.All)
                return allSoftware;

            return allSoftware.Where(s => s.Category == category).ToList();
        }

        /// <summary>
        /// Scans a specific registry key for installed software
        /// </summary>
        private static List<InstalledSoftware> ScanRegistryKey(RegistryKey root, string path, bool isWow64)
        {
            var software = new List<InstalledSoftware>();

            try
            {
                using (RegistryKey key = root.OpenSubKey(path))
                {
                    if (key == null)
                        return software;

                    foreach (string subkeyName in key.GetSubKeyNames())
                    {
                        try
                        {
                            using (RegistryKey subkey = key.OpenSubKey(subkeyName))
                            {
                                if (subkey == null)
                                    continue;

                                var app = ExtractSoftwareInfo(subkey, subkeyName, isWow64);
                                if (app != null && IsValidSoftware(app))
                                {
                                    software.Add(app);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error($"Error reading subkey {subkeyName}: {ex.Message}", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error scanning registry path {path}: {ex.Message}", ex);
            }

            return software;
        }

        /// <summary>
        /// Extracts software information from a registry key
        /// </summary>
        private static InstalledSoftware ExtractSoftwareInfo(RegistryKey key, string keyName, bool isWow64)
        {
            try
            {
                var name = key.GetValue("DisplayName") as string;
                if (string.IsNullOrWhiteSpace(name))
                    return null;

                var publisher = key.GetValue("Publisher") as string;
                var installLocation = key.GetValue("InstallLocation") as string;

                var app = new InstalledSoftware
                {
                    Name = name?.Trim(),
                    Publisher = publisher?.Trim(),
                    Version = key.GetValue("DisplayVersion") as string,
                    InstallLocation = installLocation?.Trim(),
                    UninstallString = key.GetValue("UninstallString") as string,
                    IconPath = key.GetValue("DisplayIcon") as string,
                    RegistryKey = keyName,
                    IsWow64 = isWow64,
                    Source = "Registry"
                };

                // Try to parse install date
                var installDate = key.GetValue("InstallDate") as string;
                if (!string.IsNullOrEmpty(installDate) && installDate.Length == 8)
                {
                    // Format is usually YYYYMMDD
                    if (DateTime.TryParseExact(installDate, "yyyyMMdd", null,
                        System.Globalization.DateTimeStyles.None, out DateTime date))
                    {
                        app.InstallDate = date;
                    }
                }

                // Try to find executable path
                if (!string.IsNullOrEmpty(app.InstallLocation))
                {
                    app.ExecutablePath = FindExecutableInDirectory(app.InstallLocation, app.Name);
                }

                app.Category = SoftwareCategorizer.Categorize(app.Name, app.Publisher, app.InstallLocation);

                return app;
            }
            catch (Exception ex)
            {
                log.Error($"Error extracting software info: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Validates that the software entry has meaningful data
        /// </summary>
        private static bool IsValidSoftware(InstalledSoftware app)
        {
            // Filter out system components, updates, and garbage entries
            if (string.IsNullOrWhiteSpace(app.Name))
                return false;

            var lowerName = app.Name.ToLower();

            // Filter out Windows updates and hotfixes
            if (lowerName.Contains("hotfix") || lowerName.Contains("update for") ||
                lowerName.Contains("security update") || lowerName.StartsWith("kb"))
                return false;

            // Filter out Visual C++ redistributables (too many)
            if (lowerName.Contains("microsoft visual c++") && lowerName.Contains("redistributable"))
                return false;

            // Filter out .NET Framework updates
            if (lowerName.Contains("microsoft .net") && lowerName.Contains("update"))
                return false;

            // No need to filter game store paths here
            // Priority-based deduplication in GetAllInstalled() handles this elegantly:
            // Specialized detectors (Steam, GOG, etc.) take precedence over Registry entries
            // This scales better - no hardcoded paths needed when adding new detectors

            return true;
        }

        /// <summary>
        /// Attempts to find the main executable in an installation directory
        /// </summary>
        private static string FindExecutableInDirectory(string directory, string appName)
        {
            try
            {
                if (string.IsNullOrEmpty(directory) || !System.IO.Directory.Exists(directory))
                    return null;

                // Look for exe with similar name
                var exeFiles = System.IO.Directory.GetFiles(directory, "*.exe", System.IO.SearchOption.TopDirectoryOnly);

                // Try to find exe with matching name
                var simpleName = System.IO.Path.GetFileNameWithoutExtension(appName)?.ToLower();
                if (!string.IsNullOrEmpty(simpleName))
                {
                    var match = exeFiles.FirstOrDefault(f =>
                        System.IO.Path.GetFileNameWithoutExtension(f)?.ToLower() == simpleName);
                    if (match != null)
                        return match;
                }

                // Return first exe if any
                return exeFiles.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets games from all supported game store platforms
        /// </summary>
        private static List<InstalledSoftware> GetAllGames()
        {
            var allGames = new List<InstalledSoftware>();

            // Create list of all game store detectors
            var detectors = new List<IGameStoreDetector>
            {
                new SteamStoreDetector(),
                new EpicGamesStoreDetector(),
                new GOGGalaxyDetector(),
                new UbisoftConnectDetector(),
                new EAAppDetector(),
                new AmazonGamesDetector()
            };

            foreach (var detector in detectors)
            {
                try
                {
                    if (!detector.IsInstalled())
                    {
                        log.Debug($"{detector.PlatformName} not installed, skipping");
                        continue;
                    }

                    var games = GetGamesFromStore(detector);
                    allGames.AddRange(games);
                    log.Debug($"Added {games.Count} games from {detector.PlatformName}");
                }
                catch (Exception ex)
                {
                    log.Error($"Error getting games from {detector.PlatformName}: {ex.Message}", ex);
                }
            }

            return allGames;
        }

        /// <summary>
        /// Gets games from a specific game store detector
        /// Converts GameInfo to InstalledSoftware format
        /// </summary>
        private static List<InstalledSoftware> GetGamesFromStore(IGameStoreDetector detector)
        {
            var softwareList = new List<InstalledSoftware>();

            try
            {
                var games = detector.GetInstalledGames();

                foreach (var game in games)
                {
                    var software = new InstalledSoftware
                    {
                        Name = game.Name,
                        Publisher = detector.PlatformName,
                        InstallLocation = game.InstallDir,
                        ExecutablePath = game.ExecutablePath ?? game.ShortcutPath,
                        IconPath = game.IconPath,
                        Category = SoftwareCategory.Games,
                        Version = null,
                        InstallDate = game.LastUpdated,
                        RegistryKey = $"{detector.PlatformName}:{game.GameId}",
                        IsWow64 = false,
                        Source = detector.PlatformName
                    };

                    softwareList.Add(software);
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error processing games from {detector.PlatformName}: {ex.Message}", ex);
            }

            return softwareList;
        }
    }
}
