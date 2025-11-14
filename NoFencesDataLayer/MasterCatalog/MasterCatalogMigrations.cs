using log4net;
using System;
using System.Data.SQLite;

namespace NoFencesDataLayer.MasterCatalog
{
    /// <summary>
    /// Handles schema migrations for the master_catalog.db database.
    /// Provides backward-compatible database updates.
    /// </summary>
    public static class MasterCatalogMigrations
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MasterCatalogMigrations));

        /// <summary>
        /// Applies all pending migrations to the database.
        /// Call this on application startup to ensure database schema is current.
        /// </summary>
        public static void ApplyMigrations(string connectionString)
        {
            try
            {
                log.Info("Checking for pending database migrations...");

                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // Migration 1: Add Type column to software_ref table (Session 16)
                    AddSoftwareTypeColumn(connection);

                    log.Info("Database migrations completed successfully");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error applying database migrations: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Migration: Add Type column to software_ref table.
        /// Session 16 - November 14, 2025
        /// Adds SoftwareType enum support for distinguishing Games vs Applications vs Tools.
        /// </summary>
        private static void AddSoftwareTypeColumn(SQLiteConnection connection)
        {
            try
            {
                // Check if column already exists
                using (var cmd = new SQLiteCommand("PRAGMA table_info(software_ref)", connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        bool typeColumnExists = false;
                        while (reader.Read())
                        {
                            string columnName = reader.GetString(1); // Column name is at index 1
                            if (columnName.Equals("Type", StringComparison.OrdinalIgnoreCase))
                            {
                                typeColumnExists = true;
                                break;
                            }
                        }

                        if (typeColumnExists)
                        {
                            log.Debug("Type column already exists in software_ref table - skipping migration");
                            return;
                        }
                    }
                }

                log.Info("Adding Type column to software_ref table...");

                // Add the column with a default value
                using (var cmd = new SQLiteCommand(@"
                    ALTER TABLE software_ref
                    ADD COLUMN Type NVARCHAR(50) NOT NULL DEFAULT 'Unknown'", connection))
                {
                    cmd.ExecuteNonQuery();
                }

                // Create index on Type column for performance
                using (var cmd = new SQLiteCommand(@"
                    CREATE INDEX IF NOT EXISTS IX_SoftwareRef_Type
                    ON software_ref(Type)", connection))
                {
                    cmd.ExecuteNonQuery();
                }

                log.Info("Type column added successfully to software_ref table");
            }
            catch (Exception ex)
            {
                log.Error($"Error adding Type column: {ex.Message}", ex);
                throw;
            }
        }
    }
}
