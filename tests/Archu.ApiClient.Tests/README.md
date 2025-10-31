# Archu.ApiClient.Tests

Unit tests for the Archu.ApiClient library using xUnit, Moq, and MockHttp.

## Overview

This project contains comprehensive unit tests for the `Archu.ApiClient` HTTP client library. The tests verify the behavior of API clients, exception handling, resilience policies, and authentication framework.

---

## Test Framework & Tools

| Tool | Version | Purpose |
|------|---------|---------|
| **xUnit** | 2.9.3 | Test framework |
| **Moq** | 4.20.72 | Mocking framework |
| **MockHttp** | 7.0.0 | HTTP message handler mocking |
| **FluentAssertions** | 7.0.0 | Fluent assertion library |
| **AutoFixture** | 4.18.1 | Test data generation |
| **AutoFixture.AutoMoq** | 4.18.1 | Auto-mocking with AutoFixture |
| **Coverlet** | 6.0.3 | Code coverage collection |

---

## Test Structure

```
Archu.ApiClient.Tests/
├── Services/
│   └── ProductsApiClientTests.cs      # ProductsApiClient unit tests (11 tests)
├── TestHelpers/
│   ├── Fixtures/
│   │   └── AutoMoqDataAttribute.cs    # AutoFixture + AutoMoq integration
│   └── MockHttpMessageHandlerFactory.cs # Mock HTTP handler factory
└── README.md      # This file
```

---

## Current Test Coverage

### ProductsApiClient Tests (11 tests)

| Test | Description | Status |
|------|-------------|--------|
| `GetProductsAsync_ShouldReturnSuccess_WhenApiReturnsProducts` | Verifies successful product retrieval | ✅ |
| `GetProductsAsync_ShouldUseDefaultPagination_WhenNoParametersProvided` | Tests default pagination (page 1, size 10) | ✅ |
| `GetProductsAsync_ShouldSendCorrectQueryParameters` | Tests various pagination parameters (Theory) | ✅ |
| `GetProductsAsync_ShouldReturnFailedResponse_When404` | Tests 404 ResourceNotFoundException handling | ✅ |
| `GetProductsAsync_ShouldReturnFailedResponse_When401` | Tests 401 AuthorizationException handling | ✅ |
| `GetProductsAsync_ShouldReturnFailedResponse_When500` | Tests 500 ServerException handling | ✅ |
| `GetProductsAsync_ShouldHandleRequestCancellation` | Tests cancellation token support | ✅ |
| `GetProductsAsync_ShouldHandleEmptyResponse` | Tests empty product list handling | ✅ |
| `GetProductsAsync_ShouldDeserializePagedResultCorrectly` | Tests PagedResult deserialization | ✅ |

**Total**: 11 tests (all passing) ✅

---

## Running Tests

### Run All Tests

```bash
# From repository root
dotnet test tests/Archu.ApiClient.Tests

# From test project directory
cd tests/Archu.ApiClient.Tests
dotnet test
```

### Run Specific Test Class

```bash
dotnet test --filter "FullyQualifiedName~ProductsApiClientTests"
```

### Run Specific Test

```bash
dotnet test --filter "GetProductsAsync_ShouldReturnSuccess_WhenApiReturnsProducts"
```

### Run with Code Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## Code Coverage Configuration

The project is configured with **80% coverage threshold** for both line and branch coverage:

```xml
<PropertyGroup>
  <CollectCoverage>true</CollectCoverage>
  <CoverletOutputFormat>opencover,cobertura</CoverletOutputFormat>
  <CoverletOutput>./TestResults/</CoverletOutput>
  <Threshold>80</Threshold>
  <ThresholdType>line,branch</ThresholdType>
</PropertyGroup>
```

---

## Test Patterns

### 1. Mock HTTP Responses

```csharp
_mockHttp
    .When("https://api.test.com/api/v1/products?pageNumber=1&pageSize=10")
    .Respond("application/json", JsonSerializer.Serialize(apiResponse));
```

### 2. Exception Handling Tests

```csharp
_mockHttp
    .When("https://api.test.com/api/v1/products?pageNumber=1&pageSize=10")
  .Respond(HttpStatusCode.NotFound, "application/json", 
        "{\"success\":false,\"message\":\"Not found\"}");

await Assert.ThrowsAsync<ResourceNotFoundException>(
    async () => await _apiClient.GetProductsAsync());
```

### 3. FluentAssertions

```csharp
result.Should().NotBeNull();
result.Success.Should().BeTrue();
result.Data.Should().NotBeNull();
result.Data!.Items.Should().HaveCount(2);
```

### 4. Theory Tests (Data-Driven)

```csharp
[Theory]
[InlineData(1, 10)]
[InlineData(2, 20)]
[InlineData(5, 50)]
public async Task GetProductsAsync_ShouldSendCorrectQueryParameters(
    int pageNumber, int pageSize)
{
    // Test implementation
}
```

---

## Test Helpers

### AutoMoqDataAttribute

Combines AutoFixture with AutoMoq for automatic test data generation and mocking:

```csharp
[Theory, AutoMoqData]
public async Task MyTest(
    [Frozen] Mock<ILogger<ProductsApiClient>> mockLogger,
    ProductDto product)
{
    // mockLogger and product are automatically generated
}
```

### MockHttpMessageHandlerFactory

Factory for creating configured MockHttpMessageHandler instances:

```csharp
var mockHttp = MockHttpMessageHandlerFactory.Create();
var httpClient = mockHttp.ToHttpClient();
httpClient.BaseAddress = new Uri("https://api.test.com");
```

---

## What's Tested

### ✅ Covered

- **ProductsApiClient**
  - GET products with pagination
  - Default pagination behavior
  - Query parameter construction
  - HTTP status code handling (200, 401, 404, 500)
  - Exception mapping (ResourceNotFoundException, AuthorizationException, ServerException)
  - Request cancellation
  - Empty response handling
  - PagedResult deserialization

### 🚧 Planned (Not Yet Implemented)

- **AuthenticationApiClient**
  - Login endpoint
  - Refresh token endpoint
  - Token validation
- **Authentication Framework**
  - TokenManager tests
  - AuthenticationService tests
  - Token storage tests (InMemory, BrowserLocal)
  - AuthenticationMessageHandler tests
  - ApiAuthenticationStateProvider tests
- **Resilience Policies**
  - Retry policy behavior
  - Circuit breaker behavior
  - Exponential backoff verification
- **Configuration**
  - ApiClientOptions validation
  - AuthenticationOptions validation

---

## Known Issues

### ⚠️ CA1063 Warnings

**Issue**: Dispose pattern warnings for `ProductsApiClientTests`:

```
warning CA1063: Provide an overridable implementation of Dispose(bool)
warning CA1063: Modify 'ProductsApiClientTests.Dispose' so that it calls Dispose(true)
```

**Impact**: Low - Tests work correctly, but disposal pattern could be improved.

**Resolution**: Mark class as `sealed` or implement full dispose pattern with `Dispose(bool disposing)`.

---

## Best Practices

### ✅ DO

- Use `MockHttpMessageHandler` for HTTP client testing
- Use FluentAssertions for readable test assertions
- Test both success and error scenarios
- Test exception handling explicitly
- Use `Theory` tests for data-driven scenarios
- Clean up resources in `Dispose()`
- Verify HTTP request details (URL, query parameters, headers)

### ❌ DON'T

- Mock `HttpClient` directly (use `MockHttpMessageHandler` instead)
- Test implementation details (test behavior, not internals)
- Share state between tests (use fixtures or reinitialize)
- Ignore exception scenarios
- Skip cancellation token testing

---

## Future Improvements

### Test Coverage Expansion

1. **Authentication Framework Tests** (High Priority)
   - `TokenManager` unit tests
   - `AuthenticationService` unit tests
   - Token storage implementations
   - JWT token parsing and validation
   - Token refresh logic

2. **Resilience Policy Tests** (Medium Priority)
   - Retry policy with exponential backoff
   - Circuit breaker open/close/half-open states
   - Timeout handling
   - Logging verification

3. **Configuration Tests** (Medium Priority)
   - Options validation
   - Default value verification
   - Invalid configuration handling

4. **Additional API Clients** (Low Priority)
   - `AuthenticationApiClient` tests
   - Future API clients (Orders, Customers, etc.)

### Code Quality

1. Fix CA1063 warnings by marking test classes as `sealed`
2. Increase code coverage to 90%+
3. Add mutation testing with Stryker.NET
4. Add performance benchmarks with BenchmarkDotNet

---

## Related Documentation

- 📖 **[Archu.ApiClient README](../../src/Archu.ApiClient/README.md)** - Client library documentation
- 📖 **[Resilience Guide](../../src/Archu.ApiClient/RESILIENCE.md)** - Resilience patterns
- 📖 **[Authentication Framework](../../src/Archu.ApiClient/Authentication/README.md)** - Authentication guide
- 📖 **[Integration Tests](../Archu.IntegrationTests/README.md)** - API integration tests

---

## Statistics

| Metric | Value |
|--------|-------|
| **Total Tests** | 11 ✅ |
| **Test Classes** | 1 |
| **Passing Rate** | 100% |
| **Code Coverage** | Target: 80% (line + branch) |
| **Test Execution Time** | ~7 seconds |
| **Framework** | xUnit 2.9.3 |
| **Target Framework** | .NET 9 |

---

**Last Updated**: 2025-01-24  
**Maintainer**: Archu Development Team  
**Status**: Active Development  
**Test Count**: 11 tests (all passing)

