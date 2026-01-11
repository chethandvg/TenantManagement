# TentMan Test Projects - Phase 1 Complete ✅

This directory contains all test projects for the TentMan application, organized by test type and scope.

## 📁 Test Project Structure

```
tests/
├── TentMan.UnitTests/                    # Unit tests for business logic
│   ├── Application/                    # Tests for Application layer
│   │   └── Products/
│   │       └── Queries/
│   │           └── GetProductsQueryHandlerTests.cs
│   ├── TestHelpers/                    # Shared test utilities ✅ NEW
│   │   ├── Builders/                   # Test data builders ✅ NEW
│   │   │   ├── ProductBuilder.cs
│   │   │   └── UserBuilder.cs
│   │   └── Fixtures/
│   │       └── AutoMoqDataAttribute.cs ✅ NEW
│   └── TentMan.UnitTests.csproj         # Updated with new packages ✅
│
├── TentMan.ApiClient.Tests/             # Tests for API client library
│   ├── TestHelpers/                    # ✅ NEW
│   │   ├── Fixtures/
│   │   │   └── AutoMoqDataAttribute.cs
│   │   └── MockHttpMessageHandlerFactory.cs
│   └── TentMan.ApiClient.Tests.csproj   # Updated with new packages ✅
│
├── TentMan.IntegrationTests/            # Integration and E2E tests
│   ├── TestHelpers/                    # ✅ NEW
│   │   └── Fixtures/
│   │       └── AutoMoqDataAttribute.cs
│   ├── TentMan.IntegrationTests.csproj  # Updated with new packages ✅
│   └── README.md                       # Existing documentation
│
└── TESTING_GUIDE.md                   # This file ✅ NEW
```

## 🎉 Phase 1 Completion Summary

### ✅ Completed Tasks

1. **Updated All Test Project Files**
   - Added `AutoFixture.AutoMoq` package
   - Configured code coverage (80% threshold)
   - Added `Bogus` for fake data generation (IntegrationTests)

2. **Created Test Infrastructure**
   - AutoMoqDataAttribute for automatic dependency injection
   - ProductBuilder and UserBuilder for test data
   - MockHttpMessageHandlerFactory for API client testing

3. **Organized Test Structure**
   - Created TestHelpers directories
   - Organized Builders and Fixtures folders
   - Added comprehensive documentation

### 📦 New NuGet Packages Added

| Package | Version | Purpose | Projects |
|---------|---------|---------|----------|
| `AutoFixture.AutoMoq` | 4.18.1 | Auto-mocking with AutoFixture | All test projects |
| `Bogus` | 35.6.1 | Fake data generation | IntegrationTests only |

### 🧪 Test Categories

Tests are organized using xUnit traits:

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "Products")]
public class MyTests { }
```

| Category | Description | Example |
|----------|-------------|---------|
| `Unit` | Fast, isolated tests for business logic | `[Trait("Category", "Unit")]` |
| `Integration` | Tests with external dependencies (DB, API) | `[Trait("Category", "Integration")]` |
| `ApiClient` | HTTP client and API communication tests | `[Trait("Category", "ApiClient")]` |

## 🚀 Running Tests

### Basic Commands

```bash
# Run all tests
dotnet test

# Run specific category
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
dotnet test --filter "Category=ApiClient"

# Run feature-specific tests
dotnet test --filter "Feature=Products"
dotnet test --filter "Feature=Billing"

# Run billing tests (166 unit tests)
dotnet test tests/TentMan.UnitTests/TentMan.UnitTests.csproj --filter "Category=Unit&Feature=Billing"

# Run with code coverage
dotnet test /p:CollectCoverage=true
```

### Billing Engine Test Suite

The billing engine has comprehensive test coverage:

```bash
# All billing unit tests (166 tests)
dotnet test tests/TentMan.UnitTests/TentMan.UnitTests.csproj --filter "Category=Unit&Feature=Billing"

# Billing integration tests (11 tests)
dotnet test tests/TentMan.IntegrationTests/TentMan.IntegrationTests.csproj --filter "Category=Integration&Feature=Billing"

# Edge case tests
dotnet test tests/TentMan.UnitTests/TentMan.UnitTests.csproj --filter "TestType=EdgeCases"

# Controller tests
dotnet test tests/TentMan.UnitTests/TentMan.UnitTests.csproj --filter "Component=Controller"
```

**Coverage Summary:**
- ✅ 166 unit tests (proration, calculations, services, controllers, edge cases)
- ✅ 11 integration tests (E2E workflows, batch processing, concurrency)
- ✅ 100% edge case coverage per requirements
- ✅ Thread-safe concurrent operations validated
- ✅ Performance tests with 50+ leases

See `BILLING_TEST_SUITE_SUMMARY.md` for detailed coverage information.


### Coverage Reports

Coverage reports are automatically generated in `TestResults/`:
- `coverage.cobertura.xml` - Cobertura format
- `coverage.opencover.xml` - OpenCover format

**Coverage Thresholds:**
- Line Coverage: 80%
- Branch Coverage: 80%

## 📚 Using the New Test Infrastructure

### 1. AutoMoqData Attribute

Automatically generates test data and mocks:

```csharp
using TentMan.UnitTests.TestHelpers.Fixtures;
using AutoFixture.Xunit2;
using FluentAssertions;
using Moq;
using Xunit;

[Theory, AutoMoqData]
public async Task Handle_ShouldReturnProducts_WhenProductsExist(
    [Frozen] Mock<IProductRepository> mockRepository,
    [Frozen] Mock<ILogger<GetProductsQueryHandler>> mockLogger,
    GetProductsQueryHandler handler,
    List<Product> products)
{
    // Arrange - mockRepository and handler are auto-created
    mockRepository
        .Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
        .ReturnsAsync((products, products.Count));

    var query = new GetProductsQuery(PageNumber: 1, PageSize: 10);

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().HaveCount(products.Count);
}
```

**Benefits:**
- No manual mock creation
- Auto-generated test data with realistic values
- Reduced boilerplate code
- Consistent test setup

### 2. Test Data Builders

Create test entities with fluent API:

```csharp
using TentMan.UnitTests.TestHelpers.Builders;

// Simple product with defaults
var product = new ProductBuilder().Build();

// Custom product
var expensiveProduct = new ProductBuilder()
    .WithName("Premium Product")
    .WithPrice(999.99m)
    .WithOwnerId(userId)
    .Build();

// Multiple products
var products = ProductBuilder.CreateMany(5);

// Products for specific owner
var userProducts = ProductBuilder.CreateManyForOwner(userId, 3);

// Deleted product
var deletedProduct = new ProductBuilder()
    .WithName("Deleted Product")
    .AsDeleted()
    .Build();
```

**Available Builders:**
- `ProductBuilder` - Creates Product entities
- `UserBuilder` - Creates User entities

### 3. MockHttp for API Client Tests

Test HTTP clients without real network calls:

```csharp
using TentMan.ApiClient.Tests.TestHelpers;
using RichardSzalay.MockHttp;

public class ProductsApiClientTests
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly ProductsApiClient _client;

    public ProductsApiClientTests()
    {
        _mockHttp = MockHttpMessageHandlerFactory.CreateHandler();
        var client = _mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://api.example.com/");
        _client = new ProductsApiClient(client, NullLogger<ProductsApiClient>.Instance);
    }

    [Fact]
    public async Task GetProducts_ReturnsSuccess()
    {
        // Arrange
        var expectedResponse = new ApiResponse<PagedResult<ProductDto>>
        {
            Success = true,
            Data = new PagedResult<ProductDto> { /* ... */ }
        };

        _mockHttp.When("https://api.example.com/products*")
            .Respond("application/json", JsonSerializer.Serialize(expectedResponse));

        // Act
        var result = await _client.GetProductsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }
}
```

## 🎯 Example: Refactoring Existing Tests

### Before (Manual Setup)

```csharp
public class GetProductsQueryHandlerTests
{
    private readonly Mock<IProductRepository> _mockRepository;
    private readonly Mock<ILogger<GetProductsQueryHandler>> _mockLogger;
    private readonly GetProductsQueryHandler _handler;

    public GetProductsQueryHandlerTests()
    {
        _mockRepository = new Mock<IProductRepository>();
        _mockLogger = new Mock<ILogger<GetProductsQueryHandler>>();
        _handler = new GetProductsQueryHandler(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "Product 1", Price = 10.99m, RowVersion = new byte[] { 1, 2, 3 } },
            new() { Id = Guid.NewGuid(), Name = "Product 2", Price = 20.99m, RowVersion = new byte[] { 4, 5, 6 } }
        };

        // ... test continues
    }
}
```

### After (Using AutoMoqData and Builders)

```csharp
using TentMan.UnitTests.TestHelpers.Fixtures;
using TentMan.UnitTests.TestHelpers.Builders;

public class GetProductsQueryHandlerTests
{
    [Theory, AutoMoqData]
    public async Task Handle_ShouldReturnProducts(
        [Frozen] Mock<IProductRepository> mockRepository,
        GetProductsQueryHandler handler)
    {
        // Arrange
        var products = ProductBuilder.CreateMany(2);

        mockRepository
            .Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, products.Count));

        // ... test continues
    }
}
```

**Improvements:**
- ✅ No constructor setup needed
- ✅ Cleaner test data creation
- ✅ More readable and maintainable
- ✅ Easier to add new test parameters

## 🎨 Best Practices

1. **Use AutoMoqData for Dependency Injection**
   - Let AutoFixture create mocks and objects
   - Use `[Frozen]` for shared mock instances

2. **Use Builders for Test Data**
   - Keep test data creation consistent
   - Make tests more readable
   - Easy to create complex scenarios

3. **Follow AAA Pattern**
   - **Arrange**: Setup test data and mocks
   - **Act**: Execute the method under test
   - **Assert**: Verify the results

4. **One Assertion Per Test (when possible)**
   - Focus on single behavior
   - Makes failures easier to diagnose

5. **Meaningful Test Names**
   - Use `MethodName_StateUnderTest_ExpectedBehavior`
   - Examples:
     - `Handle_WhenProductsExist_ReturnsPagedProducts`
     - `CreateProduct_WhenRequestIsValid_CreatesProduct`

6. **Use Traits for Organization**
   ```csharp
   [Trait("Category", "Unit")]
   [Trait("Feature", "Products")]
   ```

## 📋 Next Steps (Upcoming Phases)

### Phase 2: Additional Test Coverage (Week 1)
- [ ] Add command handler tests (Create, Update, Delete)
- [ ] Add validator tests
- [ ] Add domain entity tests

### Phase 3: Integration Test Setup (Week 2)
- [ ] Create WebApplicationFactory setup
- [ ] Add database helpers and seeders
- [ ] Implement first integration tests

### Phase 4: API Client Testing (Week 2)
- [ ] Complete ProductsApiClient tests
- [ ] Add AuthenticationApiClient tests
- [ ] Test error handling scenarios

### Phase 5: Coverage & Documentation (Week 3)
- [ ] Generate coverage reports
- [ ] Add coverage badges
- [ ] Write testing guidelines

### Phase 6: CI/CD Integration (Week 4)
- [ ] Setup GitHub Actions for tests
- [ ] Add coverage reporting
- [ ] Add pull request checks

## 🔍 Verification Steps

Run these commands to verify Phase 1 setup:

```bash
# 1. Restore packages
dotnet restore

# 2. Build all test projects
dotnet build tests/TentMan.UnitTests
dotnet build tests/TentMan.ApiClient.Tests
dotnet build tests/TentMan.IntegrationTests

# 3. Run existing tests to verify infrastructure
dotnet test tests/TentMan.UnitTests --filter "FullyQualifiedName~GetProductsQueryHandlerTests"

# 4. Check coverage configuration
dotnet test tests/TentMan.UnitTests /p:CollectCoverage=true
```

## 📖 Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [AutoFixture Documentation](https://github.com/AutoFixture/AutoFixture)
- [AutoFixture.AutoMoq](https://github.com/AutoFixture/AutoFixture/tree/master/Src/AutoMoq)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Moq Documentation](https://github.com/moq/moq)
- [MockHttp Documentation](https://github.com/richardszalay/mockhttp)
- [Testcontainers Documentation](https://dotnet.testcontainers.org/)

## ✅ Phase 1 Checklist

- [x] Updated TentMan.UnitTests.csproj with new packages
- [x] Updated TentMan.ApiClient.Tests.csproj with new packages
- [x] Updated TentMan.IntegrationTests.csproj with new packages
- [x] Created AutoMoqDataAttribute for UnitTests
- [x] Created AutoMoqDataAttribute for ApiClient.Tests
- [x] Created AutoMoqDataAttribute for IntegrationTests
- [x] Created ProductBuilder
- [x] Created UserBuilder
- [x] Created MockHttpMessageHandlerFactory
- [x] Added code coverage configuration to all projects
- [x] Created comprehensive testing documentation

---

**Phase 1 Status**: ✅ **COMPLETED**  
**Last Updated**: 2025-01-22  
**Next Phase**: Phase 2 - Additional Test Coverage  
**Maintainer**: TentMan Development Team
