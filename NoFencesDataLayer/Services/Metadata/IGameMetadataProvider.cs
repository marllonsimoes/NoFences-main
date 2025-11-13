using System.Threading.Tasks;

namespace NoFencesDataLayer.Services.Metadata
{
    /// <summary>
    /// Interface for game metadata providers (RAWG, IGDB, etc.).
    /// Session 11: Game metadata enrichment.
    /// </summary>
    public interface IGameMetadataProvider : IMetadataProvider
    {
        /// <summary>
        /// Searches for a game by name.
        /// </summary>
        /// <param name="gameName">Name of the game to search for</param>
        /// <returns>Metadata result or null if not found</returns>
        Task<MetadataResult> SearchByNameAsync(string gameName);

        /// <summary>
        /// Gets game metadata by Steam AppID (if supported by provider).
        /// </summary>
        /// <param name="steamAppId">Steam application ID</param>
        /// <returns>Metadata result or null if not found</returns>
        Task<MetadataResult> GetBySteamAppIdAsync(int steamAppId);

        /// <summary>
        /// Gets game metadata by GOG game ID (if supported by provider).
        /// </summary>
        /// <param name="gogId">GOG game identifier</param>
        /// <returns>Metadata result or null if not found</returns>
        Task<MetadataResult> GetByGogIdAsync(string gogId);
    }
}
