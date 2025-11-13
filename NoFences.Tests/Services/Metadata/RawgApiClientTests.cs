using System;
using System.Threading.Tasks;
using FluentAssertions;
using NoFencesDataLayer.Services.Metadata;
using Xunit;

namespace NoFences.Tests.Services.Metadata
{
    /// <summary>
    /// Unit tests for RawgApiClient.
    /// Tests RAWG API client for game metadata enrichment.
    /// Note: Integration tests require valid RAWG API key.
    /// </summary>
    public class RawgApiClientTests
    {
        [Fact]
        public void Constructor_WithApiKey_ShouldSucceed()
        {
            // Arrange & Act
            var client = new RawgApiClient("test_api_key");

            // Assert
            client.Should().NotBeNull();
            client.ProviderName.Should().Be("RAWG");
            client.Priority.Should().Be(1);
        }

        [Fact]
        public void Constructor_Default_ShouldSucceed()
        {
            // Arrange & Act
            var client = new RawgApiClient();

            // Assert
            client.Should().NotBeNull();
            client.ProviderName.Should().Be("RAWG");
        }

        [Fact]
        public void IsAvailable_WithoutApiKey_ReturnsFalse()
        {
            // Arrange
            var client = new RawgApiClient("");

            // Act
            bool available = client.IsAvailable();

            // Assert
            available.Should().BeFalse("Should return false when API key is empty");
        }

        [Fact]
        public void IsAvailable_WithApiKey_ReturnsTrue()
        {
            // Arrange
            var client = new RawgApiClient("valid_key");

            // Act
            bool available = client.IsAvailable();

            // Assert
            available.Should().BeTrue("Should return true when API key is provided");
        }

        [Fact]
        public async Task SearchByNameAsync_WithNullName_ReturnsNull()
        {
            // Arrange
            var client = new RawgApiClient("test_key");

            // Act
            var result = await client.SearchByNameAsync(null);

            // Assert
            result.Should().BeNull("Should return null for null game name");
        }

        [Fact]
        public async Task SearchByNameAsync_WithEmptyName_ReturnsNull()
        {
            // Arrange
            var client = new RawgApiClient("test_key");

            // Act
            var result = await client.SearchByNameAsync("");

            // Assert
            result.Should().BeNull("Should return null for empty game name");
        }

        [Fact]
        public async Task SearchByNameAsync_WithoutApiKey_ReturnsNull()
        {
            // Arrange
            var client = new RawgApiClient("");

            // Act
            var result = await client.SearchByNameAsync("Test Game");

            // Assert
            result.Should().BeNull("Should return null when API key not configured");
        }

        [Fact]
        public void ProviderProperties_ShouldHaveCorrectValues()
        {
            // Arrange
            var client = new RawgApiClient();

            // Act & Assert
            client.ProviderName.Should().Be("RAWG");
            client.Priority.Should().Be(1, "RAWG should have highest priority for games");
        }

        // Note: Real API integration tests would require:
        // - Valid RAWG API key from configuration
        // - Network connectivity
        // - API rate limit handling
        // These would be marked with [Category("Integration")] and skipped in CI/CD
    }
}
