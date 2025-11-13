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
        List<InstalledSoftwareEntry> GetAll();

        /// <summary>
        /// Inserts or updates an installed software entry.
        /// If entry with same ExecutablePath exists, updates it.
        /// Otherwise, creates new entry.
        /// </summary>
        /// <param name="entry">Software entry to upsert</param>
        /// <returns>Updated or inserted entry with ID</returns>
        InstalledSoftwareEntry Upsert(InstalledSoftwareEntry entry);

        /// <summary>
        /// Batch upsert for multiple entries (more efficient than individual upserts).
        /// </summary>
        /// <param name="entries">List of entries to upsert</param>
        void UpsertBatch(List<InstalledSoftwareEntry> entries);

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
    }
}
