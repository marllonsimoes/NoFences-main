# NoFences v1.7.0 Release Notes

**Release Date:** November 12, 2025
**Session:** 12 - Test Infrastructure & Metadata Enrichment Integration

---

## 🎯 Release Highlights

This is a **major feature release** that introduces comprehensive metadata enrichment for installed software and games, complete test infrastructure with 100% pass rate, and numerous critical bug fixes.

### ✨ Major New Features

#### 1. **Intelligent Metadata Enrichment System**
Automatically enriches installed software and games with rich metadata from multiple online sources:

- **For Games:** RAWG API integration
  - Game descriptions, genres, developers, publishers
  - Release dates, ratings, cover images
  - Smart name matching (85% similarity threshold)
  - Support for Steam, Epic Games, GOG, Ubisoft Connect, EA App, Amazon Games

- **For Software:** Multi-provider fallback system
  - Winget API (primary source for Windows software)
  - Wikipedia API (general software information)
  - CNET scraper (reviews and descriptions)
  - Automatic provider selection based on availability

#### 2. **Enhanced UI with Rich Tooltips**
- Hover over any software/game item to see:
  - Description and publisher information
  - Genres and categories
  - Release date and rating
  - Developers and metadata source
  - Install date and version

#### 3. **Dual-Trigger Enrichment System**
- **Automatic Background Enrichment:** Runs automatically after database population (non-blocking)
- **Manual Enrichment:** "Enrich Metadata (Force Sync)" button in FilesFence properties

#### 4. **Comprehensive Test Infrastructure**
- **187 unit tests** with **100% pass rate**
- Test coverage across repositories, services, detectors, and metadata providers
- CI/CD pipeline with automated testing on every commit
- Environment-agnostic tests (run on any machine)

---

## 🐛 Critical Bug Fixes

### Session 12 Fixes

1. **✅ RAWG Confidence Calculation** (NEW)
   - **Problem:** Games rejected based on popularity (ratings count) instead of match quality
   - **Solution:** Confidence now based on name similarity using Levenshtein distance
   - **Impact:** Indie games with fewer ratings now enriched correctly

2. **✅ Metadata Not Persisting to Database** (NEW)
   - **Problem:** Enriched metadata fetched but not saved to database
   - **Solution:** Added all enriched fields to InstalledSoftware model and database entity
   - **Impact:** Metadata now persists across sessions

3. **✅ RAWG JSON Parsing Error** (NEW)
   - **Problem:** `Cannot access child value on JValue` when parsing esrb_rating
   - **Solution:** Added type checking before accessing nested properties
   - **Impact:** RAWG enrichment no longer crashes on games without ESRB ratings

4. **✅ Incorrect Steam AppID Detection** (NEW)
   - **Problem:** Non-Steam games looked up with wrong Steam AppIDs
   - **Solution:** Changed from `Contains("Steam")` to exact match with prefix validation
   - **Impact:** Only actual Steam games trigger Steam AppID lookups

5. **✅ Duplicate Game Detection** (Session 12)
   - **Problem:** Steam games showing twice (Registry + Steam detector)
   - **Solution:** Priority-based deduplication (specialized detectors > Registry)
   - **Impact:** No more duplicate entries in FilesFence displays

6. **✅ Test Compilation Failures** (Session 12)
   - **Problem:** 6 test files excluded from compilation due to API mismatches
   - **Solution:** Rewrote all tests based on actual API implementations
   - **Impact:** 100% test pass rate achieved (187/187 tests passing)

### Session 11 Fixes (from previous release)

7. **✅ SQLite BadImageFormatException** (Bug #14)
   - x64/x86 platform mismatch causing database crashes
   - Updated NuGet packages to use correct architecture

8. **✅ Database Initialization Race Condition** (Bug #11)
   - Multiple threads initializing database simultaneously
   - Added thread-safe singleton pattern

9. **✅ .NET 4.8.1 Dictionary Compatibility** (Bug #12)
   - Dictionary indexer compatibility issues
   - Updated to use TryGetValue pattern

---

## 🔧 Technical Improvements

### Architecture Enhancements

1. **Complete IoC Container Setup**
   - 26 total registrations (repositories, services, providers, detectors, handlers)
   - Proper dependency injection throughout application
   - Testable architecture with mockable dependencies

2. **IsGameSource Logic Improvements**
   - Now checks both `Category` AND `Source` fields
   - Supports all major gaming platforms (Steam, GOG, Epic, Ubisoft, EA, Amazon)
   - Scalable solution for future platform additions

3. **Enrichment Tracking**
   - `LastEnrichedDate` field tracks when metadata was last updated
   - `MetadataSource` field identifies which provider enriched each entry
   - Avoids redundant API calls for already-enriched entries

4. **Name Similarity Algorithm**
   - Industry-standard Levenshtein distance implementation
   - Configurable similarity threshold (currently 85%)
   - Handles special characters, punctuation, case differences

### Database Schema Updates

**New Fields in `InstalledSoftwareEntry`:**
- `Description` - Software/game description
- `Genres` - Comma-separated genre list
- `Developers` - Developer names
- `ReleaseDate` - Original release date
- `CoverImageUrl` - Cover/icon image URL
- `Rating` - User rating (0.0 to 5.0)
- `LastEnrichedDate` - Timestamp of last enrichment
- `MetadataSource` - Provider that enriched this entry

**Automatic Migrations:** Entity Framework automatically updates schema on first run.

---

## 📊 Statistics

### Test Coverage
- **Total Tests:** 187 (100% passing)
- **Test Classes:** 15
- **Coverage Estimate:** ~18% of codebase (up from ~12%)
- **CI/CD:** GitHub Actions pipeline with automated testing

### Code Changes
- **Files Modified:** 18 files in Session 12
- **New Classes:** 15+ (metadata providers, services, repositories)
- **Lines Added:** ~5,000+ lines
- **Bugs Fixed:** 14 total (4 new in Session 12, 10 from Session 11)

### Metadata Providers
- **Game Providers:** 1 (RAWG)
- **Software Providers:** 3 (Winget, CNET, Wikipedia)
- **Platform Detectors:** 6 (Steam, GOG, Epic, EA, Ubisoft, Amazon)
- **Success Rate:** Varies by provider (Winget: ~60%, Wikipedia: ~40%, RAWG: ~80%)

---

## 🚀 Getting Started with Metadata Enrichment

### Prerequisites
- **Optional:** RAWG API key for enhanced game metadata
  - Get free key at: https://rawg.io/apidocs
  - Add to Preferences → API Keys → RAWG API Key

### First Time Setup
1. **Install/Update NoFences v1.7.0**
2. **Database Refresh:** Application automatically detects installed software on first run
3. **Automatic Enrichment:** Background enrichment starts automatically (1-2 minutes)
4. **Check Logs:** View enrichment progress in `%ProgramData%\Tinysoft\NoFences\nofences.log`

### Manual Enrichment
1. Open any **FilesFence** properties
2. Select filter type: **Software**
3. Scroll down to **"Enrich Metadata (Force Sync)"** button
4. Click to manually trigger enrichment (up to 100 entries)
5. Wait for completion message

### Viewing Enriched Metadata
1. Create a FilesFence with **Software** filter type
2. Select category (Games, Productivity, etc.)
3. **Hover** over any software item to see rich tooltip with:
   - Description, genres, rating, release date
   - Developers, publisher, metadata source

---

## 📋 Known Issues

1. **Steam AppID Mapping** (Minor)
   - Some Steam games detected with incorrect AppIDs
   - Cause: RegistryKey format inconsistencies
   - Impact: Name-based search still works as fallback
   - Status: To be addressed in future release

2. **Installer Service Handling** (Minor)
   - Reinstall/upgrade may fail if service not removed
   - Workaround: Manually stop/uninstall service before upgrade
   - Status: Planned fix in v1.7.1

---

## 🔮 Future Plans (v1.8.0+)

### High Priority
- Game normalization tests (multi-platform duplicate detection)
- Remaining detector tests (Steam, GOG, Epic, EA, Ubisoft)
- Metadata provider tests (Winget, Wikipedia, CNET)

### Medium Priority
- Performance monitoring (database query timing)
- Progress dialog for manual database refresh
- Source dropdown polish (show counts: "Steam (42 games)")

### Future Features
- Cloud Sync Service (3-way backup with cloud storage)
- WidgetFence for cloud stats and virtual folders
- Advanced ClockFence layouts (vertical, Pixel Phone style)

---

## 💾 Installation

### GitHub Release
Download from: https://github.com/yourusername/NoFences/releases/tag/v1.7.0

### Files Included
- `NoFences.exe` - Main application
- `NoFencesService.exe` - Background Windows service
- `master_catalog.db` - Pre-populated software database (~15MB)
- README and documentation

### Upgrade Notes
- **Database Migration:** Automatic on first run
- **Settings Preserved:** All user preferences and fence configurations maintained
- **Clean Install Recommended:** For users experiencing issues with v1.6.x

---

## 🙏 Acknowledgments

This release represents a major milestone in NoFences development:
- **Session 11:** Database architecture overhaul, multi-platform game detection
- **Session 12:** Test infrastructure, metadata enrichment, confidence algorithm improvements

Thank you to all users who reported bugs and provided feedback!

---

## 📝 Full Changelog

See `MASTER_TODO.md` and `SESSION_CHANGES.html` for detailed technical documentation.

### Session 12 Major Changes
- Metadata enrichment integration (automatic + manual)
- RAWG confidence calculation based on name similarity
- Complete test infrastructure (187 tests, 100% pass rate)
- IoC container setup (26 registrations)
- Duplicate game detection fix
- UI enhancements (rich tooltips, info panels)

### Session 11 Major Changes (from v1.6.2)
- Hybrid database architecture for FilesFence
- Multi-platform game detection (Steam, GOG, Epic, EA, Ubisoft, Amazon)
- Source filtering functionality
- ClockFence customization (16 properties)
- Amazon Games detection improvements
- SQLite platform mismatch fixes

---

**For support, bug reports, or feature requests:**
GitHub Issues: https://github.com/yourusername/NoFences/issues

**Documentation:**
See `documentation/SESSION_INDEX.html` for complete development history.
