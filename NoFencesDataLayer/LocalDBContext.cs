using NoFences.Core.Util;
using NoFencesDataLayer.MasterCatalog.Entities;
using NoFencesDataLayer.Migrations;
using SQLite.CodeFirst;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.IO;

namespace NoFencesService.Repository
{
    /// <summary>
    /// Local database context for user-specific application data.
    /// Contains machine-specific installation data only.
    ///
    /// Session 12: Now includes InstalledSoftware table (moved from master_catalog.db).
    /// Note: Software/game enriched metadata is in master_catalog.db (SoftwareReference table).
    /// </summary>
    [DbConfigurationType(typeof(LocalDBConfiguration))]
    public class LocalDBContext : DbContext
    {
        /// <summary>
        /// Local installation data with foreign key to SoftwareReference in master_catalog.db.
        /// Session 12: Database architecture refactor - moved from master_catalog.db to ref.db.
        /// </summary>
        public DbSet<InstalledSoftwareEntry> InstalledSoftware { get; set; }

        /// <summary>
        /// User's installed Steam games (legacy table from before Session 11).
        /// Kept for backward compatibility. May be deprecated in future.
        /// </summary>
        public DbSet<InstalledSteamGame> InstalledSteamGames { get; set; }

        private static readonly string serviceDatabase = "ref.db";

        private static string basePath = Path.Combine(
                new string[] {
                    AppEnvUtil.GetAppEnvironmentPath(),
                    serviceDatabase
                });

        static LocalDBContext() => Database.SetInitializer(new MigrateDatabaseToLatestVersion<LocalDBContext, Configuration>(true));

        public LocalDBContext() : base($"Data Source={basePath}") { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<LocalDBContext>(modelBuilder);
            Database.SetInitializer(sqliteConnectionInitializer);

            // Remove pluralizing convention
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            // Session 12: InstalledSoftware indexes
            // Index on SoftwareRefId for fast JOIN with master_catalog.software_ref
            modelBuilder.Entity<InstalledSoftwareEntry>()
                .HasIndex(e => e.SoftwareRefId)
                .HasName("IX_InstalledSoftware_SoftwareRefId");

            // Index on LastDetected for cleanup queries
            modelBuilder.Entity<InstalledSoftwareEntry>()
                .HasIndex(e => e.LastDetected)
                .HasName("IX_InstalledSoftware_LastDetected");

            // Unique constraint: SoftwareRefId + InstallLocation
            // Same software can be installed in multiple locations on same machine
            modelBuilder.Entity<InstalledSoftwareEntry>()
                .HasIndex(e => new { e.SoftwareRefId, e.InstallLocation })
                .IsUnique()
                .HasName("IX_InstalledSoftware_SoftwareRefId_InstallLocation");

            base.OnModelCreating(modelBuilder);
        }
    }

    /// <summary>
    /// Installed Steam games detected on this machine.
    /// User-specific data - tracks which games the user has installed.
    /// </summary>
    [Table("InstalledSteamGame")]
    public class InstalledSteamGame
    {
        [Key]
        public long Id { get; set; }

        [Index(IsUnique = true)]
        public int AppID { get; set; }

        [MaxLength(500)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string InstallDir { get; set; }

        [MaxLength(1000)]
        public string LibraryPath { get; set; }

        [MaxLength(1000)]
        public string ShortcutPath { get; set; } // Path to .url shortcut if exists

        public long? SizeOnDisk { get; set; }

        public DateTime? LastUpdated { get; set; }

        public DateTime LastScanned { get; set; }
    }
}
