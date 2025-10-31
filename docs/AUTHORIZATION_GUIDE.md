# Archu Authorization Guide

Complete guide to role-based authorization, permission-based policies, security restrictions, and access control in Archu.

---

## 📚 Table of Contents

- [Overview](#overview)
- [Role System](#role-system)
- [Permission System](#permission-system)
- [Authorization Policies](#authorization-policies)
- [Implementation](#implementation)
- [Usage Examples](#usage-examples)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

---

## 🎯 Overview

### Authorization vs Authentication

| Concept | Purpose | Question | Layer |
|---------|---------|----------|-------|
| **Authentication** | Verify identity | "Who are you?" | Handled first |
| **Authorization** | Control access | "What can you do?" | Handled second |

### Authorization System

Archu uses a **two-tier authorization system**:
- ✅ **Role-Based Access Control (RBAC)** - Coarse-grained access based on user roles
- ✅ **Permission-Based Policies** - Fine-grained access based on specific permissions
- ✅ **Custom Requirements** - Email verification, 2FA, resource ownership
- ✅ **Hierarchical Roles** - SuperAdmin > Administrator > Manager > User > Guest
- ✅ **Policy-Based Authorization** - Declarative access control with `[Authorize]`

### Key Components

| Component | Purpose | Location |
|-----------|---------|----------|
| **PolicyNames** | Authorization policy constants | `Archu.Api/Authorization/PolicyNames.cs` |
| **AuthorizationPolicyExtensions** | Policy configuration | `Archu.Api/Authorization/AuthorizationPolicyExtensions.cs` |
| **AuthorizationRequirements** | Custom authorization requirements | `Archu.Api/Authorization/Requirements/` |
| **AuthorizationHandlers** | Requirement validation logic | `Archu.Api/Authorization/Handlers/` |
| **RoleNames** | Role constants | `Archu.Domain/Constants/RoleNames.cs` |
| **PermissionNames** | Permission constants | `Archu.Domain/Constants/PermissionNames.cs` |

---

## 🎭 Role System

### System Roles

Archu defines **5 built-in roles** in a hierarchical structure:

| Role | Level | Description | Typical Permissions |
|------|-------|-------------|---------------------|
| **SuperAdmin** | 4 | System administrator | Full system access, all operations |
| **Administrator** | 3 | Application admin | User management, most operations |
| **Manager** | 2 | Team manager | Create/update resources, team management |
| **User** | 1 | Standard user | Read access, modify own data |
| **Guest** | 0 | Unauthenticated | Public access only |

### Role Hierarchy

```
SuperAdmin (Level 4)
  ↓ Includes all Administrator permissions, plus:
  ├─ Can assign SuperAdmin role
  ├─ Can manage system configuration
  ├─ Full audit log access
  └─ No restrictions

Administrator (Level 3)
  ↓ Includes all Manager permissions, plus:
  ├─ Create/delete users
  ├─ Assign User/Manager roles (NOT SuperAdmin)
  ├─ Manage application settings
  └─ View system logs

Manager (Level 2)
  ↓ Includes all User permissions, plus:
  ├─ Create/update products
  ├─ View all users (read-only)
  └─ Manage team resources

User (Level 1)
  ↓ Includes all Guest permissions, plus:
  ├─ Access protected resources
  ├─ Modify own account
  ├─ View products
  └─ Basic CRUD on own data

Guest (Level 0)
  ├─ Public access only
  ├─ Registration
  └─ Login
```

### Role Constants

**Location:** `src/Archu.Domain/Constants/RoleNames.cs`

```csharp
public static class RoleNames
{
    public const string Guest = "Guest";
    public const string User = "User";
    public const string Manager = "Manager";
    public const string Administrator = "Administrator";
    public const string SuperAdmin = "SuperAdmin";

    // Helper arrays
  public static readonly string[] All = 
    [
        Guest, User, Manager, Administrator, SuperAdmin
    ];

    public static readonly string[] AdminRoles = 
    [
        Administrator, SuperAdmin
    ];

    public static readonly string[] ManagerAndAbove = 
    [
        Manager, Administrator, SuperAdmin
    ];
}
```

### Role Assignment Rules

**Who can assign roles:**

| Assigner Role | Can Assign Roles |
|---------------|------------------|
| **SuperAdmin** | All roles (Guest, User, Manager, Administrator, SuperAdmin) |
| **Administrator** | Guest, User, Manager (NOT Administrator or SuperAdmin) |
| **Manager** | None (cannot assign roles) |
| **User** | None |
| **Guest** | None |

**Security Restrictions:**
- ❌ Administrators **cannot** assign Administrator or SuperAdmin roles (prevents privilege escalation)
- ❌ Users **cannot** remove their own SuperAdmin or Administrator role
- ❌ **Cannot** delete the last SuperAdmin in the system
- ❌ **Cannot** assign roles to deleted users
- ✅ Users can have **multiple roles** simultaneously

---

## 🔐 Permission System

### Permission Structure

Permissions follow the pattern: `{resource}:{action}`

**Example:** `products:create`, `users:delete`, `roles:read`

### Permission Categories

#### 1. Product Permissions

**Location:** `PermissionNames.Products`

| Permission | Value | Description |
|------------|-------|-------------|
| **Read** | `products:read` | View products |
| **Create** | `products:create` | Create new products |
| **Update** | `products:update` | Update existing products |
| **Delete** | `products:delete` | Delete products |
| **Manage** | `products:manage` | All product operations |

#### 2. User Management Permissions

**Location:** `PermissionNames.Users`

| Permission | Value | Description |
|------------|-------|-------------|
| **Read** | `users:read` | View users |
| **Create** | `users:create` | Create new users |
| **Update** | `users:update` | Update existing users |
| **Delete** | `users:delete` | Delete users |
| **Manage** | `users:manage` | All user operations |

#### 3. Role Management Permissions

**Location:** `PermissionNames.Roles`

| Permission | Value | Description |
|------------|-------|-------------|
| **Read** | `roles:read` | View roles |
| **Create** | `roles:create` | Create new roles |
| **Update** | `roles:update` | Update existing roles |
| **Delete** | `roles:delete` | Delete roles |
| **Manage** | `roles:manage` | All role operations |

### Permission Constants

**Location:** `src/Archu.Domain/Constants/PermissionNames.cs`

```csharp
public static class PermissionNames
{
    public static class Products
    {
        public const string Read = "products:read";
        public const string Create = "products:create";
        public const string Update = "products:update";
        public const string Delete = "products:delete";
        public const string Manage = "products:manage";
    }

    public static class Users
    {
        public const string Read = "users:read";
        public const string Create = "users:create";
        public const string Update = "users:update";
        public const string Delete = "users:delete";
    public const string Manage = "users:manage";
    }

    public static class Roles
{
        public const string Read = "roles:read";
     public const string Create = "roles:create";
        public const string Update = "roles:update";
        public const string Delete = "roles:delete";
  public const string Manage = "roles:manage";
    }

    // Helper methods
    public static string[] GetAllProductPermissions() => new[]
    {
        Products.Read, Products.Create, Products.Update, 
        Products.Delete, Products.Manage
    };

    public static string[] GetAllPermissions()
    {
        return GetAllProductPermissions()
            .Concat(GetAllUserPermissions())
   .Concat(GetAllRolePermissions())
            .ToArray();
    }
}
```

---

## 📋 Authorization Policies

### Policy Overview

**Location:** `src/Archu.Api/Authorization/PolicyNames.cs`

Archu defines **11 authorization policies**:

| Policy Category | Policy Name | Description |
|-----------------|-------------|-------------|
| **Email & MFA** | EmailVerified | Requires email confirmation |
| **Email & MFA** | TwoFactorEnabled | Requires 2FA enabled |
| **Role-Based** | RequireUserRole | Requires User role or higher |
| **Role-Based** | RequireManagerRole | Requires Manager role or higher |
| **Role-Based** | RequireAdminRole | Requires Administrator role or higher |
| **Role-Based** | RequireSuperAdminRole | Requires SuperAdmin role |
| **Ownership** | ResourceOwner | User owns the resource |
| **Products** | Products.View | Can view products |
| **Products** | Products.Create | Can create products |
| **Products** | Products.Update | Can update products |
| **Products** | Products.Delete | Can delete products |

### Policy Constants

**Location:** `src/Archu.Api/Authorization/PolicyNames.cs`

```csharp
public static class PolicyNames
{
    // Email & MFA policies
    public const string EmailVerified = "EmailVerified";
    public const string TwoFactorEnabled = "TwoFactorEnabled";

  // Role-based policies
    public const string RequireUserRole = "RequireUserRole";
    public const string RequireManagerRole = "RequireManagerRole";
    public const string RequireAdminRole = "RequireAdminRole";
    public const string RequireSuperAdminRole = "RequireSuperAdminRole";

    // Resource ownership policy
    public const string ResourceOwner = "ResourceOwner";

    // Product policies (permission-based)
    public static class Products
    {
        public const string View = "Products.View";
    public const string Create = "Products.Create";
        public const string Update = "Products.Update";
        public const string Delete = "Products.Delete";
    }
}
```

### Policy Configuration

**Location:** `src/Archu.Api/Authorization/AuthorizationPolicyExtensions.cs`

```csharp
public static void ConfigureArchuPolicies(this AuthorizationOptions options)
{
    // Email verification policy
    options.AddPolicy(PolicyNames.EmailVerified, policy =>
    {
    policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new EmailVerifiedRequirement());
    });

    // Two-factor authentication policy
    options.AddPolicy(PolicyNames.TwoFactorEnabled, policy =>
    {
    policy.RequireAuthenticatedUser();
   policy.Requirements.Add(new TwoFactorEnabledRequirement());
    });

    // Role-based policies (hierarchical)
    options.AddPolicy(PolicyNames.RequireUserRole, policy =>
    {
   policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new MinimumRoleRequirement(RoleNames.User));
    });

  options.AddPolicy(PolicyNames.RequireManagerRole, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new MinimumRoleRequirement(RoleNames.Manager));
    });

    options.AddPolicy(PolicyNames.RequireAdminRole, policy =>
    {
  policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new MinimumRoleRequirement(RoleNames.Administrator));
    });

    options.AddPolicy(PolicyNames.RequireSuperAdminRole, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new MinimumRoleRequirement(RoleNames.SuperAdmin));
    });

    // Resource ownership policy
options.AddPolicy(PolicyNames.ResourceOwner, policy =>
    {
   policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new ResourceOwnerRequirement());
    });

    // Permission-based product policies
    options.AddPolicy(PolicyNames.Products.View, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new PermissionRequirement(PermissionNames.Products.Read));
    });

    options.AddPolicy(PolicyNames.Products.Create, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new PermissionRequirement(PermissionNames.Products.Create));
    });

    options.AddPolicy(PolicyNames.Products.Update, policy =>
    {
     policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new PermissionRequirement(PermissionNames.Products.Update));
    });

    options.AddPolicy(PolicyNames.Products.Delete, policy =>
    {
        policy.RequireAuthenticatedUser();
    policy.Requirements.Add(new PermissionRequirement(PermissionNames.Products.Delete));
    });
}
```

### Custom Requirements

#### EmailVerifiedRequirement

**Purpose:** Ensures user's email is confirmed

**Implementation:**
```csharp
public class EmailVerifiedRequirement : IAuthorizationRequirement { }

public class EmailVerifiedRequirementHandler 
    : AuthorizationHandler<EmailVerifiedRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
      EmailVerifiedRequirement requirement)
    {
        var emailConfirmed = context.User.FindFirst("email_verified")?.Value;

    if (emailConfirmed == "true")
        {
     context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

#### MinimumRoleRequirement

**Purpose:** Hierarchical role checking

**Implementation:**
```csharp
public class MinimumRoleRequirement : IAuthorizationRequirement
{
    public string MinimumRole { get; }
    
    public MinimumRoleRequirement(string minimumRole)
    {
        MinimumRole = minimumRole;
    }
}

public class MinimumRoleRequirementHandler 
    : AuthorizationHandler<MinimumRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumRoleRequirement requirement)
    {
   var userRoles = context.User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
      .ToList();

   // Check if user has minimum required role or higher
     if (HasMinimumRole(userRoles, requirement.MinimumRole))
     {
 context.Succeed(requirement);
}

        return Task.CompletedTask;
    }

    private bool HasMinimumRole(List<string> userRoles, string minimumRole)
    {
     var roleHierarchy = new Dictionary<string, int>
 {
            [RoleNames.Guest] = 0,
      [RoleNames.User] = 1,
      [RoleNames.Manager] = 2,
            [RoleNames.Administrator] = 3,
            [RoleNames.SuperAdmin] = 4
        };

        var minimumLevel = roleHierarchy[minimumRole];
        return userRoles.Any(role => 
   roleHierarchy.TryGetValue(role, out var level) && level >= minimumLevel);
    }
}
```

#### PermissionRequirement

**Purpose:** Fine-grained permission checking

**Implementation:**
```csharp
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    
    public PermissionRequirement(string permission)
    {
      Permission = permission;
    }
}

public class PermissionRequirementHandler 
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var permissions = context.User.FindAll("permission")
      .Select(c => c.Value)
.ToList();

        if (permissions.Contains(requirement.Permission))
   {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

#### ResourceOwnerRequirement

**Purpose:** Verify user owns the resource

**Implementation:**
```csharp
public class ResourceOwnerRequirement : IAuthorizationRequirement { }

public class ResourceOwnerRequirementHandler 
    : AuthorizationHandler<ResourceOwnerRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceOwnerRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
   
        // Get resource from context
      if (context.Resource is IHasOwner resource)
        {
  if (resource.OwnerId.ToString() == userId)
    {
                context.Succeed(requirement);
          }
        }

        return Task.CompletedTask;
    }
}
```

---

## 🛠️ Implementation

### 1. Service Registration

**Location:** `src/Archu.Api/Program.cs`

```csharp
// Register authorization handlers
builder.Services.AddAuthorizationHandlers();

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    options.ConfigureArchuPolicies();
});
```

### 2. Handler Registration

**Location:** `src/Archu.Api/Authorization/AuthorizationHandlerExtensions.cs`

```csharp
public static class AuthorizationHandlerExtensions
{
    public static IServiceCollection AddAuthorizationHandlers(
        this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, EmailVerifiedRequirementHandler>();
    services.AddScoped<IAuthorizationHandler, TwoFactorEnabledRequirementHandler>();
    services.AddScoped<IAuthorizationHandler, MinimumRoleRequirementHandler>();
        services.AddScoped<IAuthorizationHandler, PermissionRequirementHandler>();
        services.AddScoped<IAuthorizationHandler, ResourceOwnerRequirementHandler>();

        return services;
  }
}
```

### 3. Middleware Configuration

**Order matters!**

```csharp
// src/Archu.Api/Program.cs
app.UseAuthentication(); // 1. FIRST - Validates JWT, populates User claims
app.UseAuthorization();  // 2. SECOND - Checks policies and roles
```

---

## 💼 Usage Examples

### 1. Role-Based Authorization

**Controller Level:**
```csharp
// Require Manager role or higher
[Authorize(Policy = PolicyNames.RequireManagerRole)]
public class ProductsController : ControllerBase
{
    // All actions require Manager role
}
```

**Action Level:**
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    // Public - no auth required
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetPublicProducts() { }

    // Requires authentication (any role)
    [Authorize]
    [HttpGet("my")]
  public async Task<IActionResult> GetMyProducts() { }

    // Requires User role or higher
    [Authorize(Policy = PolicyNames.RequireUserRole)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id) { }

  // Requires Manager role or higher
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request) { }

    // Requires Administrator role or higher
    [Authorize(Policy = PolicyNames.RequireAdminRole)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id) { }
}
```

### 2. Permission-Based Authorization

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    // Requires products:read permission
    [Authorize(Policy = PolicyNames.Products.View)]
    [HttpGet]
    public async Task<IActionResult> GetProducts() { }

    // Requires products:create permission
    [Authorize(Policy = PolicyNames.Products.Create)]
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request) { }

    // Requires products:update permission
    [Authorize(Policy = PolicyNames.Products.Update)]
  [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request) { }

    // Requires products:delete permission
    [Authorize(Policy = PolicyNames.Products.Delete)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id) { }
}
```

### 3. Multiple Policies

```csharp
// Require BOTH email verification AND Manager role
[Authorize(Policy = PolicyNames.EmailVerified)]
[Authorize(Policy = PolicyNames.RequireManagerRole)]
[HttpPost("sensitive-operation")]
public async Task<IActionResult> SensitiveOperation() { }
```

### 4. Programmatic Authorization Check

```csharp
public class ProductService
{
    private readonly IAuthorizationService _authorizationService;

    public async Task<bool> CanDeleteProduct(ClaimsPrincipal user, Product product)
    {
// Check permission-based policy
     var authResult = await _authorizationService.AuthorizeAsync(
            user, 
     product, 
            PolicyNames.Products.Delete);

        return authResult.Succeeded;
    }

    public async Task DeleteProduct(Guid productId, ClaimsPrincipal user)
    {
 var product = await _repository.GetByIdAsync(productId);

        if (product == null)
     throw new NotFoundException("Product not found");

      // Check authorization
     var authResult = await _authorizationService.AuthorizeAsync(
            user, 
       product, 
            PolicyNames.Products.Delete);

        if (!authResult.Succeeded)
            throw new ForbiddenException("You don't have permission to delete this product");

   await _repository.DeleteAsync(productId);
    }
}
```

### 5. Resource Ownership Check

```csharp
public class ProductsController : ControllerBase
{
    // Only product owner can update
    [Authorize(Policy = PolicyNames.ResourceOwner)]
    [HttpPut("{id}")]
  public async Task<IActionResult> UpdateMyProduct(
        Guid id, 
   [FromBody] UpdateProductRequest request)
    {
        var product = await _repository.GetByIdAsync(id);

        if (product == null)
    return NotFound();

        // Authorization service checks if user owns the product
        var authResult = await _authorizationService.AuthorizeAsync(
      User, 
      product, 
        PolicyNames.ResourceOwner);

        if (!authResult.Succeeded)
      return Forbid();

        // User owns the product - proceed with update
        await _productService.UpdateAsync(id, request);
     return Ok();
    }
}
```

---

## 📋 Best Practices

### ✅ DO

**1. Use Policy Names Constants:**
```csharp
// ✅ GOOD - Type-safe, refactorable
[Authorize(Policy = PolicyNames.RequireManagerRole)]

// ❌ BAD - Magic strings, error-prone
[Authorize(Policy = "RequireManagerRole")]
```

**2. Use Hierarchical Roles:**
```csharp
// ✅ GOOD - Manager includes User permissions
[Authorize(Policy = PolicyNames.RequireManagerRole)]

// ❌ BAD - Repeating role checks
[Authorize(Roles = "Manager,Administrator,SuperAdmin")]
```

**3. Separate Concerns:**
```csharp
// ✅ GOOD - Clear separation
[Authorize(Policy = PolicyNames.RequireManagerRole)]  // Role check
[Authorize(Policy = PolicyNames.EmailVerified)]       // Email check

// ❌ BAD - Mixed concerns
[Authorize(Policy = "ManagerAndEmailVerified")]
```

**4. Use Permission-Based for Fine-Grained Control:**
```csharp
// ✅ GOOD - Specific permission
[Authorize(Policy = PolicyNames.Products.Delete)]

// ❌ BAD - Too broad
[Authorize(Policy = PolicyNames.RequireAdminRole)]
```

**5. Check Authorization Programmatically When Needed:**
```csharp
// ✅ GOOD - Dynamic authorization
var authResult = await _authorizationService.AuthorizeAsync(
    user, 
    resource, 
    PolicyNames.ResourceOwner);

if (!authResult.Succeeded)
    return Forbid();
```

### ❌ DON'T

**1. Don't Use Magic Strings:**
```csharp
// ❌ BAD
[Authorize(Roles = "Manager")]
[Authorize(Policy = "Products.View")]

// ✅ GOOD
[Authorize(Policy = PolicyNames.RequireManagerRole)]
[Authorize(Policy = PolicyNames.Products.View)]
```

**2. Don't Hardcode Role Names:**
```csharp
// ❌ BAD
if (user.IsInRole("Manager")) { }

// ✅ GOOD
if (user.IsInRole(RoleNames.Manager)) { }
```

**3. Don't Bypass Authorization:**
```csharp
// ❌ BAD - No authorization check
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteProduct(Guid id)
{
    await _repository.DeleteAsync(id);
    return Ok();
}

// ✅ GOOD - Authorization required
[Authorize(Policy = PolicyNames.Products.Delete)]
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteProduct(Guid id)
{
    await _repository.DeleteAsync(id);
    return Ok();
}
```

**4. Don't Implement Authorization in Domain Layer:**
```csharp
// ❌ BAD - Authorization in domain
public class Product
{
    public bool CanBeDeletedBy(string userRole)
    {
        return userRole == "Admin";
  }
}

// ✅ GOOD - Authorization in API/Application layer
[Authorize(Policy = PolicyNames.RequireAdminRole)]
public async Task<IActionResult> DeleteProduct(Guid id) { }
```

**5. Don't Skip Email Verification for Sensitive Operations:**
```csharp
// ❌ BAD - No email verification
[Authorize(Policy = PolicyNames.RequireAdminRole)]
[HttpPost("sensitive")]
public async Task<IActionResult> SensitiveOperation() { }

// ✅ GOOD - Require email verification
[Authorize(Policy = PolicyNames.EmailVerified)]
[Authorize(Policy = PolicyNames.RequireAdminRole)]
[HttpPost("sensitive")]
public async Task<IActionResult> SensitiveOperation() { }
```

---

## 🐛 Troubleshooting

### Issue 1: "403 Forbidden" - User Has Correct Role

**Symptoms:**
- User is authenticated (not 401)
- User has the required role
- Still getting 403 Forbidden

**Diagnostic Steps:**

1. **Verify Role Claims:**
```csharp
// In controller, log user claims
var roles = User.Claims
    .Where(c => c.Type == ClaimTypes.Role)
    .Select(c => c.Value)
    .ToList();
    
_logger.LogInformation("User roles: {Roles}", string.Join(", ", roles));
```

2. **Check Policy Configuration:**
```csharp
// Verify policy exists
// src/Archu.Api/Program.cs
builder.Services.AddAuthorization(options =>
{
    options.ConfigureArchuPolicies(); // Must be called
});
```

3. **Verify Authorization Handler Registration:**
```csharp
// src/Archu.Api/Program.cs
builder.Services.AddAuthorizationHandlers(); // Must be registered
```

4. **Check Middleware Order:**
```csharp
app.UseAuthentication(); // MUST be before UseAuthorization
app.UseAuthorization();
```

---

### Issue 2: Custom Requirement Always Fails

**Symptoms:**
- Custom authorization requirement never succeeds
- Always returns 403

**Common Causes:**

1. **Handler Not Registered:**
```csharp
// ✅ MUST register handler
services.AddScoped<IAuthorizationHandler, MyCustomRequirementHandler>();
```

2. **Requirement Not Added to Policy:**
```csharp
// ✅ MUST add requirement to policy
options.AddPolicy("MyPolicy", policy =>
{
    policy.Requirements.Add(new MyCustomRequirement());
});
```

3. **Handler Doesn't Call Succeed:**
```csharp
// ❌ BAD - Requirement never succeeds
protected override Task HandleRequirementAsync(...)
{
    if (condition)
    {
        // Missing context.Succeed(requirement);
    }
    return Task.CompletedTask;
}

// ✅ GOOD
protected override Task HandleRequirementAsync(...)
{
    if (condition)
    {
        context.Succeed(requirement); // Must call
    }
    return Task.CompletedTask;
}
```

---

### Issue 3: Role Hierarchy Not Working

**Symptoms:**
- Manager role cannot access User-level endpoints
- Need to specify all roles explicitly

**Solution:**

Use `MinimumRoleRequirement` instead of direct role checking:

```csharp
// ❌ BAD - Doesn't support hierarchy
[Authorize(Roles = "User")]

// ✅ GOOD - Supports hierarchy (User, Manager, Admin, SuperAdmin)
[Authorize(Policy = PolicyNames.RequireUserRole)]
```

---

### Issue 4: Permission Claims Not Found

**Symptoms:**
- Permission-based policies always fail
- No "permission" claims in token

**Solution:**

Ensure permissions are added to JWT claims during login:

```csharp
// In AuthenticationService or JwtTokenService
var claims = new List<Claim>
{
    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new(ClaimTypes.Email, user.Email),
    new(ClaimTypes.Name, user.UserName),
};

// Add role claims
foreach (var role in userRoles)
{
    claims.Add(new Claim(ClaimTypes.Role, role));
}

// ✅ Add permission claims
foreach (var permission in userPermissions)
{
    claims.Add(new Claim("permission", permission));
}

var token = _jwtTokenService.GenerateAccessToken(claims);
```

---

### Issue 5: Resource Ownership Check Fails

**Symptoms:**
- `ResourceOwnerRequirement` always returns 403
- User should own the resource

**Diagnostic Steps:**

1. **Verify Resource Implements IHasOwner:**
```csharp
public class Product : BaseEntity, IHasOwner
{
    public Guid OwnerId { get; set; }
    
    public bool IsOwnedBy(Guid userId) => OwnerId == userId;
}
```

2. **Pass Resource to Authorization:**
```csharp
// ✅ CORRECT - Pass resource as second parameter
var authResult = await _authorizationService.AuthorizeAsync(
    User,   // ClaimsPrincipal
    product,   // Resource (IHasOwner)
    PolicyNames.ResourceOwner);

// ❌ WRONG - Missing resource
var authResult = await _authorizationService.AuthorizeAsync(
    User, 
    PolicyNames.ResourceOwner);
```

3. **Check User ID Claim:**
```csharp
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
_logger.LogInformation("User ID: {UserId}, Owner ID: {OwnerId}", userId, product.OwnerId);
```

---

## 📚 Related Documentation

- **[Authentication Guide](AUTHENTICATION_GUIDE.md)** - JWT authentication, login, tokens
- **[Password Security Guide](PASSWORD_SECURITY_GUIDE.md)** - Password policies
- **[API Guide](API_GUIDE.md)** - Complete API reference
- **[Getting Started](GETTING_STARTED.md)** - Initial setup
- **[Development Guide](DEVELOPMENT_GUIDE.md)** - Development workflow

---

## 🔄 Version History

| Version | Date | Changes |
|---------|------|---------|
| 2.0 | 2025-01-24 | ✅ **Complete verification** against codebase: Added all 11 policies, verified PolicyNames.cs, added permission system from PermissionNames.cs, complete examples and troubleshooting |
| 1.0 | 2025-01-22 | Initial authorization guide |

---

**Last Updated**: 2025-01-24  
**Version**: 2.0 ✅ **VERIFIED**  
**Maintainer**: Archu Development Team

**Questions?** See [docs/README.md](README.md) or open an [issue](https://github.com/chethandvg/archu/issues)
