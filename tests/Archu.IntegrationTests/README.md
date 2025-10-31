# Archu.IntegrationTests

Integration tests for the Archu API using xUnit, WebApplicationFactory, and Testcontainers.

## Overview

This project contains comprehensive integration tests for the Archu API endpoints. Tests use real SQL Server containers (via Testcontainers), actual HTTP requests, and JWT authentication to verify end-to-end functionality.

---

## Test Framework & Tools

| Tool | Version | Purpose |
|------|---------|---------|
| **xUnit** | 2.9.3 | Test framework |
| **WebApplicationFactory** | .NET 9 | In-memory API hosting |
| **Testcontainers** | Latest | Docker container management |
| **FluentAssertions** | 7.0.0 | Fluent assertion library |
| **AutoFixture** | 4.18.1 | Test data generation |
| **AutoFixture.AutoMoq** | 4.18.1 | Auto-mocking with AutoFixture |
| **Respawn** | Latest | Database cleanup between tests |
| **Moq** | 4.20.72 | Mocking framework |

---

## Test Structure

```
Archu.IntegrationTests/
├── Api/
│   └── Products/
│       └── GetProductsEndpointTests.cs  # GET /api/v1/products tests (17 tests)
├── Fixtures/
│   ├── WebApplicationFactoryFixture.cs   # Test environment setup
│   └── InMemoryWebApplicationFactory.cs  # In-memory test factory
├── TestHelpers/
│   ├── Fixtures/
│   │   └── AutoMoqDataAttribute.cs       # AutoFixture integration
│   └── TestDataSeeder.cs     # Test data seeding
└── README.md           # This file
```

---

## Test Infrastructure

### WebApplicationFactoryFixture

The `WebApplicationFactoryFixture` provides a complete test environment:

- ✅ **SQL Server Container**: Uses Testcontainers to spin up a real SQL Server instance
- ✅ **Database Management**: Automatically applies migrations and resets database via Respawn
- ✅ **JWT Authentication**: Generates test JWT tokens with proper claims and permissions
- ✅ **Test Services**: Provides test implementations of `ICurrentUser` and `ITimeProvider`
- ✅ **HTTP Client**: Pre-configured HttpClient for API requests

### JWT Authentication in Tests

The fixture generates JWT tokens with both **role claims** and **permission claims** to satisfy the API's authorization policies.

#### Permission-Based Authorization

The API uses permission-based authorization policies. The fixture automatically adds the appropriate permission claims based on the user's role:

| Role | Permissions Granted |
|------|---------------------|
| **User** | `products:read` |
| **Manager** | `products:read`, `products:create`, `products:update` |
| **Administrator** | `products:read`, `products:create`, `products:update`, `products:delete` |

#### Generating Test Tokens

```csharp
// Generate a token for a User role (read-only permissions)
var token = await _factory.GetJwtTokenAsync("User", userId.ToString());

// Generate a token for a Manager role (read, create, update permissions)
var token = await _factory.GetJwtTokenAsync("Manager", userId.ToString());

// Generate a token for an Administrator role (all permissions)
var token = await _factory.GetJwtTokenAsync("Administrator", userId.ToString());
```

The `GetJwtTokenAsync` method automatically includes the correct permission claims based on the role, ensuring that authorization policies work correctly in tests.

---

## Current Test Coverage

### Product Endpoint Tests (17 tests)

| Test | Description | Status |
|------|-------------|--------|
| `GetProducts_ShouldReturn200OK_WithPagedProducts` | Validates successful product retrieval with pagination | ✅ |
| `GetProducts_ShouldReturnCorrectProductData` | Verifies product data integrity | ✅ |
| `GetProducts_ShouldReturn401_WhenUnauthorized` | Tests authentication requirements | ✅ |
| `GetProducts_ShouldReturnEmptyPagedResult_WhenNoProducts` | Behavior when no products exist | ✅ |
| `GetProducts_ShouldAllowAccessForAuthenticatedRoles` | Tests User, Manager, Administrator roles (Theory: 3 tests) | ✅ |
| `GetProducts_ShouldHaveCorrectContentType` | Validates `application/json` content type | ✅ |
| `GetProducts_ShouldNotIncludeSoftDeletedProducts` | Soft delete filtering verification | ✅ |
| `GetProducts_ShouldHandleConcurrentRequests` | Multiple simultaneous requests | ✅ |
| `GetProducts_ShouldIncludeTimestamp` | Response timestamp validation | ✅ |
| `GetProducts_ShouldHandleDifferentPaginationParameters` | Various page sizes and numbers (Theory: 4 tests) | ✅ |
| `GetProducts_ShouldEnforceMaxPageSize` | Page size limit enforcement | ✅ |
| `GetProducts_ShouldUseDefaultPaginationWhenNotSpecified` | Default pagination when not specified | ✅ |

**Total**: 17 tests (all passing) ✅

---

## Running Tests

### Prerequisites

- **Docker Desktop** must be running (for Testcontainers SQL Server)
- **Port 1433** must be available (or Testcontainers will assign a random port)

### Run All Tests

```bash
# From repository root
dotnet test tests/Archu.IntegrationTests

# From test project directory
cd tests/Archu.IntegrationTests
dotnet test
```

### Run Specific Test Class

```bash
dotnet test --filter "FullyQualifiedName~GetProductsEndpointTests"
```

### Run Specific Test

```bash
dotnet test --filter "GetProducts_ShouldReturn200OK_WithPagedProducts"
```

### Run with Detailed Output

```bash
dotnet test --verbosity detailed
```

---

## Test Patterns

### 1. Database Reset Between Tests

```csharp
public async Task InitializeAsync()
{
    await _factory.ResetDatabaseAsync();
}
```

### 2. JWT Token Authentication

```csharp
var token = await _factory.GetJwtTokenAsync("Manager", userId.ToString());
_client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);
```

### 3. Seeding Test Data

```csharp
await _factory.SeedProductsAsync(count: 3);
```

### 4. FluentAssertions

```csharp
response.StatusCode.Should().Be(HttpStatusCode.OK);
apiResponse.Success.Should().BeTrue();
apiResponse.Data.Should().NotBeNull();
apiResponse.Data!.Items.Should().HaveCount(3);
```

### 5. Theory Tests (Data-Driven)

```csharp
[Theory]
[InlineData("User")]
[InlineData("Manager")]
[InlineData("Administrator")]
public async Task GetProducts_ShouldAllowAccessForAuthenticatedRoles(string role)
{
    // Test implementation
}
```

---

## What's Tested

### ✅ Covered

- **Product Endpoints**
  - GET /api/v1/products (list with pagination)
  - Authentication requirements (401 when no token)
  - Role-based access (User, Manager, Administrator)
  - Empty result sets
  - Soft delete filtering
  - Concurrent requests
  - Pagination (various page sizes and numbers)
  - Default pagination behavior
  - Max page size enforcement
  - Response format (ApiResponse wrapper)
  - Timestamp inclusion

### 🚧 Planned (Not Yet Implemented)

- **Product CRUD Operations**
  - POST /api/v1/products (create)
  - PUT /api/v1/products/{id} (update)
  - DELETE /api/v1/products/{id} (delete)
  - GET /api/v1/products/{id} (get by ID)

- **Authentication Endpoints**
  - POST /api/v1/authentication/login
  - POST /api/v1/authentication/refresh-token
  - POST /api/v1/authentication/logout

- **Error Scenarios**
  - Validation errors (400)
  - Not found errors (404)
  - Concurrency conflicts (409)
  - Server errors (500)

- **Performance Tests**
- Load testing
  - Response time benchmarks
  - Database query optimization

---

## Troubleshooting

### Tests failing with 403 Forbidden

**Cause**: The test JWT token doesn't include the required permission claims.

**Solution**: Ensure you're using `GetJwtTokenAsync` with an appropriate role:

```csharp
// ❌ Wrong: No role specified (defaults to "User" with only read permissions)
var token = await _factory.GetJwtTokenAsync();

// ✅ Correct: Specify the role with appropriate permissions
var token = await _factory.GetJwtTokenAsync("Manager", userId.ToString());
```

### Tests failing with empty JSON responses

**Cause**: The API might be returning an error response that isn't JSON formatted.

**Solution**: Check the HTTP status code before deserializing:

```csharp
var response = await _client.GetAsync("/api/v1/products");

// Add this to debug
if (!response.IsSuccessStatusCode)
{
    var content = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"Error response: {content}");
}

response.StatusCode.Should().Be(HttpStatusCode.OK);
```

### Testcontainer SQL Server fails to start

**Cause**: Docker is not running or insufficient resources.

**Solution**: 
1. Ensure Docker Desktop is running
2. Check that port 1433 is not already in use
3. Increase Docker resource limits if needed
4. Check Docker logs: `docker logs <container_id>`

### Database state issues between tests

**Cause**: Database not properly reset between tests.

**Solution**: Ensure `ResetDatabaseAsync()` is called in `InitializeAsync()`:

```csharp
public async Task InitializeAsync()
{
    await _factory.ResetDatabaseAsync();
}
```

---

## Best Practices

### ✅ DO

- Use `WebApplicationFactoryFixture` via `[Collection("Integration Tests")]`
- Reset database between tests with `ResetDatabaseAsync()`
- Include proper JWT authorization headers for protected endpoints
- Test both success and error scenarios
- Use FluentAssertions for readable assertions
- Test with different user roles
- Verify response format (ApiResponse wrapper)
- Test pagination thoroughly

### ❌ DON'T

- Share state between tests
- Assume database is clean (always reset)
- Skip authorization testing
- Test only happy paths
- Use hardcoded user IDs or product IDs
- Ignore concurrent request testing
- Skip soft delete verification

---

## Recent Changes

### 2025-01-24: Updated Documentation

**Changes**:
- ✅ Added comprehensive test coverage table
- ✅ Documented all 17 tests
- ✅ Improved troubleshooting section
- ✅ Added test statistics
- ✅ Enhanced best practices

### 2025-01-22: Fixed Authorization in Integration Tests

**Issue**: Integration tests were failing with 403 Forbidden errors because test JWT tokens only contained role claims but not permission claims.

**Solution**: Updated `WebApplicationFactoryFixture.GetJwtTokenAsync()` to include permission claims based on the user's role. The method now adds appropriate `permission` claims (e.g., `products:read`, `products:create`) that match the API's permission-based authorization requirements.

**Impact**: All 17 integration tests now pass successfully. ✅

---

## Future Improvements

### Test Coverage Expansion

1. **Product CRUD Tests** (High Priority)
   - Create product endpoint
   - Update product endpoint
   - Delete product endpoint
   - Get product by ID endpoint

2. **Authentication Tests** (High Priority)
   - Login endpoint
   - Refresh token endpoint
   - Logout endpoint
   - Token expiration handling

3. **Error Scenario Tests** (Medium Priority)
   - Validation errors
   - Not found errors
   - Concurrency conflicts
   - Server errors

4. **Performance Tests** (Low Priority)
   - Load testing with NBomber
   - Response time benchmarks
   - Database query optimization

### Code Quality

1. Add mutation testing with Stryker.NET
2. Increase code coverage to 90%+
3. Add API contract testing with Pact
4. Add security testing (OWASP)

---

## Related Documentation

- 📖 **[Archu.Api README](../../src/Archu.Api/README.md)** - API project documentation
- 📖 **[Archu.AdminApi README](../../src/Archu.AdminApi/README.md)** - Admin API documentation
- 📖 **[API Guide](../../docs/API_GUIDE.md)** - Complete API reference
- 📖 **[Authentication Guide](../../docs/AUTHENTICATION_GUIDE.md)** - JWT authentication
- 📖 **[Authorization Guide](../../docs/AUTHORIZATION_GUIDE.md)** - Role-based access control
- 📖 **[Unit Tests](../Archu.UnitTests/README.md)** - Unit test documentation

---

## Statistics

| Metric | Value |
|--------|-------|
| **Total Tests** | 17 ✅ |
| **Test Classes** | 1 |
| **Passing Rate** | 100% |
| **Test Execution Time** | ~9.2 seconds (includes container startup) |
| **Framework** | xUnit 2.9.3 + WebApplicationFactory |
| **Database** | SQL Server 2022 (via Testcontainers) |
| **Target Framework** | .NET 9 |

---

**Last Updated**: 2025-01-24  
**Maintainer**: Archu Development Team  
**Status**: Active Development  
**Test Count**: 17 tests (all passing)  
**Coverage**: Product GET endpoint fully tested
