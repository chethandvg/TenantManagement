# TentMan.ApiClient

A .NET 9 HTTP client library for interacting with the TentMan API using the HttpClientFactory pattern and Polly for resilience.

## Features

- ✅ Clean Architecture with separation of concerns
- ✅ HttpClientFactory pattern for efficient HTTP client management
- ✅ **Advanced Resilience Policies** - Configurable retry and circuit breaker with Polly
- ✅ **Comprehensive Logging** - Structured logging for all operations and errors
- ✅ **JWT Authentication Framework** - Complete authentication solution
- ✅ **Blazor Integration** - First-class support for Blazor Server and WebAssembly
- ✅ **Platform-Specific Registration** - Proper token storage lifetimes for WASM and Server
- ✅ Strongly-typed API clients
- ✅ Configuration-based setup
- ✅ Comprehensive exception handling with custom exceptions
- ✅ Full nullable reference type support

## Installation

Add a project reference to your application:

```xml
<ProjectReference Include="..\TentMan.ApiClient\TentMan.ApiClient.csproj" />
```

## Configuration

Add the following configuration to your `appsettings.json`:

```json
{
  "ApiClient": {
    "BaseUrl": "https://localhost:7001",
    "TimeoutSeconds": 30,
    "RetryCount": 3,
    "ApiVersion": "v1",
    "EnableDetailedLogging": false,
    "CircuitBreakerFailureThreshold": 5,
    "CircuitBreakerDurationSeconds": 30,
    "RetryBaseDelaySeconds": 1.0,
    "EnableCircuitBreaker": true,
    "EnableRetryPolicy": true
  },
  "Authentication": {
    "AutoAttachToken": true,
    "TokenExpirationBufferSeconds": 60,
    "AutoRefreshToken": true,
    "TokenRefreshThresholdSeconds": 300,
    "AuthenticationEndpoint": "api/auth/login",
    "RefreshTokenEndpoint": "api/auth/refresh",
    "UseBrowserStorage": false
  }
}
```

## 🛡️ Resilience & Error Handling

The API client includes comprehensive error handling, structured logging, and automatic retry policies. For detailed documentation, see:

📖 **[Resilience & Error Handling Documentation](RESILIENCE.md)**

### Key Resilience Features

- ✅ **Automatic Retry with Exponential Backoff** - Smart retry for transient failures
- ✅ **Circuit Breaker Pattern** - Prevents cascading failures with automatic recovery
- ✅ **Configurable Policies** - Customize retry count, delays, and thresholds
- ✅ **Structured Logging** - Complete visibility into request lifecycle and failures
- ✅ **Exception Hierarchy** - Type-safe error handling with specific exception types

### Quick Configuration Example

```csharp
builder.Services.AddApiClientForWasm(options =>
{
    options.BaseUrl = "https://api.example.com";
    options.RetryCount = 3;  // Retry up to 3 times
    options.RetryBaseDelaySeconds = 1.0;  // Exponential backoff starting at 1s
    options.CircuitBreakerFailureThreshold = 5;  // Open after 5 failures
    options.CircuitBreakerDurationSeconds = 30;  // Stay open for 30s
    options.EnableDetailedLogging = true;  // Enable verbose logging
});
```

## Quick Start

### 1. Register the API Client in Dependency Injection

⚠️ **IMPORTANT**: Choose the appropriate registration method based on your Blazor hosting model.

**For Blazor WebAssembly:**

```csharp
using TentMan.ApiClient.Extensions;

// Using configuration (recommended)
builder.Services.AddApiClientForWasm(builder.Configuration, authOptions =>
{
    authOptions.AutoAttachToken = true;
    authOptions.UseBrowserStorage = true; // Optional: use browser local storage
});

// Add authorization for Blazor
builder.Services.AddAuthorizationCore();
```

**For Blazor Server:**

```csharp
using TentMan.ApiClient.Extensions;

// Using configuration (recommended)
builder.Services.AddApiClientForServer(builder.Configuration, authOptions =>
{
    authOptions.AutoAttachToken = true;
    // Note: UseBrowserStorage is ignored for Server - always uses scoped in-memory storage
});

// Add authorization for Blazor
builder.Services.AddAuthorizationCore();
```

**Why Different Methods?**

- **Blazor WebAssembly**: Uses **singleton** token storage (single-user, client-side context)
- **Blazor Server**: Uses **scoped** token storage (multi-user, per-circuit isolation to prevent token leakage between users)

### 2. Use the API Client

```csharp
@inject IProductsApiClient ProductsClient
@inject IAuthenticationService AuthService

// Login
var loginResult = await AuthService.LoginAsync(username, password);
if (loginResult.Success)
{
    // Tokens are automatically attached to subsequent requests
    var products = await ProductsClient.GetProductsAsync();
}

// Logout
await AuthService.LogoutAsync();
```

## 🔐 Authentication Framework

The API client includes a complete authentication framework with JWT token management. For detailed documentation, see:

📖 **[Authentication Framework Documentation](Authentication/README.md)**

### Authentication Features

- ✅ JWT token acquisition and management
- ✅ Automatic token attachment to HTTP requests
- ✅ Token storage (in-memory or browser local storage)
- ✅ **Platform-specific token storage lifetimes** (singleton for WASM, scoped for Server)
- ✅ AuthenticationStateProvider for Blazor
- ✅ Token refresh support
- ✅ Claims extraction from JWT tokens
- ✅ Thread-safe operations

### Quick Authentication Example

```csharp
@page "/login"
@inject IAuthenticationService AuthService
@inject NavigationManager Navigation

<h3>Login</h3>

<EditForm Model="@model" OnValidSubmit="HandleLoginAsync">
    <InputText @bind-Value="model.Username" placeholder="Username" />
    <InputText @bind-Value="model.Password" type="password" placeholder="Password" />
    <button type="submit">Login</button>
</EditForm>

@if (!string.IsNullOrEmpty(errorMessage))
{
    <p class="error">@errorMessage</p>
}

@code {
    private LoginModel model = new();
    private string? errorMessage;

    private async Task HandleLoginAsync()
    {
        var result = await AuthService.LoginAsync(model.Username, model.Password);
        
        if (result.Success)
        {
            Navigation.NavigateTo("/");
        }
        else
        {
            errorMessage = result.ErrorMessage;
        }
    }

    private class LoginModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
```

### Protect Routes in Blazor

```razor
@page "/"
@attribute [Authorize]

<h1>Protected Page</h1>

<AuthorizeView>
    <Authorized>
        <p>Hello, @context.User.Identity?.Name!</p>
    </Authorized>
    <NotAuthorized>
        <RedirectToLogin />
    </NotAuthorized>
</AuthorizeView>
```

## Platform-Specific Registration

### Blazor WebAssembly Registration

```csharp
// Program.cs for Blazor WASM
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register with configuration
builder.Services.AddApiClientForWasm(builder.Configuration);

// Or with custom options
builder.Services.AddApiClientForWasm(
    options =>
    {
        options.BaseUrl = "https://api.example.com";
        options.TimeoutSeconds = 30;
    },
    authOptions =>
    {
        authOptions.AutoAttachToken = true;
        authOptions.UseBrowserStorage = true;
    });

builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
```

### Blazor Server Registration

```csharp
// Program.cs for Blazor Server
var builder = WebApplication.CreateBuilder(args);

// Register with configuration
builder.Services.AddApiClientForServer(builder.Configuration);

// Or with custom options
builder.Services.AddApiClientForServer(
    options =>
    {
        options.BaseUrl = "https://api.example.com";
        options.TimeoutSeconds = 30;
    },
    authOptions =>
    {
        authOptions.AutoAttachToken = true;
        // UseBrowserStorage is not used for Server
    });

builder.Services.AddAuthorizationCore();

var app = builder.Build();
// ... rest of configuration
```

### Token Storage Lifetimes

| Platform | Token Storage Lifetime | Reason |
|----------|----------------------|--------|
| **Blazor WebAssembly** | Singleton | Single-user, client-side context - one user per browser instance |
| **Blazor Server** | Scoped (per circuit) | Multi-user, server-side - prevents token leakage between concurrent users |

⚠️ **Security Note**: Using singleton token storage in Blazor Server would cause all users to share the same token after any login, which is a serious security vulnerability. Always use `AddApiClientForServer` for Blazor Server applications.

## Usage Examples

### Get Products

```csharp
using TentMan.ApiClient.Services;
using TentMan.ApiClient.Exceptions;
using TentMan.Contracts.Products;

public class ProductService
{
    private readonly IProductsApiClient _productsApiClient;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductsApiClient productsApiClient,
        ILogger<ProductService> logger)
    {
        _productsApiClient = productsApiClient;
        _logger = logger;
    }

    public async Task<ProductDto?> GetProductAsync(Guid productId)
    {
        try
        {
            var response = await _productsApiClient.GetProductByIdAsync(productId);
            
            if (response.Success)
            {
                return response.Data;
            }
            
            _logger.LogWarning("Failed to get product: {Message}", response.Message);
            return null;
        }
        catch (ResourceNotFoundException ex)
        {
            _logger.LogWarning(ex, "Product {ProductId} not found", productId);
            return null;
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId}", productId);
            throw;
        }
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        var response = await _productsApiClient.GetProductsAsync(pageNumber: 1, pageSize: 50);
        
        return response.Success 
            ? response.Data?.Items ?? Enumerable.Empty<ProductDto>()
            : Enumerable.Empty<ProductDto>();
    }

    public async Task<ProductDto?> CreateProductAsync(CreateProductRequest request)
    {
        try
        {
            var response = await _productsApiClient.CreateProductAsync(request);
            
            if (response.Success)
            {
                return response.Data;
            }
            
            foreach (var error in response.Errors ?? Enumerable.Empty<string>())
            {
                _logger.LogWarning("Validation error: {Error}", error);
            }
            
            return null;
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed: {Errors}", string.Join(", ", ex.Errors));
            throw;
        }
    }
}
```

## Exception Handling

The library provides structured exception handling with custom exception types:

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
try
{
    var response = await _productsApiClient.GetProductByIdAsync(productId);
    // Handle success
}
catch (ResourceNotFoundException ex)
{
    // Handle 404 - resource not found
    Console.WriteLine($"Product not found: {ex.Message}");
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
        // Redirect to login
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
catch (NetworkException ex)
{
    // Handle network errors
    Console.WriteLine("Network error. Please check your connection.");
}
catch (ApiClientException ex)
{
    // Handle other API errors
    Console.WriteLine($"API error: {ex.Message}");
}
```

### Using the Exception Handler Helper

```csharp
using TentMan.ApiClient.Helpers;

try
{
    var response = await _productsApiClient.GetProductByIdAsync(productId);
}
catch (Exception ex)
{
    // Log the exception with context
    ExceptionHandler.HandleException(ex, _logger, "GetProductById");
    
    // Get user-friendly message
    var userMessage = ExceptionHandler.GetUserFriendlyMessage(ex);
    
    // Check if retryable
    if (ExceptionHandler.IsRetryable(ex))
    {
        // Implement your retry logic
    }
}
```

## Architecture

### Project Structure

```
TentMan.ApiClient/
├── Authentication/                       # 🔐 Authentication Framework
│   ├── Configuration/
│   │   └── AuthenticationOptions.cs     # Authentication configuration
│   ├── Models/
│   │   ├── TokenResponse.cs             # Token response from API
│   │   ├── StoredToken.cs               # Stored token representation
│   │   └── AuthenticationState.cs       # User authentication state
│   ├── Storage/
│   │   ├── ITokenStorage.cs             # Token storage interface
│   │   ├── InMemoryTokenStorage.cs      # In-memory storage
│   │   └── BrowserLocalTokenStorage.cs  # Browser storage
│   ├── Services/
│   │   ├── ITokenManager.cs             # Token management
│   │   ├── TokenManager.cs
│   │   ├── IAuthenticationService.cs    # Authentication operations
│   │   └── AuthenticationService.cs
│   ├── Handlers/
│   │   └── AuthenticationMessageHandler.cs # Token attachment
│   ├── Providers/
│   │   └── ApiAuthenticationStateProvider.cs # Blazor integration
│   ├── Examples/
│   │   └── AuthenticationExample.cs     # Usage examples
│   └── README.md                        # Authentication documentation
├── Configuration/
│   └── ApiClientOptions.cs              # Configuration options
├── Exceptions/
│   ├── ApiClientException.cs            # Base exception
│   ├── ResourceNotFoundException.cs      # 404 exceptions
│   ├── ValidationException.cs           # 400/422 exceptions
│   ├── AuthorizationException.cs        # 401/403 exceptions
│   ├── ServerException.cs               # 5xx exceptions
│   └── NetworkException.cs              # Network errors
├── Extensions/
│   └── ServiceCollectionExtensions.cs   # DI registration
├── Helpers/
│   └── ExceptionHandler.cs              # Exception handling utilities
├── Services/
│   ├── ApiClientServiceBase.cs          # Base HTTP client
│   ├── IProductsApiClient.cs            # Products API client interface
│   ├── ProductsApiClient.cs             # Products API client implementation
│   ├── IBuildingsApiClient.cs           # Buildings & Units API client interface
│   ├── BuildingsApiClient.cs            # Buildings & Units API client implementation
│   ├── IOwnersApiClient.cs              # Owners API client interface
│   ├── OwnersApiClient.cs               # Owners API client implementation
│   ├── ITenantsApiClient.cs             # Tenants API client interface
│   ├── TenantsApiClient.cs              # Tenants API client implementation
│   ├── ITenantInvitesApiClient.cs       # Tenant Invites API client interface
│   ├── TenantInvitesApiClient.cs        # Tenant Invites API client implementation
│   ├── ILeasesApiClient.cs              # Leases API client interface
│   └── LeasesApiClient.cs               # Leases API client implementation
└── README.md
```

### Base Service Features

The `ApiClientServiceBase` provides:
- Generic GET, POST, PUT, DELETE operations
- Automatic response deserialization
- Comprehensive exception handling with custom exceptions
- ApiResponse wrapper support
- JSON serialization configuration
- Structured error parsing

### Resilience Policies

The library includes Polly policies for:
- **Retry Policy**: Exponential backoff with configurable retry count and logging
- **Circuit Breaker**: Opens after 5 consecutive failures, breaks for 30 seconds with logging

### Logging

All retry attempts, circuit breaker events, and exceptions are logged using `Microsoft.Extensions.Logging`:

```csharp
// Retry logging
logger.LogWarning("Request failed with {StatusCode}. Waiting {Delay}s before retry attempt {RetryAttempt}/{RetryCount}")

// Circuit breaker logging
logger.LogError("Circuit breaker opened for {BreakDelay}s due to {StatusCode}")
logger.LogInformation("Circuit breaker reset, requests will be allowed through")

// Exception logging via ExceptionHandler
ExceptionHandler.HandleException(exception, logger, "OperationName")
```

## Extending the Client

To add a new API client (e.g., for Orders):

1. Create the interface:

```csharp
public interface IOrdersApiClient
{
    Task<ApiResponse<OrderDto>> GetOrderAsync(Guid id, CancellationToken cancellationToken = default);
}
```

2. Implement the client:

```csharp
public sealed class OrdersApiClient : ApiClientServiceBase, IOrdersApiClient
{
    public OrdersApiClient(HttpClient httpClient) : base(httpClient) { }
    
    protected override string BasePath => "api/orders";
    
    public Task<ApiResponse<OrderDto>> GetOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return GetAsync<OrderDto>($"{id}", cancellationToken);
    }
}
```

3. Register in DI (in ServiceCollectionExtensions):

```csharp
services.AddHttpClient<IOrdersApiClient, OrdersApiClient>(client =>
{
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
})
.AddPolicyHandler(GetRetryPolicy(options.RetryCount, logger))
.AddPolicyHandler(GetCircuitBreakerPolicy(logger))
.AddHttpMessageHandler<AuthenticationMessageHandler>(); // Automatic token attachment
```

## Best Practices

1. **Always use interfaces** for dependency injection
2. **Handle ApiResponse properly** - check the Success property before accessing Data
3. **Use specific exception types** - catch specific exceptions before generic ones
4. **Use CancellationToken** to support request cancellation
5. **Configure appropriate timeouts** based on your API's characteristics
6. **Monitor circuit breaker status** in production environments
7. **Log exceptions with context** using the ExceptionHandler helper
8. **Provide user-friendly error messages** using ExceptionHandler.GetUserFriendlyMessage()
9. **Don't retry non-retryable exceptions** - use ExceptionHandler.IsRetryable() to check
10. **Use structured logging** to capture important context
11. **Secure token storage** - Use appropriate storage based on platform (in-memory for server, secure storage for client)
12. **Implement token refresh** - Handle token expiration gracefully

## Error Response Format

The API client expects error responses in the following format:

```json
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    "Name is required",
    "Price must be greater than 0"
  ],
  "timestamp": "2024-01-01T00:00:00Z"
}
```

## Dependencies

- Microsoft.Extensions.Http (9.0.0)
- Microsoft.Extensions.Http.Polly (9.0.0)
- Microsoft.Extensions.Options.ConfigurationExtensions (9.0.0)
- System.IdentityModel.Tokens.Jwt (8.14.0)
- Microsoft.AspNetCore.Components.Authorization (9.0.10)
- TentMan.Contracts (Project Reference)

## Related Documentation

- 📖 **[Resilience & Error Handling](RESILIENCE.md)** - Comprehensive resilience documentation
- 📖 **[Authentication Framework](Authentication/README.md)** - Complete authentication documentation
- 📖 [Exception Handling Examples](Examples/ProductServiceExample.cs) - Exception handling patterns
- 📖 [Authentication Examples](Authentication/Examples/AuthenticationExample.cs) - Authentication usage examples

## Contributing

Follow clean code architecture principles and modern C# best practices when contributing.

---

**Last Updated**: 2025-01-22  
**Version**: 2.1  
**Maintainer**: TentMan Development Team
