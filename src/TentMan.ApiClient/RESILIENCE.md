# API Client Resilience & Error Handling

This document describes the comprehensive error handling, logging, and automatic retry policies implemented in the TentMan.ApiClient library.

## 🛡️ Features

### ✅ Comprehensive Error Handling
- **Structured Exception Hierarchy**: Type-safe exception handling with specific exception types
- **Contextual Error Information**: Rich error context including status codes, error messages, and validation details
- **User-Friendly Messages**: Automatic translation of technical errors to user-friendly messages

### ✅ Structured Logging
- **Request/Response Logging**: Detailed logging of all HTTP operations
- **Performance Tracking**: Debug-level logging for monitoring request performance
- **Error Context**: Rich contextual information in error logs
- **Configurable Log Levels**: Different log levels for different scenarios

### ✅ Automatic Retry Policies (Polly)
- **Exponential Backoff**: Smart retry with increasing delays
- **Transient Error Handling**: Automatic retry for network and server errors
- **Configurable Retry Count**: Customize retry behavior per environment
- **Retry Logging**: Detailed logging of retry attempts

### ✅ Circuit Breaker Pattern
- **Automatic Failure Detection**: Opens circuit after threshold failures
- **Self-Healing**: Automatically tests service recovery
- **Configurable Thresholds**: Customize failure tolerance
- **State Change Logging**: Clear visibility of circuit breaker state

## 📊 Exception Hierarchy

```
Exception
└── ApiClientException (Base for all API errors)
    ├── NetworkException (Network connectivity issues)
    ├── ServerException (5xx server errors)
    ├── AuthorizationException (401/403 auth failures)
    ├── ResourceNotFoundException (404 not found)
    └── ValidationException (400/422 validation errors)
```

## 🔧 Configuration

### appsettings.json Configuration

```json
{
  "ApiClient": {
    "BaseUrl": "https://api.example.com",
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

### Configuration Options Explained

| Option | Default | Description |
|--------|---------|-------------|
| `BaseUrl` | Required | Base URL of the API |
| `TimeoutSeconds` | 30 | HTTP request timeout |
| `RetryCount` | 3 | Number of retry attempts |
| `ApiVersion` | "v1" | API version to use |
| `EnableDetailedLogging` | false | Enable verbose logging |
| `CircuitBreakerFailureThreshold` | 5 | Failures before circuit opens |
| `CircuitBreakerDurationSeconds` | 30 | Duration circuit stays open |
| `RetryBaseDelaySeconds` | 1.0 | Base delay for exponential backoff |
| `EnableCircuitBreaker` | true | Enable/disable circuit breaker |
| `EnableRetryPolicy` | true | Enable/disable retry policy |

### Programmatic Configuration

```csharp
builder.Services.AddApiClientForWasm(options =>
{
    options.BaseUrl = "https://api.example.com";
    options.TimeoutSeconds = 60;
    options.RetryCount = 5;
    options.EnableCircuitBreaker = true;
    options.CircuitBreakerFailureThreshold = 3;
    options.CircuitBreakerDurationSeconds = 60;
    options.RetryBaseDelaySeconds = 2.0;
});
```

## 🔄 Retry Policy

### How It Works

The retry policy uses **exponential backoff** with the following formula:

```
Delay = BaseDelaySeconds × 2^(RetryAttempt - 1)
```

**Example with RetryCount=3 and RetryBaseDelaySeconds=1.0:**
- 1st retry: 1 second delay
- 2nd retry: 2 seconds delay
- 3rd retry: 4 seconds delay

### Retryable Conditions

The retry policy automatically handles:
- **Transient HTTP errors** (408, 5xx except 501)
- **TaskCanceledException** (timeout scenarios)
- **HttpRequestException** (network errors)

### Non-Retryable Errors

These errors will NOT be retried:
- **Client errors** (400, 401, 403, 404, 422)
- **Validation errors**
- **Authentication failures**
- **Not implemented** (501)

### Retry Logging Example

```
2024-01-22 10:30:15 [Warning] Request to https://api.example.com/api/products/123 failed 
(Status: ServiceUnavailable, Exception: None). Waiting 1.00s before retry 1/3
```

## ⚡ Circuit Breaker

### States

1. **Closed** (Normal Operation)
   - All requests pass through
   - Failures are counted
   
2. **Open** (Service Unavailable)
   - Requests fail immediately
   - No calls to downstream service
   - After duration, moves to Half-Open
   
3. **Half-Open** (Testing Recovery)
   - One test request allowed
   - Success → Close circuit
   - Failure → Re-open circuit

### Configuration Example

```csharp
// Open circuit after 5 consecutive failures
// Keep open for 30 seconds before testing
options.CircuitBreakerFailureThreshold = 5;
options.CircuitBreakerDurationSeconds = 30;
```

### Circuit Breaker Logging

```
// Circuit Opens
[Error] Circuit breaker OPENED for 30s after 5 consecutive failures. 
Last failure - Status: InternalServerError, Exception: None, Request: https://api.example.com/api/products

// Circuit Half-Opens
[Information] Circuit breaker HALF-OPEN - Testing service availability with next request

// Circuit Resets
[Information] Circuit breaker RESET - Normal operations resumed
```

## 📝 Logging

### Log Levels

| Level | Used For |
|-------|----------|
| **Debug** | Request/response details, deserialization success |
| **Information** | Successful operations, circuit breaker state changes |
| **Warning** | Retry attempts, non-critical errors, validation failures |
| **Error** | API errors, server errors, circuit breaker opens |

### Example Log Output

```csharp
// Request Logging (Debug)
[Debug] Sending GET request to api/products/123
[Debug] GET request to api/products/123 completed with status 200
[Debug] Successfully deserialized response from https://api.example.com/api/products/123. Success: True

// Error Logging (Warning)
[Warning] Request to https://api.example.com/api/products/456 failed with status 404. 
Message: Product with ID 456 was not found, Errors: None

// Exception Logging (Error)
[Error] Network error occurred during HTTP request
System.Net.Http.HttpRequestException: Connection refused
```

### Structured Logging Benefits

- **Searchable**: Easy to query logs by URI, status code, etc.
- **Traceable**: Track complete request lifecycle
- **Actionable**: Clear error context for troubleshooting
- **Performance**: Monitor slow requests and bottlenecks

## 💡 Usage Examples

### Basic Error Handling

```csharp
try
{
    var response = await _productsClient.GetProductByIdAsync(productId);
    
    if (response.Success)
    {
        return response.Data;
    }
    
    // Handle non-exception failures
    _logger.LogWarning("Failed to get product: {Message}", response.Message);
    return null;
}
catch (ResourceNotFoundException ex)
{
    _logger.LogWarning(ex, "Product {ProductId} not found", productId);
    return null;
}
catch (AuthorizationException ex)
{
    _logger.LogError(ex, "Unauthorized access to product {ProductId}", productId);
    throw; // Re-throw for auth handling
}
catch (ApiClientException ex)
{
    _logger.LogError(ex, "Error getting product {ProductId}", productId);
    throw;
}
```

### Using Exception Helper

```csharp
using TentMan.ApiClient.Helpers;

try
{
    var response = await _productsClient.CreateProductAsync(request);
    return response.Data;
}
catch (Exception ex)
{
    // Log with context
    ExceptionHandler.HandleException(ex, _logger, "CreateProduct");
    
    // Get user-friendly message
    var userMessage = ExceptionHandler.GetUserFriendlyMessage(ex);
    
    // Check if retryable
    if (ExceptionHandler.IsRetryable(ex))
    {
        // Implement custom retry logic
    }
    
    return null;
}
```

### Validation Error Handling

```csharp
try
{
    var response = await _productsClient.CreateProductAsync(request);
    return (true, response.Data, string.Empty);
}
catch (ValidationException ex)
{
    var errorMessages = string.Join(", ", ex.Errors);
    _logger.LogWarning("Validation failed: {Errors}", errorMessages);
    return (false, null, ExceptionHandler.GetUserFriendlyMessage(ex));
}
```

## 🎯 Best Practices

### 1. Always Handle Specific Exceptions First

```csharp
try
{
    // API call
}
catch (ResourceNotFoundException ex)
{
    // Handle 404
}
catch (ValidationException ex)
{
    // Handle validation
}
catch (AuthorizationException ex)
{
    // Handle auth
}
catch (ApiClientException ex)
{
    // Handle other API errors
}
```

### 2. Use CancellationToken

```csharp
public async Task<ProductDto?> GetProductAsync(
    Guid id, 
    CancellationToken cancellationToken = default)
{
    return await _productsClient.GetProductByIdAsync(id, cancellationToken);
}
```

### 3. Check ApiResponse.Success

```csharp
var response = await _productsClient.GetProductsAsync();

if (response.Success && response.Data != null)
{
    return response.Data;
}

_logger.LogWarning("Failed to get products: {Message}", response.Message);
return Enumerable.Empty<ProductDto>();
```

### 4. Configure Appropriately per Environment

**Development:**
```json
{
  "ApiClient": {
    "TimeoutSeconds": 60,
    "RetryCount": 2,
    "EnableDetailedLogging": true,
    "EnableCircuitBreaker": false
  }
}
```

**Production:**
```json
{
  "ApiClient": {
    "TimeoutSeconds": 30,
    "RetryCount": 3,
    "EnableDetailedLogging": false,
    "EnableCircuitBreaker": true,
    "CircuitBreakerFailureThreshold": 5
  }
}
```

### 5. Monitor Circuit Breaker Events

Set up alerts for circuit breaker state changes in production:

```csharp
// Monitor circuit breaker opens
logger.LogError("Circuit breaker OPENED...")
// → Trigger alert to operations team
```

## 🔍 Troubleshooting

### Issue: Too Many Retries

**Symptom:** Requests taking too long due to many retries

**Solution:**
```json
{
  "ApiClient": {
    "RetryCount": 2,  // Reduce retry count
    "TimeoutSeconds": 15  // Reduce timeout
  }
}
```

### Issue: Circuit Breaker Opens Too Often

**Symptom:** Circuit breaker opening during legitimate traffic spikes

**Solution:**
```json
{
  "ApiClient": {
    "CircuitBreakerFailureThreshold": 10,  // Increase threshold
    "CircuitBreakerDurationSeconds": 15  // Reduce break duration
  }
}
```

### Issue: Missing Error Details

**Symptom:** Not enough information in error logs

**Solution:**
```json
{
  "ApiClient": {
    "EnableDetailedLogging": true
  }
}
```

Or configure logging level:
```json
{
  "Logging": {
    "LogLevel": {
      "TentMan.ApiClient": "Debug"
    }
  }
}
```

## 📚 Related Documentation

- [Main README](README.md) - General API client documentation
- [Authentication](Authentication/README.md) - Authentication framework
- [Exception Handling Examples](Examples/ProductServiceExample.cs) - Code examples

## 🔄 Version History

- **v2.0** - Added configurable retry and circuit breaker policies with comprehensive logging
- **v1.0** - Initial release with basic error handling

---

**Last Updated**: 2025-01-22  
**Maintainer**: TentMan Development Team
