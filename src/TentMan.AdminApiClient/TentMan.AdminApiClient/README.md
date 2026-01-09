# TentMan.AdminApiClient

A .NET client library for the TentMan Admin API, providing type-safe access to administrative operations including user management, role management, user-role assignments, and system initialization.

## Features

- **Type-safe API access**: Strongly-typed request and response models
- **Resilience**: Built-in retry policies and circuit breaker patterns using Polly
- **Dependency injection**: Easy integration with .NET's DI container
- **Logging**: Comprehensive logging for debugging and monitoring
- **Exception handling**: Custom exception types for different error scenarios

## Installation

Add a reference to the `TentMan.AdminApiClient` project in your application.

## Configuration

### appsettings.json

```json
{
  "AdminApiClient": {
    "BaseUrl": "https://your-admin-api.com",
    "TimeoutSeconds": 30,
    "RetryCount": 3,
    "ApiVersion": "v1",
    "EnableDetailedLogging": false,
    "CircuitBreakerFailureThreshold": 10,
    "CircuitBreakerDurationSeconds": 30,
    "RetryBaseDelaySeconds": 1.0,
    "EnableCircuitBreaker": true,
    "EnableRetryPolicy": true
  }
}
```

### Service Registration

```csharp
using TentMan.AdminApiClient.Extensions;

// Using configuration from appsettings.json
services.AddAdminApiClient(configuration);

// Or using custom configuration
services.AddAdminApiClient(options =>
{
    options.BaseUrl = "https://your-admin-api.com";
    options.TimeoutSeconds = 30;
    options.RetryCount = 3;
});
```

## Usage

### Users API Client

```csharp
public class MyService
{
    private readonly IUsersApiClient _usersClient;

    public MyService(IUsersApiClient usersClient)
    {
        _usersClient = usersClient;
    }

    public async Task<IEnumerable<UserDto>> GetUsersAsync()
    {
        var response = await _usersClient.GetUsersAsync(pageNumber: 1, pageSize: 10);
        
        if (response.Success)
        {
            return response.Data!;
        }
        
        throw new Exception(response.Message);
    }

    public async Task<UserDto> CreateUserAsync(string userName, string email, string password)
    {
        var request = new CreateUserRequest
        {
            UserName = userName,
            Email = email,
            Password = password
        };
        
        var response = await _usersClient.CreateUserAsync(request);
        return response.Data!;
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        await _usersClient.DeleteUserAsync(userId);
    }
}
```

### Roles API Client

```csharp
public async Task<IEnumerable<RoleDto>> GetRolesAsync()
{
    var response = await _rolesClient.GetRolesAsync();
    return response.Data!;
}

public async Task<RoleDto> CreateRoleAsync(string name, string? description = null)
{
    var request = new CreateRoleRequest
    {
        Name = name,
        Description = description
    };
    
    var response = await _rolesClient.CreateRoleAsync(request);
    return response.Data!;
}
```

### UserRoles API Client

```csharp
public async Task<IEnumerable<RoleDto>> GetUserRolesAsync(Guid userId)
{
    var response = await _userRolesClient.GetUserRolesAsync(userId);
    return response.Data!;
}

public async Task AssignRoleToUserAsync(Guid userId, Guid roleId)
{
    var request = new AssignRoleRequest
    {
        UserId = userId,
        RoleId = roleId
    };
    
    await _userRolesClient.AssignRoleAsync(request);
}

public async Task RemoveRoleFromUserAsync(Guid userId, Guid roleId)
{
    await _userRolesClient.RemoveRoleAsync(userId, roleId);
}
```

### Initialization API Client

```csharp
public async Task<InitializationResultDto> InitializeSystemAsync(
    string userName, 
    string email, 
    string password)
{
    var request = new InitializeSystemRequest
    {
        UserName = userName,
        Email = email,
        Password = password
    };
    
    var response = await _initializationClient.InitializeSystemAsync(request);
    return response.Data!;
}
```

## Exception Handling

The client throws specific exceptions for different error scenarios:

- `ValidationException`: Request validation failed (400 Bad Request)
- `AuthorizationException`: Authentication or authorization failed (401 Unauthorized, 403 Forbidden)
- `ResourceNotFoundException`: Requested resource not found (404 Not Found)
- `ServerException`: Server-side error occurred (5xx status codes)
- `NetworkException`: Network error during request
- `AdminApiClientException`: Base exception for other API errors

```csharp
try
{
    await _usersClient.CreateUserAsync(request);
}
catch (ValidationException ex)
{
    // Handle validation errors
    Console.WriteLine($"Validation failed: {ex.Message}");
    if (ex.Errors != null)
    {
        foreach (var error in ex.Errors)
        {
            Console.WriteLine($"  - {error}");
        }
    }
}
catch (AuthorizationException ex)
{
    // Handle authentication/authorization errors
    Console.WriteLine($"Access denied: {ex.Message}");
}
catch (AdminApiClientException ex)
{
    // Handle other API errors
    Console.WriteLine($"API error: {ex.Message} (Status: {ex.StatusCode})");
}
```

## Resilience Features

### Retry Policy

The client automatically retries failed requests using exponential backoff:

- Transient HTTP errors (5xx, 408, etc.) trigger retries
- Configurable retry count and base delay
- Exponential backoff: delay doubles with each retry attempt

### Circuit Breaker

The circuit breaker pattern prevents cascading failures:

- **Closed**: Normal operation, requests flow through
- **Open**: After threshold failures, requests are blocked
- **Half-Open**: After duration, a test request is allowed

## API Endpoints

| Client | Method | Endpoint |
|--------|--------|----------|
| `IUsersApiClient` | `GetUsersAsync` | GET /api/v1/admin/users |
| `IUsersApiClient` | `CreateUserAsync` | POST /api/v1/admin/users |
| `IUsersApiClient` | `DeleteUserAsync` | DELETE /api/v1/admin/users/{id} |
| `IRolesApiClient` | `GetRolesAsync` | GET /api/v1/admin/roles |
| `IRolesApiClient` | `CreateRoleAsync` | POST /api/v1/admin/roles |
| `IUserRolesApiClient` | `GetUserRolesAsync` | GET /api/v1/admin/userroles/{userId} |
| `IUserRolesApiClient` | `AssignRoleAsync` | POST /api/v1/admin/userroles/assign |
| `IUserRolesApiClient` | `RemoveRoleAsync` | DELETE /api/v1/admin/userroles/{userId}/roles/{roleId} |
| `IInitializationApiClient` | `InitializeSystemAsync` | POST /api/v1/admin/initialization/initialize |

## License

This project is part of the TentMan solution.
