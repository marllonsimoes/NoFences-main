using System;
using System.Drawing;
using System.IO;

namespace NoFences.Core.Model
{
    /// <summary>
    /// Represents an installed software application or local file/folder.
    /// Supports both database software and local filesystem items.
    /// </summary>
    public class InstalledSoftware
    {
        /// <summary>
        /// Display name of the software
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Publisher/company name
        /// </summary>
        public string Publisher { get; set; }

        /// <summary>
        /// Version string
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Installation directory
        /// </summary>
        public string InstallLocation { get; set; }

        /// <summary>
        /// Path to the executable (if available)
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// Path to the application icon (if available)
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// Installation date (if available)
        /// </summary>
        public DateTime? InstallDate { get; set; }

        /// <summary>
        /// Uninstall command/path
        /// </summary>
        public string UninstallString { get; set; }

        /// <summary>
        /// Automatically categorized software type
        /// </summary>
        public SoftwareCategory Category { get; set; }

        /// <summary>
        /// Registry key where this software was found
        /// </summary>
        public string RegistryKey { get; set; }

        /// <summary>
        /// Whether this is a 32-bit app on 64-bit Windows (WOW6432Node)
        /// </summary>
        public bool IsWow64 { get; set; }

        /// <summary>
        /// Source/origin of this item (Local, Steam, Epic, GOG, etc.)
        /// Tracks where the software came from.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Foreign key to SoftwareReference.Id in master_catalog.db.
        /// Links this installation to enriched metadata.
        /// </summary>
        public long SoftwareRefId { get; set; }

        // Enriched metadata from external sources (RAWG, Winget, Wikipedia, CNET)

        /// <summary>
        /// Software/game description from metadata enrichment.
        /// Populated by MetadataEnrichmentService.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Genres (for games) as comma-separated string.
        /// Example: "Action, RPG, Adventure"
        /// Populated by game metadata providers (RAWG).
        /// </summary>
        public string Genres { get; set; }

        /// <summary>
        /// Developers (for games) as comma-separated string.
        /// Example: "FromSoftware, Bandai Namco"
        /// Populated by game metadata providers (RAWG).
        /// </summary>
        public string Developers { get; set; }

        /// <summary>
        /// Release date (for games/software).
        /// Populated by metadata providers.
        /// </summary>
        public DateTime? ReleaseDate { get; set; }

        /// <summary>
        /// URL to cover art/icon image from external source.
        /// Populated by metadata providers.
        /// </summary>
        public string CoverImageUrl { get; set; }

        /// <summary>
        /// Average rating (0.0 to 5.0 scale).
        /// Populated by metadata providers (RAWG, CNET).
        /// </summary>
        public double? Rating { get; set; }

        /// <summary>
        /// Cached icon (non-serialized) to avoid repeated extraction.
        /// Performance optimization.
        /// </summary>
        [NonSerialized]
        private Icon cachedIcon;

        /// <summary>
        /// Gets or sets the cached icon. Not serialized to database.
        /// </summary>
        public Icon CachedIcon
        {
            get => cachedIcon;
            set => cachedIcon = value;
        }

        public InstalledSoftware()
        {
            Category = SoftwareCategory.Other;
            Source = "WindowsRegistry"; // Default source
        }

        /// <summary>
        /// Creates an InstalledSoftware instance from a local file or folder path.
        /// Unifies local files with database software.
        /// </summary>
        /// <param name="path">Full path to file or folder</param>
        /// <returns>InstalledSoftware instance representing the local item</returns>
        public static InstalledSoftware FromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var item = new InstalledSoftware
            {
                Source = "Local"
            };

            // Determine if it's a file or folder
            if (Directory.Exists(path))
            {
                item.InstallLocation = path;
                item.Name = Path.GetFileName(path);
                item.Category = SoftwareCategory.Other;
            }
            else if (File.Exists(path))
            {
                var extension = Path.GetExtension(path).ToLowerInvariant();

                if (extension == ".exe" || extension == ".msi")
                {
                    item.ExecutablePath = path;
                    item.Name = Path.GetFileNameWithoutExtension(path);
                    item.Category = SoftwareCategory.Other;
                }
                else if (extension == ".lnk" || extension == ".url")
                {
                    item.ExecutablePath = path;
                    item.Name = Path.GetFileNameWithoutExtension(path);
                    item.Category = SoftwareCategory.Other;
                }
                else
                {
                    // Regular file
                    item.ExecutablePath = path;
                    item.Name = Path.GetFileNameWithoutExtension(path);
                    item.Category = SoftwareCategory.Other;
                }
            }
            else
            {
                // Path doesn't exist - still create item but mark appropriately
                item.ExecutablePath = path;
                item.Name = Path.GetFileName(path) ?? path;
                item.Category = SoftwareCategory.Other;
            }

            return item;
        }

        /// <summary>
        /// Creates InstalledSoftware from joined database entities (LocalInstallation + SoftwareReference).
        /// This is the primary way to construct complete software objects with enriched metadata.
        /// Session 14: Factory method pattern for unified model.
        /// </summary>
        /// <param name="local">Local installation data from ref.db</param>
        /// <param name="reference">Enriched metadata from master_catalog.db</param>
        /// <returns>Complete InstalledSoftware object</returns>
        public static InstalledSoftware FromJoin(object local, object reference)
        {
            // Using object parameters to avoid circular reference between Core and DataLayer
            // Caller must pass LocalInstallation and SoftwareReference
            // Properties are accessed via reflection-like dynamic access

            dynamic localDyn = local;
            dynamic refDyn = reference;

            var software = new InstalledSoftware
            {
                // Local data (from ref.db via LocalInstallation)
                SoftwareRefId = localDyn.SoftwareRefId,
                InstallLocation = localDyn.InstallLocation,
                ExecutablePath = localDyn.ExecutablePath,
                IconPath = localDyn.IconPath,
                RegistryKey = localDyn.RegistryKey,
                Version = localDyn.Version,
                InstallDate = localDyn.InstallDate,

                // Reference data (from master_catalog.db via SoftwareReference)
                Name = refDyn.Name,
                Publisher = refDyn.Publisher,
                Source = refDyn.Source,
                Description = refDyn.Description,
                Genres = refDyn.Genres,
                Developers = refDyn.Developers,
                ReleaseDate = refDyn.ReleaseDate,
                CoverImageUrl = refDyn.CoverImageUrl
            };

            // Parse category from string
            if (!string.IsNullOrEmpty(refDyn.Category))
            {
                SoftwareCategory category;
                if (Enum.TryParse<SoftwareCategory>(refDyn.Category, out category))
                {
                    software.Category = category;
                }
            }

            // Extract rating from MetadataJson
            software.Rating = ExtractRating(refDyn.MetadataJson);

            return software;
        }

        /// <summary>
        /// Creates InstalledSoftware from local installation only (no enriched metadata).
        /// Used when SoftwareReference is not available yet or when metadata is not needed.
        /// Session 14: Factory method for partial data scenarios.
        /// </summary>
        /// <param name="local">Local installation data from ref.db</param>
        /// <returns>InstalledSoftware with local data only</returns>
        public static InstalledSoftware FromLocal(object local)
        {
            dynamic localDyn = local;

            return new InstalledSoftware
            {
                SoftwareRefId = localDyn.SoftwareRefId,
                InstallLocation = localDyn.InstallLocation,
                ExecutablePath = localDyn.ExecutablePath,
                IconPath = localDyn.IconPath,
                RegistryKey = localDyn.RegistryKey,
                Version = localDyn.Version,
                InstallDate = localDyn.InstallDate,
                Name = "[Loading...]", // Placeholder - metadata not loaded
                Source = "Local"
            };
        }

        /// <summary>
        /// Creates InstalledSoftware from SoftwareReference only (enriched metadata, no local paths).
        /// Used for catalog browsing scenarios where local installation data is not relevant.
        /// Session 14: Factory method for metadata-only scenarios.
        /// </summary>
        /// <param name="reference">Enriched metadata from master_catalog.db</param>
        /// <returns>InstalledSoftware with metadata only</returns>
        public static InstalledSoftware FromReference(object reference)
        {
            dynamic refDyn = reference;

            var software = new InstalledSoftware
            {
                Name = refDyn.Name,
                Publisher = refDyn.Publisher,
                Source = refDyn.Source,
                Description = refDyn.Description,
                Genres = refDyn.Genres,
                Developers = refDyn.Developers,
                ReleaseDate = refDyn.ReleaseDate,
                CoverImageUrl = refDyn.CoverImageUrl
            };

            // Parse category from string
            if (!string.IsNullOrEmpty(refDyn.Category))
            {
                SoftwareCategory category;
                if (Enum.TryParse<SoftwareCategory>(refDyn.Category, out category))
                {
                    software.Category = category;
                }
            }

            // Extract rating from MetadataJson
            software.Rating = ExtractRating(refDyn.MetadataJson);

            return software;
        }

        /// <summary>
        /// Extracts rating value from MetadataJson string.
        /// Rating is stored in JSON format: {"rating": 4.5, ...}
        /// Session 14: Helper for factory methods.
        /// </summary>
        private static double? ExtractRating(string metadataJson)
        {
            if (string.IsNullOrEmpty(metadataJson))
                return null;

            try
            {
                // Simple JSON parsing for rating field
                // Format: {"rating":4.5,...}
                int ratingIndex = metadataJson.IndexOf("\"rating\"");
                if (ratingIndex >= 0)
                {
                    int colonIndex = metadataJson.IndexOf(":", ratingIndex);
                    if (colonIndex >= 0)
                    {
                        int commaIndex = metadataJson.IndexOf(",", colonIndex);
                        int braceIndex = metadataJson.IndexOf("}", colonIndex);

                        int endIndex = commaIndex >= 0 ?
                            (braceIndex >= 0 ? Math.Min(commaIndex, braceIndex) : commaIndex) :
                            (braceIndex >= 0 ? braceIndex : metadataJson.Length);

                        if (endIndex > colonIndex)
                        {
                            string ratingStr = metadataJson.Substring(colonIndex + 1, endIndex - colonIndex - 1).Trim();
                            if (double.TryParse(ratingStr, out double rating))
                            {
                                return rating;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Silent fail - rating is optional
            }

            return null;
        }

        public override string ToString()
        {
            return $"{Name} ({Publisher}) - {Category}";
        }
    }
}
