# TentMan.AdminApiClient

A .NET 9 HTTP client library for interacting with the TentMan Admin API using the HttpClientFactory pattern and Polly for resilience.

## Features

- ✅ Clean Architecture with separation of concerns
- ✅ HttpClientFactory pattern for efficient HTTP client management
- ✅ **Advanced Resilience Policies** - Configurable retry and circuit breaker with Polly
- ✅ **Comprehensive Logging** - Structured logging for all operations and errors
- ✅ **JWT Authentication Support** - Designed to work with bearer token authentication
- ✅ Strongly-typed API clients
- ✅ Configuration-based setup
- ✅ Comprehensive exception handling with custom exceptions
- ✅ Full nullable reference type support

## Overview

The Admin API Client provides programmatic access to administrative operations in the TentMan system:

- **System Initialization** - Bootstrap the application with default roles and super admin user
- **User Management** - Create, list, and delete users
- **Role Management** - Create and manage system roles
- **User-Role Assignment** - Assign and remove roles from users

## Installation

Add a project reference to your application:

```xml
<ProjectReference Include="..\TentMan.AdminApiClient\TentMan.AdminApiClient.csproj" />
```

## Configuration

Add the following configuration to your `appsettings.json`:

```json
{
  "AdminApiClient": {
    "BaseUrl": "https://localhost:7002",
    "TimeoutSeconds": 30,
    "RetryCount": 3,
    "ApiVersion": "v1",
    "EnableDetailedLogging": false,
    "CircuitBreakerFailureThreshold": 5,
    "CircuitBreakerDurationSeconds": 30,
    "RetryBaseDelaySeconds": 1.0,
    "EnableCircuitBreaker": true,
    "EnableRetryPolicy": true
  }
}
```

## Quick Start

### 1. Register the Admin API Client in Dependency Injection

```csharp
using TentMan.AdminApiClient.Extensions;

// Using configuration (recommended)
builder.Services.AddAdminApiClient(builder.Configuration);

// Or with custom options
builder.Services.AddAdminApiClient(options =>
{
    options.BaseUrl = "https://admin-api.example.com";
    options.TimeoutSeconds = 60;
    options.RetryCount = 5;
});
```

### 2. Use the Admin API Clients

```csharp
@inject IInitializationApiClient InitializationClient
@inject IRolesApiClient RolesClient
@inject IUsersApiClient UsersClient
@inject IUserRolesApiClient UserRolesClient

// Initialize the system (first-time setup only)
var initRequest = new InitializeSystemRequest
{
    UserName = "superadmin",
    Email = "admin@example.com",
    Password = "SecurePassword123!"
};
var initResult = await InitializationClient.InitializeSystemAsync(initRequest);

// Get all roles
var rolesResponse = await RolesClient.GetRolesAsync();

// Create a new user
var createUserRequest = new CreateUserRequest
{
    UserName = "john.doe",
    Email = "john.doe@example.com",
    Password = "SecurePassword123!",
    EmailConfirmed = true
};
var userResponse = await UsersClient.CreateUserAsync(createUserRequest);

// Assign role to user
var assignRoleRequest = new AssignRoleRequest
{
    UserId = userResponse.Data!.Id,
    RoleId = managerRoleId
};
await UserRolesClient.AssignRoleAsync(assignRoleRequest);
```

## 🔐 Authentication

Most Admin API endpoints require authentication with a JWT bearer token. You need to:

1. **Authenticate with the main API** to get a JWT token (use `TentMan.ApiClient` for this)
2. **Include the token** in the `Authorization` header as `Bearer {token}`

### Adding Authentication to Admin API Client

The Admin API Client doesn't include built-in authentication (unlike TentMan.ApiClient) because admin operations typically require a previously authenticated session. You can add authentication by:

**Option 1: Manual Token Injection**

```csharp
// Get token from your authentication service
var token = await authService.GetAccessTokenAsync();

// Manually add to HTTP client (if needed for specific scenarios)
var httpClient = httpClientFactory.CreateClient("AdminApiClient");
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);
```

**Option 2: Use DelegatingHandler** (recommended)

Create a custom message handler to automatically attach tokens:

```csharp
public class AdminAuthenticationHandler : DelegatingHandler
{
    private readonly ITokenManager _tokenManager;

    public AdminAuthenticationHandler(ITokenManager tokenManager)
    {
        _tokenManager = tokenManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _tokenManager.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

// Register in DI
services.AddTransient<AdminAuthenticationHandler>();
services.AddAdminApiClient(builder.Configuration);

// Add handler to HTTP clients
services.AddHttpClient<IUsersApiClient, UsersApiClient>()
    .AddHttpMessageHandler<AdminAuthenticationHandler>();
```

## API Clients

### IInitializationApiClient

System initialization and setup operations.

```csharp
public interface IInitializationApiClient
{
    Task<ApiResponse<InitializationResult>> InitializeSystemAsync(
        InitializeSystemRequest request,
        CancellationToken cancellationToken = default);
}
```

**Example:**

```csharp
var request = new InitializeSystemRequest
{
    UserName = "superadmin",
    Email = "admin@yourcompany.com",
    Password = "YourSecurePassword123!"
};

var result = await initializationClient.InitializeSystemAsync(request);

if (result.Success)
{
    Console.WriteLine($"Initialized with {result.Data!.RolesCount} roles");
    Console.WriteLine($"Super admin user ID: {result.Data.UserId}");
}
```

### IRolesApiClient

Role management operations.

```csharp
public interface IRolesApiClient
{
    Task<ApiResponse<IEnumerable<RoleDto>>> GetRolesAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<RoleDto>> CreateRoleAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default);
}
```

**Example:**

```csharp
// Get all roles
var rolesResponse = await rolesClient.GetRolesAsync();
foreach (var role in rolesResponse.Data ?? Enumerable.Empty<RoleDto>())
{
    Console.WriteLine($"Role: {role.Name} - {role.Description}");
}

// Create a custom role
var createRoleRequest = new CreateRoleRequest
{
    Name = "ContentEditor",
    Description = "Can edit content but not manage users"
};

var roleResponse = await rolesClient.CreateRoleAsync(createRoleRequest);
if (roleResponse.Success)
{
    Console.WriteLine($"Created role with ID: {roleResponse.Data!.Id}");
}
```

### IUsersApiClient

User management operations.

```csharp
public interface IUsersApiClient
{
    Task<ApiResponse<IEnumerable<UserDto>>> GetUsersAsync(
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<UserDto>> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> DeleteUserAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
```

**Example:**

```csharp
// Get users with pagination
var usersResponse = await usersClient.GetUsersAsync(pageNumber: 1, pageSize: 20);
foreach (var user in usersResponse.Data ?? Enumerable.Empty<UserDto>())
{
    Console.WriteLine($"User: {user.UserName} ({user.Email})");
    Console.WriteLine($"  Roles: {string.Join(", ", user.Roles)}");
}

// Create a new user
var createUserRequest = new CreateUserRequest
{
    UserName = "jane.smith",
    Email = "jane.smith@example.com",
    Password = "SecurePassword123!",
    PhoneNumber = "+1234567890",
    EmailConfirmed = true,
    TwoFactorEnabled = false
};

var userResponse = await usersClient.CreateUserAsync(createUserRequest);

// Delete a user
var deleteResponse = await usersClient.DeleteUserAsync(userId);
if (deleteResponse.Success)
{
    Console.WriteLine("User deleted successfully");
}
```

### IUserRolesApiClient

User-role assignment management operations.

```csharp
public interface IUserRolesApiClient
{
    Task<ApiResponse<IEnumerable<RoleDto>>> GetUserRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> AssignRoleAsync(
        AssignRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> RemoveRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default);
}
```

**Example:**

```csharp
// Get user's current roles
var userRolesResponse = await userRolesClient.GetUserRolesAsync(userId);
Console.WriteLine($"User has {userRolesResponse.Data?.Count() ?? 0} roles");

// Assign a role to a user
var assignRequest = new AssignRoleRequest
{
    UserId = userId,
    RoleId = managerRoleId
};

var assignResponse = await userRolesClient.AssignRoleAsync(assignRequest);
if (assignResponse.Success)
{
    Console.WriteLine("Role assigned successfully");
}

// Remove a role from a user
var removeResponse = await userRolesClient.RemoveRoleAsync(userId, managerRoleId);
if (removeResponse.Success)
{
    Console.WriteLine("Role removed successfully");
}
```

## 🛡️ Resilience & Error Handling

The Admin API client includes comprehensive error handling and automatic retry policies.

### Resilience Features

- ✅ **Automatic Retry with Exponential Backoff** - Smart retry for transient failures
- ✅ **Circuit Breaker Pattern** - Prevents cascading failures with automatic recovery
- ✅ **Configurable Policies** - Customize retry count, delays, and thresholds
- ✅ **Structured Logging** - Complete visibility into request lifecycle and failures

### Configuration Example

```csharp
builder.Services.AddAdminApiClient(options =>
{
    options.BaseUrl = "https://admin-api.example.com";
    options.RetryCount = 3;  // Retry up to 3 times
    options.RetryBaseDelaySeconds = 1.0;  // Exponential backoff starting at 1s
    options.CircuitBreakerFailureThreshold = 5;  // Open after 5 failures
    options.CircuitBreakerDurationSeconds = 30;  // Stay open for 30s
    options.EnableDetailedLogging = true;  // Enable verbose logging
});
```

## Exception Handling

The library provides structured exception handling with custom exception types (shared with `TentMan.ApiClient`):

### Exception Types

| Exception | Status Code | Description | Retryable |
|-----------|-------------|-------------|-----------|
| `ResourceNotFoundException` | 404 | Resource not found | No |
| `ValidationException` | 400, 422 | Validation error | No |
| `AuthorizationException` | 401, 403 | Authentication/Authorization failure | No |
| `ServerException` | 5xx | Server-side error | Yes (except 501) |
| `NetworkException` | N/A | Network connectivity issue | Yes |
| `ApiClientException` | Other | Base exception for all API errors | Depends |

### Handling Exceptions

```csharp
using TentMan.ApiClient.Exceptions;

try
{
    var response = await usersClient.CreateUserAsync(createRequest);
    // Handle success
}
catch (ValidationException ex)
{
    // Handle 400/422 - validation errors
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"Validation error: {error}");
    }
}
catch (AuthorizationException ex)
{
    // Handle 401/403 - auth errors
    if (ex.StatusCode == 401)
    {
        // Re-authenticate
    }
    else
    {
        // Show access denied
    }
}
catch (ServerException ex)
{
    // Handle 5xx - server errors
    Console.WriteLine("Server error. Please try again later.");
}
catch (ApiClientException ex)
{
    // Handle other API errors
    Console.WriteLine($"API error: {ex.Message}");
}
```

## Architecture

### Project Structure

```
TentMan.AdminApiClient/
├── Configuration/
│   └── AdminApiClientOptions.cs       # Configuration options
├── Extensions/
│   └── ServiceCollectionExtensions.cs # DI registration
├── Services/
│   ├── AdminApiClientServiceBase.cs   # Base HTTP client
│   ├── IInitializationApiClient.cs    # Initialization API interface
│   ├── InitializationApiClient.cs     # Initialization API implementation
│   ├── IRolesApiClient.cs             # Roles API interface
│   ├── RolesApiClient.cs              # Roles API implementation
│   ├── IUsersApiClient.cs             # Users API interface
│   ├── UsersApiClient.cs              # Users API implementation
│   ├── IUserRolesApiClient.cs         # UserRoles API interface
│   └── UserRolesApiClient.cs          # UserRoles API implementation
└── README.md
```

### Base Service Features

The `AdminApiClientServiceBase` provides:
- Generic GET, POST, PUT, DELETE operations
- Automatic response deserialization
- Comprehensive exception handling with custom exceptions
- ApiResponse wrapper support
- JSON serialization configuration
- Structured error parsing

### Resilience Policies

The library includes Polly policies for:
- **Retry Policy**: Exponential backoff with configurable retry count and logging
- **Circuit Breaker**: Opens after N consecutive failures, breaks for specified duration with logging

## Best Practices

1. **Always use interfaces** for dependency injection
2. **Handle ApiResponse properly** - check the Success property before accessing Data
3. **Use specific exception types** - catch specific exceptions before generic ones
4. **Use CancellationToken** to support request cancellation
5. **Configure appropriate timeouts** based on your API's characteristics
6. **Monitor circuit breaker status** in production environments
7. **Log exceptions with context** for better troubleshooting
8. **Secure admin operations** - Always require authentication for admin endpoints
9. **Validate permissions** - Ensure users have appropriate roles before calling admin endpoints
10. **Use structured logging** to capture important context

## Security Considerations

### Admin API Access

- ⚠️ **Always require authentication** for admin operations (except initialization)
- ⚠️ **Use strong JWT tokens** with appropriate expiration times
- ⚠️ **Implement role-based access control** to restrict admin operations
- ⚠️ **Log all admin operations** for audit trails
- ⚠️ **Protect initialization endpoint** - should only work when no users exist
- ⚠️ **Use HTTPS** in production to protect credentials and tokens

### Example: Admin Operation with Authentication

```csharp
// In your application, use the authentication service to get a token
var loginResult = await authService.LoginAsync("admin@example.com", "password");

if (!loginResult.Success)
{
    Console.WriteLine("Authentication failed");
    return;
}

// Token is automatically attached by the authentication handler
// Now you can call admin endpoints
var users = await usersClient.GetUsersAsync();
```

## Common Workflows

### Initial System Setup

```csharp
// 1. Initialize the system (first-time only)
var initRequest = new InitializeSystemRequest
{
    UserName = "superadmin",
    Email = "admin@yourcompany.com",
    Password = "YourSecurePassword123!"
};

var initResult = await initializationClient.InitializeSystemAsync(initRequest);

// 2. Login with super admin credentials (using main API client)
var loginResult = await authService.LoginAsync(
    initRequest.Email,
    initRequest.Password);
```

### Creating a New Administrator

```csharp
// 1. Get all roles to find Administrator role ID
var rolesResponse = await rolesClient.GetRolesAsync();
var adminRole = rolesResponse.Data?.FirstOrDefault(r => r.Name == "Administrator");

// 2. Create the user
var createUserRequest = new CreateUserRequest
{
    UserName = "newadmin",
    Email = "newadmin@yourcompany.com",
    Password = "SecurePassword123!",
    EmailConfirmed = true
};

var userResponse = await usersClient.CreateUserAsync(createUserRequest);

// 3. Assign Administrator role
if (userResponse.Success && adminRole != null)
{
    var assignRequest = new AssignRoleRequest
    {
        UserId = userResponse.Data!.Id,
        RoleId = adminRole.Id
    };

    await userRolesClient.AssignRoleAsync(assignRequest);
}
```

## Dependencies

- Microsoft.Extensions.Http (9.0.0)
- Microsoft.Extensions.Http.Polly (9.0.0)
- Microsoft.Extensions.Options.ConfigurationExtensions (9.0.0)
- TentMan.Contracts (Project Reference)
- TentMan.ApiClient (Project Reference - for exception types)

## Related Projects

- **TentMan.AdminApi** - The Admin API server this client connects to
- **TentMan.ApiClient** - Main API client for regular application operations
- **TentMan.Contracts** - Shared DTOs and request/response models

## Contributing

Follow clean code architecture principles and modern C# best practices when contributing.

---

**Last Updated**: 2025-01-09  
**Version**: 1.0  
**Maintainer**: TentMan Development Team
