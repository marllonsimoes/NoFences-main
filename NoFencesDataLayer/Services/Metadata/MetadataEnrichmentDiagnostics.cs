using log4net;
using NoFencesDataLayer.MasterCatalog;
using NoFencesDataLayer.MasterCatalog.Entities;
using NoFencesDataLayer.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFencesDataLayer.Services.Metadata
{
    /// <summary>
    /// Diagnostic utility for investigating metadata enrichment issues.
    /// Helps identify which games are missing metadata and why.
    /// </summary>
    public class MetadataEnrichmentDiagnostics
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MetadataEnrichmentDiagnostics));
        private readonly ISoftwareReferenceRepository softwareRefRepository;

        public MetadataEnrichmentDiagnostics(ISoftwareReferenceRepository repository)
        {
            this.softwareRefRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public MetadataEnrichmentDiagnostics()
            : this(new SoftwareReferenceRepository(new MasterCatalogContext()))
        {
        }

        /// <summary>
        /// Generates a comprehensive diagnostic report about metadata enrichment status.
        /// </summary>
        public string GenerateDiagnosticReport()
        {
            var report = new StringBuilder();
            report.AppendLine("=".PadRight(80, '='));
            report.AppendLine("METADATA ENRICHMENT DIAGNOSTIC REPORT");
            report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("=".PadRight(80, '='));
            report.AppendLine();

            try
            {
                // Get all software references
                var allEntries = softwareRefRepository.GetAllEntries();
                report.AppendLine($"Total Software References: {allEntries.Count}");
                report.AppendLine();

                // 1. Overall Statistics
                report.AppendLine("--- OVERALL STATISTICS ---");
                var enrichedCount = allEntries.Count(e => e.LastEnrichedDate != null);
                var neverAttemptedCount = allEntries.Count(e => e.LastEnrichmentAttempt == null);
                var attemptedButFailedCount = allEntries.Count(e => e.LastEnrichmentAttempt != null && e.LastEnrichedDate == null);
                var successfulCount = allEntries.Count(e => e.LastEnrichedDate != null);

                report.AppendLine($"Successfully Enriched:      {successfulCount,6} ({CalculatePercentage(successfulCount, allEntries.Count),5:F1}%)");
                report.AppendLine($"Never Attempted:            {neverAttemptedCount,6} ({CalculatePercentage(neverAttemptedCount, allEntries.Count),5:F1}%)");
                report.AppendLine($"Attempted but Failed:       {attemptedButFailedCount,6} ({CalculatePercentage(attemptedButFailedCount, allEntries.Count),5:F1}%)");
                report.AppendLine();

                // 2. Breakdown by Source
                report.AppendLine("--- BREAKDOWN BY SOURCE ---");
                var sourceGroups = allEntries.GroupBy(e => e.Source ?? "Unknown");
                foreach (var group in sourceGroups.OrderByDescending(g => g.Count()))
                {
                    int total = group.Count();
                    int enriched = group.Count(e => e.LastEnrichedDate != null);
                    int failed = group.Count(e => e.LastEnrichmentAttempt != null && e.LastEnrichedDate == null);
                    int neverTried = group.Count(e => e.LastEnrichmentAttempt == null);

                    report.AppendLine($"{group.Key,-20} Total: {total,5}  Enriched: {enriched,5} ({CalculatePercentage(enriched, total),5:F1}%)  Failed: {failed,5}  Never: {neverTried,5}");
                }
                report.AppendLine();

                // 3. Breakdown by Category
                report.AppendLine("--- BREAKDOWN BY CATEGORY ---");
                var categoryGroups = allEntries.GroupBy(e => e.Category ?? "Unknown");
                foreach (var group in categoryGroups.OrderByDescending(g => g.Count()))
                {
                    int total = group.Count();
                    int enriched = group.Count(e => e.LastEnrichedDate != null);
                    int failed = group.Count(e => e.LastEnrichmentAttempt != null && e.LastEnrichedDate == null);

                    report.AppendLine($"{group.Key,-20} Total: {total,5}  Enriched: {enriched,5} ({CalculatePercentage(enriched, total),5:F1}%)  Failed: {failed,5}");
                }
                report.AppendLine();

                // 4. Top 20 Games with Failed Enrichment
                report.AppendLine("--- TOP 20 GAMES WITH FAILED ENRICHMENT ---");
                report.AppendLine("(Attempted enrichment but no metadata found)");
                report.AppendLine();

                var failedGames = allEntries
                    .Where(e => e.Category == "Games" &&
                                e.LastEnrichmentAttempt != null &&
                                e.LastEnrichedDate == null)
                    .OrderByDescending(e => e.LastEnrichmentAttempt)
                    .Take(20)
                    .ToList();

                if (failedGames.Count > 0)
                {
                    report.AppendLine($"{"ID",-8} {"Last Attempt",-20} {"Source",-15} {"Name"}");
                    report.AppendLine("".PadRight(80, '-'));

                    foreach (var game in failedGames)
                    {
                        string lastAttempt = game.LastEnrichmentAttempt?.ToString("yyyy-MM-dd HH:mm") ?? "Never";
                        string name = game.Name.Length > 40 ? game.Name.Substring(0, 37) + "..." : game.Name;
                        report.AppendLine($"{game.Id,-8} {lastAttempt,-20} {game.Source,-15} {name}");
                    }
                }
                else
                {
                    report.AppendLine("No games with failed enrichment found.");
                }
                report.AppendLine();

                // 5. Games Never Attempted
                report.AppendLine("--- TOP 20 GAMES NEVER ATTEMPTED ---");
                var neverAttemptedGames = allEntries
                    .Where(e => e.Category == "Games" && e.LastEnrichmentAttempt == null)
                    .OrderBy(e => e.Name)
                    .Take(20)
                    .ToList();

                if (neverAttemptedGames.Count > 0)
                {
                    report.AppendLine($"{"ID",-8} {"Source",-15} {"Name"}");
                    report.AppendLine("".PadRight(80, '-'));

                    foreach (var game in neverAttemptedGames)
                    {
                        string name = game.Name.Length > 50 ? game.Name.Substring(0, 47) + "..." : game.Name;
                        report.AppendLine($"{game.Id,-8} {game.Source,-15} {name}");
                    }
                }
                else
                {
                    report.AppendLine("All games have been attempted.");
                }
                report.AppendLine();

                // 6. Rate Limiting Status
                report.AppendLine("--- RATE LIMITING STATUS ---");
                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1);
                var attemptedTodayCount = allEntries
                    .Count(e => e.LastEnrichmentAttempt != null &&
                                e.LastEnrichmentAttempt >= today &&
                                e.LastEnrichmentAttempt < tomorrow);

                report.AppendLine($"Entries attempted today (rate limited): {attemptedTodayCount}");
                report.AppendLine($"These entries will be eligible for retry after: {tomorrow:yyyy-MM-dd 00:00:00} UTC");
                report.AppendLine();

                // 7. Recommendations
                report.AppendLine("--- RECOMMENDATIONS ---");

                // Check RAWG API key configuration (critical for game enrichment)
                var rawgClient = new RawgApiClient();
                if (!rawgClient.IsAvailable())
                {
                    report.AppendLine("🔴 CRITICAL: RAWG API KEY NOT CONFIGURED!");
                    report.AppendLine("  - Game metadata enrichment is DISABLED");
                    report.AppendLine("  - Get free API key from: https://rawg.io/apidocs");
                    report.AppendLine("  - Add to UserPreferences or App.config: <add key=\"RawgApiKey\" value=\"YOUR_KEY\" />");
                    report.AppendLine();
                }
                else
                {
                    report.AppendLine("✓ RAWG API Key: Configured");
                    report.AppendLine();
                }

                if (attemptedButFailedCount > 0)
                {
                    double failureRate = CalculatePercentage(attemptedButFailedCount, allEntries.Count);
                    if (failureRate > 20)
                    {
                        report.AppendLine($"⚠ HIGH FAILURE RATE ({failureRate:F1}%):");
                        report.AppendLine("  1. Check if RAWG API key is valid and has quota remaining");
                        report.AppendLine("  2. Consider lowering confidence threshold from 0.85 to 0.75");
                        report.AppendLine("  3. Review game names for cleaning improvements");
                    }
                    else if (failureRate > 10)
                    {
                        report.AppendLine($"⚠ MODERATE FAILURE RATE ({failureRate:F1}%):");
                        report.AppendLine("  - Some games may not be in RAWG database (especially older/indie titles)");
                        report.AppendLine("  - Check if name cleaning is removing too much information");
                    }
                    else
                    {
                        report.AppendLine($"✓ ACCEPTABLE FAILURE RATE ({failureRate:F1}%)");
                        report.AppendLine("  - Most games are being enriched successfully");
                    }
                }

                if (neverAttemptedCount > 0)
                {
                    report.AppendLine($"\n⚠ {neverAttemptedCount} entries never attempted:");
                    report.AppendLine("  - Run manual enrichment to process these entries");
                    report.AppendLine("  - Check database population logs for errors");
                }

                report.AppendLine();
                report.AppendLine("=".PadRight(80, '='));
                report.AppendLine("END OF REPORT");
                report.AppendLine("=".PadRight(80, '='));
            }
            catch (Exception ex)
            {
                report.AppendLine();
                report.AppendLine($"ERROR GENERATING REPORT: {ex.Message}");
                log.Error($"Error generating diagnostic report: {ex.Message}", ex);
            }

            string reportText = report.ToString();
            log.Info("Metadata enrichment diagnostic report generated");
            log.Debug(reportText);

            return reportText;
        }

        /// <summary>
        /// Gets a list of games that failed enrichment with detailed information.
        /// </summary>
        public List<EnrichmentFailureInfo> GetFailedEnrichments()
        {
            try
            {
                var allEntries = softwareRefRepository.GetAllEntries();
                var failedEntries = allEntries
                    .Where(e => e.LastEnrichmentAttempt != null && e.LastEnrichedDate == null)
                    .ToList();

                var result = new List<EnrichmentFailureInfo>();
                foreach (var entry in failedEntries)
                {
                    result.Add(new EnrichmentFailureInfo
                    {
                        Id = entry.Id,
                        Name = entry.Name,
                        Source = entry.Source,
                        Category = entry.Category,
                        LastAttempt = entry.LastEnrichmentAttempt,
                        IsGame = entry.Category == "Games",
                        Publisher = entry.Publisher
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                log.Error($"Error getting failed enrichments: {ex.Message}", ex);
                return new List<EnrichmentFailureInfo>();
            }
        }

        /// <summary>
        /// Resets enrichment attempts for entries that failed.
        /// This allows them to be retried without waiting for rate limiting.
        /// USE WITH CAUTION - This will cause all failed entries to be retried.
        /// </summary>
        public int ResetFailedAttempts()
        {
            try
            {
                log.Warn("ResetFailedAttempts called - resetting all failed enrichment attempts");

                var allEntries = softwareRefRepository.GetAllEntries();
                var failedEntries = allEntries
                    .Where(e => e.LastEnrichmentAttempt != null && e.LastEnrichedDate == null)
                    .ToList();

                int resetCount = 0;
                foreach (var entry in failedEntries)
                {
                    entry.LastEnrichmentAttempt = null;
                    softwareRefRepository.Update(entry);
                    resetCount++;
                }

                log.Info($"Reset {resetCount} failed enrichment attempts");
                return resetCount;
            }
            catch (Exception ex)
            {
                log.Error($"Error resetting failed attempts: {ex.Message}", ex);
                return 0;
            }
        }

        private double CalculatePercentage(int part, int total)
        {
            if (total == 0) return 0;
            return (part * 100.0) / total;
        }
    }

    /// <summary>
    /// Information about a failed enrichment attempt.
    /// </summary>
    public class EnrichmentFailureInfo
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Source { get; set; }
        public string Category { get; set; }
        public DateTime? LastAttempt { get; set; }
        public bool IsGame { get; set; }
        public string Publisher { get; set; }
    }
}
