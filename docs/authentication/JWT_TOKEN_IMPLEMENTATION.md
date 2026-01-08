# JWT Token Generation - Implementation Guide

## Overview

This document describes the JWT (JSON Web Token) implementation for secure authentication in the TentMan application. The implementation includes access token generation, refresh token handling, and token validation following security best practices.

**Date**: 2025-01-22  
**Version**: 1.0  
**Status**: ✅ Completed

---

## 📋 Implementation Summary

### What Was Implemented

1. ✅ **JWT Configuration (`JwtOptions`)**
   - Configurable JWT settings via appsettings.json
   - Secret key, issuer, audience configuration
   - Token expiration settings
   - Built-in validation

2. ✅ **JWT Token Service Interface (`IJwtTokenService`)**
   - Application layer abstraction
   - Access token generation
   - Refresh token generation
   - Token validation
   - Expiration time helpers

3. ✅ **JWT Token Service Implementation (`JwtTokenService`)**
   - HS256 algorithm for signing
   - Claims-based token generation
   - Role-based authorization support
   - Token validation with security checks

4. ✅ **Refresh Token Handler (`RefreshTokenHandler`)**
   - Refresh token generation and storage
   - Token validation logic
   - Token rotation (security best practice)
   - Automatic cleanup of expired tokens

---

## 🗂️ Files Created

### 1. `JwtOptions.cs` (Infrastructure Layer)

**Location**: `src/TentMan.Infrastructure/Authentication/JwtOptions.cs`

**Purpose**: Configuration options for JWT token generation.

**Properties**:
- `Secret`: Signing key (min 256 bits/32 characters)
- `Issuer`: Token issuer (your API URL)
- `Audience`: Token audience (who it's for)
- `AccessTokenExpirationMinutes`: Access token lifetime (default: 60 min)
- `RefreshTokenExpirationDays`: Refresh token lifetime (default: 7 days)

**Example Configuration** (`appsettings.json`):
```json
{
  "Jwt": {
    "Secret": "YourSuperSecretKeyThatIsAtLeast256BitsLong!ChangeThisInProduction",
    "Issuer": "https://api.tentman.com",
    "Audience": "https://api.tentman.com",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

**Security Notes**:
- ⚠️ **NEVER commit the real secret to source control**
- ✅ Use Azure Key Vault, AWS Secrets Manager, or environment variables for production
- ✅ Minimum 32 characters (256 bits) for HS256 algorithm
- ✅ Use cryptographically random strings (e.g., from `openssl rand -base64 32`)

---

### 2. `IJwtTokenService.cs` (Application Layer)

**Location**: `src/TentMan.Application/Abstractions/Authentication/IJwtTokenService.cs`

**Purpose**: Abstraction for JWT token operations.

**Methods**:
```csharp
string GenerateAccessToken(string userId, string email, string userName, IEnumerable<string> roles);
string GenerateRefreshToken();
ClaimsPrincipal? ValidateToken(string token);
TimeSpan GetAccessTokenExpiration();
TimeSpan GetRefreshTokenExpiration();
DateTime GetAccessTokenExpiryUtc();
DateTime GetRefreshTokenExpiryUtc();
```

**Design Principle**: Interface defined in Application layer, implementation in Infrastructure layer (Dependency Inversion Principle).

---

### 3. `JwtTokenService.cs` (Infrastructure Layer)

**Location**: `src/TentMan.Infrastructure/Authentication/JwtTokenService.cs`

**Purpose**: Implementation of JWT token generation and validation.

**Key Features**:
- ✅ **HS256 Algorithm**: Symmetric key signing (HMAC-SHA256)
- ✅ **Standard JWT Claims**: `sub`, `email`, `jti`, `iat`
- ✅ **Role Claims**: `ClaimTypes.Role` and OIDC `role` claim
- ✅ **Token Validation**: Issuer, audience, lifetime, signature validation
- ✅ **Zero Clock Skew**: Strict expiration enforcement
- ✅ **Secure Refresh Tokens**: Cryptographically random, not JWTs

**Access Token Structure**:
```json
{
  "sub": "user-guid",
  "email": "user@example.com",
  "unique_name": "username",
  "jti": "token-guid",
  "iat": 1234567890,
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": "user-guid",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "username",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress": "user@example.com",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": ["Admin", "User"],
  "role": ["Admin", "User"],
  "iss": "https://api.tentman.com",
  "aud": "https://api.tentman.com",
  "exp": 1234571490,
  "nbf": 1234567890
}
```

**Refresh Token Format**:
- Base64-encoded 64-byte random string
- Example: `Xt7R2p9... (512 bits of randomness)`
- **NOT a JWT** - just an opaque identifier

---

### 4. `RefreshTokenHandler.cs` (Infrastructure Layer)

**Location**: `src/TentMan.Infrastructure/Authentication/RefreshTokenHandler.cs`

**Purpose**: Manages refresh token lifecycle and security.

**Methods**:
```csharp
(string, DateTime) GenerateAndStoreRefreshToken(ApplicationUser user);
bool ValidateRefreshToken(ApplicationUser user, string refreshToken);
void RevokeRefreshToken(ApplicationUser user);
(string, DateTime) RotateRefreshToken(ApplicationUser user);
Task<int> CleanupExpiredTokensAsync(DbContext dbContext, CancellationToken ct);
```

**Security Features**:
- ✅ **Token Rotation**: New token on refresh (prevents token reuse)
- ✅ **Expiry Validation**: Checks expiration time
- ✅ **Token Matching**: Verifies token matches stored value
- ✅ **Automatic Cleanup**: Background task to remove expired tokens
- ✅ **Audit Logging**: Logs all token operations

---

## 🔒 Security Best Practices

### Access Tokens (JWT)

1. **Short Expiration**
   - Default: 60 minutes (1 hour)
   - Recommended: 15-60 minutes
   - Balances security (short window for stolen tokens) and UX (less frequent refreshes)

2. **Claims-Based**
   - User ID, email, username, roles included
   - No sensitive data (passwords, SSNs, etc.)
   - Client can decode and inspect claims (they're not encrypted)

3. **Stateless**
   - Server doesn't store access tokens
   - Validated by signature verification
   - Can't be revoked until expiration (use short lifetimes)

### Refresh Tokens

1. **Long Expiration**
   - Default: 7 days
   - Recommended: 7-30 days
   - Allows users to stay logged in without re-entering password

2. **Opaque & Random**
   - Not JWTs (can't be decoded)
   - 512 bits of cryptographic randomness
   - Impossible to guess or brute-force

3. **Stored in Database**
   - Enables revocation (logout, security breach)
   - One refresh token per user (automatic rotation)
   - Expired tokens automatically cleaned up

4. **Token Rotation**
   - New refresh token issued on each use
   - Old token immediately invalidated
   - Prevents token replay attacks

### Algorithm Choice: HS256 vs RS256

**HS256 (Symmetric Key)** - **Current Implementation**:
- ✅ Faster performance (symmetric crypto)
- ✅ Simpler setup (one shared secret)
- ✅ Good for single-server or trusted multi-server environments
- ❌ Same key for signing and validation (must be kept secret)
- ❌ Can't distribute public key to external parties

**RS256 (Asymmetric Key)** - **Future Enhancement**:
- ✅ Public key can be shared (external services can validate)
- ✅ Private key only on auth server (more secure for distributed systems)
- ✅ Better for microservices or external API consumers
- ❌ Slower performance (asymmetric crypto)
- ❌ More complex setup (key pair generation, distribution)

**Recommendation**: HS256 is sufficient for most applications. Consider RS256 if:
- You have external parties validating tokens
- You have untrusted microservices
- You need to rotate signing keys without downtime

---

## 🚀 Usage Examples

### 1. Register a New User

```csharp
// In AuthenticationService implementation
public async Task<Result<AuthenticationResult>> RegisterAsync(
    string email,
    string password,
    string userName,
    CancellationToken ct = default)
{
    // 1. Hash password
    var passwordHash = _passwordHasher.HashPassword(password);
    
    // 2. Create user
    var user = new ApplicationUser
    {
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        UserName = userName,
        PasswordHash = passwordHash,
        SecurityStamp = Guid.NewGuid().ToString(),
        EmailConfirmed = false
    };
    
    await _userRepository.AddAsync(user, ct);
    
    // 3. Assign default role
    var userRole = new UserRole
    {
        UserId = user.Id,
        RoleId = defaultRoleId,
        AssignedAtUtc = DateTime.UtcNow,
        AssignedBy = "System"
    };
    
    await _userRoleRepository.AddAsync(userRole, ct);
    await _unitOfWork.SaveChangesAsync(ct);
    
    // 4. Generate tokens
    var roles = new[] { ApplicationRoles.User };
    var accessToken = _jwtTokenService.GenerateAccessToken(
        user.Id.ToString(),
        user.Email,
        user.UserName,
        roles);
    
    var (refreshToken, refreshExpiresAt) = _refreshTokenHandler.GenerateAndStoreRefreshToken(user);
    await _unitOfWork.SaveChangesAsync(ct);
    
    return Result<AuthenticationResult>.Success(new AuthenticationResult
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        AccessTokenExpiresAt = _jwtTokenService.GetAccessTokenExpiryUtc(),
        RefreshTokenExpiresAt = refreshExpiresAt,
        TokenType = "Bearer",
        User = new UserInfo
        {
            Id = user.Id.ToString(),
            UserName = user.UserName,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            Roles = roles
        }
    });
}
```

---

### 2. Login Existing User

```csharp
public async Task<Result<AuthenticationResult>> LoginAsync(
    string email,
    string password,
    CancellationToken ct = default)
{
    // 1. Find user by email
    var user = await _userRepository.GetByEmailAsync(email, ct);
    if (user is null)
        return Result<AuthenticationResult>.Failure("Invalid email or password");
    
    // 2. Check if locked out
    if (user.IsLockedOut)
        return Result<AuthenticationResult>.Failure("Account is locked. Try again later.");
    
    // 3. Verify password
    var passwordValid = _passwordHasher.VerifyPassword(password, user.PasswordHash);
    
    if (!passwordValid)
    {
        // Increment failed attempts
        user.AccessFailedCount++;
        
        if (user.AccessFailedCount >= 5)
        {
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
        }
        
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<AuthenticationResult>.Failure("Invalid email or password");
    }
    
    // 4. Reset failed attempts on successful login
    user.AccessFailedCount = 0;
    user.LockoutEnd = null;
    
    // 5. Get user roles
    var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
    
    // 6. Generate tokens
    var accessToken = _jwtTokenService.GenerateAccessToken(
        user.Id.ToString(),
        user.Email,
        user.UserName,
        roles);
    
    var (refreshToken, refreshExpiresAt) = _refreshTokenHandler.RotateRefreshToken(user);
    await _unitOfWork.SaveChangesAsync(ct);
    
    return Result<AuthenticationResult>.Success(new AuthenticationResult
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        AccessTokenExpiresAt = _jwtTokenService.GetAccessTokenExpiryUtc(),
        RefreshTokenExpiresAt = refreshExpiresAt,
        TokenType = "Bearer",
        User = new UserInfo
        {
            Id = user.Id.ToString(),
            UserName = user.UserName,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            Roles = roles
        }
    });
}
```

---

### 3. Refresh Access Token

```csharp
public async Task<Result<AuthenticationResult>> RefreshTokenAsync(
    string refreshToken,
    CancellationToken ct = default)
{
    // 1. Find user by refresh token
    var user = await _userRepository.GetByRefreshTokenAsync(refreshToken, ct);
    if (user is null)
        return Result<AuthenticationResult>.Failure("Invalid refresh token");
    
    // 2. Validate refresh token
    if (!_refreshTokenHandler.ValidateRefreshToken(user, refreshToken))
        return Result<AuthenticationResult>.Failure("Refresh token expired or invalid");
    
    // 3. Get user roles
    var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
    
    // 4. Generate new tokens (rotate refresh token)
    var accessToken = _jwtTokenService.GenerateAccessToken(
        user.Id.ToString(),
        user.Email,
        user.UserName,
        roles);
    
    var (newRefreshToken, refreshExpiresAt) = _refreshTokenHandler.RotateRefreshToken(user);
    await _unitOfWork.SaveChangesAsync(ct);
    
    return Result<AuthenticationResult>.Success(new AuthenticationResult
    {
        AccessToken = accessToken,
        RefreshToken = newRefreshToken,
        AccessTokenExpiresAt = _jwtTokenService.GetAccessTokenExpiryUtc(),
        RefreshTokenExpiresAt = refreshExpiresAt,
        TokenType = "Bearer",
        User = new UserInfo
        {
            Id = user.Id.ToString(),
            UserName = user.UserName,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            Roles = roles
        }
    });
}
```

---

### 4. Logout

```csharp
public async Task<Result> LogoutAsync(string userId, CancellationToken ct = default)
{
    var user = await _userRepository.GetByIdAsync(Guid.Parse(userId), ct);
    if (user is null)
        return Result.Failure("User not found");
    
    _refreshTokenHandler.RevokeRefreshToken(user);
    await _unitOfWork.SaveChangesAsync(ct);
    
    return Result.Success();
}
```

---

### 5. Validate Token (in Middleware/Controller)

```csharp
// In authentication middleware
var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
var principal = _jwtTokenService.ValidateToken(token);

if (principal is null)
{
    return Unauthorized();
}

// Extract user ID from claims
var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
var email = principal.FindFirstValue(ClaimTypes.Email);
var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
```

---

## 📦 NuGet Packages Installed

```xml
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.2.1" />
```

**Dependencies** (automatically installed):
- `Microsoft.IdentityModel.Tokens` 8.2.1
- `Microsoft.IdentityModel.JsonWebTokens` 8.2.1
- `Microsoft.IdentityModel.Logging` 8.2.1
- `Microsoft.IdentityModel.Abstractions` 8.2.1

---

## 🔧 Configuration & Setup

### Step 1: Add Configuration to appsettings.json

**Development** (`appsettings.Development.json`):
```json
{
  "Jwt": {
    "Secret": "DevelopmentSecretKeyForTestingOnly!MustBeAtLeast32Characters",
    "Issuer": "https://localhost:5001",
    "Audience": "https://localhost:5001",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

**Production** (`appsettings.Production.json` or Azure App Configuration):
```json
{
  "Jwt": {
    "Secret": "#{JWT_SECRET}#",  // Injected from Azure Key Vault
    "Issuer": "https://api.tentman.com",
    "Audience": "https://api.tentman.com",
    "AccessTokenExpirationMinutes": 30,
    "RefreshTokenExpirationDays": 14
  }
}
```

---

### Step 2: Register Services in DI Container

**Update `Program.cs` in API project**:

```csharp
// JWT Configuration
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

// Register JWT Services
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<RefreshTokenHandler>();

// Configure JWT Authentication
var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()!;

jwtOptions.Validate(); // Validate on startup

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
```

**Add middleware**:
```csharp
app.UseAuthentication();  // ← Add BEFORE UseAuthorization
app.UseAuthorization();
```

---

### Step 3: Protect API Endpoints

```csharp
[Authorize] // Requires valid JWT
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
    {
        // Only accessible with valid access token
    }
    
    [Authorize(Roles = ApplicationRoles.Admin)] // Requires Admin role
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProduct(Guid id)
    {
        // Only accessible by Admin users
    }
}
```

---

## 🧪 Testing

### Unit Tests

```csharp
[Fact]
public void GenerateAccessToken_WithValidParameters_ReturnsValidJwt()
{
    // Arrange
    var jwtOptions = new JwtOptions
    {
        Secret = "ThisIsATestSecretKeyThatIsAtLeast32CharactersLong!",
        Issuer = "test-issuer",
        Audience = "test-audience",
        AccessTokenExpirationMinutes = 60
    };
    
    var tokenService = new JwtTokenService(Options.Create(jwtOptions));
    
    // Act
    var token = tokenService.GenerateAccessToken(
        userId: "user-123",
        email: "test@example.com",
        userName: "testuser",
        roles: new[] { "User", "Admin" });
    
    // Assert
    Assert.NotNull(token);
    Assert.NotEmpty(token);
    
    // Validate token structure
    var handler = new JwtSecurityTokenHandler();
    var jwtToken = handler.ReadJwtToken(token);
    
    Assert.Equal("user-123", jwtToken.Subject);
    Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
}

[Fact]
public void ValidateToken_WithExpiredToken_ReturnsNull()
{
    // Arrange
    var jwtOptions = new JwtOptions
    {
        Secret = "ThisIsATestSecretKeyThatIsAtLeast32CharactersLong!",
        Issuer = "test-issuer",
        Audience = "test-audience",
        AccessTokenExpirationMinutes = -1 // Expired immediately
    };
    
    var tokenService = new JwtTokenService(Options.Create(jwtOptions));
    
    var token = tokenService.GenerateAccessToken(
        "user-123", "test@example.com", "testuser", new[] { "User" });
    
    // Wait for token to expire
    Thread.Sleep(1000);
    
    // Act
    var principal = tokenService.ValidateToken(token);
    
    // Assert
    Assert.Null(principal);
}

[Fact]
public void GenerateRefreshToken_ReturnsCryptographicallySecureToken()
{
    // Arrange
    var tokenService = new JwtTokenService(Options.Create(ValidJwtOptions));
    
    // Act
    var token1 = tokenService.GenerateRefreshToken();
    var token2 = tokenService.GenerateRefreshToken();
    
    // Assert
    Assert.NotEqual(token1, token2); // Tokens must be unique
    Assert.True(token1.Length > 80); // Base64-encoded 64 bytes ≈ 88 chars
}
```

---

### Integration Tests

```csharp
[Fact]
public async Task Login_WithValidCredentials_ReturnsTokens()
{
    // Arrange
    var client = _factory.CreateClient();
    var request = new LoginRequest
    {
        Email = "test@example.com",
        Password = "TestPassword123!"
    };
    
    // Act
    var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);
    
    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthenticationResult>>();
    
    Assert.NotNull(result.Data);
    Assert.NotEmpty(result.Data.AccessToken);
    Assert.NotEmpty(result.Data.RefreshToken);
    Assert.Equal("Bearer", result.Data.TokenType);
}

[Fact]
public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/api/v1/products");
    
    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}

[Fact]
public async Task ProtectedEndpoint_WithValidToken_ReturnsSuccess()
{
    // Arrange
    var client = _factory.CreateClient();
    var token = await GetValidAccessToken(); // Helper method
    
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    
    // Act
    var response = await client.GetAsync("/api/v1/products");
    
    // Assert
    response.EnsureSuccessStatusCode();
}
```

---

## 🛠️ Troubleshooting

### Problem: "JWT Secret is not configured"

**Solution**: Add JWT configuration to `appsettings.json`:
```json
{
  "Jwt": {
    "Secret": "YourSecretKeyHere...",
    "Issuer": "...",
    "Audience": "..."
  }
}
```

---

### Problem: "JWT Secret must be at least 32 characters"

**Solution**: Increase secret length:
```bash
# Generate secure random secret (PowerShell)
-join ((65..90) + (97..122) + (48..57) + (33..47) | Get-Random -Count 32 | % {[char]$_})

# Or use openssl (Linux/Mac)
openssl rand -base64 32
```

---

### Problem: "401 Unauthorized" on protected endpoints

**Possible causes**:
1. Token not included in request
2. Token expired
3. Invalid signature (wrong secret)
4. Token validation middleware not configured

**Solution**:
```csharp
// Ensure middleware order is correct
app.UseAuthentication(); // ← Must be before UseAuthorization
app.UseAuthorization();

// Check token in request
Authorization: Bearer <your-token-here>
```

---

### Problem: Tokens expire too quickly

**Solution**: Adjust expiration times in `appsettings.json`:
```json
{
  "Jwt": {
    "AccessTokenExpirationMinutes": 60,  // Increase this
    "RefreshTokenExpirationDays": 14      // Increase this
  }
}
```

---

## 📊 Performance Considerations

### Token Size

**Access Token Size**: ~1-2 KB
- Includes user ID, email, username, roles
- Sent with every API request
- Keep claims minimal to reduce bandwidth

**Refresh Token Size**: ~88 bytes (base64-encoded 64-byte random)
- Only sent during token refresh
- Not included in regular API requests

### Recommendations

1. **Cache JWT Validation Parameters**
   - JwtTokenService caches `SigningCredentials` and `TokenValidationParameters`
   - Avoids recreating on every validation

2. **Use Singleton for IJwtTokenService**
   - Stateless service - safe to share across requests
   - Reduces memory allocations

3. **Background Cleanup**
   - Run `CleanupExpiredTokensAsync()` periodically
   - Prevents database bloat from expired tokens
   - Recommended: Daily background job

```csharp
// Example: Hangfire background job
RecurringJob.AddOrUpdate<RefreshTokenHandler>(
    "cleanup-expired-tokens",
    handler => handler.CleanupExpiredTokensAsync(dbContext, CancellationToken.None),
    Cron.Daily);
```

---

## 🔐 Security Checklist

Before deploying to production:

- [ ] JWT Secret is at least 256 bits (32 characters)
- [ ] JWT Secret stored securely (Azure Key Vault, not in source control)
- [ ] HTTPS enforced for all API endpoints
- [ ] Access token expiration ≤ 60 minutes
- [ ] Refresh token rotation enabled
- [ ] Token validation middleware configured correctly
- [ ] `ClockSkew` set to zero (strict expiration)
- [ ] Sensitive claims (passwords, SSNs) excluded from tokens
- [ ] Role-based authorization implemented for sensitive endpoints
- [ ] Account lockout after failed login attempts
- [ ] Security logging enabled for token operations
- [ ] Expired refresh tokens automatically cleaned up

---

## 📚 Next Steps

1. **Implement Authentication Service**
   - Create `AuthenticationService.cs` in Infrastructure
   - Implement `IAuthenticationService` interface
   - Use `JwtTokenService` and `RefreshTokenHandler`

2. **Create Authentication Controller**
   - `POST /api/v1/auth/register`
   - `POST /api/v1/auth/login`
   - `POST /api/v1/auth/refresh`
   - `POST /api/v1/auth/logout`

3. **Add Password Hashing**
   - Use `Microsoft.AspNetCore.Identity` password hasher
   - Or implement custom hasher with BCrypt/Argon2

4. **Implement Email Confirmation**
   - Generate confirmation tokens
   - Send confirmation emails
   - Validate tokens and confirm emails

5. **Add Password Reset**
   - Generate reset tokens
   - Send password reset emails
   - Validate tokens and reset passwords

---

## 📖 Further Reading

- [JWT.io](https://jwt.io/) - JWT debugger and introduction
- [RFC 7519: JSON Web Token (JWT)](https://datatracker.ietf.org/doc/html/rfc7519)
- [OWASP JWT Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/JSON_Web_Token_for_Java_Cheat_Sheet.html)
- [Microsoft Identity Model Docs](https://learn.microsoft.com/en-us/dotnet/api/microsoft.identitymodel.tokens)
- [ASP.NET Core Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)

---

**Last Updated**: 2025-01-22  
**Implemented By**: Infrastructure Team  
**Status**: ✅ Ready for Integration

---

## Summary

This JWT implementation provides:

- ✅ Secure token generation with HS256 algorithm
- ✅ Access token with user claims and roles
- ✅ Refresh token with rotation support
- ✅ Token validation with strict security checks
- ✅ Configurable token expiration
- ✅ Automatic cleanup of expired tokens
- ✅ Comprehensive logging
- ✅ Production-ready security features

The next step is to integrate these services into the `AuthenticationService` implementation and expose authentication endpoints in the API layer.
