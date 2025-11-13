# Manual Testing Checklist - Session 11

## Environment Setup Requirements

### API Keys Needed
- [ ] **RAWG API Key** - Get free key at https://rawg.io/apidocs
  - Configure in: NoFences → Preferences → API Keys
  - Used for: Game metadata enrichment

### Test Data Requirements
- [ ] **Install test software** from various sources:
  - Steam games (2-3 games)
  - GOG games (1-2 games)
  - Epic Games (1-2 games)
  - Regular Windows software (Chrome, Firefox, etc.)
  - Amazon Games (if available)
  - EA App games (if available)

### Database State
- [ ] **Fresh database** (delete existing `ref.db` and `master_catalog.db` to test initialization)
- [ ] **Existing database** (keep current to test migrations)

---

## Priority 1: Critical Functionality

### ✅ Bug Fix #14: BadImageFormatException (SQLite)
**Status:** FIXED - Needs verification
**Test:** Application startup

**Steps:**
1. Delete bin/Debug folder to force clean rebuild
2. Build solution (should compile without errors)
3. Run NoFences.exe
4. **Expected:** Application starts without BadImageFormatException
5. **Expected:** No errors in nofences.log related to SQLite loading

**Success Criteria:**
- ✅ Application starts successfully
- ✅ No "BadImageFormatException" in logs
- ✅ No "incorrect format" errors

---

### 🔄 Bug Fix #11: Automatic Database Population
**Status:** VERIFIED in logs - Needs manual confirmation
**Test:** Database initialization on first run

**Steps:**
1. Close NoFences if running
2. Delete `%AppData%\TinySoft\NoFences\ref.db`
3. Start NoFences
4. Wait 30 seconds for background population
5. Check nofences.log for: "Successfully populated database with X software entries"

**Success Criteria:**
- ✅ Database created automatically
- ✅ InstalledSoftware table populated (check log count)
- ✅ No "no such table: InstalledSoftware" errors
- ✅ Application remains responsive during population

**Verify in Log:**
```
"Checking if installed software database needs initialization..."
"Background thread: Starting installed software detection and database population"
"Successfully populated database with XXX software entries"
```

---

### 🆕 Feature: Source Filtering (FilesFence)
**Status:** IMPLEMENTED - Needs testing
**Test:** Source dropdown in FilesFence properties

**Steps:**
1. Create a new FilesFence (Files type)
2. Open fence properties (right-click → Edit)
3. **Expected:** See "Source" dropdown in filter section
4. **Expected:** Dropdown contains:
   - "All Sources" (default)
   - "Steam"
   - "GOG Galaxy"
   - "Epic Games Store"
   - "Amazon Games"
   - "EA App"
   - "Ubisoft Connect"
   - Other sources if detected
5. Select "Steam" from dropdown
6. Click "Refresh"
7. **Expected:** Only Steam games displayed
8. Select "GOG Galaxy"
9. Click "Refresh"
10. **Expected:** Only GOG games displayed

**Success Criteria:**
- ✅ Source dropdown visible and populated
- ✅ Selecting source filters correctly
- ✅ "All Sources" shows everything
- ✅ Performance is fast (database-backed, <100ms)

**Performance Test:**
- Check nofences.log for query times
- Should see: "Database query completed in XX ms"
- Expected: < 100ms for database query

---

## Priority 2: New Features

### 🆕 Feature: Metadata Enrichment API Clients
**Status:** IMPLEMENTED - Needs integration testing
**Test:** Metadata providers (currently not integrated into UI)

**Prerequisites:**
1. Configure RAWG API key in Preferences → API Keys
2. Ensure internet connection

**Test 1: RAWG API Client (Games)**
**Manual code test** (requires temporary test method):
```csharp
// Add to Program.cs or create test button
var rawgClient = new RawgApiClient();
Console.WriteLine($"RAWG Available: {rawgClient.IsAvailable()}");

// Test game search
var result = await rawgClient.SearchByNameAsync("Cyberpunk 2077");
Console.WriteLine($"Found: {result?.Name}, Confidence: {result?.Confidence}");
```

**Expected Results:**
- IsAvailable() = true (if API key configured)
- SearchByNameAsync returns metadata with confidence > 0.7
- Data includes: Name, Publisher, Developers, Description, Genres

**Test 2: Winget Client (Software)**
**Manual test:**
```csharp
var wingetClient = new WingetApiClient();
Console.WriteLine($"Winget Available: {wingetClient.IsAvailable()}");

var result = await wingetClient.SearchByNameAsync("Google Chrome");
Console.WriteLine($"Found: {result?.Name}, Publisher: {result?.Publisher}");
```

**Expected Results:**
- IsAvailable() = true (if winget CLI installed)
- SearchByNameAsync returns metadata
- Publisher should be "Google LLC"

**Test 3: Wikipedia Client (Fallback)**
```csharp
var wikiClient = new WikipediaApiClient();
var result = await wikiClient.SearchByNameAsync("Visual Studio Code");
Console.WriteLine($"Found: {result?.Name}, Confidence: {result?.Confidence}");
```

**Expected Results:**
- Always available (public API)
- Returns description and URL
- Confidence typically 0.7-0.9

**Test 4: CNET Scraper**
```csharp
var cnetClient = new CnetScraperClient();
var result = await cnetClient.SearchByNameAsync("Adobe Photoshop");
// Wait at least 2 seconds (rate limiting)
```

**Expected Results:**
- Rate limiting enforced (2 second minimum between requests)
- Returns metadata if found
- Handles errors gracefully

---

### 🆕 Feature: Metadata Enrichment Service (Orchestrator)
**Status:** IMPLEMENTED - Needs integration testing
**Test:** Priority-based provider selection

**Manual test:**
```csharp
var service = new MetadataEnrichmentService();
var stats = service.GetProviderStatistics();
Console.WriteLine($"Game providers: {stats.GameProvidersAvailable}/{stats.GameProvidersTotal}");
Console.WriteLine($"Software providers: {stats.SoftwareProvidersAvailable}/{stats.SoftwareProvidersTotal}");

// Test game enrichment
var gameSoftware = new InstalledSoftware
{
    Name = "Elden Ring",
    Source = "Steam"
};
bool enriched = await service.EnrichSoftwareAsync(gameSoftware);
Console.WriteLine($"Enriched: {enriched}, Publisher: {gameSoftware.Publisher}");

// Test software enrichment
var appSoftware = new InstalledSoftware
{
    Name = "Visual Studio Code",
    Source = "Manual Install"
};
enriched = await service.EnrichSoftwareAsync(appSoftware);
Console.WriteLine($"Enriched: {enriched}, Publisher: {appSoftware.Publisher}");
```

**Expected Results:**
- Provider statistics show available providers
- Game detection works (Source = "Steam" routes to game providers)
- Priority order respected (tries highest priority first)
- Confidence filtering works (only accepts > 0.5)

---

## Priority 3: Existing Features (Regression Testing)

### 🔄 FilesFence: Hybrid Architecture (Database + In-Memory)
**Status:** IMPLEMENTED (Session 11) - Verify still working
**Test:** FilesFence with database-backed filtering

**Steps:**
1. Create FilesFence
2. Set filter to show only ".exe" files
3. Click "Refresh"
4. **Expected:** Fast refresh (<100ms from database)
5. **Expected:** Icons cached (subsequent refreshes instant)

**Check Logs:**
```
"Database query completed in XX ms"
"Retrieved XX items from database"
"Icon cache hit ratio: XX%"
```

---

### 🔄 Amazon Games Detection
**Status:** FIXED (Session 11) - Verify improvements
**Test:** Amazon Games library detection

**Prerequisites:**
- Amazon Games launcher installed
- At least 1 game installed via Amazon Games

**Steps:**
1. Close NoFences
2. Start NoFences
3. Check nofences.log for: "Amazon Games: Found X installed games"
4. Create FilesFence with Source = "Amazon Games"
5. **Expected:** Amazon games displayed with correct names and icons

**Success Criteria:**
- ✅ Games detected (not "Unknown Game")
- ✅ Proper names from ProductDetails.sqlite
- ✅ Icons extracted correctly

---

### 🔄 ClockFence Customization
**Status:** IMPLEMENTED (Session 11) - Verify functionality
**Test:** ClockFence format customization

**Steps:**
1. Create ClockFence (or edit existing)
2. Open properties
3. **Expected:** See format customization options:
   - Time format (12h/24h)
   - Date format options
   - Font customization
   - Color customization
4. Change format to "yyyy-MM-dd HH:mm:ss"
5. Apply
6. **Expected:** Clock displays in new format
7. **Expected:** Format persists after restart

---

## Priority 4: Performance & Stability

### ⚡ Performance: Database Query Speed
**Test:** Source filtering query performance

**Steps:**
1. Create FilesFence with ~100+ software entries
2. Open Properties
3. Select different sources from dropdown rapidly
4. Check nofences.log for query times

**Success Criteria:**
- ✅ Queries complete in <100ms
- ✅ UI remains responsive
- ✅ No lag when switching sources

---

### 🔒 Stability: Thread-Safe Database Initialization
**Status:** FIXED (Bug #11) - Verify no race conditions
**Test:** Rapid application restarts

**Steps:**
1. Close NoFences
2. Delete `ref.db` (force initialization)
3. Start NoFences
4. Immediately close NoFences (within 5 seconds)
5. Start NoFences again
6. Repeat 3-4 times rapidly

**Success Criteria:**
- ✅ No "no such table" errors
- ✅ No database corruption
- ✅ No crashes during initialization
- ✅ Database initializes correctly even if interrupted

---

## Priority 5: Configuration & Settings

### 🆕 Preferences Window: API Keys Section
**Status:** IMPLEMENTED - Needs UI testing
**Test:** API key configuration UI

**Steps:**
1. Open NoFences → System Tray → Preferences
2. **Expected:** See "API Keys" section between "Auto-Update" and "General"
3. **Expected:** See "RAWG API Key" text box
4. **Expected:** Watermark text: "Enter your RAWG API key (optional)"
5. **Expected:** Help text: "Get your free API key at: https://rawg.io/apidocs"
6. **Expected:** Info banner explaining API keys are optional
7. Enter test API key: "test-key-12345"
8. Click "Save"
9. **Expected:** "Preferences saved successfully" message
10. Reopen Preferences
11. **Expected:** API key field shows "test-key-12345"

**Success Criteria:**
- ✅ UI section visible and properly formatted
- ✅ Text box accepts input
- ✅ Save persists to UserPreferences.xml
- ✅ Load retrieves from UserPreferences.xml
- ✅ Empty/whitespace treated as null

**Verify Storage:**
- Check `%AppData%\NoFences\UserPreferences.xml`
- Should contain: `<RawgApiKey>test-key-12345</RawgApiKey>`

---

## Priority 6: Error Handling & Edge Cases

### 🛡️ Error Handling: Missing API Keys
**Test:** Metadata providers without API keys

**Steps:**
1. Remove RAWG API key from Preferences (leave blank)
2. Try metadata enrichment test (from Priority 2)
3. **Expected:** RAWG provider reports IsAvailable() = false
4. **Expected:** No crashes or exceptions
5. **Expected:** Graceful fallback to other providers

---

### 🛡️ Error Handling: Network Failures
**Test:** Metadata providers with no internet

**Steps:**
1. Disconnect internet
2. Try metadata enrichment tests
3. **Expected:** Graceful timeout
4. **Expected:** Log warnings (not errors)
5. **Expected:** Application remains functional

---

### 🛡️ Error Handling: Rate Limiting (CNET)
**Test:** CNET scraper rate limit enforcement

**Steps:**
1. Call CNET scraper 3 times in rapid succession
2. Check timing between requests
3. **Expected:** Minimum 2 seconds between requests
4. **Expected:** Log entries: "Rate limit: waiting X ms"

---

## Logs to Monitor

During all tests, monitor `%ProgramData%\TinySoft\NoFences\nofences.log` for:

### ✅ Success Indicators:
```
"Successfully populated database with XXX software entries"
"Database query completed in XX ms"
"Retrieved XX items from database"
"Icon cache hit ratio: XX%"
"Metadata enrichment: IsAvailable = true"
```

### ⚠️ Warning Signs (OK if occasional):
```
"no such table: DbSet" (in ProductDetails.sqlite - non-critical)
"Rate limit: waiting X ms" (expected for CNET)
"Provider not available: [name]" (OK if API key not configured)
```

### ❌ Error Signs (Investigate):
```
"no such table: InstalledSoftware" (should NOT appear anymore - Bug #11 fixed)
"BadImageFormatException" (should NOT appear - Bug #14 fixed)
"Database corruption detected"
"Unhandled exception"
```

---

## Test Results Template

Copy and fill out after testing:

```
## Session 11 Manual Testing Results

Date: ___________
Tester: ___________
Build: Debug/Release
Configuration: ___________

### Priority 1: Critical
- [ ] BadImageFormatException fix: PASS / FAIL - Notes: ___________
- [ ] Automatic database population: PASS / FAIL - Notes: ___________
- [ ] Source filtering: PASS / FAIL - Notes: ___________

### Priority 2: New Features
- [ ] RAWG API client: PASS / FAIL / SKIP - Notes: ___________
- [ ] Winget client: PASS / FAIL / SKIP - Notes: ___________
- [ ] Wikipedia client: PASS / FAIL / SKIP - Notes: ___________
- [ ] CNET scraper: PASS / FAIL / SKIP - Notes: ___________
- [ ] Metadata enrichment service: PASS / FAIL / SKIP - Notes: ___________

### Priority 3: Regression
- [ ] FilesFence hybrid architecture: PASS / FAIL - Notes: ___________
- [ ] Amazon Games detection: PASS / FAIL / SKIP - Notes: ___________
- [ ] ClockFence customization: PASS / FAIL - Notes: ___________

### Priority 4: Performance
- [ ] Database query speed: PASS / FAIL - Notes: ___________
- [ ] Thread-safe initialization: PASS / FAIL - Notes: ___________

### Priority 5: Configuration
- [ ] Preferences window API keys: PASS / FAIL - Notes: ___________

### Priority 6: Error Handling
- [ ] Missing API keys: PASS / FAIL - Notes: ___________
- [ ] Network failures: PASS / FAIL / SKIP - Notes: ___________
- [ ] Rate limiting: PASS / FAIL / SKIP - Notes: ___________

### Issues Found:
1. ___________
2. ___________

### Overall Status: ✅ PASS / ⚠️ PASS WITH ISSUES / ❌ FAIL
```

---

## Next Steps After Testing

1. **If all tests pass:** Ready for Session 11 completion, move to installer bugs
2. **If metadata enrichment tests can't run:** Create integration UI (add button to trigger enrichment)
3. **If issues found:** Document in GitHub issues, prioritize fixes
4. **Unit tests:** Proceed with automated test implementation (see separate test plan)
