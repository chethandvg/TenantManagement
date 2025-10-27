# Integration Testing Guide

## 🎯 Overview

This guide explains how to write and run integration tests for the Archu API using the in-memory database approach.

**Key Points:**
- ✅ No real database required
- ✅ In-memory database (EF Core)
- ✅ Fast test execution
- ✅ Full stack testing (Controllers → Services → Database)

---

## 📚 Table of Contents

1. [Getting Started](#getting-started)
2. [Test Structure](#test-structure)
3. [Data Seeding](#data-seeding)
4. [Writing Tests](#writing-tests)
5. [Running Tests](#running-tests)
6. [Best Practices](#best-practices)

---

## 🚀 Getting Started

### Prerequisites

All dependencies are already configured in `Archu.IntegrationTests.csproj`:
- xUnit 2.9.3
- FluentAssertions 7.0.0
- Microsoft.AspNetCore.Mvc.Testing 9.0.0
- Microsoft.EntityFrameworkCore.InMemory 9.0.0

No Docker or SQL Server installation needed!

### Basic Test Template

```csharp
using Archu.IntegrationTests.Fixtures;
using Archu.Infrastructure.Persistence;
using Archu.IntegrationTests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using Xunit;

[Collection("Integration Tests InMemory")]
public class MyFeatureEndpointTests : IAsyncLifetime
{
    private readonly InMemoryWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private ApplicationDbContext _context = null!;

    public MyFeatureEndpointTests(InMemoryWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        _context = _factory.GetDbContext();
        await TestDataSeeder.ClearAllDataAsync(_context);
    }

    public async Task DisposeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    [Fact]
    public async Task MyEndpoint_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new { /* request data */ };

        // Act
        var response = await _client.PostAsJsonAsync("/api/endpoint", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

---

## 📋 Test Structure

### Collection Definition

```csharp
[Collection("Integration Tests InMemory")]
public class MyTests : IAsyncLifetime
{
    // Tests here share the same factory instance
    // Factory is initialized once for the collection
    // Each test gets its own database cleanup
}
```

**Why collections?**
- Efficient resource usage
- WebApplicationFactory is created once per collection
- Each test still gets isolated database state

### Lifecycle Management

```csharp
public class MyTests : IAsyncLifetime
{
    // ✅ Called before each test
    public async Task InitializeAsync()
    {
        _context = _factory.GetDbContext();
        await TestDataSeeder.ClearAllDataAsync(_context);
    }

    // ✅ Called after each test
    public async Task DisposeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }
}
```

---

## 💾 Data Seeding

### Seed a Single Product

```csharp
var productId = await TestDataSeeder.SeedProductAsync(
    _context,
    name: "Test Product",
    price: 99.99m,
    ownerId: null);  // Uses default user ID
```

### Seed Multiple Products

```csharp
var productIds = await TestDataSeeder.SeedProductsAsync(
    _context,
    count: 5,
    ownerId: null);  // Returns List<Guid>
```

### Seed a User

```csharp
var userId = await TestDataSeeder.SeedUserAsync(
    _context,
    email: "user@example.com",
    username: "testuser",
    emailConfirmed: true);
```

### Clear All Data

```csharp
await TestDataSeeder.ClearAllDataAsync(_context);
```

---

## ✍️ Writing Tests

### Example 1: GET with Seeded Data

```csharp
[Fact]
public async Task GetProducts_WithSeededData_ReturnsAllProducts()
{
    // Arrange - Seed test data
    await TestDataSeeder.SeedProductsAsync(_context, count: 3);

    // Act - Call endpoint
    var response = await _client.GetAsync("/api/v1/products");

    // Assert - Verify response
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    
    var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductDto>>>();
    result!.Success.Should().BeTrue();
    result.Data!.Items.Should().HaveCount(3);
}
```

### Example 2: POST with Request

```csharp
[Fact]
public async Task CreateProduct_WithValidRequest_ReturnsCreated()
{
    // Arrange
    var request = new CreateProductRequest
    {
        Name = "New Product",
        Price = 49.99m
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/v1/products", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    
    var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
    result!.Data!.Name.Should().Be("New Product");
}
```

### Example 3: PUT with Update

```csharp
[Fact]
public async Task UpdateProduct_WithValidRequest_ReturnsUpdated()
{
    // Arrange - Seed existing product
    var productId = await TestDataSeeder.SeedProductAsync(
        _context, 
        name: "Original", 
        price: 50.00m);
    
    var product = _context.Products.First(p => p.Id == productId);
    var updateRequest = new UpdateProductRequest
    {
        Id = productId,
        Name = "Updated",
        Price = 75.00m,
        RowVersion = product.RowVersion
    };

    // Act
    var response = await _client.PutAsJsonAsync($"/api/v1/products/{productId}", updateRequest);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    
    var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
    result!.Data!.Name.Should().Be("Updated");
}
```

### Example 4: DELETE

```csharp
[Fact]
public async Task DeleteProduct_WithExistingProduct_ReturnsNoContent()
{
    // Arrange - Seed product
    var productId = await TestDataSeeder.SeedProductAsync(_context);

    // Act
    var response = await _client.DeleteAsync($"/api/v1/products/{productId}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    
    // Verify soft delete
    var deletedProduct = _context.Products.IgnoreQueryFilters()
        .First(p => p.Id == productId);
    deletedProduct.IsDeleted.Should().BeTrue();
}
```

---

## 🏃 Running Tests

### Run All Integration Tests

```bash
dotnet test tests/Archu.IntegrationTests
```

### Run Specific Test Class

```bash
dotnet test tests/Archu.IntegrationTests \
    --filter "FullyQualifiedName~GetProductsEndpointTests"
```

### Run Specific Test Method

```bash
dotnet test tests/Archu.IntegrationTests \
    --filter "FullyQualifiedName~GetProductsEndpointTests.GetProducts_WithSeededData_ReturnsAllProducts"
```

### Run with Detailed Output

```bash
dotnet test tests/Archu.IntegrationTests \
    --verbosity normal \
    --logger "console;verbosity=detailed"
```

### Run with Code Coverage

```bash
dotnet test tests/Archu.IntegrationTests /p:CollectCoverage=true
```

---

## 🎯 Best Practices

### ✅ DO

1. **Use `IAsyncLifetime` for setup/teardown**
   ```csharp
   public async Task InitializeAsync() { /* setup */ }
   public async Task DisposeAsync() { /* cleanup */ }
   ```

2. **Clear data in `InitializeAsync()`**
   ```csharp
   await TestDataSeeder.ClearAllDataAsync(_context);
   ```

3. **Use descriptive test names**
   ```csharp
   public async Task CreateProduct_WithValidRequest_ReturnsCreated()
   ```

4. **Seed minimal necessary data**
   ```csharp
   var productId = await TestDataSeeder.SeedProductAsync(_context);
   ```

5. **Follow AAA pattern**
   ```csharp
   // Arrange
   var request = new CreateProductRequest();
   
   // Act
   var response = await _client.PostAsJsonAsync("/api/v1/products", request);
   
   // Assert
   response.StatusCode.Should().Be(HttpStatusCode.Created);
   ```

### ❌ DON'T

1. **Don't share state between tests**
   ```csharp
   // ❌ BAD - uses data from previous test
   var allProducts = _context.Products.ToList();
   
   // ✅ GOOD - explicitly seeds needed data
   await TestDataSeeder.SeedProductsAsync(_context, count: 3);
   ```

2. **Don't forget to clear data**
   ```csharp
   // ❌ BAD
   public async Task InitializeAsync() { }
   
   // ✅ GOOD
   public async Task InitializeAsync()
   {
       await TestDataSeeder.ClearAllDataAsync(_context);
   }
   ```

3. **Don't test implementation details**
   ```csharp
   // ❌ BAD - testing internal method
   var internalResult = await handler.InternalMethod();
   
   // ✅ GOOD - testing via API
   var response = await _client.GetAsync("/api/endpoint");
   ```

4. **Don't use real external APIs**
   ```csharp
   // ❌ BAD - calls real email service
   await emailService.SendAsync(email);
   
   // ✅ GOOD - mock external dependencies
   mockEmailService.Setup(...).Returns(...);
   ```

---

## 🔍 Common Patterns

### Testing with Invalid Input

```csharp
[Theory]
[InlineData("", 99.99)]     // Empty name
[InlineData("Valid", 0)]    // Zero price
[InlineData("Valid", -10)]  // Negative price
public async Task CreateProduct_WithInvalidInput_ReturnsBadRequest(string name, decimal price)
{
    var request = new CreateProductRequest { Name = name, Price = price };
    var response = await _client.PostAsJsonAsync("/api/v1/products", request);
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}
```

### Testing with Pagination

```csharp
[Fact]
public async Task GetProducts_WithPagination_ReturnsCorrectPage()
{
    // Seed 15 products
    await TestDataSeeder.SeedProductsAsync(_context, count: 15);

    // Request page 2 with size 5
    var response = await _client.GetAsync("/api/v1/products?pageNumber=2&pageSize=5");

    var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductDto>>>();
    result!.Data!.Items.Should().HaveCount(5);
    result.Data.PageNumber.Should().Be(2);
}
```

### Testing Soft Deletes

```csharp
[Fact]
public async Task DeleteProduct_ExcludesFromGET()
{
    // Seed products
    var ids = await TestDataSeeder.SeedProductsAsync(_context, count: 3);

    // Delete one
    await _client.DeleteAsync($"/api/v1/products/{ids[0]}");

    // GET should exclude deleted
    var response = await _client.GetAsync("/api/v1/products");
    var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductDto>>>();
    
    result!.Data!.Items.Should().HaveCount(2);
    result.Data.Items.Should().NotContain(p => p.Id == ids[0]);
}
```

---

## 🧪 Test Traits

Organize tests with traits for filtering:

```csharp
[Collection("Integration Tests InMemory")]
[Trait("Category", "Integration")]
[Trait("Feature", "Products")]
public class GetProductsEndpointTests
{
    // Tests...
}
```

**Run by trait:**
```bash
dotnet test --filter "Feature=Products"
dotnet test --filter "Category=Integration"
```

---

## 📊 Troubleshooting

### Issue: "The database connection was not closed"

**Solution:** Ensure `DisposeAsync()` is called:
```csharp
public async Task DisposeAsync()
{
    await _factory.ResetDatabaseAsync();
}
```

### Issue: Tests interfere with each other

**Solution:** Clear data in `InitializeAsync()`:
```csharp
public async Task InitializeAsync()
{
    _context = _factory.GetDbContext();
    await TestDataSeeder.ClearAllDataAsync(_context);
}
```

### Issue: "Cannot access an instance of sealed class"

**Solution:** The in-memory database is sealed per test collection. Use a new collection if needed:
```csharp
[CollectionDefinition("My Integration Tests")]
public class MyTestCollection : ICollectionFixture<InMemoryWebApplicationFactory>
{
}

[Collection("My Integration Tests")]
public class MyTests { }
```

---

## 🎓 Learning Resources

- [xUnit Fixtures](https://xunit.net/docs/shared-context)
- [FluentAssertions](https://fluentassertions.com/)
- [EF Core In-Memory Database](https://learn.microsoft.com/en-us/ef/core/providers/in-memory/)
- [ASP.NET Core Testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)

---

## 📝 Template for New Tests

```csharp
using Archu.Contracts.Products;
using Archu.Infrastructure.Persistence;
using Archu.IntegrationTests.Fixtures;
using Archu.IntegrationTests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Archu.IntegrationTests.Api.Products;

[Collection("Integration Tests InMemory")]
[Trait("Category", "Integration")]
[Trait("Feature", "Products")]
public class YourFeatureEndpointTests : IAsyncLifetime
{
    private readonly InMemoryWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private ApplicationDbContext _context = null!;

    public YourFeatureEndpointTests(InMemoryWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        _context = _factory.GetDbContext();
        await TestDataSeeder.ClearAllDataAsync(_context);
    }

    public async Task DisposeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    [Fact]
    public async Task YourTest_WithCondition_ExpectsResult()
    {
        // Arrange
        
        // Act
        
        // Assert
    }
}
```

---

**Happy testing!** 🎉
