# Archu UI Development Guide

Complete guide for building user interfaces with Archu's Blazor components, authorization, and loading patterns.

---

## 📚 Table of Contents

- [Overview](#overview)
- [Getting Started](#getting-started)
- [UI Authorization](#ui-authorization)
- [Loading and Error Boundaries](#loading-and-error-boundaries)
- [API Documentation](#api-documentation)
- [Best Practices](#best-practices)

---

## 🎯 Overview

### UI Stack

- **Framework**: Blazor WebAssembly
- **Component Library**: MudBlazor + Archu.Ui
- **State Management**: UiState service
- **Authorization**: Permission-based with JWT claims
- **API Client**: Archu.ApiClient with authentication

### Key Components

| Component | Purpose | Location |
|-----------|---------|----------|
| `BusyBoundary` | Loading & error handling | Archu.Ui |
| `UiState` | Global UI state | Archu.Ui |
| `ClaimsPrincipalAuthorizationExtensions` | Permission checks | Archu.ApiClient.Authentication |
| `PermissionNames` | Permission constants | Archu.SharedKernel.Constants |

---

## 🚀 Getting Started

### Register Services

```csharp
builder.Services.AddArchuUi();
```

This automatically registers:
- UiState container
- BusyState service
- Required MudBlazor components

---

## 🔐 UI Authorization

### Overview

The UI authorization system reads permissions from JWT claims, eliminating the need to make API calls every time you need to check permissions. All checks are performed client-side against the authenticated user's claims.

**Important**: Always use **permission-based authorization** for frontend and API access control. Role-based checks are provided for backward compatibility but should be avoided in favor of granular permission checks.

### Architecture

Authorization constants are located in `Archu.SharedKernel.Constants` to maintain Clean Architecture principles:

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
    [SharedKernel]  ← Shared constants (NO dependencies)
```

This ensures:
- ✅ Domain has minimal dependencies (only SharedKernel)
- ✅ SharedKernel has zero dependencies
- ✅ Client code can access constants through Contracts
- ✅ No circular dependencies
- ✅ Pure Clean Architecture with proper layering

### ClaimsPrincipalAuthorizationExtensions

Extension methods for `ClaimsPrincipal` that make it easy to check permissions and roles:

```csharp
using Archu.ApiClient.Authentication.Extensions;

// Check a single permission
bool canDelete = user.HasPermission(PermissionNames.Products.Delete);

// Check if user has any of the specified permissions
bool hasAccess = user.HasAnyPermission(
    PermissionNames.Products.Create,
    PermissionNames.Products.Update,
    PermissionNames.Products.Delete);

// Check if user has all specified permissions
bool canManage = user.HasAllPermissions(
    PermissionNames.Products.Read,
    PermissionNames.Products.Update);

// Role checks (legacy - prefer permissions)
bool isAdmin = user.HasRole(RoleNames.Administrator);
bool hasAnyRole = user.HasAnyRole(RoleNames.Administrator, RoleNames.Manager);
bool hasAllRoles = user.HasAllRoles(RoleNames.User, RoleNames.Manager);
```

### Using Authorization in Pages

#### Check Permissions in Code-Behind

```razor
@page "/products"
@inject AuthenticationStateProvider AuthStateProvider
@using Archu.SharedKernel.Constants

<PageTitle>Products</PageTitle>

@if (_canCreateProducts)
{
    <MudButton Color="Color.Primary" OnClick="CreateProduct">
        <MudIcon Icon="@Icons.Material.Filled.Add" Class="mr-2" />
        Create Product
    </MudButton>
}

@code {
    private bool _canCreateProducts;
    private bool _canDeleteProducts;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        _canCreateProducts = user.HasPermission(PermissionNames.Products.Create);
        _canDeleteProducts = user.HasPermission(PermissionNames.Products.Delete);
    }
}
```

#### Conditional Rendering Based on Permissions

```razor
@code {
    private async Task<bool> CanDeleteProduct()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        return authState.User.HasPermission(PermissionNames.Products.Delete);
    }
}

<MudTable Items="@products">
    <RowTemplate>
        <MudTd>@context.Name</MudTd>
        <MudTd>@context.Price</MudTd>
        <MudTd>
            @if (await CanDeleteProduct())
            {
                <MudIconButton Icon="@Icons.Material.Filled.Delete" 
                             Color="Color.Error"
                             OnClick="@(() => DeleteProduct(context.Id))" />
            }
        </MudTd>
    </RowTemplate>
</MudTable>
```

### Permission Constants

```csharp
// From Archu.SharedKernel.Constants
public static class PermissionNames
{
    public static class Products
    {
        public const string Create = "products.create";
        public const string Read = "products.read";
        public const string Update = "products.update";
        public const string Delete = "products.delete";
    }

    public static class Users
    {
        public const string Create = "users.create";
        public const string Read = "users.read";
        public const string Update = "users.update";
        public const string Delete = "users.delete";
        public const string ManageRoles = "users.manage_roles";
        public const string ManagePermissions = "users.manage_permissions";
    }
}

public static class RoleNames
{
    public const string Guest = "Guest";
    public const string User = "User";
    public const string Manager = "Manager";
    public const string Administrator = "Administrator";
    public const string SuperAdmin = "SuperAdmin";
}
```

### Custom Claim Types

```csharp
public static class CustomClaimTypes
{
    public const string Permission = "permission";
    public const string EmailVerified = "email_verified";
    public const string TwoFactorEnabled = "two_factor_enabled";
}
```

### Extension Methods Implementation

```csharp
public static class ClaimsPrincipalAuthorizationExtensions
{
    public static bool HasPermission(this ClaimsPrincipal principal, string permission)
    {
        return principal.HasClaim(CustomClaimTypes.Permission, permission);
    }

    public static bool HasAnyPermission(this ClaimsPrincipal principal, params string[] permissions)
    {
        return permissions.Any(permission => principal.HasPermission(permission));
    }

    public static bool HasAllPermissions(this ClaimsPrincipal principal, params string[] permissions)
    {
        return permissions.All(permission => principal.HasPermission(permission));
    }

    public static bool HasRole(this ClaimsPrincipal principal, string role)
    {
        return principal.IsInRole(role);
    }

    public static bool HasAnyRole(this ClaimsPrincipal principal, params string[] roles)
    {
        return roles.Any(role => principal.IsInRole(role));
    }

    public static bool HasAllRoles(this ClaimsPrincipal principal, params string[] roles)
    {
        return roles.All(role => principal.IsInRole(role));
    }
}
```

### Best Practices

✅ **DO**:
- Use permission-based checks for granular access control
- Check permissions in code-behind during initialization
- Cache permission checks to avoid repeated authentication state queries
- Use constants from `PermissionNames` instead of hard-coded strings
- Hide UI elements users don't have permission to use

❌ **DON'T**:
- Rely solely on UI authorization (always validate on the server)
- Use string literals for permissions (use constants)
- Check permissions on every render (cache the results)
- Use role-based checks for fine-grained access (use permissions)

### Security Note

⚠️ **Client-side authorization is for UX only!** Always validate permissions on the server. The UI authorization prevents users from seeing options they can't use, but malicious users can bypass client-side checks. Server-side validation is mandatory.

---

## 📊 Loading and Error Boundaries

### Overview

The `BusyBoundary` component and accompanying `UiState`/`BusyState` services provide a consistent way to surface loading and error feedback across pages.

### Basic Usage

Wrap your page content with `<BusyBoundary>` and trigger busy/error transitions through the injected `UiState` service:

```razor
@page "/products"
@inject UiState UiState
@inject IProductsApiClient ProductsClient

<PageTitle>Products</PageTitle>

<MudText Typo="Typo.h3" GutterBottom="true">Products</MudText>

<BusyBoundary Retry="LoadProductsAsync">
    @if (products == null || !products.Any())
    {
        <MudAlert Severity="Severity.Info">No products found.</MudAlert>
    }
    else
    {
        <MudTable Items="@products">
            <HeaderContent>
                <MudTh>Name</MudTh>
                <MudTh>Price</MudTh>
            </HeaderContent>
            <RowTemplate>
                <MudTd>@context.Name</MudTd>
                <MudTd>@context.Price.ToString("C")</MudTd>
            </RowTemplate>
        </MudTable>
    }
</BusyBoundary>

@code {
    private List<ProductDto> products = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        using var busyScope = UiState.Busy.Begin("Loading products...");
        UiState.Busy.ClearError();

        try
        {
            var response = await ProductsClient.GetProductsAsync(pageNumber: 1, pageSize: 50);

            if (response.Success && response.Data is { Items: { } items })
            {
                products = items;
            }
            else
            {
                UiState.Busy.SetError("Failed to load products");
            }
        }
        catch (Exception ex)
        {
            UiState.Busy.SetError($"Error loading products: {ex.Message}");
        }
    }
}
```

### BusyBoundary Features

**Progress Indicator:**
- Automatic circular progress display while loading
- Shows custom message passed to `Begin()`

**Error Display:**
- Red alert with error message
- Retry button (calls the `Retry` callback)

**Auto-Reset:**
- `Begin()` returns `IDisposable`
- Automatically clears busy state when disposed
- Use `using` statement for automatic cleanup

### UiState Service

**Methods:**

```csharp
// Start busy state
IDisposable Begin(string message = "Loading...")

// End busy state manually (usually not needed with using)
void End()

// Set error state
void SetError(string error)

// Clear error state
void ClearError()

// Check states
bool IsBusy { get; }
bool HasError { get; }
string? Message { get; }
string? Error { get; }
```

### Advanced Patterns

#### Multiple Concurrent Operations

```csharp
private async Task LoadPageDataAsync()
{
    using var busyScope = UiState.Busy.Begin("Loading page data...");
    UiState.Busy.ClearError();

    try
    {
        // Run multiple operations concurrently
        var productsTask = ProductsClient.GetProductsAsync(1, 50);
        var categoriesTask = CategoriesClient.GetCategoriesAsync();

        await Task.WhenAll(productsTask, categoriesTask);

        products = productsTask.Result.Data?.Items ?? new();
        categories = categoriesTask.Result.Data?.Items ?? new();
    }
    catch (Exception ex)
    {
        UiState.Busy.SetError($"Error loading data: {ex.Message}");
    }
}
```

#### Nested Busy States

```csharp
private async Task SaveProductAsync()
{
    using var outerScope = UiState.Busy.Begin("Saving product...");
    
    try
    {
        // Validate first
        if (!ValidateProduct())
        {
            UiState.Busy.SetError("Validation failed");
            return;
        }

        // Save to API
        var response = await ProductsClient.UpdateProductAsync(product);

        if (response.Success)
        {
            // Reload data with nested scope
            using var innerScope = UiState.Busy.Begin("Refreshing list...");
            await LoadProductsAsync();
        }
        else
        {
            UiState.Busy.SetError("Failed to save product");
        }
    }
    catch (Exception ex)
    {
        UiState.Busy.SetError($"Error: {ex.Message}");
    }
}
```

---

## 📚 API Documentation

### DocFX Integration

The `docs/archu-ui` folder contains DocFX configuration for generating API reference documentation from XML comments.

### Generate Documentation

```bash
# 1. Restore dotnet tools
dotnet tool restore

# 2. Run DocFX
dotnet docfx docs/archu-ui/docfx.json

# 3. Open in browser
# Open docs/archu-ui/_site/index.html
```

### XML Documentation

DocFX uses the `Archu.Ui` project file as its metadata source. Any XML comments added to components automatically appear in the rendered documentation.

**Example:**

```csharp
/// <summary>
/// Displays a loading indicator and error boundary for page content.
/// </summary>
/// <remarks>
/// Wrap your page content with this component to automatically handle
/// loading states and errors. Requires UiState service to be injected.
/// </remarks>
public partial class BusyBoundary : ComponentBase
{
    /// <summary>
    /// The content to display when not busy and no error.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Callback to invoke when the retry button is clicked.
    /// </summary>
    [Parameter]
    public Func<Task>? Retry { get; set; }
}
```

---

## ✅ Best Practices

### Authorization

✅ **DO**:
- Use permission-based authorization for fine-grained control
- Cache permission checks to avoid repeated queries
- Hide UI elements users don't have permission to use
- Use constants for permission names
- Always validate on the server

❌ **DON'T**:
- Use hard-coded permission strings
- Check permissions on every render
- Rely solely on client-side authorization
- Use role-based checks for granular access

### State Management

✅ **DO**:
- Use `BusyBoundary` for consistent loading/error UX
- Use `using` statements with `Begin()` for auto-cleanup
- Provide meaningful error messages
- Clear errors before starting new operations

❌ **DON'T**:
- Forget to call `ClearError()` before retrying
- Leave busy state active after errors
- Show technical error details to users

### Performance

✅ **DO**:
- Cache authorization state during component lifecycle
- Use `async`/`await` for all API calls
- Run independent operations concurrently with `Task.WhenAll`
- Dispose of busy scopes properly

❌ **DON'T**:
- Check permissions synchronously
- Block on async operations
- Create memory leaks by not disposing scopes

---

## 📖 Related Documentation

- **[Archu.Web README](../src/Archu.Web/README.md)** - Blazor WebAssembly application
- **[Archu.ApiClient README](../src/Archu.ApiClient/README.md)** - HTTP client library
- **[Authentication Framework](../src/Archu.ApiClient/Authentication/README.md)** - JWT authentication
- **[Archu.Ui README](../src/Archu.Ui/README.md)** - Shared component library
- **[Authorization Guide](./AUTHORIZATION_GUIDE.md)** - Role and permission system

---

**Last Updated**: 2025-01-24  
**Version**: 1.0  
**Maintainer**: Archu Development Team
