using System;
using FluentAssertions;
using NoFencesDataLayer.Repositories;
using Xunit;

namespace NoFences.Tests.Repositories
{
    /// <summary>
    /// Unit tests for AmazonGamesRepository.
    /// Tests repository interface contract and error handling.
    /// Note: These are integration tests that depend on Amazon Games installation.
    /// </summary>
    public class AmazonGamesRepositoryTests
    {
        [Fact]
        public void Constructor_ShouldSucceed()
        {
            // Arrange & Act
            Action act = () => new AmazonGamesRepository();

            // Assert
            act.Should().NotThrow("Constructor should always succeed");
        }

        [Fact]
        public void IsAvailable_ReturnsBoolean()
        {
            // Arrange
            var repository = new AmazonGamesRepository();

            // Act
            bool available = repository.IsAvailable();

            // Assert
            // Just verify it returns without throwing - value depends on installation
            Action act = () => repository.IsAvailable();
            act.Should().NotThrow();
        }

        [Fact]
        public void GetDatabasePath_ReturnsStringOrNull()
        {
            // Arrange
            var repository = new AmazonGamesRepository();

            // Act
            string path = repository.GetDatabasePath();

            // Assert
            // Returns SQL folder path (not individual .sqlite file)
            // Will be null if Amazon Games not installed
            if (path != null)
            {
                path.Should().NotBeEmpty();
                path.Should().Contain("Amazon Games");
            }
        }

        [Fact]
        public void GetInstalledGames_ReturnsListNotNull()
        {
            // Arrange
            var repository = new AmazonGamesRepository();

            // Act
            var games = repository.GetInstalledGames();

            // Assert
            games.Should().NotBeNull("Should always return a list, even if empty");
        }

        [Fact]
        public void GetInstalledGames_WhenAvailable_ReturnsValidGames()
        {
            // Arrange
            var repository = new AmazonGamesRepository();

            // Act
            if (repository.IsAvailable())
            {
                var games = repository.GetInstalledGames();

                // Assert
                games.Should().NotBeNull();
                // If Amazon Games is installed and has games, verify structure
                if (games.Count > 0)
                {
                    games.Should().OnlyContain(g => !string.IsNullOrEmpty(g.Name));
                    games.Should().OnlyContain(g => g.Platform == "Amazon Games");
                }
            }
        }

        [Fact]
        public void GetInstalledGames_WhenUnavailable_ReturnsEmptyList()
        {
            // Arrange
            var repository = new AmazonGamesRepository();

            // Act
            if (!repository.IsAvailable())
            {
                var games = repository.GetInstalledGames();

                // Assert
                games.Should().NotBeNull();
                games.Should().BeEmpty("Should return empty list when Amazon Games not installed");
            }
        }

        [Fact]
        public void MultipleInstantiations_ShouldSucceed()
        {
            // Test that multiple repository instances can be created
            // (tests for static state issues)

            // Arrange & Act
            var repo1 = new AmazonGamesRepository();
            var repo2 = new AmazonGamesRepository();

            // Assert
            repo1.Should().NotBeNull();
            repo2.Should().NotBeNull();
            repo1.Should().NotBeSameAs(repo2);
        }

        [Fact]
        public void GetInstalledGames_CalledMultipleTimes_DoesNotThrow()
        {
            // Test that repository methods can be called multiple times
            // (tests for resource cleanup issues)

            // Arrange
            var repository = new AmazonGamesRepository();

            // Act & Assert
            for (int i = 0; i < 3; i++)
            {
                Action act = () => repository.GetInstalledGames();
                act.Should().NotThrow($"Call #{i + 1} should not throw");
            }
        }

        [Fact]
        public void IsAvailable_MatchesDatabasePathResult()
        {
            // Test consistency between IsAvailable() and GetDatabasePath()

            // Arrange
            var repository = new AmazonGamesRepository();

            // Act
            bool available = repository.IsAvailable();
            string path = repository.GetDatabasePath();

            // Assert
            if (available)
            {
                path.Should().NotBeNullOrEmpty("Database path should exist when available");
            }
            else
            {
                path.Should().BeNullOrEmpty("Database path should be null when unavailable");
            }
        }
    }
}
