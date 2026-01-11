# TentMan Authorization Guide

Complete guide to role-based authorization, security restrictions, and access control in TentMan.

---

## 📚 Table of Contents

- [Overview](#overview)
- [Architecture and Constants](#architecture-and-constants)
- [Role System](#role-system)
- [Authorization Policies](#authorization-policies)
- [Security Restrictions](#security-restrictions)
- [Implementation](#implementation)
- [Best Practices](#best-practices)

---

## 🎯 Overview

### Authorization vs Authentication

| Concept | Purpose | Question |
|---------|---------|----------|
| **Authentication** | Verify identity | "Who are you?" |
| **Authorization** | Control access | "What can you do?" |

### Authorization System

TentMan uses **role-based authorization** with:
- ✅ ASP.NET Core Identity roles
- ✅ Policy-based authorization
- ✅ Attribute-based access control (`[Authorize]`)
- ✅ Custom security restrictions
- ✅ Hierarchical role system

---

## 🏗️ Architecture and Constants

### Clean Architecture Principles

TentMan follows **Clean Architecture** principles for authorization:

#### Authorization Constants Location

**✅ Centralized in Shared Layer**: `TentMan.Shared.Constants.Authorization`

All authorization-related constants are centralized in the shared layer to ensure consistency and avoid duplication:

| Constant Type | Class | Purpose | Example |
|--------------|-------|---------|---------|
| **Policy Names** | `PolicyNames` | Authorization policy identifiers | `RequireUserRole`, `CanViewTenantPortal` |
| **Role Names** | `RoleNames` | System role identifiers | `Administrator`, `Manager`, `User`, `Tenant` |
| **Permission Values** | `PermissionValues` | Permission claim values | `products:create`, `buildings:read` |
| **Claim Types** | `ClaimTypes` | Custom claim type identifiers | `permission`, `email_verified` |

**Example Usage**:
```csharp
using TentMan.Shared.Constants.Authorization;

// In controllers
[Authorize(Policy = PolicyNames.RequireManagerRole)]
public class ProductsController : ControllerBase { }

// In policy configuration
policy.RequireRole(RoleNames.Administrator, RoleNames.Manager);

// In handlers
if (context.User.HasClaim(c => c.Type == ClaimTypes.Permission))
```

#### Authorization Policy Configuration Location

**✅ API Layer**: `TentMan.Api.Authorization`

Authorization policy **configuration** (not constants) stays in the API layer:

| Component | Location | Purpose |
|-----------|----------|---------|
| Policy Configuration | `AuthorizationPolicyExtensions.cs` | Configures policies using shared constants |
| Requirements | `Requirements/` folder | Custom authorization requirements |
| Handlers | `Handlers/` folder | Authorization requirement handlers |

**Example**:
```csharp
// TentMan.Api.Authorization.AuthorizationPolicyExtensions.cs
using TentMan.Shared.Constants.Authorization;

public static void ConfigureTentManPolicies(this AuthorizationOptions options)
{
    options.AddPolicy(PolicyNames.RequireUserRole, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new MinimumRoleRequirement(RoleNames.User));
    });
}
```

### Why This Architecture?

**✅ Benefits**:
- **No Duplication**: Constants defined once in shared layer
- **Consistency**: Same constants used across all layers (Application, Infrastructure, API, UI)
- **Clean Architecture**: Business logic (Application layer) can reference shared constants without depending on API layer
- **Type Safety**: Compile-time checking for policy/role names
- **Easy Maintenance**: Change policy names in one place

**❌ Previous Problem** (Fixed):
- Authorization constants were duplicated in `TentMan.Api.Authorization.AuthorizationPolicies`
- Created ambiguity about which constants to use
- Violated DRY (Don't Repeat Yourself) principle
- Made maintenance difficult

---

## 🎭 Role System

### System Roles

TentMan defines several built-in roles:

| Role | Level | Description | Default Permissions |
|------|-------|-------------|---------------------|
| **Guest** | 0 | Unverified user | Read-only, limited access |
| **User** | 1 | Standard user | Full read, limited write |
| **Tenant** | 1 | Tenant portal user | Tenant-specific access to lease, documents |
| **Manager** | 2 | Team manager | Create/update resources |
| **Administrator** | 3 | System admin | User management, most operations |
| **SuperAdmin** | 4 | Super admin | Full system access, no restrictions |

### Role Hierarchy

```
SuperAdmin (4)
    ↓ Can do everything Administrator can do, plus:
    - Assign SuperAdmin role
    - Delete last SuperAdmin (with checks)
    
Administrator (3)
    ↓ Can do everything Manager can do, plus:
    - Manage users (create, delete)
    - Assign User/Manager roles
    - Cannot assign SuperAdmin role
    
Manager (2)
    ↓ Can do everything User can do, plus:
    - Create/update products
    - View all users (read-only)
    
User (1)
    ↓ Can do everything Guest can do, plus:
    - Access protected resources
    - Modify own account
    
Guest (0)
    - Public access only
    - Registration/login
```

### Role Assignment

**Who can assign roles**:

| Admin Role | Can Assign |
|------------|------------|
| **SuperAdmin** | All roles (Guest, User, Manager, Administrator, SuperAdmin) |
| **Administrator** | Guest, User, Manager |
| **Manager** | Cannot assign roles |

**Security rules**:
- ❌ Administrators cannot assign SuperAdmin role
- ❌ Administrators cannot assign Administrator role (to prevent privilege escalation)
- ❌ Cannot remove your own privileged roles (SuperAdmin, Administrator)
- ❌ Cannot delete the last SuperAdmin

---

## 🔐 Authorization Policies

### Built-in Policies

TentMan defines several authorization policies:

#### 1. **RequireUser**
```csharp
policy.RequireRole("User", "Manager", "Administrator", "SuperAdmin");
```

**Purpose**: Basic authenticated user  
**Usage**: Most protected endpoints

#### 2. **RequireManager**
```csharp
policy.RequireRole("Manager", "Administrator", "SuperAdmin");
```

**Purpose**: Management operations  
**Usage**: Create/update resources

#### 3. **RequireAdministrator**
```csharp
policy.RequireRole("Administrator", "SuperAdmin");
```

**Purpose**: Administrative operations  
**Usage**: User management, system settings

#### 4. **RequireSuperAdmin**
```csharp
policy.RequireRole("SuperAdmin");
```

**Purpose**: Critical system operations  
**Usage**: Role assignment, system initialization

#### 5. **RequireTenantRole** ✨ NEW!
```csharp
policy.RequireRole("Tenant");
```

**Purpose**: Tenant portal access  
**Usage**: Tenant-specific operations (lease summary, documents, move-in handover)

### Policy Configuration

**Registration** (`Program.cs`):
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireUser", policy => 
        policy.RequireRole("User", "Manager", "Administrator", "SuperAdmin"));
    
    options.AddPolicy("RequireManager", policy => 
        policy.RequireRole("Manager", "Administrator", "SuperAdmin"));
    
    options.AddPolicy("RequireAdministrator", policy => 
        policy.RequireRole("Administrator", "SuperAdmin"));
    
    options.AddPolicy("RequireSuperAdmin", policy => 
        policy.RequireRole("SuperAdmin"));
    
    options.AddPolicy("RequireTenantRole", policy => 
        policy.RequireRole("Tenant"));
});
```

### Applying Policies

#### Controller-Level Authorization
```csharp
[Authorize(Policy = "RequireAdministrator")]
[ApiController]
[Route("api/v1/admin/users")]
public class UsersController : ControllerBase
{
    // All actions require Administrator or SuperAdmin role
}
```

#### Action-Level Authorization
```csharp
[HttpPost]
[Authorize(Policy = "RequireManager")]
public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
{
    // Only Manager, Administrator, or SuperAdmin can create products
}

[HttpDelete("{id}")]
[Authorize(Policy = "RequireAdministrator")]
public async Task<IActionResult> DeleteProduct(Guid id)
{
    // Only Administrator or SuperAdmin can delete products
}
```

#### Multiple Policies
```csharp
[Authorize(Policy = "RequireUser")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        // Any authenticated user
    }

    [HttpPost]
    [Authorize(Policy = "RequireManager")] // Overrides controller policy
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        // Requires Manager or higher
    }
}
```

---

## 🛡️ Security Restrictions

### Role Assignment Restrictions

#### Restriction 1: SuperAdmin-Only Role Assignment

**Rule**: Only SuperAdmin can assign SuperAdmin or Administrator roles

**Implementation**:
```csharp
public async Task<Result> AssignRoleAsync(string userId, string roleId, string adminUserId)
{
    var role = await _roleManager.FindByIdAsync(roleId);
    var admin = await _userManager.FindByIdAsync(adminUserId);
    var adminRoles = await _userManager.GetRolesAsync(admin);

    // Check if trying to assign privileged role
    if (role.Name is "SuperAdmin" or "Administrator")
    {
        if (!adminRoles.Contains("SuperAdmin"))
        {
            return Result.Failure(
                "Only SuperAdmin can assign SuperAdmin or Administrator roles");
        }
    }

    // Proceed with assignment
    var user = await _userManager.FindByIdAsync(userId);
    await _userManager.AddToRoleAsync(user, role.Name);
    return Result.Success();
}
```

**Error Response** (403 Forbidden):
```json
{
  "success": false,
  "message": "Only SuperAdmin can assign SuperAdmin or Administrator roles"
}
```

#### Restriction 2: Cannot Remove Own Privileged Roles

**Rule**: Cannot remove SuperAdmin or Administrator role from yourself

**Implementation**:
```csharp
public async Task<Result> RemoveRoleAsync(string userId, string roleId, string adminUserId)
{
    if (userId == adminUserId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role.Name is "SuperAdmin" or "Administrator")
        {
            return Result.Failure(
                "You cannot remove your own SuperAdmin or Administrator role");
        }
    }

    // Proceed with removal
    var user = await _userManager.FindByIdAsync(userId);
    await _userManager.RemoveFromRoleAsync(user, role.Name);
    return Result.Success();
}
```

**Error Response** (400 Bad Request):
```json
{
  "success": false,
  "message": "You cannot remove your own SuperAdmin or Administrator role"
}
```

#### Restriction 3: Cannot Delete Last SuperAdmin

**Rule**: System must always have at least one SuperAdmin

**Implementation**:
```csharp
public async Task<Result> DeleteUserAsync(string userId)
{
    var user = await _userManager.FindByIdAsync(userId);
    var userRoles = await _userManager.GetRolesAsync(user);

    if (userRoles.Contains("SuperAdmin"))
    {
        var superAdminCount = await _userManager.GetUsersInRoleAsync("SuperAdmin").Count();
        if (superAdminCount <= 1)
        {
            return Result.Failure(
                "Cannot delete the last SuperAdmin. System must have at least one SuperAdmin.");
        }
    }

    await _userManager.DeleteAsync(user);
    return Result.Success();
}
```

**Error Response** (400 Bad Request):
```json
{
  "success": false,
  "message": "Cannot delete the last SuperAdmin. System must have at least one SuperAdmin."
}
```

#### Restriction 4: Cannot Delete Yourself

**Rule**: Users cannot delete their own account via Admin API

**Implementation**:
```csharp
public async Task<Result> DeleteUserAsync(string userId, string adminUserId)
{
    if (userId == adminUserId)
    {
        return Result.Failure("You cannot delete your own account");
    }

    var user = await _userManager.FindByIdAsync(userId);
    await _userManager.DeleteAsync(user);
    return Result.Success();
}
```

**Error Response** (400 Bad Request):
```json
{
  "success": false,
  "message": "You cannot delete your own account"
}
```

### Summary of Security Restrictions

| Operation | Restriction | Enforced By |
|-----------|-------------|-------------|
| Assign SuperAdmin/Administrator role | Only SuperAdmin can do this | Role check |
| Remove SuperAdmin/Administrator role | Cannot remove from yourself | User ID check |
| Delete user | Cannot delete yourself | User ID check |
| Delete user with SuperAdmin role | Cannot delete last SuperAdmin | Count check |

---

## 🏗️ Implementation

### Command Handler with Authorization

```csharp
public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ICurrentUser _currentUser;

    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken ct)
    {
        // Get current admin user
        var adminUserId = _currentUser.UserId;
        var admin = await _userManager.FindByIdAsync(adminUserId);
        var adminRoles = await _userManager.GetRolesAsync(admin);

        // Get target role
        var role = await _roleManager.FindByIdAsync(request.RoleId);
        if (role == null)
            return Result.Failure("Role not found");

        // SECURITY CHECK: Only SuperAdmin can assign privileged roles
        if (role.Name is "SuperAdmin" or "Administrator")
        {
            if (!adminRoles.Contains("SuperAdmin"))
            {
                return Result.Failure(
                    "Only SuperAdmin can assign SuperAdmin or Administrator roles");
            }
        }

        // Get target user
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return Result.Failure("User not found");

        // Check if user already has role
        if (await _userManager.IsInRoleAsync(user, role.Name))
            return Result.Failure("User already has this role");

        // Assign role
        var result = await _userManager.AddToRoleAsync(user, role.Name);
        if (!result.Succeeded)
            return Result.Failure("Failed to assign role");

        return Result.Success();
    }
}
```

### Controller with Authorization

```csharp
[ApiController]
[Route("api/v1/admin/user-roles")]
[Authorize(Policy = "RequireAdministrator")] // Base policy
public class UserRolesController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost("assign")]
    [Authorize(Policy = "RequireSuperAdmin")] // Override with stricter policy
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
    {
        var command = new AssignRoleCommand(request.UserId, request.RoleId);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { success = false, message = result.Error });

        return Ok(new { success = true, message = "Role assigned successfully" });
    }

    [HttpDelete("{userId}/roles/{roleId}")]
    public async Task<IActionResult> RemoveRole(string userId, string roleId)
    {
        var command = new RemoveRoleCommand(userId, roleId);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { success = false, message = result.Error });

        return Ok(new { success = true, message = "Role removed successfully" });
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserRoles(string userId)
    {
        var query = new GetUserRolesQuery(userId);
        var result = await _mediator.Send(query);

        return Ok(result);
    }
}
```

### ICurrentUser Service

**Interface**:
```csharp
public interface ICurrentUser
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    IEnumerable<string> Roles { get; }
}
```

**Implementation**:
```csharp
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => 
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public string? UserName => 
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;

    public bool IsAuthenticated => 
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => 
        _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;

    public IEnumerable<string> Roles => 
        _httpContextAccessor.HttpContext?.User?.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value) ?? Enumerable.Empty<string>();
}
```

---

## ✅ Best Practices

### Authorization Constants

✅ **DO**:
- **Always** use constants from `TentMan.Shared.Constants.Authorization`
- Reference `PolicyNames` for authorization policies
- Reference `RoleNames` for role checks
- Reference `PermissionValues` for permission claims
- Reference `ClaimTypes` for custom claim types
- Keep authorization configuration in the API layer
- Keep authorization constants in the shared layer

❌ **DON'T**:
- Hard-code policy names, role names, or permissions as string literals
- Duplicate constants across layers
- Define authorization constants in the API layer (use shared layer instead)
- Create separate constants in each project

**Example**:
```csharp
// ✅ CORRECT - Use shared constants
using TentMan.Shared.Constants.Authorization;

[Authorize(Policy = PolicyNames.RequireManagerRole)]
public class ProductsController : ControllerBase { }

if (user.IsInRole(RoleNames.Administrator)) { }

// ❌ INCORRECT - Hard-coded strings
[Authorize(Policy = "RequireManagerRole")]  // Don't do this!

if (user.IsInRole("Administrator")) { }  // Don't do this!
```

### Authorization

✅ **DO**:
- Use policy-based authorization
- Apply `[Authorize]` to controllers/actions
- Check authorization in business logic
- Use hierarchical roles
- Implement security restrictions
- Log authorization failures
- Test authorization rules

❌ **DON'T**:
- Rely on client-side authorization only
- Hard-code role names everywhere
- Skip authorization checks
- Allow privilege escalation
- Ignore security restrictions

### Role Management

✅ **DO**:
- Use descriptive role names
- Document role permissions
- Assign minimal required roles
- Audit role assignments
- Protect privileged roles
- Maintain at least one SuperAdmin

❌ **DON'T**:
- Give everyone Administrator role
- Allow users to assign their own roles
- Delete the last SuperAdmin
- Skip security checks

### Security

✅ **DO**:
- Validate all authorization decisions server-side
- Use `ICurrentUser` service for user context
- Implement defense in depth
- Log security events
- Review authorization policies regularly

❌ **DON'T**:
- Trust client-side role claims
- Skip business-level authorization checks
- Expose sensitive operations to unauthorized users

---

## 📚 Related Documentation

- **[AUTHENTICATION_GUIDE.md](AUTHENTICATION_GUIDE.md)** - JWT and authentication
- **[API_GUIDE.md](API_GUIDE.md)** - API endpoints and requirements
- **[GETTING_STARTED.md](GETTING_STARTED.md)** - Initial setup
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - System architecture

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Maintainer**: TentMan Development Team
