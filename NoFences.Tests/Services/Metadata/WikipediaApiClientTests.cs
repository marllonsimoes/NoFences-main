using System;
using System.Threading.Tasks;
using FluentAssertions;
using NoFencesDataLayer.Services.Metadata;
using Xunit;

namespace NoFences.Tests.Services.Metadata
{
    public class WikipediaApiClientTests
    {
        [Fact]
        public void Constructor_ShouldSucceed()
        {
            var client = new WikipediaApiClient();

            client.Should().NotBeNull();
            client.ProviderName.Should().Be("Wikipedia");
            client.Priority.Should().Be(99);
        }

        [Fact]
        public void IsAvailable_ShouldReturnTrue()
        {
            var client = new WikipediaApiClient();

            bool available = client.IsAvailable();

            available.Should().BeTrue("Wikipedia API should always be available (no API key needed)");
        }

        [Fact]
        public async Task SearchByNameAsync_WithNullName_ReturnsNull()
        {
            var client = new WikipediaApiClient();

            var result = await client.SearchByNameAsync(null);

            result.Should().BeNull("Should return null for null search term");
        }

        [Fact]
        public async Task SearchByNameAsync_WithEmptyName_ReturnsNull()
        {
            var client = new WikipediaApiClient();

            var result = await client.SearchByNameAsync("");

            result.Should().BeNull("Should return null for empty search term");
        }

        [Fact]
        public async Task SearchByNameAsync_WithWhitespaceName_ReturnsNull()
        {
            var client = new WikipediaApiClient();

            var result = await client.SearchByNameAsync("   ");

            result.Should().BeNull("Should return null for whitespace search term");
        }

        [Fact]
        public async Task SearchByNameAsync_ShouldNotThrow()
        {
            var client = new WikipediaApiClient();

            Func<Task> act = async () => await client.SearchByNameAsync("Microsoft Windows");

            await act.Should().NotThrowAsync("Should handle Wikipedia API calls gracefully");
        }

        [Fact]
        public async Task SearchByNameAsync_WithValidName_ReturnsMetadataOrNull()
        {
            var client = new WikipediaApiClient();

            var result = await client.SearchByNameAsync("Microsoft Windows");

            if (result != null)
            {
                result.Should().NotBeNull();
                result.Source.Should().Be("Wikipedia");
                result.Confidence.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
                result.Description.Should().NotBeNullOrEmpty("Wikipedia should provide a description");
            }
        }

        [Fact]
        public void ProviderName_ShouldReturnWikipedia()
        {
            var client = new WikipediaApiClient();

            var providerName = client.ProviderName;

            providerName.Should().Be("Wikipedia");
        }

        [Fact]
        public void Priority_ShouldBe99()
        {
            var client = new WikipediaApiClient();

            var priority = client.Priority;

            priority.Should().Be(99, "Wikipedia should be third priority (fallback after RAWG and Winget)");
        }
    }
}
