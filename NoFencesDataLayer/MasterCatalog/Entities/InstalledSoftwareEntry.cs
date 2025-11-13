using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoFencesDataLayer.MasterCatalog.Entities
{
    /// <summary>
    /// Tracks local installation data for software/games on the user's machine.
    /// Part of ref.db - machine-specific data only.
    /// Session 12: Database architecture refactor - moved from master_catalog.db to ref.db.
    /// References SoftwareReference table in master_catalog.db for enriched metadata.
    /// </summary>
    [Table("InstalledSoftware")]
    public class InstalledSoftwareEntry
    {
        /// <summary>
        /// Auto-increment primary key (local installation ID)
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// Foreign key to SoftwareReference.Id in master_catalog.db.
        /// Links local installation to global software metadata.
        /// Session 12: New field for normalized architecture.
        /// </summary>
        [Required]
        public long SoftwareRefId { get; set; }

        /// <summary>
        /// Installation directory path (machine-specific)
        /// </summary>
        [MaxLength(1000)]
        public string InstallLocation { get; set; }

        /// <summary>
        /// Path to primary executable (machine-specific)
        /// </summary>
        [MaxLength(1000)]
        public string ExecutablePath { get; set; }

        /// <summary>
        /// Path to locally cached icon file (machine-specific)
        /// </summary>
        [MaxLength(1000)]
        public string IconPath { get; set; }

        /// <summary>
        /// Full registry key path where software was found (machine-specific)
        /// Examples: "HKLM\SOFTWARE\...", "Steam:440", "Epic:ue4-mandalore"
        /// Session 12: Now stored in local database
        /// </summary>
        [MaxLength(1000)]
        public string RegistryKey { get; set; }

        /// <summary>
        /// Installed version string (machine-specific)
        /// </summary>
        [MaxLength(100)]
        public string Version { get; set; }

        /// <summary>
        /// When the software was installed on this machine
        /// </summary>
        public DateTime? InstallDate { get; set; }

        /// <summary>
        /// Installation size in bytes (machine-specific)
        /// </summary>
        public long? SizeBytes { get; set; }

        /// <summary>
        /// Last time this software was detected as installed on this machine.
        /// Used to identify uninstalled software (not detected recently)
        /// </summary>
        [Required]
        public DateTime LastDetected { get; set; }

        /// <summary>
        /// When this local installation record was first created (UTC)
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When this local installation record was last updated (UTC)
        /// </summary>
        [Required]
        public DateTime UpdatedAt { get; set; }
    }
}
