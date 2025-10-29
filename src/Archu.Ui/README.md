# Archu.Ui – Blazor UI Package

Archu.Ui is a Razor Class Library that delivers the layout, navigation, and routing helpers used by the Archu front-end. The project is intentionally lightweight and focuses on opinionated experiences that wrap MudBlazor primitives so that the host applications stay consistent.

## Feature Highlights

- ✅ Application layout with authenticated navigation chrome (`Layouts/MainLayout`)
- ✅ Navigation menu that adapts to authorization state
- ✅ Login redirection helper to simplify protecting pages
- ✅ Ready-to-use sample pages that demonstrate authentication and data loading patterns
- ✅ Busy and error boundaries that wrap MudBlazor primitives for consistent loading workflows
- ✅ Runtime theme token service that keeps MudBlazor styling in sync across components

## Component Inventory

The catalog below reflects the components that currently ship in the library.

### Layouts

| Component | File | Description |
|-----------|------|-------------|
| `MainLayout` | `Layouts/MainLayout.razor`<br/>`Layouts/MainLayout.razor.cs` | Provides the global shell (app bar, navigation drawer, snackbar/menu providers) and wires the runtime theming service into MudBlazor. |

### Reusable Components

| Component | File | Description |
|-----------|------|-------------|
| `NavMenu` | `Components/Navigation/NavMenu.razor`<br/>`Components/Navigation/NavMenu.razor.cs` | Drawer navigation that exposes authenticated and anonymous links through `AuthorizeView`. |
| `RedirectToLogin` | `Components/Routing/RedirectToLogin.razor`<br/>`Components/Routing/RedirectToLogin.razor.cs` | Redirects unauthenticated visitors to the login page while preserving their requested URL. |
| `BusyBoundary` | `Components/State/BusyBoundary.razor`<br/>`Components/State/BusyBoundary.razor.cs` | Displays a standardized busy indicator, error alert, and retry affordance that can be reused across pages. |

### Application Pages

| Page | Route | Files | Purpose |
|------|-------|-------|---------|
| `Index` | `/` | `Pages/Index.razor`<br/>`Pages/Index.razor.cs` | Landing page that greets signed-in users and expects anonymous users to be redirected. |
| `Login` | `/login` | `Pages/Login.razor`<br/>`Pages/Login.razor.cs` | Email/password login form that surfaces snackbar notifications and respects a `returnUrl` query parameter. |
| `Register` | `/register` | `Pages/Register.razor`<br/>`Pages/Register.razor.cs` | Registration form with confirmation validation and navigation back to the login screen. |
| `Products` | `/products` | `Pages/Products.razor`<br/>`Pages/Products.razor.cs` | Authenticated catalog view that fetches products from `IProductsApiClient` and reports load/error states. |
| `Counter` | `/counter` | `Pages/Counter.razor`<br/>`Pages/Counter.razor.cs` | Sample counter experience used for diagnostics and template parity. |
| `FetchData` | `/fetchdata` | `Pages/FetchData.razor`<br/>`Pages/FetchData.razor.cs` | Weather forecast sample that demonstrates basic API calls through `HttpClient`. |

### State Containers

| Service | File | Description |
|---------|------|-------------|
| `UiState` | `State/UiState.cs` | Aggregates reusable UI state services for pages, including the busy workflow container. |
| `BusyState` | `State/BusyState.cs` | Tracks in-flight operations, busy messages, and errors so components can react to standardized notifications. |

## Getting Started

### 1. Add the Project Reference

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
    options.Tokens.Colors.Primary = "#1D4ED8"; // Customize the primary color
});
```

The optional callback receives `ThemeOptions` so you can override design tokens before the MudBlazor theme is materialized.

### 3. Update `_Imports.razor`

```razor
@using Archu.Ui
@using Archu.Ui.Components.Navigation
@using Archu.Ui.Components.Routing
@using Archu.Ui.Layouts
```

### 4. Include MudBlazor Assets

```html
<link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />

<script src="_content/MudBlazor/MudBlazor.min.js"></script>
```

## Theming and Layout Integration

`MainLayout` automatically registers `MudThemeProvider`, dialog/snackbar/popover services, and the `NavMenu`. It listens for `IThemeTokenService.TokensChanged` so runtime edits to design tokens immediately refresh the MudBlazor theme.

To override tokens at runtime:

```csharp
using Archu.Ui.Theming;

public sealed class ThemeController
{
    private readonly IThemeTokenService _theme;

    public ThemeController(IThemeTokenService theme)
    {
        _theme = theme;
    }

    public void SwitchToDarkMode()
    {
        _theme.ApplyOverrides(tokens =>
        {
            tokens.Colors.Background = "#1C1B1F";
            tokens.Colors.OnBackground = "#E6E1E5";
            tokens.Colors.Surface = "#1C1B1F";
        });
    }
}
```

## API Documentation Automation

XML documentation comments throughout the components are used to generate API reference material. The repository includes a DocFX configuration under `docs/archu-ui` so you can produce browsable docs directly from the source.

1. Restore the local dotnet tools (DocFX is pinned in `.config/dotnet-tools.json`):
   ```bash
   dotnet tool restore
   ```
2. Generate the API site:
   ```bash
   dotnet docfx docs/archu-ui/docfx.json
   ```

DocFX builds metadata from `Archu.Ui.csproj` and outputs a static site to `docs/archu-ui/_site`. The generated pages surface the XML comments for every public component, parameter, and service.

## Contributing

When adding new components or pages:

1. Place them in the appropriate namespace folder (`Components`, `Layouts`, or `Pages`).
2. Add XML documentation for public parameters and services so DocFX can emit accurate API docs.
3. Update the inventory tables above to reflect the new functionality.
4. Prefer MudBlazor primitives and keep application-specific logic inside the host app.
