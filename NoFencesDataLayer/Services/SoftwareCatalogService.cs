using log4net;
using Newtonsoft.Json.Linq;
using NoFences.Core.Model;
using NoFencesDataLayer.MasterCatalog;
using NoFencesDataLayer.MasterCatalog.Entities;
using NoFencesService.Repository;
using System;
using System.IO;
using System.Linq;

namespace NoFencesDataLayer.Services
{
    /// <summary>
    /// Service for looking up software in the master catalog database.
    /// Provides accurate categorization based on comprehensive CSV data.
    /// Uses master catalog directly - no migration needed.
    /// </summary>
    public class SoftwareCatalogService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SoftwareCatalogService));

        private readonly MasterCatalogContext catalogContext;
        private readonly LocalDBContext localContext; // For installed games tracking only
        private bool _catalogChecked = false;
        private bool _catalogAvailable = false;

        /// <summary>
        /// Constructor with master catalog path.
        /// Enhanced with graceful fallback when master_catalog.db doesn't exist.
        /// </summary>
        /// <param name="masterCatalogPath">Path to master catalog database</param>
        /// <param name="localContext">Local context for installed games tracking (optional)</param>
        public SoftwareCatalogService()
        {
            try
            {
                // Check if master catalog file exists before creating context
                string catalogPath = GetDefaultCatalogPath();
                if (File.Exists(catalogPath))
                {
                    this.catalogContext = new MasterCatalogContext();
                    log.Debug($"Initialized with master catalog at {catalogPath}");
                }
                else
                {
                    log.Info($"Master catalog not found at {catalogPath} - will use fallback methods");
                    this.catalogContext = null; // No catalog available
                }

                this.localContext = new LocalDBContext();
            }
            catch (Exception ex)
            {
                log.Error($"Error initializing catalog service: {ex.Message}", ex);
                this.catalogContext = null; // Graceful fallback
            }
        }

        /// <summary>
        /// Checks if the software catalog has been imported and is available.
        /// Enhanced with null check for when catalog file doesn't exist.
        /// </summary>
        public bool IsCatalogAvailable()
        {
            if (_catalogChecked)
                return _catalogAvailable;

            try
            {
                // Check if catalog context is null (file doesn't exist)
                if (catalogContext == null)
                {
                    log.Debug("Catalog context is null - master_catalog.db not found");
                    _catalogAvailable = false;
                    _catalogChecked = true;
                    return false;
                }

                // Check if any catalog entries exist
                _catalogAvailable = catalogContext.Software.Any(s => !s.IsDeleted);
                _catalogChecked = true;

                if (_catalogAvailable)
                {
                    var count = catalogContext.Software.Count(s => !s.IsDeleted);
                    log.Debug($"Catalog available with {count} entries");
                }
                else
                {
                    log.Warn("Catalog not available - no entries found");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error checking catalog: {ex.Message}", ex);
                _catalogAvailable = false;
                _catalogChecked = true;
            }

            return _catalogAvailable;
        }

        /// <summary>
        /// Looks up software in the catalog by exact name match.
        /// </summary>
        public MasterSoftwareEntry LookupByName(string softwareName)
        {
            if (string.IsNullOrWhiteSpace(softwareName))
                return null;

            if (!IsCatalogAvailable())
                return null;

            try
            {
                return catalogContext.Software
                    .Where(s => !s.IsDeleted)
                    .FirstOrDefault(s => s.Name.Equals(softwareName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                log.Error($"Error looking up software: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Looks up software in the catalog by partial name match.
        /// Useful for detecting software with version numbers or extra text in the name.
        /// </summary>
        public MasterSoftwareEntry LookupByPartialName(string softwareName)
        {
            if (string.IsNullOrWhiteSpace(softwareName))
                return null;

            if (!IsCatalogAvailable())
                return null;

            try
            {
                var cleanName = CleanSoftwareName(softwareName);

                // Try exact match first
                var exact = catalogContext.Software
                    .Where(s => !s.IsDeleted)
                    .FirstOrDefault(s => s.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase));

                if (exact != null)
                    return exact;

                // Try partial match (catalog name contained in software name)
                var partial = catalogContext.Software
                    .Where(s => !s.IsDeleted)
                    .FirstOrDefault(s => cleanName.Contains(s.Name));

                return partial;
            }
            catch (Exception ex)
            {
                log.Error($"Error looking up software by partial name: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Looks up a game by Steam AppID.
        /// Searches through platform-agnostic game entries.
        /// Enhanced with null check for catalog availability.
        /// </summary>
        public MasterGameEntry LookupGameBySteamAppId(int appId)
        {
            if (!IsCatalogAvailable())
                return null;

            try
            {
                var games = catalogContext.Games
                    .Where(g => !g.IsDeleted)
                    .ToList(); // Load into memory for JSON parsing

                foreach (var game in games)
                {
                    var steamAppId = ExtractSteamAppId(game.PlatformIds);
                    if (steamAppId == appId)
                        return game;
                }

                return null;
            }
            catch (Exception ex)
            {
                log.Error($"Error looking up game by Steam AppID: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Looks up a game by name.
        /// Enhanced with null check for catalog availability.
        /// </summary>
        public MasterGameEntry LookupGameByName(string gameName)
        {
            if (string.IsNullOrWhiteSpace(gameName))
                return null;

            if (!IsCatalogAvailable())
                return null;

            try
            {
                return catalogContext.Games
                    .Where(g => !g.IsDeleted)
                    .FirstOrDefault(g => g.Name.Equals(gameName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                log.Error($"Error looking up game by name: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Gets the category for software based on catalog lookup.
        /// Falls back to heuristic categorization if not found in catalog.
        /// </summary>
        public SoftwareCategory GetCategory(string softwareName, string publisher = null)
        {
            // Try catalog lookup first
            var catalogEntry = LookupByPartialName(softwareName);
            if (catalogEntry != null && !string.IsNullOrEmpty(catalogEntry.Category))
            {
                if (Enum.TryParse<SoftwareCategory>(catalogEntry.Category, out var category))
                {
                    log.Debug($"Found '{softwareName}' in catalog as {category}");
                    return category;
                }
            }

            // Fallback to heuristic categorization
            log.Debug($"'{softwareName}' not in catalog, using heuristic");
            return SoftwareCategorizer.Categorize(softwareName, publisher);
        }

        /// <summary>
        /// Static helper method to get category using master catalog.
        /// Creates its own catalog context and handles catalog lookup + fallback.
        /// </summary>
        /// <param name="softwareName">Name of the software to categorize</param>
        /// <param name="publisher">Optional publisher name</param>
        /// <param name="installLocation">Optional install location for heuristic fallback</param>
        /// <param name="catalogPath">Path to master catalog database (optional, uses default if not provided)</param>
        /// <returns>Software category</returns>
        public static SoftwareCategory GetCategoryStatic(string softwareName, string publisher = null, string installLocation = null, string catalogPath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(catalogPath))
                    catalogPath = GetDefaultCatalogPath();

                if (File.Exists(catalogPath))
                {
                    var service = new SoftwareCatalogService();

                    // Only try catalog if it's available
                    if (service.IsCatalogAvailable())
                    {
                        return service.GetCategory(softwareName, publisher);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error in static categorization: {ex.Message}", ex);
            }

            // Fallback to heuristic if catalog not available or error occurred
            return SoftwareCategorizer.Categorize(softwareName, publisher, installLocation);
        }

        /// <summary>
        /// Gets statistics about the catalog.
        /// Enhanced with null check for catalog availability.
        /// </summary>
        public CatalogStatistics GetStatistics()
        {
            var stats = new CatalogStatistics();

            try
            {
                // Return empty stats if catalog not available
                if (!IsCatalogAvailable())
                {
                    log.Debug("Catalog not available - returning empty statistics");
                    return stats;
                }

                stats.TotalSoftware = catalogContext.Software.Count(s => !s.IsDeleted);
                stats.TotalGames = catalogContext.Games.Count(g => !g.IsDeleted);

                if (localContext != null)
                {
                    stats.TotalInstalledSteamGames = localContext.InstalledSteamGames.Count();
                }

                // Count by category
                foreach (SoftwareCategory cat in Enum.GetValues(typeof(SoftwareCategory)))
                {
                    var catName = cat.ToString();
                    var count = catalogContext.Software.Count(s => !s.IsDeleted && s.Category == catName);
                    stats.CategoryCounts[cat] = count;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error getting statistics: {ex.Message}", ex);
            }

            return stats;
        }

        /// <summary>
        /// Gets list of available software categories from the catalog database.
        /// Only returns categories that have at least one entry.
        /// Enhanced with fallback to return all categories if catalog unavailable.
        /// </summary>
        /// <returns>List of SoftwareCategory values that have entries in the catalog</returns>
        public System.Collections.Generic.List<SoftwareCategory> GetAvailableCategories()
        {
            var availableCategories = new System.Collections.Generic.List<SoftwareCategory>();

            try
            {
                // Fallback to all categories if catalog not available
                if (!IsCatalogAvailable())
                {
                    log.Debug("Catalog not available - returning all categories");
                    return Enum.GetValues(typeof(SoftwareCategory)).Cast<SoftwareCategory>().ToList();
                }

                // Get distinct categories from database
                var categoryNames = catalogContext.Software
                    .Where(s => !s.IsDeleted && !string.IsNullOrEmpty(s.Category))
                    .Select(s => s.Category)
                    .Distinct()
                    .ToList();

                // Convert to enum values
                foreach (var categoryName in categoryNames)
                {
                    if (Enum.TryParse<SoftwareCategory>(categoryName, out var category))
                    {
                        availableCategories.Add(category);
                    }
                }

                log.Info($"Found {availableCategories.Count} available categories");
            }
            catch (Exception ex)
            {
                log.Error($"Error getting available categories: {ex.Message}", ex);
                // Fallback on error
                return Enum.GetValues(typeof(SoftwareCategory)).Cast<SoftwareCategory>().ToList();
            }

            return availableCategories;
        }

        /// <summary>
        /// Static helper to get available categories from the catalog.
        /// </summary>
        public static System.Collections.Generic.List<SoftwareCategory> GetAvailableCategoriesStatic(string catalogPath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(catalogPath))
                    catalogPath = GetDefaultCatalogPath();

                if (File.Exists(catalogPath))
                {
                    var service = new SoftwareCatalogService();
                    var categories = service.GetAvailableCategories();
                    service.Dispose();
                    return categories;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error in static GetAvailableCategories: {ex.Message}", ex);
            }

            // Fallback: return all enum values if catalog not available
            return Enum.GetValues(typeof(SoftwareCategory)).Cast<SoftwareCategory>().ToList();
        }

        /// <summary>
        /// Gets the default path to the master catalog database.
        /// </summary>
        public static string GetDefaultCatalogPath()
        {
            var appDataPath = NoFences.Core.Util.AppEnvUtil.GetAppEnvironmentPath();
            return Path.Combine(appDataPath, "master_catalog.db");
        }

        #region Helper Methods

        /// <summary>
        /// Cleans software name by removing version numbers and extra text.
        /// Example: "Google Chrome 96.0" -> "Google Chrome"
        /// </summary>
        private string CleanSoftwareName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            // Remove common version patterns
            // "Software 1.2.3" -> "Software"
            // "Software (x64)" -> "Software"
            // "Software v2.0" -> "Software"

            var cleaned = name;

            // Remove version numbers at end (e.g., "Chrome 96.0.4664.110")
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+\d+(\.\d+)+$", "");

            // Remove (x64), (x86), (64-bit), etc.
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s*\((x64|x86|64-bit|32-bit)\)\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Remove version prefix (v1.0, version 2.0, etc.)
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+(v|version)\s*\d+(\.\d+)*$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return cleaned.Trim();
        }

        /// <summary>
        /// Extracts Steam AppID from PlatformIds JSON string.
        /// Example: {"Steam":730,"GOG":"id"} -> 730
        /// </summary>
        private int? ExtractSteamAppId(string platformIdsJson)
        {
            if (string.IsNullOrEmpty(platformIdsJson))
                return null;

            try
            {
                var json = JObject.Parse(platformIdsJson);
                if (json["Steam"] != null)
                {
                    return json["Steam"].Value<int>();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error parsing PlatformIds JSON: {ex.Message}", ex);
            }

            return null;
        }

        #endregion

        public void Dispose()
        {
            catalogContext?.Dispose();
        }
    }

    /// <summary>
    /// Statistics about the software catalog.
    /// </summary>
    public class CatalogStatistics
    {
        public int TotalSoftware { get; set; }
        public int TotalGames { get; set; }
        public int TotalInstalledSteamGames { get; set; }
        public System.Collections.Generic.Dictionary<SoftwareCategory, int> CategoryCounts { get; set; } = new System.Collections.Generic.Dictionary<SoftwareCategory, int>();

        public override string ToString()
        {
            return $"Software: {TotalSoftware}, Games: {TotalGames}, Installed: {TotalInstalledSteamGames}";
        }
    }
}
