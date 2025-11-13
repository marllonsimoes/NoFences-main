using NoFencesDataLayer.MasterCatalog.Entities;
using System;
using System.Collections.Generic;

namespace NoFencesDataLayer.Repositories
{
    /// <summary>
    /// Repository interface for tracking installed software on the user's machine.
    /// Part of hybrid architecture: Detectors populate DB, fences query DB.
    /// </summary>
    public interface IInstalledSoftwareRepository
    {
        /// <summary>
        /// Gets all installed software entries.
        /// </summary>
        /// <returns>List of all installed software</returns>
        List<LocalInstallation> GetAll();

        /// <summary>
        /// Inserts or updates an installed software entry.
        /// If entry with same ExecutablePath exists, updates it.
        /// Otherwise, creates new entry.
        /// </summary>
        /// <param name="entry">Software entry to upsert</param>
        /// <returns>Updated or inserted entry with ID</returns>
        LocalInstallation Upsert(LocalInstallation entry);

        /// <summary>
        /// Batch upsert for multiple entries (more efficient than individual upserts).
        /// </summary>
        /// <param name="entries">List of entries to upsert</param>
        void UpsertBatch(List<LocalInstallation> entries);

        /// <summary>
        /// Removes entries that haven't been detected since the specified date.
        /// Used to clean up uninstalled software.
        /// </summary>
        /// <param name="olderThan">Remove entries with LastDetected before this date</param>
        /// <returns>Number of entries removed</returns>
        int RemoveStaleEntries(DateTime olderThan);

        /// <summary>
        /// Gets the total count of installed software entries.
        /// </summary>
        /// <returns>Total count</returns>
        int GetCount();

        /// <summary>
        /// Gets all installed software with enriched metadata (performs JOIN with SoftwareReference).
        /// This is the primary method for UI display with complete data.
        /// Session 14: Repository JOIN helper method.
        /// </summary>
        /// <param name="softwareRefRepository">Software reference repository for JOIN</param>
        /// <returns>List of InstalledSoftware with complete data</returns>
        List<NoFences.Core.Model.InstalledSoftware> GetAllWithMetadata(ISoftwareReferenceRepository softwareRefRepository);

        /// <summary>
        /// Gets filtered installed software with enriched metadata (performs JOIN).
        /// Session 14: Repository JOIN helper method with filtering.
        /// </summary>
        /// <param name="softwareRefRepository">Software reference repository for JOIN</param>
        /// <param name="category">Category filter (optional)</param>
        /// <param name="source">Source filter (optional)</param>
        /// <returns>List of filtered InstalledSoftware with complete data</returns>
        List<NoFences.Core.Model.InstalledSoftware> GetFilteredWithMetadata(
            ISoftwareReferenceRepository softwareRefRepository,
            string category = null,
            string source = null);
    }
}
