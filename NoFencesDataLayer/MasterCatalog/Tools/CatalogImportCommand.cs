using log4net;
using NoFencesDataLayer.MasterCatalog.Services;
using NoFencesDataLayer.Services;
using System;
using System.IO;
using System.Linq;

namespace NoFencesDataLayer.MasterCatalog.Tools
{
    /// <summary>
    /// Command-line tool for importing catalog data into the master database.
    /// This is typically used by administrators to build the source of truth database.
    /// </summary>
    public class CatalogImportCommand
    {

        private static readonly ILog log = LogManager.GetLogger(typeof(CatalogImportCommand));

        /// <summary>
        /// Execute the catalog import with console output
        /// </summary>
        public static int Execute(string[] args)
        {
            log.Info("╔════════════════════════════════════════════════════════════╗");
            log.Info("║   NoFences Master Catalog Importer                         ║");
            log.Info("║   Creates the source of truth database                     ║");
            log.Info("╚════════════════════════════════════════════════════════════╝");
            log.Info("");

            // Parse arguments
            string inputDir = args.Length > 1 ? args[1] : @"_software_list";
            string dbPath = args.Length > 2 ? args[2] : @"master_catalog.db";
            int maxSteamGames = args.Length > 3 && int.TryParse(args[3], out int max) ? max : 10000;

            log.Info($"Input directory:  {Path.GetFullPath(inputDir)}");
            log.Info($"Database file:    {Path.GetFullPath(dbPath)}");
            log.Info($"Max Steam games:  {maxSteamGames:N0}");
            log.Info("");

            if (!Directory.Exists(inputDir))
            {
                log.Error($"ERROR: Input directory not found: {inputDir}");
                log.Error("");
                log.Error("Usage: NoFences.exe --import-catalog <input_dir> <db_path> [max_steam_games]");
                log.Error("");
                log.Error("Example:");
                log.Error(@"  NoFences.exe --import-catalog _software_list master_catalog.db 10000");
                return 1;
            }

            try
            {
                var connectionString = $"Data Source={dbPath};Version=3;";

                // Delete old database if exists
                if (File.Exists(dbPath))
                {
                    log.Warn($"⚠ Database file already exists: {dbPath}");
                    log.Warn("  Deleting old database to start fresh...");
                    File.Delete(dbPath);
                    log.Info("  ✓ Old database deleted");
                    log.Info("");
                }

                log.Info("Initializing database...");
                using (var context = new MasterCatalogContext())
                {
                    context.Database.Initialize(force: false);
                    context.SeedInitialData();
                    log.Info("✓ Database initialized");
                    log.Info("");
                }

                log.Info("Starting import...");
                log.Info("─────────────────────────────────────────────────────────────");
                log.Info("");

                var startTime = DateTime.Now;

                using (var context = new MasterCatalogContext())
                {
                    var importer = new MasterCatalogImporter(context);

                    // Import Software.csv
                    var softwarePath = Path.Combine(inputDir, "Software.csv");
                    if (File.Exists(softwarePath))
                    {
                        log.Info("Importing Software.csv...");
                        var result = importer.ImportSoftwareCsv(softwarePath, "ImportTool");
                        PrintResult(result);
                        log.Info("");
                    }
                    else
                    {
                        log.Warn("⚠ Software.csv not found, skipping");
                        log.Info("");
                    }

                    // Import steam.csv
                    var steamPath = Path.Combine(inputDir, "steam.csv");
                    if (File.Exists(steamPath))
                    {
                        log.Info($"Importing steam.csv as Games (limited to {maxSteamGames:N0} entries)...");
                        var result = importer.ImportSteamCsv(steamPath, maxSteamGames, "ImportTool");
                        PrintResult(result);
                        log.Info("");
                    }
                    else
                    {
                        log.Warn("⚠ steam.csv not found, skipping");
                        log.Info("");
                    }
                }

                var duration = DateTime.Now - startTime;

                log.Info("─────────────────────────────────────────────────────────────");
                log.Info("");

                // Print statistics
                using (var context = new MasterCatalogContext())
                {
                    var version = context.CatalogVersion.Find(1);
                    var softwareCount = context.Software.Count(s => !s.IsDeleted);
                    var gamesCount = context.Games.Count(g => !g.IsDeleted);

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    log.Info("╔════════════════════════════════════════════════════════════╗");
                    log.Info("║   Import Complete!                                         ║");
                    log.Info("╚════════════════════════════════════════════════════════════╝");
                    Console.ResetColor();
                    log.Info("");
                    log.Info($"Catalog Version:      {version.CurrentVersion}");
                    log.Info($"Total Software:       {softwareCount:N0} entries");
                    log.Info($"Total Games:          {gamesCount:N0} entries");
                    log.Info($"Last Updated:         {version.LastUpdated:yyyy-MM-dd HH:mm:ss} UTC");
                    log.Info($"Import Duration:      {duration.TotalSeconds:F1} seconds");
                    log.Info("");
                    log.Info($"Database file:        {Path.GetFullPath(dbPath)}");
                    log.Info($"Database size:        {new FileInfo(dbPath).Length / 1024 / 1024:F2} MB");
                    log.Info("");
                }

                Console.ForegroundColor = ConsoleColor.Green;
                log.Info("✓ Master catalog database is ready!");
                Console.ResetColor();
                log.Info("");
                log.Info("Next steps:");
                log.Info("  1. Upload this database to a web server");
                log.Info("  2. Configure catalog URL in NoFences settings");
                log.Info("  3. NoFences will download on first run");

                return 0;
            }
            catch (Exception ex)
            {
                log.Error("");
                log.Error("╔════════════════════════════════════════════════════════════╗");
                log.Error("║   ERROR                                                    ║");
                log.Error("╚════════════════════════════════════════════════════════════╝");
                log.Error("");
                log.Error($"Message: {ex.Message}");

                var innerEx = ex.InnerException;
                int level = 1;
                while (innerEx != null)
                {
                    log.Error("");
                    log.Error($"Inner Exception {level}:");
                    log.Error($"  Message: {innerEx.Message}");
                    innerEx = innerEx.InnerException;
                    level++;
                }

                log.Error("");
                log.Error("Stack trace:");
                log.Error(ex.StackTrace, ex);

                return 1;
            }
        }

        private static void PrintResult(ImportResult result)
        {
            if (result.Success)
            {
                log.Info("  ✓ ");
                log.Info($"Imported: {result.ImportedCount:N0} entries");
                if (result.SkippedCount > 0)
                {
                    log.Warn("  ⚠ ");
                    log.Warn($"Skipped:  {result.SkippedCount:N0} entries");
                }
            }
            else
            {
                
                log.Error("  ✗ ");
                log.Error($"Failed:");
                log.Error("");
                var lines = result.ErrorMessage.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    log.Error($"    {line}");
                }
            }
        }
    }
}
