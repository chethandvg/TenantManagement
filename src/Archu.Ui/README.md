# Archu.Ui – Blazor Component Library

Archu.Ui is a Razor Class Library that provides reusable Blazor components, layouts, and pages for the Archu platform. Built with **MudBlazor**, it delivers a consistent, modern UI experience across Blazor Server, WebAssembly, and Hybrid applications.

## ✨ Features

- ✅ **Platform-Agnostic** - Works with Blazor Server, WebAssembly, and Hybrid (MAUI)
- ✅ **MudBlazor Integration** - Built on Material Design components
- ✅ **Authentication-Aware** - Components adapt to user authentication state
- ✅ **Reusable Layouts** - Pre-built application shell with navigation
- ✅ **State Management** - Built-in busy/error state handling
- ✅ **Theme Customization** - Runtime theme token service
- ✅ **Sample Pages** - Login, registration, products, and more

## 📦 Component Inventory

### Layouts

| Component | Description |
|-----------|-------------|
| **MainLayout** | Application shell with app bar, navigation drawer, and MudBlazor providers |

### Navigation

| Component | Description |
|-----------|-------------|
| **NavMenu** | Responsive navigation drawer with authentication-aware links |
| **RedirectToLogin** | Automatic login redirection for unauthenticated users |

### State Management

| Component | Description |
|-----------|-------------|
| **BusyBoundary** | Displays loading indicators, error alerts, and retry affordances |

### Application Pages

| Page | Route | Purpose |
|------|-------|---------|
| **Index** | `/` | Landing page (requires authentication) |
| **Login** | `/login` | Email/password login form |
| **Register** | `/register` | User registration form |
| **Products** | `/products` | Product catalog with CRUD operations (requires authentication) |
| **Counter** | `/counter` | Sample counter page for testing |
| **FetchData** | `/fetchdata` | Sample weather forecast page |

### State Services

| Service | Description |
|---------|-------------|
| **UiState** | Aggregates UI state services (busy state, theme, etc.) |
| **BusyState** | Tracks in-flight operations, loading messages, and errors |

## 🚀 Getting Started

### 1. Add Project Reference

```bash
dotnet add reference ..\Archu.Ui\Archu.Ui.csproj
```

### 2. Register Services

In your `Program.cs` (Blazor Server, WebAssembly, or Hybrid):

```csharp
using Archu.Ui;
using Archu.Ui.Theming;

builder.Services.AddArchuUi(options =>
{
    // Optional: Customize theme tokens
 options.Tokens.Colors.Primary = "#1D4ED8";
    options.Tokens.Colors.Secondary = "#10B981";
});
```

### 3. Update `_Imports.razor`

```razor
@using Archu.Ui
@using Archu.Ui.Components.Navigation
@using Archu.Ui.Components.Routing
@using Archu.Ui.Components.State
@using Archu.Ui.Layouts
@using Archu.Ui.Pages
@using MudBlazor
```

### 4. Include MudBlazor Assets

Add to your `index.html` (WebAssembly) or `_Host.cshtml` (Server):

```html
<!-- In <head> -->
<link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />

<!-- Before closing </body> -->
<script src="_content/MudBlazor/MudBlazor.min.js"></script>
```

### 5. Use MainLayout

In your `App.razor`:

```razor
<Router AppAssembly="@typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
    </Found>
    <NotFound>
        <LayoutView Layout="@typeof(MainLayout)">
    <p>Sorry, there's nothing at this address.</p>
      </LayoutView>
    </NotFound>
</Router>
```

## 🎨 Theming

### Default Theme

MainLayout automatically configures MudBlazor theme with:
- Primary color: `#594AE2` (purple)
- Material Design 3 palette
- Dark mode support (via `MudThemeProvider`)

### Customizing Theme Tokens

Override design tokens at startup:

```csharp
builder.Services.AddArchuUi(options =>
{
    options.Tokens.Colors.Primary = "#1D4ED8";
    options.Tokens.Colors.Background = "#F9FAFB";
    options.Tokens.Typography.FontFamily = "Inter, sans-serif";
});
```

### Runtime Theme Changes

Use `IThemeTokenService` to change theme at runtime:

```csharp
@inject IThemeTokenService ThemeService

private void SwitchToDarkMode()
{
    ThemeService.ApplyOverrides(tokens =>
    {
        tokens.Colors.Background = "#1C1B1F";
        tokens.Colors.Surface = "#1C1B1F";
        tokens.Colors.OnBackground = "#E6E1E5";
    });
}
```

The `MainLayout` automatically listens for `IThemeTokenService.TokensChanged` and updates the MudBlazor theme.

## 🔐 Authentication Integration

Components adapt to authentication state using Blazor's `AuthorizeView`:

### NavMenu Example

```razor
<AuthorizeView>
    <Authorized>
        <MudNavLink Href="/products" Icon="@Icons.Material.Filled.ShoppingCart">
            Products
        </MudNavLink>
        <MudNavLink @onclick="LogoutAsync" Icon="@Icons.Material.Filled.Logout">
            Logout
        </MudNavLink>
    </Authorized>
    <NotAuthorized>
        <MudNavLink Href="/login" Icon="@Icons.Material.Filled.Login">
    Login
        </MudNavLink>
  <MudNavLink Href="/register" Icon="@Icons.Material.Filled.PersonAdd">
         Register
        </MudNavLink>
    </NotAuthorized>
</AuthorizeView>
```

### Protecting Pages

Use `[Authorize]` attribute and `RedirectToLogin`:

```razor
@page "/protected"
@attribute [Authorize]

<AuthorizeView>
    <Authorized>
        <h3>Protected Content</h3>
    </Authorized>
<NotAuthorized>
 <RedirectToLogin />
    </NotAuthorized>
</AuthorizeView>
```

## 🛠️ State Management

### Using BusyState

The `BusyState` service provides centralized loading and error state management:

```csharp
@inject UiState State

private async Task LoadDataAsync()
{
    State.Busy.SetBusy("Loading products...");
    
    try
    {
        var products = await ProductsClient.GetProductsAsync();
   State.Busy.SetSuccess("Products loaded successfully!");
    }
    catch (Exception ex)
    {
        State.Busy.SetError("Failed to load products", ex.Message);
    }
}
```

### Using BusyBoundary Component

Wrap your content with `BusyBoundary` to display loading/error states:

```razor
<BusyBoundary OnRetry="LoadDataAsync">
    @if (products.Any())
    {
        <ProductList Items="@products" />
  }
</BusyBoundary>
```

**Features:**
- Displays loading spinner when `BusyState.IsBusy`
- Shows error alert when `BusyState.HasError`
- Provides retry button that calls `OnRetry` callback

## 📋 Component API Reference

### MainLayout

**Parameters:** None

**Features:**
- MudThemeProvider with runtime theme support
- MudDialogProvider for dialogs
- MudSnackbarProvider for notifications
- MudPopoverProvider for popovers
- Responsive navigation drawer
- App bar with title and menu toggle

### NavMenu

**Parameters:** None

**Features:**
- Adapts links based on authentication state
- Home, Counter, Products, Login/Logout links
- Icons from Material Design
- Mobile-responsive

### RedirectToLogin

**Parameters:**
- `ReturnUrl` (string, optional) - URL to redirect to after login

**Usage:**
```razor
<RedirectToLogin ReturnUrl="/products" />
```

### BusyBoundary

**Parameters:**
- `OnRetry` (EventCallback, optional) - Callback for retry button

**Usage:**
```razor
<BusyBoundary OnRetry="@LoadDataAsync">
    <ChildContent>
        <!-- Your content here -->
    </ChildContent>
</BusyBoundary>
```

## ♿ Accessibility

The library follows accessibility best practices:

### Required Practices

- ✅ **Semantic HTML** - Proper landmarks (`<header>`, `<nav>`, `<main>`)
- ✅ **ARIA Labels** - All icon buttons have `aria-label`
- ✅ **Keyboard Navigation** - Full keyboard support via MudBlazor
- ✅ **Focus Management** - Visible focus indicators

### Testing

**Manual Testing:**
1. Navigate with Tab/Shift+Tab
2. Activate with Enter/Space
3. Test with screen reader (NVDA, JAWS, VoiceOver)

**Automated Testing:**
```bash
cd tests/Archu.Ui.Tests
dotnet test --filter "Category=Accessibility"
```

## 🧪 Testing

### Unit Tests

The library includes bUnit tests for all components:

```bash
cd tests/Archu.Ui.Tests
dotnet test
```

**Test Coverage:**
- Component rendering
- Authentication state changes
- Theme customization
- Busy state management
- Navigation behavior

## 📦 Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.AspNetCore.Components.Web` | 9.0.10 | Blazor component model |
| `Microsoft.AspNetCore.Components.Authorization` | 9.0.10 | Authentication/authorization |
| `MudBlazor` | 8.0.0 | Material Design components |
| `Archu.ApiClient` | - | API client with authentication |
| `Archu.Contracts` | - | Shared DTOs |

## 🤝 Contributing

When adding new components:

1. **Place in appropriate folder:**
   - `Components/` - Reusable components
   - `Layouts/` - Layout components
   - `Pages/` - Routable pages

2. **Add XML documentation:**
   ```csharp
   /// <summary>
   /// Displays a list of products with pagination.
   /// </summary>
   ```

3. **Follow MudBlazor patterns:**
   - Use MudBlazor components as building blocks
   - Maintain consistent styling
   - Support dark mode

4. **Update inventory:**
   - Add component to this README
   - Include description and usage example

5. **Write tests:**
   - Add bUnit test in `Archu.Ui.Tests`
   - Test component rendering and interactions

## 🔗 Related Projects

- **[Archu.Web](../Archu.Web/README.md)** - Blazor WebAssembly application
- **[Archu.ApiClient](../Archu.ApiClient/README.md)** - HTTP client library
- **[Archu.Api](../Archu.Api/README.md)** - Backend API

## 📚 Additional Resources

- **[MudBlazor Documentation](https://mudblazor.com/)**
- **[Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)**
- **[Material Design 3](https://m3.material.io/)**
- **[WCAG 2.2 Guidelines](https://www.w3.org/WAI/WCAG22/quickref/)**

---

**Platform Support**: Blazor Server, WebAssembly, Hybrid  
**Target Framework**: .NET 9.0  
**Version**: 1.0  
**Last Updated**: 2025-01-23  
**Maintainer**: Archu Development Team

