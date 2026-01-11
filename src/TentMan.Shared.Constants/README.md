# TentMan.Shared.Constants

Shared constants library for the TentMan Tenant Management System. This project provides a single source of truth for authorization constants and other shared values used across all application layers.

---

## 📁 Folder Structure

```
TentMan.Shared.Constants/
├── Authorization/
│   └── AuthorizationConstants.cs   # Authorization policy names, roles, permissions, claim types
└── TentMan.Shared.Constants.csproj
```

---

## 🎯 Purpose

The Shared Constants project:
- Provides a centralized location for constants used across multiple layers
- Ensures consistency in authorization policies, role names, and permissions
- Enables compile-time verification of constant values
- Maintains clean architecture by being accessible from all layers without introducing circular dependencies

---

## 🔒 Authorization Constants

All authorization-related constants are defined in `Authorization/AuthorizationConstants.cs`:

### Policy Names (`PolicyNames` class)

Authorization policy identifiers used with `[Authorize(Policy = ...)]` attributes:

```csharp
using TentMan.Shared.Constants.Authorization;

// Role-based policies
PolicyNames.RequireUserRole
PolicyNames.RequireManagerRole
PolicyNames.RequireAdminRole
PolicyNames.RequireSuperAdminRole
PolicyNames.RequireTenantRole

// Feature-based policies
PolicyNames.CanViewTenantPortal
PolicyNames.CanViewPropertyManagement
PolicyNames.CanViewBuildings
PolicyNames.CanViewTenants
PolicyNames.CanViewLeases

// Permission-based policies (Products)
PolicyNames.Products.View
PolicyNames.Products.Create
PolicyNames.Products.Update
PolicyNames.Products.Delete

// Additional policies
PolicyNames.EmailVerified
PolicyNames.TwoFactorEnabled
PolicyNames.ResourceOwner
PolicyNames.LeaseAccess
```

### Role Names (`RoleNames` class)

System role identifiers:

```csharp
RoleNames.Administrator
RoleNames.SuperAdmin
RoleNames.Manager
RoleNames.User
RoleNames.Tenant
RoleNames.Guest
```

### Permission Values (`PermissionValues` class)

Permission claim values following the `{resource}:{action}` pattern:

```csharp
// Products
PermissionValues.Products.Read
PermissionValues.Products.Create
PermissionValues.Products.Update
PermissionValues.Products.Delete

// Buildings
PermissionValues.Buildings.Read
PermissionValues.Buildings.Create
PermissionValues.Buildings.Update
PermissionValues.Buildings.Delete

// And more...
```

### Claim Types (`ClaimTypes` class)

Custom claim type identifiers:

```csharp
ClaimTypes.Permission       // "permission"
ClaimTypes.EmailVerified    // "email_verified"
ClaimTypes.TwoFactorEnabled // "two_factor_enabled"
```

---

## 📋 Usage Guidelines

### In Controllers (API Layer)

```csharp
using TentMan.Shared.Constants.Authorization;

[Authorize(Policy = PolicyNames.RequireManagerRole)]
public class AuditLogsController : ControllerBase
{
    // Manager, Administrator, and SuperAdmin can access
}

[Authorize(Policy = PolicyNames.Products.Create)]
public async Task<IActionResult> CreateProduct(...)
{
    // Requires products:create permission
}
```

### In Blazor Pages (UI Layer)

```razor
@page "/tenant/dashboard"
@using TentMan.Shared.Constants.Authorization
@attribute [Authorize(Policy = PolicyNames.RequireTenantRole)]

<h1>Tenant Dashboard</h1>
```

### In Blazor Components (UI Layer)

```razor
@using TentMan.Shared.Constants.Authorization

<AuthorizeView Policy="@PolicyNames.CanViewTenantPortal">
    <Authorized>
        <MudNavLink Href="/tenant/dashboard">Dashboard</MudNavLink>
    </Authorized>
</AuthorizeView>
```

### In Authorization Policy Configuration (API Layer)

```csharp
using TentMan.Shared.Constants.Authorization;

options.AddPolicy(PolicyNames.RequireManagerRole, policy =>
{
    policy.RequireAuthenticatedUser();
    policy.Requirements.Add(new MinimumRoleRequirement(
        RoleNames.Manager,
        RoleNames.Administrator,
        RoleNames.SuperAdmin));
});
```

---

## 🏛️ Clean Architecture Compliance

This project is designed to be referenced by all layers:

```
         ┌─────────────────┐
         │   API / UI      │  ← Uses constants
         └────────┬────────┘
                  │
         ┌────────▼────────┐
         │ Infrastructure  │  ← Uses constants
         └────────┬────────┘
                  │
         ┌────────▼────────┐
         │  Application    │  ← Uses constants
         └────────┬────────┘
                  │
         ┌────────▼────────┐
         │    Domain       │  ← Can use constants if needed
         └─────────────────┘
                  ▲
                  │
         ┌────────┴────────┐
         │ Shared.Constants│  ← No dependencies
         └─────────────────┘
```

**Dependency Rules**:
- `TentMan.Shared.Constants` has **no dependencies** on other TentMan projects
- All other projects can reference `TentMan.Shared.Constants` without violating clean architecture

---

## ✅ Benefits

1. **Single Source of Truth**: All authorization constants defined in one place
2. **Type Safety**: Compile-time verification prevents typos
3. **Consistency**: Same constants used across all layers (API, UI, Application, Infrastructure)
4. **Clean Architecture**: No circular dependencies, accessible from all layers
5. **Easy Maintenance**: Change constants in one place, update everywhere
6. **IntelliSense Support**: IDE autocomplete helps discover available constants

---

## 🚫 Anti-Patterns to Avoid

**❌ DON'T** hard-code authorization strings:
```csharp
// Bad - hard-coded string
[Authorize(Policy = "RequireManagerRole")]
[Authorize(Roles = "Manager")]
```

**✅ DO** use constants from this project:
```csharp
// Good - using shared constants
using TentMan.Shared.Constants.Authorization;

[Authorize(Policy = PolicyNames.RequireManagerRole)]
```

**❌ DON'T** duplicate constants in other projects

**✅ DO** add new constants to this project when needed

---

## 📚 Related Documentation

- **[AUTHORIZATION_GUIDE.md](../../docs/AUTHORIZATION_GUIDE.md)** - Complete authorization documentation
- **[ARCHITECTURE.md](../../docs/ARCHITECTURE.md)** - System architecture overview
- **[API README](../TentMan.Api/README.md)** - API authorization usage
- **[UI README](../TentMan.Ui/README.md)** - UI authorization usage

---

**Last Updated**: 2026-01-11  
**Maintainer**: TentMan Development Team
