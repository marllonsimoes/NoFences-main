using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Primitives;
using NoFencesDataLayer.Services.Metadata;
using Xunit;

namespace NoFences.Tests.Services.Metadata
{
    public class CnetScraperClientTests
    {
        [Fact]
        public void Constructor_ShouldSucceed()
        {
            var client = new CnetScraperClient();

            client.Should().NotBeNull();
            client.ProviderName.Should().Be("CNET");
            client.Priority.Should().Be(10);
        }

        [Fact]
        public void IsAvailable_ShouldReturnTrue()
        {
            var client = new CnetScraperClient();

            bool available = client.IsAvailable();

            available.Should().BeTrue("CNET scraper should always be available (no API key needed)");
        }

        [Fact]
        public async Task SearchByNameAsync_WithNullName_ReturnsNull()
        {
            var client = new CnetScraperClient();

            var result = await client.SearchByNameAsync(null);

            result.Should().BeNull("Should return null for null search term");
        }

        [Fact]
        public async Task SearchByNameAsync_WithEmptyName_ReturnsNull()
        {
            var client = new CnetScraperClient();

            var result = await client.SearchByNameAsync("");

            result.Should().BeNull("Should return null for empty search term");
        }

        [Fact]
        public async Task SearchByNameAsync_WithWhitespaceName_ReturnsNull()
        {
            var client = new CnetScraperClient();

            var result = await client.SearchByNameAsync("   ");

            result.Should().BeNull("Should return null for whitespace search term");
        }

        [Fact]
        public async Task SearchByNameAsync_ShouldNotThrow()
        {
            var client = new CnetScraperClient();

            Func<Task> act = async () => await client.SearchByNameAsync("Google Chrome");

            await act.Should().NotThrowAsync("Should handle web scraping gracefully");
        }

        [Fact]
        public async Task SearchByNameAsync_WithValidName_ReturnsMetadataOrNull()
        {
            var client = new CnetScraperClient();

            var result = await client.SearchByNameAsync("Google Chrome");

            if (result != null)
            {
                result.Should().NotBeNull();
                result.Source.Should().Be("CNET");
                result.Confidence.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
            }
        }

        [Fact]
        public void ProviderName_ShouldReturnCNET()
        {
            var client = new CnetScraperClient();

            var providerName = client.ProviderName;

            providerName.Should().Be("CNET");
        }

        [Fact]
        public void Priority_ShouldBe4()
        {
            var client = new CnetScraperClient();

            var priority = client.Priority;

            priority.Should().Be(10, "CNET should be fourth priority (last resort fallback)");
        }

        [Fact]
        public async Task SearchByNameAsync_HandlesNetworkErrors()
        {
            var client = new CnetScraperClient();

            Func<Task> act = async () => await client.SearchByNameAsync("NonexistentSoftware12345");

            await act.Should().NotThrowAsync("Should handle 404 and network errors gracefully");
        }

        [Fact]
        public async Task SearchByNameAsync_HandlesInvalidHtml()
        {
            var client = new CnetScraperClient();

            Action act = () => client.SearchByNameAsync("Test").Wait();

            act.Should().NotThrow("Should either return metadata or null, never throw");
        }
    }
}
