# Platform-Agnostic Game Catalog Architecture

**Created:** 2025-11-07 (Session 9)
**Reason:** Avoid massive duplication across game platforms

---

## 🎯 The Problem

**Original Design:** Platform-specific game tables (SteamGames, GOGGames, EpicGames, etc.)

**Result:** Massive duplication!
```
SteamGames: Cyberpunk 2077
GOGGames: Cyberpunk 2077
EpicGames: Cyberpunk 2077
XboxGames: Cyberpunk 2077
...
```

**Issues:**
- ❌ Same game stored 5+ times
- ❌ Wasted storage space
- ❌ Difficult to merge data
- ❌ User confusion (which version?)
- ❌ Sync nightmare

---

## ✅ The Solution

**New Design:** One `Games` table, platform-agnostic

**Key Principle:** *Focus on the GAME, not where you bought it*

```
Games: Cyberpunk 2077
  - Platforms: ["Steam", "GOG", "Epic", "Xbox", "PlayStation"]
  - PlatformIds: {"Steam": 1091500, "GOG": "1423049311", ...}
```

**Benefits:**
- ✅ Each game appears exactly once
- ✅ All platforms listed in one entry
- ✅ Easy to query "all games" regardless of platform
- ✅ Merge data from multiple sources
- ✅ Users see "Games", not "Steam Games vs GOG Games"

---

## 🗂️ Database Schema

### Games Table

```sql
CREATE TABLE Games (
    -- Identity
    Id TEXT PRIMARY KEY,                -- "cyberpunk-2077"
    Name TEXT NOT NULL,                 -- "Cyberpunk 2077"

    -- Platform Information (JSON arrays/objects)
    Platforms TEXT,                     -- ["Steam", "GOG", "Epic"]
    PlatformIds TEXT,                   -- {"Steam": 1091500, "GOG": "1423..."}

    -- Game Metadata
    ReleaseDate TEXT,                   -- "2020-12-10"
    Developers TEXT,                    -- ["CD Projekt Red"]
    Publishers TEXT,                    -- ["CD Projekt"]
    Genres TEXT,                        -- ["RPG", "Action"]
    Tags TEXT,                          -- ["open-world", "cyberpunk"]
    Description TEXT,                   -- Brief description
    HeaderImage TEXT,                   -- Cover art URL
    Website TEXT,                       -- Official site

    -- Platform Support
    SupportedOS TEXT,                   -- {"windows": true, "mac": false, ...}

    -- Ratings & Reviews (aggregated)
    MetacriticScore INTEGER,            -- 86
    PositiveReviews INTEGER,            -- Combined across platforms
    NegativeReviews INTEGER,            -- Combined across platforms

    -- Pricing
    Price REAL,                         -- Typical price (stored as double)
    IsFreeToPlay BOOLEAN,               -- Is it F2P?
    Rating TEXT,                        -- "M" (ESRB rating)

    -- Version Tracking
    Version LONG NOT NULL,              -- Entry version
    CreatedAt DATETIME NOT NULL,        -- When added
    UpdatedAt DATETIME NOT NULL,        -- When modified
    IsDeleted BOOLEAN,                  -- Soft delete
    LastModifiedBy TEXT,                -- Who changed it

    -- Metadata Tracking
    DataSource TEXT                     -- "Steam", "IGDB", etc.
);
```

---

## 📝 JSON Field Examples

### Platforms
```json
["Steam", "GOG", "Epic", "Xbox", "PlayStation"]
```

**Simple string array** of platform names.

### PlatformIds
```json
{
  "Steam": 1091500,
  "GOG": "1423049311",
  "Epic": "epic-game-id",
  "Xbox": "9NXQXXXXX",
  "PlayStation": "CUSA12345"
}
```

**Object mapping** platform name → platform-specific ID.

### SupportedOS
```json
{
  "windows": true,
  "mac": true,
  "linux": false,
  "steamdeck": true
}
```

**Boolean flags** for each OS.

### Developers/Publishers/Genres/Tags
```json
["CD Projekt Red"]
["CD Projekt"]
["RPG", "Action", "Open World"]
["cyberpunk", "futuristic", "story-rich"]
```

**Simple string arrays**.

---

## 🔄 Import Process

### Importing from Steam

```csharp
// Read steam.csv
var steamEntry = ParseSteamCsv(line);

// Create generic game entry
var game = new MasterGameEntry
{
    Id = "cyberpunk-2077",
    Name = "Cyberpunk 2077",
    Platforms = JsonSerializer.Serialize(new[] { "Steam" }),
    PlatformIds = JsonSerializer.Serialize(new { Steam = 1091500 }),
    Developers = JsonSerializer.Serialize(new[] { "CD Projekt Red" }),
    // ... other fields
    DataSource = "Steam"
};
```

### Importing from GOG (Future)

```csharp
// Read GOG data
var gogEntry = ParseGogData(line);

// Check if game already exists
var existing = context.Games.Find("cyberpunk-2077");

if (existing != null)
{
    // Game exists, ADD GOG as another platform
    var platforms = JsonSerializer.Deserialize<List<string>>(existing.Platforms);
    if (!platforms.Contains("GOG"))
    {
        platforms.Add("GOG");
        existing.Platforms = JsonSerializer.Serialize(platforms);
    }

    // Add GOG platform ID
    var platformIds = JsonSerializer.Deserialize<Dictionary<string, object>>(existing.PlatformIds);
    platformIds["GOG"] = "1423049311";
    existing.PlatformIds = JsonSerializer.Serialize(platformIds);

    // Update version tracking
    existing.Version++;
    existing.UpdatedAt = DateTime.UtcNow;
}
else
{
    // Game doesn't exist, create new entry
    var game = new MasterGameEntry
    {
        Id = "cyberpunk-2077",
        Name = "Cyberpunk 2077",
        Platforms = JsonSerializer.Serialize(new[] { "GOG" }),
        PlatformIds = JsonSerializer.Serialize(new { GOG = "1423049311" }),
        DataSource = "GOG"
    };
    context.Games.Add(game);
}
```

---

## 🔍 Querying

### Get All Games
```csharp
var allGames = context.Games
    .Where(g => !g.IsDeleted)
    .ToList();
// Returns: ALL games regardless of platform
```

### Get Games Available on Steam
```csharp
var steamGames = context.Games
    .Where(g => !g.IsDeleted && g.Platforms.Contains("Steam"))
    .ToList();
```

### Get Cross-Platform Games
```csharp
var crossPlatform = context.Games
    .Where(g => !g.IsDeleted)
    .ToList()
    .Where(g => {
        var platforms = JsonSerializer.Deserialize<List<string>>(g.Platforms);
        return platforms.Count > 1;
    })
    .ToList();
```

### Get Free Games
```csharp
var freeGames = context.Games
    .Where(g => !g.IsDeleted && g.IsFreeToPlay)
    .ToList();
```

---

## 🎮 User Perspective

### Before (Platform-Specific)
```
Your Software:
├── Steam Games (47 items)
│   ├── Cyberpunk 2077
│   ├── The Witcher 3
│   └── ...
├── GOG Games (23 items)
│   ├── Cyberpunk 2077    ← Duplicate!
│   ├── The Witcher 3     ← Duplicate!
│   └── ...
└── Epic Games (15 items)
    ├── Cyberpunk 2077    ← Duplicate!
    └── ...
```

**Confusing!** Same game appears 3 times.

### After (Platform-Agnostic)
```
Your Games:
├── Cyberpunk 2077 (Steam, GOG, Epic)
├── The Witcher 3 (Steam, GOG)
├── Fortnite (Epic, Xbox, PlayStation)
└── ...
```

**Clear!** Each game once, platforms listed.

---

## 🔮 Future Enhancements

### 1. Merge Duplicate Entries

If Steam and GOG data are imported separately:
```csharp
// Find duplicates by name similarity
var duplicates = context.Games
    .GroupBy(g => g.Name.ToLower())
    .Where(group => group.Count() > 1)
    .ToList();

foreach (var group in duplicates)
{
    var primary = group.First();
    foreach (var duplicate in group.Skip(1))
    {
        // Merge platforms
        var platforms = JsonSerializer.Deserialize<List<string>>(primary.Platforms);
        var dupPlatforms = JsonSerializer.Deserialize<List<string>>(duplicate.Platforms);
        platforms.AddRange(dupPlatforms.Except(platforms));
        primary.Platforms = JsonSerializer.Serialize(platforms);

        // Merge platform IDs
        var platformIds = JsonSerializer.Deserialize<Dictionary<string, object>>(primary.PlatformIds);
        var dupPlatformIds = JsonSerializer.Deserialize<Dictionary<string, object>>(duplicate.PlatformIds);
        foreach (var kvp in dupPlatformIds)
            platformIds[kvp.Key] = kvp.Value;
        primary.PlatformIds = JsonSerializer.Serialize(platformIds);

        // Mark duplicate as deleted
        duplicate.IsDeleted = true;
    }
}
context.SaveChanges();
```

### 2. Platform-Specific Pricing

Store different prices per platform:
```json
{
  "prices": {
    "Steam": 59.99,
    "GOG": 49.99,
    "Epic": 59.99
  }
}
```

### 3. Platform Availability Dates

Track when game was added to each platform:
```json
{
  "platformDates": {
    "Steam": "2020-12-10",
    "GOG": "2020-12-10",
    "Epic": "2022-02-15"
  }
}
```

### 4. Platform-Specific Reviews

Separate review counts per platform:
```json
{
  "reviews": {
    "Steam": {
      "positive": 400000,
      "negative": 50000
    },
    "GOG": {
      "positive": 15000,
      "negative": 1000
    }
  }
}
```

---

## 📊 Storage Comparison

### Platform-Specific Design
```
SteamGames:     10,000 entries
GOGGames:       8,000 entries (70% overlap)
EpicGames:      5,000 entries (60% overlap)
XboxGames:      12,000 entries (50% overlap)
-------------------------------------------------
Total:          35,000 entries
Unique Games:   ~15,000 games
Duplication:    133% overhead!
```

### Platform-Agnostic Design
```
Games:          15,000 entries (unique)
Average Platforms per Game: 2.3
-------------------------------------------------
Total:          15,000 entries
Unique Games:   15,000 games
Duplication:    0% overhead!
Storage Saved:  57%
```

---

## ✅ Benefits Summary

| Aspect | Platform-Specific | Platform-Agnostic |
|--------|-------------------|-------------------|
| **Storage** | 35,000 entries | 15,000 entries |
| **Duplication** | 133% overhead | 0% overhead |
| **User Experience** | Confusing (duplicates) | Clear (one per game) |
| **Querying** | Complex (union queries) | Simple (one table) |
| **Maintenance** | Update 5 tables | Update 1 table |
| **Merging** | Very difficult | Easy (add to array) |
| **Scalability** | Poor (N tables) | Excellent (1 table) |

---

## 🚀 Migration Path

### From Existing Code

If you have existing Steam-specific code:

**Before:**
```csharp
var steamGames = context.SteamGames
    .Where(g => g.AppId == 730)
    .FirstOrDefault();

Console.WriteLine($"Found: {steamGames.Name}");
Console.WriteLine($"Steam AppID: {steamGames.AppId}");
```

**After:**
```csharp
var game = context.Games
    .FirstOrDefault(g => g.PlatformIds.Contains("\"Steam\":730"));

if (game != null)
{
    var platformIds = JsonSerializer.Deserialize<Dictionary<string, int>>(game.PlatformIds);
    Console.WriteLine($"Found: {game.Name}");
    Console.WriteLine($"Steam AppID: {platformIds["Steam"]}");
    Console.WriteLine($"Available on: {string.Join(", ", JsonSerializer.Deserialize<List<string>>(game.Platforms))}");
}
```

---

## 🎯 Key Takeaway

**Think Game-Centric, Not Platform-Centric**

Users don't care about "Steam games" vs "GOG games" - they care about **GAMES**.

The platform is just one attribute of the game, not its identity!

```
Game = {
  identity: "Cyberpunk 2077",
  attributes: {
    platforms: ["Steam", "GOG", "Epic"],
    price: 59.99,
    genre: "RPG"
  }
}
```

This architecture is:
- ✅ More intuitive
- ✅ More efficient
- ✅ More scalable
- ✅ Future-proof

---

**Ready for multi-platform game management!** 🎮
