using System;
using System.Collections.Generic;

namespace NoFencesDataLayer.Model
{
    /// <summary>
    /// Root container for the software catalog JSON format.
    /// This represents the normalized, structured catalog data.
    /// </summary>
    public class SoftwareCatalogJson
    {
        /// <summary>
        /// Catalog metadata
        /// </summary>
        public CatalogMetadata Metadata { get; set; }

        /// <summary>
        /// List of general software entries
        /// </summary>
        public List<SoftwareEntry> Software { get; set; } = new List<SoftwareEntry>();

        /// <summary>
        /// List of Steam game entries
        /// </summary>
        public List<SteamGameEntry> SteamGames { get; set; } = new List<SteamGameEntry>();
    }

    /// <summary>
    /// Metadata about the catalog
    /// </summary>
    public class CatalogMetadata
    {
        /// <summary>
        /// Catalog version (e.g., "1.0.0")
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Date when catalog was generated (ISO 8601)
        /// </summary>
        public DateTime GeneratedDate { get; set; }

        /// <summary>
        /// Total number of software entries
        /// </summary>
        public int TotalSoftware { get; set; }

        /// <summary>
        /// Total number of Steam games
        /// </summary>
        public int TotalSteamGames { get; set; }

        /// <summary>
        /// Description of this catalog version
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Normalized software entry
    /// </summary>
    public class SoftwareEntry
    {
        /// <summary>
        /// Unique identifier (generated from name + company)
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Software name (required)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Company/publisher name
        /// </summary>
        public string Company { get; set; }

        /// <summary>
        /// Software category (Games, Development, OfficeProductivity, etc.)
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// License type (Free, Paid, OpenSource, Freemium, etc.)
        /// </summary>
        public string License { get; set; }

        /// <summary>
        /// Brief description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Official website URL
        /// </summary>
        public string Website { get; set; }

        /// <summary>
        /// Icon URL (optional)
        /// </summary>
        public string IconUrl { get; set; }

        /// <summary>
        /// Alternative names / aliases
        /// </summary>
        public List<string> Aliases { get; set; } = new List<string>();

        /// <summary>
        /// Tags for better categorization
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
    }

    /// <summary>
    /// Normalized Steam game entry
    /// </summary>
    public class SteamGameEntry
    {
        /// <summary>
        /// Steam AppID (unique identifier)
        /// </summary>
        public int AppId { get; set; }

        /// <summary>
        /// Game name (required)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Release date (ISO 8601)
        /// </summary>
        public string ReleaseDate { get; set; }

        /// <summary>
        /// Developer(s)
        /// </summary>
        public List<string> Developers { get; set; } = new List<string>();

        /// <summary>
        /// Publisher(s)
        /// </summary>
        public List<string> Publishers { get; set; } = new List<string>();

        /// <summary>
        /// Game genres
        /// </summary>
        public List<string> Genres { get; set; } = new List<string>();

        /// <summary>
        /// User tags
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Header image URL
        /// </summary>
        public string HeaderImage { get; set; }

        /// <summary>
        /// Platform support
        /// </summary>
        public PlatformSupport Platforms { get; set; } = new PlatformSupport();

        /// <summary>
        /// Metacritic score (0-100)
        /// </summary>
        public int? MetacriticScore { get; set; }

        /// <summary>
        /// Number of positive reviews
        /// </summary>
        public int PositiveReviews { get; set; }

        /// <summary>
        /// Number of negative reviews
        /// </summary>
        public int NegativeReviews { get; set; }

        /// <summary>
        /// Price in USD
        /// </summary>
        public decimal? Price { get; set; }
    }

    /// <summary>
    /// Platform support flags
    /// </summary>
    public class PlatformSupport
    {
        public bool Windows { get; set; }
        public bool Mac { get; set; }
        public bool Linux { get; set; }
    }
}
