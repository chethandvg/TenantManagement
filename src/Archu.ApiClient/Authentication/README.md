# Authentication Framework - Archu.ApiClient

## Overview

The Archu.ApiClient authentication framework provides a complete solution for managing JWT-based authentication in .NET applications, with first-class support for Blazor (Server and WebAssembly).

## Features

✅ **JWT Token Management** - Automatic token acquisition, storage, and validation  
✅ **Token Storage** - In-memory and browser local storage implementations  
✅ **Platform-Specific Lifetimes** - Singleton for WASM, scoped for Server (prevents token leakage)  
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
| `UseBrowserStorage` | Use browser local storage (WASM only) | `false` |

## Setup

⚠️ **IMPORTANT**: Choose the correct registration method based on your Blazor hosting model to ensure proper token storage lifetimes.

### 1. Register Services - Blazor WebAssembly

**Program.cs (Blazor WASM)**

```csharp
using Archu.ApiClient.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add API client with authentication for WASM (singleton token storage)
builder.Services.AddApiClientForWasm(builder.Configuration, authOptions =>
{
    authOptions.AutoAttachToken = true;
    authOptions.UseBrowserStorage = true; // Optional: persist tokens in browser
});

// Add authorization services
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
```

### 1. Register Services - Blazor Server

**Program.cs (Blazor Server)**

```csharp
using Archu.ApiClient.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add API client with authentication for Server (scoped token storage)
builder.Services.AddApiClientForServer(builder.Configuration, authOptions =>
{
    authOptions.AutoAttachToken = true;
    // Note: UseBrowserStorage is not used for Server
});

// Add authorization services
builder.Services.AddAuthorizationCore();

var app = builder.Build();
// ... rest of configuration
```

### Token Storage Lifetimes Explained

| Platform | Method | Token Storage Lifetime | Reason |
|----------|--------|----------------------|--------|
| **Blazor WebAssembly** | `AddApiClientForWasm` | **Singleton** | Single-user, client-side context - one user per browser instance |
| **Blazor Server** | `AddApiClientForServer` | **Scoped** (per circuit) | Multi-user, server-side - prevents token leakage between concurrent users |

⚠️ **Security Warning**: Using singleton token storage in Blazor Server would cause all users to share the same token after any login, which is a **critical security vulnerability**. Always use `AddApiClientForServer` for Blazor Server applications.

### Custom Configuration (Without appsettings.json)

**Blazor WebAssembly:**

```csharp
builder.Services.AddApiClientForWasm(
    options =>
    {
        options.BaseUrl = "https://api.example.com";
        options.TimeoutSeconds = 30;
        options.RetryCount = 3;
    },
    authOptions =>
    {
        authOptions.AutoAttachToken = true;
        authOptions.UseBrowserStorage = true;
        authOptions.TokenExpirationBufferSeconds = 60;
    });
```

**Blazor Server:**

```csharp
builder.Services.AddApiClientForServer(
    options =>
    {
        options.BaseUrl = "https://api.example.com";
        options.TimeoutSeconds = 30;
        options.RetryCount = 3;
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

## Token Storage

### In-Memory Storage

**Blazor Server (Scoped):**
- Tokens stored in memory per user session/circuit
- Automatically disposed when user disconnects
- Prevents token sharing between users
- **Registered automatically when using `AddApiClientForServer`**

**Blazor WebAssembly (Singleton):**
- Tokens stored in memory for the application lifetime
- Suitable for single-user client-side context
- Lost when browser tab is closed or refreshed
- **Registered automatically when using `AddApiClientForWasm` with `UseBrowserStorage = false`**

### Browser Local Storage (WASM Only)

Tokens are persisted in browser local storage across page refreshes.

```csharp
builder.Services.AddApiClientForWasm(builder.Configuration, authOptions =>
{
    authOptions.UseBrowserStorage = true; // Enable browser storage
});
```

**Note:** The `BrowserLocalTokenStorage` implementation requires JavaScript interop to be completed. You'll need to inject `IJSRuntime` and implement the localStorage API calls.

## Authentication State Provider

The `ApiAuthenticationStateProvider` integrates with Blazor's built-in authentication system:

```csharp
@inject ApiAuthenticationStateProvider AuthStateProvider

// Raise notification that authentication state has changed
AuthStateProvider.RaiseAuthenticationStateChanged();

// Mark user as authenticated
AuthStateProvider.MarkUserAsAuthenticated(accessToken);

// Mark user as logged out
await AuthStateProvider.MarkUserAsLoggedOutAsync();
```

## Security Best Practices

1. **Use HTTPS** - Always use HTTPS in production to protect tokens in transit
2. **Correct Registration** - Use `AddApiClientForServer` for Blazor Server, `AddApiClientForWasm` for WASM
3. **Token Expiration** - Set appropriate token expiration times
4. **Refresh Tokens** - Implement refresh token rotation
5. **Secure Storage** - Use secure storage mechanisms (HttpOnly cookies for sensitive scenarios)
6. **CORS Configuration** - Configure CORS properly on your API
7. **Validate JWT** - Ensure your API validates JWT signatures and claims
8. **Don't Log Tokens** - Never log tokens in production
9. **Scoped Storage** - Always use scoped storage for multi-user environments

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

## Troubleshooting

### Tokens not being attached to requests

- Ensure `AutoAttachToken` is set to `true` in configuration
- Verify `AuthenticationMessageHandler` is registered in the HTTP client pipeline
- Check logs for any errors during token retrieval

### Authentication state not updating

- Call `RaiseAuthenticationStateChanged()` after login/logout
- Ensure `ApiAuthenticationStateProvider` is registered correctly
- Check that `CascadingAuthenticationState` wraps your Blazor components

### Token expired errors

- Adjust `TokenExpirationBufferSeconds` in configuration
- Implement automatic token refresh
- Ensure server and client clocks are synchronized

### Token leakage between users (Blazor Server)

- **Ensure you're using `AddApiClientForServer`** (not `AddApiClientForWasm`)
- Verify token storage is registered as scoped, not singleton
- Check logs for concurrent login attempts

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
**Version**: 1.1  
**Maintainer**: Archu Development Team
