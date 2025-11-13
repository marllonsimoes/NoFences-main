# Comprehensive Test Plan - All Sessions

This document covers ALL automated and manual tests needed for the entire NoFences application, spanning all development sessions.

---

## Test Project Structure

```
NoFences.Tests/
├── Repositories/
│   ├── InstalledSoftwareRepositoryTests.cs ✅ CREATED
│   ├── AmazonGamesRepositoryTests.cs (TODO)
│   ├── XmlFenceRepositoryTests.cs (TODO)
│   └── FenceRepositoryIntegrationTests.cs (TODO)
├── Services/
│   ├── InstalledAppsUtilTests.cs (TODO)
│   ├── EnhancedInstalledAppsServiceTests.cs (TODO)
│   ├── SoftwareCatalogServiceTests.cs (TODO)
│   ├── CatalogDownloadServiceTests.cs (TODO)
│   └── Metadata/
│       ├── RawgApiClientTests.cs ✅ CREATED
│       ├── WingetApiClientTests.cs (TODO)
│       ├── WikipediaApiClientTests.cs (TODO)
│       ├── CnetScraperClientTests.cs (TODO)
│       └── MetadataEnrichmentServiceTests.cs ✅ CREATED
├── Detectors/
│   ├── SteamStoreDetectorTests.cs (TODO)
│   ├── GOGGalaxyDetectorTests.cs (TODO)
│   ├── EpicGamesStoreDetectorTests.cs (TODO)
│   ├── AmazonGamesDetectorTests.cs (TODO - CRITICAL)
│   ├── EAAppDetectorTests.cs (TODO)
│   └── UbisoftConnectDetectorTests.cs (TODO)
├── Utilities/
│   ├── CatalogNormalizerTests.cs (TODO)
│   ├── FileFenceFilterTests.cs (TODO - HIGH PRIORITY)
│   └── AppEnvUtilTests.cs (TODO)
├── ViewModels/ (UI tests without rendering)
│   ├── FileItemViewModelTests.cs (TODO)
│   ├── ClockFenceViewModelTests.cs (TODO)
│   └── PreferencesWindowViewModelTests.cs (TODO)
├── Core/
│   ├── FenceInfoSerializationTests.cs (TODO)
│   └── UserPreferencesTests.cs (TODO)
└── Integration/
    ├── DatabaseMigrationTests.cs (TODO)
    └── EndToEndWorkflowTests.cs (TODO)
```

---

## Session-by-Session Test Coverage

### Session 0: Canvas Architecture Refactoring
**Components:** Canvas system, FilesFence rendering
**Status:** ⚠️ No tests implemented

**Tests Needed:**
- [ ] **CanvasTests.cs** (UI without rendering)
  - Test canvas initialization
  - Test fence positioning
  - Test fence stacking/z-order
  - Test multi-monitor handling

- [ ] **FilesFenceRenderingTests.cs**
  - Test file item layout calculation
  - Test icon grid rendering logic
  - Test scroll behavior calculations
  - Test resize behavior

**Priority:** Medium (foundational, but stable)

---

### Session 1: Bug Fixes (Help Icon Fade, Metadata Loss)
**Components:** UI animations, data flow
**Status:** ⚠️ No tests implemented

**Tests Needed:**
- [ ] **FadeAnimationTests.cs**
  - Test fade in/out duration
  - Test animation cleanup
  - Test animation cancellation

- [ ] **DataFlowTests.cs**
  - Test metadata preservation through FilterResult
  - Test icon cache integrity
  - Test file path to metadata mapping

**Priority:** Low (bugs fixed, need regression tests)

---

### Session 3: Image Preprocessing
**Components:** Image processing pipeline
**Status:** ⚠️ No tests implemented

**Tests Needed:**
- [ ] **ImagePreprocessingTests.cs**
  - Test image format conversion
  - Test thumbnail generation
  - Test memory management (no leaks)
  - Test concurrent image processing
  - Test invalid image handling

**Priority:** Medium (performance-critical)

---

### Session 4: Smart Filtering
**Components:** FileFenceFilter, category detection
**Status:** ⚠️ No tests implemented - **HIGH PRIORITY**

**Tests Needed:**
- [ ] **FileFenceFilterTests.cs** ⚠️ CRITICAL
  ```csharp
  [Theory]
  [InlineData("*.exe", "test.exe", true)]
  [InlineData("*.exe", "test.txt", false)]
  [InlineData("*.{exe,dll}", "test.exe", true)]
  [InlineData("*.{exe,dll}", "test.dll", true)]
  public void ApplyFilter_PatternMatching(string pattern, string filename, bool shouldMatch)
  
  [Fact]
  public void ApplyFilter_CategoryGames_ReturnsOnlyGames()
  
  [Fact]
  public void ApplyFilter_SourceSteam_ReturnsOnlySteamEntries()
  
  [Fact]
  public void ApplyFilter_DatabaseQuery_PerformanceUnder100ms()
  ```

**Priority:** ⚠️ **CRITICAL** - Core filtering logic

---

### Session 5: Game Detection System
**Components:** Steam, GOG, Epic, Amazon, EA, Ubisoft detectors
**Status:** ⚠️ No tests implemented - **HIGH PRIORITY**

**Tests Needed:**

- [ ] **SteamStoreDetectorTests.cs**
  ```csharp
  [Fact]
  public void DetectInstalledGames_WithValidSteamPath_ReturnsGames()
  
  [Fact]
  public void DetectInstalledGames_WithoutSteam_ReturnsEmpty()
  
  [Fact]
  public void ParseAppManifest_ValidVdf_ParsesCorrectly()
  
  [Fact]
  public void ExtractSteamAppId_FromManifest_ReturnsValidId()
  ```

- [ ] **GOGGalaxyDetectorTests.cs**
  ```csharp
  [Fact]
  public void DetectInstalledGames_WithValidGOGPath_ReturnsGames()
  
  [Fact]
  public void ReadGOGDatabase_ParsesGameInfo()
  ```

- [ ] **EpicGamesStoreDetectorTests.cs**
  ```csharp
  [Fact]
  public void DetectInstalledGames_ParsesManifests()
  
  [Fact]
  public void GetInstalledManifests_FindsAllGames()
  ```

- [ ] **AmazonGamesDetectorTests.cs** ⚠️ **CRITICAL**
  ```csharp
  [Fact]
  public void DetectInstalledGames_WithValidInstallPath_ReturnsGames()
  
  [Fact]
  public void GetAmazonGamesPath_FindsInstallLocation()
  
  [Fact]
  public void ParseProductDetails_ExtractsGameNames() // Bug Fix #4, #5, #6
  
  [Fact]
  public void ParseProductDetails_HandlesMultipleGames()
  
  [Fact]
  public void ParseProductDetails_HandlesCorruptDatabase() // Graceful degradation
  ```

- [ ] **EAAppDetectorTests.cs**
- [ ] **UbisoftConnectDetectorTests.cs**

**Priority:** ⚠️ **CRITICAL** - Multiple bugs fixed here, needs regression tests

---

### Session 6: Multi-Platform Games
**Components:** Game normalization, duplicate detection
**Status:** ⚠️ No tests implemented

**Tests Needed:**
- [ ] **GameNormalizationTests.cs**
  ```csharp
  [Theory]
  [InlineData("Cyberpunk 2077", "Cyberpunk 2077 - GOTY", true)] // Same game
  [InlineData("The Witcher 3", "The Witcher 3: Wild Hunt", true)]
  [InlineData("Dark Souls", "Dark Souls 2", false)] // Different games
  public void NormalizeGameName_DetectsDuplicates(string name1, string name2, bool isDuplicate)
  
  [Fact]
  public void DetectMultiPlatformGames_FindsDuplicatesAcrossStores()
  
  [Fact]
  public void PreferredPlatformSelection_PrioritizesCorrectly() // e.g., GOG > Steam
  ```

**Priority:** Medium

---

### Session 7: UI Modernization (MahApps.Metro)
**Components:** Updated UI controls, theme system
**Status:** ⚠️ No tests implemented

**Tests Needed:**
- [ ] **ThemeManagerTests.cs** (without rendering)
  - Test dark/light theme switching
  - Test theme persistence
  - Test system theme sync

- [ ] **PreferencesWindowTests.cs** (ViewModel tests)
  ```csharp
  [Fact]
  public void LoadPreferences_LoadsFromXml()
  
  [Fact]
  public void SavePreferences_PersistsToXml()
  
  [Fact]
  public void ApiKeyValidation_HandlesEmptyString()
  
  [Fact]
  public void ApiKeyValidation_TrimsWhitespace()
  ```

**Priority:** Low (UI tests)

---

### Session 8: Sprint Refactoring
**Components:** Code organization, documentation
**Status:** ⚠️ No tests impacted
**Priority:** N/A (documentation only)

---

### Session 11: THIS SESSION (Amazon Games + Metadata + Performance)

#### Section 14: Amazon Games Repository
**Status:** ⚠️ No tests - **CRITICAL**

**Tests Needed:**
- [ ] **AmazonGamesRepositoryTests.cs** ⚠️ **CRITICAL**
  ```csharp
  [Fact]
  public void Constructor_CreatesOrOpensDatabase()
  
  [Fact]
  public void GetAllGames_WithValidDatabase_ReturnsGames()
  
  [Fact]
  public void GetAllGames_WithMissingTable_ReturnsEmpty() // Bug Fix #4
  
  [Fact]
  public void GetGameByPath_FindsCorrectGame()
  
  [Fact]
  public void SchemaIntrospection_VerifiesTableExists() // Bug Fix #4
  
  [Fact]
  public void HandleCorruptDatabase_DoesNotCrash() // Graceful degradation
  ```

#### Section 15: ClockFence Customization
**Status:** ⚠️ No tests

**Tests Needed:**
- [ ] **ClockFenceTests.cs**
  ```csharp
  [Theory]
  [InlineData("HH:mm:ss", "14:30:45")]
  [InlineData("hh:mm tt", "02:30 PM")]
  [InlineData("yyyy-MM-dd HH:mm", "2025-11-12 14:30")]
  public void FormatTime_CustomFormats_DisplayCorrectly(string format, string expected)
  
  [Fact]
  public void DateFormatPersistence_SavesAndLoadsCorrectly() // Bug Fix #7
  
  [Fact]
  public void ClockUpdate_TicksEverySecond()
  ```

#### Section 16: FilesFence Data Layer Transformation
**Status:** ⚠️ No tests - **HIGH PRIORITY**

**Tests Needed:**
- [ ] **FilesFenceDataLayerTests.cs**
  ```csharp
  [Fact]
  public void GetFiles_DatabaseBacked_ReturnsFast() // < 100ms
  
  [Fact]
  public void GetFiles_WithIconCache_ReusesIcons()
  
  [Fact]
  public void Refresh_PreservesMetadata() // Bug Fix #2
  
  [Fact]
  public void HybridArchitecture_DatabaseThenMemory_WorksCorrectly()
  ```

- [ ] **FileFenceFilterTests.cs** (expanded)
  ```csharp
  [Fact]
  public void ApplyFilter_SourceSteam_QueriesDatabase() // NEW - Session 11
  
  [Fact]
  public void ApplyFilter_SourceAll_ReturnsAllEntries()
  
  [Fact]
  public void FilterPerformance_DatabaseQuery_Under100ms() // Performance test
  ```

#### Section 17: Automatic Database Population
**Status:** ⚠️ No tests - **CRITICAL**

**Tests Needed:**
- [ ] **SoftwareCatalogInitializerTests.cs** ⚠️ **CRITICAL**
  ```csharp
  [Fact]
  public void EnsureDatabaseCreated_EmptyDatabase_Initializes() // Bug Fix #11
  
  [Fact]
  public void EnsureDatabaseCreated_ThreadSafe_NoRaceCondition() // Bug Fix #11
  
  [Fact]
  public void PopulateDatabase_CollectsFromAllDetectors()
  
  [Fact]
  public void PopulateDatabase_RunsInBackground_DoesNotBlockUI()
  
  [Fact]
  public void RemoveStaleEntries_WithExistingDatabase_HandlesGracefully() // Bug Fix #11
  ```

- [ ] **InstalledSoftwareServiceTests.cs**
  ```csharp
  [Fact]
  public void RefreshDatabase_UpdatesExistingEntries()
  
  [Fact]
  public void GetAllInstalled_ReturnsComplete List()
  ```

#### Section 18: Metadata Enrichment (NEW)
**Status:** ⚠️ Partial tests created

**Tests Created:** ✅
- InstalledSoftwareRepositoryTests.cs
- MetadataEnrichmentServiceTests.cs
- RawgApiClientTests.cs

**Tests Still Needed:**
- [ ] **WingetApiClientTests.cs**
  ```csharp
  [Fact]
  public void SearchByNameAsync_WithWingetInstalled_ReturnsResults()
  
  [Fact]
  public void SearchByNameAsync_WithoutWinget_ReturnsNull()
  
  [Fact]
  public void ParseWingetOutput_ValidFormat_ParsesCorrectly()
  
  [Fact]
  public void IsAvailable_ChecksWingetCliExists()
  ```

- [ ] **WikipediaApiClientTests.cs**
  ```csharp
  [Fact]
  public void SearchByNameAsync_ValidArticle_ReturnsMetadata()
  
  [Fact]
  public void ConfidenceScore_MaxIsPointNine() // Always < 1.0 for fallback
  
  [Fact]
  public void IsAvailable_AlwaysTrue() // Public API
  ```

- [ ] **CnetScraperClientTests.cs**
  ```csharp
  [Fact]
  public void RateLimiting_EnforcesTwoSecondMinimum()
  
  [Fact]
  public void ParseSearchResults_JsonLD_ExtractsMetadata()
  
  [Fact]
  public void ParseSearchResults_FallbackToHtml_WorksWhenJsonLDMissing()
  
  [Fact]
  public async Task ConcurrentRequests_RespectRateLimit()
  ```

---

## Utilities & Core (All Sessions)

### CatalogNormalizer
**Status:** ⚠️ No tests

**Tests Needed:**
- [ ] **CatalogNormalizerTests.cs**
  ```csharp
  [Theory]
  [InlineData("Test  Software", "Test Software")] // Extra spaces
  [InlineData("TEST SOFTWARE", "Test Software")] // Case normalization
  [InlineData("Software™", "Software")] // Special characters
  public void NormalizeName_HandlesVariations(string input, string expected)
  
  [Fact]
  public void NormalizeName_PreservesVersionNumbers()
  
  [Fact]
  public void NormalizeName_RemovesTrademarks()
  ```

### AppEnvUtil
**Status:** ⚠️ No tests

**Tests Needed:**
- [ ] **AppEnvUtilTests.cs**
  ```csharp
  [Fact]
  public void GetAppEnvironmentPath_ReturnsValidDirectory()
  
  [Fact]
  public void GetAppEnvironmentPath_CreatesIfMissing()
  
  [Fact]
  public void GetAppDataPath_ReturnsCorrectLocation()
  ```

### UserPreferences
**Status:** ⚠️ No tests - **NEW (Session 11)**

**Tests Needed:**
- [ ] **UserPreferencesTests.cs**
  ```csharp
  [Fact]
  public void Load_WithMissingFile_ReturnsDefaults()
  
  [Fact]
  public void Save_PersistsToXml()
  
  [Fact]
  public void Load_WithExistingFile_LoadsCorrectly()
  
  [Fact]
  public void RawgApiKey_SavesAndLoadsCorrectly() // NEW - Session 11
  
  [Fact]
  public void RawgApiKey_NullIfEmptyString() // Session 11 - whitespace handling
  ```

---

## Integration Tests (High-Level Workflows)

### Database Migration
**Status:** ⚠️ No tests - **CRITICAL**

**Tests Needed:**
- [ ] **DatabaseMigrationTests.cs**
  ```csharp
  [Fact]
  public void Migration_FromEmptyToLatest_Succeeds()
  
  [Fact]
  public void Migration_PreservesExistingData()
  
  [Fact]
  public void Migration_HandlesSchemaChanges()
  ```

### End-to-End Workflows
**Status:** ⚠️ No tests

**Tests Needed:**
- [ ] **EndToEndWorkflowTests.cs**
  ```csharp
  [Fact]
  public void CreateFilesFence_DetectSoftware_DisplayInFence_WorksEnd ToEnd()
  
  [Fact]
  public void FilterBySource_RefreshFence_DisplaysCorrectItems()
  
  [Fact]
  public void EnrichMetadata_SaveToDatabase_LoadInFence_WorksEndToEnd()
  ```

---

## Test Execution Strategy

### For CI/CD Pipeline:
1. **Fast Unit Tests** (< 1 second each)
   - All repository tests (in-memory databases)
   - All utility tests
   - All ViewModel tests (no rendering)
   - All logic tests (filters, normalizers)

2. **Medium Integration Tests** (< 5 seconds each)
   - Database migration tests
   - Detector tests (mocked file system)
   - Service tests (mocked dependencies)

3. **Slow Integration Tests** (< 30 seconds each, optional in CI)
   - Real API calls (skipped without API keys)
   - Full end-to-end workflows
   - Performance benchmarks

### For Local Development:
- Run all tests before commit
- Use `dotnet test --filter Category!=Integration` for fast feedback
- Mark slow tests with `[Trait("Category", "Integration")]`

---

## Test Priorities

### ⚠️ **CRITICAL** (Must implement ASAP):
1. **FileFenceFilterTests.cs** - Core filtering logic
2. **AmazonGamesDetectorTests.cs** - Bug fixes #4, #5, #6
3. **AmazonGamesRepositoryTests.cs** - Bug fix #4
4. **SoftwareCatalogInitializerTests.cs** - Bug fix #11 (race condition)
5. **InstalledSoftwareRepositoryTests.cs** - ✅ DONE
6. **UserPreferencesTests.cs** - Session 11 API key handling

### 🔶 **HIGH** (Should implement soon):
1. All detector tests (Steam, GOG, Epic, EA, Ubisoft)
2. MetadataEnrichmentServiceTests.cs - ✅ DONE
3. FilesFenceDataLayerTests.cs
4. DatabaseMigrationTests.cs

### 🔷 **MEDIUM** (Important but not urgent):
1. RawgApiClientTests.cs - ✅ DONE
2. WingetApiClientTests.cs
3. WikipediaApiClientTests.cs
4. CnetScraperClientTests.cs
5. CatalogNormalizerTests.cs
6. ImagePreprocessingTests.cs

### 🔹 **LOW** (Nice to have):
1. UI tests (ThemeManagerTests, etc.)
2. CanvasTests
3. AnimationTests

---

## Implementing Tests

To implement these tests:

1. **Add test project to solution:**
   ```bash
   # Add to NoFences.sln
   dotnet sln add NoFences.Tests/NoFences.Tests.csproj
   ```

2. **Run tests:**
   ```bash
   # All tests
   dotnet test

   # Fast tests only
   dotnet test --filter Category!=Integration

   # Specific test
   dotnet test --filter FullyQualifiedName~InstalledSoftwareRepositoryTests
   ```

3. **CI/CD Integration:**
   ```yaml
   # .github/workflows/test.yml
   - name: Run Unit Tests
     run: dotnet test --filter Category!=Integration --logger "trx"
   
   - name: Run Integration Tests
     run: dotnet test --filter Category=Integration --logger "trx"
     continue-on-error: true  # Optional if tests need API keys
   ```

---

## Test Coverage Goals

- **Data Layer:** 80%+ coverage (repositories, services, detectors)
- **Core Logic:** 90%+ coverage (filters, normalizers, utilities)
- **UI Layer:** 50%+ coverage (ViewModels only, no rendering)
- **Overall:** 70%+ coverage

---

## Next Steps

1. ✅ Create test project structure - DONE
2. ✅ Implement InstalledSoftwareRepositoryTests - DONE
3. ✅ Implement MetadataEnrichmentServiceTests - DONE
4. ✅ Implement RawgApiClientTests - DONE
5. ⏳ Implement remaining CRITICAL tests (see priority list)
6. ⏳ Add tests to solution file
7. ⏳ Configure CI/CD pipeline
8. ⏳ Implement HIGH priority tests
9. ⏳ Implement MEDIUM priority tests
10. ⏳ Achieve coverage goals

**Total Tests to Implement: ~80+ test classes, ~400+ test methods**
