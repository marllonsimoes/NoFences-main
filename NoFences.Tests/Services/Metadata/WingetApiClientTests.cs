using System;
using System.Threading.Tasks;
using FluentAssertions;
using NoFencesDataLayer.Services.Metadata;
using Xunit;

namespace NoFences.Tests.Services.Metadata
{
    public class WingetApiClientTests
    {
        [Fact]
        public void Constructor_ShouldSucceed()
        {
            var client = new WingetApiClient();

            client.Should().NotBeNull();
            client.ProviderName.Should().Be("Winget");
            client.Priority.Should().Be(1);
        }

        [Fact]
        public void IsAvailable_ShouldReturnBoolean()
        {
            var client = new WingetApiClient();

            bool available = client.IsAvailable();

            Action act = () => client.IsAvailable();
            act.Should().NotThrow("Should return a boolean without throwing");
        }

        [Fact]
        public async Task SearchByNameAsync_WithNullName_ReturnsNull()
        {
            var client = new WingetApiClient();

            var result = await client.SearchByNameAsync(null);

            result.Should().BeNull("Should return null for null software name");
        }

        [Fact]
        public async Task SearchByNameAsync_WithEmptyName_ReturnsNull()
        {
            var client = new WingetApiClient();

            var result = await client.SearchByNameAsync("");

            result.Should().BeNull("Should return null for empty software name");
        }

        [Fact]
        public async Task SearchByNameAsync_WithWhitespaceName_ReturnsNull()
        {
            var client = new WingetApiClient();

            var result = await client.SearchByNameAsync("   ");

            result.Should().BeNull("Should return null for whitespace software name");
        }

        [Fact]
        public async Task SearchByNameAsync_ShouldNotThrow()
        {
            var client = new WingetApiClient();

            Func<Task> act = async () => await client.SearchByNameAsync("Visual Studio Code");

            await act.Should().NotThrowAsync("Should handle winget CLI calls gracefully");
        }

        [Fact]
        public async Task SearchByNameAsync_WithValidName_ReturnsMetadataOrNull()
        {
            var client = new WingetApiClient();

            var result = await client.SearchByNameAsync("Visual Studio Code");

            if (result != null)
            {
                result.Should().NotBeNull();
                result.Source.Should().Be("Winget");
                result.Confidence.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(1);
            }
        }

        [Fact]
        public void ProviderName_ShouldReturnWinget()
        {
            var client = new WingetApiClient();

            var providerName = client.ProviderName;

            providerName.Should().Be("Winget");
        }

        [Fact]
        public void Priority_ShouldBe1()
        {
            var client = new WingetApiClient();

            var priority = client.Priority;

            priority.Should().Be(1, "Winget should be second priority after RAWG");
        }
    }
}
