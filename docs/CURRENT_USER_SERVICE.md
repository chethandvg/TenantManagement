# CurrentUser Service Implementation - Complete Guide

## Overview

This document describes the implementation of the `ICurrentUser` service that provides access to the currently authenticated user's information throughout the application. The implementation extracts user data from the HTTP context and supports multiple identity providers.

**Date**: 2025-01-22  
**Version**: 1.0  
**Status**: ✅ Completed

---

## 📋 Implementation Summary

### What Was Implemented

1. ✅ **HttpContextCurrentUser** (Infrastructure Layer)
   - Production implementation reading from HTTP context
   - Multi-provider support (JWT, OIDC, Azure AD, Identity)
   - Comprehensive logging
   - Error handling

2. ✅ **DesignTimeCurrentUser** (Infrastructure Layer)
   - Design-time implementation for EF Core tools
   - Testing scenarios support
   - Configurable values

3. ✅ **ClaimsPrincipalExtensions** (Infrastructure Layer)
   - Utility extension methods for claims access
   - Multi-provider claim type support
   - Role checking helpers

4. ✅ **DesignTimeDbContextFactory Update**
   - Updated to use DesignTimeCurrentUser
   - Cleaner implementation

---

## 🗂️ Files Created/Modified

### 1. `HttpContextCurrentUser.cs` (New)

**Location**: `src/Archu.Infrastructure/Authentication/HttpContextCurrentUser.cs`

**Purpose**: Production implementation of `ICurrentUser` that reads from HTTP context.

**Key Features**:
- ✅ **Multi-Provider Support**: Works with JWT, OIDC, Azure AD, ASP.NET Core Identity
- ✅ **Multiple Claim Types**: Tries standard claim types for each identity provider
- ✅ **Comprehensive Logging**: Logs authentication checks, role validations, and errors
- ✅ **Error Handling**: Gracefully handles missing HTTP context or claims
- ✅ **Case-Insensitive Role Checks**: Robust role comparison

**Supported Identity Providers**:
- ASP.NET Core Identity (standard .NET claims)
- JWT tokens (custom implementation)
- OpenID Connect (OIDC) (`sub`, `email`, `role` claims)
- Azure Active Directory (`oid`, `roles` claims)
- WS-Federation (legacy claims)

**Supported Claim Types**:

| Purpose | Claim Types Checked |
|---------|-------------------|
| User ID | `ClaimTypes.NameIdentifier`, `sub`, `oid`, legacy WS-Federation |
| Roles | `ClaimTypes.Role`, `role`, `roles`, legacy WS-Federation |

---

### 2. `DesignTimeCurrentUser.cs` (New)

**Location**: `src/Archu.Infrastructure/Authentication/DesignTimeCurrentUser.cs`

**Purpose**: Implementation for EF Core migrations and testing scenarios.

**Key Features**:
- ✅ **Configurable Values**: Can set user ID, authentication status, roles
- ✅ **Default Constructor**: Returns `design-time-user` with no roles
- ✅ **Custom Constructor**: Allows full customization for testing

**Usage Example**:
```csharp
// Default (for EF Core migrations)
var currentUser = new DesignTimeCurrentUser();

// Custom (for testing)
var currentUser = new DesignTimeCurrentUser(
    userId: "test-user-123",
    isAuthenticated: true,
    roles: new[] { "Admin", "User" });
```

---

### 3. `ClaimsPrincipalExtensions.cs` (New)

**Location**: `src/Archu.Infrastructure/Authentication/ClaimsPrincipalExtensions.cs`

**Purpose**: Extension methods for easy claims access from `ClaimsPrincipal`.

**Available Methods**:

```csharp
// Get user information
string? userId = User.GetUserId();
string? email = User.GetEmail();
string? userName = User.GetUserName();

// Role checking
IEnumerable<string> roles = User.GetRoles();
bool isAdmin = User.HasRole("Admin");
bool hasAny = User.HasAnyRole("Admin", "Manager");
bool hasAll = User.HasAllRoles("User", "Verified");

// Custom claims
string? customValue = User.GetClaimValue("custom_claim");
IEnumerable<string> values = User.GetClaimValues("permissions");
```

---

### 4. `DesignTimeDbContextFactory.cs` (Updated)

**Location**: `src/Archu.Infrastructure/Persistence/DesignTimeDbContextFactory.cs`

**Changes**:
- Updated to use `DesignTimeCurrentUser` instead of inline implementation
- Cleaner, more maintainable code
- Uses `SystemTimeProvider` for time operations

---

## 🔧 Configuration & Setup

### Step 1: Register in Dependency Injection

**Update `Program.cs` in API project**:

```csharp
// Register HttpContextAccessor (required for HttpContextCurrentUser)
builder.Services.AddHttpContextAccessor();

// Register ICurrentUser implementation
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
```

**Complete Setup**:
```csharp
using Archu.Application.Abstractions;
using Archu.Infrastructure.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Required: HTTP Context Accessor
builder.Services.AddHttpContextAccessor();

// Register Current User Service
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

// ... other services

var app = builder.Build();

// Ensure authentication middleware is registered
app.UseAuthentication(); // ← Must be before UseAuthorization
app.UseAuthorization();

app.MapControllers();
app.Run();
```

---

## 📝 Usage Examples

### 1. In Command Handlers

```csharp
using Archu.Application.Abstractions;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProductCommandHandler> _logger;

    public UpdateProductCommandHandler(
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        ILogger<UpdateProductCommandHandler> logger)
    {
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> Handle(
        UpdateProductCommand request,
        CancellationToken ct)
    {
        // Check if user is authenticated
        if (!_currentUser.IsAuthenticated)
        {
            _logger.LogWarning("Unauthenticated user attempted to update product");
            return Result<ProductDto>.Failure("Authentication required");
        }

        // Get current user ID for auditing
        var userId = _currentUser.UserId;
        _logger.LogInformation("User {UserId} is updating product {ProductId}",
            userId, request.Id);

        // Check user role
        if (!_currentUser.IsInRole(ApplicationRoles.Admin))
        {
            _logger.LogWarning("User {UserId} lacks admin role for product update", userId);
            return Result<ProductDto>.Failure("Admin access required");
        }

        // ... rest of implementation
    }
}
```

---

### 2. In Controllers

```csharp
using Archu.Application.Abstractions;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly IMediator _mediator;

    public ProductsController(
        ICurrentUser currentUser,
        IMediator mediator)
    {
        _currentUser = currentUser;
        _mediator = mediator;
    }

    [Authorize]
    [HttpGet("my-products")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetMyProducts()
    {
        // Get current user ID
        var userId = _currentUser.UserId;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Use user ID in query
        var query = new GetUserProductsQuery(userId);
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProduct(Guid id)
    {
        // Additional role check (redundant but demonstrates usage)
        if (!_currentUser.IsInRole(ApplicationRoles.Admin))
            return Forbid();

        // Get user info for logging
        var userId = _currentUser.UserId;
        var roles = _currentUser.GetRoles();

        // ... implementation
    }
}
```

---

### 3. In Repositories (Auditing)

```csharp
public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    private readonly ICurrentUser _currentUser;

    public ProductRepository(
        ApplicationDbContext context,
        ICurrentUser currentUser) : base(context)
    {
        _currentUser = currentUser;
    }

    public async Task<Product> AddAsync(Product product, CancellationToken ct = default)
    {
        // Set audit fields manually if needed (though DbContext handles this automatically)
        product.CreatedBy = _currentUser.UserId;

        DbSet.Add(product);
        return product;
    }
}
```

---

### 4. Using Extension Methods

```csharp
[Authorize]
[HttpGet("profile")]
public ActionResult<UserProfile> GetProfile()
{
    // Using ClaimsPrincipalExtensions
    var userId = User.GetUserId();
    var email = User.GetEmail();
    var userName = User.GetUserName();
    var roles = User.GetRoles();

    // Role checks
    var isAdmin = User.HasRole(ApplicationRoles.Admin);
    var canModerate = User.HasAnyRole(ApplicationRoles.Admin, ApplicationRoles.Moderator);

    return Ok(new UserProfile
    {
        UserId = userId,
        Email = email,
        UserName = userName,
        Roles = roles,
        IsAdmin = isAdmin,
        CanModerate = canModerate
    });
}
```

---

## 🔒 Security Features

### 1. Multiple Claim Type Support

The implementation checks multiple claim types for each piece of information, ensuring compatibility with different identity providers:

**User ID Claims**:
```csharp
ClaimTypes.NameIdentifier    // ASP.NET Core Identity, classic
"sub"                         // OIDC standard subject claim
"oid"                         // Azure AD object identifier
// + legacy WS-Federation claim
```

**Role Claims**:
```csharp
ClaimTypes.Role              // Standard .NET role
"role"                       // OIDC role claim (singular)
"roles"                      // OIDC role claim (plural, some providers)
// + legacy WS-Federation claim
```

---

### 2. Authentication Status Validation

Every property and method checks authentication status:

```csharp
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
        // ... continue
    }
}
```

---

### 3. Comprehensive Logging

All operations are logged for security auditing:

```csharp
// Authentication checks
_logger.LogDebug("User authentication check: {IsAuthenticated}", isAuthenticated);

// Role checks
_logger.LogTrace("User '{UserId}' role check for '{Role}': {IsInRole}",
    UserId ?? "unknown", role, isInRole);

// Missing claims warnings
_logger.LogWarning("Authenticated user found but no user ID claim present. Available claims: {Claims}",
    string.Join(", ", user.Claims.Select(c => c.Type)));
```

---

### 4. Error Handling

Gracefully handles missing HTTP context or exceptions:

```csharp
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
```

---

## 🧪 Testing

### Unit Tests

```csharp
[Fact]
public void UserId_WhenAuthenticated_ReturnsUserId()
{
    // Arrange
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

    // Act
    var userId = currentUser.UserId;

    // Assert
    Assert.Equal("user-123", userId);
}

[Fact]
public void IsInRole_WhenUserHasRole_ReturnsTrue()
{
    // Arrange
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "user-123"),
        new Claim(ClaimTypes.Role, "Admin")
    };
    var identity = new ClaimsIdentity(claims, "TestAuth");
    var principal = new ClaimsPrincipal(identity);

    var httpContext = new DefaultHttpContext { User = principal };
    var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
    var logger = new NullLogger<HttpContextCurrentUser>();

    var currentUser = new HttpContextCurrentUser(httpContextAccessor, logger);

    // Act
    var isAdmin = currentUser.IsInRole("Admin");

    // Assert
    Assert.True(isAdmin);
}

[Fact]
public void GetRoles_WhenUserHasMultipleRoles_ReturnsAllRoles()
{
    // Arrange
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "user-123"),
        new Claim(ClaimTypes.Role, "Admin"),
        new Claim(ClaimTypes.Role, "User"),
        new Claim("role", "Moderator") // OIDC claim
    };
    var identity = new ClaimsIdentity(claims, "TestAuth");
    var principal = new ClaimsPrincipal(identity);

    var httpContext = new DefaultHttpContext { User = principal };
    var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
    var logger = new NullLogger<HttpContextCurrentUser>();

    var currentUser = new HttpContextCurrentUser(httpContextAccessor, logger);

    // Act
    var roles = currentUser.GetRoles().ToList();

    // Assert
    Assert.Contains("Admin", roles);
    Assert.Contains("User", roles);
    Assert.Contains("Moderator", roles);
    Assert.Equal(3, roles.Count);
}

[Fact]
public void UserId_WhenNotAuthenticated_ReturnsNull()
{
    // Arrange
    var httpContext = new DefaultHttpContext();
    var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
    var logger = new NullLogger<HttpContextCurrentUser>();

    var currentUser = new HttpContextCurrentUser(httpContextAccessor, logger);

    // Act
    var userId = currentUser.UserId;

    // Assert
    Assert.Null(userId);
}
```

---

### Integration Tests

```csharp
[Fact]
public async Task GetProfile_WithValidToken_ReturnsUserInfo()
{
    // Arrange
    var client = _factory.CreateClient();
    var token = await GetValidAccessToken(); // Helper method

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await client.GetAsync("/api/v1/users/profile");

    // Assert
    response.EnsureSuccessStatusCode();
    var profile = await response.Content.ReadFromJsonAsync<UserProfile>();

    Assert.NotNull(profile);
    Assert.NotNull(profile.UserId);
    Assert.NotEmpty(profile.Email);
}

[Fact]
public async Task AdminEndpoint_WithoutAdminRole_ReturnsForbidden()
{
    // Arrange
    var client = _factory.CreateClient();
    var token = await GetUserAccessToken(); // Regular user token

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await client.DeleteAsync("/api/v1/products/123");

    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

---

## 🛠️ Troubleshooting

### Problem: "IHttpContextAccessor not registered"

**Error**: `Unable to resolve service for type 'IHttpContextAccessor'`

**Solution**: Add to `Program.cs`:
```csharp
builder.Services.AddHttpContextAccessor();
```

---

### Problem: "UserId is always null"

**Possible Causes**:
1. User not authenticated
2. Missing NameIdentifier claim in token
3. Authentication middleware not configured

**Solution**:
```csharp
// 1. Ensure authentication middleware is registered
app.UseAuthentication(); // Before UseAuthorization

// 2. Check token includes user ID claim
var token = _jwtTokenService.GenerateAccessToken(
    userId: user.Id.ToString(), // ← Must be provided
    email: user.Email,
    userName: user.UserName,
    roles: roles);

// 3. Verify token is sent in Authorization header
Authorization: Bearer <token>
```

---

### Problem: "IsInRole always returns false"

**Possible Causes**:
1. Role claim not included in token
2. Role name mismatch (case-sensitive in some providers)
3. Using wrong role claim type

**Solution**:
```csharp
// 1. Ensure roles are added to token
var token = _jwtTokenService.GenerateAccessToken(
    userId: user.Id.ToString(),
    email: user.Email,
    userName: user.UserName,
    roles: new[] { "Admin", "User" }); // ← Must include roles

// 2. Use constants for role names
if (_currentUser.IsInRole(ApplicationRoles.Admin)) // ✅ Good
if (_currentUser.IsInRole("admin")) // ❌ Case might not match

// 3. Check role claims in token (JWT.io debugger)
```

---

### Problem: "No HTTP context in background jobs"

**Explanation**: Background jobs don't have HTTP context.

**Solution**: Use `DesignTimeCurrentUser` or pass user ID explicitly:

```csharp
// In background job
public class EmailBackgroundJob
{
    private readonly IServiceProvider _serviceProvider;

    public async Task SendEmailAsync(string userId)
    {
        using var scope = _serviceProvider.CreateScope();

        // Don't use ICurrentUser here - no HTTP context
        // Instead, pass userId explicitly or use DesignTimeCurrentUser
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var user = await userRepository.GetByIdAsync(Guid.Parse(userId));

        // ... send email
    }
}
```

---

## 📊 Performance Considerations

### 1. Scoped Lifetime

`HttpContextCurrentUser` is registered as **Scoped** to match the HTTP request lifetime:

```csharp
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
```

**Why Scoped?**
- One instance per HTTP request
- Safe to cache claims during request
- Disposed after request completes

---

### 2. Lazy Evaluation

Properties use lazy evaluation - claims are extracted only when accessed:

```csharp
public string? UserId
{
    get
    {
        // Claims extracted on first access
        var user = GetClaimsPrincipal();
        // ...
    }
}
```

---

### 3. Caching Consideration

For scenarios with frequent access, consider caching:

```csharp
public class CachedHttpContextCurrentUser : ICurrentUser
{
    private string? _cachedUserId;
    private bool _userIdResolved;

    public string? UserId
    {
        get
        {
            if (!_userIdResolved)
            {
                _cachedUserId = ExtractUserIdFromClaims();
                _userIdResolved = true;
            }
            return _cachedUserId;
        }
    }
}
```

---

## ✅ Verification Checklist

Before deploying:

- [ ] `AddHttpContextAccessor()` registered in `Program.cs`
- [ ] `ICurrentUser` registered as Scoped service
- [ ] Authentication middleware configured (`UseAuthentication()`)
- [ ] JWT tokens include user ID claim (`sub` or `NameIdentifier`)
- [ ] JWT tokens include role claims if using role-based authorization
- [ ] Tested with authenticated requests
- [ ] Tested with unauthenticated requests
- [ ] Tested role-based authorization
- [ ] Logging configured for security auditing
- [ ] Unit tests written for `HttpContextCurrentUser`
- [ ] Integration tests written for protected endpoints

---

## 📚 Related Documentation

- [JWT Token Implementation](./JWT_TOKEN_IMPLEMENTATION.md)
- [Infrastructure Authentication Setup](./INFRASTRUCTURE_AUTH_SETUP.md)
- [Application Layer Authentication](../src/Archu.Application/docs/Authentication/README.md)
- [Architecture Guide](./ARCHITECTURE.md)

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Implemented By**: Infrastructure Team  
**Status**: ✅ Ready for Production

---

## Summary

This `ICurrentUser` implementation provides:

- ✅ Multi-provider support (JWT, OIDC, Azure AD, Identity)
- ✅ Robust claim extraction with fallbacks
- ✅ Comprehensive logging for security auditing
- ✅ Error handling for missing HTTP context
- ✅ Extension methods for easy claims access
- ✅ Design-time implementation for tooling
- ✅ Case-insensitive role checking
- ✅ Production-ready with best practices

The service seamlessly integrates with your authentication system and provides consistent access to user information throughout the application.
