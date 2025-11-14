using NoFencesDataLayer.MasterCatalog.Entities;
using System.Collections.Generic;

namespace NoFencesDataLayer.Repositories
{
    /// <summary>
    /// Repository for SoftwareReference table in master_catalog.db.
    /// Manages shareable software/game reference data with enriched metadata.
    /// </summary>
    public interface ISoftwareReferenceRepository
    {
        /// <summary>
        /// Gets a software reference by its database ID.
        /// Used for JOIN operations when converting to Core model.
        /// </summary>
        /// <param name="id">Software reference ID</param>
        /// <returns>Software reference or null if not found</returns>
        SoftwareReference GetById(long id);

        /// <summary>
        /// Finds a software reference by source and external ID.
        /// This is the primary lookup method for platform software (Steam, GOG, etc.)
        /// </summary>
        /// <param name="source">Platform/source (e.g., "Steam", "GOG", "Epic")</param>
        /// <param name="externalId">Platform-specific ID (e.g., Steam AppID "440")</param>
        /// <returns>Existing reference or null if not found</returns>
        SoftwareReference FindByExternalId(string source, string externalId);

        /// <summary>
        /// Finds a software reference by name (fallback for software without external ID).
        /// Used for Registry-detected software or software without platform IDs.
        /// </summary>
        /// <param name="name">Software name</param>
        /// <param name="source">Optional source filter</param>
        /// <returns>Existing reference or null if not found</returns>
        SoftwareReference FindByName(string name, string source = null);

        /// <summary>
        /// Inserts a new software reference entry.
        /// </summary>
        /// <param name="reference">Software reference to insert</param>
        /// <returns>Inserted reference with assigned ID</returns>
        SoftwareReference Insert(SoftwareReference reference);

        /// <summary>
        /// Updates an existing software reference (typically after enrichment).
        /// </summary>
        /// <param name="reference">Software reference to update</param>
        void Update(SoftwareReference reference);

        /// <summary>
        /// Finds or creates a software reference entry.
        /// If entry exists, returns it. If not, creates new entry.
        /// This is the main method used during software detection.
        /// </summary>
        /// <param name="name">Software name</param>
        /// <param name="source">Platform/source</param>
        /// <param name="externalId">External platform ID (nullable)</param>
        /// <param name="category">Software category</param>
        /// <returns>Existing or newly created reference</returns>
        SoftwareReference FindOrCreate(string name, string source, string externalId, string category);

        /// <summary>
        /// Gets all software references that haven't been enriched or need re-enrichment.
        /// </summary>
        /// <param name="maxAge">Maximum age in days for last enrichment</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <returns>List of references needing enrichment</returns>
        List<SoftwareReference> GetUnenrichedEntries(int maxAge = 30, int maxResults = 100);

        /// <summary>
        /// Gets all software references (for UI display, export, etc.)
        /// </summary>
        /// <returns>All software references</returns>
        List<SoftwareReference> GetAllEntries();

        /// <summary>
        /// Gets software references filtered by software type.
        /// </summary>
        /// <param name="softwareType">Software type (Game, Application, Tool, Utility)</param>
        /// <returns>Filtered list of software references</returns>
        List<SoftwareReference> GetByType(string softwareType);

        /// <summary>
        /// Gets software references filtered by software type and category.
        /// </summary>
        /// <param name="softwareType">Software type</param>
        /// <param name="category">Category</param>
        /// <returns>Filtered list of software references</returns>
        List<SoftwareReference> GetByTypeAndCategory(string softwareType, string category);
    }
}
