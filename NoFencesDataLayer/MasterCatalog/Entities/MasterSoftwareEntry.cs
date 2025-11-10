using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoFencesDataLayer.MasterCatalog.Entities
{
    /// <summary>
    /// Master catalog software entry with version tracking.
    /// This is the source of truth for all software data.
    /// </summary>
    [Table("Software")]
    public class MasterSoftwareEntry
    {
        /// <summary>
        /// Unique identifier (e.g., "google-chrome")
        /// </summary>
        [Key]
        [MaxLength(200)]
        public string Id { get; set; }

        /// <summary>
        /// Software name (required)
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Name { get; set; }

        /// <summary>
        /// Company/publisher name
        /// </summary>
        [MaxLength(500)]
        public string Company { get; set; }

        /// <summary>
        /// Software category (Games, Development, etc.)
        /// </summary>
        [MaxLength(100)]
        public string Category { get; set; }

        /// <summary>
        /// Icon URL
        /// </summary>
        [MaxLength(1000)]
        public string IconUrl { get; set; }

        /// <summary>
        /// Tags as JSON array string (e.g., ["browser","free"])
        /// </summary>
        [MaxLength(1000)]
        public string Tags { get; set; }

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
