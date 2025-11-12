# Software Catalog Database Architecture

**Created:** 2025-11-07 (Session 9)
**Updated:** 2025-11-07 (Session 9 - Simplified Architecture)
**Purpose:** Explain the single-database architecture and interchangeability

---

## ✅ Simplified Architecture: One Database to Rule Them All

NoFences now uses a **single master catalog database** that serves as the source of truth for all software and game metadata.

### Key Principles

1. **One Database Schema** - All instances use the same master catalog schema
2. **Interchangeable** - Database files can be swapped/replaced without conversion
3. **Read-Only** - Application reads from catalog, rarely writes
4. **Distributable** - Single file can be hosted and downloaded
5. **Version Tracked** - Built-in versioning and change tracking

---

## 📊 Database Architecture

### Two Separate Databases (Different Purposes)

```
┌─────────────────────────┐
│   master_catalog.db     │  ← Software/Game Catalog (Read-Only, Distributable)
│  (MasterCatalogContext) │
├─────────────────────────┤
│ • Software (~9,000)     │  Version-tracked, soft deletes
│ • Games (~76,988)       │  Platform-agnostic entries
│ • CatalogVersion        │  Global version number
│ • ChangeLog             │  Audit trail
└─────────────────────────┘

┌─────────────────────────┐
│       ref.db            │  ← Application Data (Read-Write, Local)
│   (LocalDBContext)      │
├─────────────────────────┤
│ • FolderConfiguration   │  User's folder monitoring rules
│ • BackupConfig          │  User's backup settings
│ • MonitoredPath         │  Paths being watched
│ • DeviceInfo            │  Connected devices
│ • InstalledSteamGames   │  User's installed games tracking
└─────────────────────────┘
```

### Clear Separation of Concerns

| Aspect | master_catalog.db | ref.db |
|--------|-------------------|---------|
| **Purpose** | Software/game metadata | Application configuration |
| **Source** | Downloaded or built from CSV | Created locally |
| **Access** | Read-only (mostly) | Read-write |
| **Distribution** | Shared among all users | User-specific |
| **Updates** | Download new version | Modified by application |
| **Interchangeable** | ✅ YES - Just replace file | ❌ NO - User-specific |

---

## 🏗️ Master Catalog Schema

### Software Table

| Field | Type | Purpose |
|-------|------|---------|
| **Id** | string | Unique identifier (UUID or normalized name) |
| **Name** | string (500) | Software name |
| **Company** | string (500) | Publisher/developer |
| **License** | string (500) | License type (MIT, GPL, Proprietary, etc.) |
| **Description** | string (2000) | Detailed description |
| **Website** | string (1000) | Official website URL |
| **Category** | string (100) | SoftwareCategory enum name |
| **Tags** | string (JSON) | Additional tags as JSON array |
| **Version** | int | Entry version number (increments on changes) |
| **CreatedAt** | DateTime | When entry was first created |
| **UpdatedAt** | DateTime | Last modification timestamp |
| **IsDeleted** | bool | Soft delete flag |
| **LastModifiedBy** | string (100) | Who made the last change |

### Games Table (Platform-Agnostic)

| Field | Type | Purpose |
|-------|------|---------|
| **Id** | string | Unique identifier |
| **Name** | string (500) | Game name |
| **Platforms** | string (JSON) | Array: ["Steam", "GOG", "Epic"] |
| **PlatformIds** | string (JSON) | Object: {"Steam": 730, "GOG": "id"} |
| **Developers** | string (JSON) | Array of developer names |
| **Publishers** | string (JSON) | Array of publisher names |
| **Genres** | string | Comma-separated genres |
| **Tags** | string | Comma-separated tags |
| **ReleaseDate** | string | Release date (various formats) |
| **HeaderImage** | string (1000) | Cover image URL |
| **SupportedOS** | string (JSON) | Object: {"windows": true, "mac": false} |
| **Price** | double? | Current price (USD) |
| **MetacriticScore** | int? | Metacritic rating |
| **PositiveReviews** | int | Positive review count |
| **NegativeReviews** | int | Negative review count |
| **Version, CreatedAt, UpdatedAt, IsDeleted, LastModifiedBy** | (same as Software) | Version tracking fields |

### CatalogVersion Table

| Field | Type | Purpose |
|-------|------|---------|
| **Version** | int | Global catalog version (increments on any change) |
| **LastUpdated** | DateTime | Last catalog modification |

### ChangeLog Table

| Field | Type | Purpose |
|-------|------|---------|
| **Id** | long (auto) | Log entry ID |
| **EntityType** | string | "Software" or "Game" |
| **EntityId** | string | ID of affected entry |
| **ChangeType** | string | "Create", "Update", "Delete" |
| **ChangedBy** | string | Who made the change |
| **ChangedAt** | DateTime | When change occurred |
| **Description** | string | What changed |

---

## 🔄 How It Works

### 1. Building the Catalog (One-Time or Periodic)

```bash
# Build master catalog from CSV files
NoFences.exe --import-catalog _software_list master_catalog.db 76988

# Output: master_catalog.db (~10-15 MB)
```

**What happens:**
- Reads Software.csv (9,000 entries)
- Reads steam.csv (76,988 games)
- Creates master_catalog.db with all entries
- Adds version tracking (Version = 1, UpdatedAt = now)
- Creates ChangeLog entries for all imports

### 2. Distributing the Catalog

**Option A: Upload to server**
```
https://yourserver.com/catalogs/master_catalog.db
https://yourserver.com/catalogs/v2/master_catalog.db
```

**Option B: Bundle with installer**
- Include master_catalog.db in application package
- Extracted to default location on first run

### 3. Client-Side Usage

**On first run:**
```csharp
// Check if catalog exists
if (!SoftwareCatalogInitializer.IsCatalogInitialized())
{
    // Download from remote
    bool success = SoftwareCatalogInitializer.InitializeFromRemote(
        "https://yourserver.com/catalogs/master_catalog.db"
    );
}
```

**During normal operation:**
```csharp
// Application reads directly from master catalog
var catalogPath = SoftwareCatalogService.GetDefaultCatalogPath();
var service = new SoftwareCatalogService(catalogPath);

// Lookup software
var entry = service.LookupByName("Visual Studio Code");
if (entry != null)
{
    Console.WriteLine($"{entry.Name} by {entry.Company}");
    Console.WriteLine($"Category: {entry.Category}");
    Console.WriteLine($"Description: {entry.Description}");
}

// Lookup game
var game = service.LookupGameBySteamAppId(730); // Counter-Strike: Global Offensive
if (game != null)
{
    Console.WriteLine($"{game.Name}");
    Console.WriteLine($"Platforms: {game.Platforms}"); // JSON array
    Console.WriteLine($"Metacritic: {game.MetacriticScore}");
}
```

---

## 🔄 Updating the Catalog

### Server-Side: Build New Version

```bash
# Update CSV files with new entries
# Then rebuild catalog
NoFences.exe --import-catalog _software_list master_catalog_v2.db 76988

# Upload to server
upload master_catalog_v2.db https://yourserver.com/catalogs/
```

### Client-Side: Download Update

```csharp
// Replace catalog with new version
bool success = SoftwareCatalogInitializer.ReplaceCatalogFromRemote(
    "https://yourserver.com/catalogs/master_catalog_v2.db"
);

if (success)
{
    Console.WriteLine("✓ Catalog updated successfully!");

    var stats = SoftwareCatalogInitializer.GetCatalogStatistics();
    Console.WriteLine($"  Software: {stats.TotalSoftware}");
    Console.WriteLine($"  Games: {stats.TotalGames}");
}
```

**ReplaceCatalogFromRemote() features:**
- Backs up current catalog before download
- Downloads new version
- Verifies integrity
- Restores backup if download fails
- Atomic operation (either succeeds completely or fails safely)

---

## ✅ Benefits of This Architecture

### 1. Simplicity
- **No migration layer** - Application reads directly from master catalog
- **No schema conversion** - Same schema everywhere
- **No complexity** - Simple file download and replace

### 2. Interchangeability
- **Database files are interchangeable** - Same schema, same data
- **Easy recovery** - Corrupt? Just re-download
- **Testing** - Swap catalog files for testing different datasets

### 3. Distribution
- **Single file** - One master_catalog.db to distribute
- **Small size** - ~10-15 MB (vs 222 MB CSV)
- **Fast download** - Quick for users to get started

### 4. Versioning
- **Built-in tracking** - Every entry has version number
- **Change history** - ChangeLog records all modifications
- **Global version** - CatalogVersion table tracks catalog version

### 5. Future-Proof
- **Soft deletes** - IsDeleted flag preserves history
- **Incremental sync ready** - Version tracking enables delta updates
- **Audit trail** - ChangeLog enables compliance and debugging

---

## 🚀 Workflow Examples

### Example 1: First-Time User

```
1. User installs NoFences
2. Application starts, checks for catalog
3. Catalog not found
4. Automatically downloads from server
5. User immediately has 9,000 software entries + 76,988 games
6. Accurate categorization from day one
```

### Example 2: Catalog Update

```
1. Developer builds new catalog with updated entries
2. Uploads to server as master_catalog_v2.db
3. Application checks version (future feature)
4. Detects newer version available
5. Downloads and replaces old catalog
6. Users benefit from latest data
```

### Example 3: Corruption Recovery

```
1. User's catalog file becomes corrupted
2. Application detects invalid database
3. Automatically re-downloads from server
4. Fresh catalog restored
5. No user intervention needed
```

### Example 4: Offline Development

```
1. Developer builds catalog locally from CSV
2. Uses local catalog for testing
3. No internet connection needed
4. When ready, uploads to server for distribution
```

---

## 📝 Best Practices

### For Developers

1. **Version your catalogs**
   - Use semantic versioning: `master_catalog_v1.0.db`
   - Track changes in ChangeLog table
   - Increment CatalogVersion on updates

2. **Test before distribution**
   - Verify integrity with SoftwareCatalogService
   - Check entry counts match expectations
   - Test lookup performance

3. **Provide checksums**
   - SHA256 hash for integrity verification
   - Host alongside catalog: `master_catalog.db.sha256`

4. **Keep old versions**
   - Allow users to rollback if needed
   - Archive previous versions on server

### For Users

1. **Let application manage catalog**
   - Don't manually edit database
   - Use replacement mechanism if needed
   - Report corrections to developer

2. **Monitor disk space**
   - Catalog is ~10-15 MB
   - Backups are created during updates
   - Old backups can be cleaned up

3. **Check for updates periodically**
   - New entries added regularly
   - Corrections and improvements
   - Better categorization over time

---

## 🔧 Troubleshooting

### Catalog Not Found

```
Problem: Application can't find master_catalog.db
Solution:
1. Check default path: %APPDATA%\NoFences\master_catalog.db
2. Run: SoftwareCatalogInitializer.InitializeFromRemote()
3. Verify network connection if downloading
```

### Catalog Corrupted

```
Problem: Database file is corrupted or invalid
Solution:
1. Delete corrupted file
2. Run: SoftwareCatalogInitializer.ReplaceCatalogFromRemote()
3. Fresh catalog will be downloaded
```

### Download Fails

```
Problem: Cannot download catalog from server
Solution:
1. Check network connection
2. Verify URL is accessible
3. Check firewall/antivirus settings
4. Try alternative URL or manual download
```

### Lookup Not Working

```
Problem: Software not found in catalog
Fallback: Application uses heuristic categorization
Solution:
1. Entry might not be in catalog yet
2. Try partial name match
3. Submit correction to developer for next version
```

---

## 🔮 Future Enhancements

### Incremental Sync (Planned)

Instead of downloading entire catalog:
```csharp
// Client has version 1, server has version 5
// Download only changes between versions 1-5
var changes = CatalogSyncService.GetChangesSince(version: 1);

// Apply changes locally
foreach (var change in changes)
{
    ApplyChange(change); // Update, Insert, or Delete
}

// Update local version to 5
```

**Benefits:**
- Faster updates (KB vs MB)
- Less bandwidth
- Near real-time sync

### User Contributions (Planned)

```csharp
// User finds incorrect categorization
var correction = new CatalogCorrection
{
    EntryId = "visual-studio-code",
    Field = "Category",
    OldValue = "Development",
    NewValue = "IDEs",
    Reason = "More specific category"
};

// Submit to server
CatalogContributionService.SubmitCorrection(correction);

// Server reviews and merges into next version
// All users benefit from correction
```

---

## 📊 Statistics

### Database Size

| Component | Entries | Size | Percentage |
|-----------|---------|------|------------|
| Software | 9,000 | ~2 MB | 15% |
| Games | 76,988 | ~10 MB | 75% |
| Indexes | - | ~1 MB | 10% |
| **Total** | **85,988** | **~13 MB** | **100%** |

### Comparison

| Format | Size | Compression |
|--------|------|-------------|
| CSV Files | 222 MB | - |
| SQLite DB | 13 MB | 94% reduction |
| gzip DB | 4 MB | 98% reduction |

---

## ✅ Summary

| Question | Answer |
|----------|--------|
| **Can I replace the catalog file?** | ✅ YES - Database is interchangeable |
| **Do I need migration?** | ❌ NO - Application reads directly |
| **Can I download updates?** | ✅ YES - ReplaceCatalogFromRemote() |
| **Is it read-only?** | ✅ MOSTLY - Application rarely writes |
| **Can it be corrupted?** | ⚠️ POSSIBLE - But easy to re-download |
| **Is it version tracked?** | ✅ YES - Built-in versioning |
| **Can users contribute?** | 🔮 PLANNED - Future feature |

---

**The master catalog is now a simple, interchangeable, distributable database file!** 🎉

No migration complexity, no schema conversion, just download and use.
