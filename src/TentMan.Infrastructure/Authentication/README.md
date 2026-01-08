# TentMan Authentication Infrastructure

This directory contains the complete authentication infrastructure for the TentMan application, including JWT token generation, password hashing, and refresh token management.

---

## 📁 Directory Structure

```
Authentication/
├── AuthenticationService.cs              # Main authentication service
├── ClaimsPrincipalExtensions.cs         # Claims extraction utilities
├── DesignTimeCurrentUser.cs             # Design-time user for migrations
├── HttpContextCurrentUser.cs            # Production user context
├── JwtOptions.cs                        # JWT configuration options
├── JwtTokenService.cs                   # JWT token generation/validation
├── PasswordHasher.cs                    # BCrypt password hashing
└── RefreshTokenHandler.cs               # Refresh token management
```

---

## 🔐 Components

### 1. JwtOptions.cs

Configuration class for JWT token settings. Validates settings on startup.

**Properties:**
- `Secret` - Signing key (min 32 characters, 256 bits)
- `Issuer` - Token issuer (your API URL)
- `Audience` - Token audience (typically same as issuer)
- `AccessTokenExpirationMinutes` - Access token lifetime (default: 60 minutes)
- `RefreshTokenExpirationDays` - Refresh token lifetime (default: 7 days)

**Example Configuration:**
```json
{
  "Jwt": {
    "Secret": "YourSecure32CharacterOrLongerSecretKey!",
    "Issuer": "https://api.tentman.com",
    "Audience": "https://api.tentman.com",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

### 2. JwtTokenService.cs

Implements `IJwtTokenService` for JWT token operations.

**Features:**
- ✅ Generates access tokens with claims (user ID, email, roles)
- ✅ Generates secure refresh tokens (cryptographically random)
- ✅ Validates tokens with expiration, issuer, and audience checks
- ✅ Uses HS256 algorithm for signing
- ✅ Thread-safe token validation

**Usage:**
```csharp
var accessToken = _jwtTokenService.GenerateAccessToken(
    userId: "123",
    email: "user@example.com",
    userName: "user123",
    roles: new[] { "Admin", "User" }
);

var refreshToken = _jwtTokenService.GenerateRefreshToken();

var principal = _jwtTokenService.ValidateToken(accessToken);
if (principal != null)
{
    var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
```

### 3. PasswordHasher.cs

Implements `IPasswordHasher` using BCrypt for secure password hashing.

**Features:**
- ✅ Uses BCrypt with work factor 12
- ✅ Automatic salt generation
- ✅ Resistant to rainbow table attacks
- ✅ Configurable work factor for future-proofing

**Usage:**
```csharp
// Hash a password
var hashedPassword = _passwordHasher.HashPassword("MySecurePassword123!");

// Verify a password
bool isValid = _passwordHasher.VerifyPassword("MySecurePassword123!", hashedPassword);
```

### 4. AuthenticationService.cs

Implements `IAuthenticationService` for complete authentication workflows.

**Features:**
- ✅ User login with password verification
- ✅ Token generation (access + refresh)
- ✅ Token refresh
- ✅ Email confirmation
- ✅ Password reset
- ✅ Token revocation

**Usage:**
```csharp
// Login
var result = await _authenticationService.LoginAsync(
    "user@example.com",
    "MyPassword123!",
    cancellationToken
);

if (result.IsSuccess)
{
    var accessToken = result.Value!.AccessToken;
    var refreshToken = result.Value!.RefreshToken;
}

// Refresh token
var refreshResult = await _authenticationService.RefreshTokenAsync(
    refreshToken,
    cancellationToken
);
```

### 5. RefreshTokenHandler.cs

Manages refresh token lifecycle and validation.

**Features:**
- ✅ Stores refresh tokens in database with expiration
- ✅ Validates refresh token ownership and expiration
- ✅ Revokes used refresh tokens
- ✅ Cleans up expired tokens

### 6. ClaimsPrincipalExtensions.cs

Utility extensions for extracting claims from JWT tokens.

**Usage:**
```csharp
var userId = principal.GetUserId();
var email = principal.GetEmail();
var userName = principal.GetUserName();
var roles = principal.GetRoles();
```

### 7. HttpContextCurrentUser.cs

Implements `ICurrentUser` for production use, extracting user info from HTTP context.

**Features:**
- ✅ Extracts user ID from claims
- ✅ Thread-safe
- ✅ Integrates with ASP.NET Core authentication

### 8. DesignTimeCurrentUser.cs

Implements `ICurrentUser` for design-time scenarios (EF migrations).

---

## 🚀 Getting Started

### 1. Configure JWT Settings

**Option A: User Secrets (Development)**

```bash
cd src/TentMan.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "YourSecure32CharacterOrLongerSecretKey!"
dotnet user-secrets set "Jwt:Issuer" "https://localhost:7001"
dotnet user-secrets set "Jwt:Audience" "https://localhost:7001"
```

**Option B: Environment Variables (Production)**

```bash
export Jwt__Secret="YourSecure32CharacterOrLongerSecretKey!"
export Jwt__Issuer="https://api.tentman.com"
export Jwt__Audience="https://api.tentman.com"
```

**Option C: Azure Key Vault (Production)**

See [JWT Configuration Guide](../../../docs/JWT_CONFIGURATION_GUIDE.md) for detailed instructions.

### 2. Register Services

The Infrastructure layer provides a clean DependencyInjection extension:

```csharp
// In Program.cs
using TentMan.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Register all Infrastructure services (Database, Authentication, Repositories)
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// ... rest of configuration

var app = builder.Build();

app.UseAuthentication(); // Enable JWT authentication
app.UseAuthorization();  // Enable authorization

app.Run();
```

### 3. Protect API Endpoints

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize] // Require authentication
public class ProductsController : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")] // Require specific role
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
    {
        // Only authenticated Admin users can access
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdminRole")] // Use custom policy
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductRequest request)
    {
        // Only users meeting policy requirements can access
    }
}
```

---

## 🔑 JWT Token Structure

### Access Token Claims

```json
{
  "sub": "user-id-123",
  "email": "user@example.com",
  "unique_name": "johndoe",
  "jti": "token-unique-id",
  "iat": 1234567890,
  "role": ["Admin", "User"],
  "nbf": 1234567890,
  "exp": 1234571490,
  "iss": "https://api.tentman.com",
  "aud": "https://api.tentman.com"
}
```

**Standard Claims:**
- `sub` - Subject (user ID)
- `email` - User email
- `unique_name` - Username
- `jti` - JWT ID (unique identifier for this token)
- `iat` - Issued at (Unix timestamp)
- `nbf` - Not before (Unix timestamp)
- `exp` - Expiration (Unix timestamp)
- `iss` - Issuer (your API)
- `aud` - Audience (who can use this token)

**Custom Claims:**
- `role` - User roles (array)

### Refresh Token

Refresh tokens are **not** JWTs. They are cryptographically secure random strings (64 bytes, base64 encoded) stored in the database.

---

## 🛡️ Security Best Practices

### 1. Secret Management

✅ **DO:**
- Use User Secrets for local development
- Use Azure Key Vault for production
- Generate cryptographically secure secrets (min 32 characters)
- Rotate secrets periodically (every 90 days)

❌ **DON'T:**
- Commit secrets to Git
- Use weak or predictable secrets
- Share secrets between environments
- Store secrets in plain text

### 2. Token Expiration

**Recommended Settings:**
- **Access Tokens:** 15-60 minutes (short lifetime for security)
- **Refresh Tokens:** 7-30 days (longer lifetime for UX)

**Rationale:**
- Short access token lifetime limits damage if compromised
- Refresh tokens allow seamless re-authentication without user input
- Revoke refresh tokens on suspicious activity

### 3. Password Hashing

✅ **BCrypt with work factor 12:**
- Resistant to brute force attacks
- Automatic salt generation
- Adaptive (increase work factor as hardware improves)

❌ **Never use:**
- Plain text passwords
- MD5 or SHA1 hashing
- Simple hashing without salt

### 4. HTTPS Only

✅ **Production:**
- Always use HTTPS
- Enable `RequireHttpsMetadata` in JWT configuration
- Implement HSTS headers

❌ **Development:**
- HTTP is acceptable for localhost only
- Never use HTTP in production

---

## 📊 Token Lifecycle

```
┌─────────────────────────────────────────────────────────────┐
│                     User Authentication                      │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌─────────────────┐
                    │  POST /auth/login │
                    │  {email, password}│
                    └─────────────────┘
                              │
                              ▼
                    ┌─────────────────┐
                    │ Verify Password  │
                    └─────────────────┘
                              │
                              ▼
                ┌────────────────────────────┐
                │ Generate Access Token      │
                │ (Expires in 60 minutes)    │
                └────────────────────────────┘
                              │
                              ▼
                ┌────────────────────────────┐
                │ Generate Refresh Token     │
                │ (Expires in 7 days)        │
                │ Store in Database          │
                └────────────────────────────┘
                              │
                              ▼
                ┌────────────────────────────┐
                │ Return Both Tokens         │
                └────────────────────────────┘
                              │
                              ▼
        ┌────────────────────────────────────────┐
        │ Client Stores Tokens                   │
        │ - Access Token: Memory/Session Storage │
        │ - Refresh Token: Secure HTTP-only Cookie│
        └────────────────────────────────────────┘
                              │
          ┌───────────────────┴───────────────────┐
          │                                       │
          ▼                                       ▼
┌──────────────────┐                  ┌──────────────────┐
│ Make API Request │                  │ Access Token     │
│ with Access Token│                  │ Expires          │
└──────────────────┘                  └──────────────────┘
          │                                       │
          ▼                                       ▼
┌──────────────────┐                  ┌──────────────────┐
│ API Validates    │                  │ POST /auth/refresh│
│ Token            │                  │ {refreshToken}   │
└──────────────────┘                  └──────────────────┘
          │                                       │
          ▼                                       ▼
┌──────────────────┐                  ┌──────────────────┐
│ Return Data      │                  │ Validate Refresh │
└──────────────────┘                  │ Token in DB      │
                                      └──────────────────┘
                                                │
                                                ▼
                                    ┌──────────────────────┐
                                    │ Generate New Access  │
                                    │ Token                │
                                    └──────────────────────┘
                                                │
                                                ▼
                                    ┌──────────────────────┐
                                    │ Return New Token     │
                                    └──────────────────────┘
```

---

## 🧪 Testing Authentication

### 1. Login

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "admin@tentman.com",
  "password": "Admin@123"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "abc123xyz...",
    "expiresAt": "2025-01-22T10:00:00Z",
    "user": {
      "id": "123",
      "email": "admin@tentman.com",
      "userName": "admin"
    }
  }
}
```

### 2. Access Protected Endpoint

```http
GET /api/v1/products
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 3. Refresh Token

```http
POST /api/v1/auth/refresh
Content-Type: application/json

{
  "refreshToken": "abc123xyz..."
}
```

---

## 📚 Related Documentation

- 📖 [JWT Configuration Guide](../../../docs/JWT_CONFIGURATION_GUIDE.md)
- 📖 [Architecture Guide](../../../docs/ARCHITECTURE.md)
- 📖 [API Authentication Controller](../../TentMan.Api/Controllers/AuthenticationController.cs)
- 📖 [.NET Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- 📖 [JWT Best Practices](https://datatracker.ietf.org/doc/html/rfc8725)

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Maintainer**: TentMan Development Team
