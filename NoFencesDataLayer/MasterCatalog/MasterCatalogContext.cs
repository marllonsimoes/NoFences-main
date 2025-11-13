using NoFences.Core.Util;
using NoFencesDataLayer.MasterCatalog.Entities;
using NoFencesService.Repository;
using SQLite.CodeFirst;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.IO;

namespace NoFencesDataLayer.MasterCatalog
{
    /// <summary>
    /// Entity Framework context for the Master Catalog database.
    /// This is the source of truth for all software/game data.
    /// Separate from LocalDBContext (which is used by NoFences clients).
    /// </summary>
    [DbConfigurationType(typeof(MasterCatalogConfiguration))]
    public class MasterCatalogContext : DbContext
    {
        /// <summary>
        /// Master software entries with version tracking
        /// </summary>
        public DbSet<MasterSoftwareEntry> Software { get; set; }

        /// <summary>
        /// Master game entries (platform-agnostic) with version tracking
        /// Each game appears once regardless of how many platforms it's on
        /// </summary>
        public DbSet<MasterGameEntry> Games { get; set; }

        /// <summary>
        /// Catalog version information (single row)
        /// </summary>
        public DbSet<CatalogVersion> CatalogVersion { get; set; }

        /// <summary>
        /// Audit trail of all changes
        /// </summary>
        public DbSet<ChangeLog> ChangeLogs { get; set; }

        /// <summary>
        /// Reference table for software/games with enriched metadata.
        /// Shareable reference data that can be crowdsourced.
        /// </summary>
        public DbSet<SoftwareReference> SoftwareReferences { get; set; }

        private static readonly string serviceDatabase = "master_catalog.db";

        private static string basePath = Path.Combine(
                new string[] {
                    AppEnvUtil.GetAppEnvironmentPath(),
                    serviceDatabase
                });

        /// <summary>
        /// Constructor with explicit connection string
        /// </summary>
        public MasterCatalogContext() : base($"Data Source={basePath}")
        {
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Set up SQLite database initializer (creates tables if they don't exist)
            var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<MasterCatalogContext>(modelBuilder);
            Database.SetInitializer(sqliteConnectionInitializer);

            // Remove pluralizing convention
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            // Software indexes
            modelBuilder.Entity<MasterSoftwareEntry>()
                .HasIndex(e => e.UpdatedAt)
                .HasName("IX_Software_UpdatedAt");

            modelBuilder.Entity<MasterSoftwareEntry>()
                .HasIndex(e => e.Version)
                .HasName("IX_Software_Version");

            modelBuilder.Entity<MasterSoftwareEntry>()
                .HasIndex(e => e.Category)
                .HasName("IX_Software_Category");

            modelBuilder.Entity<MasterSoftwareEntry>()
                .HasIndex(e => e.IsDeleted)
                .HasName("IX_Software_IsDeleted");

            // Games indexes
            modelBuilder.Entity<MasterGameEntry>()
                .HasIndex(e => e.UpdatedAt)
                .HasName("IX_Games_UpdatedAt");

            modelBuilder.Entity<MasterGameEntry>()
                .HasIndex(e => e.Version)
                .HasName("IX_Games_Version");

            modelBuilder.Entity<MasterGameEntry>()
                .HasIndex(e => e.IsDeleted)
                .HasName("IX_Games_IsDeleted");

            modelBuilder.Entity<MasterGameEntry>()
                .HasIndex(e => e.Name)
                .HasName("IX_Games_Name");

            // ChangeLog indexes
            modelBuilder.Entity<ChangeLog>()
                .HasIndex(e => e.ChangedAt)
                .HasName("IX_ChangeLog_ChangedAt");

            modelBuilder.Entity<ChangeLog>()
                .HasIndex(e => new { e.EntityType, e.EntityId })
                .HasName("IX_ChangeLog_Entity");

            // SoftwareReference indexes
            // Unique constraint on Source + ExternalId (one entry per platform software)
            modelBuilder.Entity<SoftwareReference>()
                .HasIndex(e => new { e.Source, e.ExternalId })
                .IsUnique()
                .HasName("IX_SoftwareRef_Source_ExternalId");

            modelBuilder.Entity<SoftwareReference>()
                .HasIndex(e => e.Name)
                .HasName("IX_SoftwareRef_Name");

            modelBuilder.Entity<SoftwareReference>()
                .HasIndex(e => e.Category)
                .HasName("IX_SoftwareRef_Category");

            modelBuilder.Entity<SoftwareReference>()
                .HasIndex(e => e.LastEnrichedDate)
                .HasName("IX_SoftwareRef_LastEnrichedDate");

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Seeds initial data (CatalogVersion record)
        /// </summary>
        public void SeedInitialData()
        {
            // Ensure CatalogVersion table has exactly one row
            var version = CatalogVersion.Find(1);
            if (version == null)
            {
                CatalogVersion.Add(new CatalogVersion
                {
                    Id = 1,
                    CurrentVersion = 1,
                    LastUpdated = System.DateTime.UtcNow,
                    TotalSoftware = 0,
                    TotalGames = 0,
                    Description = "Initial catalog version"
                });
                SaveChanges();
            }
        }
    }
}
