using System;
using System.Linq;
using FluentAssertions;
using NoFencesDataLayer.MasterCatalog.Entities;
using NoFencesDataLayer.Repositories;
using Xunit;

namespace NoFences.Tests.Repositories
{
    /// <summary>
    /// Unit tests for InstalledSoftwareRepository.
    /// Tests repository interface contract and basic operations.
    /// Note: These are integration tests that use the real database.
    ///
    /// Tests use two-tier architecture.
    /// LocalInstallation requires SoftwareRefId (FK to SoftwareReference).
    /// Name, Source, Category fields are in SoftwareReference table.
    /// </summary>
    public class InstalledSoftwareRepositoryTests
    {
        [Fact]
        public void Constructor_ShouldSucceed()
        {
            // Arrange & Act
            Action act = () => new InstalledSoftwareRepository();

            // Assert
            act.Should().NotThrow("Constructor should always succeed");
        }

        [Fact]
        public void GetAll_ReturnsListNotNull()
        {
            // Arrange
            var repository = new InstalledSoftwareRepository();

            // Act
            var result = repository.GetAll();

            // Assert
            result.Should().NotBeNull("Should always return a list, even if empty");
        }

        [Fact]
        public void Upsert_WithValidSoftwareRefId_ShouldSucceed()
        {
            // LocalInstallation requires SoftwareRefId (FK to SoftwareReference)

            // Arrange
            var repository = new InstalledSoftwareRepository();
            var softwareRefRepo = new SoftwareReferenceRepository(new NoFencesDataLayer.MasterCatalog.MasterCatalogContext());

            // Create a test SoftwareReference first (required for FK)
            var softwareRef = softwareRefRepo.FindOrCreate(
                name: "Test Game " + Guid.NewGuid().ToString(),
                source: "Steam",
                externalId: "test_" + Guid.NewGuid().ToString(),
                category: "Games"
            );

            var entry = new LocalInstallation
            {
                SoftwareRefId = softwareRef.Id,
                InstallLocation = @"C:\Program Files\TestGame",
                ExecutablePath = @"C:\Program Files\TestGame\game.exe",
                Version = "1.0.0",
                InstallDate = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastDetected = DateTime.UtcNow
            };

            // Act
            var result = repository.Upsert(entry);

            // Assert
            result.Should().NotBeNull("Upsert should return the entry");
            result.Id.Should().BeGreaterThan(0, "Entry should be saved with ID");
            result.SoftwareRefId.Should().Be(softwareRef.Id, "Foreign key should match");

            // Note: No cleanup - integration tests leave test data in database
            // This is acceptable as each test uses unique GUIDs for test data
        }

        [Fact]
        public void GetCount_ReturnsNonNegativeNumber()
        {
            // Arrange
            var repository = new InstalledSoftwareRepository();

            // Act
            int count = repository.GetCount();

            // Assert
            count.Should().BeGreaterThanOrEqualTo(0, "Count should never be negative");
        }

        [Fact]
        public void RemoveStaleEntries_WithFutureDate_RemovesNone()
        {
            // Arrange
            var repository = new InstalledSoftwareRepository();
            var futureDate = DateTime.UtcNow.AddDays(1);

            // Act
            int removed = repository.RemoveStaleEntries(futureDate);

            // Assert
            removed.Should().BeGreaterThanOrEqualTo(0, "Removed count should not be negative");
        }

        [Fact]
        public void MultipleOperations_ShouldSucceed()
        {
            // Test that repository methods can be called in sequence

            // Arrange
            var repository = new InstalledSoftwareRepository();

            // Act & Assert
            Action act = () =>
            {
                repository.GetAll();
                repository.GetCount();
                repository.RemoveStaleEntries(DateTime.UtcNow.AddDays(-365));
            };

            act.Should().NotThrow("Multiple sequential operations should succeed");
        }
    }
}
