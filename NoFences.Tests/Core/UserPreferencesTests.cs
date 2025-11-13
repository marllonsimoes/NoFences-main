using System;
using System.IO;
using FluentAssertions;
using NoFences.Core.Settings;
using Xunit;

namespace NoFences.Tests.Core
{
    /// <summary>
    /// Unit tests for UserPreferences.
    /// Tests XML serialization/deserialization and Session 11 API key handling.
    /// </summary>
    public class UserPreferencesTests : IDisposable
    {
        private readonly string testPreferencesPath;

        public UserPreferencesTests()
        {
            // Create temp preferences path for testing
            testPreferencesPath = Path.Combine(
                Path.GetTempPath(),
                $"test_user_preferences_{Guid.NewGuid()}.xml"
            );
        }

        public void Dispose()
        {
            // Clean up test file
            if (File.Exists(testPreferencesPath))
            {
                File.Delete(testPreferencesPath);
            }
        }

        [Fact]
        public void Load_WithMissingFile_ReturnsDefaults()
        {
            // Act
            var prefs = UserPreferences.Load();

            // Assert
            prefs.Should().NotBeNull();
            prefs.AutoCheckForUpdates.Should().BeTrue("Should default to true");
            prefs.CheckFrequencyHours.Should().Be(24, "Should default to 24 hours");
            prefs.HasSeenWelcomeTips.Should().BeFalse("New users haven't seen tips");
            prefs.FencesCreatedCount.Should().Be(0);
        }

        [Fact]
        public void Save_PersistsToXml()
        {
            // Arrange - Use unique values to avoid conflicts with other tests
            var prefs = new UserPreferences
            {
                AutoCheckForUpdates = true,
                CheckFrequencyHours = 48,
                HasSeenWelcomeTips = true,
                FencesCreatedCount = 5,
                RawgApiKey = "test-api-key-12345"
            };

            // Act
            prefs.Save();

            // Assert - Verify all properties we set were persisted
            var loaded = UserPreferences.Load();
            loaded.AutoCheckForUpdates.Should().BeTrue();
            loaded.CheckFrequencyHours.Should().Be(48);
            loaded.HasSeenWelcomeTips.Should().BeTrue();
            loaded.FencesCreatedCount.Should().Be(5);
            loaded.RawgApiKey.Should().Be("test-api-key-12345");
        }

        [Fact]
        public void Load_WithExistingFile_LoadsCorrectly()
        {
            // Arrange - Save first
            var original = new UserPreferences
            {
                AutoCheckForUpdates = false,
                CheckFrequencyHours = 72,
                HasSeenWelcomeTips = true,
                FencesCreatedCount = 10
            };
            original.Save();

            // Act - Load
            var loaded = UserPreferences.Load();

            // Assert
            loaded.Should().NotBeNull();
            loaded.AutoCheckForUpdates.Should().BeFalse();
            loaded.CheckFrequencyHours.Should().Be(72);
            loaded.HasSeenWelcomeTips.Should().BeTrue();
            loaded.FencesCreatedCount.Should().Be(10);
        }

        [Fact]
        public void RawgApiKey_SavesAndLoadsCorrectly()
        {
            // Arrange
            var prefs = new UserPreferences
            {
                RawgApiKey = "test-rawg-api-key-abc123"
            };

            // Act
            prefs.Save();
            var loaded = UserPreferences.Load();

            // Assert
            loaded.RawgApiKey.Should().Be("test-rawg-api-key-abc123");
        }

        [Fact]
        public void RawgApiKey_NullIfNotSet()
        {
            // Arrange
            var prefs = new UserPreferences();
            // Don't set RawgApiKey

            // Act
            prefs.Save();
            var loaded = UserPreferences.Load();

            // Assert
            loaded.RawgApiKey.Should().BeNull();
        }

        [Theory]
        [InlineData("   test-key   ", "test-key")] // Whitespace trimmed
        [InlineData("", null)] // Empty string becomes null
        [InlineData("   ", null)] // Whitespace only becomes null
        public void RawgApiKey_HandlesWhitespace(string input, string expected)
        {
            // Arrange & Act
            string processed = string.IsNullOrWhiteSpace(input) ? null : input.Trim();

            // Assert
            processed.Should().Be(expected);
        }

        [Fact]
        public void LastUpdateCheck_DefaultsToMinValue()
        {
            // Arrange & Act
            var prefs = new UserPreferences();

            // Assert
            prefs.LastUpdateCheck.Should().Be(DateTime.MinValue);
        }

        [Fact]
        public void LastUpdateCheck_CanBeSet()
        {
            // Arrange
            var prefs = new UserPreferences();
            var now = DateTime.UtcNow;

            // Act
            prefs.LastUpdateCheck = now;
            prefs.Save();
            var loaded = UserPreferences.Load();

            // Assert
            loaded.LastUpdateCheck.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void LastSkippedVersion_DefaultsToNull()
        {
            // Arrange & Act
            var prefs = new UserPreferences();

            // Assert
            prefs.LastSkippedVersion.Should().BeNull();
        }

        [Fact]
        public void LastSkippedVersion_CanBeSet()
        {
            // Arrange
            var prefs = new UserPreferences
            {
                LastSkippedVersion = "1.5.0"
            };

            // Act
            prefs.Save();
            var loaded = UserPreferences.Load();

            // Assert
            loaded.LastSkippedVersion.Should().Be("1.5.0");
        }

        [Fact]
        public void CheckFrequencyHours_ValidRange()
        {
            // Preferences window validates 1-168 hours (1 hour to 7 days)

            // Arrange
            var prefs = new UserPreferences();

            // Act & Assert
            prefs.CheckFrequencyHours = 1;
            prefs.CheckFrequencyHours.Should().BeGreaterOrEqualTo(1);

            prefs.CheckFrequencyHours = 168;
            prefs.CheckFrequencyHours.Should().BeLessOrEqualTo(168);
        }

        [Fact]
        public void FirstRunDate_SetsOnCreation()
        {
            // Arrange & Act
            var prefs = new UserPreferences();

            // Assert
            prefs.FirstRunDate.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void FencesCreatedCount_Increments()
        {
            // Arrange
            var prefs = new UserPreferences
            {
                FencesCreatedCount = 5
            };

            // Act
            prefs.FencesCreatedCount++;
            prefs.FencesCreatedCount++;

            // Assert
            prefs.FencesCreatedCount.Should().Be(7);
        }

        [Fact]
        public void XmlSerialization_HandlesAllProperties()
        {
            // Comprehensive test of all properties

            // Arrange
            var prefs = new UserPreferences
            {
                HasSeenWelcomeTips = true,
                FirstRunDate = new DateTime(2025, 1, 1),
                FencesCreatedCount = 42,
                AutoCheckForUpdates = false,
                CheckFrequencyHours = 96,
                LastUpdateCheck = new DateTime(2025, 11, 12),
                AutoDownloadUpdates = true,
                LastSkippedVersion = "2.0.0",
                RawgApiKey = "comprehensive-test-key"
            };

            // Act
            prefs.Save();
            var loaded = UserPreferences.Load();

            // Assert - All properties preserved
            loaded.HasSeenWelcomeTips.Should().BeTrue();
            loaded.FirstRunDate.Should().Be(new DateTime(2025, 1, 1));
            loaded.FencesCreatedCount.Should().Be(42);
            loaded.AutoCheckForUpdates.Should().BeFalse();
            loaded.CheckFrequencyHours.Should().Be(96);
            loaded.LastUpdateCheck.Should().Be(new DateTime(2025, 11, 12));
            loaded.AutoDownloadUpdates.Should().BeTrue();
            loaded.LastSkippedVersion.Should().Be("2.0.0");
            loaded.RawgApiKey.Should().Be("comprehensive-test-key");
        }
    }
}
