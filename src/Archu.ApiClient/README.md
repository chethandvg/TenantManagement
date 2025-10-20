# Archu.ApiClient

A .NET 9 HTTP client library for interacting with the Archu API using the HttpClientFactory pattern and Polly for resilience.

## Features

- ✅ Clean Architecture with separation of concerns
- ✅ HttpClientFactory pattern for efficient HTTP client management
- ✅ Polly integration for retry policies and circuit breaker
- ✅ Strongly-typed API clients
- ✅ Configuration-based setup
- ✅ Comprehensive exception handling with custom exceptions
- ✅ Structured logging support
- ✅ Full nullable reference type support

## Installation

Add a project reference to your application:

```xml
<ProjectReference Include="..\Archu.ApiClient\Archu.ApiClient.csproj" />
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
    "EnableDetailedLogging": false
  }
}
```

## Usage

### 1. Register the API Client in Dependency Injection

In your `Program.cs` or startup configuration:

```csharp
using Archu.ApiClient.Extensions;

// Using configuration
builder.Services.AddApiClient(builder.Configuration);

// Or using explicit configuration
builder.Services.AddApiClient(options =>
{
    options.BaseUrl = "https://localhost:7001";
    options.TimeoutSeconds = 30;
    options.RetryCount = 3;
});
```

### 2. Inject and Use the API Client

```csharp
using Archu.ApiClient.Services;
using Archu.ApiClient.Exceptions;
using Archu.Contracts.Products;

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
using Archu.ApiClient.Helpers;

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
Archu.ApiClient/
├── Configuration/
│   └── ApiClientOptions.cs          # Configuration options
├── Exceptions/
│   ├── ApiClientException.cs        # Base exception
│   ├── ResourceNotFoundException.cs  # 404 exceptions
│   ├── ValidationException.cs       # 400/422 exceptions
│   ├── AuthorizationException.cs    # 401/403 exceptions
│   ├── ServerException.cs           # 5xx exceptions
│   └── NetworkException.cs          # Network errors
├── Extensions/
│   └── ServiceCollectionExtensions.cs # DI registration
├── Helpers/
│   └── ExceptionHandler.cs          # Exception handling utilities
├── Services/
│   ├── ApiClientServiceBase.cs      # Base HTTP client with common operations
│   ├── IProductsApiClient.cs        # Products API client interface
│   └── ProductsApiClient.cs         # Products API client implementation
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

3. Register in DI:

```csharp
services.AddHttpClient<IOrdersApiClient, OrdersApiClient>(client =>
{
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
})
.AddPolicyHandler(GetRetryPolicy(options.RetryCount, logger))
.AddPolicyHandler(GetCircuitBreakerPolicy(logger));
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
- Archu.Contracts (Project Reference)
