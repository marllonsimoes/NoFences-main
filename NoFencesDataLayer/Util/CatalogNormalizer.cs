using NoFences.Core.Model;
using NoFencesDataLayer.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace NoFencesDataLayer.Util
{
    /// <summary>
    /// Utility to normalize CSV catalog data into structured JSON format.
    /// Handles deduplication, validation, and category standardization.
    /// </summary>
    public class CatalogNormalizer
    {
        /// <summary>
        /// Converts Software.csv to normalized SoftwareEntry list
        /// </summary>
        public static List<SoftwareEntry> NormalizeSoftwareCsv(string csvPath)
        {
            var entries = new List<SoftwareEntry>();
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(csvPath))
            {
                throw new FileNotFoundException($"CSV file not found: {csvPath}");
            }

            var lines = File.ReadAllLines(csvPath);
            if (lines.Length <= 1) return entries;

            // Skip header
            for (int i = 1; i < lines.Length; i++)
            {
                try
                {
                    var fields = ParseCsvLine(lines[i]);
                    if (fields.Length < 2) continue;

                    var name = fields[0]?.Trim();
                    if (string.IsNullOrWhiteSpace(name) || seenNames.Contains(name))
                        continue;

                    var entry = new SoftwareEntry
                    {
                        Id = GenerateId(name, fields.Length > 1 ? fields[1] : null),
                        Name = name,
                        Company = fields.Length > 1 ? CleanString(fields[1]) : null,
                        License = fields.Length > 2 ? CleanString(fields[2]) : null,
                        Description = fields.Length > 3 ? CleanString(fields[3]) : null,
                        Website = fields.Length > 4 ? CleanUrl(fields[4]) : null,
                        Category = DetermineCategory(name, fields.Length > 1 ? fields[1] : null),
                        Tags = ExtractTags(name, fields.Length > 1 ? fields[1] : null)
                    };

                    entries.Add(entry);
                    seenNames.Add(name);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing line {i}: {ex.Message}");
                }
            }

            return entries;
        }

        /// <summary>
        /// Converts steam.csv to normalized SteamGameEntry list
        /// </summary>
        public static List<SteamGameEntry> NormalizeSteamCsv(string csvPath, int maxEntries = 0)
        {
            var entries = new List<SteamGameEntry>();
            var seenAppIds = new HashSet<int>();

            if (!File.Exists(csvPath))
            {
                throw new FileNotFoundException($"CSV file not found: {csvPath}");
            }

            var lines = File.ReadAllLines(csvPath, Encoding.UTF8);
            if (lines.Length <= 1) return entries;

            // Skip header
            for (int i = 1; i < lines.Length; i++)
            {
                if (maxEntries > 0 && entries.Count >= maxEntries)
                    break;

                try
                {
                    var fields = ParseCsvLine(lines[i]);
                    if (fields.Length < 2) continue;

                    if (!int.TryParse(fields[0], out int appId) || seenAppIds.Contains(appId))
                        continue;

                    var entry = new SteamGameEntry
                    {
                        AppId = appId,
                        Name = fields.Length > 1 ? CleanString(fields[1]) : null,
                        ReleaseDate = fields.Length > 2 ? CleanString(fields[2]) : null,
                        Developers = SplitList(fields.Length > 32 ? fields[32] : null),
                        Publishers = SplitList(fields.Length > 33 ? fields[33] : null),
                        Genres = SplitList(fields.Length > 35 ? fields[35] : null),
                        Tags = SplitList(fields.Length > 36 ? fields[36] : null).Take(10).ToList(), // Limit to top 10 tags
                        HeaderImage = fields.Length > 12 ? CleanUrl(fields[12]) : null,
                        Platforms = new PlatformSupport
                        {
                            Windows = fields.Length > 16 && fields[16]?.ToLower() == "true",
                            Mac = fields.Length > 17 && fields[17]?.ToLower() == "true",
                            Linux = fields.Length > 18 && fields[18]?.ToLower() == "true"
                        },
                        MetacriticScore = ParseIntOrNull(fields.Length > 19 ? fields[19] : null),
                        PositiveReviews = ParseIntOrDefault(fields.Length > 22 ? fields[22] : null),
                        NegativeReviews = ParseIntOrDefault(fields.Length > 23 ? fields[23] : null),
                        Price = ParseDecimalOrNull(fields.Length > 6 ? fields[6] : null)
                    };

                    // Skip if missing essential data
                    if (string.IsNullOrWhiteSpace(entry.Name))
                        continue;

                    entries.Add(entry);
                    seenAppIds.Add(appId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing Steam line {i}: {ex.Message}");
                }
            }

            return entries;
        }

        /// <summary>
        /// Creates a complete normalized catalog JSON from CSV files
        /// </summary>
        public static SoftwareCatalogJson CreateNormalizedCatalog(
            string softwareCsvPath,
            string steamCsvPath,
            int maxSteamGames = 10000) // Limit Steam games to keep JSON manageable
        {
            var catalog = new SoftwareCatalogJson
            {
                Metadata = new CatalogMetadata
                {
                    Version = "1.0.0",
                    GeneratedDate = DateTime.UtcNow,
                    Description = "Normalized software catalog for NoFences"
                }
            };

            Console.WriteLine("Normalizing Software.csv...");
            if (File.Exists(softwareCsvPath))
            {
                catalog.Software = NormalizeSoftwareCsv(softwareCsvPath);
                Console.WriteLine($"Normalized {catalog.Software.Count} software entries");
            }

            Console.WriteLine($"Normalizing steam.csv (max {maxSteamGames} entries)...");
            if (File.Exists(steamCsvPath))
            {
                catalog.SteamGames = NormalizeSteamCsv(steamCsvPath, maxSteamGames);
                Console.WriteLine($"Normalized {catalog.SteamGames.Count} Steam games");
            }

            catalog.Metadata.TotalSoftware = catalog.Software.Count;
            catalog.Metadata.TotalSteamGames = catalog.SteamGames.Count;

            return catalog;
        }

        #region Helper Methods

        private static string GenerateId(string name, string company)
        {
            var normalized = (name ?? "").ToLowerInvariant().Replace(" ", "-");
            if (!string.IsNullOrWhiteSpace(company))
            {
                normalized += "-" + company.ToLowerInvariant().Replace(" ", "-");
            }
            return normalized;
        }

        private static string CleanString(string input)
        {
            return string.IsNullOrWhiteSpace(input) ? null : input.Trim();
        }

        private static string CleanUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            url = url.Trim();
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                return null;
            return url;
        }

        private static List<string> SplitList(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new List<string>();

            return input
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        private static int? ParseIntOrNull(string value)
        {
            if (int.TryParse(value, out int result))
                return result;
            return null;
        }

        private static int ParseIntOrDefault(string value, int defaultValue = 0)
        {
            if (int.TryParse(value, out int result))
                return result;
            return defaultValue;
        }

        private static decimal? ParseDecimalOrNull(string value)
        {
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result;
            return null;
        }

        private static string DetermineCategory(string name, string company)
        {
            if (string.IsNullOrEmpty(name))
                return SoftwareCategory.Other.ToString();

            var lower = name.ToLower();

            // Development tools
            if (lower.Contains("visual studio") || lower.Contains("intellij") ||
                lower.Contains("eclipse") || lower.Contains("vscode") ||
                lower.Contains("compiler") || lower.Contains("sdk"))
                return SoftwareCategory.Development.ToString();

            // Office & Productivity
            if (lower.Contains("office") || lower.Contains("word") ||
                lower.Contains("excel") || lower.Contains("powerpoint") ||
                lower.Contains("outlook") || lower.Contains("onenote"))
                return SoftwareCategory.OfficeProductivity.ToString();

            // Design tools
            if (lower.Contains("photoshop") || lower.Contains("illustrator") ||
                lower.Contains("designer") || lower.Contains("inkscape") ||
                lower.Contains("gimp") || lower.Contains("blender"))
                return SoftwareCategory.Design.ToString();

            // Communication
            if (lower.Contains("chrome") || lower.Contains("firefox") ||
                lower.Contains("edge") || lower.Contains("browser") ||
                lower.Contains("teams") || lower.Contains("slack") ||
                lower.Contains("discord") || lower.Contains("zoom"))
                return SoftwareCategory.Communication.ToString();

            // Media
            if (lower.Contains("player") || lower.Contains("vlc") ||
                lower.Contains("spotify") || lower.Contains("media") ||
                lower.Contains("video") || lower.Contains("audio"))
                return SoftwareCategory.Media.ToString();

            // Games
            if (lower.Contains("game") || lower.Contains("gaming"))
                return SoftwareCategory.Games.ToString();

            return SoftwareCategory.Other.ToString();
        }

        private static List<string> ExtractTags(string name, string company)
        {
            var tags = new List<string>();

            if (string.IsNullOrEmpty(name))
                return tags;

            var lower = name.ToLower();

            // Add relevant tags based on keywords
            if (lower.Contains("free") || lower.Contains("open source"))
                tags.Add("free");
            if (lower.Contains("portable"))
                tags.Add("portable");
            if (lower.Contains("lite") || lower.Contains("light"))
                tags.Add("lightweight");
            if (lower.Contains("pro") || lower.Contains("professional"))
                tags.Add("professional");

            return tags;
        }

        private static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            bool inQuotes = false;
            var currentField = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            fields.Add(currentField.ToString());
            return fields.ToArray();
        }

        #endregion
    }
}
