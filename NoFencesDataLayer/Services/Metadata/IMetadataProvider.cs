using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NoFencesDataLayer.Services.Metadata
{
    /// <summary>
    /// Base interface for all metadata providers (games and software).
    /// </summary>
    public interface IMetadataProvider
    {
        /// <summary>
        /// Gets the name of this provider (e.g., "RAWG", "Winget", "CNET").
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Checks if the provider is configured and available for use.
        /// </summary>
        /// <returns>True if API keys are configured and service is reachable</returns>
        bool IsAvailable();

        /// <summary>
        /// Gets the priority of this provider (lower number = higher priority).
        /// Used for fallback ordering when multiple providers are available.
        /// </summary>
        int Priority { get; }
    }

    /// <summary>
    /// Metadata result containing enriched software/game information.
    /// </summary>
    public class MetadataResult
    {
        /// <summary>
        /// Name of the software/game.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Publisher or developer name.
        /// </summary>
        public string Publisher { get; set; }

        /// <summary>
        /// Comma-separated list of developers (for games).
        /// </summary>
        public string Developers { get; set; }

        /// <summary>
        /// Description or summary.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Comma-separated genres or categories.
        /// </summary>
        public string Genres { get; set; }

        /// <summary>
        /// Release date.
        /// </summary>
        public DateTime? ReleaseDate { get; set; }

        /// <summary>
        /// URL to icon or cover image.
        /// </summary>
        public string IconUrl { get; set; }

        /// <summary>
        /// URL to background image (for games).
        /// </summary>
        public string BackgroundImageUrl { get; set; }

        /// <summary>
        /// Rating (e.g., 4.5 out of 5).
        /// </summary>
        public double? Rating { get; set; }

        /// <summary>
        /// Official website URL.
        /// </summary>
        public string WebsiteUrl { get; set; }

        /// <summary>
        /// Provider that returned this metadata.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Additional metadata in key-value format.
        /// </summary>
        public Dictionary<string, string> AdditionalData { get; set; }

        /// <summary>
        /// Confidence score (0.0 to 1.0) indicating how well the result matches the query.
        /// </summary>
        public double Confidence { get; set; }

        public MetadataResult()
        {
            AdditionalData = new Dictionary<string, string>();
            Confidence = 1.0;
        }
    }
}
