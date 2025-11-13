using NoFencesDataLayer.MasterCatalog.Entities;
using System;
using System.Collections.Generic;

namespace NoFencesDataLayer.Repositories
{
    /// <summary>
    /// Repository interface for tracking installed software on the user's machine.
    /// Part of hybrid architecture: Detectors populate DB, fences query DB.
    /// Session 11: FilesFence data layer rework.
    /// </summary>
    public interface IInstalledSoftwareRepository
    {
        /// <summary>
        /// Gets all installed software entries.
        /// </summary>
        /// <returns>List of all installed software</returns>
        List<InstalledSoftwareEntry> GetAll();

        /// <summary>
        /// Gets installed software by detection source.
        /// </summary>
        /// <param name="source">Source identifier (Steam, GOG, Epic, Registry, etc.)</param>
        /// <returns>List of software from specified source</returns>
        List<InstalledSoftwareEntry> GetBySource(string source);

        /// <summary>
        /// Gets installed software by category.
        /// </summary>
        /// <param name="category">Category (Games, Productivity, Development, etc.)</param>
        /// <returns>List of software in specified category</returns>
        List<InstalledSoftwareEntry> GetByCategory(string category);

        /// <summary>
        /// Gets installed software by combined source and category filter.
        /// </summary>
        /// <param name="source">Source identifier (can be null to skip filter)</param>
        /// <param name="category">Category (can be null to skip filter)</param>
        /// <returns>List of matching software</returns>
        List<InstalledSoftwareEntry> GetBySourceAndCategory(string source, string category);

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

        /// <summary>
        /// Gets count by category for statistics.
        /// </summary>
        /// <returns>Dictionary of category name to count</returns>
        Dictionary<string, int> GetCountByCategory();

        /// <summary>
        /// Gets count by source for statistics.
        /// </summary>
        /// <returns>Dictionary of source name to count</returns>
        Dictionary<string, int> GetCountBySource();
    }
}
