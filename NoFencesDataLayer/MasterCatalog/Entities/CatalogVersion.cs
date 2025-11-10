using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoFencesDataLayer.MasterCatalog.Entities
{
    /// <summary>
    /// Tracks the current version of the entire catalog.
    /// Used for change tracking and sync coordination.
    /// </summary>
    [Table("CatalogVersion")]
    public class CatalogVersion
    {
        /// <summary>
        /// Primary key (always 1, single row table)
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Current catalog version (incremented on any change)
        /// </summary>
        [Required]
        public long CurrentVersion { get; set; }

        /// <summary>
        /// When the catalog was last modified (UTC)
        /// </summary>
        [Required]
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Total number of software entries (active only)
        /// </summary>
        public int TotalSoftware { get; set; }

        /// <summary>
        /// Total number of game entries (active only, platform-agnostic)
        /// </summary>
        public int TotalGames { get; set; }

        /// <summary>
        /// Description of this version (e.g., "November 2025 update")
        /// </summary>
        [MaxLength(1000)]
        public string Description { get; set; }
    }
}
