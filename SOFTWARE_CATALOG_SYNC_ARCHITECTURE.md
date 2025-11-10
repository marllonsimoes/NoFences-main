# Software Catalog Sync Architecture

**Date:** 2025-11-07 (Session 9)
**Status:** Design Phase

---

## 🎯 Architecture Overview

A **master-replica catalog system** where:
1. **Master Database** = Single source of truth (your admin database)
2. **Web API** = Serves paginated catalog data with change tracking
3. **NoFences** = Syncs incrementally like RSS feed

```
┌──────────────────────────────────────────────────────────┐
│                    MASTER CATALOG                        │
│                  (Source of Truth)                       │
│                                                          │
│  ┌────────────────────────────────────────────┐        │
│  │  SQLite/PostgreSQL Database                │        │
│  │  - Software table (with version tracking)  │        │
│  │  - SteamGames table                        │        │
│  │  - ChangeLogs table (audit trail)          │        │
│  └────────────────┬───────────────────────────┘        │
│                   │                                      │
│  ┌────────────────▼───────────────────────────┐        │
│  │  Admin Interface                            │        │
│  │  - Add/Edit/Delete software                │        │
│  │  - Bulk import                              │        │
│  │  - Quality control                          │        │
│  └─────────────────────────────────────────────┘        │
└──────────────────────────────────────────────────────────┘
                       │
                       │ HTTP REST API
                       ▼
┌──────────────────────────────────────────────────────────┐
│                    WEB API LAYER                         │
│                 (ASP.NET Core / Node.js)                 │
│                                                          │
│  GET /api/catalog/version                                │
│    → Returns current catalog version                     │
│                                                          │
│  GET /api/catalog/software?page=1&pageSize=100           │
│    → Returns paginated software list                     │
│                                                          │
│  GET /api/catalog/software/changes?since=2024-11-01      │
│    → Returns only new/updated entries since date         │
│                                                          │
│  GET /api/catalog/steam?page=1&pageSize=100              │
│    → Returns paginated Steam games                       │
│                                                          │
│  GET /api/catalog/steam/changes?since=2024-11-01         │
│    → Returns only new/updated Steam games                │
└──────────────────────────────────────────────────────────┘
                       │
                       │ HTTPS
                       ▼
┌──────────────────────────────────────────────────────────┐
│                  NOFENCES CLIENTS                        │
│               (Distributed Replicas)                     │
│                                                          │
│  Each NoFences installation:                             │
│  1. Checks remote version                                │
│  2. Compares with local version                          │
│  3. If outdated → fetch changes only                     │
│  4. Updates local SQLite database                        │
│  5. Logs sync timestamp                                  │
└──────────────────────────────────────────────────────────┘
```

---

## 📊 Master Database Schema

### Software Table
```sql
CREATE TABLE Software (
    Id TEXT PRIMARY KEY,              -- Unique ID (e.g., "google-chrome")
    Name TEXT NOT NULL,               -- Software name
    Company TEXT,                     -- Publisher/Developer
    Category TEXT,                    -- Games, Development, etc.
    License TEXT,                     -- Free, Paid, OpenSource
    Description TEXT,                 -- Brief description
    Website TEXT,                     -- Official URL
    IconUrl TEXT,                     -- Icon image URL
    Tags TEXT,                        -- JSON array: ["browser","free"]

    -- Change tracking
    Version INTEGER NOT NULL,         -- Incremental version number
    CreatedAt DATETIME NOT NULL,      -- First added
    UpdatedAt DATETIME NOT NULL,      -- Last modified
    IsDeleted BOOLEAN DEFAULT 0,      -- Soft delete flag

    INDEX idx_updated (UpdatedAt),
    INDEX idx_version (Version)
);
```

### SteamGames Table
```sql
CREATE TABLE SteamGames (
    AppId INTEGER PRIMARY KEY,        -- Steam AppID
    Name TEXT NOT NULL,               -- Game name
    ReleaseDate TEXT,                 -- ISO 8601 date
    Developers TEXT,                  -- JSON array
    Publishers TEXT,                  -- JSON array
    Genres TEXT,                      -- JSON array
    Tags TEXT,                        -- JSON array (top 10)
    HeaderImage TEXT,                 -- Image URL
    PlatformWindows BOOLEAN,          -- Windows support
    PlatformMac BOOLEAN,              -- Mac support
    PlatformLinux BOOLEAN,            -- Linux support
    MetacriticScore INTEGER,          -- 0-100
    PositiveReviews INTEGER,          -- Count
    NegativeReviews INTEGER,          -- Count
    Price DECIMAL(10,2),              -- USD price

    -- Change tracking
    Version INTEGER NOT NULL,         -- Incremental version number
    CreatedAt DATETIME NOT NULL,      -- First added
    UpdatedAt DATETIME NOT NULL,      -- Last modified
    IsDeleted BOOLEAN DEFAULT 0,      -- Soft delete flag

    INDEX idx_updated (UpdatedAt),
    INDEX idx_version (Version)
);
```

### CatalogVersion Table
```sql
CREATE TABLE CatalogVersion (
    Id INTEGER PRIMARY KEY,
    CurrentVersion INTEGER NOT NULL,   -- Current catalog version
    LastUpdated DATETIME NOT NULL,     -- Last modification time
    TotalSoftware INTEGER,             -- Statistics
    TotalSteamGames INTEGER,
    Description TEXT                   -- Version notes
);
```

### ChangeLog Table (Audit Trail)
```sql
CREATE TABLE ChangeLog (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EntityType TEXT NOT NULL,          -- "Software" or "SteamGame"
    EntityId TEXT NOT NULL,            -- Software.Id or SteamGames.AppId
    Action TEXT NOT NULL,              -- "Created", "Updated", "Deleted"
    ChangedAt DATETIME NOT NULL,       -- Timestamp
    ChangedBy TEXT,                    -- Admin user (optional)
    Changes TEXT                       -- JSON of what changed
);
```

---

## 🌐 Web API Endpoints

### 1. Get Catalog Version
```http
GET /api/catalog/version
```

**Response:**
```json
{
  "version": 1234,
  "lastUpdated": "2025-11-07T18:00:00Z",
  "totalSoftware": 9000,
  "totalSteamGames": 10000,
  "description": "November 2025 update"
}
```

**Use case:** NoFences checks this first to see if sync needed.

---

### 2. Get Software (Paginated)
```http
GET /api/catalog/software?page=1&pageSize=100
```

**Response:**
```json
{
  "page": 1,
  "pageSize": 100,
  "totalPages": 90,
  "totalCount": 9000,
  "data": [
    {
      "id": "google-chrome",
      "name": "Google Chrome",
      "company": "Google",
      "category": "Communication",
      "license": "Free",
      "description": "Fast web browser",
      "website": "https://www.google.com/chrome/",
      "iconUrl": "https://cdn.example.com/icons/chrome.png",
      "tags": ["browser", "free"],
      "version": 1234,
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2025-11-07T18:00:00Z"
    }
  ]
}
```

**Use case:** Initial sync - download entire catalog in pages.

---

### 3. Get Software Changes (Incremental Sync)
```http
GET /api/catalog/software/changes?since=2025-11-01T00:00:00Z&version=1200
```

**Response:**
```json
{
  "currentVersion": 1234,
  "changes": [
    {
      "id": "visual-studio-code",
      "name": "Visual Studio Code",
      "company": "Microsoft",
      "category": "Development",
      "license": "Free",
      "description": "Code editor",
      "website": "https://code.visualstudio.com/",
      "tags": ["editor", "free"],
      "version": 1230,
      "updatedAt": "2025-11-05T10:00:00Z",
      "action": "updated"
    },
    {
      "id": "old-software",
      "version": 1220,
      "updatedAt": "2025-11-03T12:00:00Z",
      "action": "deleted"
    }
  ]
}
```

**Fields:**
- `action`: "created", "updated", or "deleted"
- If deleted: only `id`, `version`, `updatedAt`, `action`
- If created/updated: full object

**Use case:** Daily sync - only get what changed since last sync.

---

### 4. Get Steam Games (Paginated)
```http
GET /api/catalog/steam?page=1&pageSize=100
```

**Response:**
```json
{
  "page": 1,
  "pageSize": 100,
  "totalPages": 100,
  "totalCount": 10000,
  "data": [
    {
      "appId": 730,
      "name": "Counter-Strike: Global Offensive",
      "releaseDate": "2012-08-21",
      "developers": ["Valve"],
      "publishers": ["Valve"],
      "genres": ["Action", "FPS"],
      "tags": ["shooter", "competitive"],
      "headerImage": "https://cdn.akamai.steamstatic.com/...",
      "platforms": {
        "windows": true,
        "mac": true,
        "linux": true
      },
      "metacriticScore": 83,
      "positiveReviews": 5000000,
      "negativeReviews": 500000,
      "price": 0,
      "version": 1234,
      "updatedAt": "2025-11-07T18:00:00Z"
    }
  ]
}
```

---

### 5. Get Steam Games Changes (Incremental Sync)
```http
GET /api/catalog/steam/changes?since=2025-11-01T00:00:00Z&version=1200
```

**Response:** Same format as software changes.

---

## 🔄 NoFences Sync Protocol

### Sync State Storage
NoFences stores sync state in local database:

```sql
CREATE TABLE SyncState (
    Id INTEGER PRIMARY KEY,
    LastSyncTimestamp DATETIME,       -- When last synced
    LastSyncVersion INTEGER,          -- Last known catalog version
    SoftwareCount INTEGER,            -- Local count
    SteamGamesCount INTEGER,          -- Local count
    NextSyncDue DATETIME              -- When to sync next
);
```

---

### Initial Sync (First Run)

```
1. GET /api/catalog/version
   → catalogVersion = 1234

2. For page = 1 to totalPages:
     GET /api/catalog/software?page={page}&pageSize=100
     → Insert into local Software table

3. For page = 1 to totalPages:
     GET /api/catalog/steam?page={page}&pageSize=100
     → Insert into local SteamGames table

4. Save SyncState:
   - LastSyncTimestamp = NOW
   - LastSyncVersion = 1234
   - NextSyncDue = NOW + 7 days
```

**Time Estimate:** 9,000 software + 10,000 Steam = 190 pages × 100ms = ~20 seconds

---

### Incremental Sync (Daily/Weekly)

```
1. GET /api/catalog/version
   → remoteVersion = 1250
   → localVersion = 1234
   → If remoteVersion == localVersion: SKIP (no changes)

2. GET /api/catalog/software/changes?version=1234
   → Process changes:
     - "created" → INSERT
     - "updated" → UPDATE
     - "deleted" → DELETE or mark IsDeleted

3. GET /api/catalog/steam/changes?version=1234
   → Process changes (same logic)

4. Update SyncState:
   - LastSyncTimestamp = NOW
   - LastSyncVersion = 1250
   - NextSyncDue = NOW + 7 days
```

**Time Estimate:** If 50 changes → 1 request × 100ms = instant!

---

### Sync Trigger Options

1. **Automatic (Background)**
   - Check daily/weekly (configurable)
   - Run in background thread
   - Show notification when complete

2. **Manual (User-Triggered)**
   - "Check for catalog updates" in menu
   - Shows progress dialog
   - Displays # of new entries

3. **On-Demand (Conditional)**
   - When user creates software fence
   - When categorization fails
   - After app update

---

## 📝 Client-Side Implementation

### SyncService Interface

```csharp
public interface ICatalogSyncService
{
    // Check if sync is needed
    Task<bool> IsSyncNeededAsync();

    // Get remote catalog version
    Task<CatalogVersionInfo> GetRemoteVersionAsync();

    // Perform full initial sync
    Task<SyncResult> PerformInitialSyncAsync(IProgress<SyncProgress> progress);

    // Perform incremental sync
    Task<SyncResult> PerformIncrementalSyncAsync();

    // Check and sync if needed
    Task<SyncResult> SyncIfNeededAsync();
}

public class SyncResult
{
    public bool Success { get; set; }
    public int SoftwareAdded { get; set; }
    public int SoftwareUpdated { get; set; }
    public int SoftwareDeleted { get; set; }
    public int SteamGamesAdded { get; set; }
    public int SteamGamesUpdated { get; set; }
    public int SteamGamesDeleted { get; set; }
    public string ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
}
```

---

## 🔐 Security & Best Practices

### API Security
- ✅ HTTPS only (no HTTP)
- ✅ Rate limiting (prevent abuse)
- ✅ Optional API key for analytics
- ✅ CORS headers for web access
- ✅ Compression (gzip) to reduce bandwidth

### Client Security
- ✅ Validate JSON schema before processing
- ✅ Size limits (don't download gigabytes)
- ✅ Timeout handling (30 second max per request)
- ✅ Retry logic with exponential backoff
- ✅ Checksum validation (optional)

### Data Integrity
- ✅ Transaction-based updates (all or nothing)
- ✅ Backup before sync (rollback on failure)
- ✅ Verify counts after sync
- ✅ Audit log of all changes

---

## 📊 Performance & Scalability

### API Performance
- **Pagination**: 100 items/page = optimal
- **Indexing**: Index on UpdatedAt, Version
- **Caching**: Cache version endpoint (5 min TTL)
- **CDN**: Serve static icon URLs from CDN

### Client Performance
- **Background sync**: Don't block UI
- **Batch inserts**: Use SQLite transactions
- **Progress reporting**: Show % complete
- **Throttling**: Respect rate limits

### Bandwidth Usage
| Sync Type | Data Transfer | Time |
|-----------|---------------|------|
| Initial sync | ~5-10 MB | ~20 sec |
| Daily incremental | ~10-50 KB | <1 sec |
| Weekly incremental | ~50-200 KB | <2 sec |

---

## 🚀 Migration Path

### Phase 1: Setup Master Database
1. Run CatalogNormalizer on CSV files
2. Import normalized data into master SQLite
3. Add version tracking columns
4. Initialize CatalogVersion table

### Phase 2: Build Web API
1. Create ASP.NET Core Web API project
2. Implement endpoints (version, software, steam, changes)
3. Add pagination, filtering
4. Test with Postman/curl

### Phase 3: Deploy API
1. Host on Azure App Service / Heroku / VPS
2. Configure HTTPS (Let's Encrypt)
3. Set up monitoring (Application Insights)
4. Document API (Swagger/OpenAPI)

### Phase 4: Update NoFences Client
1. Implement CatalogSyncService
2. Add sync state tracking
3. Integrate with existing EnhancedInstalledAppsService
4. Add "Check for updates" menu item
5. Test sync flows

---

## 💡 Advanced Features (Future)

### Delta Compression
Instead of sending full objects, send only changed fields:
```json
{
  "id": "visual-studio-code",
  "version": 1230,
  "changes": {
    "description": "New description here",
    "updatedAt": "2025-11-05T10:00:00Z"
  }
}
```

### Binary Protocol
For even more efficiency, use binary format (Protocol Buffers, MessagePack)

### Push Notifications
Instead of polling, use WebSockets or Server-Sent Events to push updates

### Analytics
Track which software is most popular, guide curation efforts

---

## 📋 Action Items

### Immediate (Session 9)
- [x] Design architecture
- [x] Define API contract
- [x] Define database schema
- [ ] Get your feedback on approach

### Next Session
- [ ] Create master database schema
- [ ] Implement normalization to master DB
- [ ] Start building Web API (choose stack)
- [ ] Implement version endpoint
- [ ] Test pagination with real data

### Future
- [ ] Complete Web API
- [ ] Deploy API
- [ ] Build NoFences sync client
- [ ] Test end-to-end sync
- [ ] Build admin interface

---

**This approach gives you:**
✅ Single source of truth (master DB)
✅ Efficient incremental sync (RSS-like)
✅ Scalable (paginated API)
✅ Fast updates (only changed entries)
✅ Professional architecture (production-ready)

**What do you think?** Should we proceed with this design?
