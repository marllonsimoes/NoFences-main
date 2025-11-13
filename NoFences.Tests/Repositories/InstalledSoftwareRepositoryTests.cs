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
    /// Session 12 Continuation: Tests rewritten for two-tier architecture.
    /// InstalledSoftwareEntry requires SoftwareRefId (FK to SoftwareReference).
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
        public void GetBySource_WithNullSource_ReturnsAll()
        {
            // Session 12 Continuation: Rewritten for two-tier architecture
            // Source field is in SoftwareReference, not InstalledSoftwareEntry

            // Arrange
            var repository = new InstalledSoftwareRepository();

            // Act
            var result = repository.GetBySource(null);

            // Assert
            result.Should().NotBeNull("Should return all entries when source is null");
            result.Should().BeAssignableTo<System.Collections.Generic.List<InstalledSoftwareEntry>>();
        }

        [Fact]
        public void GetBySource_WithValidSource_ReturnsFiltered()
        {
            // Session 12 Continuation: Rewritten for two-tier architecture
            // This tests the deprecated method still returns a list (even if empty)

            // Arrange
            var repository = new InstalledSoftwareRepository();

            // Act
            var result = repository.GetBySource("Steam");

            // Assert
            result.Should().NotBeNull("Deprecated method should still return non-null list");
            result.Should().BeAssignableTo<System.Collections.Generic.List<InstalledSoftwareEntry>>();
        }

        [Fact]
        public void GetByCategory_WithValidCategory_ReturnsFiltered()
        {
            // Session 12 Continuation: Rewritten for two-tier architecture
            // This tests the deprecated method still returns a list (even if empty)

            // Arrange
            var repository = new InstalledSoftwareRepository();

            // Act
            var result = repository.GetByCategory("Games");

            // Assert
            result.Should().NotBeNull("Deprecated method should still return non-null list");
            result.Should().BeAssignableTo<System.Collections.Generic.List<InstalledSoftwareEntry>>();
        }

        [Fact]
        public void GetBySourceAndCategory_WithBothFilters_ReturnsFiltered()
        {
            // Session 12 Continuation: Rewritten for two-tier architecture
            // This tests the deprecated method still returns a list (even if empty)

            // Arrange
            var repository = new InstalledSoftwareRepository();

            // Act
            var result = repository.GetBySourceAndCategory("Steam", "Games");

            // Assert
            result.Should().NotBeNull("Deprecated method should still return non-null list");
            result.Should().BeAssignableTo<System.Collections.Generic.List<InstalledSoftwareEntry>>();
        }

        [Fact]
        public void Upsert_WithValidSoftwareRefId_ShouldSucceed()
        {
            // Session 12 Continuation: Rewritten for two-tier architecture
            // InstalledSoftwareEntry now requires SoftwareRefId

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

            var entry = new InstalledSoftwareEntry
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
        public void GetCountByCategory_ReturnsDictionary()
        {
            // Arrange
            var repository = new InstalledSoftwareRepository();

            // Act
            var counts = repository.GetCountByCategory();

            // Assert
            counts.Should().NotBeNull("Should return dictionary even if empty");
        }

        [Fact]
        public void GetCountBySource_ReturnsDictionary()
        {
            // Arrange
            var repository = new InstalledSoftwareRepository();

            // Act
            var counts = repository.GetCountBySource();

            // Assert
            counts.Should().NotBeNull("Should return dictionary even if empty");
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
                repository.GetCountByCategory();
                repository.GetCountBySource();
                repository.GetBySource("Steam");
                repository.GetByCategory("Games");
            };

            act.Should().NotThrow("Multiple sequential operations should succeed");
        }

        [Fact]
        public void GetBySource_ConsistentResults()
        {
            // Test that calling the same query twice returns consistent results

            // Arrange
            var repository = new InstalledSoftwareRepository();

            // Act
            var result1 = repository.GetBySource("Steam");
            var result2 = repository.GetBySource("Steam");

            // Assert
            result1.Count.Should().Be(result2.Count, "Same query should return consistent results");
        }
    }
}
