# Architecture Review - Session 14
**Date:** November 13, 2025
**Status:** 🔴 CRITICAL - Architecture Pain Points Identified
**Purpose:** Document current two-tier database architecture and propose path forward

---

## Executive Summary

The two-tier database architecture (ref.db + master_catalog.db) was implemented in Sessions 11-12 to separate machine-specific data from shareable reference data. While architecturally sound, **the implementation has created significant complexity** that is impacting development velocity and will become a major pain point if not addressed.

**Key Findings:**
- ✅ **Architectural Concept:** SOLID - separation of concerns is correct
- ⚠️ **Model Duplication:** THREE models for same concept (InstalledSoftware, InstalledSoftwareEntry, SoftwareReference)
- ⚠️ **JOIN Complexity:** Every display operation requires manual JOIN across two databases
- ⚠️ **Testing Burden:** Tests must handle two databases, mock two repositories
- ⚠️ **Developer Confusion:** Which model to use when? Which repository to call?

**Recommendation:** **UNIFY CORE MODELS** while keeping database separation

---

## Current Architecture Analysis

### Database Structure

```
┌──────────────────────────────────────┐
│ master_catalog.db                    │
│ (Shareable Reference Data)           │
├──────────────────────────────────────┤
│ SoftwareReference Table:             │
│ - Id (PK)                            │
│ - Name                               │
│ - Source (Steam, GOG, etc.)          │
│ - ExternalId (AppID, etc.)           │
│ - Category                           │
│ - Description                        │
│ - Genres, Developers                 │
│ - ReleaseDate, CoverImageUrl         │
│ - MetadataJson (Rating, etc.)        │
│ - LastEnrichedDate, MetadataSource   │
│ - CreatedAt, UpdatedAt               │
└──────────────────────────────────────┘
           ↑
           │ Foreign Key (SoftwareRefId)
           │
┌──────────────────────────────────────┐
│ ref.db                               │
│ (Machine-Specific Local Data)        │
├──────────────────────────────────────┤
│ InstalledSoftware Table:             │
│ - Id (PK)                            │
│ - SoftwareRefId (FK)                 │
│ - InstallLocation                    │
│ - ExecutablePath                     │
│ - IconPath                           │
│ - RegistryKey                        │
│ - Version                            │
│ - InstallDate                        │
│ - SizeBytes                          │
│ - LastDetected                       │
│ - CreatedAt, UpdatedAt               │
└──────────────────────────────────────┘
```

### Model Hierarchy (THE PROBLEM)

We have **THREE models** for the same logical entity:

#### 1. InstalledSoftware (Core Model)
**Location:** `NoFencesCore/Model/InstalledSoftware.cs` (206 lines)
**Purpose:** UI/business logic layer representation
**Fields:** Mix of local data + enriched metadata (28 properties)

```csharp
public class InstalledSoftware
{
    // Local data (from ref.db)
    public string InstallLocation { get; set; }
    public string ExecutablePath { get; set; }
    public string IconPath { get; set; }
    public string RegistryKey { get; set; }
    public string Version { get; set; }
    public DateTime? InstallDate { get; set; }

    // Enriched metadata (from master_catalog.db)
    public string Name { get; set; }
    public string Publisher { get; set; }
    public string Description { get; set; }
    public string Genres { get; set; }
    public string Developers { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string CoverImageUrl { get; set; }
    public double? Rating { get; set; }

    // Both databases
    public string Source { get; set; }
    public SoftwareCategory Category { get; set; }
    public long SoftwareRefId { get; set; }

    // Non-serialized
    public Icon CachedIcon { get; set; }
}
```

**Problem:** Represents JOIN result, but doesn't enforce it - can contain partial data

#### 2. InstalledSoftwareEntry (DataLayer Entity)
**Location:** `NoFencesDataLayer/MasterCatalog/Entities/InstalledSoftwareEntry.cs` (89 lines)
**Purpose:** EF6 entity for ref.db table
**Fields:** Only machine-specific data (11 properties)

```csharp
[Table("InstalledSoftware")]
public class InstalledSoftwareEntry
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long SoftwareRefId { get; set; }  // FK to SoftwareReference

    public string InstallLocation { get; set; }
    public string ExecutablePath { get; set; }
    public string IconPath { get; set; }
    public string RegistryKey { get; set; }
    public string Version { get; set; }
    public DateTime? InstallDate { get; set; }
    public long? SizeBytes { get; set; }

    [Required]
    public DateTime LastDetected { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; }
    [Required]
    public DateTime UpdatedAt { get; set; }
}
```

**Problem:** Name collision with InstalledSoftware core model - confusing!

#### 3. SoftwareReference (DataLayer Entity)
**Location:** `NoFencesDataLayer/MasterCatalog/Entities/SoftwareReference.cs`
**Purpose:** EF6 entity for master_catalog.db table
**Fields:** Enriched metadata (18 properties)

```csharp
[Table("SoftwareReference")]
public class SoftwareReference
{
    [Key]
    public long Id { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public string Source { get; set; }  // Steam, GOG, Epic, etc.

    public string ExternalId { get; set; }  // AppID, etc.

    public string Category { get; set; }

    // Enriched metadata
    public string Description { get; set; }
    public string Genres { get; set; }
    public string Developers { get; set; }
    public string Publisher { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string CoverImageUrl { get; set; }

    // Flexible storage (Rating stored here)
    public string MetadataJson { get; set; }

    // Enrichment tracking
    public DateTime? LastEnrichedDate { get; set; }
    public string MetadataSource { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

---

## Pain Points Identified

### 1. Model Confusion
**Severity:** 🔴 HIGH

**Problem:** Developers must constantly decide which model to use:
- Use `InstalledSoftware` for UI/display?
- Use `InstalledSoftwareEntry` for database writes?
- Use `SoftwareReference` for enrichment?
- How to convert between them?

**Example from Code:**
```csharp
// InstalledSoftwareService.cs - Line 100-120
// Manual conversion between models
var localEntry = new InstalledSoftwareEntry
{
    SoftwareRefId = softwareRef.Id,
    InstallLocation = software.InstallLocation,
    ExecutablePath = software.ExecutablePath,
    // ... 10 more fields
};

// Then later... another conversion
public InstalledSoftware ConvertToCoreModel(InstalledSoftwareEntry entry, SoftwareReference reference)
{
    return new InstalledSoftware
    {
        Name = reference.Name,
        Publisher = reference.Publisher,
        Description = reference.Description,
        // ... 15 more fields from reference

        InstallLocation = entry.InstallLocation,
        ExecutablePath = entry.ExecutablePath,
        // ... 8 more fields from entry
    };
}
```

### 2. Manual JOIN Operations
**Severity:** 🔴 HIGH

**Problem:** Every display operation requires manual JOIN across two databases:

```csharp
// From DB_REFACTOR_PROGRESS.md - Lines 303-331
public List<InstalledSoftwareWithMetadata> GetInstalledWithMetadata()
{
    using (var localContext = new LocalDBContext())
    using (var masterContext = new MasterCatalogContext())
    {
        var installed = localContext.InstalledSoftware.ToList();
        var softwareRefs = masterContext.SoftwareReferences.ToList();

        var combined = from inst in installed
                       join sref in softwareRefs on inst.SoftwareRefId equals sref.Id
                       select new InstalledSoftwareWithMetadata
                       {
                           // Copy 28 properties manually...
                       };

        return combined.ToList();
    }
}
```

**Issues:**
- ❌ Manual property mapping (error-prone)
- ❌ Two database connections per query
- ❌ Can't use EF6 navigation properties
- ❌ Can't use EF6 eager loading (.Include())
- ❌ Must load entire tables into memory for JOIN
- ❌ No lazy loading option

### 3. Testing Complexity
**Severity:** 🟠 MEDIUM

**Problem:** Tests must mock two repositories and set up two database contexts:

```csharp
// Example test setup
[Fact]
public void RefreshInstalledSoftware_Should_CreateBothEntries()
{
    // Arrange
    var mockInstalledRepo = new Mock<IInstalledSoftwareRepository>();
    var mockSoftwareRefRepo = new Mock<ISoftwareReferenceRepository>();
    var mockEnrichmentService = new Mock<MetadataEnrichmentService>();

    var service = new InstalledSoftwareService(
        mockInstalledRepo.Object,
        mockSoftwareRefRepo.Object,
        mockEnrichmentService.Object
    );

    // Act
    service.RefreshInstalledSoftware();

    // Assert - must verify calls to BOTH repositories
    mockSoftwareRefRepo.Verify(r => r.FindOrCreate(...), Times.AtLeastOnce());
    mockInstalledRepo.Verify(r => r.Upsert(...), Times.AtLeastOnce());
}
```

### 4. Repository Duplication
**Severity:** 🟠 MEDIUM

**Problem:** Two repositories for conceptually the same data:
- `IInstalledSoftwareRepository` - 12 methods for ref.db
- `ISoftwareReferenceRepository` - 9 methods for master_catalog.db
- Service layer must inject BOTH
- Every operation touches BOTH

### 5. Name Collision
**Severity:** 🟡 LOW (but annoying)

**Problem:** `InstalledSoftware` exists in two namespaces:
- `NoFences.Core.Model.InstalledSoftware` (core model)
- `InstalledSoftwareEntry` has `[Table("InstalledSoftware")]` attribute

**Result:** Confusing table names vs. class names

---

## Current Workarounds and Their Costs

### Workaround 1: Manual Conversions
**File:** `InstalledSoftwareService.cs` - Lines 300-400

**Cost:**
- 100+ lines of mapping code
- Fragile (miss a field = silent data loss)
- Must be updated whenever models change

### Workaround 2: Obsolete Methods
**File:** `InstalledSoftwareService.cs` - Marked `[Obsolete]` but still called

**Cost:**
- Maintain duplicate code paths
- Confusion about which method to use
- Technical debt accumulation

### Workaround 3: Service Layer Complexity
**File:** `InstalledSoftwareService.cs` - 520 lines

**Cost:**
- Service contains database JOIN logic (wrong layer)
- Can't leverage EF6 features
- Difficult to optimize

---

## Proposed Solution: Unified Core Model

### Goal
**Keep the two-database architecture** (ref.db + master_catalog.db) for its benefits:
- ✅ Shareable reference data (master_catalog.db)
- ✅ Machine-specific data (ref.db)
- ✅ Clear separation of concerns

**But simplify the model layer** by:
1. Rename `InstalledSoftwareEntry` → `LocalInstallation`
2. Keep `SoftwareReference` as-is
3. Make `InstalledSoftware` (core model) the **single source of truth**
4. Add factory methods to construct `InstalledSoftware` from database entities

### Proposed Structure

```
┌─────────────────────────────────────────────┐
│ Core Layer (NoFencesCore)                   │
├─────────────────────────────────────────────┤
│ InstalledSoftware (Unified Model)           │
│ - All properties (local + enriched)         │
│ - Factory: FromJoin(LocalInstallation, SoftwareReference) │
│ - Factory: FromLocal(LocalInstallation)      │
│ - Factory: FromReference(SoftwareReference)  │
└─────────────────────────────────────────────┘
                    ↑
                    │ Converts from
                    │
┌─────────────────────────────────────────────┐
│ DataLayer (NoFencesDataLayer)               │
├─────────────────────────────────────────────┤
│ LocalInstallation (ref.db entity)           │
│ - Machine-specific fields only              │
│ - FK: SoftwareRefId                         │
│                                             │
│ SoftwareReference (master_catalog.db entity)│
│ - Enriched metadata only                    │
│ - PK: Id                                    │
└─────────────────────────────────────────────┘
```

### Implementation Plan

#### Phase 1: Rename (30 minutes)
**Goal:** Eliminate name collision

1. Rename `InstalledSoftwareEntry` → `LocalInstallation`
   - File: `NoFencesDataLayer/MasterCatalog/Entities/InstalledSoftwareEntry.cs`
   - Table attribute stays: `[Table("InstalledSoftware")]`

2. Update all references:
   - `LocalDBContext.cs` - DbSet name
   - `InstalledSoftwareRepository.cs` - Return types
   - `InstalledSoftwareService.cs` - Variable types
   - Tests

**Result:** Clear distinction between core model and database entities

#### Phase 2: Add Factory Methods (1 hour)
**Goal:** Encapsulate conversion logic

Add to `InstalledSoftware.cs`:

```csharp
/// <summary>
/// Creates InstalledSoftware from joined database entities.
/// This is the primary way to construct complete software objects.
/// </summary>
public static InstalledSoftware FromJoin(LocalInstallation local, SoftwareReference reference)
{
    return new InstalledSoftware
    {
        // Local data
        SoftwareRefId = local.SoftwareRefId,
        InstallLocation = local.InstallLocation,
        ExecutablePath = local.ExecutablePath,
        IconPath = local.IconPath,
        RegistryKey = local.RegistryKey,
        Version = local.Version,
        InstallDate = local.InstallDate,

        // Reference data
        Name = reference.Name,
        Publisher = reference.Publisher,
        Description = reference.Description,
        Genres = reference.Genres,
        Developers = reference.Developers,
        ReleaseDate = reference.ReleaseDate,
        CoverImageUrl = reference.CoverImageUrl,
        Source = reference.Source,
        Category = ParseCategory(reference.Category),

        // Parse MetadataJson for Rating
        Rating = ExtractRating(reference.MetadataJson)
    };
}

/// <summary>
/// Creates InstalledSoftware from local installation only (no enriched data).
/// Used when SoftwareReference is not available yet.
/// </summary>
public static InstalledSoftware FromLocal(LocalInstallation local)
{
    return new InstalledSoftware
    {
        SoftwareRefId = local.SoftwareRefId,
        InstallLocation = local.InstallLocation,
        ExecutablePath = local.ExecutablePath,
        IconPath = local.IconPath,
        RegistryKey = local.RegistryKey,
        Version = local.Version,
        InstallDate = local.InstallDate,
        Name = "[Loading...]",  // Placeholder
        Source = "Local"
    };
}

private static SoftwareCategory ParseCategory(string categoryString)
{
    if (Enum.TryParse<SoftwareCategory>(categoryString, out var category))
        return category;
    return SoftwareCategory.Other;
}

private static double? ExtractRating(string metadataJson)
{
    if (string.IsNullOrEmpty(metadataJson))
        return null;

    try
    {
        var metadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(metadataJson);
        if (metadata != null && metadata.ContainsKey("rating"))
        {
            return Convert.ToDouble(metadata["rating"]);
        }
    }
    catch
    {
        // Silent fail - rating is optional
    }

    return null;
}
```

#### Phase 3: Simplify Service Layer (2 hours)
**Goal:** Remove manual conversion code

**Before:**
```csharp
public InstalledSoftware ConvertToCoreModel(InstalledSoftwareEntry entry, SoftwareReference reference)
{
    // 50 lines of manual mapping
}
```

**After:**
```csharp
public InstalledSoftware ConvertToCoreModel(LocalInstallation local, SoftwareReference reference)
{
    return InstalledSoftware.FromJoin(local, reference);
}
```

#### Phase 4: Add Repository Helper (1 hour)
**Goal:** Encapsulate JOIN logic

Add to `InstalledSoftwareRepository.cs`:

```csharp
/// <summary>
/// Gets all installed software with enriched metadata (performs JOIN).
/// This is the primary method for UI display.
/// </summary>
public List<InstalledSoftware> GetAllWithMetadata(
    ISoftwareReferenceRepository softwareRefRepo)
{
    using (var localContext = new LocalDBContext())
    using (var masterContext = new MasterCatalogContext())
    {
        var localInstalls = localContext.InstalledSoftware.ToList();
        var references = masterContext.SoftwareReferences.ToList();

        var result = from local in localInstalls
                     join reference in references on local.SoftwareRefId equals reference.Id
                     select InstalledSoftware.FromJoin(local, reference);

        return result.ToList();
    }
}

/// <summary>
/// Gets all installed software without enriched metadata (faster, no JOIN).
/// Use when metadata is not needed.
/// </summary>
public List<InstalledSoftware> GetAllLocalOnly()
{
    using (var localContext = new LocalDBContext())
    {
        var localInstalls = localContext.InstalledSoftware.ToList();
        return localInstalls.Select(InstalledSoftware.FromLocal).ToList();
    }
}
```

#### Phase 5: Update Tests (1 hour)
**Goal:** Verify refactor didn't break anything

1. Update test fixtures to use `LocalInstallation`
2. Test factory methods
3. Verify JOIN logic

---

## Benefits of Proposed Solution

### 1. Developer Experience
✅ **Single model to understand:** `InstalledSoftware` is the only public-facing model
✅ **Clear naming:** `LocalInstallation` vs. `SoftwareReference` - no confusion
✅ **Factory methods:** `FromJoin()` makes intent explicit

### 2. Code Simplification
✅ **Remove 100+ lines** of manual mapping code
✅ **Service layer focus:** Business logic, not JOIN operations
✅ **Testability:** Mock repositories, not JOIN logic

### 3. Maintainability
✅ **Single source of truth:** Change `InstalledSoftware` properties once
✅ **Compile-time safety:** Factory methods catch missing fields
✅ **Clear contracts:** Each factory method documents its purpose

### 4. Performance (No Change)
➡️ **Same JOIN operations:** Just moved to repository layer (correct place)
➡️ **Same database calls:** No extra queries
➡️ **Can optimize later:** Centralized JOIN logic easier to optimize

---

## Alternative Considered: Single Database

### Approach
Merge ref.db and master_catalog.db into one database with foreign keys.

### Pros
- Native EF6 navigation properties
- Automatic JOINs with .Include()
- Single DbContext
- True relational database

### Cons
- ❌ Lose shareability of master_catalog.db
- ❌ Lose ability to crowdsource reference data
- ❌ Migrations become more complex (two tables always migrate together)
- ❌ Can't distribute pre-built catalog database
- ❌ Lose clear separation of concerns

### Decision
**Reject** - The two-database architecture is sound, just needs better model unification

---

## Risks and Mitigation

### Risk 1: Breaking Changes
**Likelihood:** 🟠 MEDIUM
**Impact:** 🔴 HIGH (code won't compile)

**Mitigation:**
1. Phase 1 (Rename) is low-risk - IDE can find/replace
2. Add `[Obsolete]` attributes during transition
3. Keep old conversion methods temporarily
4. Test thoroughly before committing

### Risk 2: Performance Regression
**Likelihood:** 🟢 LOW
**Impact:** 🟡 MEDIUM

**Mitigation:**
1. JOIN logic is same - just moved
2. Add performance tests for JOIN operations
3. Profile before and after

### Risk 3: Test Failures
**Likelihood:** 🟠 MEDIUM
**Impact:** 🟡 MEDIUM

**Mitigation:**
1. Update tests incrementally (per phase)
2. Keep test coverage at 100% during refactor
3. Add integration tests for factory methods

---

## Estimated Effort

| Phase | Task | Estimate | Risk |
|-------|------|----------|------|
| 1 | Rename InstalledSoftwareEntry → LocalInstallation | 30 min | 🟢 Low |
| 2 | Add factory methods to InstalledSoftware | 1 hour | 🟢 Low |
| 3 | Simplify InstalledSoftwareService | 2 hours | 🟠 Medium |
| 4 | Add repository JOIN helper methods | 1 hour | 🟢 Low |
| 5 | Update tests | 1 hour | 🟠 Medium |
| 6 | Update UI layer (FilesFenceHandlerWpf) | 1 hour | 🟢 Low |
| 7 | Testing and validation | 1 hour | 🟠 Medium |

**Total Estimate:** 7.5 hours (1 full development day)

---

## Recommendation

**Priority:** 🔴 CRITICAL - Do before adding more features

**Rationale:**
1. **Technical Debt:** Model confusion will compound with every new feature
2. **Developer Velocity:** Current architecture slows down development
3. **Testing Burden:** Gets worse with each new test
4. **Onboarding:** New developers will struggle with three models

**Action Plan:**
1. ✅ Get stakeholder approval for 1-day refactor
2. ✅ Create feature branch: `refactor/unified-core-model`
3. ✅ Implement phases 1-7
4. ✅ Run full test suite
5. ✅ Merge to main
6. ✅ Update documentation

**Success Criteria:**
- All tests pass (100% coverage maintained)
- Zero compilation warnings
- Simplified InstalledSoftwareService (< 400 lines, down from 520)
- Clear model hierarchy documented

---

## Next Steps After Refactor

Once core model is unified:
1. **Add installer service handling** - Simpler with clear model
2. **Complete batch enrichment loop** - Service layer is cleaner
3. **Add integration tests** - Easier with unified model
4. **Documentation** - Update CLAUDE.md with new architecture

---

## Conclusion

The two-tier database architecture is **architecturally correct** but **implementation needs refinement**. By unifying the core model layer while keeping database separation, we get:

✅ Best of both worlds: Clean code + shareable data
✅ Reduced complexity: 1 model instead of 3
✅ Better maintainability: Factory methods encapsulate conversions
✅ Preserved architecture: Two databases stay separated

**Recommendation: PROCEED with unified core model refactor before Session 14 features**

---

**Document Version:** 1.0
**Author:** Claude Code
**Date:** 2025-11-13
**Status:** Pending Approval
