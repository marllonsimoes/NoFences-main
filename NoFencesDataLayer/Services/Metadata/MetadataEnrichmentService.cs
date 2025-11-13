using log4net;
using NoFences.Core.Model;
using NoFencesDataLayer.MasterCatalog.Entities;
using NoFencesDataLayer.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NoFencesDataLayer.Services.Metadata
{
    /// <summary>
    /// Service for enriching software/game metadata from multiple providers.
    /// Orchestrates RAWG (games), Winget, CNET, and Wikipedia (software) providers.
    /// Session 11: Metadata enrichment orchestration.
    /// </summary>
    public class MetadataEnrichmentService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MetadataEnrichmentService));

        private readonly List<IGameMetadataProvider> gameProviders;
        private readonly List<ISoftwareMetadataProvider> softwareProviders;
        private readonly ISoftwareReferenceRepository softwareRefRepository;

        /// <summary>
        /// Constructor with dependency injection for all metadata providers.
        /// Session 12: Now injects ISoftwareReferenceRepository (enriches software_ref, not InstalledSoftware).
        /// </summary>
        public MetadataEnrichmentService(
            ISoftwareReferenceRepository softwareRefRepository,
            IEnumerable<IGameMetadataProvider> gameMetadataProviders,
            IEnumerable<ISoftwareMetadataProvider> softwareMetadataProviders)
        {
            this.softwareRefRepository = softwareRefRepository ?? throw new ArgumentNullException(nameof(softwareRefRepository));

            // Store and sort providers by priority
            gameProviders = (gameMetadataProviders ?? throw new ArgumentNullException(nameof(gameMetadataProviders)))
                .OrderBy(p => p.Priority)
                .ToList();

            softwareProviders = (softwareMetadataProviders ?? throw new ArgumentNullException(nameof(softwareMetadataProviders)))
                .OrderBy(p => p.Priority)
                .ToList();

            if (gameProviders.Count == 0 && softwareProviders.Count == 0)
            {
                log.Warn("MetadataEnrichmentService initialized with no providers");
            }
        }

        /// <summary>
        /// Default constructor for legacy code (creates providers internally).
        /// Session 12: Updated to use MasterCatalogContext for SoftwareReferenceRepository.
        /// </summary>
        public MetadataEnrichmentService() : this(
            new SoftwareReferenceRepository(new MasterCatalog.MasterCatalogContext()),
            new List<IGameMetadataProvider> { new RawgApiClient() },
            new List<ISoftwareMetadataProvider>
            {
                new WingetApiClient(),
                new CnetScraperClient(),
                new WikipediaApiClient()
            })
        {
        }

        /// <summary>
        /// Enriches a single installed software entry with metadata from providers.
        /// Session 12: Now returns enrichment result with metadata source tracking.
        /// </summary>
        /// <param name="software">Software to enrich</param>
        /// <returns>Tuple: (success, metadataSource)</returns>
        public async Task<(bool success, string metadataSource)> EnrichSoftwareAsync(InstalledSoftware software)
        {
            if (software == null || string.IsNullOrEmpty(software.Name))
                return (false, null);

            try
            {
                log.Debug($"Enriching metadata for: {software.Name}");

                // Session 12: Determine if this is a game based on Category AND Source
                bool isGame = IsGameSource(software);
                log.Debug($"  → Source: '{software.Source}', Category: {software.Category}, IsGame: {isGame}");

                MetadataResult metadata = null;

                if (isGame)
                {
                    // Try game providers
                    log.Debug($"  → Using game providers (RAWG)");
                    metadata = await EnrichWithGameProviders(software);
                }
                else
                {
                    // Try software providers
                    log.Debug($"  → Using software providers (Winget, CNET, Wikipedia)");
                    metadata = await EnrichWithSoftwareProviders(software);
                }

                if (metadata != null)
                {
                    // Apply metadata to software object
                    ApplyMetadata(software, metadata);
                    log.Info($"Successfully enriched '{software.Name}' from {metadata.Source}");
                    return (true, metadata.Source);
                }

                log.Debug($"No metadata found for '{software.Name}'");
                return (false, null);
            }
            catch (Exception ex)
            {
                log.Error($"Error enriching metadata for '{software.Name}': {ex.Message}", ex);
                return (false, null);
            }
        }

        /// <summary>
        /// Enriches multiple software entries in batch.
        /// Session 12: DEPRECATED - Use EnrichSoftwareReferenceBatchAsync() instead.
        /// This method is kept for backward compatibility but no longer updates the database.
        /// </summary>
        /// <param name="softwareList">List of software to enrich</param>
        /// <param name="updateDatabase">IGNORED - database update no longer supported with old schema</param>
        /// <returns>Number of successfully enriched entries</returns>
        [Obsolete("Use EnrichSoftwareReferenceBatchAsync() instead for two-tier architecture")]
        public async Task<int> EnrichBatchAsync(List<InstalledSoftware> softwareList, bool updateDatabase = true)
        {
            if (softwareList == null || softwareList.Count == 0)
                return 0;

            int enrichedCount = 0;
            log.Info($"Starting batch enrichment for {softwareList.Count} software entries (legacy method)");

            foreach (var software in softwareList)
            {
                var (success, metadataSource) = await EnrichSoftwareAsync(software);
                if (success)
                {
                    enrichedCount++;
                }

                // Small delay between requests to avoid rate limiting
                await Task.Delay(500);
            }

            if (updateDatabase)
            {
                log.Warn("EnrichBatchAsync: Database update is no longer supported. Use EnrichSoftwareReferenceBatchAsync() instead.");
            }

            log.Info($"Batch enrichment complete: {enrichedCount}/{softwareList.Count} enriched (metadata applied to objects only, not persisted)");
            return enrichedCount;
        }

        /// <summary>
        /// Enriches a single SoftwareReference with metadata from providers.
        /// Session 12: NEW method for two-tier architecture - enriches software_ref directly.
        /// </summary>
        /// <param name="softwareRef">SoftwareReference to enrich</param>
        /// <returns>Tuple: (success, metadataSource)</returns>
        public async Task<(bool success, string metadataSource)> EnrichSoftwareReferenceAsync(SoftwareReference softwareRef)
        {
            if (softwareRef == null || string.IsNullOrEmpty(softwareRef.Name))
                return (false, null);

            try
            {
                log.Debug($"Enriching metadata for SoftwareReference: {softwareRef.Name} (ID: {softwareRef.Id})");

                // Session 12: Determine if this is a game based on Category AND Source
                bool isGame = IsGameSourceRef(softwareRef);
                log.Debug($"  → Source: '{softwareRef.Source}', Category: {softwareRef.Category}, IsGame: {isGame}");

                MetadataResult metadata = null;

                if (isGame)
                {
                    // Try game providers
                    log.Debug($"  → Using game providers (RAWG)");
                    metadata = await EnrichWithGameProvidersRef(softwareRef);
                }
                else
                {
                    // Try software providers
                    log.Debug($"  → Using software providers (Winget, CNET, Wikipedia)");
                    metadata = await EnrichWithSoftwareProvidersRef(softwareRef);
                }

                if (metadata != null)
                {
                    // Apply metadata to SoftwareReference
                    ApplyMetadataToReference(softwareRef, metadata);

                    // Update in database
                    softwareRefRepository.Update(softwareRef);

                    log.Info($"Successfully enriched '{softwareRef.Name}' from {metadata.Source}");
                    return (true, metadata.Source);
                }

                // Session 12 Continuation: Set LastEnrichmentAttempt even on failure (for rate limiting)
                log.Debug($"No metadata found for '{softwareRef.Name}' - marking attempt date");
                softwareRef.LastEnrichmentAttempt = DateTime.UtcNow;
                softwareRef.UpdatedAt = DateTime.UtcNow;
                softwareRefRepository.Update(softwareRef);

                return (false, null);
            }
            catch (Exception ex)
            {
                log.Error($"Error enriching metadata for '{softwareRef.Name}': {ex.Message}", ex);

                // Session 12 Continuation: Set LastEnrichmentAttempt even on error (for rate limiting)
                try
                {
                    softwareRef.LastEnrichmentAttempt = DateTime.UtcNow;
                    softwareRef.UpdatedAt = DateTime.UtcNow;
                    softwareRefRepository.Update(softwareRef);
                    log.Debug($"Marked enrichment attempt date despite error");
                }
                catch (Exception updateEx)
                {
                    log.Error($"Failed to update LastEnrichmentAttempt: {updateEx.Message}");
                }

                return (false, null);
            }
        }

        /// <summary>
        /// Enriches multiple SoftwareReference entries in batch.
        /// Session 12: NEW method for two-tier architecture.
        /// </summary>
        /// <param name="softwareRefs">List of SoftwareReference to enrich</param>
        /// <returns>Number of successfully enriched entries</returns>
        public async Task<int> EnrichSoftwareReferenceBatchAsync(List<SoftwareReference> softwareRefs)
        {
            if (softwareRefs == null || softwareRefs.Count == 0)
                return 0;

            int enrichedCount = 0;
            log.Info($"Starting batch enrichment for {softwareRefs.Count} software reference entries");

            foreach (var softwareRef in softwareRefs)
            {
                var (success, metadataSource) = await EnrichSoftwareReferenceAsync(softwareRef);
                if (success)
                {
                    enrichedCount++;
                }

                // Small delay between requests to avoid rate limiting
                await Task.Delay(500);
            }

            log.Info($"Batch enrichment complete: {enrichedCount}/{softwareRefs.Count} enriched");
            return enrichedCount;
        }

        /// <summary>
        /// Enriches software using game metadata providers.
        /// </summary>
        private async Task<MetadataResult> EnrichWithGameProviders(InstalledSoftware software)
        {
            // Try providers in priority order
            foreach (var provider in gameProviders.Where(p => p.IsAvailable()))
            {
                try
                {
                    log.Debug($"Trying game provider: {provider.ProviderName}");

                    MetadataResult result = null;

                    // Session 12: Try Steam AppID lookup ONLY for actual Steam games
                    // Check: 1) Is it a game, 2) Source is exactly "Steam", 3) RegistryKey starts with "Steam:"
                    if (software.Category == SoftwareCategory.Games &&
                        software.Source == "Steam" &&
                        software.RegistryKey?.StartsWith("Steam:") == true)
                    {
                        string appIdStr = software.RegistryKey.Substring("Steam:".Length);
                        if (int.TryParse(appIdStr, out int steamAppId))
                        {
                            log.Debug($"Attempting Steam AppID lookup for {software.Name} (AppID: {steamAppId})");
                            result = await provider.GetBySteamAppIdAsync(steamAppId);
                        }
                    }

                    // Fallback to name search
                    if (result == null)
                    {
                        result = await provider.SearchByNameAsync(software.Name);
                    }

                    if (result != null)
                    {
                        log.Debug($"Game provider {provider.ProviderName} returned confidence: {result.Confidence:F2}");
                        // Session 12: Threshold is 0.85 (85% name similarity)
                        // Confidence now based on name matching quality instead of ratings count
                        if (result.Confidence >= 0.85)
                        {
                            return result;
                        }
                        else
                        {
                            log.Debug($"Confidence {result.Confidence:F2} too low (threshold: 0.85), rejecting result");
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Warn($"Provider {provider.ProviderName} failed: {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Enriches software using software metadata providers.
        /// </summary>
        private async Task<MetadataResult> EnrichWithSoftwareProviders(InstalledSoftware software)
        {
            // Try providers in priority order
            foreach (var provider in softwareProviders.Where(p => p.IsAvailable()))
            {
                try
                {
                    log.Debug($"Trying software provider: {provider.ProviderName}");

                    MetadataResult result = null;

                    // Try search with publisher if available
                    if (!string.IsNullOrEmpty(software.Publisher))
                    {
                        result = await provider.SearchByNameAndPublisherAsync(software.Name, software.Publisher);
                    }

                    // Fallback to name-only search
                    if (result == null)
                    {
                        result = await provider.SearchByNameAsync(software.Name);
                    }

                    if (result != null)
                    {
                        log.Debug($"Software provider {provider.ProviderName} returned confidence: {result.Confidence:F2}");
                        if (result.Confidence > 0.5)
                        {
                            return result;
                        }
                        else
                        {
                            log.Debug($"Confidence {result.Confidence:F2} too low (threshold: 0.5), rejecting result");
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Warn($"Provider {provider.ProviderName} failed: {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Applies metadata result to software object.
        /// Session 12: Now populates all enriched metadata fields (Description, Genres, Rating, etc.)
        /// </summary>
        private void ApplyMetadata(InstalledSoftware software, MetadataResult metadata)
        {
            if (metadata == null)
                return;

            // Only update fields that are empty or improve quality
            if (string.IsNullOrEmpty(software.Publisher) && !string.IsNullOrEmpty(metadata.Publisher))
            {
                software.Publisher = metadata.Publisher;
            }

            // For games, prefer metadata name (often cleaner)
            if (IsGameSource(software) && !string.IsNullOrEmpty(metadata.Name))
            {
                software.Name = metadata.Name;
            }

            // Session 12: Populate all enriched metadata fields
            if (!string.IsNullOrEmpty(metadata.Description))
                software.Description = metadata.Description;

            if (!string.IsNullOrEmpty(metadata.Genres))
                software.Genres = metadata.Genres;

            if (!string.IsNullOrEmpty(metadata.Developers))
                software.Developers = metadata.Developers;

            if (metadata.ReleaseDate.HasValue)
                software.ReleaseDate = metadata.ReleaseDate;

            if (!string.IsNullOrEmpty(metadata.IconUrl))
                software.CoverImageUrl = metadata.IconUrl;
            else if (!string.IsNullOrEmpty(metadata.BackgroundImageUrl))
                software.CoverImageUrl = metadata.BackgroundImageUrl;

            if (metadata.Rating.HasValue)
                software.Rating = metadata.Rating;

            string descPreview = metadata.Description != null && metadata.Description.Length > 50
                ? metadata.Description.Substring(0, 50) + "..."
                : metadata.Description;
            log.Debug($"Applied metadata: Publisher={metadata.Publisher}, Description={descPreview}, Genres={metadata.Genres}, Source={metadata.Source}");
        }

        /// <summary>
        /// Determines if the software is a game.
        /// Session 12: Now checks BOTH Source field and Category field.
        /// </summary>
        private bool IsGameSource(InstalledSoftware software)
        {
            if (software == null)
                return false;

            // Check 1: Is it categorized as a game?
            if (software.Category == SoftwareCategory.Games)
                return true;

            // Check 2: Is it from a gaming platform?
            if (!string.IsNullOrEmpty(software.Source))
            {
                string lowerSource = software.Source.ToLower();
                if (lowerSource.Contains("steam") ||
                    lowerSource.Contains("gog") ||
                    lowerSource.Contains("epic") ||
                    lowerSource.Contains("amazon games") ||
                    lowerSource.Contains("ea app") ||
                    lowerSource.Contains("origin") ||
                    lowerSource.Contains("ubisoft") ||
                    lowerSource.Contains("battlenet") ||
                    lowerSource.Contains("xbox"))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets statistics about available providers.
        /// </summary>
        public ProviderStatistics GetProviderStatistics()
        {
            return new ProviderStatistics
            {
                GameProvidersAvailable = gameProviders.Count(p => p.IsAvailable()),
                GameProvidersTotal = gameProviders.Count,
                SoftwareProvidersAvailable = softwareProviders.Count(p => p.IsAvailable()),
                SoftwareProvidersTotal = softwareProviders.Count,
                ProviderNames = gameProviders.Select(p => p.ProviderName)
                    .Concat(softwareProviders.Select(p => p.ProviderName))
                    .ToList()
            };
        }

        // ============================================================================
        // Session 12: NEW methods for SoftwareReference enrichment (two-tier architecture)
        // ============================================================================

        /// <summary>
        /// Determines if the SoftwareReference is a game.
        /// Session 12: Works with SoftwareReference instead of InstalledSoftware.
        /// </summary>
        private bool IsGameSourceRef(SoftwareReference softwareRef)
        {
            if (softwareRef == null)
                return false;

            // Check 1: Is it categorized as a game?
            if (softwareRef.Category == "Games")
                return true;

            // Check 2: Is it from a gaming platform?
            if (!string.IsNullOrEmpty(softwareRef.Source))
            {
                string lowerSource = softwareRef.Source.ToLower();
                if (lowerSource.Contains("steam") ||
                    lowerSource.Contains("gog") ||
                    lowerSource.Contains("epic") ||
                    lowerSource.Contains("amazon games") ||
                    lowerSource.Contains("ea app") ||
                    lowerSource.Contains("origin") ||
                    lowerSource.Contains("ubisoft") ||
                    lowerSource.Contains("battlenet") ||
                    lowerSource.Contains("xbox"))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Enriches SoftwareReference using game metadata providers.
        /// Session 12: Uses ExternalId directly (no RegistryKey parsing needed).
        /// </summary>
        private async Task<MetadataResult> EnrichWithGameProvidersRef(SoftwareReference softwareRef)
        {
            // Try providers in priority order
            foreach (var provider in gameProviders.Where(p => p.IsAvailable()))
            {
                try
                {
                    log.Debug($"Trying game provider: {provider.ProviderName}");

                    MetadataResult result = null;

                    // Session 12: ExternalId is explicit - no parsing needed!
                    if (softwareRef.Source == "Steam" && !string.IsNullOrEmpty(softwareRef.ExternalId))
                    {
                        if (int.TryParse(softwareRef.ExternalId, out int steamAppId))
                        {
                            log.Debug($"Attempting Steam AppID lookup for {softwareRef.Name} (AppID: {steamAppId})");
                            result = await provider.GetBySteamAppIdAsync(steamAppId);
                        }
                    }

                    // Fallback to name search
                    if (result == null)
                    {
                        result = await provider.SearchByNameAsync(softwareRef.Name);
                    }

                    if (result != null)
                    {
                        log.Debug($"Game provider {provider.ProviderName} returned confidence: {result.Confidence:F2}");
                        if (result.Confidence >= 0.85)
                        {
                            return result;
                        }
                        else
                        {
                            log.Debug($"Confidence {result.Confidence:F2} too low (threshold: 0.85), rejecting result");
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Warn($"Provider {provider.ProviderName} failed: {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Enriches SoftwareReference using software metadata providers.
        /// Session 12: Works with SoftwareReference instead of InstalledSoftware.
        /// </summary>
        private async Task<MetadataResult> EnrichWithSoftwareProvidersRef(SoftwareReference softwareRef)
        {
            // Try providers in priority order
            foreach (var provider in softwareProviders.Where(p => p.IsAvailable()))
            {
                try
                {
                    log.Debug($"Trying software provider: {provider.ProviderName}");

                    MetadataResult result = null;

                    // Try search with publisher if available
                    if (!string.IsNullOrEmpty(softwareRef.Publisher))
                    {
                        result = await provider.SearchByNameAndPublisherAsync(softwareRef.Name, softwareRef.Publisher);
                    }

                    // Fallback to name-only search
                    if (result == null)
                    {
                        result = await provider.SearchByNameAsync(softwareRef.Name);
                    }

                    if (result != null)
                    {
                        log.Debug($"Software provider {provider.ProviderName} returned confidence: {result.Confidence:F2}");
                        if (result.Confidence > 0.5)
                        {
                            return result;
                        }
                        else
                        {
                            log.Debug($"Confidence {result.Confidence:F2} too low (threshold: 0.5), rejecting result");
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Warn($"Provider {provider.ProviderName} failed: {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Applies metadata result to SoftwareReference.
        /// Session 12: NEW method - updates software_ref fields directly.
        /// Rating goes in MetadataJson (as requested by user).
        /// </summary>
        private void ApplyMetadataToReference(SoftwareReference softwareRef, MetadataResult metadata)
        {
            if (metadata == null)
                return;

            // Update publisher if empty
            if (string.IsNullOrEmpty(softwareRef.Publisher) && !string.IsNullOrEmpty(metadata.Publisher))
            {
                softwareRef.Publisher = metadata.Publisher;
            }

            // Update enriched metadata fields
            if (!string.IsNullOrEmpty(metadata.Description))
                softwareRef.Description = metadata.Description;

            if (!string.IsNullOrEmpty(metadata.Genres))
                softwareRef.Genres = metadata.Genres;

            if (!string.IsNullOrEmpty(metadata.Developers))
                softwareRef.Developers = metadata.Developers;

            if (metadata.ReleaseDate.HasValue)
                softwareRef.ReleaseDate = metadata.ReleaseDate;

            if (!string.IsNullOrEmpty(metadata.IconUrl))
                softwareRef.CoverImageUrl = metadata.IconUrl;
            else if (!string.IsNullOrEmpty(metadata.BackgroundImageUrl))
                softwareRef.CoverImageUrl = metadata.BackgroundImageUrl;

            // Session 12: Store Rating and other extras in MetadataJson
            var metadataDict = new Dictionary<string, object>();
            if (metadata.Rating.HasValue)
                metadataDict["rating"] = metadata.Rating.Value;
            if (!string.IsNullOrEmpty(metadata.BackgroundImageUrl))
                metadataDict["backgroundImage"] = metadata.BackgroundImageUrl;

            if (metadataDict.Count > 0)
            {
                softwareRef.MetadataJson = Newtonsoft.Json.JsonConvert.SerializeObject(metadataDict);
            }

            // Update enrichment tracking
            softwareRef.LastEnrichedDate = DateTime.UtcNow;
            softwareRef.MetadataSource = metadata.Source;
            softwareRef.LastEnrichmentAttempt = DateTime.UtcNow; // Session 12: Rate limiting
            softwareRef.UpdatedAt = DateTime.UtcNow;

            string descPreview = metadata.Description != null && metadata.Description.Length > 50
                ? metadata.Description.Substring(0, 50) + "..."
                : metadata.Description;
            log.Debug($"Applied metadata to SoftwareReference: Publisher={metadata.Publisher}, Description={descPreview}, Genres={metadata.Genres}, Source={metadata.Source}");
        }
    }

    /// <summary>
    /// Statistics about metadata providers.
    /// </summary>
    public class ProviderStatistics
    {
        public int GameProvidersAvailable { get; set; }
        public int GameProvidersTotal { get; set; }
        public int SoftwareProvidersAvailable { get; set; }
        public int SoftwareProvidersTotal { get; set; }
        public List<string> ProviderNames { get; set; }
    }
}
