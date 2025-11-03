# UI Authorization Guide

This document explains how to use permission-based authorization in the Blazor UI to control what users can see and do.

## Overview

The UI authorization system reads permissions and roles from JWT claims, eliminating the need to make API calls every time you need to check permissions. All checks are performed client-side against the authenticated user's claims.

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

// Check roles
bool isAdmin = user.HasRole(RoleNames.Administrator);
bool hasManagementAccess = user.HasAnyRole(RoleNames.Manager, RoleNames.Administrator);
```

### 2. IUiAuthorizationService

A service that provides async methods for permission/role checks in Blazor components:

```csharp
@inject IUiAuthorizationService AuthorizationService

// In component code
protected override async Task OnInitializedAsync()
{
    var canCreate = await AuthorizationService.HasPermissionAsync(PermissionNames.Products.Create);
    var isManager = await AuthorizationService.HasRoleAsync(RoleNames.Manager);
}
```

### 3. Authorized Component

A reusable component for declarative permission-based rendering:

```razor
<!-- Show content only if user has the permission -->
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

<!-- Role-based authorization -->
<Authorized Role="@RoleNames.Administrator">
    <MudButton>Admin Panel</MudButton>
</Authorized>
```

## Usage Examples

### Example 1: Products Page

```razor
@page "/products"
@attribute [Authorize]
@using Archu.Domain.Constants

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

1. **Permission Names**: Always use constants from `PermissionNames` class to avoid typos
2. **No API Calls**: All checks are done locally against JWT claims - fast and offline-capable
3. **API Alignment**: UI should check the same permission the API enforces
4. **Role vs Permission**: Prefer permission checks over role checks for fine-grained control
5. **NotAuthorized Fallback**: Use the `NotAuthorized` section to show disabled states or explanatory messages

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
