using NoFences.Core.Util;
using System.Collections.Generic;

namespace NoFencesDataLayer.Repositories
{
    /// <summary>
    /// Repository interface for Amazon Games data access.
    /// Provides abstraction over Amazon Games SQLite database.
    /// Session 11: Created to separate data access from business logic.
    /// </summary>
    public interface IAmazonGamesRepository
    {
        /// <summary>
        /// Gets all installed Amazon Games from the database.
        /// </summary>
        /// <returns>List of GameInfo objects with full metadata</returns>
        List<GameInfo> GetInstalledGames();

        /// <summary>
        /// Checks if the Amazon Games database is accessible.
        /// </summary>
        /// <returns>True if database exists and can be opened</returns>
        bool IsAvailable();

        /// <summary>
        /// Gets the path to the Amazon Games SQLite database.
        /// </summary>
        /// <returns>Full path to CommonData.sqlite, or null if not found</returns>
        string GetDatabasePath();
    }
}
