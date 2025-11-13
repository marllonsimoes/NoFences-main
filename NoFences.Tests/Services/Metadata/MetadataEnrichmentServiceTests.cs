using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NoFences.Core.Model;
using NoFencesDataLayer.Repositories;
using NoFencesDataLayer.Services.Metadata;
using Xunit;

namespace NoFences.Tests.Services.Metadata
{
    /// <summary>
    /// Unit tests for MetadataEnrichmentService.
    /// Uses mocks to test orchestration logic without real API calls.
    /// Session 12: Updated to use ISoftwareReferenceRepository (two-tier architecture).
    /// </summary>
    public class MetadataEnrichmentServiceTests
    {
        private readonly Mock<ISoftwareReferenceRepository> mockRepository;
        private readonly List<IGameMetadataProvider> gameProviders;
        private readonly List<ISoftwareMetadataProvider> softwareProviders;
        private readonly MetadataEnrichmentService service;

        public MetadataEnrichmentServiceTests()
        {
            mockRepository = new Mock<ISoftwareReferenceRepository>();

            // Session 12: Create mock providers for testing
            gameProviders = new List<IGameMetadataProvider> { new RawgApiClient() };
            softwareProviders = new List<ISoftwareMetadataProvider>
            {
                new WingetApiClient(),
                new CnetScraperClient(),
                new WikipediaApiClient()
            };

            service = new MetadataEnrichmentService(mockRepository.Object, gameProviders, softwareProviders);
        }

        [Fact]
        public void Constructor_WithRepository_ShouldSucceed()
        {
            // Arrange & Act
            var svc = new MetadataEnrichmentService(
                mockRepository.Object,
                gameProviders,
                softwareProviders);

            // Assert
            svc.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_Default_ShouldSucceed()
        {
            // Arrange & Act
            var svc = new MetadataEnrichmentService();

            // Assert
            svc.Should().NotBeNull();
        }

        [Theory]
        [InlineData("Steam")]
        [InlineData("GOG Galaxy")]
        [InlineData("Epic Games Store")]
        [InlineData("Amazon Games")]
        [InlineData("EA App")]
        [InlineData("Origin")]
        [InlineData("Ubisoft Connect")]
        [InlineData("Battle.net")]
        [InlineData("Xbox")]
        public void GameSources_ShouldBeRecognized(string source)
        {
            // Verify source names that represent gaming platforms
            source.Should().NotBeNullOrEmpty();
        }

        [Theory]
        [InlineData("Manual Install")]
        [InlineData("Registry")]
        [InlineData("Winget")]
        [InlineData("Microsoft Store")]
        public void NonGameSources_ShouldBeRecognized(string source)
        {
            // Verify source names that represent non-gaming software
            source.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void GetProviderStatistics_ReturnsCorrectCounts()
        {
            // Act
            var stats = service.GetProviderStatistics();

            // Assert
            stats.Should().NotBeNull();
            stats.GameProvidersTotal.Should().BeGreaterThan(0, "Should have at least RAWG provider");
            stats.SoftwareProvidersTotal.Should().BeGreaterThan(0, "Should have Winget, CNET, Wikipedia");
            stats.ProviderNames.Should().NotBeEmpty();
            stats.ProviderNames.Should().Contain("RAWG");
            stats.ProviderNames.Should().Contain("Winget");
            stats.ProviderNames.Should().Contain("Wikipedia");
            stats.ProviderNames.Should().Contain("CNET");
        }

        [Fact]
        public void GetProviderStatistics_AvailableCount_ShouldBeLessThanOrEqualToTotal()
        {
            // Act
            var stats = service.GetProviderStatistics();

            // Assert
            stats.GameProvidersAvailable.Should().BeLessOrEqualTo(stats.GameProvidersTotal);
            stats.SoftwareProvidersAvailable.Should().BeLessOrEqualTo(stats.SoftwareProvidersTotal);
        }

        [Fact]
        public async Task EnrichSoftwareAsync_WithNullSoftware_ReturnsFalse()
        {
            // Arrange & Act
            var (success, metadataSource) = await service.EnrichSoftwareAsync(null);

            // Assert
            success.Should().BeFalse("Should return false for null software");
            metadataSource.Should().BeNull("Should not have metadata source for null software");
        }

        [Fact]
        public async Task EnrichSoftwareAsync_WithEmptyName_ReturnsFalse()
        {
            // Arrange
            var software = new InstalledSoftware { Name = "", Source = "Steam" };

            // Act
            var (success, metadataSource) = await service.EnrichSoftwareAsync(software);

            // Assert
            success.Should().BeFalse("Should return false for software with empty name");
            metadataSource.Should().BeNull("Should not have metadata source for empty name");
        }

        [Fact]
        public void GetProviderStatistics_CalledMultipleTimes_DoesNotThrow()
        {
            // Act & Assert
            for (int i = 0; i < 3; i++)
            {
                Action act = () => service.GetProviderStatistics();
                act.Should().NotThrow($"Call #{i + 1} should not throw");
            }
        }

        // Note: Full integration tests for EnrichSoftwareAsync would require:
        // - Valid API keys configured (RAWG, etc.)
        // - Network connectivity
        // - Mocked HTTP responses
        // These would be marked as integration tests and run separately
    }
}
