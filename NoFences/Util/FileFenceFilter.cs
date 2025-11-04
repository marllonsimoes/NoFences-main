using log4net;
using NoFences.Core.Model;
using NoFences.Core.Util;
using NoFencesDataLayer.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NoFences.Util
{
    /// <summary>
    /// Utility class for filtering files in Files fences based on various filter types.
    /// Extracted from FilesFenceHandlerWpf to improve separation of concerns.
    /// Supports 5 filter types: None, Category, Extensions, Software, Pattern.
    /// </summary>
    public static class FileFenceFilter
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(FileFenceFilter));

        /// <summary>
        /// Result of filtering operation containing file paths and optional icon cache entries
        /// </summary>
        public class FilterResult
        {
            public List<string> FilePaths { get; set; } = new List<string>();
            public Dictionary<string, string> IconCache { get; set; } = new Dictionary<string, string>();
        }

        /// <summary>
        /// Applies the specified filter to get a list of files.
        /// </summary>
        /// <param name="filter">The file filter configuration</param>
        /// <param name="monitorPath">The path to monitor (for file-based filters)</param>
        /// <param name="manualItems">List of manually added items</param>
        /// <returns>FilterResult containing file paths and icon cache entries</returns>
        public static FilterResult ApplyFilter(FileFilter filter, string monitorPath, List<string> manualItems)
        {
            var result = new FilterResult();

            if (filter == null)
            {
                log.Warn("FileFenceFilter: Filter is null, returning empty result");
                return result;
            }

            // Handle software-based filtering
            if (filter.FilterType == FileFilterType.Software)
            {
                return ApplySoftwareFilter(filter);
            }

            // Handle file-based filtering (category, extensions, pattern, none)
            return ApplyFileBasedFilter(filter, monitorPath, manualItems);
        }

        /// <summary>
        /// Applies software category filter (e.g., Games, Browsers)
        /// Uses enhanced categorization from software catalog database when available.
        /// </summary>
        private static FilterResult ApplySoftwareFilter(FileFilter filter)
        {
            var result = new FilterResult();
            var installedSoftware = EnhancedInstalledAppsService.GetByCategoryEnhanced(filter.SoftwareCategory);
            log.Info($"FileFenceFilter: Found {installedSoftware.Count} software items for category {filter.SoftwareCategory}");

            foreach (var software in installedSoftware)
            {
                // Prioritize executable path, fall back to install location
                string path = null;

                if (!string.IsNullOrEmpty(software.ExecutablePath) && File.Exists(software.ExecutablePath))
                {
                    path = software.ExecutablePath;
                }
                else if (!string.IsNullOrEmpty(software.InstallLocation) && Directory.Exists(software.InstallLocation))
                {
                    path = software.InstallLocation;
                }

                if (path != null)
                {
                    result.FilePaths.Add(path);

                    // Cache icon path for software items (especially for Steam games)
                    if (!string.IsNullOrEmpty(software.IconPath))
                    {
                        result.IconCache[path] = software.IconPath;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Applies file-based filters (Category, Extensions, Pattern, None)
        /// </summary>
        private static FilterResult ApplyFileBasedFilter(FileFilter filter, string monitorPath, List<string> manualItems)
        {
            var result = new FilterResult();

            // Handle folder monitoring path
            if (!string.IsNullOrEmpty(monitorPath))
            {
                if (!Directory.Exists(monitorPath))
                {
                    log.Warn($"FileFenceFilter: Monitor path does not exist: {monitorPath}");
                    return result;
                }

                SearchOption searchOption = filter.IncludeSubfolders
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                try
                {
                    var allFiles = Directory.GetFileSystemEntries(monitorPath, "*", searchOption);

                    foreach (var filePath in allFiles)
                    {
                        if (filter.MatchesFile(filePath))
                        {
                            result.FilePaths.Add(filePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"FileFenceFilter: Error scanning directory {monitorPath}: {ex.Message}");
                }
            }
            // Handle manually added items
            else if (manualItems?.Count > 0)
            {
                foreach (var item in manualItems)
                {
                    if ((Directory.Exists(item) || File.Exists(item)) && filter.MatchesFile(item))
                    {
                        result.FilePaths.Add(item);
                    }
                }
            }

            log.Info($"FileFenceFilter: Smart filter returned {result.FilePaths.Count} items");
            return result;
        }

        /// <summary>
        /// Legacy pattern matching for backward compatibility.
        /// Checks if a path matches a given pattern (filename, directory name, or regex).
        /// </summary>
        public static bool MatchesPattern(string path, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return true;

            string filename = Path.GetFileNameWithoutExtension(path);
            var dirInfo = new DirectoryInfo(path);
            string dirName = dirInfo.Exists ? dirInfo.Name : null;

            bool patternIsFileName = filename.Equals(pattern, StringComparison.OrdinalIgnoreCase);
            bool patternIsDirName = dirName != null && dirName.Equals(pattern, StringComparison.OrdinalIgnoreCase);

            // Check shortcut info
            ShortcutInfo shortcutInfo = FileUtils.GetShortcutInfo(path);
            bool patternIsShortcutName = false;
            bool patternIsUrl = false;
            if (shortcutInfo != null)
            {
                patternIsShortcutName = shortcutInfo.Name.Equals(pattern, StringComparison.OrdinalIgnoreCase);
                if (shortcutInfo.Url != null)
                {
                    try
                    {
                        patternIsUrl = Regex.IsMatch(shortcutInfo.Url, pattern, RegexOptions.IgnoreCase);
                    }
                    catch (Exception ex)
                    {
                        log.Warn($"FileFenceFilter: Invalid regex pattern '{pattern}': {ex.Message}");
                    }
                }
            }

            // Try regex match on full path
            bool regexMatch = false;
            try
            {
                regexMatch = Regex.IsMatch(path, pattern, RegexOptions.IgnoreCase);
            }
            catch (Exception ex)
            {
                log.Warn($"FileFenceFilter: Invalid regex pattern '{pattern}': {ex.Message}");
            }

            return regexMatch || patternIsFileName || patternIsDirName ||
                   patternIsShortcutName || patternIsUrl;
        }
    }
}
