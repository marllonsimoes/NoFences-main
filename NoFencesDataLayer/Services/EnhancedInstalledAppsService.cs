using log4net;
using NoFences.Core.Model;
using NoFences.Core.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NoFencesDataLayer.Services
{
    /// <summary>
    /// Enhanced version of InstalledAppsUtil that uses the two-tier database architecture
    /// (ref.db + master_catalog.db) for efficient queries with enriched metadata.
    ///
    /// Queries the database first for better performance, falling back to system scan
    /// if database is empty or unavailable.
    /// </summary>
    public class EnhancedInstalledAppsService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(EnhancedInstalledAppsService));

        private readonly SoftwareCatalogService catalogService;
        private readonly InstalledSoftwareService installedSoftwareService;
        private readonly bool catalogAvailable;

        /// <summary>
        /// Constructor with master catalog path
        /// </summary>
        /// <param name="masterCatalogPath">Path to master catalog database</param>
        public EnhancedInstalledAppsService(string masterCatalogPath)
        {
            if (string.IsNullOrEmpty(masterCatalogPath))
                throw new ArgumentNullException(nameof(masterCatalogPath));

            if (!File.Exists(masterCatalogPath))
            {
                log.Debug($"Master catalog not found at {masterCatalogPath}");
                catalogAvailable = false;
                return;
            }

            try
            {
                catalogService = new SoftwareCatalogService();
                installedSoftwareService = new InstalledSoftwareService();
                catalogAvailable = catalogService.IsCatalogAvailable();

                if (catalogAvailable)
                {
                    log.Debug("Two-tier database architecture available for queries");
                }
                else
                {
                    log.Debug("Database not available, will fall back to system scan");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error initializing services: {ex.Message}", ex);
                catalogAvailable = false;
            }
        }

        /// <summary>
        /// Gets all installed software with enhanced categorization using the master catalog database.
        /// </summary>
        public List<InstalledSoftware> GetAllInstalled()
        {
            // Get software using the original utility from NoFencesCore
            var software = InstalledAppsUtil.GetAllInstalled();

            if (!catalogAvailable || catalogService == null)
            {
                log.Debug("Catalog not available, returning original results");
                return software;
            }

            // Enhance categorization for non-game software
            int enhancedCount = 0;
            foreach (var app in software)
            {
                // Skip if already categorized as game (games are already well-categorized by store detectors)
                if (app.Category == SoftwareCategory.Games)
                    continue;

                try
                {
                    var originalCategory = app.Category;
                    app.Category = catalogService.GetCategory(app.Name, app.Publisher);

                    if (app.Category != originalCategory)
                    {
                        enhancedCount++;
                        log.Debug($"Enhanced '{app.Name}': {originalCategory} → {app.Category}");
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"Error categorizing '{app.Name}': {ex.Message}", ex);
                }
            }

            log.Debug($"Enhanced categorization for {enhancedCount} of {software.Count} software items");

            return software;
        }

        /// <summary>
        /// Gets installed software filtered by category.
        /// Uses two-tier database architecture for fast queries with enriched metadata.
        /// Falls back to system scan if database is empty or unavailable.
        /// </summary>
        public List<InstalledSoftware> GetByCategory(SoftwareCategory category)
        {
            // Try database query first (much faster)
            if (installedSoftwareService != null)
            {
                try
                {
                    string categoryFilter = category == SoftwareCategory.All ? null : category.ToString();
                    var dbResults = installedSoftwareService.QueryInstalledSoftware(categoryFilter, source: null);

                    if (dbResults != null && dbResults.Count > 0)
                    {
                        log.Debug($"Retrieved {dbResults.Count} items from database for category '{category}'");
                        return dbResults;
                    }

                    log.Debug("Database query returned empty results, falling back to system scan");
                }
                catch (Exception ex)
                {
                    log.Error($"Error querying database, falling back to system scan: {ex.Message}", ex);
                }
            }

            // Fallback: Scan system directly (slower but always works)
            log.Debug("Using system scan fallback");
            return InstalledAppsUtil.GetByCategory(category);
        }

        /// <summary>
        /// Static helper method to get all installed software.
        /// Queries database first, falls back to system scan if needed.
        /// </summary>
        public static List<InstalledSoftware> GetAllInstalledEnhanced()
        {
            try
            {
                // Try database query first
                var installedSoftwareService = new InstalledSoftwareService();
                var dbResults = installedSoftwareService.QueryInstalledSoftware(category: null, source: null);

                if (dbResults != null && dbResults.Count > 0)
                {
                    log.Info($"Retrieved {dbResults.Count} items from database");
                    return dbResults;
                }

                log.Info("Database empty, scanning system");
            }
            catch (Exception ex)
            {
                log.Error($"Error querying database, falling back to system scan: {ex.Message}", ex);
            }

            // Fallback to system scan
            return InstalledAppsUtil.GetAllInstalled();
        }

        /// <summary>
        /// Static helper method to get installed software by category.
        /// Queries database first, falls back to system scan if needed.
        /// </summary>
        public static List<InstalledSoftware> GetByCategoryEnhanced(SoftwareCategory category)
        {
            try
            {
                // Try database query first
                var installedSoftwareService = new InstalledSoftwareService();
                string categoryFilter = category == SoftwareCategory.All ? null : category.ToString();
                var dbResults = installedSoftwareService.QueryInstalledSoftware(categoryFilter, source: null);

                if (dbResults != null && dbResults.Count > 0)
                {
                    log.Info($"Retrieved {dbResults.Count} items from database for category '{category}'");
                    return dbResults;
                }

                log.Info($"Database empty for category '{category}', scanning system");
            }
            catch (Exception ex)
            {
                log.Error($"Error querying database, falling back to system scan: {ex.Message}", ex);
            }

            // Fallback to system scan
            return InstalledAppsUtil.GetByCategory(category);
        }
    }
}
