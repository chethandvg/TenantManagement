# TentMan.Ui – Blazor UI Package

TentMan.Ui is a Razor Class Library that delivers the layout, navigation, and routing helpers used by the TentMan front-end. The project is intentionally lightweight and focuses on opinionated experiences that wrap MudBlazor primitives so that the host applications stay consistent.

## Feature Highlights

- ✅ Application layout with authenticated navigation chrome (`Layouts/MainLayout`)
- ✅ Navigation menu that adapts to authorization state
- ✅ Login redirection helper to simplify protecting pages
- ✅ Ready-to-use sample pages that demonstrate authentication and data loading patterns
- ✅ Busy and error boundaries that wrap MudBlazor primitives for consistent loading workflows
- ✅ Runtime theme token service that keeps MudBlazor styling in sync across components
- ✅ **Tenant invite management UI** with generate invite dialog, status tracking, and lifecycle management

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
| `TenantsList` | `/tenants` | `Pages/Tenants/TenantsList.razor`<br/>`Pages/Tenants/TenantsList.razor.cs` | Displays all tenants with search by phone/name and add/edit capabilities. |
| `TenantDetails` | `/tenants/{id}` | `Pages/Tenants/TenantDetails.razor`<br/>`Pages/Tenants/TenantDetails.razor.cs` | Tabbed view for tenant profile, addresses, documents, lease history, and **invite management**. Includes Generate Invite dialog with configurable expiry, invite status tracking (Pending/Used/Expired), copy-to-clipboard for invite URLs, and cancel invite functionality. |
| `AcceptInvite` | `/accept-invite` | `Pages/Tenant/AcceptInvite.razor`<br/>`Pages/Tenant/AcceptInvite.razor.cs` | Public page for tenants to accept invite links and create accounts. Validates token, displays registration form, and redirects to tenant dashboard on success. |
| `TenantDashboard` | `/tenant/dashboard` | `Pages/Tenant/Dashboard.razor`<br/>`Pages/Tenant/Dashboard.razor.cs` | Authenticated dashboard for tenants showing their active lease summary and quick actions. Protected by `PolicyNames.RequireTenantRole` authorization. |
| `TenantLeaseSummary` | `/tenant/lease-summary` | `Pages/Tenant/LeaseSummary.razor`<br/>`Pages/Tenant/LeaseSummary.razor.cs` | Detailed view of tenant's active lease including financial details, deposit history, rent timeline, lease parties, and terms. Protected by `PolicyNames.RequireTenantRole` authorization. |
| `TenantDocuments` | `/tenant/documents` | `Pages/Tenant/Documents.razor`<br/>`Pages/Tenant/Documents.razor.cs` | View and manage tenant's documents related to their lease. Protected by `PolicyNames.RequireTenantRole` authorization. |
| `TenantMoveIn` | `/tenant/move-in` | `Pages/Tenant/MoveInHandover.razor`<br/>`Pages/Tenant/MoveInHandover.razor.cs` | Move-in handover form for tenants to complete unit inspection. Protected by `PolicyNames.RequireTenantRole` authorization. |
| `CreateLease` | `/leases/create`<br/>`/leases/create/{unitId}` | `Pages/Leases/CreateLease.razor`<br/>`Pages/Leases/CreateLease.razor.cs` | 7-step wizard for creating new leases with parties, financial terms, and move-in handover. |

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

## Authorization and Access Control

TentMan.Ui uses **policy-based authorization** for consistent access control across all pages and components.

### Page-Level Authorization

Pages use the `@attribute [Authorize]` directive with policy names from `TentMan.Shared.Constants.Authorization`:

```razor
@page "/tenant/dashboard"
@using TentMan.Shared.Constants.Authorization
@attribute [Authorize(Policy = PolicyNames.RequireTenantRole)]

<h1>Tenant Dashboard</h1>
```

### Component-Level Authorization

Navigation and UI elements use `AuthorizeView` with policy names:

```razor
@using TentMan.Shared.Constants.Authorization

<AuthorizeView Policy="@PolicyNames.CanViewTenantPortal">
    <Authorized>
        <MudNavLink Href="/tenant/dashboard">Dashboard</MudNavLink>
    </Authorized>
    <NotAuthorized>
        <p>Access denied</p>
    </NotAuthorized>
</AuthorizeView>
```

### Authorization Constants

All authorization constants are centralized in `TentMan.Shared.Constants.Authorization`:
- **Policy Names**: `PolicyNames` class (e.g., `PolicyNames.RequireTenantRole`)
- **Role Names**: `RoleNames` class (e.g., `RoleNames.Tenant`)
- **Permission Values**: `PermissionValues` class
- **Claim Types**: `ClaimTypes` class

### Helper Services

The `AuthorizationHelper` service provides programmatic authorization checks:

```csharp
@inject AuthorizationHelper AuthHelper

@code {
    private async Task CheckAccessAsync()
    {
        var hasTenantAccess = await AuthHelper.HasPolicyAsync(PolicyNames.RequireTenantRole);
        var isAuthenticated = await AuthHelper.IsAuthenticatedAsync();
    }
}
```

See [AUTHORIZATION_GUIDE.md](../../docs/AUTHORIZATION_GUIDE.md) for complete details.


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

### Coding Guidelines

See **[CONTRIBUTING.md](../../CONTRIBUTING.md)** for complete guidelines. Key rules for this project:

| Rule | Limit | Action When Exceeded |
|------|-------|---------------------|
| `.razor` files | **200 lines** (+20 max) | Extract child components |
| `.razor.cs` code-behind | **300 lines** (+30 max) | Use partial classes |
| Component parameters | 5-7 max | Use parameter objects |

#### Required: Code-Behind Pattern

**Always** use code-behind files for components with logic:

```
Components/
├── MyComponent/
│   ├── MyComponent.razor        # Markup only (max 100 lines preferred)
│   ├── MyComponent.razor.cs     # Code-behind (max 300 lines)
│   └── MyComponent.razor.css    # Scoped styles (optional)
```

#### Component Extraction

Extract a new component when:
- UI section is reused in multiple places
- A section has independent state/logic
- The parent component exceeds line limits
- Logic can be tested independently

#### Modular Design Principles

1. **Single Responsibility**: Each component does one thing well
2. **Reusability**: Design components to be used in multiple contexts
3. **Composability**: Build complex UIs from simple components
4. **Isolation**: Components should not depend on parent implementation details

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

