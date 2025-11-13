using log4net;
using Newtonsoft.Json;
using NoFences.Core.Model;
using NoFencesDataLayer.MasterCatalog;
using NoFencesDataLayer.MasterCatalog.Entities;
using NoFencesDataLayer.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NoFencesDataLayer.Services
{
    /// <summary>
    /// Service for managing installed software detection and database synchronization.
    /// Database architecture: two-tier system with normalized references.
    /// Part of hybrid architecture: Detectors → Service → Repository → Database.
    /// </summary>
    public class InstalledSoftwareService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(InstalledSoftwareService));
        private readonly IInstalledSoftwareRepository installedRepository;
        private readonly ISoftwareReferenceRepository softwareRefRepository;
        private readonly Metadata.MetadataEnrichmentService enrichmentService;

        /// <summary>
        /// Constructor with dependency injection.
        /// Injects both repositories for two-tier architecture.
        /// </summary>
        public InstalledSoftwareService(
            IInstalledSoftwareRepository installedRepository,
            ISoftwareReferenceRepository softwareRefRepository,
            Metadata.MetadataEnrichmentService enrichmentService)
        {
            this.installedRepository = installedRepository ?? throw new ArgumentNullException(nameof(installedRepository));
            this.softwareRefRepository = softwareRefRepository ?? throw new ArgumentNullException(nameof(softwareRefRepository));
            this.enrichmentService = enrichmentService ?? throw new ArgumentNullException(nameof(enrichmentService));
        }

        /// <summary>
        /// Default constructor creates dependencies internally (for legacy code).
        /// Creates both repositories.
        /// </summary>
        public InstalledSoftwareService()
            : this(
                new InstalledSoftwareRepository(),
                new SoftwareReferenceRepository(new MasterCatalogContext()),
                new Metadata.MetadataEnrichmentService())
        {
        }

        /// <summary>
        /// Scans system for all installed software and updates databases (two-tier architecture).
        /// Two-phase approach:
        /// Phase 1: Find/create software_ref entries (master_catalog.db)
        /// Phase 2: Save local installation data (ref.db)
        /// Phase 3: Trigger enrichment for new software
        /// </summary>
        /// <returns>Number of local installations written to database</returns>
        public int RefreshInstalledSoftware()
        {
            try
            {
                log.Info("Starting installed software detection and database sync (two-tier architecture)");

                // Phase 1: Detect all installed software
                var detectedSoftware = InstalledAppsUtil.GetAllInstalled();
                log.Info($"Detected {detectedSoftware.Count} installed software entries");

                if (detectedSoftware.Count == 0)
                {
                    log.Warn("No installed software detected - this may indicate an error");
                    return 0;
                }

                var localEntries = new List<InstalledSoftwareEntry>();
                var newSoftwareRefIds = new List<long>(); // Track new software for enrichment

                // Phase 2: For each detected software, create/find reference and save local data
                foreach (var software in detectedSoftware)
                {
                    try
                    {
                        // Step 1: Extract ExternalId from RegistryKey
                        string externalId = ExtractExternalId(software.RegistryKey, software.Source);

                        // Step 2: Find or create software_ref entry
                        var softwareRef = softwareRefRepository.FindOrCreate(
                            software.Name,
                            software.Source,
                            externalId,
                            software.Category.ToString()
                        );

                        // Track if this is a new software_ref (needs enrichment)
                        if (softwareRef.LastEnrichedDate == null)
                        {
                            newSoftwareRefIds.Add(softwareRef.Id);
                        }

                        // Step 3: Create local installation entry with FK
                        var localEntry = new InstalledSoftwareEntry
                        {
                            SoftwareRefId = softwareRef.Id, // Foreign key!
                            InstallLocation = software.InstallLocation,
                            ExecutablePath = software.ExecutablePath,
                            IconPath = software.IconPath,
                            RegistryKey = software.RegistryKey, // Full key: "Steam:440", "HKLM\SOFTWARE\..."
                            Version = software.Version,
                            InstallDate = software.InstallDate,
                            SizeBytes = 0, // Could be calculated if needed
                            LastDetected = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        localEntries.Add(localEntry);
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Error processing software '{software.Name}': {ex.Message}", ex);
                    }
                }

                log.Info($"Created {localEntries.Count} local installation entries with software_ref links");

                // Phase 3: Batch write local installations to ref.db
                installedRepository.UpsertBatch(localEntries);

                // Phase 4: Clean up stale entries (not detected in last 30 days)
                var staleThreshold = DateTime.UtcNow.AddDays(-30);
                var removedCount = installedRepository.RemoveStaleEntries(staleThreshold);
                if (removedCount > 0)
                {
                    log.Info($"Removed {removedCount} stale entries (not detected since {staleThreshold:yyyy-MM-dd})");
                }

                log.Info($"Database refresh complete: {localEntries.Count} installations, {newSoftwareRefIds.Count} new software references");

                // Phase 5: Automatic metadata enrichment (background) for ALL unenriched software
                // Process ALL entries in batches until complete
                if (newSoftwareRefIds.Count > 0)
                {
                    log.Info($"Triggering background enrichment for all unenriched software ({newSoftwareRefIds.Count} new entries detected)");
                    Task.Run(async () =>
                    {
                        try
                        {
                            await EnrichAllUnenrichedEntriesAsync();
                        }
                        catch (Exception ex)
                        {
                            log.Error($"Background metadata enrichment failed: {ex.Message}", ex);
                        }
                    });
                }

                return localEntries.Count;
            }
            catch (Exception ex)
            {
                log.Error($"Error refreshing installed software database: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// Extracts ExternalId from RegistryKey based on source.
        /// Examples:
        /// - "Steam:440" → "440"
        /// - "Epic:ue4-mandalore" → "ue4-mandalore"
        /// - "HKLM\\SOFTWARE\\..." → null (Registry entries don't have ExternalId)
        /// </summary>
        private string ExtractExternalId(string registryKey, string source)
        {
            if (string.IsNullOrEmpty(registryKey))
                return null;

            // Platform software uses "Source:ExternalId" format
            if (registryKey.Contains(":") && !registryKey.StartsWith("HK"))
            {
                var parts = registryKey.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    return parts[1]; // Return the ExternalId part
                }
            }

            // Registry keys don't have ExternalId
            return null;
        }




        /// <summary>
        /// Gets the count of installed software entries in the database.
        /// Used to check if database needs initialization.
        /// </summary>
        public int GetInstalledSoftwareCount()
        {
            try
            {
                return installedRepository.GetAll().Count;
            }
            catch (Exception ex)
            {
                log.Error($"Error getting installed software count: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// Gets all installed software from database (query layer for fences).
        /// </summary>
        public List<InstalledSoftwareEntry> GetAll()
        {
            return installedRepository.GetAll();
        }

        /// <summary>
        /// Gets installed software as Core model (InstalledSoftware) for UI consumption.
        /// Converts database entities → Core model objects.
        /// This is the main query method for FileFenceFilter integration.
        /// Updated for two-tier architecture - queries software_ref first, then joins with InstalledSoftware.
        /// </summary>
        /// <param name="category">Category filter (Games, Productivity, etc.) - can be null for all</param>
        /// <param name="source">Source filter (Steam, GOG, etc.) - can be null for all</param>
        /// <returns>List of InstalledSoftware objects ready for display</returns>
        public List<InstalledSoftware> QueryInstalledSoftware(string category = null, string source = null)
        {
            try
            {
                log.Debug($"=== QueryInstalledSoftware START === category='{category}', source='{source}'");

                // Two-tier architecture approach
                // Step 1: Query software_ref for matching entries
                List<SoftwareReference> softwareRefs;

                if (!string.IsNullOrEmpty(source) || !string.IsNullOrEmpty(category))
                {
                    // Get all software_ref entries and filter in memory
                    // (Future optimization: Add filtering methods to ISoftwareReferenceRepository)
                    softwareRefs = softwareRefRepository.GetAllEntries();
                    log.Debug($"Step 1: Retrieved {softwareRefs.Count} total software_ref entries from database");

                    if (!string.IsNullOrEmpty(source))
                    {
                        var beforeFilter = softwareRefs.Count;
                        softwareRefs = softwareRefs
                            .Where(r => string.Equals(r.Source, source, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        log.Debug($"Step 1a: Filtered by source '{source}': {beforeFilter} -> {softwareRefs.Count} entries");

                        if (softwareRefs.Count > 0 && softwareRefs.Count <= 5)
                        {
                            log.Debug($"  Sample matching entries: {string.Join(", ", softwareRefs.Take(5).Select(r => $"[{r.Id}] {r.Name}"))}");
                        }
                    }

                    if (!string.IsNullOrEmpty(category))
                    {
                        var beforeFilter = softwareRefs.Count;
                        softwareRefs = softwareRefs
                            .Where(r => string.Equals(r.Category, category, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        log.Debug($"Step 1b: Filtered by category '{category}': {beforeFilter} -> {softwareRefs.Count} entries");
                    }
                }
                else
                {
                    // No filters - get all
                    softwareRefs = softwareRefRepository.GetAllEntries();
                    log.Debug($"Step 1: Retrieved {softwareRefs.Count} software_ref entries (no filters)");
                }

                // Step 2: Get the IDs of matching software_ref entries
                var softwareRefIds = softwareRefs.Select(r => r.Id).ToHashSet();
                log.Debug($"Step 2: Extracted {softwareRefIds.Count} SoftwareRefIds to match");

                // Step 3: Get all InstalledSoftware entries
                var allEntries = installedRepository.GetAll();
                log.Debug($"Step 3: Retrieved {allEntries.Count} total InstalledSoftware entries from ref.db");

                // Step 4: Filter to only entries with matching SoftwareRefIds
                var matchingEntries = allEntries
                    .Where(e => softwareRefIds.Contains(e.SoftwareRefId))
                    .ToList();

                log.Debug($"Step 4: Filtered to {matchingEntries.Count} InstalledSoftware entries with matching SoftwareRefIds");

                // Step 5: Convert to Core model (joins with software_ref automatically in ConvertToCoreModel)
                var softwareList = matchingEntries
                    .Select(ConvertToCoreModel)
                    .Where(s => s != null)
                    .ToList();

                // Log enrichment stats
                var enrichedCount = softwareList.Count(s => !string.IsNullOrEmpty(s.Description) || !string.IsNullOrEmpty(s.Genres));
                log.Debug($"Step 5: Converted to {softwareList.Count} Core models, {enrichedCount} have enriched metadata");

                if (softwareList.Count > 0 && softwareList.Count <= 3)
                {
                    foreach (var sw in softwareList.Take(3))
                    {
                        var hasMetadata = !string.IsNullOrEmpty(sw.Description) || !string.IsNullOrEmpty(sw.Genres);
                        log.Debug($"  Sample: '{sw.Name}' - Source={sw.Source}, HasMetadata={hasMetadata}");
                    }
                }

                log.Info($"=== QueryInstalledSoftware END === Returned {softwareList.Count} items ({enrichedCount} enriched)");
                return softwareList;
            }
            catch (Exception ex)
            {
                log.Error($"Error querying installed software: {ex.Message}", ex);
                return new List<InstalledSoftware>();
            }
        }

        /// <summary>
        /// Converts InstalledSoftwareEntry (database entity) → InstalledSoftware (Core model).
        /// JOINs with SoftwareReference to get enriched metadata.
        /// </summary>
        private InstalledSoftware ConvertToCoreModel(InstalledSoftwareEntry entry)
        {
            if (entry == null)
                return null;

            try
            {
                // Lookup software_ref for enriched metadata
                var softwareRef = softwareRefRepository.GetById(entry.SoftwareRefId);
                if (softwareRef == null)
                {
                    log.Warn($"SoftwareReference not found for SoftwareRefId={entry.SoftwareRefId}");
                    return null;
                }

                // Parse category enum from software_ref
                SoftwareCategory categoryEnum = SoftwareCategory.Other;
                if (!string.IsNullOrEmpty(softwareRef.Category))
                {
                    Enum.TryParse(softwareRef.Category, out categoryEnum);
                }

                return new InstalledSoftware
                {
                    // Local installation data from ref.db
                    InstallLocation = entry.InstallLocation,
                    ExecutablePath = entry.ExecutablePath,
                    IconPath = entry.IconPath,
                    Version = entry.Version,
                    InstallDate = entry.InstallDate,
                    RegistryKey = entry.RegistryKey, // Full key stored: "Steam:440", "HKLM\\SOFTWARE\\..."

                    // Enriched metadata from master_catalog.db (software_ref)
                    Name = softwareRef.Name,
                    Publisher = softwareRef.Publisher,
                    Source = softwareRef.Source,
                    Category = categoryEnum,
                    Description = softwareRef.Description,
                    Genres = softwareRef.Genres,
                    Developers = softwareRef.Developers,
                    ReleaseDate = softwareRef.ReleaseDate,
                    CoverImageUrl = softwareRef.CoverImageUrl,
                    SoftwareRefId = softwareRef.Id, // FK for reference

                    // Not stored in database
                    UninstallString = null,
                    IsWow64 = false
                };
            }
            catch (Exception ex)
            {
                log.Error($"Error converting entry ID {entry.Id} to core model: {ex.Message}", ex);
                return null;
            }
        }




        /// <summary>
        /// Gets a list of all unique source values from the database.
        /// Queries software_ref table (Source is no longer in InstalledSoftware).
        /// Used to populate the source dropdown in FilesPropertiesPanel.
        /// </summary>
        /// <returns>List of unique source names (e.g., "Steam", "GOG", "Epic Games")</returns>
        public static List<string> GetAvailableSources()
        {
            try
            {
                // Source is now in software_ref table, not InstalledSoftware
                var softwareRefRepository = new SoftwareReferenceRepository(new MasterCatalog.MasterCatalogContext());
                var allEntries = softwareRefRepository.GetAllEntries();

                // Get distinct sources, excluding null/empty
                var sources = allEntries
                    .Where(e => !string.IsNullOrWhiteSpace(e.Source))
                    .Select(e => e.Source)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();

                log.Debug($"GetAvailableSources: Found {sources.Count} unique sources");
                return sources;
            }
            catch (Exception ex)
            {
                log.Error($"Error getting available sources: {ex.Message}", ex);
                // Return common sources as fallback
                return new List<string> { "Steam", "GOG", "Epic Games", "Amazon Games", "Registry" };
            }
        }

        /// <summary>
        /// Enriches ALL un-enriched software entries in the database by processing in batches.
        /// Processes all entries until none remain.
        /// Runs in background to avoid blocking UI.
        /// </summary>
        /// <returns>Task that completes when all entries are enriched</returns>
        public async Task EnrichAllUnenrichedEntriesAsync()
        {
            try
            {
                log.Info("=== EnrichAllUnenrichedEntriesAsync START === Processing ALL unenriched entries in batches");

                int totalEnriched = 0;
                int batchNumber = 0;
                int batchSize = 50; // Process 50 entries per batch

                while (true)
                {
                    batchNumber++;
                    log.Info($"Processing enrichment batch #{batchNumber} (batch size: {batchSize})");

                    // Get next batch of unenriched entries
                    var unenrichedEntries = softwareRefRepository.GetUnenrichedEntries(maxResults: batchSize);

                    if (unenrichedEntries.Count == 0)
                    {
                        log.Info($"No more unenriched entries found - enrichment complete after {batchNumber - 1} batches");
                        break;
                    }

                    log.Info($"Batch #{batchNumber}: Found {unenrichedEntries.Count} unenriched entries");

                    // Enrich this batch
                    int enrichedCount = await enrichmentService.EnrichSoftwareReferenceBatchAsync(unenrichedEntries);
                    totalEnriched += enrichedCount;

                    log.Info($"Batch #{batchNumber}: Enriched {enrichedCount}/{unenrichedEntries.Count} entries (total so far: {totalEnriched})");

                    // Safety check: don't process more than 20 batches (1000 entries) in one session
                    if (batchNumber >= 20)
                    {
                        log.Warn($"Reached maximum batch limit (20 batches, ~1000 entries). Stopping automatic enrichment.");
                        log.Warn($"Use 'Enrich Metadata (Force Sync)' button to continue enrichment.");
                        break;
                    }

                    // Small delay between batches to avoid overloading APIs
                    await Task.Delay(1000);
                }

                log.Info($"=== EnrichAllUnenrichedEntriesAsync END === Total enriched: {totalEnriched} entries across {batchNumber} batches");
            }
            catch (Exception ex)
            {
                log.Error($"Error during automatic metadata enrichment: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Automatically enriches un-enriched software entries in the database (single batch).
        /// Automatic metadata enrichment after database population.
        /// Runs in background to avoid blocking UI.
        /// </summary>
        /// <param name="maxBatchSize">Maximum number of entries to enrich in one batch (default: 50)</param>
        /// <returns>Task that completes when enrichment is done</returns>
        public async Task EnrichUnenrichedEntriesAsync(int maxBatchSize = 50)
        {
            try
            {
                log.Info($"=== EnrichUnenrichedEntriesAsync START === maxBatchSize={maxBatchSize}");

                // Query software_ref for unenriched entries (not InstalledSoftware)
                var unenrichedEntries = softwareRefRepository.GetUnenrichedEntries(maxResults: maxBatchSize);

                if (unenrichedEntries.Count == 0)
                {
                    log.Info("No un-enriched entries found - all entries already have metadata");
                    return;
                }

                log.Info($"Found {unenrichedEntries.Count} un-enriched entries (limit: {maxBatchSize})");

                // Log details about the un-enriched entries
                log.Debug("Un-enriched entries selected for batch:");
                foreach (var entry in unenrichedEntries.Take(10))
                {
                    log.Debug($"  [{entry.Id}] {entry.Name} - Source: {entry.Source}, Category: {entry.Category}");
                }
                if (unenrichedEntries.Count > 10)
                {
                    log.Debug($"  ... and {unenrichedEntries.Count - 10} more entries");
                }

                // Enrich SoftwareReference objects directly (no conversion needed)
                int enrichedCount = await enrichmentService.EnrichSoftwareReferenceBatchAsync(unenrichedEntries);

                log.Info($"=== EnrichUnenrichedEntriesAsync END === {enrichedCount}/{unenrichedEntries.Count} entries enriched successfully");
            }
            catch (Exception ex)
            {
                log.Error($"Error during automatic metadata enrichment: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Statistics about installed software in the database.
    /// </summary>
    public class InstalledSoftwareStatistics
    {
        public int TotalCount { get; set; }
        public Dictionary<string, int> CountByCategory { get; set; }
        public Dictionary<string, int> CountBySource { get; set; }
    }
}
