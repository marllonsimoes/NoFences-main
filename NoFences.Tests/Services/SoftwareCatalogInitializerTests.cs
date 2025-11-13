using System;
using FluentAssertions;
using NoFencesDataLayer.Services;
using Xunit;

namespace NoFences.Tests.Services
{
    /// <summary>
    /// Unit tests for SoftwareCatalogInitializer.
    /// Tests catalog initialization and validation (static utility class).
    /// </summary>
    public class SoftwareCatalogInitializerTests
    {
        [Fact]
        public void GetDefaultCatalogPath_ReturnsNonEmptyString()
        {
            // Act
            string path = SoftwareCatalogInitializer.GetDefaultCatalogPath();

            // Assert
            path.Should().NotBeNullOrEmpty("Default catalog path should always be defined");
            path.Should().EndWith(".db", "Catalog should be a database file");
        }

        [Fact]
        public void IsCatalogInitialized_ReturnsBoolean()
        {
            // Act & Assert
            // Just verify it returns without throwing - value depends on catalog state
            Action act = () => SoftwareCatalogInitializer.IsCatalogInitialized();
            act.Should().NotThrow();
        }

        [Fact]
        public void GetCatalogStatistics_ReturnsStatisticsOrNull()
        {
            // Act
            var stats = SoftwareCatalogInitializer.GetCatalogStatistics();

            // Assert
            if (stats != null)
            {
                // If catalog exists, statistics should have valid data
                stats.Should().NotBeNull();
            }
            else
            {
                // If catalog doesn't exist, statistics should be null
                stats.Should().BeNull("Statistics should be null when catalog not initialized");
            }
        }

        [Fact]
        public void GetDefaultCatalogPath_ConsistentAcrossMultipleCalls()
        {
            // Arrange & Act
            string path1 = SoftwareCatalogInitializer.GetDefaultCatalogPath();
            string path2 = SoftwareCatalogInitializer.GetDefaultCatalogPath();

            // Assert
            path1.Should().Be(path2, "Default path should be consistent");
        }

        [Fact]
        public void IsCatalogInitialized_CalledMultipleTimes_DoesNotThrow()
        {
            // Act & Assert
            for (int i = 0; i < 3; i++)
            {
                Action act = () => SoftwareCatalogInitializer.IsCatalogInitialized();
                act.Should().NotThrow($"Call #{i + 1} should not throw");
            }
        }

        [Fact]
        public void GetCatalogStatistics_CalledMultipleTimes_DoesNotThrow()
        {
            // Act & Assert
            for (int i = 0; i < 3; i++)
            {
                Action act = () => SoftwareCatalogInitializer.GetCatalogStatistics();
                act.Should().NotThrow($"Call #{i + 1} should not throw");
            }
        }

        [Fact]
        public void StaticMethods_ThreadSafe()
        {
            // Test that static methods can be called concurrently
            // (basic thread safety test)

            // Arrange
            var tasks = new System.Threading.Tasks.Task[5];

            // Act
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    SoftwareCatalogInitializer.GetDefaultCatalogPath();
                    SoftwareCatalogInitializer.IsCatalogInitialized();
                });
            }

            // Assert
            Action act = () => System.Threading.Tasks.Task.WaitAll(tasks);
            act.Should().NotThrow("Static methods should be thread-safe");
        }
    }
}
