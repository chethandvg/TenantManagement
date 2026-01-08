# TentMan Authentication Guide

Complete guide to authentication in TentMan, including JWT configuration, token management, and security best practices.

---

## 📚 Table of Contents

- [Overview](#overview)
- [JWT Configuration](#jwt-configuration)
- [Authentication Flow](#authentication-flow)
- [Token Management](#token-management)
- [Security Best Practices](#security-best-practices)
- [Implementation Details](#implementation-details)
- [Troubleshooting](#troubleshooting)

---

## 🎯 Overview

### Authentication System

TentMan uses **JWT (JSON Web Token)** authentication with:
- ✅ ASP.NET Core Identity for user management
- ✅ JWT Bearer tokens for API authentication
- ✅ Refresh tokens for seamless re-authentication
- ✅ Role-based authorization
- ✅ Secure secret management

### Key Components

| Component | Purpose |
|-----------|---------|
| **JwtTokenGenerator** | Generates access and refresh tokens |
| **TokenValidator** | Validates JWT tokens |
| **RefreshTokenService** | Manages refresh token lifecycle |
| **PasswordHasher** | Securely hashes passwords |
| **UserManager** | ASP.NET Core Identity user management |

---

## 🔧 JWT Configuration

### Quick Setup (User Secrets)

**Recommended for local development**:

```bash
cd src/TentMan.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "YourSecure64CharacterSecretKeyForJWTTokenGeneration1234567890"
dotnet user-secrets set "Jwt:Issuer" "https://localhost:7123"
dotnet user-secrets set "Jwt:Audience" "https://localhost:7123"
dotnet user-secrets set "Jwt:AccessTokenExpirationMinutes" "60"
dotnet user-secrets set "Jwt:RefreshTokenExpirationDays" "7"
```

### Configuration Options

| Setting | Type | Description | Default | Recommended |
|---------|------|-------------|---------|-------------|
| `Jwt:Secret` | string | Signing key (min 32 chars) | - | **Required**, 64+ chars |
| `Jwt:Issuer` | string | Token issuer (your API URL) | - | `https://yourdomain.com` |
| `Jwt:Audience` | string | Token audience | - | `https://yourdomain.com` |
| `Jwt:AccessTokenExpirationMinutes` | int | Access token lifetime | 60 | 15-60 minutes |
| `Jwt:RefreshTokenExpirationDays` | int | Refresh token lifetime | 7 | 7-30 days |

### Security Requirements

✅ **Secret must be**:
- At least 32 characters (256 bits recommended)
- Cryptographically random
- Never committed to source control
- Rotated periodically in production

✅ **URLs should**:
- Use HTTPS in production
- Match your actual API URL
- Be consistent across environments

### Generating Secure Secrets

**PowerShell:**
```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RNGCryptoServiceProvider]::Create().GetBytes($bytes)
$secret = [Convert]::ToBase64String($bytes)
Write-Host $secret
```

**Linux/macOS:**
```bash
openssl rand -base64 32
```

**Online (Use with caution)**:
```
https://randomkeygen.com/ (256-bit keys)
```

### Configuration Files

**appsettings.json** (default, development):
```json
{
  "Jwt": {
    "Secret": "PLACEHOLDER_WILL_BE_OVERRIDDEN_BY_USER_SECRETS",
    "Issuer": "https://localhost:7123",
    "Audience": "https://localhost:7123",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

**appsettings.Staging.json**:
```json
{
  "Jwt": {
    "Issuer": "https://staging-api.yourdomain.com",
    "Audience": "https://staging-api.yourdomain.com",
    "AccessTokenExpirationMinutes": 30
  }
}
```

**appsettings.Production.json**:
```json
{
  "Jwt": {
    "Issuer": "https://api.yourdomain.com",
    "Audience": "https://api.yourdomain.com",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 14
  }
}
```

⚠️ **Secret is NEVER in these files** - loaded from User Secrets or Azure Key Vault

### Azure Key Vault (Production)

For production, store secrets in Azure Key Vault:

**1. Install NuGet packages**:
```bash
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
```

**2. Configure in Program.cs**:
```csharp
if (builder.Environment.IsProduction())
{
    var keyVaultUrl = builder.Configuration["KeyVault:Url"];
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential());
}
```

**3. Store secret in Key Vault**:
```bash
az keyvault secret set --vault-name "your-vault" --name "Jwt--Secret" --value "your-secret"
```

**4. Grant access**:
```bash
# For Managed Identity
az keyvault set-policy --name "your-vault" \
  --object-id <managed-identity-id> \
  --secret-permissions get list
```

---

## 🔐 Authentication Flow

### Registration Flow

```
1. User submits registration form
   ↓
2. Validate input (username, email, password)
   ↓
3. Check if user/email already exists
   ↓
4. Hash password (ASP.NET Core Identity)
   ↓
5. Create user in database
   ↓
6. Generate JWT access token + refresh token
   ↓
7. Return tokens to user
   ↓
8. User can immediately access protected endpoints
```

**API Call**:
```http
POST /api/v1/authentication/register
Content-Type: application/json

{
  "userName": "johndoe",
  "email": "john@example.com",
  "password": "SecurePass123!"
}
```

**Response**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "abc123xyz...",
  "expiresIn": 3600
}
```

### Login Flow

```
1. User submits credentials (email + password)
   ↓
2. Find user by email
   ↓
3. Verify password hash
   ↓
4. Check if account is locked/disabled
   ↓
5. Load user roles
   ↓
6. Generate JWT access token with claims
   ↓
7. Generate refresh token and store in DB
   ↓
8. Return tokens to user
```

**API Call**:
```http
POST /api/v1/authentication/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "SecurePass123!"
}
```

**Response**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "abc123xyz...",
  "expiresIn": 3600
}
```

### Token Usage

```
1. User makes request to protected endpoint
   ↓
2. Include access token in Authorization header
   ↓
3. Middleware validates token signature
   ↓
4. Check token expiration
   ↓
5. Extract user claims (userId, roles)
   ↓
6. Authorize based on roles
   ↓
7. Execute endpoint logic
   ↓
8. Return response
```

**API Call**:
```http
GET /api/v1/products
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Refresh Token Flow

```
1. Access token expires (HTTP 401)
   ↓
2. User sends refresh token
   ↓
3. Validate refresh token exists in DB
   ↓
4. Check token expiration
   ↓
5. Check if token is revoked
   ↓
6. Generate new access token + refresh token
   ↓
7. Revoke old refresh token
   ↓
8. Store new refresh token
   ↓
9. Return new tokens
```

**API Call**:
```http
POST /api/v1/authentication/refresh
Content-Type: application/json

{
  "refreshToken": "abc123xyz..."
}
```

**Response**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "new123xyz...",
  "expiresIn": 3600
}
```

### Logout Flow

```
1. User sends logout request
   ↓
2. Validate access token
   ↓
3. Revoke all user's refresh tokens
   ↓
4. Return success
   ↓
5. Client discards tokens
```

**API Call**:
```http
POST /api/v1/authentication/logout
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 🎫 Token Management

### JWT Token Structure

**Header**:
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

**Payload (Claims)**:
```json
{
  "sub": "user-id-guid",
  "email": "john@example.com",
  "name": "johndoe",
  "role": ["User", "Manager"],
  "iat": 1674123456,
  "exp": 1674127056,
  "iss": "https://localhost:7123",
  "aud": "https://localhost:7123"
}
```

**Signature**:
```
HMACSHA256(
  base64UrlEncode(header) + "." + base64UrlEncode(payload),
  secret
)
```

### Access Token

**Purpose**: Short-lived token for API authentication

**Lifetime**: 15-60 minutes

**Storage**: 
- ✅ Memory (recommended)
- ✅ Session storage (browser)
- ❌ LocalStorage (vulnerable to XSS)

**Claims**:
- `sub` - User ID
- `email` - User email
- `name` - Username
- `role` - User roles (array)
- `exp` - Expiration timestamp
- `iss` - Issuer
- `aud` - Audience

### Refresh Token

**Purpose**: Long-lived token for obtaining new access tokens

**Lifetime**: 7-30 days

**Storage**: 
- ✅ HttpOnly cookie (recommended)
- ✅ Secure storage (encrypted)
- ❌ LocalStorage (vulnerable to XSS)

**Database Storage**:
```csharp
public class RefreshToken
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Token Validation

**Access Token Validation**:
1. ✅ Signature verification (using secret)
2. ✅ Expiration check (`exp` claim)
3. ✅ Issuer verification (`iss` claim)
4. ✅ Audience verification (`aud` claim)
5. ✅ Not before check (`nbf` claim, optional)

**Refresh Token Validation**:
1. ✅ Token exists in database
2. ✅ Not expired
3. ✅ Not revoked
4. ✅ Belongs to requesting user

### Token Revocation

**When to revoke**:
- User logs out
- User changes password
- User account is disabled
- Security breach detected
- Administrative action

**How to revoke**:
```csharp
// Revoke specific token
await _refreshTokenService.RevokeTokenAsync(refreshToken);

// Revoke all user tokens
await _refreshTokenService.RevokeAllUserTokensAsync(userId);
```

---

## 🛡️ Security Best Practices

### Secret Management

✅ **DO**:
- Use User Secrets for local development
- Use Azure Key Vault for production
- Rotate secrets periodically (e.g., every 90 days)
- Use 256-bit (32+ character) secrets
- Generate secrets cryptographically

❌ **DON'T**:
- Commit secrets to source control
- Share secrets via email/chat
- Use weak/predictable secrets
- Reuse secrets across environments
- Store secrets in plaintext

### Token Security

✅ **DO**:
- Use short-lived access tokens (15-60 min)
- Use HTTPS only in production
- Validate all token claims
- Implement token refresh flow
- Revoke tokens on sensitive actions
- Store refresh tokens in database
- Use HttpOnly cookies for refresh tokens

❌ **DON'T**:
- Store tokens in localStorage (XSS risk)
- Use long-lived access tokens
- Skip signature validation
- Allow HTTP in production
- Share tokens between users

### Password Security

✅ **DO**:
- Use ASP.NET Core Identity password hasher
- Enforce password complexity rules
- Require minimum 8 characters
- Include uppercase, lowercase, digits, symbols
- Check against common password lists
- Implement account lockout
- Rate limit login attempts

❌ **DON'T**:
- Store passwords in plaintext
- Use weak hashing algorithms (MD5, SHA1)
- Allow weak passwords
- Skip password validation

### API Security

✅ **DO**:
- Require authentication for sensitive endpoints
- Implement role-based authorization
- Use [Authorize] attributes
- Validate input rigorously
- Implement rate limiting
- Log authentication failures
- Monitor suspicious activity

❌ **DON'T**:
- Expose sensitive data without auth
- Trust client-side validation only
- Skip authorization checks
- Ignore security logs

---

## 🏗️ Implementation Details

### Service Registration

**Infrastructure Layer** (`DependencyInjection.cs`):
```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration,
    IHostEnvironment environment)
{
    // Add authentication services
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var jwtSettings = configuration.GetSection("Jwt").Get<JwtOptions>();
            
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.Secret))
            };
        });

    // Add Identity
    services.AddIdentity<ApplicationUser, IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

    // Add custom services
    services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
    services.AddScoped<IRefreshTokenService, RefreshTokenService>();
    
    return services;
}
```

### JWT Token Generator

**Interface**:
```csharp
public interface IJwtTokenGenerator
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
}
```

**Implementation**:
```csharp
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _jwtOptions;

    public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.UserName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add role claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
```

### Authentication Controller

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IRefreshTokenService _refreshTokenService;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenGenerator.GenerateAccessToken(user, roles);
        var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(user.Id);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _jwtOptions.AccessTokenExpirationMinutes * 60
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenGenerator.GenerateAccessToken(user, roles);
        var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(user.Id);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _jwtOptions.AccessTokenExpirationMinutes * 60
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var principal = _tokenGenerator.GetPrincipalFromExpiredToken(request.AccessToken);
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!await _refreshTokenService.ValidateRefreshTokenAsync(userId, request.RefreshToken))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        var roles = await _userManager.GetRolesAsync(user);
        
        var newAccessToken = _tokenGenerator.GenerateAccessToken(user, roles);
        var newRefreshToken = await _refreshTokenService.RotateRefreshTokenAsync(userId, request.RefreshToken);

        return Ok(new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = _jwtOptions.AccessTokenExpirationMinutes * 60
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await _refreshTokenService.RevokeAllUserTokensAsync(userId);
        return Ok();
    }
}
```

---

## 🚨 Troubleshooting

### "JWT Secret is not configured"

**Cause**: Missing JWT secret in configuration

**Solution**:
```bash
cd src/TentMan.Api
dotnet user-secrets set "Jwt:Secret" "YourSecure64CharacterSecretKey"
```

### "JWT Secret must be at least 32 characters"

**Cause**: Secret is too short

**Solution**: Generate a secure 256-bit secret (see Generating Secure Secrets section)

### "Invalid token signature"

**Cause**: Token signed with different secret

**Solution**: Ensure all APIs use the same JWT secret

### "Token has expired"

**Cause**: Access token lifetime exceeded

**Solution**: Use refresh token to get new access token

### "Unauthorized (401)" on valid token

**Possible causes**:
1. Token expired
2. Invalid signature
3. Issuer/audience mismatch
4. Token not included in request

**Debug steps**:
1. Decode token at jwt.io
2. Check expiration (`exp` claim)
3. Verify issuer and audience match configuration
4. Ensure Authorization header is present: `Bearer <token>`

### Refresh token not working

**Possible causes**:
1. Refresh token expired
2. Refresh token revoked
3. Token not found in database

**Debug steps**:
1. Check token expiration in database
2. Verify `IsRevoked` flag is false
3. Check logs for validation errors

---

## 📚 Related Documentation

- **[GETTING_STARTED.md](GETTING_STARTED.md)** - Initial setup
- **[API_GUIDE.md](API_GUIDE.md)** - API endpoints
- **[AUTHORIZATION_GUIDE.md](AUTHORIZATION_GUIDE.md)** - Role-based authorization
- **[PASSWORD_SECURITY_GUIDE.md](PASSWORD_SECURITY_GUIDE.md)** - Password policies
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - System architecture

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Maintainer**: TentMan Development Team
