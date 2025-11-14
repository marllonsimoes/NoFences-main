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
        /// Result of filtering operation containing InstalledSoftware objects.
        /// Preserves all metadata instead of just file paths.
        /// </summary>
        public class FilterResult
        {
            /// <summary>
            /// Software items from database (with full metadata)
            /// </summary>
            public List<InstalledSoftware> SoftwareItems { get; set; } = new List<InstalledSoftware>();

            /// <summary>
            /// File/folder items from local filesystem (with basic metadata)
            /// </summary>
            public List<InstalledSoftware> FileItems { get; set; } = new List<InstalledSoftware>();

            /// <summary>
            /// Gets all items combined (software + files)
            /// </summary>
            public List<InstalledSoftware> AllItems
            {
                get
                {
                    var combined = new List<InstalledSoftware>();
                    combined.AddRange(SoftwareItems);
                    combined.AddRange(FileItems);
                    return combined;
                }
            }
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
        /// Updated to query database via InstalledSoftwareService (hybrid architecture).
        /// Supports source-based categorization with subfilters (Q3 answer priority 1, Q4 answer: same type with subfilter).
        /// Enhanced to return full InstalledSoftware objects instead of just paths.
        /// Falls back to in-memory service if database is empty or unavailable.
        /// </summary>
        private static FilterResult ApplySoftwareFilter(FileFilter filter)
        {
            var result = new FilterResult();
            List<InstalledSoftware> installedSoftware = null;

            try
            {
                // Try to query database via InstalledSoftwareService (hybrid architecture)
                var service = new InstalledSoftwareService();

                // Use CategoryString if available, otherwise fall back to SoftwareCategory enum
                string categoryFilter = null;
                if (!string.IsNullOrEmpty(filter.CategoryString))
                {
                    // Use raw string from database
                    categoryFilter = filter.CategoryString == "All" ? null : filter.CategoryString;
                }
                else if (filter.SoftwareCategory != SoftwareCategory.All)
                {
                    // Fall back to enum value for backward compatibility
                    categoryFilter = filter.SoftwareCategory.ToString();
                }

                // Source filter support (Steam, GOG, etc.)
                string sourceFilter = filter.Source;

                // Query with simplified 2-parameter signature (category + source)
                installedSoftware = service.QueryInstalledSoftware(categoryFilter, sourceFilter);

                // Log with source information if specified
                if (!string.IsNullOrEmpty(sourceFilter))
                {
                    log.Info($"FileFenceFilter: Database query with category={filter.SoftwareCategory}, source={sourceFilter} returned {installedSoftware.Count} items");
                }
                else
                {
                    log.Info($"FileFenceFilter: Database query returned {installedSoftware.Count} software items for category {filter.SoftwareCategory}");
                }

                // Empty database is valid - UI will show loading state
                if (installedSoftware.Count == 0)
                {
                    log.Info("FileFenceFilter: Database returned 0 items (may be loading initial data)");
                }
            }
            catch (Exception ex)
            {
                log.Error($"FileFenceFilter: Error querying database for software filter: {ex.Message}", ex);
                // Return empty list on error - UI will handle this gracefully
                installedSoftware = new List<InstalledSoftware>();
            }

            // Filter and add items with valid paths
            foreach (var software in installedSoftware)
            {
                // Only include items with valid paths
                bool hasValidPath = (!string.IsNullOrEmpty(software.ExecutablePath) && File.Exists(software.ExecutablePath)) ||
                                   (!string.IsNullOrEmpty(software.InstallLocation) && Directory.Exists(software.InstallLocation));

                if (hasValidPath)
                {
                    // Return the complete InstalledSoftware object - preserves ALL metadata!
                    result.SoftwareItems.Add(software);
                }
            }

            log.Info($"FileFenceFilter: Returning {result.SoftwareItems.Count} software items with valid paths");
            return result;
        }

        /// <summary>
        /// Applies file-based filters (Category, Extensions, Pattern, None)
        /// Enhanced in Session 11 to return InstalledSoftware objects instead of just paths.
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
                            // Convert path to InstalledSoftware object
                            result.FileItems.Add(InstalledSoftware.FromPath(filePath));
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
                        // Convert path to InstalledSoftware object
                        result.FileItems.Add(InstalledSoftware.FromPath(item));
                    }
                }
            }

            log.Info($"FileFenceFilter: File-based filter returned {result.FileItems.Count} items");
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
