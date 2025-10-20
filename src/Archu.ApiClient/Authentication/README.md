# Authentication Framework - Archu.ApiClient

## Overview

The Archu.ApiClient authentication framework provides a complete solution for managing JWT-based authentication in .NET applications, with first-class support for Blazor (Server and WebAssembly).

## Features

✅ **JWT Token Management** - Automatic token acquisition, storage, and validation  
✅ **Token Storage** - In-memory and browser local storage implementations  
✅ **Automatic Token Attachment** - HTTP message handler automatically adds tokens to requests  
✅ **AuthenticationStateProvider** - Blazor integration for authentication state  
✅ **Token Refresh** - Support for refresh token flow  
✅ **Claims Extraction** - Automatic extraction of user claims from JWT tokens  
✅ **Configurable** - Flexible configuration options  
✅ **Thread-Safe** - Safe for concurrent operations  

## Architecture

```
Authentication/
├── Configuration/
│   └── AuthenticationOptions.cs          # Configuration options
├── Models/
│   ├── TokenResponse.cs                  # Token response from API
│   ├── StoredToken.cs                    # Stored token representation
│   └── AuthenticationState.cs            # User authentication state
├── Storage/
│   ├── ITokenStorage.cs                  # Token storage interface
│   ├── InMemoryTokenStorage.cs           # In-memory storage (server-side)
│   └── BrowserLocalTokenStorage.cs       # Browser local storage (WASM)
├── Services/
│   ├── ITokenManager.cs                  # Token management interface
│   ├── TokenManager.cs                   # Token management implementation
│   ├── IAuthenticationService.cs         # Authentication service interface
│   └── AuthenticationService.cs          # Authentication service implementation
├── Handlers/
│   └── AuthenticationMessageHandler.cs   # HTTP message handler for token attachment
├── Providers/
│   └── ApiAuthenticationStateProvider.cs # Blazor authentication state provider
└── Examples/
    └── AuthenticationExample.cs          # Usage examples
```

## Installation

The authentication framework is included in the Archu.ApiClient package. The following dependencies are automatically installed:

- `System.IdentityModel.Tokens.Jwt` - JWT token handling
- `Microsoft.AspNetCore.Components.Authorization` - Blazor authentication support

## Configuration

### appsettings.json

```json
{
  "ApiClient": {
    "BaseUrl": "https://localhost:7001",
    "TimeoutSeconds": 30,
    "RetryCount": 3
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

### Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| `AutoAttachToken` | Automatically attach tokens to HTTP requests | `true` |
| `TokenExpirationBufferSeconds` | Consider token expired N seconds before actual expiration | `60` |
| `AutoRefreshToken` | Automatically refresh tokens when needed | `true` |
| `TokenRefreshThresholdSeconds` | Refresh token when lifetime is less than N seconds | `300` |
| `AuthenticationEndpoint` | API endpoint for login | `api/auth/login` |
| `RefreshTokenEndpoint` | API endpoint for token refresh | `api/auth/refresh` |
| `UseBrowserStorage` | Use browser local storage (WASM) vs in-memory (Server) | `false` |

## Setup

### 1. Register Services

**Program.cs (Blazor Server or API Client Consumer)**

```csharp
using Archu.ApiClient.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add API client with authentication
builder.Services.AddApiClient(builder.Configuration, authOptions =>
{
    authOptions.AutoAttachToken = true;
    authOptions.UseBrowserStorage = false; // false for Blazor Server, true for WASM
});

// Add authorization services (for Blazor)
builder.Services.AddAuthorizationCore();

var app = builder.Build();
```

**Or with custom configuration:**

```csharp
builder.Services.AddApiClient(
    options =>
    {
        options.BaseUrl = "https://api.example.com";
        options.TimeoutSeconds = 30;
    },
    authOptions =>
    {
        authOptions.AutoAttachToken = true;
        authOptions.TokenExpirationBufferSeconds = 60;
    });
```

### 2. Update App.razor (Blazor)

```razor
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
                <NotAuthorized>
                    <RedirectToLogin />
                </NotAuthorized>
            </AuthorizeRouteView>
        </Found>
    </Router>
</CascadingAuthenticationState>
```

## Usage

### Login

```csharp
@inject IAuthenticationService AuthService
@inject NavigationManager Navigation

private async Task HandleLoginAsync()
{
    var result = await AuthService.LoginAsync(username, password);
    
    if (result.Success)
    {
        Navigation.NavigateTo("/");
    }
    else
    {
        errorMessage = result.ErrorMessage;
    }
}
```

### Logout

```csharp
@inject IAuthenticationService AuthService

private async Task HandleLogoutAsync()
{
    await AuthService.LogoutAsync();
    Navigation.NavigateTo("/login");
}
```

### Check Authentication Status

```csharp
@inject ITokenManager TokenManager

protected override async Task OnInitializedAsync()
{
    var isAuthenticated = await TokenManager.IsAuthenticatedAsync();
    
    if (!isAuthenticated)
    {
        Navigation.NavigateTo("/login");
    }
}
```

### Get Current User Information

```csharp
@inject IAuthenticationService AuthService

private string? userName;
private string? userEmail;
private IEnumerable<string> roles = Array.Empty<string>();

protected override async Task OnInitializedAsync()
{
    var authState = await AuthService.GetAuthenticationStateAsync();
    
    if (authState.IsAuthenticated)
    {
        userName = authState.UserName;
        userEmail = authState.Email;
        roles = authState.Roles;
    }
}
```

### Use with AuthenticationState in Blazor

```razor
@inject AuthenticationStateProvider AuthStateProvider

<AuthorizeView>
    <Authorized>
        <p>Hello, @context.User.Identity?.Name!</p>
        <p>Email: @context.User.FindFirst("email")?.Value</p>
        <p>Roles: @string.Join(", ", context.User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value))</p>
    </Authorized>
    <NotAuthorized>
        <p>Please log in.</p>
    </NotAuthorized>
</AuthorizeView>
```

### Automatic Token Attachment

The `AuthenticationMessageHandler` automatically attaches tokens to HTTP requests:

```csharp
// No need to manually add Authorization header
// It's added automatically by the AuthenticationMessageHandler
var response = await productsApiClient.GetProductsAsync();
```

### Manual Token Management

```csharp
@inject ITokenManager TokenManager

// Get access token
var accessToken = await TokenManager.GetAccessTokenAsync();

// Check if authenticated
var isAuthenticated = await TokenManager.IsAuthenticatedAsync();

// Store a token manually (e.g., after receiving from API)
var tokenResponse = new TokenResponse
{
    AccessToken = "eyJhbGc...",
    RefreshToken = "refresh_token",
    ExpiresIn = 3600
};
await TokenManager.StoreTokenAsync(tokenResponse);

// Remove token (logout)
await TokenManager.RemoveTokenAsync();
```

## Token Storage

### In-Memory Storage (Default for Blazor Server)

Tokens are stored in memory and lost when the application restarts. Suitable for server-side Blazor.

```csharp
builder.Services.AddApiClient(builder.Configuration, authOptions =>
{
    authOptions.UseBrowserStorage = false; // Use in-memory storage
});
```

### Browser Local Storage (For Blazor WebAssembly)

Tokens are persisted in browser local storage. Requires JavaScript interop implementation.

```csharp
builder.Services.AddApiClient(builder.Configuration, authOptions =>
{
    authOptions.UseBrowserStorage = true; // Use browser storage
});
```

**Note:** The `BrowserLocalTokenStorage` implementation requires JavaScript interop to be completed. You'll need to inject `IJSRuntime` and implement the localStorage API calls.

## Authentication State Provider

The `ApiAuthenticationStateProvider` integrates with Blazor's built-in authentication system:

```csharp
@inject ApiAuthenticationStateProvider AuthStateProvider

// Notify Blazor that authentication state has changed
AuthStateProvider.NotifyAuthenticationStateChanged();

// Mark user as authenticated
await AuthStateProvider.MarkUserAsAuthenticatedAsync(accessToken);

// Mark user as logged out
await AuthStateProvider.MarkUserAsLoggedOutAsync();
```

## Security Best Practices

1. **Use HTTPS** - Always use HTTPS in production to protect tokens in transit
2. **Token Expiration** - Set appropriate token expiration times
3. **Refresh Tokens** - Implement refresh token rotation
4. **Secure Storage** - Use secure storage mechanisms (HttpOnly cookies for sensitive scenarios)
5. **CORS Configuration** - Configure CORS properly on your API
6. **Validate JWT** - Ensure your API validates JWT signatures and claims
7. **Don't Log Tokens** - Never log tokens in production

## Token Refresh Flow

The framework supports automatic token refresh:

```csharp
@inject IAuthenticationService AuthService

// Manual token refresh
var result = await AuthService.RefreshTokenAsync();

if (result.Success)
{
    // Token refreshed successfully
}
```

The `AuthenticationMessageHandler` can be extended to automatically refresh tokens on 401 responses.

## Claims Extraction

JWT claims are automatically extracted and available through the authentication state:

```csharp
var authState = await AuthService.GetAuthenticationStateAsync();

// Standard claims
var userId = authState.UserId;        // sub or nameidentifier
var userName = authState.UserName;    // name
var email = authState.Email;          // email
var roles = authState.Roles;          // role claims

// Custom claims
var customClaim = authState.User.FindFirst("custom_claim_type")?.Value;
```

## Integration with Existing Code

The authentication framework integrates seamlessly with existing API client code:

```csharp
// Before authentication
builder.Services.AddApiClient(builder.Configuration);

// After adding authentication
builder.Services.AddApiClient(builder.Configuration, authOptions =>
{
    authOptions.AutoAttachToken = true;
});

// Your existing API clients automatically get authentication!
@inject IProductsApiClient ProductsClient

var products = await ProductsClient.GetProductsAsync(); // Token automatically attached
```

## Error Handling

```csharp
try
{
    var result = await AuthService.LoginAsync(username, password);
    
    if (!result.Success)
    {
        // Handle authentication failure
        Console.WriteLine($"Login failed: {result.ErrorMessage}");
    }
}
catch (AuthorizationException ex)
{
    // Handle 401/403 errors
    Console.WriteLine("Authentication failed");
}
catch (Exception ex)
{
    // Handle other errors
    Console.WriteLine($"Error: {ex.Message}");
}
```

## Testing

### Mocking Authentication

```csharp
// Mock ITokenManager
var mockTokenManager = new Mock<ITokenManager>();
mockTokenManager
    .Setup(x => x.IsAuthenticatedAsync(It.IsAny<CancellationToken>()))
    .ReturnsAsync(true);

// Mock IAuthenticationService
var mockAuthService = new Mock<IAuthenticationService>();
mockAuthService
    .Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(AuthenticationResult.Succeeded(authState));
```

## Extending the Framework

### Custom Token Storage

Implement `ITokenStorage` for custom storage mechanisms:

```csharp
public class CustomTokenStorage : ITokenStorage
{
    public async Task StoreTokenAsync(StoredToken token, CancellationToken ct)
    {
        // Your custom storage implementation
    }
    
    // Implement other methods...
}

// Register
services.AddSingleton<ITokenStorage, CustomTokenStorage>();
```

### Custom Authentication Handler

Extend `AuthenticationMessageHandler` for custom behavior:

```csharp
public class CustomAuthHandler : AuthenticationMessageHandler
{
    public CustomAuthHandler(ITokenManager tokenManager, ILogger<CustomAuthHandler> logger)
        : base(tokenManager, logger)
    {
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ct)
    {
        // Custom logic before request
        var response = await base.SendAsync(request, ct);
        // Custom logic after response
        return response;
    }
}
```

## API Contract Models

The framework expects the following API contract models (available in `Archu.Contracts.Authentication`):

```csharp
// Login request
public record LoginRequest
{
    public string Username { get; init; }
    public string Password { get; init; }
    public bool RememberMe { get; init; }
}

// Authentication response
public record AuthenticationResponse
{
    public string AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public string TokenType { get; init; }
    public int ExpiresIn { get; init; }
    public string UserId { get; init; }
    public string Username { get; init; }
    public string? Email { get; init; }
    public IEnumerable<string> Roles { get; init; }
}

// Refresh token request
public record RefreshTokenRequest
{
    public string RefreshToken { get; init; }
}
```

## Troubleshooting

### Tokens not being attached to requests

- Ensure `AutoAttachToken` is set to `true` in configuration
- Verify `AuthenticationMessageHandler` is registered in the HTTP client pipeline
- Check logs for any errors during token retrieval

### Authentication state not updating

- Call `NotifyAuthenticationStateChanged()` after login/logout
- Ensure `ApiAuthenticationStateProvider` is registered correctly
- Check that `CascadingAuthenticationState` wraps your Blazor components

### Token expired errors

- Adjust `TokenExpirationBufferSeconds` in configuration
- Implement automatic token refresh
- Ensure server and client clocks are synchronized

## Examples

See the `Authentication/Examples/AuthenticationExample.cs` file for comprehensive usage examples.

## Future Enhancements

- [ ] Automatic token refresh on 401 responses
- [ ] JavaScript interop implementation for browser storage
- [ ] OAuth2/OpenID Connect support
- [ ] Multi-tenant support
- [ ] Token encryption at rest
- [ ] Biometric authentication support

## Contributing

Follow clean code architecture principles and modern C# best practices when contributing to this framework.

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Maintainer**: Archu Development Team
