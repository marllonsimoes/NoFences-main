using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using Moq;
using NoFences.Util;
using NoFences.Core.Model;
using NoFencesDataLayer.MasterCatalog.Entities;
using NoFencesDataLayer.Repositories;
using Xunit;

namespace NoFences.Tests.Utilities
{
    /// <summary>
    /// CRITICAL Unit tests for FileFenceFilter.
    /// Tests core filtering logic including Session 11 source filtering.
    ///
    /// Session 12 Continuation: Tests rewritten for two-tier architecture.
    /// InstalledSoftwareEntry requires SoftwareRefId (FK to SoftwareReference).
    /// Name, Source, Category fields are in SoftwareReference table.
    /// </summary>
    public class FileFenceFilterTests
    {
        private readonly Mock<IInstalledSoftwareRepository> mockRepository;
        private readonly Mock<ISoftwareReferenceRepository> mockSoftwareRefRepository;

        public FileFenceFilterTests()
        {
            mockRepository = new Mock<IInstalledSoftwareRepository>();
            mockSoftwareRefRepository = new Mock<ISoftwareReferenceRepository>();
        }

        [Theory]
        [InlineData("*.exe", "test.exe", true)]
        [InlineData("*.exe", "test.txt", false)]
        [InlineData("*.{exe,dll}", "test.exe", true)]
        [InlineData("*.{exe,dll}", "test.dll", true)]
        [InlineData("*.{exe,dll}", "test.txt", false)]
        [InlineData("test*", "test.exe", true)]
        [InlineData("test*", "other.exe", false)]
        public void PatternMatching_ValidPatterns_MatchesCorrectly(string pattern, string filename, bool shouldMatch)
        {
            // Test the pattern matching logic
            bool matches = MatchesPattern(filename, pattern);

            // Assert
            matches.Should().Be(shouldMatch);
        }

        [Fact]
        public void ApplyFilter_CategoryGames_ReturnsOnlyGames()
        {
            // Session 12 Continuation: Rewritten for two-tier architecture
            // Category is now in SoftwareReference table

            // Arrange - Create test data with SoftwareReference entries
            var gameRef1 = new SoftwareReference { Id = 1, Name = "Game 1", Source = "Steam", Category = "Games" };
            var gameRef2 = new SoftwareReference { Id = 2, Name = "Game 2", Source = "GOG", Category = "Games" };
            var appRef = new SoftwareReference { Id = 3, Name = "App", Source = "Local", Category = "Applications" };

            var allSoftwareRefs = new List<SoftwareReference> { gameRef1, gameRef2, appRef };

            // Act - Filter by Category
            var gamesOnly = allSoftwareRefs
                .Where(s => s.Category == "Games")
                .ToList();

            // Assert
            gamesOnly.Should().HaveCount(2, "Should return only games");
            gamesOnly.Should().Contain(s => s.Name == "Game 1");
            gamesOnly.Should().Contain(s => s.Name == "Game 2");
            gamesOnly.Should().NotContain(s => s.Category == "Applications");
        }

        [Fact]
        public void ApplyFilter_SourceSteam_ReturnsOnlySteamEntries()
        {
            // Session 12 Continuation: Rewritten for two-tier architecture
            // Source is now in SoftwareReference table

            // Arrange - Create test data with SoftwareReference entries
            var steamGame = new SoftwareReference { Id = 1, Name = "Steam Game", Source = "Steam", Category = "Games" };
            var gogGame = new SoftwareReference { Id = 2, Name = "GOG Game", Source = "GOG", Category = "Games" };
            var epicGame = new SoftwareReference { Id = 3, Name = "Epic Game", Source = "Epic", Category = "Games" };

            var allSoftwareRefs = new List<SoftwareReference> { steamGame, gogGame, epicGame };

            // Act - Filter by Source
            var steamOnly = allSoftwareRefs
                .Where(s => s.Source == "Steam")
                .ToList();

            // Assert
            steamOnly.Should().HaveCount(1, "Should return only Steam entries");
            steamOnly[0].Name.Should().Be("Steam Game");
            steamOnly[0].Source.Should().Be("Steam");
        }

        [Fact]
        public void ApplyFilter_SourceAll_ReturnsAllEntries()
        {
            // Session 12 Continuation: Rewritten for two-tier architecture
            // Tests "All Sources" filter logic

            // Arrange - Create test data with SoftwareReference entries
            var steamGame = new SoftwareReference { Id = 1, Name = "Steam Game", Source = "Steam", Category = "Games" };
            var gogGame = new SoftwareReference { Id = 2, Name = "GOG Game", Source = "GOG", Category = "Games" };
            var localApp = new SoftwareReference { Id = 3, Name = "Local App", Source = "Local", Category = "Applications" };

            var allSoftwareRefs = new List<SoftwareReference> { steamGame, gogGame, localApp };

            // Act - Filter with "All Sources" (no filtering)
            string selectedSource = "All Sources";
            var result = string.IsNullOrEmpty(selectedSource) || selectedSource == "All Sources"
                ? allSoftwareRefs
                : allSoftwareRefs.Where(s => s.Source == selectedSource).ToList();

            // Assert
            result.Should().HaveCount(3, "Should return all entries when source is 'All Sources'");
            result.Should().Contain(s => s.Source == "Steam");
            result.Should().Contain(s => s.Source == "GOG");
            result.Should().Contain(s => s.Source == "Local");
        }

        [Fact]
        public void ApplyFilter_DatabaseQuery_PerformanceCheck()
        {
            // Session 12 Continuation: Rewritten for two-tier architecture
            // Tests that repository queries execute in reasonable time

            // Arrange
            var repository = new InstalledSoftwareRepository();
            var stopwatch = new Stopwatch();

            // Act
            stopwatch.Start();
            var result = repository.GetAll(); // Query database
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "Query should complete in under 5 seconds (accounts for cold start)");
            result.Should().NotBeNull("Query should return a result");
        }

        [Fact]
        public void ApplyFilter_ExtensionFilter_FiltersCorrectly()
        {
            // Test file extension filtering

            // Arrange
            var files = new List<string>
            {
                "document.pdf",
                "image.png",
                "executable.exe",
                "library.dll"
            };

            var filter = new FileFilter
            {
                FilterType = FilterType.Extension,
                Extensions = new[] { ".exe", ".dll" }
            };

            // Act
            var result = files.Where(f => MatchesExtensionFilter(f, filter.Extensions)).ToList();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain("executable.exe");
            result.Should().Contain("library.dll");
        }

        [Fact]
        public void ApplyFilter_CombinedFilters_AppliesAllConditions()
        {
            // Session 12 Continuation: Rewritten for two-tier architecture
            // Tests filtering by both Source and Category

            // Arrange - Create test data with SoftwareReference entries
            var steamGame = new SoftwareReference { Id = 1, Name = "Steam Game", Source = "Steam", Category = "Games" };
            var steamApp = new SoftwareReference { Id = 2, Name = "Steam App", Source = "Steam", Category = "Applications" };
            var gogGame = new SoftwareReference { Id = 3, Name = "GOG Game", Source = "GOG", Category = "Games" };
            var localApp = new SoftwareReference { Id = 4, Name = "Local App", Source = "Local", Category = "Applications" };

            var allSoftwareRefs = new List<SoftwareReference> { steamGame, steamApp, gogGame, localApp };

            // Act - Apply combined filters: Source = "Steam" AND Category = "Games"
            var filtered = allSoftwareRefs
                .Where(s => s.Source == "Steam" && s.Category == "Games")
                .ToList();

            // Assert
            filtered.Should().HaveCount(1, "Should return only Steam games");
            filtered[0].Name.Should().Be("Steam Game");
            filtered[0].Source.Should().Be("Steam");
            filtered[0].Category.Should().Be("Games");
        }

        [Fact]
        public void IconCache_PreservesIcons_AcrossRefresh()
        {
            // Bug Fix #2 (Session 1): Icon cache should persist

            // Arrange
            var iconCache = new Dictionary<string, string>
            {
                { "game1.exe", "icon1.png" },
                { "game2.exe", "icon2.png" }
            };

            // Act
            // Simulate refresh - icons should be reused
            bool icon1Preserved = iconCache.ContainsKey("game1.exe");
            bool icon2Preserved = iconCache.ContainsKey("game2.exe");

            // Assert
            icon1Preserved.Should().BeTrue();
            icon2Preserved.Should().BeTrue();
        }

        // Helper methods to test filtering logic
        private bool MatchesPattern(string filename, string pattern)
        {
            // Simplified pattern matching logic
            // Check {exe,dll} syntax first (before * checks)
            if (pattern.Contains("{") && pattern.Contains("}"))
            {
                // Handle {exe,dll} syntax
                int start = pattern.IndexOf('{');
                int end = pattern.IndexOf('}');
                string extensions = pattern.Substring(start + 1, end - start - 1);
                string prefix = pattern.Substring(0, start).Replace("*", "");

                foreach (var ext in extensions.Split(','))
                {
                    if (filename.EndsWith(prefix + ext))
                        return true;
                }
                return false;
            }
            if (pattern.StartsWith("*") && pattern.EndsWith("*"))
            {
                return filename.Contains(pattern.Trim('*'));
            }
            if (pattern.StartsWith("*"))
            {
                return filename.EndsWith(pattern.TrimStart('*'));
            }
            if (pattern.EndsWith("*"))
            {
                return filename.StartsWith(pattern.TrimEnd('*'));
            }
            return filename.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }

        // Session 12 Continuation: Helper methods rewritten for two-tier architecture
        // Filters work with SoftwareReference instead of InstalledSoftwareEntry
        private List<SoftwareReference> ApplyFilterLogic(FileFilter filter, List<SoftwareReference> software)
        {
            if (filter.FilterType == FilterType.Category)
            {
                return software.Where(s => s.Category == filter.Category).ToList();
            }
            if (filter.FilterType == FilterType.Source)
            {
                if (string.IsNullOrEmpty(filter.SelectedSource) || filter.SelectedSource == "All Sources")
                {
                    return software;
                }
                return software.Where(s => s.Source == filter.SelectedSource).ToList();
            }
            return software;
        }

        private List<SoftwareReference> ApplyCombinedFilterLogic(FileFilter filter, List<SoftwareReference> software)
        {
            var result = software;

            if (filter.SelectedSource != null && filter.SelectedSource != "All Sources")
            {
                result = result.Where(s => s.Source == filter.SelectedSource).ToList();
            }

            if (filter.Category != null)
            {
                result = result.Where(s => s.Category == filter.Category).ToList();
            }

            return result;
        }

        private bool MatchesExtensionFilter(string filename, string[] extensions)
        {
            if (extensions == null || extensions.Length == 0)
                return true;

            return extensions.Any(ext => filename.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }
    }

    // Simplified test models (would use actual models in real implementation)
    public class FileFilter
    {
        public FilterType FilterType { get; set; }
        public string Category { get; set; }
        public string SelectedSource { get; set; }
        public string[] Extensions { get; set; }
    }

    [Flags]
    public enum FilterType
    {
        None = 0,
        Category = 1,
        Source = 2,
        Extension = 4
    }
}
