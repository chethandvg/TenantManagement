# Authorization Policy - Clean Architecture Implementation

## 🏗️ Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         API LAYER                               │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  AuthorizationPolicyExtensions.cs                        │  │
│  │  - ConfigureArchuPolicies()                              │  │
│  │  - ConfigureProductPolicies()                            │  │
│  │                                                          │  │
│  │  Uses:                                                   │  │
│  │    ✅ PermissionNames.Products.Read                      │  │
│  │    ✅ PermissionNames.Products.Create                    │  │
│  │    ✅ PermissionNames.Products.Update                    │  │
│  │    ✅ PermissionNames.Products.Delete                    │  │
│  └────────────────────────┬─────────────────────────────────┘  │
│                           │                                     │
│  ┌────────────────────────▼─────────────────────────────────┐  │
│  │  ResourceOwnerRequirementHandler.cs                      │  │
│  │  - CheckProductOwnershipAsync()                          │  │
│  │  - Uses IProductRepository from Application             │  │
│  └──────────────────────────────────────────────────────────┘  │
└───────────────────────────┬─────────────────────────────────────┘
                            │ References
                            │
┌───────────────────────────▼─────────────────────────────────────┐
│                      DOMAIN LAYER                               │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  PermissionNames.cs                                      │  │
│  │  ├─ Products { Read, Create, Update, Delete, Manage }   │  │
│  │  ├─ Users { Read, Create, Update, Delete, Manage }      │  │
│  │  └─ Roles { Read, Create, Update, Delete, Manage }      │  │
│  │                                                          │  │
│  │  Pattern: "{resource}:{action}"                          │  │
│  │  Example: "products:read", "users:manage"               │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  RolePermissionClaims.cs                                 │  │
│  │  ├─ Guest    → [products:read]                           │  │
│  │  ├─ User     → [products:read, create, update]           │  │
│  │  ├─ Manager  → [products:*, users:read]                  │  │
│  │  ├─ Admin    → [products:manage, users:manage]           │  │
│  │  └─ SuperAdmin → [*:manage]                              │  │
│  │                                                          │  │
│  │  Methods:                                                │  │
│  │    ✅ GetPermissionClaimsForRole(roleName)               │  │
│  │    ✅ GetPermissionClaimsForRoles(roleNames)             │  │
│  │    ✅ RoleHasPermissionClaim(role, permission)           │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
│  📦 Zero Dependencies ✅                                        │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔄 Data Flow

### **1. Authorization Policy Configuration (Startup)**
```
Program.cs
    ↓
ConfigureArchuPolicies()
    ↓
ConfigureProductPolicies()
    ↓
Uses: PermissionNames.Products.Read (from Domain)
    ↓
Creates: PermissionRequirement("products:read")
    ↓
Registered as: PolicyNames.Products.View
```

### **2. JWT Token Generation (Login)**
```
User Login
    ↓
Get User Roles (e.g., ["User", "Manager"])
    ↓
RolePermissionClaims.GetPermissionClaimsForRoles(roles)
    ↓
Returns: ["products:read", "products:create", "products:update", 
          "products:delete", "users:read"]
    ↓
Add Claims to JWT Token
    ↓
Token Contains: permission: "products:read", permission: "products:create", ...
```

### **3. Authorization Check (Request)**
```
HTTP Request to [Authorize(Policy = "Products.Create")]
    ↓
PermissionRequirementHandler.HandleRequirementAsync()
    ↓
Check: User has claim(permission, "products:create")?
    ↓
If YES → context.Succeed(requirement) → ✅ Access Granted
If NO  → ❌ 403 Forbidden
```

### **4. Resource Ownership Check (Request)**
```
HTTP Request to [Authorize(Policy = "ResourceOwner")]
    ↓
ResourceOwnerRequirementHandler.HandleRequirementAsync()
    ↓
Extract: User ID from Claims.NameIdentifier
Extract: Resource ID from requirement.ResourceId
    ↓
Check: Is user Admin/SuperAdmin?
    ↓ YES → ✅ Bypass ownership check (context.Succeed)
    ↓ NO
    ↓
Load Product via IProductRepository.GetByIdAsync(resourceId)
    ↓
Check: product.IsOwnedBy(userId)?
    ↓
If YES → context.Succeed(requirement) → ✅ Access Granted
If NO  → ❌ 403 Forbidden
```

---

## 📝 Code Examples

### **Example 1: Define New Permission**

```csharp
// File: src/Archu.Domain/Constants/PermissionNames.cs

public static class Orders  // NEW
{
    public const string Read = "orders:read";
    public const string Create = "orders:create";
    public const string Update = "orders:update";
    public const string Delete = "orders:delete";
    public const string Manage = "orders:manage";
}
```

### **Example 2: Assign Permission to Role**

```csharp
// File: src/Archu.Domain/Constants/RolePermissionClaims.cs

[RoleNames.Manager] = new[]
{
    PermissionNames.Products.Read,
    PermissionNames.Products.Create,
    PermissionNames.Products.Update,
    PermissionNames.Products.Delete,
    PermissionNames.Users.Read,
    PermissionNames.Orders.Read,  // NEW
    PermissionNames.Orders.Create // NEW
}
```

### **Example 3: Create Authorization Policy**

```csharp
// File: src/Archu.Api/Authorization/AuthorizationPolicyExtensions.cs

options.AddPolicy(PolicyNames.Orders.Create, policy =>
{
    policy.RequireAuthenticatedUser();
    policy.Requirements.Add(new PermissionRequirement(
        PermissionNames.Orders.Create)); // ✅ Type-safe!
});
```

### **Example 4: Protect Controller Endpoint**

```csharp
// File: src/Archu.Api/Controllers/ProductsController.cs

[Authorize(Policy = PolicyNames.Products.Create)]
[HttpPost]
public async Task<IActionResult> CreateProduct(
    [FromBody] CreateProductRequest request)
{
    var command = new CreateProductCommand(request.Name, request.Price);
    var result = await _mediator.Send(command);
    return Ok(result);
}
```

### **Example 5: Protect with Resource Ownership**

```csharp
// File: src/Archu.Api/Controllers/ProductsController.cs

[Authorize(Policy = PolicyNames.ResourceOwner)]
[HttpPut("{id}")]
public async Task<IActionResult> UpdateProduct(
    Guid id,
    [FromBody] UpdateProductRequest request)
{
    // Resource ownership check happens in handler
    // Only owner or admin can update
    var command = new UpdateProductCommand(id, request.Name, request.Price);
    var result = await _mediator.Send(command);
    return Ok(result);
}
```

---

## ✅ Validation Checklist

- [x] **PermissionNames.cs** created in Domain layer
- [x] **RolePermissionClaims.cs** created in Domain layer
- [x] **AuthorizationPolicyExtensions.cs** updated to use PermissionNames
- [x] **ResourceOwnerRequirementHandler.cs** fixed (added using directive)
- [x] **Build succeeds** without errors
- [x] **Clean Architecture** principles maintained
- [x] **Type safety** enforced with constants
- [x] **Zero magic strings** in authorization code
- [x] **Documentation** created

---

## 🎯 Key Benefits

| Benefit | Description |
|---------|-------------|
| **Type Safety** | Compile-time checking prevents typos |
| **Centralized** | Single source of truth for all permissions |
| **Maintainable** | Easy to refactor and find usages |
| **Consistent** | Same format everywhere: `{resource}:{action}` |
| **Clean Architecture** | Domain layer has zero dependencies |
| **IntelliSense** | IDE autocomplete support |
| **Testable** | Easy to unit test authorization logic |
| **Scalable** | Easy to add new permissions and resources |

---

## 🚀 Usage in Production

### **Step 1: Assign Permissions to Roles (Database Seeding)**
```csharp
var managerPermissions = RolePermissionClaims.GetPermissionClaimsForRole(
    RoleNames.Manager);

foreach (var permission in managerPermissions)
{
    // Store in database: RolePermission table
    await _rolePermissionRepository.AddAsync(new RolePermission
    {
        RoleId = managerRoleId,
        Permission = permission
    });
}
```

### **Step 2: Generate JWT Token with Permissions**
```csharp
// Get user's roles
var userRoles = await _userManager.GetRolesAsync(user);

// Get permissions for all roles
var permissions = RolePermissionClaims.GetPermissionClaimsForRoles(userRoles);

// Add permission claims to token
foreach (var permission in permissions)
{
    claims.Add(new Claim(CustomClaimTypes.Permission, permission));
}

// Generate token
var token = _jwtTokenService.GenerateToken(claims);
```

### **Step 3: Authorize Requests**
```csharp
// Authorization happens automatically via [Authorize(Policy = ...)]
// PermissionRequirementHandler checks claims against policy
```

---

**Last Updated**: 2025-01-22  
**Architecture**: Clean ✅  
**Type Safety**: Enforced ✅  
**Build Status**: Success ✅
