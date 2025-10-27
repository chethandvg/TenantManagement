# Archu Integration Tests

## Overview

This project contains integration tests for the Archu API using xUnit, WebApplicationFactory, and Testcontainers.

## Test Infrastructure

### WebApplicationFactoryFixture

The `WebApplicationFactoryFixture` class provides a complete test environment for integration tests:

- **SQL Server Container**: Uses Testcontainers to spin up a real SQL Server instance for testing
- **Database Management**: Automatically applies migrations and provides database reset functionality via Respawn
- **JWT Authentication**: Generates test JWT tokens with proper claims and permissions
- **Test Services**: Provides test implementations of `ICurrentUser` and `ITimeProvider`

### Authentication & Authorization in Tests

The integration tests use JWT tokens that include both **role claims** and **permission claims** to satisfy the API's authorization policies.

#### Permission-Based Authorization

The API uses permission-based authorization policies that check for specific permission claims in the JWT token. The `WebApplicationFactoryFixture` automatically adds the appropriate permission claims based on the user's role:

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

## Running Tests

### Run all integration tests

```bash
dotnet test tests\Archu.IntegrationTests
```

### Run specific test class

```bash
dotnet test tests\Archu.IntegrationTests --filter "FullyQualifiedName~GetProductsEndpointTests"
```

### Run specific test method

```bash
dotnet test tests\Archu.IntegrationTests --filter "GetProducts_ShouldReturn200OK_WithPagedProducts"
```

## Test Structure

### Basic Tests (Initial 3 Tests)

These tests verify the core functionality:

1. **GetProducts_ShouldReturn200OK_WithPagedProducts** - Validates successful product retrieval with pagination
2. **GetProducts_ShouldReturnCorrectProductData** - Verifies product data integrity
3. **GetProducts_ShouldReturn401_WhenUnauthorized** - Tests authentication requirements

### Extended Test Coverage

Additional tests cover:

- **Empty result sets** - Behavior when no products exist
- **Role-based access** - Different permissions for User, Manager, Administrator
- **Content type validation** - Proper HTTP headers
- **Soft delete filtering** - Deleted products are excluded
- **Concurrent requests** - Multiple simultaneous requests
- **Pagination parameters** - Various page sizes and page numbers
- **Default values** - Default pagination when not specified
- **Max page size enforcement** - Page size limits

## Troubleshooting

### Tests failing with 403 Forbidden

**Cause**: The test JWT token doesn't include the required permission claims.

**Solution**: Ensure you're using `GetJwtTokenAsync` with an appropriate role that has the required permissions for the endpoint you're testing.

```csharp
// ❌ Wrong: No role specified (defaults to "User" with only read permissions)
var token = await _factory.GetJwtTokenAsync();

// ✅ Correct: Specify the role with appropriate permissions
var token = await _factory.GetJwtTokenAsync("Manager", userId.ToString());
```

### Tests failing with empty JSON responses

**Cause**: The API might be returning an error response that isn't JSON formatted.

**Solution**: Check the HTTP status code before deserializing the response:

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

## Best Practices

1. **Use the WebApplicationFactoryFixture**: All integration tests should use the `WebApplicationFactoryFixture` via the `[Collection("Integration Tests")]` attribute
2. **Reset database between tests**: Call `await _factory.ResetDatabaseAsync()` in `InitializeAsync()` to ensure clean state
3. **Include proper authorization**: Always set the Authorization header with a valid JWT token for protected endpoints
4. **Test both success and error cases**: Include tests for unauthorized access, invalid input, and edge cases
5. **Use FluentAssertions**: For better test readability and error messages

## Recent Fixes

### 2025-01-22: Fixed Authorization in Integration Tests

**Issue**: Integration tests were failing with 403 Forbidden errors because the test JWT tokens only contained role claims but not permission claims required by the authorization policies.

**Solution**: Updated `WebApplicationFactoryFixture.GetJwtTokenAsync()` to include permission claims based on the user's role. The method now adds appropriate `permission` claims (e.g., `products:read`, `products:create`) that match the API's permission-based authorization requirements.

**Impact**: All 17 integration tests now pass successfully. ✅

---

**Last Updated**: 2025-01-22  
**Test Count**: 17 tests (all passing)  
**Framework**: xUnit with WebApplicationFactory and Testcontainers  
**Database**: SQL Server 2022 (via Docker)
