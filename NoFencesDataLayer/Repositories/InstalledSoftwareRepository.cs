using log4net;
using NoFencesDataLayer.MasterCatalog.Entities;
using NoFencesService.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace NoFencesDataLayer.Repositories
{
    /// <summary>
    /// Repository implementation for tracking installed software on the user's machine.
    /// Uses LocalDBContext with ref.db.
    /// Stores machine-specific installation data with foreign key to SoftwareReference.
    /// Part of hybrid architecture: Detectors populate DB, fences query DB.
    /// </summary>
    public class InstalledSoftwareRepository : IInstalledSoftwareRepository
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(InstalledSoftwareRepository));
        private static bool databaseInitialized = false;
        private static readonly object initLock = new object();

        /// <summary>
        /// Ensures the local database (ref.db) and InstalledSoftware table exist.
        /// Uses LocalDBContext.
        /// Called before first database operation.
        /// </summary>
        private void EnsureDatabaseCreated()
        {
            if (databaseInitialized)
                return;

            lock (initLock)
            {
                if (databaseInitialized)
                    return;

                try
                {
                    log.Debug("Ensuring local database (ref.db) is initialized...");
                    using (var context = new LocalDBContext())
                    {
                        // Force EF to create database/tables if they don't exist
                        context.Database.Initialize(force: false);

                        // Verify InstalledSoftware table exists by trying to query it
                        var count = context.InstalledSoftware.Count();
                        log.Debug($"Local database (ref.db) initialized successfully (InstalledSoftware count: {count})");
                    }
                    databaseInitialized = true;
                }
                catch (Exception ex)
                {
                    log.Error($"Error ensuring database created: {ex.Message}", ex);
                    // Don't set databaseInitialized = true on error, will retry next time
                }
            }
        }

        /// <summary>
        /// Gets all installed software entries.
        /// Returns local installation data only (no Name field).
        /// To get full data with Name/Publisher/etc, use InstalledSoftwareService.QueryInstalledSoftware()
        /// which performs JOIN with software_ref table.
        /// </summary>
        public List<LocalInstallation> GetAll()
        {
            EnsureDatabaseCreated();

            try
            {
                using (var context = new LocalDBContext())
                {
                    var entries = context.InstalledSoftware
                        .OrderBy(s => s.SoftwareRefId)
                        .ThenBy(s => s.InstallLocation)
                        .ToList();

                    log.Debug($"Retrieved {entries.Count} installed software entries");
                    return entries;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error getting all installed software: {ex.Message}", ex);
                return new List<LocalInstallation>();
            }
        }

        /// <summary>
        /// Inserts or updates an installed software entry.
        /// Updated for two-tier architecture - only updates local installation data.
        /// Matches by SoftwareRefId + InstallLocation (unique constraint).
        /// </summary>
        public LocalInstallation Upsert(LocalInstallation entry)
        {
            if (entry == null)
            {
                log.Warn("Upsert called with null entry");
                return null;
            }

            if (entry.SoftwareRefId == 0)
            {
                log.Error("Upsert called with invalid SoftwareRefId (0)");
                return null;
            }

            try
            {
                using (var context = new LocalDBContext())
                {
                    // Match by SoftwareRefId + InstallLocation (unique constraint)
                    LocalInstallation existing = null;

                    if (!string.IsNullOrEmpty(entry.InstallLocation))
                    {
                        existing = context.InstalledSoftware
                            .FirstOrDefault(s => s.SoftwareRefId == entry.SoftwareRefId &&
                                                s.InstallLocation == entry.InstallLocation);
                    }

                    // Fallback: match by SoftwareRefId + ExecutablePath
                    if (existing == null && !string.IsNullOrEmpty(entry.ExecutablePath))
                    {
                        existing = context.InstalledSoftware
                            .FirstOrDefault(s => s.SoftwareRefId == entry.SoftwareRefId &&
                                                s.ExecutablePath == entry.ExecutablePath);
                    }

                    if (existing != null)
                    {
                        // Update existing entry (only local installation data)
                        existing.InstallLocation = entry.InstallLocation;
                        existing.ExecutablePath = entry.ExecutablePath;
                        existing.IconPath = entry.IconPath;
                        existing.RegistryKey = entry.RegistryKey;
                        existing.Version = entry.Version;
                        existing.InstallDate = entry.InstallDate;
                        existing.SizeBytes = entry.SizeBytes;
                        existing.LastDetected = DateTime.UtcNow;
                        existing.UpdatedAt = DateTime.UtcNow;

                        log.Debug($"Updated existing entry: SoftwareRefId={entry.SoftwareRefId} (ID: {existing.Id})");
                    }
                    else
                    {
                        // Insert new entry
                        entry.CreatedAt = DateTime.UtcNow;
                        entry.UpdatedAt = DateTime.UtcNow;
                        entry.LastDetected = DateTime.UtcNow;

                        context.InstalledSoftware.Add(entry);
                        log.Debug($"Inserted new entry: SoftwareRefId={entry.SoftwareRefId}");
                    }

                    context.SaveChanges();
                    return existing ?? entry;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error upserting installed software (SoftwareRefId={entry.SoftwareRefId}): {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Batch upsert for multiple entries (more efficient than individual upserts).
        /// Updated for two-tier architecture - only updates local installation data.
        /// </summary>
        public void UpsertBatch(List<LocalInstallation> entries)
        {
            EnsureDatabaseCreated();

            if (entries == null || entries.Count == 0)
            {
                log.Debug("UpsertBatch called with null or empty list");
                return;
            }

            try
            {
                using (var context = new LocalDBContext())
                {
                    int insertCount = 0;
                    int updateCount = 0;

                    foreach (var entry in entries)
                    {
                        if (entry.SoftwareRefId == 0)
                        {
                            log.Warn("Skipping entry with invalid SoftwareRefId (0)");
                            continue;
                        }

                        // Match by SoftwareRefId + InstallLocation (unique constraint)
                        LocalInstallation existing = null;

                        if (!string.IsNullOrEmpty(entry.InstallLocation))
                        {
                            existing = context.InstalledSoftware
                                .FirstOrDefault(s => s.SoftwareRefId == entry.SoftwareRefId &&
                                                    s.InstallLocation == entry.InstallLocation);
                        }

                        // Fallback: match by SoftwareRefId + ExecutablePath
                        if (existing == null && !string.IsNullOrEmpty(entry.ExecutablePath))
                        {
                            existing = context.InstalledSoftware
                                .FirstOrDefault(s => s.SoftwareRefId == entry.SoftwareRefId &&
                                                    s.ExecutablePath == entry.ExecutablePath);
                        }

                        if (existing != null)
                        {
                            // Update (only local installation data)
                            existing.InstallLocation = entry.InstallLocation;
                            existing.ExecutablePath = entry.ExecutablePath;
                            existing.IconPath = entry.IconPath;
                            existing.RegistryKey = entry.RegistryKey;
                            existing.Version = entry.Version;
                            existing.InstallDate = entry.InstallDate;
                            existing.SizeBytes = entry.SizeBytes;
                            existing.LastDetected = DateTime.UtcNow;
                            existing.UpdatedAt = DateTime.UtcNow;

                            updateCount++;
                        }
                        else
                        {
                            // Insert
                            entry.CreatedAt = DateTime.UtcNow;
                            entry.UpdatedAt = DateTime.UtcNow;
                            entry.LastDetected = DateTime.UtcNow;

                            context.InstalledSoftware.Add(entry);
                            insertCount++;
                        }
                    }

                    context.SaveChanges();
                    log.Info($"Batch upsert complete: {insertCount} inserted, {updateCount} updated");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error in batch upsert: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Removes entries that haven't been detected since the specified date.
        /// </summary>
        public int RemoveStaleEntries(DateTime olderThan)
        {
            EnsureDatabaseCreated();

            try
            {
                using (var context = new LocalDBContext())
                {
                    var staleEntries = context.InstalledSoftware
                        .Where(s => s.LastDetected < olderThan)
                        .ToList();

                    if (staleEntries.Count > 0)
                    {
                        context.InstalledSoftware.RemoveRange(staleEntries);
                        context.SaveChanges();

                        log.Info($"Removed {staleEntries.Count} stale entries (not detected since {olderThan:yyyy-MM-dd HH:mm:ss})");
                        return staleEntries.Count;
                    }

                    return 0;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error removing stale entries: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// Gets the total count of installed software entries.
        /// </summary>
        public int GetCount()
        {
            EnsureDatabaseCreated();

            try
            {
                using (var context = new LocalDBContext())
                {
                    return context.InstalledSoftware.Count();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error getting count: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// Gets all installed software with enriched metadata (performs JOIN).
        /// Session 14: Repository JOIN helper method.
        /// </summary>
        public List<NoFences.Core.Model.InstalledSoftware> GetAllWithMetadata(ISoftwareReferenceRepository softwareRefRepository)
        {
            EnsureDatabaseCreated();

            try
            {
                using (var localContext = new LocalDBContext())
                {
                    var localInstalls = localContext.InstalledSoftware.ToList();

                    if (localInstalls.Count == 0)
                    {
                        log.Debug("No local installations found");
                        return new List<NoFences.Core.Model.InstalledSoftware>();
                    }

                    // Get all referenced software IDs
                    var softwareRefIds = localInstalls.Select(l => l.SoftwareRefId).Distinct().ToList();

                    // Batch fetch all software references
                    var references = new Dictionary<long, MasterCatalog.Entities.SoftwareReference>();
                    foreach (var refId in softwareRefIds)
                    {
                        var reference = softwareRefRepository.GetById(refId);
                        if (reference != null)
                        {
                            references[refId] = reference;
                        }
                    }

                    // Perform in-memory JOIN using factory method
                    var result = new List<NoFences.Core.Model.InstalledSoftware>();
                    foreach (var local in localInstalls)
                    {
                        if (references.TryGetValue(local.SoftwareRefId, out var reference))
                        {
                            var software = NoFences.Core.Model.InstalledSoftware.FromJoin(local, reference);
                            if (software != null)
                            {
                                result.Add(software);
                            }
                        }
                        else
                        {
                            log.Warn($"SoftwareReference {local.SoftwareRefId} not found for local installation {local.Id}");
                        }
                    }

                    log.Debug($"GetAllWithMetadata: Returned {result.Count} entries with metadata");
                    return result;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error in GetAllWithMetadata: {ex.Message}", ex);
                return new List<NoFences.Core.Model.InstalledSoftware>();
            }
        }

        /// <summary>
        /// Gets filtered installed software with enriched metadata (performs JOIN).
        /// Session 14: Repository JOIN helper method with filtering.
        /// </summary>
        public List<NoFences.Core.Model.InstalledSoftware> GetFilteredWithMetadata(
            ISoftwareReferenceRepository softwareRefRepository,
            string category = null,
            string source = null)
        {
            // Get all with metadata first
            var allSoftware = GetAllWithMetadata(softwareRefRepository);

            // Apply filters if specified
            if (!string.IsNullOrEmpty(category))
            {
                allSoftware = allSoftware.Where(s => s.Category.ToString() == category).ToList();
            }

            if (!string.IsNullOrEmpty(source))
            {
                allSoftware = allSoftware.Where(s => s.Source == source).ToList();
            }

            log.Debug($"GetFilteredWithMetadata: Returned {allSoftware.Count} entries (category={category}, source={source})");
            return allSoftware;
        }
    }
}
