using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoFencesDataLayer.MasterCatalog.Entities
{
    /// <summary>
    /// Audit trail for all changes to the catalog.
    /// Useful for debugging, rollback, and understanding catalog evolution.
    /// </summary>
    [Table("ChangeLog")]
    public class ChangeLog
    {
        /// <summary>
        /// Primary key
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// Type of entity changed ("Software" or "SteamGame")
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; }

        /// <summary>
        /// ID of the entity that changed
        /// For Software: string ID, For SteamGame: AppId as string
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string EntityId { get; set; }

        /// <summary>
        /// Action performed: "Created", "Updated", "Deleted"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Action { get; set; }

        /// <summary>
        /// When the change occurred (UTC)
        /// </summary>
        [Required]
        public DateTime ChangedAt { get; set; }

        /// <summary>
        /// Who made the change (optional, for admin tracking)
        /// </summary>
        [MaxLength(200)]
        public string ChangedBy { get; set; }

        /// <summary>
        /// JSON object describing what changed
        /// Format: {"field": {"old": "value1", "new": "value2"}}
        /// </summary>
        [MaxLength(4000)]
        public string Changes { get; set; }

        /// <summary>
        /// Catalog version after this change
        /// </summary>
        public long CatalogVersion { get; set; }
    }
}
