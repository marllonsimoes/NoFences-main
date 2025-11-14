using NoFences.Core.Model;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoFencesDataLayer.MasterCatalog.Entities
{
    /// <summary>
    /// Reference table for software/games with enriched metadata.
    /// Part of master_catalog.db - shareable reference data.
    /// </summary>
    [Table("software_ref")]
    public class SoftwareReference
    {
        /// <summary>
        /// Auto-increment primary key - global software identifier
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// Software/game name (required)
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Name { get; set; }

        /// <summary>
        /// External platform ID (Steam AppID, GOG ID, Epic Namespace, etc.)
        /// Used for API lookups and matching. Format depends on platform.
        /// Examples: "440" (Steam), "1234567890" (GOG), "ue4-mandalore" (Epic)
        /// </summary>
        [MaxLength(200)]
        public string ExternalId { get; set; }

        /// <summary>
        /// Detection source/platform (Steam, GOG, Epic, Registry, etc.)
        /// Combined with ExternalId for unique identification.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Source { get; set; }

        /// <summary>
        /// Publisher/company name
        /// </summary>
        [MaxLength(500)]
        public string Publisher { get; set; }

        /// <summary>
        /// Category assigned by source-based logic or catalog lookup.
        /// Examples: Games, Productivity, Development, Multimedia, etc.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Category { get; set; }

        /// <summary>
        /// High-level software type classification.
        /// Determined automatically based on metadata source:
        /// - RAWG → Game
        /// - Winget/CNET/Wikipedia → Application
        /// - Development category → Tool
        /// - Utilities category → Utility
        /// Default: Unknown
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = SoftwareType.Unknown.ToString();

        /// <summary>
        /// Software/game description from metadata enrichment.
        /// Populated by metadata providers (RAWG, Winget, Wikipedia, CNET)
        /// </summary>
        [MaxLength(4000)]
        public string Description { get; set; }

        /// <summary>
        /// Genres (for games) as comma-separated string.
        /// Example: "Action, RPG, Adventure"
        /// Populated by game metadata providers (RAWG).
        /// </summary>
        [MaxLength(1000)]
        public string Genres { get; set; }

        /// <summary>
        /// Developers as comma-separated string.
        /// Example: "FromSoftware, Bandai Namco"
        /// Populated by metadata providers.
        /// </summary>
        [MaxLength(1000)]
        public string Developers { get; set; }

        /// <summary>
        /// Release date (for games/software).
        /// Populated by metadata providers.
        /// </summary>
        public DateTime? ReleaseDate { get; set; }

        /// <summary>
        /// URL to cover art/icon image from external metadata provider.
        /// Populated by metadata providers (RAWG, Winget).
        /// </summary>
        [MaxLength(500)]
        public string CoverImageUrl { get; set; }

        /// <summary>
        /// JSON-serialized platform-specific metadata.
        /// Allows storing arbitrary data without schema changes.
        /// Examples:
        /// - {"rating": 4.5, "steam_tags": ["FPS", "Multiplayer"], "achievements": 520}
        /// - {"winget_package_id": "Valve.Steam", "chocolatey_id": "steam"}
        /// - {"metacritic_score": 92, "esrb_rating": "M"}
        /// </summary>
        public string MetadataJson { get; set; }

        /// <summary>
        /// Last time metadata enrichment was performed on this entry.
        /// Null if never enriched. Used to avoid redundant API calls.
        /// </summary>
        public DateTime? LastEnrichedDate { get; set; }

        /// <summary>
        /// Metadata source that provided enrichment data.
        /// Examples: "RAWG", "Winget", "Wikipedia", "CNET", "Manual"
        /// Track enrichment source for audit purposes.
        /// </summary>
        [MaxLength(100)]
        public string MetadataSource { get; set; }

        /// <summary>
        /// Last time we ATTEMPTED to enrich metadata (regardless of success/failure).
        /// Rate limiting - only attempt enrichment once per day.
        /// Used to prevent overwhelming external APIs.
        /// Null if never attempted.
        /// </summary>
        public DateTime? LastEnrichmentAttempt { get; set; }

        /// <summary>
        /// When this record was first created (UTC)
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When this record was last updated (UTC)
        /// </summary>
        [Required]
        public DateTime UpdatedAt { get; set; }
    }
}
