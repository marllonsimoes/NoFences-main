using NoFences.Core.Model;
using System;
using System.Collections.Generic;

namespace NoFencesDataLayer.Repositories
{
    /// <summary>
    /// Repository interface for FenceInfo persistence.
    /// Supports multiple storage backends (XML, SQLite, etc.)
    /// </summary>
    public interface IFenceRepository
    {
        /// <summary>
        /// Loads all fences from storage.
        /// </summary>
        /// <returns>List of all fences, or empty list if none exist</returns>
        IEnumerable<FenceInfo> GetAll();

        /// <summary>
        /// Loads a single fence by ID.
        /// </summary>
        /// <param name="id">Fence ID</param>
        /// <returns>FenceInfo if found, null otherwise</returns>
        FenceInfo GetById(Guid id);

        /// <summary>
        /// Saves a fence (creates new or updates existing).
        /// </summary>
        /// <param name="fence">Fence to save</param>
        /// <returns>True if successful, false otherwise</returns>
        bool Save(FenceInfo fence);

        /// <summary>
        /// Deletes a fence from storage.
        /// </summary>
        /// <param name="id">Fence ID to delete</param>
        /// <returns>True if deleted, false if not found or error</returns>
        bool Delete(Guid id);

        /// <summary>
        /// Checks if a fence exists in storage.
        /// </summary>
        /// <param name="id">Fence ID</param>
        /// <returns>True if exists, false otherwise</returns>
        bool Exists(Guid id);

        /// <summary>
        /// Gets the count of all fences in storage.
        /// </summary>
        /// <returns>Number of fences</returns>
        int Count();
    }
}
