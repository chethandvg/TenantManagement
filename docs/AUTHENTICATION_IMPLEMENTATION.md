# Authentication Implementation Guide

Complete technical implementation guide for authentication infrastructure in Archu, including JWT tokens, CurrentUser service, and database setup.

**Date**: 2025-01-22  
**Version**: 1.0  
**Status**: ✅ Completed

---

## 📚 Table of Contents

- [Overview](#overview)
- [Infrastructure Database Setup](#infrastructure-database-setup)
- [JWT Token Implementation](#jwt-token-implementation)
- [CurrentUser Service](#currentuser-service)
- [Security Features](#security-features)
- [Usage Examples](#usage-examples)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)

---

## 🎯 Overview

This guide covers three core authentication components:

1. **Database Infrastructure** - User, Role, and UserRole entities with EF Core
2. **JWT Token Service** - Access token and refresh token generation/validation
3. **CurrentUser Service** - HTTP context-based user information access

### Key Technologies

- **Database**: SQL Server with Entity Framework Core 9
- **Authentication**: JWT Bearer tokens (HS256)
- **Password Hashing**: BCrypt (via ASP.NET Core Identity)
- **Concurrency**: Optimistic control with rowversion
- **Audit**: Automatic timestamp and user tracking

---

## 🗄️ Infrastructure Database Setup

### Database Schema

#### Users Table
Stores user authentication and account information.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | uniqueidentifier | User unique identifier (GUID) |
| `UserName` | nvarchar(256) | Unique username |
| `Email` | nvarchar(256) | User email address |
| `NormalizedEmail` | nvarchar(256) | Normalized email for lookups |
| `PasswordHash` | nvarchar(512) | Hashed password |
| `EmailConfirmed` | bit | Email verification status |
| `SecurityStamp` | nvarchar(256) | Token invalidation stamp |
| `RefreshToken` | nvarchar(512) | JWT refresh token |
| `RefreshTokenExpiryTime` | datetime2 | Token expiry timestamp |
| `AccessFailedCount` | int | Failed login attempts |
| `LockoutEnabled` | bit | Lockout feature flag |
| `LockoutEnd` | datetime2 | Lockout expiry timestamp |
| `TwoFactorEnabled` | bit | 2FA enablement flag |
| `RowVersion` | rowversion | Concurrency token |

**Unique Indexes:** Email, NormalizedEmail, UserName

#### Roles Table
Stores security roles for role-based access control.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | uniqueidentifier | Role unique identifier (GUID) |
| `Name` | nvarchar(256) | Role name (e.g., "Admin") |
| `NormalizedName` | nvarchar(256) | Normalized role name |
| `Description` | nvarchar(500) | Role description |
| `RowVersion` | rowversion | Concurrency token |

**Unique Indexes:** Name, NormalizedName

#### UserRoles Table
Many-to-many relationship between Users and Roles.

| Column | Type | Description |
|--------|------|-------------|
| `UserId` | uniqueidentifier | Reference to Users table |
| `RoleId` | uniqueidentifier | Reference to Roles table |
| `AssignedAtUtc` | datetime2 | Assignment timestamp |
| `AssignedBy` | nvarchar(256) | User who assigned role |

**Composite Primary Key:** (`UserId`, `RoleId`)

### Entity Configurations

**ApplicationUserConfiguration.cs:**
```csharp
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.HasIndex(u => u.Email).IsUnique();
            
        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Apply Migration

```bash
cd src/Archu.Infrastructure
dotnet ef database update --startup-project ../Archu.Api
```

---

## 🔐 JWT Token Implementation

### Configuration

**appsettings.json:**
```json
{
  "Jwt": {
    "Secret": "your-secret-key-at-least-32-characters-long-for-security",
    "Issuer": "https://localhost:7123",
    "Audience": "https://localhost:7123",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

**⚠️ Security Requirements:**
- Secret must be ≥32 characters (256 bits minimum)
- Never commit secrets to source control
- Use Azure Key Vault or environment variables in production

### JWT Token Service

**JwtTokenService.cs** (`Infrastructure/Authentication/`):

**Key Features:**
- ✅ HS256 algorithm (HMAC-SHA256)
- ✅ Standard JWT claims (`sub`, `email`, `jti`, `iat`)
- ✅ Role claims for authorization
- ✅ Zero clock skew (strict expiration)
- ✅ Secure refresh tokens (cryptographically random)

**Access Token Structure:**
```json
{
  "sub": "user-guid",
  "email": "user@example.com",
  "unique_name": "username",
  "role": ["Admin", "User"],
  "iss": "https://localhost:7123",
  "aud": "https://localhost:7123",
  "exp": 1234571490
}
```

**Methods:**
```csharp
string GenerateAccessToken(string userId, string email, string userName, IEnumerable<string> roles);
string GenerateRefreshToken();
ClaimsPrincipal? ValidateToken(string token);
TimeSpan GetAccessTokenExpiration();
DateTime GetAccessTokenExpiryUtc();
```

### Refresh Token Handler

**RefreshTokenHandler.cs** (`Infrastructure/Authentication/`):

**Security Features:**
- ✅ Token rotation (new token on each refresh)
- ✅ Expiry validation
- ✅ Automatic cleanup of expired tokens
- ✅ Audit logging

**Methods:**
```csharp
(string, DateTime) GenerateAndStoreRefreshToken(ApplicationUser user);
bool ValidateRefreshToken(ApplicationUser user, string refreshToken);
void RevokeRefreshToken(ApplicationUser user);
(string, DateTime) RotateRefreshToken(ApplicationUser user);
```

### Setup in Program.cs

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

jwtOptions.Validate();

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

// Add middleware
app.UseAuthentication();
app.UseAuthorization();
```

---

## 👤 CurrentUser Service

### Overview

Provides access to authenticated user information from HTTP context throughout the application.

**Location**: `Infrastructure/Authentication/HttpContextCurrentUser.cs`

### Key Features

- ✅ Multi-provider support (JWT, OIDC, Azure AD, Identity)
- ✅ Multiple claim type support
- ✅ Comprehensive logging
- ✅ Error handling for missing HTTP context
- ✅ Case-insensitive role checking

### Supported Identity Providers

- ASP.NET Core Identity (standard .NET claims)
- JWT tokens (custom implementation)
- OpenID Connect (OIDC) (`sub`, `email`, `role` claims)
- Azure Active Directory (`oid`, `roles` claims)
- WS-Federation (legacy claims)

### Implementation

**HttpContextCurrentUser.cs:**
```csharp
public class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<HttpContextCurrentUser> _logger;

    public HttpContextCurrentUser(
        IHttpContextAccessor httpContextAccessor,
        ILogger<HttpContextCurrentUser> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string? UserId
    {
        get
        {
            var user = GetClaimsPrincipal();
            if (user is null || !user.Identity?.IsAuthenticated == true)
            {
                _logger.LogDebug("No authenticated user in current HTTP context");
                return null;
            }
            
            // Try multiple claim types for compatibility
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? user.FindFirstValue("sub")
                      ?? user.FindFirstValue("oid");
            
            return userId;
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            var user = GetClaimsPrincipal();
            return user?.Identity?.IsAuthenticated == true;
        }
    }

    public bool IsInRole(string role)
    {
        var user = GetClaimsPrincipal();
        if (user is null || !IsAuthenticated) return false;
        
        return user.IsInRole(role);
    }

    public IEnumerable<string> GetRoles()
    {
        var user = GetClaimsPrincipal();
        if (user is null) return Enumerable.Empty<string>();
        
        // Support multiple role claim types
        return user.FindAll(ClaimTypes.Role)
            .Union(user.FindAll("role"))
            .Union(user.FindAll("roles"))
            .Select(c => c.Value)
            .Distinct();
    }

    private ClaimsPrincipal? GetClaimsPrincipal()
    {
        try
        {
            return _httpContextAccessor.HttpContext?.User;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accessing HTTP context user");
            return null;
        }
    }
}
```

### ClaimsPrincipal Extensions

**ClaimsPrincipalExtensions.cs:**
```csharp
public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
    }

    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email");
    }

    public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role)
            .Union(principal.FindAll("role"))
            .Select(c => c.Value);
    }

    public static bool HasRole(this ClaimsPrincipal principal, string role)
    {
        return principal.IsInRole(role);
    }

    public static bool HasAnyRole(this ClaimsPrincipal principal, params string[] roles)
    {
        return roles.Any(role => principal.IsInRole(role));
    }
}
```

### Setup in Program.cs

```csharp
// Required: HTTP Context Accessor
builder.Services.AddHttpContextAccessor();

// Register Current User Service
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
```

---

## 🔒 Security Features

### Password Security

- BCrypt hashing (12 rounds by default)
- Never store plain text passwords
- Security stamp for token invalidation

### Token Security

**Access Tokens:**
- Short expiration (15-60 minutes recommended)
- Stateless (validated by signature)
- Can't be revoked until expiration

**Refresh Tokens:**
- Long expiration (7-30 days recommended)
- Opaque and cryptographically random (512 bits)
- Stored in database (enables revocation)
- Token rotation on each use (prevents replay)

### Account Protection

- Email confirmation required
- Account lockout after failed attempts
- Lockout duration: 15 minutes
- Maximum failed attempts: 5

---

## 🚀 Usage Examples

### 1. Register New User

```csharp
public async Task<Result<AuthenticationResult>> RegisterAsync(
    string email,
    string password,
    string userName,
    CancellationToken ct = default)
{
    // Hash password
    var passwordHash = _passwordHasher.HashPassword(password);
    
    // Create user
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
    await _unitOfWork.SaveChangesAsync(ct);
    
    // Generate tokens
    var roles = new[] { ApplicationRoles.User };
    var accessToken = _jwtTokenService.GenerateAccessToken(
        user.Id.ToString(), user.Email, user.UserName, roles);
    
    var (refreshToken, refreshExpiresAt) = 
        _refreshTokenHandler.GenerateAndStoreRefreshToken(user);
    await _unitOfWork.SaveChangesAsync(ct);
    
    return Result<AuthenticationResult>.Success(new AuthenticationResult
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        TokenType = "Bearer"
    });
}
```

### 2. Login

```csharp
public async Task<Result<AuthenticationResult>> LoginAsync(
    string email,
    string password,
    CancellationToken ct = default)
{
    var user = await _userRepository.GetByEmailAsync(email, ct);
    if (user is null)
        return Result<AuthenticationResult>.Failure("Invalid credentials");
    
    if (user.IsLockedOut)
        return Result<AuthenticationResult>.Failure("Account locked");
    
    var passwordValid = _passwordHasher.VerifyPassword(password, user.PasswordHash);
    
    if (!passwordValid)
    {
        user.AccessFailedCount++;
        if (user.AccessFailedCount >= 5)
        {
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
        }
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<AuthenticationResult>.Failure("Invalid credentials");
    }
    
    user.AccessFailedCount = 0;
    user.LockoutEnd = null;
    
    var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
    var accessToken = _jwtTokenService.GenerateAccessToken(
        user.Id.ToString(), user.Email, user.UserName, roles);
    
    var (refreshToken, refreshExpiresAt) = 
        _refreshTokenHandler.RotateRefreshToken(user);
    await _unitOfWork.SaveChangesAsync(ct);
    
    return Result<AuthenticationResult>.Success(new AuthenticationResult
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        TokenType = "Bearer"
    });
}
```

### 3. Using CurrentUser in Command Handlers

```csharp
public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<ProductDto>> Handle(
        UpdateProductCommand request,
        CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            return Result<ProductDto>.Failure("Authentication required");

        var userId = _currentUser.UserId;
        
        if (!_currentUser.IsInRole(ApplicationRoles.Admin))
            return Result<ProductDto>.Failure("Admin access required");

        // ... rest of implementation
    }
}
```

### 4. Using CurrentUser in Controllers

```csharp
[Authorize]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly IMediator _mediator;

    [HttpGet("my-products")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetMyProducts()
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var query = new GetUserProductsQuery(userId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProduct(Guid id)
    {
        if (!_currentUser.IsInRole(ApplicationRoles.Admin))
            return Forbid();

        // ... implementation
    }
}
```

---

## 🧪 Testing

### Unit Tests - JWT Token Service

```csharp
[Fact]
public void GenerateAccessToken_WithValidParameters_ReturnsValidJwt()
{
    var jwtOptions = new JwtOptions
    {
        Secret = "ThisIsATestSecretKeyThatIsAtLeast32CharactersLong!",
        Issuer = "test-issuer",
        Audience = "test-audience",
        AccessTokenExpirationMinutes = 60
    };
    
    var tokenService = new JwtTokenService(Options.Create(jwtOptions));
    
    var token = tokenService.GenerateAccessToken(
        userId: "user-123",
        email: "test@example.com",
        userName: "testuser",
        roles: new[] { "User", "Admin" });
    
    Assert.NotNull(token);
    Assert.NotEmpty(token);
    
    var handler = new JwtSecurityTokenHandler();
    var jwtToken = handler.ReadJwtToken(token);
    
    Assert.Equal("user-123", jwtToken.Subject);
    Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
}
```

### Unit Tests - CurrentUser Service

```csharp
[Fact]
public void UserId_WhenAuthenticated_ReturnsUserId()
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "user-123"),
        new Claim(ClaimTypes.Email, "test@example.com")
    };
    var identity = new ClaimsIdentity(claims, "TestAuth");
    var principal = new ClaimsPrincipal(identity);

    var httpContext = new DefaultHttpContext { User = principal };
    var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
    var logger = new NullLogger<HttpContextCurrentUser>();

    var currentUser = new HttpContextCurrentUser(httpContextAccessor, logger);
    
    Assert.Equal("user-123", currentUser.UserId);
}
```

### Integration Tests

```csharp
[Fact]
public async Task Login_WithValidCredentials_ReturnsTokens()
{
    var client = _factory.CreateClient();
    var request = new LoginRequest
    {
        Email = "test@example.com",
        Password = "TestPassword123!"
    };
    
    var response = await client.PostAsJsonAsync("/api/v1/auth/login", request);
    
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<AuthenticationResult>();
    
    Assert.NotNull(result);
    Assert.NotEmpty(result.AccessToken);
    Assert.NotEmpty(result.RefreshToken);
}
```

---

## 🛠️ Troubleshooting

### "IHttpContextAccessor not registered"

**Solution:** Add to Program.cs:
```csharp
builder.Services.AddHttpContextAccessor();
```

### "UserId is always null"

**Causes:**
1. User not authenticated
2. Missing NameIdentifier claim in token
3. Authentication middleware not configured

**Solution:**
```csharp
// Ensure middleware order
app.UseAuthentication(); // Before UseAuthorization
app.UseAuthorization();

// Check token includes user ID claim
var token = _jwtTokenService.GenerateAccessToken(
    userId: user.Id.ToString(), // Must be provided
    email: user.Email,
    userName: user.UserName,
    roles: roles);
```

### "JWT Secret must be at least 32 characters"

**Solution:** Generate secure secret:
```bash
# PowerShell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | % {[char]$_})

# Linux/Mac
openssl rand -base64 32
```

### "401 Unauthorized" on protected endpoints

**Solution:**
1. Ensure authentication middleware is registered
2. Check token is sent in Authorization header: `Authorization: Bearer <token>`
3. Verify token hasn't expired
4. Confirm correct signing secret

---

## ✅ Verification Checklist

Before deploying:

- [ ] IHttpContextAccessor registered
- [ ] ICurrentUser registered as Scoped
- [ ] JWT configuration validated
- [ ] Authentication middleware configured
- [ ] JWT tokens include user ID claim
- [ ] JWT tokens include role claims
- [ ] Migration applied to database
- [ ] Default roles seeded
- [ ] Password hashing implemented
- [ ] Refresh token rotation working
- [ ] Account lockout configured
- [ ] Security logging enabled
- [ ] Unit tests passing
- [ ] Integration tests passing

---

## 📚 Related Documentation

- [Authentication Guide](./AUTHENTICATION_GUIDE.md) - User-facing authentication guide (includes security architecture)
- [Authorization Guide](./AUTHORIZATION_GUIDE.md) - Role and permission-based access control
- [Architecture Guide](./ARCHITECTURE.md) - Clean Architecture overview

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Status**: ✅ Ready for Production

---

## Summary

This implementation provides:

- ✅ Complete database schema for authentication
- ✅ Secure JWT token generation and validation
- ✅ Multi-provider CurrentUser service
- ✅ Refresh token rotation
- ✅ Account lockout protection
- ✅ Optimistic concurrency control
- ✅ Automatic audit tracking
- ✅ Comprehensive error handling
- ✅ Production-ready security features
