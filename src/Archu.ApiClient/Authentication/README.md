# Archu.ApiClient Authentication Framework

Complete JWT-based authentication framework for Blazor applications with automatic token management, state synchronization, and platform-specific optimizations.

## 🔐 Features

- ✅ **JWT Token Management** - Secure token acquisition, storage, and validation
- ✅ **Automatic Token Attachment** - Seamless authorization header injection via HTTP message handler
- ✅ **Blazor Integration** - First-class AuthenticationStateProvider for Blazor Server and WebAssembly
- ✅ **Platform-Specific Storage** - Singleton for WASM, scoped for Server (prevents token leakage)
- ✅ **Browser Storage Support** - Optional localStorage integration for WebAssembly
- ✅ **Token Refresh** - Framework for automatic token refresh (requires API support)
- ✅ **Claims Extraction** - JWT claims parsing for user identity
- ✅ **Thread-Safe** - SemaphoreSlim-based concurrency control

## 📦 Architecture

```
Authentication/
├── Configuration/
│   └── AuthenticationOptions.cs    # Authentication configuration
├── Models/
│   ├── TokenResponse.cs               # API token response
│   ├── StoredToken.cs    # In-memory token representation
│   └── AuthenticationState.cs         # User authentication state
├── Storage/
│   ├── ITokenStorage.cs       # Token storage abstraction
│ ├── InMemoryTokenStorage.cs        # In-memory storage (default)
│   └── BrowserLocalTokenStorage.cs    # Browser localStorage (WASM)
├── Services/
│   ├── ITokenManager.cs      # Token management interface
│   ├── TokenManager.cs                # Token lifecycle management
│   ├── IAuthenticationService.cs      # Authentication operations
│   └── AuthenticationService.cs       # Login/logout/refresh
├── Handlers/
│   └── AuthenticationMessageHandler.cs # Automatic token attachment
├── Providers/
│   └── ApiAuthenticationStateProvider.cs # Blazor auth state
└── README.md                  # This file
```

## 🚀 Quick Start

### 1. Configure Authentication

**appsettings.json:**
```json
{
  "ApiClient": {
    "BaseUrl": "https://localhost:7123",
    "TimeoutSeconds": 30,
    "RetryCount": 3
  },
  "Authentication": {
    "AutoAttachToken": true,
    "TokenExpirationBufferSeconds": 60,
    "AutoRefreshToken": false,
    "TokenRefreshThresholdSeconds": 300,
    "AuthenticationEndpoint": "api/v1/authentication/login",
    "RefreshTokenEndpoint": "api/v1/authentication/refresh-token",
    "UseBrowserStorage": false
  }
}
```

### 2. Register Services

**Blazor WebAssembly:**
```csharp
using Archu.ApiClient.Extensions;

// With configuration (recommended)
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
        authOptions.UseBrowserStorage = true; // Optional: browser localStorage
});

// Add Blazor authorization
builder.Services.AddAuthorizationCore();
```

**Blazor Server:**
```csharp
using Archu.ApiClient.Extensions;

// With configuration (recommended)
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
        // UseBrowserStorage is ignored for Server
    });

builder.Services.AddAuthorizationCore();
```

### 3. Use in Blazor Components

```razor
@page "/login"
@inject IAuthenticationService AuthService
@inject NavigationManager Navigation

<EditForm Model="@model" OnValidSubmit="HandleLoginAsync">
    <InputText @bind-Value="model.Email" placeholder="Email" />
    <InputText @bind-Value="model.Password" type="password" placeholder="Password" />
    <button type="submit">Login</button>
</EditForm>

@code {
    private LoginModel model = new();

    private async Task HandleLoginAsync()
    {
        var result = await AuthService.LoginAsync(model.Email, model.Password);
        
        if (result.Success)
        {
            Navigation.NavigateTo("/");
        }
        else
        {
// Show error
        }
    }

    private class LoginModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
```

### 4. Protect Routes

```razor
@page "/"
@attribute [Authorize]

<AuthorizeView>
    <Authorized>
        <h1>Welcome, @context.User.Identity?.Name!</h1>
    </Authorized>
    <NotAuthorized>
    <RedirectToLogin />
    </NotAuthorized>
</AuthorizeView>
```

## 🔧 Configuration Options

### AuthenticationOptions

| Option | Default | Description |
|--------|---------|-------------|
| `AutoAttachToken` | `true` | Automatically attach JWT to API requests |
| `TokenExpirationBufferSeconds` | `60` | Buffer time before token expiration |
| `AutoRefreshToken` | `false` | Enable automatic token refresh (requires API support) |
| `TokenRefreshThresholdSeconds` | `300` | Refresh token when less than this time remains |
| `AuthenticationEndpoint` | `api/v1/authentication/login` | Login endpoint path |
| `RefreshTokenEndpoint` | `api/v1/authentication/refresh-token` | Token refresh endpoint |
| `UseBrowserStorage` | `false` | Use browser localStorage (WASM only) |

## 🏗️ Components

### IAuthenticationService

Main authentication service interface:

```csharp
public interface IAuthenticationService
{
    // Login with username/password
    Task<AuthenticationResult> LoginAsync(string username, string password);
    
    // Logout and clear tokens
  Task LogoutAsync();
    
    // Check if user is authenticated
    Task<bool> IsAuthenticatedAsync();
    
    // Get current authentication state
  Task<AuthenticationState> GetAuthenticationStateAsync();
 
  // Refresh access token (requires API support)
    Task<bool> RefreshTokenAsync();
}
```

**Usage:**
```csharp
// Login
var result = await _authService.LoginAsync("user@example.com", "password");
if (result.Success)
{
    // Navigate to protected page
}

// Check authentication
if (await _authService.IsAuthenticatedAsync())
{
    // User is logged in
}

// Logout
await _authService.LogoutAsync();
```

### ITokenManager

Low-level token management:

```csharp
public interface ITokenManager
{
    // Store token
    Task StoreTokenAsync(StoredToken token);
    
    // Retrieve token
    Task<StoredToken?> GetTokenAsync();
    
    // Check if token is valid
    Task<bool> IsTokenValidAsync();
    
    // Clear stored tokens
    Task ClearTokenAsync();
    
    // Extract claims from JWT
  IEnumerable<Claim> GetClaimsFromToken(string token);
}
```

### AuthenticationMessageHandler

HTTP message handler that automatically attaches JWT tokens to requests:

```csharp
// Automatically registered for all API clients
// No manual configuration needed when using AddApiClientForWasm/Server
```

**How it works:**
1. Intercepts outgoing HTTP requests
2. Retrieves stored JWT token
3. Adds `Authorization: Bearer <token>` header
4. Forwards request

### ApiAuthenticationStateProvider

Blazor `AuthenticationStateProvider` implementation:

```csharp
// Automatically registered
// Provides authentication state to Blazor components
```

**Features:**
- Synchronizes with token storage
- Extracts claims from JWT
- Notifies components of authentication state changes
- Integrates with `<AuthorizeView>` and `[Authorize]`

## 🔒 Token Storage

### Platform-Specific Lifetimes

| Platform | Storage Lifetime | Implementation | Reason |
|----------|-----------------|----------------|--------|
| **Blazor WebAssembly** | Singleton | `InMemoryTokenStorage` or `BrowserLocalTokenStorage` | Single-user, client-side context |
| **Blazor Server** | Scoped (per circuit) | `InMemoryTokenStorage` | Multi-user, prevents token leakage between users |

### Storage Options

#### 1. In-Memory Storage (Default)

**Pros:**
- ✅ Secure (not persisted)
- ✅ Fast
- ✅ Works on all platforms

**Cons:**
- ❌ Lost on page refresh (WASM)
- ❌ Lost on app restart (Server)

**When to use:** Server-side or short sessions

#### 2. Browser localStorage (WASM Only)

**Pros:**
- ✅ Persists across page refreshes
- ✅ Survives browser restarts

**Cons:**
- ❌ Accessible via JavaScript (XSS risk)
- ❌ Not suitable for highly sensitive applications

**When to use:** WebAssembly with acceptable security trade-offs

**Enable:**
```csharp
builder.Services.AddApiClientForWasm(
    builder.Configuration,
    authOptions =>
    {
        authOptions.UseBrowserStorage = true;
    });
```

## 🔄 Token Refresh

The framework supports automatic token refresh (requires API support):

### Server Requirements

Your API must provide a token refresh endpoint:

```csharp
POST /api/v1/authentication/refresh-token
Content-Type: application/json

{
  "refreshToken": "your-refresh-token"
}
```

**Response:**
```json
{
  "token": "new-access-token",
  "refreshToken": "new-refresh-token",
  "expiresAt": "2025-01-23T12:00:00Z"
}
```

### Client Configuration

```json
{
  "Authentication": {
    "AutoRefreshToken": true,
    "TokenRefreshThresholdSeconds": 300,
  "RefreshTokenEndpoint": "api/v1/authentication/refresh-token"
  }
}
```

**Manual Refresh:**
```csharp
var success = await _authService.RefreshTokenAsync();
if (!success)
{
    // Redirect to login
}
```

## 🎭 Claims and User Identity

### Extracting Claims

```csharp
var token = await _tokenManager.GetTokenAsync();
if (token != null)
{
    var claims = _tokenManager.GetClaimsFromToken(token.AccessToken);
    var userId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
    var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
    var roles = claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
}
```

### Using in Blazor

```razor
<AuthorizeView>
    <Authorized>
        <p>User ID: @context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value</p>
        <p>Email: @context.User.FindFirst(ClaimTypes.Email)?.Value</p>
  <p>Roles: @string.Join(", ", context.User.FindAll(ClaimTypes.Role).Select(c => c.Value))</p>
    </Authorized>
</AuthorizeView>
```

### Role-Based Authorization

```razor
<AuthorizeView Roles="Administrator,Manager">
    <Authorized>
        <button @onclick="DeleteProduct">Delete</button>
    </Authorized>
    <NotAuthorized>
  <p>You don't have permission to delete products.</p>
    </NotAuthorized>
</AuthorizeView>
```

## 🧵 Thread Safety

All authentication services are thread-safe:

- `TokenManager` uses `SemaphoreSlim` for async locking
- `InMemoryTokenStorage` is thread-safe
- `BrowserLocalTokenStorage` uses JS interop (inherently single-threaded)

## 🔐 Security Best Practices

### 1. Token Storage

**For highly sensitive applications:**
- ❌ Avoid `UseBrowserStorage = true` on WebAssembly
- ✅ Use in-memory storage and require re-login on refresh
- ✅ Implement short-lived access tokens (15-60 minutes)

**For user-friendly applications:**
- ✅ Use browser storage with awareness of XSS risks
- ✅ Implement HTTP-only cookies for refresh tokens (requires API changes)

### 2. Token Expiration

**Recommended settings:**
```json
{
  "Jwt": {
 "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "Authentication": {
    "TokenExpirationBufferSeconds": 60,
    "TokenRefreshThresholdSeconds": 300
  }
}
```

### 3. HTTPS Only

**Always use HTTPS in production:**
- JWT tokens are sensitive credentials
- Transmit only over encrypted connections
- Configure HSTS headers

### 4. Server-Side Validation

**API must validate:**
- Token signature (JWT validation)
- Token expiration
- Token issuer and audience
- User permissions for each request

## 🐛 Troubleshooting

### Issue: Tokens Lost on Page Refresh (WASM)

**Symptom:** User logged out on browser refresh

**Solutions:**
1. Enable browser storage:
   ```csharp
   authOptions.UseBrowserStorage = true;
   ```
2. Or implement "Remember Me" functionality with long-lived refresh tokens

### Issue: Unauthorized Errors Despite Login

**Symptom:** API returns 401 even after successful login

**Checklist:**
1. Verify token is stored:
   ```csharp
   var token = await _tokenManager.GetTokenAsync();
   // Should not be null
   ```
2. Check `AutoAttachToken = true` in configuration
3. Verify API endpoint configuration in `appsettings.json`
4. Inspect network requests for `Authorization` header

### Issue: Token Leakage Between Users (Server)

**Symptom:** Users seeing each other's data in Blazor Server

**Solution:** Ensure using `AddApiClientForServer` (not `AddApiClientForWasm`):
```csharp
// ✅ Correct for Blazor Server
builder.Services.AddApiClientForServer(builder.Configuration);

// ❌ WRONG for Blazor Server (causes singleton token storage)
builder.Services.AddApiClientForWasm(builder.Configuration);
```

### Issue: JWT Claims Not Available

**Symptom:** `context.User.Claims` is empty

**Solutions:**
1. Verify JWT token contains claims (inspect token at jwt.io)
2. Check `ApiAuthenticationStateProvider` is registered
3. Ensure authentication state is refreshed after login:
   ```csharp
   await _authStateProvider.GetAuthenticationStateAsync();
   ```

## 📚 Examples

See the [Examples](Examples/) folder for complete working examples:

- **[AuthenticationExample.cs](Examples/AuthenticationExample.cs)** - Login/logout/refresh flows
- **[ProductServiceExample.cs](../Examples/ProductServiceExample.cs)** - Using authenticated API clients

## 🔗 Related Documentation

- **[Main README](../README.md)** - Complete API client documentation
- **[Resilience Guide](../RESILIENCE.md)** - Error handling and retry policies
- **[Archu.Api Authentication](../../Archu.Api/README.md#authentication)** - API authentication endpoints

## 📋 API Requirements

Your backend API should provide:

1. **Login Endpoint**
```
   POST /api/v1/authentication/login
   Body: { "email": "...", "password": "..." }
   Response: { "token": "...", "refreshToken": "...", "expiresAt": "..." }
   ```

2. **Token Refresh Endpoint** (optional)
   ```
   POST /api/v1/authentication/refresh-token
   Body: { "refreshToken": "..." }
   Response: { "token": "...", "refreshToken": "...", "expiresAt": "..." }
   ```

3. **JWT Configuration**
   - Include standard claims: `sub` (user ID), `email`, `name`
   - Include role claims for authorization
   - Set appropriate expiration times

## 🔄 Version History

- **v2.0** - Added platform-specific registration methods, browser storage support
- **v1.0** - Initial release with JWT authentication

---

**Last Updated**: 2025-01-23  
**Version**: 2.0  
**Maintainer**: Archu Development Team
