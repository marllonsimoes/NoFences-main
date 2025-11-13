using log4net;
using NoFencesDataLayer.MasterCatalog;
using NoFencesDataLayer.MasterCatalog.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NoFencesDataLayer.Repositories
{
    /// <summary>
    /// Repository for SoftwareReference table in master_catalog.db.
    /// Manages shareable software/game reference data with enriched metadata.
    /// Session 12: Database architecture refactor.
    /// </summary>
    public class SoftwareReferenceRepository : ISoftwareReferenceRepository
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SoftwareReferenceRepository));
        private readonly MasterCatalogContext context;

        public SoftwareReferenceRepository(MasterCatalogContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Gets a software reference by its database ID.
        /// Session 12: Used for JOIN operations when converting to Core model.
        /// </summary>
        public SoftwareReference GetById(long id)
        {
            try
            {
                var reference = context.SoftwareReferences.Find(id);

                if (reference == null)
                {
                    log.Warn($"SoftwareReference with ID {id} not found");
                }

                return reference;
            }
            catch (Exception ex)
            {
                log.Error($"Error getting software reference by ID {id}: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Finds a software reference by source and external ID.
        /// </summary>
        public SoftwareReference FindByExternalId(string source, string externalId)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(externalId))
                return null;

            try
            {
                var reference = context.SoftwareReferences
                    .FirstOrDefault(s => s.Source == source && s.ExternalId == externalId);

                if (reference != null)
                {
                    log.Debug($"Found existing software reference: {reference.Name} ({source}:{externalId})");
                }

                return reference;
            }
            catch (Exception ex)
            {
                log.Error($"Error finding software reference by ExternalId {source}:{externalId}: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Finds a software reference by name (fallback for software without external ID).
        /// </summary>
        public SoftwareReference FindByName(string name, string source = null)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            try
            {
                var query = context.SoftwareReferences.Where(s => s.Name == name);

                if (!string.IsNullOrEmpty(source))
                {
                    query = query.Where(s => s.Source == source);
                }

                var reference = query.FirstOrDefault();

                if (reference != null)
                {
                    log.Debug($"Found existing software reference by name: {reference.Name} ({reference.Source})");
                }

                return reference;
            }
            catch (Exception ex)
            {
                log.Error($"Error finding software reference by name '{name}': {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Inserts a new software reference entry.
        /// </summary>
        public SoftwareReference Insert(SoftwareReference reference)
        {
            if (reference == null)
                throw new ArgumentNullException(nameof(reference));

            try
            {
                reference.CreatedAt = DateTime.UtcNow;
                reference.UpdatedAt = DateTime.UtcNow;

                context.SoftwareReferences.Add(reference);
                context.SaveChanges();

                log.Info($"Inserted new software reference: {reference.Name} (ID: {reference.Id}, {reference.Source}:{reference.ExternalId})");
                return reference;
            }
            catch (Exception ex)
            {
                log.Error($"Error inserting software reference '{reference.Name}': {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing software reference (typically after enrichment).
        /// </summary>
        public void Update(SoftwareReference reference)
        {
            if (reference == null)
                throw new ArgumentNullException(nameof(reference));

            try
            {
                reference.UpdatedAt = DateTime.UtcNow;

                var existing = context.SoftwareReferences.Find(reference.Id);
                if (existing == null)
                {
                    log.Warn($"Cannot update software reference: ID {reference.Id} not found");
                    return;
                }

                // Update all fields
                existing.Name = reference.Name;
                existing.Publisher = reference.Publisher;
                existing.Category = reference.Category;
                existing.Description = reference.Description;
                existing.Genres = reference.Genres;
                existing.Developers = reference.Developers;
                existing.ReleaseDate = reference.ReleaseDate;
                existing.CoverImageUrl = reference.CoverImageUrl;
                existing.MetadataJson = reference.MetadataJson;
                existing.LastEnrichedDate = reference.LastEnrichedDate;
                existing.MetadataSource = reference.MetadataSource;
                existing.LastEnrichmentAttempt = reference.LastEnrichmentAttempt; // Session 12: Rate limiting
                existing.UpdatedAt = reference.UpdatedAt;

                context.SaveChanges();

                log.Debug($"Updated software reference: {reference.Name} (ID: {reference.Id})");
            }
            catch (Exception ex)
            {
                log.Error($"Error updating software reference ID {reference.Id}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Finds or creates a software reference entry.
        /// This is the main method used during software detection.
        /// </summary>
        public SoftwareReference FindOrCreate(string name, string source, string externalId, string category)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(source))
                throw new ArgumentNullException("Name and Source are required");

            try
            {
                // Try to find by ExternalId first (most reliable)
                SoftwareReference existing = null;
                if (!string.IsNullOrEmpty(externalId))
                {
                    existing = FindByExternalId(source, externalId);
                }

                // Fallback to name lookup (for Registry software without ExternalId)
                if (existing == null)
                {
                    existing = FindByName(name, source);
                }

                // If found, return existing
                if (existing != null)
                {
                    return existing;
                }

                // Not found - create new entry
                var newReference = new SoftwareReference
                {
                    Name = name,
                    Source = source,
                    ExternalId = externalId,
                    Category = category,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var inserted = Insert(newReference);
                log.Info($"Created new software reference: {name} ({source}:{externalId ?? "N/A"}) - ID: {inserted.Id}");

                return inserted;
            }
            catch (Exception ex)
            {
                log.Error($"Error in FindOrCreate for '{name}': {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets all software references that haven't been enriched or need re-enrichment.
        /// Session 12: Enhanced with detailed DEBUG logging to track selection criteria.
        /// Session 12 Continuation: Rate limiting - only returns entries that haven't been attempted today.
        /// </summary>
        public List<SoftwareReference> GetUnenrichedEntries(int maxAge = 30, int maxResults = 100)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-maxAge);
                // Session 12 Continuation: Rate limiting - calculate start of today (midnight UTC)
                var todayStart = DateTime.UtcNow.Date;
                var tomorrowStart = todayStart.AddDays(1);

                log.Debug($"=== GetUnenrichedEntries START === maxAge={maxAge} days, maxResults={maxResults}");
                log.Debug($"  Query: WHERE LastEnrichedDate IS NULL OR LastEnrichedDate < '{cutoffDate:yyyy-MM-dd HH:mm:ss}'");
                log.Debug($"  AND (LastEnrichmentAttempt IS NULL OR LastEnrichmentAttempt < '{todayStart:yyyy-MM-dd}')");
                log.Debug($"  OrderBy: LastEnrichedDate (nulls first)");
                log.Debug($"  Limit: {maxResults}");

                // Get total count before filtering
                var totalCount = context.SoftwareReferences.Count();
                log.Debug($"  Total software_ref entries in database: {totalCount}");

                // Count never enriched
                var neverEnrichedCount = context.SoftwareReferences
                    .Count(s => s.LastEnrichedDate == null);
                log.Debug($"  Never enriched (LastEnrichedDate IS NULL): {neverEnrichedCount}");

                // Count stale entries
                var staleCount = context.SoftwareReferences
                    .Count(s => s.LastEnrichedDate != null && s.LastEnrichedDate < cutoffDate);
                log.Debug($"  Stale entries (LastEnrichedDate < {cutoffDate:yyyy-MM-dd}): {staleCount}");

                // Session 12 Continuation: Rate limiting - only attempt enrichment once per day
                // Include entries where:
                // 1. Never been enriched (LastEnrichedDate IS NULL) OR needs re-enrichment (LastEnrichedDate < cutoffDate)
                // 2. AND (Never attempted OR last attempt was before today)
                // Note: Can't use .Date property in LINQ to Entities, so compare with date ranges instead
                var unenriched = context.SoftwareReferences
                    .Where(s => (s.LastEnrichedDate == null || s.LastEnrichedDate < cutoffDate) &&
                                (s.LastEnrichmentAttempt == null || s.LastEnrichmentAttempt < todayStart))
                    .OrderBy(s => s.LastEnrichedDate ?? DateTime.MinValue) // Null values first (never enriched)
                    .Take(maxResults)
                    .ToList();

                // Count entries filtered out by rate limiting (attempted today)
                // This runs in memory after ToList(), so .Date is allowed
                var attemptedTodayCount = context.SoftwareReferences
                    .Where(s => s.LastEnrichmentAttempt != null && s.LastEnrichmentAttempt >= todayStart && s.LastEnrichmentAttempt < tomorrowStart)
                    .Count();
                if (attemptedTodayCount > 0)
                {
                    log.Debug($"  Filtered out by rate limiting (attempted today): {attemptedTodayCount}");
                }

                log.Info($"Found {unenriched.Count} unenriched software references (max age: {maxAge} days, limit: {maxResults}, rate limited: {attemptedTodayCount})");

                // Log ID range for debugging
                if (unenriched.Count > 0)
                {
                    var minId = unenriched.Min(e => e.Id);
                    var maxId = unenriched.Max(e => e.Id);
                    log.Debug($"  Selected entries ID range: {minId} to {maxId}");
                    log.Debug($"  First 3 entries: {string.Join(", ", unenriched.Take(3).Select(e => $"[{e.Id}] {e.Name}"))}");
                }

                return unenriched;
            }
            catch (Exception ex)
            {
                log.Error($"Error getting unenriched entries: {ex.Message}", ex);
                return new List<SoftwareReference>();
            }
        }

        /// <summary>
        /// Gets all software references.
        /// </summary>
        public List<SoftwareReference> GetAllEntries()
        {
            try
            {
                var all = context.SoftwareReferences.ToList();
                log.Debug($"Retrieved {all.Count} software references");
                return all;
            }
            catch (Exception ex)
            {
                log.Error($"Error getting all software references: {ex.Message}", ex);
                return new List<SoftwareReference>();
            }
        }
    }
}
