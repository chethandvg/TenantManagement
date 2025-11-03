# UI Authorization Guide

This document explains how to use permission-based authorization in the Blazor UI to control what users can see and do.

## Overview

The UI authorization system reads permissions from JWT claims, eliminating the need to make API calls every time you need to check permissions. All checks are performed client-side against the authenticated user's claims.

**Important**: Always use **permission-based authorization** for frontend and API access control. Role-based checks are provided for backward compatibility but should be avoided in favor of granular permission checks.

## Architecture

The authorization constants (`CustomClaimTypes`, `PermissionNames`, `RoleNames`) are located in `Archu.SharedKernel.Constants` to maintain Clean Architecture principles:

```
External Apps (UI, Mobile, 3rd Party)
         ↓
    [Contracts]  ← Public API surface (references SharedKernel)
         ↓
   [Application]  ← Use cases
         ↓
    [Infrastructure] ← Data access
         ↓
    [Domain]  ← Core business logic (references SharedKernel)
         ↓
    [SharedKernel]  ← Shared constants and cross-cutting concerns (NO dependencies)
```

This ensures:
- ✅ Domain has minimal dependencies (only SharedKernel)
- ✅ SharedKernel has zero dependencies
- ✅ Client code can access constants through Contracts (which references SharedKernel)
- ✅ No circular dependencies
- ✅ Pure Clean Architecture with proper layering

## Key Components

### 1. ClaimsPrincipalAuthorizationExtensions

Extension methods for `ClaimsPrincipal` that make it easy to check permissions and roles:

```csharp
using Archu.ApiClient.Authentication.Extensions;

// Check a single permission
bool canDelete = user.HasPermission(PermissionNames.Products.Delete);

// Check if user has any of the specified permissions
bool hasAccess = user.HasAnyPermission(
    PermissionNames.Products.Create,
    PermissionNames.Products.Update
);

// Check if user has all of the specified permissions
bool hasFullAccess = user.HasAllPermissions(
    PermissionNames.Products.Create,
    PermissionNames.Products.Update,
    PermissionNames.Products.Delete
);

// Check roles (avoid - use permissions instead)
// Role checks are available but not recommended for authorization
bool isAdmin = user.HasRole(RoleNames.Administrator);
bool hasManagementAccess = user.HasAnyRole(RoleNames.Manager, RoleNames.Administrator);
```

### 2. IUiAuthorizationService

A service that provides async methods for permission checks in Blazor components:

```csharp
@inject IUiAuthorizationService AuthorizationService

// In component code
protected override async Task OnInitializedAsync()
{
    // Use permission checks (recommended)
    var canCreate = await AuthorizationService.HasPermissionAsync(PermissionNames.Products.Create);
    
    // Role checks available but not recommended
    var isManager = await AuthorizationService.HasRoleAsync(RoleNames.Manager);
}
```

### 3. Authorized Component

A reusable component for declarative permission-based rendering:

**Best Practice**: Always use `Permission`, `AnyPermission`, or `AllPermissions` parameters. Avoid `Role`, `AnyRole`, and `AllRoles` parameters.

**Parameter Priority**: If multiple parameters are provided, only the first non-null parameter is evaluated in this order:
1. `Permission` (recommended)
2. `AnyPermission` 
3. `AllPermissions`
4. `Role` (not recommended)
5. `AnyRole` (not recommended)
6. `AllRoles` (not recommended)

```razor
<!-- Show content only if user has the permission (recommended approach) -->
<Authorized Permission="@PermissionNames.Products.Create">
    <MudButton>Create Product</MudButton>
</Authorized>

<!-- Show content if user has any of the permissions -->
<Authorized AnyPermission="@new[] { PermissionNames.Products.Update, PermissionNames.Products.Delete }">
    <MudButton>Edit or Delete</MudButton>
</Authorized>

<!-- Show content if user has all permissions -->
<Authorized AllPermissions="@new[] { PermissionNames.Products.Update, PermissionNames.Products.Delete }">
    <MudButton>Edit and Delete</MudButton>
</Authorized>

<!-- Show alternative content when not authorized -->
<Authorized Permission="@PermissionNames.Products.Delete">
    <ChildContent>
        <MudButton Color="Color.Error">Delete</MudButton>
    </ChildContent>
    <NotAuthorized>
        <MudTooltip Text="You don't have permission to delete products">
            <MudButton Color="Color.Error" Disabled="true">Delete</MudButton>
        </MudTooltip>
    </NotAuthorized>
</Authorized>

<!-- AVOID: Role-based authorization (use permissions instead) -->
<!-- This example is shown for reference only - prefer permission-based checks -->
<Authorized Role="@RoleNames.Administrator">
    <MudButton>Admin Panel</MudButton>
</Authorized>
```

## Usage Examples

All examples below use **permission-based authorization**, which is the recommended approach.

### Example 1: Products Page

```razor
@page "/products"
@attribute [Authorize]
@using Archu.SharedKernel.Constants

<MudText Typo="Typo.h3">Products</MudText>

<!-- Create button - only show if user has create permission -->
<Authorized Permission="@PermissionNames.Products.Create">
    <MudButton Variant="Variant.Filled" Color="Color.Primary">
        Create Product
    </MudButton>
</Authorized>

<!-- Product cards with conditional buttons -->
@foreach (var product in products)
{
    <MudCard>
        <MudCardContent>
            <MudText>@product.Name</MudText>
        </MudCardContent>
        <MudCardActions>
            <!-- View is available to everyone -->
            <MudButton>View</MudButton>
            
            <!-- Edit only for those with update permission -->
            <Authorized Permission="@PermissionNames.Products.Update">
                <MudButton>Edit</MudButton>
            </Authorized>
            
            <!-- Delete with tooltip when disabled -->
            <Authorized Permission="@PermissionNames.Products.Delete">
                <ChildContent>
                    <MudButton Color="Color.Error">Delete</MudButton>
                </ChildContent>
                <NotAuthorized>
                    <MudTooltip Text="No permission">
                        <MudButton Color="Color.Error" Disabled="true">Delete</MudButton>
                    </MudTooltip>
                </NotAuthorized>
            </Authorized>
        </MudCardActions>
    </MudCard>
}
```

### Example 2: Navigation Menu

```razor
<MudNavMenu>
    <AuthorizeView>
        <Authorized>
            <!-- Products link - only for users with read permission -->
            <Authorized Permission="@PermissionNames.Products.Read">
                <MudNavLink Href="/products">Products</MudNavLink>
            </Authorized>
            
            <!-- Admin section - for users with admin permissions -->
            <Authorized AnyPermission="@adminPermissions">
                <MudNavGroup Title="Admin">
                    <Authorized Permission="@PermissionNames.Users.Read">
                        <MudNavLink Href="/admin/users">Users</MudNavLink>
                    </Authorized>
                    <Authorized Permission="@PermissionNames.Roles.Read">
                        <MudNavLink Href="/admin/roles">Roles</MudNavLink>
                    </Authorized>
                </MudNavGroup>
            </Authorized>
        </Authorized>
    </AuthorizeView>
</MudNavMenu>

@code {
    private readonly string[] adminPermissions = new[] 
    { 
        PermissionNames.Users.Read, 
        PermissionNames.Roles.Read 
    };
}
```

### Example 3: Programmatic Checks

```csharp
@inject IUiAuthorizationService AuthorizationService

@code {
    private bool canCreateProducts;
    private bool canDeleteProducts;

    protected override async Task OnInitializedAsync()
    {
        // Check individual permissions
        canCreateProducts = await AuthorizationService.HasPermissionAsync(
            PermissionNames.Products.Create);
            
        canDeleteProducts = await AuthorizationService.HasPermissionAsync(
            PermissionNames.Products.Delete);
            
        // Get current user for more complex logic
        var user = await AuthorizationService.GetCurrentUserAsync();
        if (user != null)
        {
            var permissions = user.GetPermissions();
            // Do something with permissions
        }
    }
}
```

## Important Notes

1. **Use Permissions Only**: Always use permission-based authorization for frontend and API access control
2. **Permission Names**: Always use constants from `PermissionNames` class to avoid typos
3. **No API Calls**: All checks are done locally against JWT claims - fast and offline-capable
4. **API Alignment**: UI should check the same permission the API enforces
5. **Avoid Roles**: Role-based checks are available for backward compatibility but should not be used for authorization
6. **NotAuthorized Fallback**: Use the `NotAuthorized` section to show disabled states or explanatory messages
7. **Single Parameter**: Only provide one authorization parameter to the `Authorized` component to avoid confusion

## Available Permissions

See `PermissionNames.cs` for all available permissions:

- **Products**: Read, Create, Update, Delete, Manage
- **Users**: Read, Create, Update, Delete, Manage  
- **Roles**: Read, Create, Update, Delete, Manage

## Testing

Authorization checks can be tested by creating a `ClaimsPrincipal` with appropriate claims:

```csharp
var claims = new[]
{
    new Claim(CustomClaimTypes.Permission, PermissionNames.Products.Create),
    new Claim(ClaimTypes.Role, RoleNames.User)
};
var identity = new ClaimsIdentity(claims, "Test");
var principal = new ClaimsPrincipal(identity);

// Test extensions
Assert.True(principal.HasPermission(PermissionNames.Products.Create));
Assert.True(principal.HasRole(RoleNames.User));
```
