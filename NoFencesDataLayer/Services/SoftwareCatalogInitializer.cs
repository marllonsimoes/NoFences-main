using log4net;
using NoFencesDataLayer.MasterCatalog.Tools;
using System;
using System.Diagnostics;
using System.IO;

namespace NoFencesDataLayer.Services
{
    /// <summary>
    /// Utility for initializing the master catalog database.
    /// This is typically run once during first-time setup or when updating the catalog.
    ///
    /// The catalog is a single interchangeable database file that can be:
    /// - Built locally from CSV files
    /// - Downloaded from a remote server
    /// - Replaced when corrupted
    ///
    /// No migration needed - application reads directly from master catalog.
    /// </summary>
    public class SoftwareCatalogInitializer
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SoftwareCatalogInitializer));
        /// <summary>
        /// Gets the default path where the master catalog should be stored.
        /// </summary>
        public static string GetDefaultCatalogPath()
        {
            return SoftwareCatalogService.GetDefaultCatalogPath();
        }

        /// <summary>
        /// Checks if the master catalog exists and is valid.
        /// </summary>
        /// <returns>True if catalog exists and contains data, false otherwise</returns>
        public static bool IsCatalogInitialized()
        {
            try
            {
                var catalogPath = GetDefaultCatalogPath();

                if (!File.Exists(catalogPath))
                {
                    log.Warn($"Catalog not found at {catalogPath}");
                    return false;
                }

                // Check if catalog is valid and has data
                var service = new SoftwareCatalogService();
                bool available = service.IsCatalogAvailable();
                service.Dispose();

                return available;
            }
            catch (Exception ex)
            {
                log.Error($"Error checking catalog status: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets statistics about the current catalog.
        /// </summary>
        /// <returns>Catalog statistics or null if catalog is not initialized</returns>
        public static CatalogStatistics GetCatalogStatistics()
        {
            try
            {
                var catalogPath = GetDefaultCatalogPath();

                if (!File.Exists(catalogPath))
                    return null;

                var service = new SoftwareCatalogService();

                if (!service.IsCatalogAvailable())
                {
                    service.Dispose();
                    return null;
                }

                var stats = service.GetStatistics();
                service.Dispose();

                return stats;
            }
            catch (Exception ex)
            {
                log.Error($"Error getting catalog statistics: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Builds master catalog from CSV files in specified directory.
        /// This creates the catalog database that can be distributed to users.
        /// </summary>
        /// <param name="csvDirectory">Directory containing CSV files (Software.csv, steam.csv, etc.)</param>
        /// <param name="outputPath">Where to save the master catalog database (optional, uses default if not provided)</param>
        /// <param name="maxGames">Maximum number of games to import (optional, default 10000)</param>
        /// <returns>True if build was successful, false otherwise</returns>
        public static bool BuildCatalogFromCsv(string csvDirectory, string outputPath = null, int maxGames = 200_000)
        {
            if (!Directory.Exists(csvDirectory))
            {
                log.Warn($"CSV directory not found: {csvDirectory}");
                return false;
            }

            if (string.IsNullOrEmpty(outputPath))
                outputPath = GetDefaultCatalogPath();

            log.Debug($"Building catalog from {csvDirectory} to {outputPath}");

            try
            {
                // Use the catalog import command to build the database
                var args = new string[] { "--import-catalog", csvDirectory, outputPath, maxGames.ToString() };
                var exitCode = CatalogImportCommand.Execute(args);

                bool success = exitCode == 0 && File.Exists(outputPath);

                if (success)
                {
                    log.Debug($"Catalog built successfully at {outputPath}");
                }
                else
                {
                    log.Error($"Failed to build catalog (exit code: {exitCode})");
                }

                return success;
            }
            catch (Exception ex)
            {
                log.Error($"Error building catalog: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Downloads master catalog from remote server.
        /// The catalog is a single interchangeable database file.
        /// </summary>
        /// <param name="catalogUrl">URL to master catalog database (optional, uses default if not provided)</param>
        /// <param name="destinationPath">Where to save the catalog (optional, uses default if not provided)</param>
        /// <returns>True if download was successful, false otherwise</returns>
        public static bool InitializeFromRemote(string catalogUrl = null, string destinationPath = null)
        {
            log.Debug("Starting remote catalog download");

            try
            {
                if (string.IsNullOrEmpty(destinationPath))
                    destinationPath = GetDefaultCatalogPath();

                // Create directory if it doesn't exist
                var directory = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Download catalog
                log.Debug($"Downloading to {destinationPath}");

                bool downloaded = CatalogDownloadService.DownloadCatalog(catalogUrl, destinationPath);
                if (!downloaded)
                {
                    log.Warn("Download failed");
                    return false;
                }

                log.Debug("Download complete");

                // Verify catalog is valid
                if (!IsCatalogInitialized())
                {
                    log.Warn("Downloaded catalog is invalid");

                    // Clean up invalid file
                    try
                    {
                        if (File.Exists(destinationPath))
                            File.Delete(destinationPath);
                    }
                    catch { /* Ignore cleanup errors */ }

                    return false;
                }

                var stats = GetCatalogStatistics();
                if (stats != null)
                {
                    log.Debug($"Catalog initialized successfully - {stats}");
                }

                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Remote initialization failed: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Tries to build catalog from local CSV files first, falls back to remote download if not found.
        /// </summary>
        /// <param name="csvDirectory">Directory containing CSV files (optional, tries default locations)</param>
        /// <returns>True if initialization was successful, false otherwise</returns>
        public static bool InitializeFromLocalOrRemote(string csvDirectory = null)
        {
            log.Debug("Attempting local build first");

            // Try to find CSV directory if not specified
            if (string.IsNullOrEmpty(csvDirectory))
            {
                csvDirectory = FindCsvDirectory();
            }

            // Try local build if CSV directory exists
            if (!string.IsNullOrEmpty(csvDirectory) && Directory.Exists(csvDirectory))
            {
                log.Debug($"Found CSV directory at {csvDirectory}");

                if (BuildCatalogFromCsv(csvDirectory))
                {
                    log.Debug("Local build succeeded");
                    return true;
                }
                else
                {
                    log.Warn("Local build failed");
                }
            }
            else
            {
                log.Warn("CSV directory not found, skipping local build");
            }

            // Fall back to remote download
            log.Debug("Falling back to remote download");
            return InitializeFromRemote();
        }

        /// <summary>
        /// Replaces the current catalog with a new one from remote server.
        /// Useful for updating the catalog or recovering from corruption.
        /// </summary>
        /// <param name="catalogUrl">URL to master catalog database (optional, uses default if not provided)</param>
        /// <returns>True if replacement was successful, false otherwise</returns>
        public static bool ReplaceCatalogFromRemote(string catalogUrl = null)
        {
            log.Debug("Replacing catalog from remote");

            var catalogPath = GetDefaultCatalogPath();

            // Backup existing catalog if it exists
            string backupPath = null;
            if (File.Exists(catalogPath))
            {
                backupPath = catalogPath + ".backup";
                try
                {
                    File.Copy(catalogPath, backupPath, true);
                    log.Debug($"Backed up existing catalog to {backupPath}");
                }
                catch (Exception ex)
                {
                    log.Error($"Failed to backup catalog: {ex.Message}", ex);
                }
            }

            // Try to download new catalog
            bool success = InitializeFromRemote(catalogUrl, catalogPath);

            if (success)
            {
                // Delete backup if successful
                if (File.Exists(backupPath))
                {
                    try
                    {
                        File.Delete(backupPath);
                        log.Debug("Deleted backup after successful replacement");
                    }
                    catch { /* Ignore cleanup errors */ }
                }
            }
            else
            {
                // Restore backup if download failed
                if (File.Exists(backupPath))
                {
                    try
                    {
                        File.Copy(backupPath, catalogPath, true);
                        File.Delete(backupPath);
                        log.Debug("Restored backup after failed replacement");
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Failed to restore backup: {ex.Message}", ex);
                    }
                }
            }

            return success;
        }

        #region Helper Methods

        /// <summary>
        /// Tries to find the _software_list directory in common locations.
        /// </summary>
        private static string FindCsvDirectory()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var searchPaths = new[]
            {
                Path.Combine(baseDirectory, "_software_list"),
                Path.Combine(baseDirectory, "..", "..", "_software_list"),
                Path.Combine(baseDirectory, "..", "..", "..", "_software_list"),
            };

            // Also try relative to base directory parent directories
            var current = new DirectoryInfo(baseDirectory);
            for (int i = 0; i < 5 && current != null; i++)
            {
                var testPath = Path.Combine(current.FullName, "_software_list");
                if (Directory.Exists(testPath))
                {
                    return Path.GetFullPath(testPath);
                }
                current = current.Parent;
            }

            foreach (var path in searchPaths)
            {
                var normalizedPath = Path.GetFullPath(path);
                if (Directory.Exists(normalizedPath))
                {
                    log.Debug($"Found CSV directory at {normalizedPath}");
                    return normalizedPath;
                }
            }

            log.Warn("CSV directory not found in any search location");
            return null;
        }

        #endregion
    }
}
