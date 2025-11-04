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
    /// Enhanced version of InstalledAppsUtil that uses the master catalog database
    /// for more accurate categorization.
    ///
    /// This wraps the original InstalledAppsUtil from NoFencesCore and enhances
    /// the categorization by looking up software in the master catalog database first,
    /// before falling back to heuristic categorization.
    /// </summary>
    public class EnhancedInstalledAppsService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(EnhancedInstalledAppsService));

        private readonly SoftwareCatalogService catalogService;
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
                catalogAvailable = catalogService.IsCatalogAvailable();

                if (catalogAvailable)
                {
                    log.Debug("Catalog available for enhanced categorization");
                }
                else
                {
                    log.Debug("Catalog not available, using heuristic categorization only");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error initializing catalog service: {ex.Message}", ex);
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
        /// Gets installed software filtered by category with enhanced categorization.
        /// </summary>
        public List<InstalledSoftware> GetByCategory(SoftwareCategory category)
        {
            var allSoftware = GetAllInstalled();

            if (category == SoftwareCategory.All)
                return allSoftware;

            return allSoftware.FindAll(s => s.Category == category);
        }

        /// <summary>
        /// Static helper method to get all installed software with enhanced categorization.
        /// Uses default catalog path.
        /// </summary>
        public static List<InstalledSoftware> GetAllInstalledEnhanced()
        {
            try
            {
                var catalogPath = SoftwareCatalogService.GetDefaultCatalogPath();
                if (!File.Exists(catalogPath))
                {
                    log.Info($"Catalog not found at {catalogPath}, using heuristic only");
                    return InstalledAppsUtil.GetAllInstalled();
                }

                var service = new EnhancedInstalledAppsService(catalogPath);
                return service.GetAllInstalled();
            }
            catch (Exception ex)
            {
                log.Error($"Error in static method, falling back to original: {ex.Message}", ex);
                return InstalledAppsUtil.GetAllInstalled();
            }
        }

        /// <summary>
        /// Static helper method to get installed software by category with enhanced categorization.
        /// Uses default catalog path.
        /// </summary>
        public static List<InstalledSoftware> GetByCategoryEnhanced(SoftwareCategory category)
        {
            try
            {
                var catalogPath = SoftwareCatalogService.GetDefaultCatalogPath();
                if (!File.Exists(catalogPath))
                {
                    log.Info($"Catalog not found at {catalogPath}, using heuristic only");
                    return InstalledAppsUtil.GetByCategory(category);
                }

                var service = new EnhancedInstalledAppsService(catalogPath);
                return service.GetByCategory(category);
            }
            catch (Exception ex)
            {
                log.Error($"Error in static method, falling back to original: {ex.Message}", ex);
                return InstalledAppsUtil.GetByCategory(category);
            }
        }
    }
}
