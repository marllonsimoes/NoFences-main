using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoFencesDataLayer.MasterCatalog.Entities
{
    /// <summary>
    /// Master catalog game entry (platform-agnostic).
    /// A single entry represents ONE game across ALL platforms.
    /// </summary>
    [Table("Games")]
    public class MasterGameEntry
    {
        /// <summary>
        /// Unique identifier (normalized game name)
        /// E.g., "cyberpunk-2077", "counter-strike-global-offensive"
        /// </summary>
        [Key]
        [MaxLength(200)]
        public string Id { get; set; }

        /// <summary>
        /// Game name (required)
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Name { get; set; }

        /// <summary>
        /// Platform-specific IDs as JSON object
        /// E.g., {"Steam": 730, "GOG": "1234567890", "Epic": "epic-game-id"}
        /// </summary>
        [MaxLength(1000)]
        public string PlatformIds { get; set; }

        /// <summary>
        /// Release date (ISO 8601 format)
        /// </summary>
        [MaxLength(50)]
        public string ReleaseDate { get; set; }

        /// <summary>
        /// Developer(s) as JSON array
        /// </summary>
        [MaxLength(1000)]
        public string Developers { get; set; }

        /// <summary>
        /// Publisher(s) as JSON array
        /// </summary>
        [MaxLength(1000)]
        public string Publishers { get; set; }

        /// <summary>
        /// Game genres as JSON array
        /// E.g., ["Action", "FPS", "Multiplayer"]
        /// </summary>
        [MaxLength(1000)]
        public string Genres { get; set; }

        /// <summary>
        /// Supported operating systems as JSON object
        /// E.g., {"windows": true, "mac": true, "linux": false}
        /// </summary>
        [MaxLength(500)]
        public string SupportedOS { get; set; }

        // Version tracking

        /// <summary>
        /// Incremental version number for this entry
        /// Incremented on each update
        /// </summary>
        [Required]
        public long Version { get; set; }

        /// <summary>
        /// When this entry was first created (UTC)
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When this entry was last updated (UTC)
        /// </summary>
        [Required]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Soft delete flag - if true, entry is marked as deleted
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Optional: Who made the last change (for audit trail)
        /// </summary>
        [MaxLength(200)]
        public string LastModifiedBy { get; set; }
    }
}
