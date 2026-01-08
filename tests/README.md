# TentMan Backend Testing

Comprehensive testing suite for the "Get Products" endpoint covering Unit Tests, Integration Tests, and API Client Tests.

## 📋 Test Projects

| Project | Type | Description | Test Count |
|---------|------|-------------|------------|
| **TentMan.UnitTests** | Unit Tests | Tests business logic in Application layer | 9 tests |
| **TentMan.IntegrationTests** | Integration Tests | Tests full API endpoints with database | 10 tests |
| **TentMan.ApiClient.Tests** | API Client Tests | Tests HTTP client behavior | 9 tests |

**Total**: 28 tests covering the Get Products functionality

---

## 🚀 Quick Start

### Prerequisites

- .NET 9 SDK installed
- Docker Desktop running (for Testcontainers in Integration Tests)
- Visual Studio 2022 or JetBrains Rider (or VS Code with C# Dev Kit)

### Run All Tests

```bash
# From solution root
dotnet test

# With detailed output
dotnet test --logger "console;verbosity=detailed"

# Generate code coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

### Run Specific Test Project

```bash
# Unit Tests only
dotnet test tests/TentMan.UnitTests

# Integration Tests only (requires Docker)
dotnet test tests/TentMan.IntegrationTests

# API Client Tests only
dotnet test tests/TentMan.ApiClient.Tests
```

### Run in Visual Studio

1. Open `TentMan.sln`
2. Open **Test Explorer** (Test → Test Explorer)
3. Click **Run All Tests** or right-click specific tests

---

## 📦 Test Coverage

### Unit Tests (`TentMan.UnitTests`)

**Target**: `GetProductsQueryHandler` in Application layer

**Coverage**:
- ✅ Returns all products when they exist
- ✅ Returns empty list when no products exist
- ✅ Maps entity properties to DTOs correctly
- ✅ Respects cancellation tokens
- ✅ Logs information messages
- ✅ Handles different product counts (0, 1, 5, 100)
- ✅ Handles empty RowVersion
- ✅ Calls repository exactly once

**Technology Stack**:
- xUnit for test framework
- Moq for mocking dependencies
- FluentAssertions for readable assertions
- AutoFixture for test data generation

**Example Test**:
```csharp
[Fact]
public async Task Handle_ShouldReturnAllProducts_WhenProductsExist()
{
    // Arrange
    var products = new List<Product> { /* test data */ };
    _mockRepository
        .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(products);

    // Act
    var result = await _handler.Handle(new GetProductsQuery(), CancellationToken.None);

    // Assert
    result.Should().HaveCount(3);
}
```

---

### Integration Tests (`TentMan.IntegrationTests`)

**Target**: Full HTTP request/response cycle through the Products API

**Coverage**:
- ✅ Returns 200 OK with all products
- ✅ Returns correct product data with proper mapping
- ✅ Returns 401 Unauthorized without authentication
- ✅ Returns empty list when no products exist
- ✅ Allows access for authenticated roles (User, Manager, Administrator)
- ✅ Returns correct Content-Type header
- ✅ Excludes soft-deleted products
- ✅ Handles concurrent requests safely
- ✅ Includes timestamp in response
- ✅ Returns proper API response format

**Technology Stack**:
- WebApplicationFactory for in-memory API hosting
- Testcontainers for real SQL Server database
- Respawn for database cleanup between tests
- JWT token generation for authentication
- FluentAssertions for assertions

**Database Strategy**:
- Real SQL Server 2022 container via Testcontainers
- Automatic schema creation with EF Core migrations
- Database reset between tests using Respawn
- Isolated test data per test method

**Example Test**:
```csharp
[Fact]
public async Task GetProducts_ShouldReturn200OK_WithAllProducts()
{
    // Arrange
    var token = await _factory.GetJwtTokenAsync("User");
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await _client.GetAsync("/api/v1/products");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<ProductDto>>>();
    apiResponse!.Data.Should().HaveCount(3);
}
```

---

### API Client Tests (`TentMan.ApiClient.Tests`)

**Target**: `ProductsApiClient` HTTP client implementation

**Coverage**:
- ✅ Returns success when API returns products
- ✅ Uses default pagination parameters
- ✅ Sends correct query parameters
- ✅ Throws ResourceNotFoundException on 404
- ✅ Throws AuthorizationException on 401
- ✅ Throws ServerException on 500
- ✅ Respects cancellation tokens
- ✅ Handles empty responses
- ✅ Deserializes PagedResult correctly

**Technology Stack**:
- RichardSzalay.MockHttp for mocking HTTP responses
- Moq for logger mocking
- FluentAssertions for assertions

**Example Test**:
```csharp
[Fact]
public async Task GetProductsAsync_ShouldThrowAuthorizationException_When401()
{
    // Arrange
    _mockHttp
        .When("https://api.test.com/api/products?pageNumber=1&pageSize=10")
        .Respond(HttpStatusCode.Unauthorized);

    // Act & Assert
    Func<Task> act = async () => await _apiClient.GetProductsAsync();
    await act.Should().ThrowAsync<AuthorizationException>()
        .Where(ex => ex.StatusCode == 401);
}
```

---

## 🏗️ Test Architecture

### Clean Architecture Alignment

```
┌─────────────────────────────────────┐
│  TentMan.ApiClient.Tests              │  HTTP Client Layer
│  (Mock HTTP responses)              │
└─────────────────┬───────────────────┘
                  │
┌─────────────────▼───────────────────┐
│  TentMan.IntegrationTests             │  API Layer + Database
│  (WebApplicationFactory +           │
│   SQL Server Container)             │
└─────────────────┬───────────────────┘
                  │
┌─────────────────▼───────────────────┐
│  TentMan.UnitTests                    │  Application Layer
│  (Mocked Repositories)              │
└─────────────────────────────────────┘
```

### Test Data Management

**Unit Tests**:
- In-memory test data
- No database required
- Fast execution (< 1 second total)

**Integration Tests**:
- Real SQL Server database in Docker container
- Database reset between tests using Respawn
- Seeded test data via EF Core
- Test isolation via collection fixtures

**API Client Tests**:
- Mocked HTTP responses
- No external dependencies
- Fast execution

---

## 🔧 Configuration

### Integration Tests Configuration

The `WebApplicationFactoryFixture` automatically configures:
- SQL Server 2022 container via Testcontainers
- Test JWT token generation with 1-hour expiration
- Database schema creation via EF Core migrations
- Connection string injection

**Environment Variables** (optional):
```bash
# None required - all configuration is automatic
```

### Test JWT Tokens

Test tokens are generated with:
- **Secret**: `ThisIsATestSecretKeyForJWTTokenGenerationWithAtLeast32Characters`
- **Issuer**: `TestIssuer`
- **Audience**: `TestAudience`
- **Expiration**: 1 hour
- **Roles**: Configurable (User, Manager, Administrator)

---

## 📊 Test Results

### Expected Output

```bash
$ dotnet test

Test run for TentMan.UnitTests.dll (.NET 9.0)
Test run for TentMan.IntegrationTests.dll (.NET 9.0)
Test run for TentMan.ApiClient.Tests.dll (.NET 9.0)

Total tests: 28
     Passed: 28
     Failed: 0
    Skipped: 0

Time: 25.4567s
```

### Code Coverage

Run with coverage:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

**Expected Coverage**:
- `GetProductsQueryHandler`: 100%
- `ProductsApiClient.GetProductsAsync`: 100%
- `ProductsController.GetProducts`: ~95% (authentication paths)

---

## 🐛 Troubleshooting

### Issue: Integration tests fail with "Docker not running"

**Solution**: Ensure Docker Desktop is running before executing integration tests.

```bash
# Check Docker status
docker info

# Start Docker Desktop, then re-run tests
dotnet test tests/TentMan.IntegrationTests
```

### Issue: Tests fail with "Connection refused"

**Solution**: Testcontainers may need more time to start SQL Server.

```bash
# Clean and rebuild
dotnet clean
dotnet build
dotnet test
```

### Issue: Unit tests fail with "Mock not configured"

**Solution**: Verify all repository methods are properly mocked:

```csharp
_mockRepository
    .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
    .ReturnsAsync(testProducts);
```

### Issue: API Client tests fail with serialization errors

**Solution**: Ensure test data matches the API response format:

```csharp
var apiResponse = ApiResponse<PagedResult<ProductDto>>.Ok(pagedResult);
var json = JsonSerializer.Serialize(apiResponse);
```

---

## 🎯 Best Practices

### Test Naming Convention

```csharp
[MethodName]_Should[ExpectedBehavior]_When[Condition]
```

Examples:
- `Handle_ShouldReturnAllProducts_WhenProductsExist`
- `GetProducts_ShouldReturn401_WhenUnauthorized`
- `GetProductsAsync_ShouldThrowServerException_When500`

### Arrange-Act-Assert Pattern

All tests follow the AAA pattern:
```csharp
[Fact]
public async Task TestMethod()
{
    // Arrange - Set up test data and dependencies
    var testData = CreateTestData();
    
    // Act - Execute the method under test
    var result = await ExecuteMethod(testData);
    
    // Assert - Verify the outcome
    result.Should().BeExpected();
}
```

### Test Independence

- Each test is independent and can run in isolation
- Database is reset between integration tests
- No shared mutable state between tests
- Tests can run in parallel (unit and API client tests)

---

## 📈 Extending the Tests

### Adding New Test Cases

**Unit Tests**:
1. Add new test method in `GetProductsQueryHandlerTests.cs`
2. Mock repository behavior
3. Execute handler
4. Assert result

**Integration Tests**:
1. Add new test method in `GetProductsEndpointTests.cs`
2. Seed database with test data
3. Make HTTP request
4. Assert response

**API Client Tests**:
1. Add new test method in `ProductsApiClientTests.cs`
2. Configure mock HTTP response
3. Call API client method
4. Assert behavior

### Testing Other Endpoints

To test other endpoints (e.g., Create Product, Update Product):

1. Create new test files following the naming pattern
2. Reuse the `WebApplicationFactoryFixture` for integration tests
3. Follow the same test structure and conventions

---

## 🔗 Related Documentation

- [Clean Architecture Guide](../../docs/ARCHITECTURE.md)
- [API Documentation](../../src/TentMan.Api/README.md)
- [API Client Documentation](../../src/TentMan.ApiClient/README.md)

---

## 📝 Test Statistics

| Metric | Value |
|--------|-------|
| **Total Tests** | 28 |
| **Unit Tests** | 9 |
| **Integration Tests** | 10 |
| **API Client Tests** | 9 |
| **Execution Time** | ~25 seconds (including container startup) |
| **Code Coverage** | > 95% for tested components |

---

## ✅ Continuous Integration

### GitHub Actions Workflow

Add to `.github/workflows/test.yml`:

```yaml
name: Backend Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Run Unit Tests
      run: dotnet test tests/TentMan.UnitTests --no-build --verbosity normal
    
    - name: Run Integration Tests
      run: dotnet test tests/TentMan.IntegrationTests --no-build --verbosity normal
    
    - name: Run API Client Tests
      run: dotnet test tests/TentMan.ApiClient.Tests --no-build --verbosity normal
    
    - name: Generate Coverage Report
      run: dotnet test /p:CollectCoverage=true /p:CoverletOutput=./coverage/ /p:CoverletOutputFormat=lcov
    
    - name: Upload Coverage
      uses: codecov/codecov-action@v4
      with:
        files: ./coverage/coverage.info
```

---

**Last Updated**: 2025-01-23  
**Version**: 1.0  
**Maintainer**: TentMan Development Team
