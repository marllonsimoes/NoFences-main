using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace NoFencesDataLayer.Services.Metadata
{
    /// <summary>
    /// RAWG API client for game metadata enrichment.
    /// API Documentation: https://api.rawg.io/docs/
    /// Session 11: Game metadata collection.
    /// </summary>
    public class RawgApiClient : IGameMetadataProvider
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(RawgApiClient));
        private const string BASE_URL = "https://api.rawg.io/api";
        private readonly string apiKey;
        private static readonly HttpClient httpClient = new HttpClient();

        public string ProviderName => "RAWG";
        public int Priority => 1; // Highest priority for games

        /// <summary>
        /// Constructor with API key.
        /// </summary>
        /// <param name="apiKey">RAWG API key (get from https://rawg.io/apidocs)</param>
        public RawgApiClient(string apiKey)
        {
            this.apiKey = apiKey;
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NoFences/1.6.2");
        }

        /// <summary>
        /// Default constructor - reads API key from configuration.
        /// </summary>
        public RawgApiClient() : this(GetApiKeyFromConfig())
        {
        }

        public bool IsAvailable()
        {
            return !string.IsNullOrEmpty(apiKey);
        }

        /// <summary>
        /// Searches for a game by name.
        /// </summary>
        public async Task<MetadataResult> SearchByNameAsync(string gameName)
        {
            if (!IsAvailable())
            {
                log.Warn("RAWG API key not configured");
                return null;
            }

            if (string.IsNullOrWhiteSpace(gameName))
                return null;

            try
            {
                // Clean game name (remove year suffixes, special editions, etc.)
                string cleanName = CleanGameName(gameName);

                // RAWG search endpoint
                string url = $"{BASE_URL}/games?key={apiKey}&search={HttpUtility.UrlEncode(cleanName)}&page_size=5";

                log.Debug($"RAWG: Searching for game '{cleanName}'");

                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(json);

                var results = data["results"] as JArray;
                if (results == null || results.Count == 0)
                {
                    log.Debug($"RAWG: No results found for '{cleanName}'");
                    return null;
                }

                // Get the first result (usually most relevant)
                var game = results[0] as JObject;
                var metadata = ParseGameData(game, gameName);

                // Get detailed information
                int gameId = game["id"]?.Value<int>() ?? 0;
                if (gameId > 0)
                {
                    await EnrichWithDetails(gameId, metadata);
                }

                log.Info($"RAWG: Found metadata for '{gameName}' -> '{metadata.Name}' (Confidence: {metadata.Confidence:F2})");
                return metadata;
            }
            catch (Exception ex)
            {
                log.Error($"RAWG: Error searching for game '{gameName}': {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Gets game metadata by Steam AppID.
        /// RAWG supports Steam ID lookups.
        /// </summary>
        public async Task<MetadataResult> GetBySteamAppIdAsync(int steamAppId)
        {
            if (!IsAvailable())
                return null;

            try
            {
                // RAWG can filter by Steam ID
                string url = $"{BASE_URL}/games?key={apiKey}&stores=1&search={steamAppId}";

                log.Debug($"RAWG: Looking up Steam AppID {steamAppId}");

                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(json);

                var results = data["results"] as JArray;
                if (results == null || results.Count == 0)
                    return null;

                var game = results[0] as JObject;
                var metadata = ParseGameData(game, expectedName: null); // No name match check for Steam AppID lookup

                log.Info($"RAWG: Found game by Steam AppID {steamAppId} -> '{metadata.Name}' (Confidence: {metadata.Confidence:F2})");
                return metadata;
            }
            catch (Exception ex)
            {
                log.Error($"RAWG: Error looking up Steam AppID {steamAppId}: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Gets game metadata by GOG ID (not directly supported by RAWG).
        /// </summary>
        public async Task<MetadataResult> GetByGogIdAsync(string gogId)
        {
            // RAWG doesn't directly support GOG IDs, would need to search by name
            log.Debug($"RAWG: GOG ID lookup not supported, gogId={gogId}");
            return null;
        }

        /// <summary>
        /// Enriches metadata with detailed game information.
        /// </summary>
        private async Task EnrichWithDetails(int gameId, MetadataResult metadata)
        {
            try
            {
                string url = $"{BASE_URL}/games/{gameId}?key={apiKey}";

                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var game = JObject.Parse(json);

                // Enhanced description
                string description = game["description_raw"]?.ToString();
                if (!string.IsNullOrEmpty(description))
                {
                    metadata.Description = description;
                }

                // Website
                string website = game["website"]?.ToString();
                if (!string.IsNullOrEmpty(website))
                {
                    metadata.WebsiteUrl = website;
                }

                // Developers
                var developers = game["developers"] as JArray;
                if (developers != null && developers.Count > 0)
                {
                    metadata.Developers = string.Join(", ", developers.Select(d => d["name"]?.ToString()));
                }

                // Publishers
                var publishers = game["publishers"] as JArray;
                if (publishers != null && publishers.Count > 0)
                {
                    metadata.Publisher = publishers[0]["name"]?.ToString();
                }

                // Additional metadata
                metadata.AdditionalData["metacritic"] = game["metacritic"]?.ToString() ?? "";
                metadata.AdditionalData["playtime"] = game["playtime"]?.ToString() ?? "";

                // Session 12: Fix JSON parsing error - check if esrb_rating is an object before accessing child
                var esrbRating = game["esrb_rating"];
                if (esrbRating != null && esrbRating.Type == JTokenType.Object)
                {
                    metadata.AdditionalData["esrb_rating"] = esrbRating["name"]?.ToString() ?? "";
                }
                else
                {
                    metadata.AdditionalData["esrb_rating"] = "";
                }

                log.Debug($"RAWG: Enriched details for game ID {gameId}");
            }
            catch (Exception ex)
            {
                log.Warn($"RAWG: Failed to enrich details for game ID {gameId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses game data from RAWG API response.
        /// Session 12: Now calculates confidence based on name matching quality instead of ratings count.
        /// </summary>
        /// <param name="game">RAWG API game object</param>
        /// <param name="expectedName">Expected game name to match against (null = skip name matching)</param>
        private MetadataResult ParseGameData(JObject game, string expectedName = null)
        {
            var metadata = new MetadataResult
            {
                Source = ProviderName,
                Name = game["name"]?.ToString(),
                ReleaseDate = ParseDate(game["released"]?.ToString()),
                Rating = game["rating"]?.Value<double?>(),
                IconUrl = game["background_image"]?.ToString(),
                BackgroundImageUrl = game["background_image"]?.ToString()
            };

            // Genres
            var genres = game["genres"] as JArray;
            if (genres != null && genres.Count > 0)
            {
                metadata.Genres = string.Join(", ", genres.Select(g => g["name"]?.ToString()));
            }

            // Short description
            metadata.Description = game["short_description"]?.ToString() ?? "";

            // Session 12: Calculate confidence based on name matching quality
            // If expectedName is provided, compare it with the returned game name
            if (!string.IsNullOrEmpty(expectedName) && !string.IsNullOrEmpty(metadata.Name))
            {
                metadata.Confidence = CalculateNameSimilarity(expectedName, metadata.Name);
            }
            else
            {
                // No name to compare (e.g., Steam AppID lookup) - assume high confidence
                metadata.Confidence = 0.95;
            }

            return metadata;
        }

        /// <summary>
        /// Cleans game name for better search results.
        /// Removes year suffixes, special editions, etc.
        /// </summary>
        private string CleanGameName(string gameName)
        {
            if (string.IsNullOrEmpty(gameName))
                return gameName;

            // Remove common suffixes
            string[] suffixesToRemove = {
                " - Game of the Year Edition",
                " - Definitive Edition",
                " - Complete Edition",
                " - Enhanced Edition",
                " - Remastered",
                " GOTY",
                " Deluxe Edition",
                " Gold Edition"
            };

            string cleaned = gameName;
            foreach (var suffix in suffixesToRemove)
            {
                if (cleaned.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned.Substring(0, cleaned.Length - suffix.Length).Trim();
                }
            }

            // Remove year in parentheses at the end (e.g., "Game Name (2023)")
            if (cleaned.EndsWith(")"))
            {
                int openParen = cleaned.LastIndexOf('(');
                if (openParen > 0)
                {
                    string yearPart = cleaned.Substring(openParen + 1, cleaned.Length - openParen - 2).Trim();
                    if (int.TryParse(yearPart, out int year) && year >= 1970 && year <= DateTime.Now.Year + 2)
                    {
                        cleaned = cleaned.Substring(0, openParen).Trim();
                    }
                }
            }

            return cleaned;
        }

        /// <summary>
        /// Parses date string to DateTime.
        /// </summary>
        private DateTime? ParseDate(string dateString)
        {
            if (string.IsNullOrEmpty(dateString))
                return null;

            if (DateTime.TryParse(dateString, out DateTime result))
                return result;

            return null;
        }

        /// <summary>
        /// Gets RAWG API key from user preferences.
        /// </summary>
        private static string GetApiKeyFromConfig()
        {
            try
            {
                // Try UserPreferences first (preferred method)
                var preferences = NoFences.Core.Settings.UserPreferences.Load();
                if (!string.IsNullOrEmpty(preferences?.RawgApiKey))
                {
                    return preferences.RawgApiKey;
                }

                // Fallback to app settings for backward compatibility
                string key = System.Configuration.ConfigurationManager.AppSettings["RawgApiKey"];
                return key;
            }
            catch (Exception ex)
            {
                log.Warn($"Failed to read RAWG API key from config: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Calculates similarity between two game names using Levenshtein distance.
        /// Returns a score between 0.0 (completely different) and 1.0 (exact match).
        /// Session 12: Used for confidence scoring based on name matching quality.
        /// </summary>
        /// <param name="name1">First name (expected)</param>
        /// <param name="name2">Second name (found)</param>
        /// <returns>Similarity score (0.0 to 1.0)</returns>
        private double CalculateNameSimilarity(string name1, string name2)
        {
            if (string.IsNullOrEmpty(name1) || string.IsNullOrEmpty(name2))
                return 0.0;

            // Normalize for comparison (lowercase, trim)
            string n1 = name1.Trim().ToLowerInvariant();
            string n2 = name2.Trim().ToLowerInvariant();

            // Exact match
            if (n1 == n2)
                return 1.0;

            // Calculate Levenshtein distance
            int distance = LevenshteinDistance(n1, n2);
            int maxLength = Math.Max(n1.Length, n2.Length);

            // Convert distance to similarity (0.0 to 1.0)
            double similarity = 1.0 - ((double)distance / maxLength);

            return Math.Max(0.0, similarity);
        }

        /// <summary>
        /// Calculates Levenshtein distance between two strings.
        /// The Levenshtein distance is the minimum number of edits (insertions, deletions, substitutions)
        /// needed to transform one string into another.
        /// </summary>
        private int LevenshteinDistance(string s1, string s2)
        {
            int len1 = s1.Length;
            int len2 = s2.Length;
            int[,] matrix = new int[len1 + 1, len2 + 1];

            // Initialize first column and row
            for (int i = 0; i <= len1; i++)
                matrix[i, 0] = i;
            for (int j = 0; j <= len2; j++)
                matrix[0, j] = j;

            // Calculate distances
            for (int i = 1; i <= len1; i++)
            {
                for (int j = 1; j <= len2; j++)
                {
                    int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;

                    matrix[i, j] = Math.Min(
                        Math.Min(
                            matrix[i - 1, j] + 1,      // Deletion
                            matrix[i, j - 1] + 1),      // Insertion
                        matrix[i - 1, j - 1] + cost);   // Substitution
                }
            }

            return matrix[len1, len2];
        }
    }
}
