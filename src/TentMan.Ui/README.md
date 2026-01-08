# TentMan.Ui – Blazor UI Package

TentMan.Ui is a Razor Class Library that delivers the layout, navigation, and routing helpers used by the TentMan front-end. The project is intentionally lightweight and focuses on opinionated experiences that wrap MudBlazor primitives so that the host applications stay consistent.

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
dotnet add reference ..\TentMan.Ui\TentMan.Ui.csproj
```

### 2. Register Services

In your `Program.cs` (Blazor Server, WebAssembly, or Hybrid):

```csharp
using TentMan.Ui;
using TentMan.Ui.Theming;

builder.Services.AddTentManUi(options =>
{
    options.Tokens.Colors.Primary = "#1D4ED8"; // Customize the primary color
});
```

The optional callback receives `ThemeOptions` so you can override design tokens before the MudBlazor theme is materialized.

### 3. Update `_Imports.razor`

```razor
@using TentMan.Ui
@using TentMan.Ui.Components.Navigation
@using TentMan.Ui.Components.Routing
@using TentMan.Ui.Layouts
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
using TentMan.Ui.Theming;

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

XML documentation comments throughout the components are used to generate API reference material. The repository includes a DocFX configuration under `docs/tentman-ui` so you can produce browsable docs directly from the source.

1. Restore the local dotnet tools (DocFX is pinned in `.config/dotnet-tools.json`):
   ```bash
   dotnet tool restore
   ```
2. Generate the API site:
   ```bash
   dotnet docfx docs/tentman-ui/docfx.json
   ```

DocFX builds metadata from `TentMan.Ui.csproj` and outputs a static site to `docs/tentman-ui/_site`. The generated pages surface the XML comments for every public component, parameter, and service.

## Contributing

When adding new components or pages:

1. Place them in the appropriate namespace folder (`Components`, `Layouts`, or `Pages`).
2. Add XML documentation for public parameters and services so DocFX can emit accurate API docs.
3. Update the inventory tables above to reflect the new functionality.
4. Prefer MudBlazor primitives and keep application-specific logic inside the host app.

## Accessibility Guidelines

The TentMan UI library ships reusable building blocks, so we hold them to a strict accessibility bar. When contributing components or layout updates, follow the practices below.

### Required Practices
- **Define page and component landmarks.** Surface semantic containers (`<header>`, `<nav>`, `<main>`, `<footer>`) or `role` attributes so assistive tech can locate core regions. Ensure every layout exposes a single `<main>` landmark and that dialogs/popovers receive the appropriate `role`. Reference the [WCAG 2.2 landmark guidance](https://www.w3.org/WAI/WCAG22/Techniques/aria/ARIA11).
- **Label non-text affordances.** All icon-only buttons, inputs rendered without visible labels, and composite widgets must provide `aria-label`, `aria-labelledby`, or `aria-describedby` values that describe their purpose. Avoid redundant labels that could create duplicate announcements. Review the [MudBlazor accessibility recommendations](https://mudblazor.com/documents/features/accessibility) to see how the underlying controls expose labeling hooks.
- **Guarantee keyboard support.** Every interactive element must be reachable with `Tab`/`Shift+Tab`, maintain a visible focus indicator, and respond to the expected keys (e.g., `Enter`/`Space` to activate buttons, arrow keys to navigate menus). Follow the [WCAG Keyboard Accessible success criterion](https://www.w3.org/WAI/WCAG22/Understanding/keyboard-accessible.html) and wire up roving tabindex patterns where MudBlazor components require it.
- **Document accessibility behaviors.** Update XML doc comments or README sections when you introduce new interaction patterns so consumers know what keyboard shortcuts and landmarks to expect.

### Testing Workflow
1. **Run the automated bUnit + axe suite.** Execute `dotnet test TentMan.sln --filter "Category=Accessibility"` to render shared components with bUnit and assert against axe rules. The suite fails if new regressions are introduced, so run it locally before pushing.
2. **Verify keyboard navigation manually.** Launch the host application that references the library (for example, `dotnet run --project src/TentMan.Web/TentMan.Web.csproj`) and tab through shared components:
   - `NavMenu` – ensure the toggle button, each menu item, and focus trap inside the drawer honor the arrow/escape key patterns.
   - `BusyBoundary` – confirm the retry button is reachable and announced when busy/error states change.
   - `RedirectToLogin` and other routing helpers – verify focus management returns users to the first actionable control after navigation.
   Record observations in the PR description so reviewers can follow the manual walkthrough.
3. **Track findings.** If axe highlights violations that require MudBlazor upstream fixes, file an issue with the `accessibility` label so the team can triage and coordinate with upstream maintainers.

### Additional Resources
- [Deque axe for Blazor quickstart](https://dequeuniversity.com/assets/html/jquery-summit/html5/slides/axe-core-api/index.html) – explains how axe assertions work inside component tests.
- [WAI-ARIA Authoring Practices Guide](https://www.w3.org/WAI/ARIA/apg/) – reference keyboard patterns for custom widgets.
- [WCAG 2.2 Quick Reference](https://www.w3.org/WAI/WCAG22/quickref/) – searchable map of applicable success criteria.

