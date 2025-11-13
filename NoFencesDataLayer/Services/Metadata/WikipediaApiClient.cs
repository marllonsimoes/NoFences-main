using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace NoFencesDataLayer.Services.Metadata
{
    /// <summary>
    /// Wikipedia API client for fallback software/game metadata.
    /// Uses MediaWiki API to search and extract information.
    /// API Documentation: https://www.mediawiki.org/wiki/API:Main_page
    /// </summary>
    public class WikipediaApiClient : ISoftwareMetadataProvider
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(WikipediaApiClient));
        private const string BASE_URL = "https://en.wikipedia.org/w/api.php";
        private static readonly HttpClient httpClient = new HttpClient();

        public string ProviderName => "Wikipedia";
        public int Priority => 99; // Lowest priority (fallback only)

        public WikipediaApiClient()
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NoFences/1.6.2 (contact@example.com)");
        }

        public bool IsAvailable()
        {
            // Wikipedia API is public, always available
            return true;
        }

        /// <summary>
        /// Searches for software by name.
        /// </summary>
        public async Task<MetadataResult> SearchByNameAsync(string softwareName)
        {
            if (string.IsNullOrWhiteSpace(softwareName))
                return null;

            try
            {
                log.Debug($"Wikipedia: Searching for '{softwareName}'");

                // Search for the article
                string searchUrl = $"{BASE_URL}?action=query&list=search&srsearch={HttpUtility.UrlEncode(softwareName)}" +
                                  "&format=json&srlimit=1";

                var searchResponse = await httpClient.GetAsync(searchUrl);
                searchResponse.EnsureSuccessStatusCode();

                string searchJson = await searchResponse.Content.ReadAsStringAsync();
                var searchData = JObject.Parse(searchJson);

                var searchResults = searchData["query"]?["search"] as JArray;
                if (searchResults == null || searchResults.Count == 0)
                {
                    log.Debug($"Wikipedia: No results found for '{softwareName}'");
                    return null;
                }

                // Get the first result's page ID
                int pageId = searchResults[0]["pageid"]?.Value<int>() ?? 0;
                string title = searchResults[0]["title"]?.ToString();

                if (pageId == 0 || string.IsNullOrEmpty(title))
                    return null;

                // Get article extract (summary)
                string extractUrl = $"{BASE_URL}?action=query&prop=extracts|info&pageids={pageId}" +
                                   "&exintro=1&explaintext=1&inprop=url&format=json";

                var extractResponse = await httpClient.GetAsync(extractUrl);
                extractResponse.EnsureSuccessStatusCode();

                string extractJson = await extractResponse.Content.ReadAsStringAsync();
                var extractData = JObject.Parse(extractJson);

                var page = extractData["query"]?["pages"]?[pageId.ToString()] as JObject;
                if (page == null)
                    return null;

                var metadata = new MetadataResult
                {
                    Source = ProviderName,
                    Name = title,
                    Description = page["extract"]?.ToString(),
                    WebsiteUrl = page["fullurl"]?.ToString(),
                    Confidence = CalculateConfidence(title, softwareName)
                };

                log.Info($"Wikipedia: Found metadata for '{softwareName}' -> '{metadata.Name}'");
                return metadata;
            }
            catch (Exception ex)
            {
                log.Error($"Wikipedia: Error searching for '{softwareName}': {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Searches for software by name and publisher.
        /// </summary>
        public async Task<MetadataResult> SearchByNameAndPublisherAsync(string softwareName, string publisher)
        {
            // Wikipedia doesn't have structured publisher search, use name search
            return await SearchByNameAsync($"{softwareName} {publisher}");
        }

        /// <summary>
        /// Calculates confidence score based on title matching.
        /// </summary>
        private double CalculateConfidence(string title, string searchTerm)
        {
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(searchTerm))
                return 0.5;

            // Exact match
            if (string.Equals(title, searchTerm, StringComparison.OrdinalIgnoreCase))
                return 0.9; // Wikipedia is a fallback, so max 0.9

            // Contains search term
            if (title.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                return 0.7;

            // Search term contains title
            if (searchTerm.IndexOf(title, StringComparison.OrdinalIgnoreCase) >= 0)
                return 0.7;

            return 0.5;
        }
    }
}
