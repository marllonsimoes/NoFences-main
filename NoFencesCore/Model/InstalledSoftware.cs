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

        public override string ToString()
        {
            return $"{Name} ({Publisher}) - {Category}";
        }
    }
}
