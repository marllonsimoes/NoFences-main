using NoFences.Core.Util;
using NoFencesDataLayer.Migrations;
using SQLite.CodeFirst;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.IO;

namespace NoFencesService.Repository
{
    /// <summary>
    /// Local database context for user-specific application data.
    /// Contains only user's installed games tracking.
    ///
    /// Note: Software/game catalog data is in master_catalog.db (MasterCatalogContext).
    /// </summary>
    [DbConfigurationType(typeof(LocalDBConfiguration))]
    public class LocalDBContext : DbContext
    {
        // User's installed Steam games (tracks what user has installed locally)
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
