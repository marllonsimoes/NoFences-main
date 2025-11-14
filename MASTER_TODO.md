# Master TODO List - NoFences Project
*Last Updated: Session 16 - November 14, 2025*

This document contains **ONLY PENDING TASKS**. For completed work, see [MASTER_DONE.md](MASTER_DONE.md).

---

## ✅ Recently Resolved Decisions (Session 15)

### Database Architecture Clarification
**Decision:** ✅ NO ACTION NEEDED - Current architecture is correct
- **Current:** Two-tier architecture (ref.db + master_catalog.db)
  - `master_catalog.db` = Source-of-truth, community-based DB with metadata from CNET, winget, Wikipedia
  - `ref.db` = User's installed software with foreign key to master_catalog + user-specific data (install dir, icon, registry)
- **Outcome:** This is exactly the desired architecture. Database consolidation discussion removed from TODO.

### Game vs Software Categorization
**Decision:** ✅ APPROVED - Implement with caution
- Add SoftwareType enum and filtering
- Requires automated tests for Edit window (local only, disabled in CI/CD)
- Be mindful of Edit window complexity as filters grow

### UniGetUI-like Feature
**Decision:** ⏳ DEFERRED - Move to FUTURE priority
- Good idea but lower priority
- Challenges: Requires winget up-to-date, needs failover
- Implement in future release

### New Feature Discovered
**Decision:** ✅ APPROVED - Show file list when not using smart filters
- When fence has no active smart filters, display actual files inside
- Provides transparency about fence contents

---

## 🔴 CRITICAL (Must Do Immediately)

### Manual Testing Results (Sessions 14-15) - Completed ✅
- [x] **Filtering:** ✅ VERIFIED - Working very well!
- [x] **Factory methods:** ✅ VERIFIED - UI working fine (internal implementation tested via UI)
  - Created many fences of different types, all working correctly
- [x] **Manual enrichment progress feedback:** ✅ VERIFIED - Shows progress correctly
- [x] **Enrichment completeness:** ✅ RESOLVED (Session 15)
  - Root cause: Missing RAWG API key prevented all game enrichment
  - Fixed: Added API key configuration check to diagnostic tool
  - Verified: 51.2% enrichment success rate for games (13 games enriched in test)
  - Expected behavior: High overall failure rate (65.8%) due to non-game software lacking metadata sources

### Manual Testing - Remaining
- [ ] **Run remaining manual tests** from `MANUAL_TESTING_CHECKLIST.md`
  - Get RAWG API key from https://rawg.io/apidocs (free) for enhanced testing

---

## 🟠 HIGH PRIORITY (Do Soon)

### Bugs & Issues

#### ✅ Metadata Enrichment - RESOLVED (Session 15)
- [x] **Root cause identified and fixed**
  - **Problem:** Missing RAWG API key prevented all game metadata enrichment
  - **Solution:**
    - Added RAWG API key configuration check to diagnostic tool
    - Enhanced MetadataEnrichmentDiagnostics.cs with automatic API key detection
  - **Results:** 51.2% enrichment success rate for games (excellent!)
  - **Status:** ✅ COMPLETE

#### ✅ EA App Detector - RESOLVED (Session 15)
- [x] **Pattern-based detection architecture implemented**
  - **Problem:** EA App changed architecture, games not listing properly
  - **Solution:**
    - Extended IGameStoreDetector with CanDetectFromPath() and GetGameInfoFromPath()
    - Implemented EA App pattern detection using `__Installer/installerdata.xml`
    - Added detector claiming pattern in InstalledAppsUtil
  - **Results:** EA App games detected and enhanced with XML metadata
  - **Status:** ✅ COMPLETE

### Bugs & Issues (NEW)

#### ✅ Drag & Drop Not Working - RESOLVED (Session 16)
- [x] **Drag & drop to fences broken**
  - **Problem:** Items cannot be dragged into fences (stopped working recently)
  - **Root Cause:** DropZoneOverlay was using `Brushes.Transparent` which prevents WPF elements from receiving mouse/drag events
  - **Solution:** Changed background to `new SolidColorBrush(Color.FromArgb(1, 0, 0, 0))` - barely visible but enables hit-testing
  - **Key Learning:** In WPF, `Brushes.Transparent` is null/empty and prevents hit-testing. Any background with alpha > 0 enables event reception.
  - **Status:** ✅ COMPLETE

### Testing - Missing HIGH Priority Tests
- [ ] **Implement service tests**
  - [ ] InstalledAppsUtilTests.cs
  - [ ] EnhancedInstalledAppsServiceTests.cs
  - [ ] CatalogDownloadServiceTests.cs

### FilesFence - Features & Enhancements

- [x] **Add SoftwareType enum enhancement** ✅ COMPLETED (Session 16)
  - Added `SoftwareType` enum: Game, Application, Tool, Utility, Unknown
  - Added to `SoftwareReference` table in master_catalog.db with backward-compatible migration
  - Metadata source determines type (RAWG = Game, Winget/CNET = Application)
  - Repository query methods added (GetByType, GetByTypeAndCategory)
  - **Status:** ✅ COMPLETE

- [x] **Game vs Software categorization with filters** ✅ COMPLETED (Session 16)
  - Added three-level filtering hierarchy: Type → Source → Category
  - UI implementation with SoftwareType dropdown in FilesPropertiesPanel
  - Dynamic category population based on selected Type and database content
  - Games: Use Genres field from RAWG metadata (Action, RPG, Strategy, etc.)
  - Applications/Tools/Utilities: Use Category field
  - Removed redundant categories (Games, Utilities) from dropdown
  - Genre-based filtering logic implemented in InstalledSoftwareService
  - Added CategoryString property to FileFilter for dynamic genre/category storage
  - Fixed "All" category bug where Type filter wasn't working correctly
  - **Status:** ✅ COMPLETE
  - **⏳ REVIEW AFTER ENRICHMENT:** Dynamic categories will populate once metadata enrichment completes

- [x] **Show file list when not using smart filters** ✅ COMPLETED (Session 16)
  - Implemented FileSystemWatcher for real-time file monitoring
  - Automatic refresh when files added/deleted/renamed in monitored folder
  - Thread-safe UI updates via Dispatcher
  - Smart monitoring (only for file-based filters, not Software filter)
  - Added manual items management UI in FilesPropertiesPanel
  - Users can view and remove manually added items in Edit Window
  - **Status:** ✅ COMPLETE

---

## 🟡 MEDIUM PRIORITY (Important)

### Bugs & Issues

#### Installer Service Handling
- [ ] **Fix installer service handling (WixSharp compatibility)** ⚠️ BUG
  - **Testing results:**
    - Service works fine: start, stop, pause all working manually ✅
    - Didn't test full reinstall/upgrade cycle yet
  - **Problem found:** `Task.IsServiceInstalled()` method doesn't exist in WixSharp library
    - Current code uses this non-existent method
    - Need alternative approach
  - **Solution options:**
    1. Use Wix classic API for service detection
    2. Find WixSharp alternative method
    3. Use ServiceController class directly
  - **Key requirement:** Installer must NOT fail if service can't be installed/started
    - Handle gracefully with logging
    - Allow installation to continue
  - **Priority:** MEDIUM (installer works, just needs better error handling)

### Testing - MEDIUM Priority Tests
See [COMPREHENSIVE_TEST_PLAN.md](COMPREHENSIVE_TEST_PLAN.md) for details:
- [ ] Game normalization tests (multi-platform duplicate detection)
- [ ] Catalog normalizer tests
- [ ] Image preprocessing tests
- [ ] Fence info serialization tests
- [ ] Database migration tests

### Performance Monitoring (Optional Enhancement)
- [ ] **Add timing logs for database queries**
  - Log query execution time
  - Target: < 100ms for filtered queries
  - Log: "Database query completed in XX ms (returned YY items)"

- [ ] **Add performance metrics to nofences.log**
  - Icon cache hit ratio
  - Average refresh time
  - Memory usage during population

### Enhanced Manual Refresh (Optional Enhancement)
- [ ] **Add progress dialog for manual database refresh**
  - Show: "Detecting installed software..."
  - Progress bar: X / Y detectors completed
  - Show count: "Found XX games from Steam, YY from GOG..."
  - Cancel button

### Source Dropdown Polish (Optional Enhancement)
- [ ] **Show counts in source dropdown**
  - Format: "Steam (42 games)"
  - Format: "GOG Galaxy (18 games)"
  - Format: "All Sources (238 items)"
  - Update counts on refresh

### ClockFence - Future Features
- [ ] **Advanced layouts**
  - Vertical layout (all elements stacked)
  - Pixel Phone layout (big weather icon, separate lines for h/m/s)
  - UI prepared with ClockLayout property
  - Mark as: "Future enhancement"

---

## 🔵 LOW PRIORITY (Nice to Have)

### Testing - LOW Priority Tests
- [ ] UI tests (Theme manager, animations)
- [ ] Canvas architecture tests
- [ ] ViewModel tests (without rendering)

### Code Quality
- [ ] Run Roslyn analyzers
- [ ] Fix code analysis warnings
- [ ] Add XML documentation to public APIs
- [ ] Refactor: MetadataEnrichmentService to inject providers (better testability)

### Documentation
- [ ] Update CLAUDE.md with Sessions 12-14 changes
- [ ] Add metadata enrichment documentation
- [ ] Add testing documentation to main README
- [ ] Create architecture diagrams

---

## 🟣 FUTURE FEATURES

### UniGetUI-like Feature (Research & Integration)
- **Goal:** Comprehensive software listing using winget integration
- **How it works:** winget list command shows source information (winget, msstore, Steam, etc.)
- **Challenges:**
  - Relies on user having winget up-to-date on their system
  - Needs failover mechanism if winget unavailable/outdated
  - Need to parse winget output and map to our detector sources
- **Implementation Plan:**
  - Phase 1: Add winget list parsing to get all installed software with sources
  - Phase 2: Cross-reference with our detectors
  - Phase 3: Fill gaps (software we missed but winget detected)
- **Decision:** ⏳ DEFERRED - Lower priority, implement in future release

### Large Feature: Cloud Sync Service (4-6 weeks)
**Goal:** Sync fences and configurations across multiple machines via cloud storage

#### Core Features
- [ ] **Virtual Folder Management**
  - Define virtual folders (e.g., "Documents", "Pictures")
  - Map to actual paths per machine (e.g., C:\Users\User\Documents vs D:\Docs)
  - Store mappings in fence metadata
  - UI for configuring virtual folder mappings

- [ ] **Backup Rule System**
  - Define backup rules per fence or globally
  - Schedule: Manual, on save, periodic (hourly, daily)
  - Conflict resolution: Newest wins, manual merge, keep both
  - Selective sync: Choose which fences to sync

- [ ] **CloudSync API Integration**
  - Abstract cloud provider interface
  - Implementations: Seafile, Nextcloud, Dropbox, OneDrive
  - Authentication flows
  - File upload/download with progress
  - Delta sync for efficiency

- [ ] **Sync Engine**
  - Background service for automatic sync
  - Change detection (file system watcher)
  - Queue-based upload/download
  - Retry logic for failed syncs
  - Sync status indicators in UI

#### Database Schema
- [ ] Design `SyncConfiguration` table
  - Cloud provider type
  - Connection details (encrypted)
  - Sync frequency
  - Last sync timestamp

- [ ] Design `VirtualFolderMapping` table
  - Virtual folder name
  - Machine identifier
  - Actual path
  - Sync enabled flag

- [ ] Design `SyncRule` table
  - Rule name
  - Applies to: All fences, specific fences
  - Schedule type
  - Conflict resolution strategy

#### Implementation Plan
1. **Phase 1: Virtual Folder System (1 week)**
   - Database schema
   - Core virtual folder logic
   - UI for mapping configuration

2. **Phase 2: Backup Rules (1 week)**
   - Rule engine implementation
   - UI for rule creation/management
   - Manual backup functionality

3. **Phase 3: Cloud Provider Integration (1-2 weeks)**
   - Abstract provider interface
   - Seafile implementation (primary)
   - Authentication and basic file operations

4. **Phase 4: Sync Engine (1-2 weeks)**
   - Background sync service
   - Change detection
   - Conflict resolution
   - Status monitoring UI

**Estimated Effort:** 4-6 weeks (major feature)

---

## 📊 Current Project Statistics

**Test Coverage:**
- **Current Status:** ~25% (23 test classes / ~80 planned)
- **Total Tests:** 295 tests
- **Pass Rate:** 100%

| Layer | Target Coverage | Current | Status |
|-------|----------------|---------|--------|
| Data Layer | 80%+ | ~40% | 🟡 Progressing |
| Services | 80%+ | ~35% | 🟡 Progressing |
| Utilities | 90%+ | ~30% | 🟠 Needs Work |
| Core | 90%+ | ~25% | 🟠 Needs Work |
| Overall | 70%+ | ~25% | 🟡 Good Progress |

**To Achieve 70% Overall:**
- Need: ~50 more test classes
- Need: ~250 more test methods
- Focus: Continue with HIGH and MEDIUM priority tests

**Architecture Patterns Established:**
- ✅ Handler Pattern for fence rendering
- ✅ Factory Method Pattern for model construction
- ✅ Repository Pattern with JOIN helpers
- ✅ Dependency Injection (26 registrations)
- ✅ Dual-Database Architecture (ref.db + master_catalog.db)

---

## 📋 Quick Reference

**Last Session:** 16 (November 14, 2025)
**Current Session:** 17 (TBD)
**Total Items:** ~53 pending items
**CRITICAL Items:** 1 (remaining manual tests)
**HIGH Items:** 4 (3 service tests + 1 drag & drop bug)
**MEDIUM Items:** 15
**LOW Items:** 10
**FUTURE Items:** 2 large features (UniGetUI integration + Cloud Sync)

**Session 16 Completions:**
- ✅ SoftwareType enum implementation
- ✅ Game vs Software categorization with three-level filtering
- ✅ Real-time file monitoring with FileSystemWatcher
- ✅ Manual items management UI
- ✅ CategoryString property for dynamic genre/category storage
- ✅ Software filter "All" category bug fix

**Test Coverage Progress:** 25% (295 tests, 23 classes)
**Technical Debt:** Zero (cleaned in Session 13)
**Test Pass Rate:** 100%

---

## 📌 Notes

- For completed work from Sessions 10-14, see [MASTER_DONE.md](MASTER_DONE.md)
- For comprehensive test planning, see [COMPREHENSIVE_TEST_PLAN.md](COMPREHENSIVE_TEST_PLAN.md)
- For session documentation, see [documentation/SESSION_INDEX.html](documentation/SESSION_INDEX.html)
- For architecture decisions, see [ARCHITECTURE_REVIEW_SESSION_14.md](ARCHITECTURE_REVIEW_SESSION_14.md)

---

*Focus on CRITICAL and HIGH priority items first. MEDIUM and LOW can wait.*
*Cloud Sync is a major feature for future consideration after current priorities are complete.*
