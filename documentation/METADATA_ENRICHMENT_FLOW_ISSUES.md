# Metadata Enrichment Flow - Critical Issues

**Date:** 2025-11-12
**Status:** 🔴 CRITICAL BUGS FOUND

## Executive Summary

The metadata enrichment system has **3 critical data loss bugs** that cause:
1. **Wrong games being looked up** (Steam AppID replaced with database ID)
2. **Metadata not being saved** (GameInfo.Metadata dictionary lost)
3. **Confusion about data sources** (Registry vs. specialized detectors)

---

## Issue #1: Steam AppID Data Loss 🔴 CRITICAL

### Problem
Steam AppIDs are lost during database round-trip, causing wrong games to be looked up.

### Data Flow (Current - BROKEN):

```
Steam Detector
├─ Finds: Team Fortress 2
├─ AppID: 440
└─ Creates: GameInfo
    ├─ GameId = "440" ✓
    └─ Metadata["AppID"] = "440" ✓

                ↓ GetGamesFromStore()

InstalledSoftware
├─ Name = "Team Fortress 2"
├─ RegistryKey = "Steam:440" ✓
└─ [Metadata dictionary NOT COPIED] ✗

                ↓ ConvertToEntity()

InstalledSoftwareEntry (Database)
├─ Id = 5 (auto-increment)
├─ Name = "Team Fortress 2"
├─ Source = "Steam"
└─ [NO RegistryKey field in database!] ✗

                ↓ Save to database

Database assigns auto-increment ID = 5

                ↓ Load from database

ConvertToCoreModel()
├─ Reconstructs RegistryKey = "Steam:5" ✗ WRONG!
└─ Should be "Steam:440" but uses database ID instead

                ↓ MetadataEnrichmentService

Enrichment reads RegistryKey = "Steam:5"
├─ Extracts AppID: 5 ✗ WRONG!
├─ Calls RAWG: GetBySteamAppIdAsync(5)
└─ Gets metadata for WRONG GAME!
```

### Root Cause Analysis:

**Problem 1:** `InstalledSoftwareEntry` (database entity) has NO field for storing RegistryKey/AppID
```csharp
// InstalledSoftwareEntry.cs - MISSING FIELD
public class InstalledSoftwareEntry
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Source { get; set; } // "Steam" but no AppID!
    // ❌ NO RegistryKey field
    // ❌ NO AppID field
    // ❌ NO ExternalId field
}
```

**Problem 2:** `ConvertToEntity()` doesn't save RegistryKey
```csharp
// InstalledSoftwareService.cs:115-154
private InstalledSoftwareEntry ConvertToEntity(InstalledSoftware software)
{
    return new InstalledSoftwareEntry
    {
        Name = software.Name,
        Source = software.Source,
        // ❌ software.RegistryKey is NOT saved!
    };
}
```

**Problem 3:** `ConvertToCoreModel()` reconstructs WRONG RegistryKey
```csharp
// InstalledSoftwareService.cs:332
RegistryKey = $"{entry.Source}:{entry.Id}" // ❌ Uses database ID instead of AppID!
// Should be: "Steam:440" (AppID from Steam)
// Actually is: "Steam:5" (database auto-increment ID)
```

---

## Issue #2: GameInfo.Metadata Dictionary Lost 🔴 CRITICAL

### Problem
GameInfo stores metadata in a dictionary, but it's completely lost when converting to InstalledSoftware.

### Data Flow (Current - BROKEN):

```
Steam Detector
└─ GameInfo
    ├─ GameId = "440"
    └─ Metadata = {
        ["AppID"] = "440",
        ["LibraryPath"] = "C:/Steam/steamapps"
    }

                ↓ GetGamesFromStore()

InstalledSoftware
├─ Name = "Team Fortress 2"
├─ RegistryKey = "Steam:440"
└─ [NO Metadata field] ✗

🔴 ALL metadata in GameInfo.Metadata dictionary is LOST!
```

### Root Cause:
```csharp
// InstalledAppsUtil.cs:345-360
private static List<InstalledSoftware> GetGamesFromStore(IGameStoreDetector detector)
{
    var games = detector.GetInstalledGames(); // Returns List<GameInfo>

    foreach (var game in games)
    {
        var software = new InstalledSoftware
        {
            Name = game.Name,
            RegistryKey = $"{detector.PlatformName}:{game.GameId}",
            // ❌ game.Metadata dictionary is NOT copied!
        };
    }
}
```

**InstalledSoftware has NO Metadata field:**
```csharp
// InstalledSoftware.cs - NO Metadata dictionary
public class InstalledSoftware
{
    public string Name { get; set; }
    public string RegistryKey { get; set; }
    // ❌ No Dictionary<string, string> Metadata field
}
```

---

## Issue #3: Startup vs Force Sync Data Sources 🟡 CONFUSING

### Problem
User sees different data after startup vs. manual "Force Sync", but doesn't understand why.

### Data Sources Explained:

#### **On Application Startup:**

```
Program.cs → Initialize()
    ↓
InstalledSoftwareService.RefreshInstalledSoftware()
    ↓
InstalledAppsUtil.GetAllInstalled()
    ↓
    ├─── [1] Registry Scan
    │    ├─ HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall
    │    ├─ HKLM\SOFTWARE\WOW6432Node\...\Uninstall (32-bit apps)
    │    └─ HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall
    │
    └─── [2] GetAllGames()
         ├─ SteamStoreDetector
         ├─ EpicGamesStoreDetector
         ├─ GOGGalaxyDetector
         ├─ UbisoftConnectDetector
         ├─ EAAppDetector
         └─ AmazonGamesDetector
    ↓
Priority-Based Deduplication:
  Priority 1: Specialized detectors (Steam, Epic, etc.)
  Priority 2: Categorized entries (non-Other category)
  Priority 3: First entry as fallback
    ↓
Save to database (ref.db → InstalledSoftware table)
    ↓
[Background] EnrichUnenrichedEntriesAsync(maxBatchSize: 50)
```

**Sources on Startup:**
- ✅ Windows Registry (all installed software)
- ✅ Specialized game detectors (Steam, Epic, GOG, etc.)
- ✅ Deduplication (specialized detectors win over Registry)
- ✅ Automatic enrichment (up to 50 entries, background)

#### **On Manual "Force Sync":**

```
User clicks "Enrich Metadata (Force Sync)" button
    ↓
InstalledSoftwareService.ForceMetadataEnrichment()
    ↓
[Option A] Re-runs RefreshInstalledSoftware()
  → Detects ALL software again (Registry + game detectors)
  → Enriches up to 100 entries
    ↓
[Option B] Just enriches existing database entries
  → Doesn't re-detect software
  → Just fetches metadata for existing entries
  → Enriches up to 100 entries
```

**The Mystery: Which option is actually implemented?**

Let me check the code...

```csharp
// FilesPropertiesPanel.cs (UI button handler)
btnEnrichMetadata_Click()
    ↓
InstalledSoftwareService.ForceMetadataEnrichment()
    ↓
// What does this method actually do?
```

Looking at the implementation:
```csharp
// InstalledSoftwareService.cs:469-498
public async Task<int> ForceMetadataEnrichment(int maxEntries = 100)
{
    // Loads entries from DATABASE
    var unenrichedEntries = repository.GetAllEntries()
        .Where(e => e.LastEnrichedDate == null ||
                    e.LastEnrichedDate < DateTime.UtcNow.AddDays(-30))
        .Take(maxEntries)
        .ToList();

    // Enriches DATABASE entries (doesn't re-detect software)
    // ...
}
```

**Answer:** Force Sync does **NOT** re-detect software from Registry/detectors. It only enriches **existing database entries**.

### Why User Sees More Data After Force Sync:

1. **Startup enrichment:** Limited to 50 entries (background task)
2. **Force Sync:** Can enrich up to 100 entries
3. **More entries get enriched** = more visible data in UI

**Not because of different data sources**, but because **more entries are enriched**.

---

## Complete Sequence Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│ APPLICATION STARTUP - DATABASE POPULATION                            │
└─────────────────────────────────────────────────────────────────────┘

User starts NoFences.exe
    ↓
Program.Main()
    ↓
DependencyInjectionSetup.InitializeIoCContainer()
  → Registers InstalledSoftwareService (singleton)
  → Registers MetadataEnrichmentService (singleton)
  → Registers all detectors and repositories
    ↓
InstalledSoftwareService.Initialize()
    ↓
RefreshInstalledSoftware()
    ↓
    ┌──────────────────────────────────────────┐
    │ STEP 1: DETECT INSTALLED SOFTWARE        │
    └──────────────────────────────────────────┘
    ↓
InstalledAppsUtil.GetAllInstalled()
    │
    ├─ [SOURCE 1] Scan Windows Registry
    │  ├─ HKLM\SOFTWARE\...\Uninstall → ~200 entries
    │  ├─ HKLM\SOFTWARE\WOW6432Node\...\Uninstall → ~50 entries (32-bit)
    │  └─ HKCU\SOFTWARE\...\Uninstall → ~20 entries (user installs)
    │  └─ Returns: List<InstalledSoftware> with Source="Registry"
    │
    └─ [SOURCE 2] GetAllGames()
       ├─ SteamStoreDetector.GetInstalledGames()
       │  ├─ Parses: libraryfolders.vdf + *.acf manifests
       │  └─ Returns: List<GameInfo>
       │      └─ GameInfo {
       │          GameId = "440",
       │          Metadata = { ["AppID"] = "440" }
       │        }
       │
       ├─ [CONVERSION] GetGamesFromStore()
       │  └─ Converts: GameInfo → InstalledSoftware
       │      ├─ RegistryKey = "Steam:440" ✓
       │      └─ Metadata dictionary LOST ✗
       │
       ├─ EpicGamesStoreDetector.GetInstalledGames()
       ├─ GOGGalaxyDetector.GetInstalledGames()
       ├─ UbisoftConnectDetector.GetInstalledGames()
       ├─ EAAppDetector.GetInstalledGames()
       └─ AmazonGamesDetector.GetInstalledGames()
    │
    └─ [DEDUPLICATION] Priority-Based Deduplication
       ├─ Groups by Name (case-insensitive)
       ├─ Priority 1: Specialized detectors (Steam, Epic, etc.) over Registry
       ├─ Priority 2: Categorized entries over uncategorized
       └─ Priority 3: First entry as fallback
    ↓
Combined List<InstalledSoftware> (~250 entries)
  - ~42 Steam games (RegistryKey = "Steam:440", "Steam:730", etc.)
  - ~10 Epic games (RegistryKey = "Epic:XXXXX")
  - ~200 Registry software (RegistryKey = "HKLM\\SOFTWARE\\...")

    ┌──────────────────────────────────────────┐
    │ STEP 2: SAVE TO DATABASE                 │
    └──────────────────────────────────────────┘
    ↓
For each InstalledSoftware:
    ↓
ConvertToEntity(software)
    ↓
new InstalledSoftwareEntry {
    Name = "Team Fortress 2",
    Source = "Steam",
    // ❌ RegistryKey NOT saved (no field in database)
}
    ↓
Database INSERT/UPDATE
  → Auto-assigns ID = 5
    ↓
Database now contains:
  { Id: 5, Name: "Team Fortress 2", Source: "Steam" }
  // ❌ AppID 440 is LOST!

    ┌──────────────────────────────────────────┐
    │ STEP 3: AUTOMATIC ENRICHMENT (Background)│
    └──────────────────────────────────────────┘
    ↓
Task.Run( EnrichUnenrichedEntriesAsync(maxBatchSize: 50) )
    ↓
Load 50 unenriched entries from database
    ↓
For each entry:
    ↓
ConvertToCoreModel(entry)
    ↓
new InstalledSoftware {
    Name = "Team Fortress 2",
    Source = "Steam",
    RegistryKey = "Steam:5"  // ❌ WRONG! Should be "Steam:440"
}
    ↓
MetadataEnrichmentService.EnrichSoftwareAsync(software)
    ↓
IsGameSource(software) → true (Source = "Steam")
    ↓
EnrichWithGameProviders(software)
    ↓
if (software.RegistryKey?.StartsWith("Steam:"))
{
    string appIdStr = "5"; // ❌ WRONG AppID!
    RawgApiClient.GetBySteamAppIdAsync(5)
        ↓
    RAWG API: https://api.rawg.io/api/games?stores=1&search=5
        ↓
    Returns: Some random game with AppID 5 (NOT Team Fortress 2!)
        ↓
    Saves WRONG metadata to database ✗
}

┌─────────────────────────────────────────────────────────────────────┐
│ MANUAL "FORCE SYNC" - METADATA ENRICHMENT                            │
└─────────────────────────────────────────────────────────────────────┘

User clicks "Enrich Metadata (Force Sync)" button
    ↓
FilesPropertiesPanel.btnEnrichMetadata_Click()
    ↓
InstalledSoftwareService.ForceMetadataEnrichment(maxEntries: 100)
    ↓
Load unenriched entries from DATABASE (doesn't re-detect software)
    ↓
repository.GetAllEntries()
  .Where(e => e.LastEnrichedDate == null)
  .Take(100)
    ↓
For each entry (up to 100):
    ↓
Same broken flow as above:
  - ConvertToCoreModel() generates WRONG RegistryKey
  - Enrichment uses WRONG AppID
  - Saves WRONG metadata

🔴 **Result:** More entries get enriched (100 vs 50), but ALL Steam games
    get enriched with WRONG metadata because AppIDs are wrong!
```

---

## Impact Assessment

### Games Affected:
- ✅ **Epic Games:** Name-based search works (no AppID needed)
- ✅ **GOG:** Name-based search works (no AppID needed)
- ✅ **Ubisoft:** Name-based search works (no AppID needed)
- ✅ **EA App:** Name-based search works (no AppID needed)
- ✅ **Amazon Games:** Name-based search works (no AppID needed)
- ❌ **Steam:** BROKEN (wrong AppIDs used, wrong metadata retrieved)

### Data Loss:
- ❌ **Steam AppIDs:** Lost (replaced with database auto-increment IDs)
- ❌ **GameInfo.Metadata:** Lost (dictionary not copied to InstalledSoftware)
- ❌ **Library paths:** Lost (stored in Metadata["LibraryPath"])
- ❌ **Any future metadata:** Will be lost (no mechanism to store it)

---

## Proposed Fixes

### Fix #1: Add External ID Field to Database

**Add new field to InstalledSoftwareEntry:**
```csharp
/// <summary>
/// External platform ID (Steam AppID, GOG ID, Epic Namespace, etc.)
/// Used for API lookups to get accurate metadata.
/// Format: Depends on platform (e.g., "440" for Steam, "XXXXX" for Epic)
/// </summary>
[MaxLength(200)]
public string ExternalId { get; set; }
```

**Update ConvertToEntity to save AppID:**
```csharp
private InstalledSoftwareEntry ConvertToEntity(InstalledSoftware software)
{
    // Extract external ID from RegistryKey
    string externalId = null;
    if (!string.IsNullOrEmpty(software.RegistryKey) && software.RegistryKey.Contains(":"))
    {
        externalId = software.RegistryKey.Split(':')[1]; // "Steam:440" → "440"
    }

    return new InstalledSoftwareEntry
    {
        Name = software.Name,
        Source = software.Source,
        ExternalId = externalId, // ✓ Save the AppID!
        // ... other fields
    };
}
```

**Update ConvertToCoreModel to reconstruct correct RegistryKey:**
```csharp
private InstalledSoftware ConvertToCoreModel(InstalledSoftwareEntry entry)
{
    // Reconstruct RegistryKey from Source and ExternalId
    string registryKey = null;
    if (!string.IsNullOrEmpty(entry.ExternalId))
    {
        registryKey = $"{entry.Source}:{entry.ExternalId}"; // "Steam:440" ✓
    }
    else
    {
        registryKey = $"{entry.Source}:{entry.Id}"; // Fallback to database ID
    }

    return new InstalledSoftware
    {
        Name = entry.Name,
        Source = entry.Source,
        RegistryKey = registryKey, // ✓ Correct AppID!
        // ... other fields
    };
}
```

### Fix #2: Preserve Metadata Dictionary

**Option A: Add Metadata JSON field to database (RECOMMENDED)**
```csharp
// InstalledSoftwareEntry.cs
/// <summary>
/// JSON-serialized metadata dictionary from detector.
/// Stores platform-specific data (library paths, additional IDs, etc.)
/// </summary>
[MaxLength(4000)]
public string MetadataJson { get; set; }
```

**Option B: Add Metadata field to InstalledSoftware**
```csharp
// InstalledSoftware.cs
/// <summary>
/// Platform-specific metadata from detectors.
/// Example: { "AppID": "440", "LibraryPath": "C:/Steam/..." }
/// </summary>
public Dictionary<string, string> Metadata { get; set; }
```

Then serialize/deserialize when converting between InstalledSoftware ↔ InstalledSoftwareEntry.

### Fix #3: Clarify Data Sources in UI

**Add informational text to "Force Sync" button:**
```
┌───────────────────────────────────────────────────┐
│ 📊 Metadata Enrichment                            │
├───────────────────────────────────────────────────┤
│                                                   │
│ ℹ️ Enriches metadata for installed software/games│
│   from online sources (RAWG, Winget, Wikipedia)  │
│                                                   │
│ • Startup: Auto-enriches 50 entries (background) │
│ • Force Sync: Enriches up to 100 entries         │
│                                                   │
│ [Enrich Metadata (Force Sync)]                   │
│                                                   │
│ Note: This does NOT re-detect installed software.│
│ To refresh installed software list, restart app. │
└───────────────────────────────────────────────────┘
```

---

## Testing Checklist

After implementing fixes:

1. ✅ **Steam AppID preserved through database round-trip**
   - Detect Steam game with AppID 440
   - Save to database
   - Load from database
   - Verify RegistryKey = "Steam:440" (not "Steam:5")

2. ✅ **Enrichment uses correct AppID**
   - Enable debug logging
   - Enrich Steam game
   - Verify log: "Attempting Steam AppID lookup for Team Fortress 2 (AppID: 440)"
   - Verify RAWG API call uses correct AppID

3. ✅ **Metadata dictionary preserved**
   - Detect Steam game
   - Verify Metadata["LibraryPath"] is saved to database
   - Load from database
   - Verify Metadata["LibraryPath"] is restored

4. ✅ **Non-Steam platforms unaffected**
   - Epic Games metadata still works (name-based search)
   - GOG metadata still works
   - Ubisoft metadata still works

---

## Migration Strategy

### Database Migration:

1. Add `ExternalId` column (nullable, MaxLength 200)
2. Add `MetadataJson` column (nullable, MaxLength 4000)
3. Run data migration to populate ExternalId from existing data:
   ```sql
   -- Extract AppID from existing data
   UPDATE InstalledSoftware
   SET ExternalId = (
       SELECT CASE
           WHEN Source = 'Steam' THEN
               -- Try to extract from Name or other fields
               -- This will be approximate, may need manual cleanup
               NULL
           ELSE NULL
       END
   )
   ```
4. For existing entries without ExternalId, enrichment will fall back to name-based search

### Code Migration:

1. Add fields to InstalledSoftwareEntry
2. Update ConvertToEntity() to extract and save ExternalId
3. Update ConvertToCoreModel() to reconstruct RegistryKey with ExternalId
4. Update GetGamesFromStore() to copy Metadata dictionary
5. Update serialization/deserialization for MetadataJson

---

## Related Documentation

- `documentation/FilesFence_Rendering_Sequence.md` - FilesFence rendering flow
- `NoFencesCore/Model/InstalledSoftware.cs` - Core data model
- `NoFencesDataLayer/MasterCatalog/Entities/InstalledSoftwareEntry.cs` - Database entity
- `NoFencesDataLayer/Services/InstalledAppsUtil.cs` - Software detection
- `NoFencesDataLayer/Services/InstalledSoftwareService.cs` - Service layer
- `NoFencesDataLayer/Services/Metadata/MetadataEnrichmentService.cs` - Enrichment logic

---

**Status:** 🔴 REQUIRES IMMEDIATE FIX BEFORE v1.7.0 RELEASE
