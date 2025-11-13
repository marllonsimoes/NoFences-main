using System.Threading.Tasks;

namespace NoFencesDataLayer.Services.Metadata
{
    /// <summary>
    /// Interface for software metadata providers (Winget, CNET, Wikipedia, etc.).
    /// Session 11: Software metadata enrichment.
    /// </summary>
    public interface ISoftwareMetadataProvider : IMetadataProvider
    {
        /// <summary>
        /// Searches for software by name.
        /// </summary>
        /// <param name="softwareName">Name of the software to search for</param>
        /// <returns>Metadata result or null if not found</returns>
        Task<MetadataResult> SearchByNameAsync(string softwareName);

        /// <summary>
        /// Searches for software by name and publisher (more accurate matching).
        /// </summary>
        /// <param name="softwareName">Name of the software</param>
        /// <param name="publisher">Publisher or developer name</param>
        /// <returns>Metadata result or null if not found</returns>
        Task<MetadataResult> SearchByNameAndPublisherAsync(string softwareName, string publisher);
    }
}
