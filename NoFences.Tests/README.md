# NoFences Test Suite

Comprehensive unit and integration tests for the NoFences application.

## Project Setup

### Requirements
- .NET Framework 4.8 or later
- xUnit test runner (Visual Studio, Rider, or `dotnet test`)
- Test packages:
  - xUnit 2.6.2
  - Moq 4.20.70 (for mocking)
  - FluentAssertions 6.12.0 (for readable assertions)

### Adding to Solution

```bash
# From solution root
dotnet sln add NoFences.Tests/NoFences.Tests.csproj
```

## Running Tests

### All Tests
```bash
dotnet test
```

### Fast Tests Only (exclude integration tests)
```bash
dotnet test --filter Category!=Integration
```

### Specific Test Class
```bash
dotnet test --filter FullyQualifiedName~InstalledSoftwareRepositoryTests
```

### With Code Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### In Visual Studio
- Test Explorer → Run All
- Or right-click test method → Run Test(s)

## Test Structure

```
NoFences.Tests/
├── Repositories/          # Data access layer tests
├── Services/              # Business logic tests
│   └── Metadata/          # API client tests
├── Detectors/             # Platform detector tests (Steam, GOG, etc.)
├── Utilities/             # Helper and filter tests
├── Core/                  # Core models and settings tests
├── ViewModels/            # UI logic tests (no rendering)
└── Integration/           # End-to-end workflow tests
```

## Test Categories

### Unit Tests (Fast)
- **Repository Tests:** In-memory/temp databases
- **Service Tests:** Mocked dependencies
- **Utility Tests:** Pure logic, no I/O
- **ViewModel Tests:** No UI rendering

### Integration Tests (Slower)
- **Detector Tests:** May access file system/registry
- **API Tests:** Real HTTP calls (skipped without API keys)
- **Database Tests:** Real SQLite operations

## Writing Tests

### Basic Test Pattern

```csharp
using Xunit;
using FluentAssertions;

public class MyComponentTests
{
    [Fact]
    public void Method_Scenario_ExpectedBehavior()
    {
        // Arrange
        var input = "test";

        // Act
        var result = ProcessInput(input);

        // Assert
        result.Should().Be("expected");
    }
}
```

### Theory Tests (Data-Driven)

```csharp
[Theory]
[InlineData("input1", "output1")]
[InlineData("input2", "output2")]
public void Method_MultipleInputs_ProducesExpectedOutputs(string input, string expected)
{
    // Act
    var result = ProcessInput(input);

    // Assert
    result.Should().Be(expected);
}
```

### Mocking Dependencies

```csharp
using Moq;

public class ServiceTests
{
    [Fact]
    public void Service_WithMockedRepository_WorksCorrectly()
    {
        // Arrange
        var mockRepo = new Mock<IRepository>();
        mockRepo.Setup(r => r.GetAll()).Returns(new List<Item> { /* test data */ });

        var service = new MyService(mockRepo.Object);

        // Act
        var result = service.DoSomething();

        // Assert
        result.Should().NotBeNull();
        mockRepo.Verify(r => r.GetAll(), Times.Once());
    }
}
```

### Test Fixtures (Setup/Cleanup)

```csharp
public class RepositoryTests : IDisposable
{
    private readonly string testDatabasePath;
    private readonly IRepository repository;

    public RepositoryTests()
    {
        // Setup - runs before each test
        testDatabasePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        repository = new Repository(testDatabasePath);
    }

    public void Dispose()
    {
        // Cleanup - runs after each test
        if (File.Exists(testDatabasePath))
            File.Delete(testDatabasePath);
    }

    [Fact]
    public void Test_UsesSetup()
    {
        // repository is ready to use
    }
}
```

## FluentAssertions Examples

```csharp
// Collections
result.Should().HaveCount(5);
result.Should().Contain(item => item.Name == "Test");
result.Should().OnlyContain(item => item.IsActive);
result.Should().BeEmpty();

// Strings
name.Should().Be("Expected");
name.Should().Contain("substring");
name.Should().StartWith("prefix");
name.Should().NotBeNullOrEmpty();

// Numbers
count.Should().BeGreaterThan(0);
count.Should().BeLessOrEqualTo(100);
duration.Should().BeLessThan(100, "performance requirement");

// Booleans
flag.Should().BeTrue();
flag.Should().BeFalse();

// Nulls
result.Should().NotBeNull();
result.Should().BeNull();

// Exceptions
Action act = () => ThrowException();
act.Should().Throw<ArgumentException>();
act.Should().NotThrow();

// Types
result.Should().BeOfType<MyType>();
result.Should().BeAssignableTo<IMyInterface>();

// Dates
date.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
date.Should().BeAfter(startDate);
```

## Test Naming Convention

Pattern: `MethodName_Scenario_ExpectedBehavior`

Examples:
- `GetAll_WithEmptyDatabase_ReturnsEmptyList`
- `Insert_ValidEntry_ShouldSucceed`
- `GetBySource_CaseInsensitive_ReturnsMatches`
- `ApplyFilter_DatabaseQuery_PerformanceUnder100ms`

## Test Documentation

Each test should have:
1. **Clear name** following the convention
2. **Arrange/Act/Assert** sections (AAA pattern)
3. **Comments** explaining complex logic or business rules
4. **Assertions with messages** for performance/business requirements

```csharp
[Fact]
public void ApplyFilter_DatabaseQuery_PerformanceUnder100ms()
{
    // Arrange
    var largeSoftwareList = /* ... */;

    // Act
    var stopwatch = Stopwatch.StartNew();
    var result = ApplyFilter(largeSoftwareList);
    stopwatch.Stop();

    // Assert
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, 
        "Database queries should be fast for good UX");
}
```

## Skipping Tests

### Skip Temporarily
```csharp
[Fact(Skip = "Temporarily disabled due to bug #123")]
public void TestName() { }
```

### Skip Integration Tests
```csharp
[Fact]
[Trait("Category", "Integration")]
public void SlowIntegrationTest() { }
```

Run with: `dotnet test --filter Category!=Integration`

### Skip Tests Requiring API Keys
```csharp
[Fact(Skip = "Requires RAWG API key")]
public async Task SearchByNameAsync_ValidGame_ReturnsMetadata() { }
```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '4.8'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore
      
      - name: Run Unit Tests
        run: dotnet test --no-build --filter Category!=Integration --logger "trx"
      
      - name: Run Integration Tests
        run: dotnet test --no-build --filter Category=Integration --logger "trx"
        continue-on-error: true  # May fail without API keys
```

## Code Coverage

### Generate Coverage Report

```bash
# 1. Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# 2. Install ReportGenerator tool
dotnet tool install -g dotnet-reportgenerator-globaltool

# 3. Generate HTML report
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html

# 4. Open report
start coverage-report/index.html
```

### Coverage Goals
- Data Layer: 80%+
- Services: 80%+
- Utilities: 90%+
- Overall: 70%+

## Best Practices

### DO:
- ✅ Write tests for all public methods
- ✅ Test edge cases and error conditions
- ✅ Use descriptive test names
- ✅ Keep tests fast (< 1s for unit tests)
- ✅ Use mocks for external dependencies
- ✅ Clean up resources (IDisposable)
- ✅ Test one thing per test

### DON'T:
- ❌ Test private methods directly (test through public API)
- ❌ Depend on external resources (network, real databases)
- ❌ Have tests depend on each other
- ❌ Use Thread.Sleep (use proper async/await)
- ❌ Ignore flaky tests (fix them)
- ❌ Write tests that touch the UI (use ViewModels)

## Test Priorities

### CRITICAL (Must have):
1. Repository tests (data integrity)
2. Filter tests (core functionality)
3. Detector tests (multiple bug fixes)
4. Service tests (business logic)

### HIGH (Should have):
1. Metadata provider tests
2. Utility tests
3. ViewModel tests

### MEDIUM (Nice to have):
1. Integration tests
2. Performance benchmarks

## Debugging Tests

### In Visual Studio
1. Set breakpoint in test
2. Right-click test → Debug Test(s)
3. Step through code

### Output Window
```csharp
[Fact]
public void Test_WithDebugOutput()
{
    System.Diagnostics.Debug.WriteLine("Debug message");
    Console.WriteLine("Console message");
    
    // These appear in Test Output window
}
```

### Test Explorer Output
- View → Test Explorer
- Click test → View Test Output

## Extending Tests

### Adding New Test Class

1. Create file in appropriate folder
2. Add to `NoFences.Tests.csproj` (if not using SDK-style)
3. Follow naming convention
4. Add `using` statements
5. Implement tests

### Adding Test Data

```csharp
public class TestData
{
    public static TheoryData<string, int> SampleData =>
        new TheoryData<string, int>
        {
            { "input1", 1 },
            { "input2", 2 },
            { "input3", 3 }
        };
}

[Theory]
[MemberData(nameof(TestData.SampleData), MemberType = typeof(TestData))]
public void Test_WithTheoryData(string input, int expected) { }
```

## Troubleshooting

### Tests Not Discovered
- Rebuild solution
- Check project references
- Verify test runner package versions

### Tests Fail on CI but Pass Locally
- Check file paths (use Path.Combine)
- Check platform-specific code
- Check for hardcoded paths/URLs

### Slow Tests
- Use `[Trait("Category", "Slow")]` to mark
- Profile with `dotnet test --blame-hang-timeout 30s`
- Consider mocking expensive operations

## Resources

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [.NET Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

## Contributing

When adding new features:
1. Write tests first (TDD)
2. Ensure all tests pass
3. Maintain or improve coverage
4. Document complex test scenarios

---

**Current Status:** 7 test classes created, ~400+ tests planned across all sessions
**Next Priority:** Implement CRITICAL tests (see COMPREHENSIVE_TEST_PLAN.md)
