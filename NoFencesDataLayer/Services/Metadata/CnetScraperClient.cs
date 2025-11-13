using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace NoFencesDataLayer.Services.Metadata
{
    /// <summary>
    /// CNET scraper for software metadata.
    /// NOTE: CNET doesn't have a public API, so this scrapes their website.
    /// Use respectfully with rate limiting and caching.
    /// Session 11: Software metadata collection.
    /// </summary>
    public class CnetScraperClient : ISoftwareMetadataProvider
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(CnetScraperClient));
        private const string SEARCH_URL = "https://www.cnet.com/search/";
        private static readonly HttpClient httpClient = new HttpClient();
        private static DateTime lastRequestTime = DateTime.MinValue;
        private static readonly TimeSpan MinRequestInterval = TimeSpan.FromSeconds(2); // Rate limiting

        public string ProviderName => "CNET";
        public int Priority => 10; // Lower priority than Winget, higher than Wikipedia

        public CnetScraperClient()
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public bool IsAvailable()
        {
            // CNET is publicly accessible, no API key needed
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
                // Rate limiting
                await EnforceRateLimit();

                log.Debug($"CNET: Searching for '{softwareName}'");

                string searchUrl = $"{SEARCH_URL}?q={HttpUtility.UrlEncode(softwareName)}";

                var response = await httpClient.GetAsync(searchUrl);
                response.EnsureSuccessStatusCode();

                string html = await response.Content.ReadAsStringAsync();

                // Parse search results
                var metadata = ParseSearchResults(html, softwareName);

                if (metadata != null)
                {
                    log.Info($"CNET: Found metadata for '{softwareName}' -> '{metadata.Name}'");
                }
                else
                {
                    log.Debug($"CNET: No results found for '{softwareName}'");
                }

                return metadata;
            }
            catch (Exception ex)
            {
                log.Error($"CNET: Error searching for '{softwareName}': {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Searches for software by name and publisher.
        /// </summary>
        public async Task<MetadataResult> SearchByNameAndPublisherAsync(string softwareName, string publisher)
        {
            // Include publisher in search query
            return await SearchByNameAsync($"{softwareName} {publisher}");
        }

        /// <summary>
        /// Parses CNET search results HTML.
        /// NOTE: This is fragile and may break if CNET changes their HTML structure.
        /// </summary>
        private MetadataResult ParseSearchResults(string html, string searchTerm)
        {
            try
            {
                // Look for JSON-LD structured data (CNET often includes this)
                var jsonLdMatch = Regex.Match(html, @"<script type=""application/ld\+json"">(.*?)</script>", RegexOptions.Singleline);
                if (jsonLdMatch.Success)
                {
                    try
                    {
                        string jsonContent = jsonLdMatch.Groups[1].Value;
                        var json = JObject.Parse(jsonContent);

                        // Check if it's a SoftwareApplication
                        string type = json["@type"]?.ToString();
                        if (type == "SoftwareApplication" || type == "WebApplication")
                        {
                            return new MetadataResult
                            {
                                Source = ProviderName,
                                Name = json["name"]?.ToString(),
                                Description = json["description"]?.ToString(),
                                Publisher = json["publisher"]?["name"]?.ToString(),
                                Rating = json["aggregateRating"]?["ratingValue"]?.Value<double?>(),
                                WebsiteUrl = json["url"]?.ToString(),
                                IconUrl = json["image"]?.ToString(),
                                Confidence = 0.8
                            };
                        }
                    }
                    catch
                    {
                        // JSON parsing failed, continue to fallback method
                    }
                }

                // Fallback: Try to extract basic information from HTML
                var titleMatch = Regex.Match(html, @"<h\d[^>]*>([^<]*" + Regex.Escape(searchTerm) + @"[^<]*)</h\d>", RegexOptions.IgnoreCase);
                if (titleMatch.Success)
                {
                    string title = titleMatch.Groups[1].Value.Trim();

                    // Try to find description
                    string description = "";
                    var descMatch = Regex.Match(html, title + @".*?<p[^>]*>(.*?)</p>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    if (descMatch.Success)
                    {
                        description = StripHtml(descMatch.Groups[1].Value).Trim();
                    }

                    return new MetadataResult
                    {
                        Source = ProviderName,
                        Name = title,
                        Description = description,
                        Confidence = 0.6 // Lower confidence for HTML parsing
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                log.Warn($"CNET: Error parsing HTML: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Strips HTML tags from text.
        /// </summary>
        private string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            // Remove HTML tags
            string result = Regex.Replace(html, @"<[^>]+>", " ");

            // Decode HTML entities
            result = HttpUtility.HtmlDecode(result);

            // Clean up whitespace
            result = Regex.Replace(result, @"\s+", " ");

            return result.Trim();
        }

        /// <summary>
        /// Enforces rate limiting to be respectful to CNET's servers.
        /// </summary>
        private async Task EnforceRateLimit()
        {
            var timeSinceLastRequest = DateTime.Now - lastRequestTime;
            if (timeSinceLastRequest < MinRequestInterval)
            {
                var delay = MinRequestInterval - timeSinceLastRequest;
                log.Debug($"CNET: Rate limiting, waiting {delay.TotalMilliseconds}ms");
                await Task.Delay(delay);
            }
            lastRequestTime = DateTime.Now;
        }
    }
}
