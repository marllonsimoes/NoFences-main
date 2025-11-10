using NoFencesDataLayer.MasterCatalog.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Text;

namespace NoFencesDataLayer.MasterCatalog.Services
{
    /// <summary>
    /// Service for importing CSV data into the master catalog database.
    /// Creates entries with version tracking and audit trail.
    /// </summary>
    public class MasterCatalogImporter
    {
        private readonly MasterCatalogContext context;
        private long currentVersion;

        public MasterCatalogImporter(MasterCatalogContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Imports Software.csv into master catalog
        /// </summary>
        public ImportResult ImportSoftwareCsv(string csvPath, string importedBy = "System")
        {
            var result = new ImportResult { Source = csvPath };
            var startTime = DateTime.UtcNow;

            try
            {
                if (!File.Exists(csvPath))
                {
                    result.Success = false;
                    result.ErrorMessage = $"File not found: {csvPath}";
                    return result;
                }

                Console.WriteLine($"  → Starting Software.csv import from {csvPath}");

                // Get current version
                currentVersion = GetNextVersion();

                var lines = File.ReadAllLines(csvPath);
                if (lines.Length <= 1)
                {
                    result.Success = false;
                    result.ErrorMessage = "CSV file is empty or has no data rows";
                    return result;
                }

                // Parse CSV (skip header)
                var importedCount = 0;
                var skippedCount = 0;

                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        var fields = ParseCsvLine(lines[i]);
                        if (fields.Length < 2) continue;

                        var name = fields[0]?.Trim();
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            skippedCount++;
                            continue;
                        }

                        var id = GenerateId(name, fields.Length > 1 ? fields[1] : null);

                        // Check if exists
                        var existing = context.Software.Find(id);

                        if (existing == null)
                        {
                            // Create new entry
                            var entry = new MasterSoftwareEntry
                            {
                                Id = id,
                                Name = name,
                                Company = fields.Length > 1 ? CleanString(fields[1]) : null,
                                Category = DetermineCategory(name, fields.Length > 1 ? fields[1] : null),
                                Tags = JsonConvert.SerializeObject(ExtractTags(name)),
                                Version = currentVersion,
                                CreatedAt = startTime,
                                UpdatedAt = startTime,
                                IsDeleted = false,
                                LastModifiedBy = importedBy
                            };

                            context.Software.Add(entry);
                            importedCount++;

                            // Log the creation
                            LogChange("Software", id, "Created", importedBy, null);

                            // Batch save every 1000 records
                            if (importedCount % 1000 == 0)
                            {
                                context.SaveChanges();
                                Console.WriteLine($"  → Progress: {importedCount} software entries imported...");
                            }
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ⚠ Warning: Error parsing line {i}: {ex.Message}");
                        skippedCount++;
                    }
                }

                // Final save
                context.SaveChanges();

                // Update catalog version
                UpdateCatalogVersion(importedCount, 0);

                result.Success = true;
                result.ImportedCount = importedCount;
                result.SkippedCount = skippedCount;

                Console.WriteLine($"  → Completed: {importedCount} software entries imported, {skippedCount} skipped");
            }
            catch (DbEntityValidationException ex)
            {
                result.Success = false;
                var errorMessages = new StringBuilder();
                errorMessages.AppendLine(ex.Message);
                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        errorMessages.AppendLine($"  - {validationError.PropertyName}: {validationError.ErrorMessage}");
                    }
                }
                result.ErrorMessage = errorMessages.ToString();
                Console.WriteLine($"  ✗ Validation error importing Software.csv:");
                Console.WriteLine(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                Console.WriteLine($"  ✗ Error importing Software.csv: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Imports steam.csv into master catalog as generic game entries
        /// Each game is stored once with "Steam" as one of its platforms
        /// </summary>
        public ImportResult ImportSteamCsv(string csvPath, int maxEntries = 0, string importedBy = "System")
        {
            var result = new ImportResult { Source = csvPath };
            var startTime = DateTime.UtcNow;

            try
            {
                if (!File.Exists(csvPath))
                {
                    result.Success = false;
                    result.ErrorMessage = $"File not found: {csvPath}";
                    return result;
                }

                Console.WriteLine($"  → Starting steam.csv import from {csvPath}");
                if (maxEntries > 0)
                {
                    Console.WriteLine($"  → Limited to {maxEntries} entries");
                }

                // Get current version
                currentVersion = GetNextVersion();

                var lines = File.ReadAllLines(csvPath, Encoding.UTF8);
                if (lines.Length <= 1)
                {
                    result.Success = false;
                    result.ErrorMessage = "CSV file is empty or has no data rows";
                    return result;
                }

                // Parse CSV (skip header)
                var importedCount = 0;
                var skippedCount = 0;

                for (int i = 1; i < lines.Length; i++)
                {
                    if (maxEntries > 0 && importedCount >= maxEntries)
                        break;

                    try
                    {
                        var fields = ParseCsvLine(lines[i]);
                        if (fields.Length < 2) continue;

                        if (!int.TryParse(fields[0], out int appId))
                        {
                            skippedCount++;
                            continue;
                        }

                        var gameName = fields.Length > 1 ? CleanString(fields[1]) : null;

                        // Skip if missing essential data
                        if (string.IsNullOrWhiteSpace(gameName))
                        {
                            skippedCount++;
                            continue;
                        }

                        // Generate game ID from name
                        var gameId = GenerateId(gameName, null);

                        // Check if game already exists
                        var existing = context.Games.Find(gameId);

                        if (existing == null)
                        {
                            // Create supported OS object
                            var supportedOS = new
                            {
                                windows = fields.Length > 16 && fields[16]?.ToLower() == "true",
                                mac = fields.Length > 17 && fields[17]?.ToLower() == "true",
                                linux = fields.Length > 18 && fields[18]?.ToLower() == "true"
                            };

                            // Create platform IDs object
                            var platformIds = new
                            {
                                Steam = appId
                            };

                            var entry = new MasterGameEntry
                            {
                                Id = gameId,
                                Name = gameName,
                                PlatformIds = JsonConvert.SerializeObject(platformIds),
                                ReleaseDate = fields.Length > 2 ? CleanString(fields[2]) : null,
                                Developers = JsonConvert.SerializeObject(SplitList(fields.Length > 32 ? fields[32] : null)),
                                Publishers = JsonConvert.SerializeObject(SplitList(fields.Length > 33 ? fields[33] : null)),
                                Genres = JsonConvert.SerializeObject(SplitList(fields.Length > 35 ? fields[35] : null)),
                                SupportedOS = JsonConvert.SerializeObject(supportedOS),
                                Version = currentVersion,
                                CreatedAt = startTime,
                                UpdatedAt = startTime,
                                IsDeleted = false,
                                LastModifiedBy = importedBy
                            };

                            context.Games.Add(entry);
                            importedCount++;

                            // Log the creation
                            LogChange("Game", gameId, "Created", importedBy, null);

                            // Batch save every 1000 records
                            if (importedCount % 1000 == 0)
                            {
                                context.SaveChanges();
                                Console.WriteLine($"  → Progress: {importedCount} games imported...");
                            }
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ⚠ Warning: Error parsing Steam line {i}: {ex.Message}");
                        skippedCount++;
                    }
                }

                // Final save
                context.SaveChanges();

                // Update catalog version
                UpdateCatalogVersion(0, importedCount);

                result.Success = true;
                result.ImportedCount = importedCount;
                result.SkippedCount = skippedCount;

                Console.WriteLine($"  → Completed: {importedCount} games imported, {skippedCount} skipped");
            }
            catch (DbEntityValidationException ex)
            {
                result.Success = false;
                var errorMessages = new StringBuilder();
                errorMessages.AppendLine(ex.Message);
                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        errorMessages.AppendLine($"  - {validationError.PropertyName}: {validationError.ErrorMessage}");
                    }
                }
                result.ErrorMessage = errorMessages.ToString();
                Console.WriteLine($"  ✗ Validation error importing steam.csv:");
                Console.WriteLine(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                Console.WriteLine($"  ✗ Error importing steam.csv: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Imports all available catalogs from a directory
        /// </summary>
        public List<ImportResult> ImportAllCatalogs(string directoryPath, int maxSteamGames = 10000)
        {
            var results = new List<ImportResult>();

            var softwarePath = Path.Combine(directoryPath, "Software.csv");
            if (File.Exists(softwarePath))
            {
                results.Add(ImportSoftwareCsv(softwarePath));
            }

            var steamPath = Path.Combine(directoryPath, "steam.csv");
            if (File.Exists(steamPath))
            {
                results.Add(ImportSteamCsv(steamPath, maxSteamGames));
            }

            return results;
        }

        #region Helper Methods

        private long GetNextVersion()
        {
            var versionRecord = context.CatalogVersion.Find(1);
            if (versionRecord == null)
            {
                // Initialize if doesn't exist
                context.SeedInitialData();
                versionRecord = context.CatalogVersion.Find(1);
            }
            return versionRecord.CurrentVersion + 1;
        }

        private void UpdateCatalogVersion(int softwareAdded, int gamesAdded)
        {
            var versionRecord = context.CatalogVersion.Find(1);
            if (versionRecord != null)
            {
                versionRecord.CurrentVersion = currentVersion;
                versionRecord.LastUpdated = DateTime.UtcNow;
                versionRecord.TotalSoftware = context.Software.Count(s => !s.IsDeleted);
                versionRecord.TotalGames = context.Games.Count(g => !g.IsDeleted);
                context.SaveChanges();
            }
        }

        private void LogChange(string entityType, string entityId, string action, string changedBy, string changes)
        {
            context.ChangeLogs.Add(new ChangeLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = changedBy,
                Changes = changes,
                CatalogVersion = currentVersion
            });
        }

        private string GenerateId(string name, string company)
        {
            var normalized = (name ?? "").ToLowerInvariant()
                .Replace(" ", "-")
                .Replace(".", "")
                .Replace(",", "");

            if (!string.IsNullOrWhiteSpace(company))
            {
                var companyNorm = company.ToLowerInvariant()
                    .Replace(" ", "-")
                    .Replace(".", "")
                    .Replace(",", "");
                normalized += "-" + companyNorm;
            }

            // Limit length
            if (normalized.Length > 200)
                normalized = normalized.Substring(0, 200);

            return normalized;
        }

        private string CleanString(string input)
        {
            return string.IsNullOrWhiteSpace(input) ? null : input.Trim();
        }

        private string CleanUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            url = url.Trim();
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                return null;
            return url;
        }

        private List<string> SplitList(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new List<string>();

            return input
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        private int? ParseIntOrNull(string value)
        {
            if (int.TryParse(value, out int result))
                return result;
            return null;
        }

        private int ParseIntOrDefault(string value, int defaultValue = 0)
        {
            if (int.TryParse(value, out int result))
                return result;
            return defaultValue;
        }

        private double? ParseDecimalOrNull(string value)
        {
            if (double.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double result))
                return result;
            return null;
        }

        private string DetermineCategory(string name, string company)
        {
            if (string.IsNullOrEmpty(name))
                return "Other";

            var lower = name.ToLower();

            if (lower.Contains("visual studio") || lower.Contains("intellij") ||
                lower.Contains("eclipse") || lower.Contains("vscode") ||
                lower.Contains("compiler") || lower.Contains("sdk"))
                return "Development";

            if (lower.Contains("office") || lower.Contains("word") ||
                lower.Contains("excel") || lower.Contains("powerpoint"))
                return "OfficeProductivity";

            if (lower.Contains("photoshop") || lower.Contains("illustrator") ||
                lower.Contains("designer") || lower.Contains("gimp") ||
                lower.Contains("blender"))
                return "Design";

            if (lower.Contains("chrome") || lower.Contains("firefox") ||
                lower.Contains("edge") || lower.Contains("browser") ||
                lower.Contains("teams") || lower.Contains("slack") ||
                lower.Contains("discord"))
                return "Communication";

            if (lower.Contains("player") || lower.Contains("vlc") ||
                lower.Contains("spotify") || lower.Contains("media"))
                return "Media";

            if (lower.Contains("game") || lower.Contains("gaming"))
                return "Games";

            return "Other";
        }

        private List<string> ExtractTags(string name)
        {
            var tags = new List<string>();
            if (string.IsNullOrEmpty(name)) return tags;

            var lower = name.ToLower();

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

        private string[] ParseCsvLine(string line)
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

    /// <summary>
    /// Result of an import operation
    /// </summary>
    public class ImportResult
    {
        public bool Success { get; set; }
        public string Source { get; set; }
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public string ErrorMessage { get; set; }

        public override string ToString()
        {
            if (Success)
            {
                return $"✓ {Source}: Imported {ImportedCount}, Skipped {SkippedCount}";
            }
            else
            {
                return $"✗ {Source}: {ErrorMessage}";
            }
        }
    }
}
