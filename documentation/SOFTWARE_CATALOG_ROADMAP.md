# Software Catalog System - 3-Phase Roadmap

**Date:** 2025-11-07 (Session 9)
**Status:** Phase 1 in Progress

---

## 🎯 Overview

Transform NoFences from hardcoded keyword matching (~50 keywords) to a comprehensive, maintainable software catalog system with 217,000+ entries.

### Current Problem
- CSV files with 217,367 software entries are unused
- CSV files are huge (steam.csv = 222MB, games.csv = 208MB)
- Bundling these files with the application is wasteful
- No way to maintain/update the catalog

### Solution Architecture
1. **Phase 1**: Normalize CSV → Structured JSON
2. **Phase 2**: Build admin interface to maintain catalog
3. **Phase 3**: Host JSON, NoFences fetches from web endpoint

---

## 📋 Phase 1: Data Normalization (IN PROGRESS)

**Goal:** Convert raw CSV files into clean, structured JSON format

### ✅ Completed Tasks

#### 1. JSON Schema Definition (`SoftwareCatalogSchema.cs`)
Defined structured format with:
- **SoftwareCatalogJson**: Root container with metadata
- **SoftwareEntry**: Normalized software data
  - Id, Name, Company, Category, License
  - Description, Website, IconUrl
  - Aliases, Tags
- **SteamGameEntry**: Normalized Steam game data
  - AppId, Name, ReleaseDate
  - Developers, Publishers, Genres, Tags
  - Platforms (Windows/Mac/Linux)
  - Reviews, Metacritic score, Price
- **CatalogMetadata**: Version, date, counts

#### 2. CSV Normalizer (`CatalogNormalizer.cs`)
Utility to convert CSV → JSON with:
- **Deduplication**: Remove duplicate entries
- **Validation**: Skip invalid/incomplete entries
- **Category Mapping**: Auto-categorize based on keywords
- **Tag Extraction**: Generate relevant tags
- **URL Validation**: Clean and validate URLs
- **List Parsing**: Split comma-separated fields properly

#### 3. Normalization Tool (`CatalogNormalizationTool.cs`)
Console application to run the normalization:
```bash
CatalogNormalizationTool.exe <input_dir> <output_dir> [max_steam_games]
```

**Features:**
- Processes Software.csv and steam.csv
- Limits Steam games (default 10,000) to keep JSON manageable
- Generates 3 output files:
  - `software_catalog.json` (full catalog)
  - `software_only.json` (general software)
  - `steam_games.json` (Steam games)
- Pretty-printed JSON with camelCase naming

### 📊 Expected Output

**Software.csv (9,000 entries) → software_only.json (~500KB)**
```json
{
  "metadata": {
    "version": "1.0.0",
    "generatedDate": "2025-11-07T18:30:00Z",
    "totalSoftware": 8999,
    "description": "Normalized software catalog for NoFences"
  },
  "software": [
    {
      "id": "google-chrome-google",
      "name": "Google Chrome",
      "company": "Google",
      "category": "Communication",
      "license": "Free",
      "description": "Fast web browser",
      "website": "https://www.google.com/chrome/",
      "tags": ["free", "browser"]
    }
  ]
}
```

**steam.csv (76,988 entries) → steam_games.json (10,000 entries, ~8MB)**
```json
{
  "metadata": {
    "version": "1.0.0",
    "generatedDate": "2025-11-07T18:30:00Z",
    "totalSteamGames": 10000
  },
  "steamGames": [
    {
      "appId": 730,
      "name": "Counter-Strike: Global Offensive",
      "releaseDate": "2012-08-21",
      "developers": ["Valve"],
      "publishers": ["Valve"],
      "genres": ["Action", "FPS"],
      "platforms": {
        "windows": true,
        "mac": true,
        "linux": true
      },
      "metacriticScore": 83,
      "positiveReviews": 5000000,
      "negativeReviews": 500000,
      "price": 0
    }
  ]
}
```

### ⚙️ Running the Normalization Tool

**Option 1: Build and run as executable**
```bash
cd NoFencesDataLayer
msbuild /p:StartupObject=NoFencesDataLayer.Util.CatalogNormalizationTool
NoFencesDataLayer.exe ..\_software_list .\normalized_catalogs 10000
```

**Option 2: Call from code**
```csharp
NoFencesDataLayer.Util.CatalogNormalizationTool.Main(new[] {
    @"C:\path\to\_software_list",
    @"C:\path\to\output",
    "10000"
});
```

### 📁 Files Created (Phase 1)

| File | Lines | Description |
|------|-------|-------------|
| `Model/SoftwareCatalogSchema.cs` | 180 | JSON schema classes |
| `Util/CatalogNormalizer.cs` | 280 | CSV → JSON converter |
| `Util/CatalogNormalizationTool.cs` | 120 | Console tool |

---

## 📋 Phase 2: Admin Interface (PLANNED)

**Goal:** Build web/desktop interface to maintain the software catalog

### Requirements

#### 1. CRUD Operations
- **Create**: Add new software/games
- **Read**: Search, filter, browse catalog
- **Update**: Edit existing entries
- **Delete**: Remove obsolete entries

#### 2. Batch Operations
- Import CSV files
- Export to JSON
- Merge duplicate entries
- Bulk categorization

#### 3. Validation & Quality Control
- Required fields validation
- URL format validation
- Category standardization
- Duplicate detection

#### 4. Statistics & Reports
- Entries by category
- Coverage metrics
- Quality scores
- Recent changes

### Technology Options

**Option A: Desktop WPF Admin Tool**
- Pros: Native, fast, works offline
- Cons: Windows only, harder to share access

**Option B: Web Admin Panel (ASP.NET Core)**
- Pros: Cross-platform, multi-user, remote access
- Cons: Requires hosting

**Option C: Simple Console CRUD Tool**
- Pros: Quick to build, no UI complexity
- Cons: Limited usability

### Recommended Approach
Start with **Option C** (console tool) for basic CRUD, then evolve to **Option B** (web panel) for team collaboration.

### Admin Interface Features

```
=== NoFences Catalog Admin ===

1. Search Software
2. Add New Software
3. Edit Software
4. Delete Software
5. View Statistics
6. Export to JSON
7. Import CSV
8. Validate Catalog
9. Exit

Choose option: _
```

---

## 📋 Phase 3: Web Hosting & Sync (PLANNED)

**Goal:** Host JSON catalog online, NoFences fetches updates

### Architecture

```
┌─────────────────┐
│  GitHub Repo    │ ← Admin pushes JSON updates
│  (or CDN)       │
└────────┬────────┘
         │ HTTPS GET
         ▼
┌─────────────────┐
│   NoFences      │ ← Downloads catalog.json
│   Application   │ ← Imports to SQLite
└─────────────────┘
```

### Hosting Options

**Option 1: GitHub Raw Files** (FREE)
- Host JSON in public GitHub repo
- URL: `https://raw.githubusercontent.com/user/repo/main/software_catalog.json`
- Pros: Free, version control, easy updates
- Cons: Rate limits, public only

**Option 2: GitHub Releases** (FREE)
- Attach JSON to releases
- URL: `https://github.com/user/repo/releases/latest/download/software_catalog.json`
- Pros: Free, versioned, no rate limits
- Cons: Manual release process

**Option 3: CDN (Cloudflare, Azure, AWS)**
- Host on edge network
- Pros: Fast, scalable, reliable
- Cons: Cost (usually minimal)

**Option 4: Custom API Endpoint**
- Build REST API with filtering, pagination
- Pros: Most flexible, can do incremental updates
- Cons: Development + hosting cost

### Recommended: GitHub Releases
1. Admin exports JSON from Phase 2 tool
2. Create GitHub release with JSON files attached
3. NoFences downloads from release URL
4. Optional: Check version, only download if newer

### NoFences Integration

**Sync Service** (Already created: `RemoteCatalogSyncService.cs`)
```csharp
public class RemoteCatalogSyncService
{
    public async Task<bool> SyncFromRemoteAsync()
    {
        // Download software_catalog.json
        var json = await httpClient.GetStringAsync(catalogUrl);

        // Parse JSON
        var catalog = JsonSerializer.Deserialize<SoftwareCatalogJson>(json);

        // Import to database
        ImportToDatabase(catalog);

        return true;
    }
}
```

**Update Strategy:**
- Check for updates: Daily/Weekly/On-demand
- Compare versions (metadata.version)
- Download if newer version available
- Import incrementally (only new/changed entries)
- Show notification to user

### Security Considerations
- **Validate JSON**: Schema validation before import
- **HTTPS only**: No HTTP, prevent MITM attacks
- **Checksum verification**: Ensure integrity
- **Size limits**: Don't download huge files blindly
- **Timeout handling**: Don't hang indefinitely

---

## 🗓️ Implementation Timeline

### Week 1: Phase 1 Completion
- [x] Create JSON schema
- [x] Build CSV normalizer
- [x] Create console tool
- [ ] **Run normalization on actual CSVs**
- [ ] Review output quality
- [ ] Adjust categories/tags as needed

### Week 2-3: Phase 2 Implementation
- [ ] Design admin interface (console or web)
- [ ] Implement CRUD operations
- [ ] Add search and filtering
- [ ] Build validation logic
- [ ] Test with real data

### Week 4: Phase 3 Setup
- [ ] Choose hosting solution
- [ ] Upload initial JSON catalog
- [ ] Update RemoteCatalogSyncService URLs
- [ ] Add JSON importer to NoFences
- [ ] Test end-to-end sync
- [ ] Document update procedure

---

## 📊 Success Metrics

| Metric | Current | Phase 1 | Phase 2 | Phase 3 |
|--------|---------|---------|---------|---------|
| Software entries | ~50 keywords | 9,000 JSON | 9,000+ | 10,000+ |
| Steam games | 0 | 10,000 JSON | 10,000+ | 20,000+ |
| Categorization accuracy | ~60% | ~85% | ~95% | ~95% |
| Update frequency | Never | Manual | Manual | Automatic |
| Application size | N/A | +0MB | +0MB | +0MB |
| Data quality | Low | Medium | High | High |

---

## 💡 Future Enhancements

### Phase 4: Community Contributions
- Users can submit missing software
- Moderation queue for admin review
- Voting system for software popularity

### Phase 5: Multi-Store Support
- Windows Store catalog
- Epic Games Store catalog
- GOG Galaxy catalog
- Aggregate all stores

### Phase 6: Smart Features
- AI-powered categorization
- Icon extraction/generation
- Automatic updates from official sources
- Usage analytics

---

## 📝 Quick Start

### To Normalize CSVs Now:

1. Build NoFencesDataLayer project
2. Set `CatalogNormalizationTool` as startup object
3. Run with arguments: `..\_software_list .\output 10000`
4. Review output JSON files
5. Iterate on categories/tags as needed

### To Test JSON Import:

```csharp
// Read JSON
var json = File.ReadAllText("software_catalog.json");
var catalog = JsonSerializer.Deserialize<SoftwareCatalogJson>(json);

// Import to database (create new importer)
using (var db = new LocalDBContext())
{
    var jsonImporter = new JsonCatalogImporter(db);
    jsonImporter.ImportFromJson(catalog);
}
```

---

**Next Step:** Run the normalization tool and review the output!
