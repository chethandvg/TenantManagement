# TentMan.Ui - Changelog

## Initial Release (v1.0.0)

### Project Structure
- ✅ Razor Class Library targeting .NET 9.0
- ✅ Platform-agnostic design that works for Blazor Server, WebAssembly, and MAUI hybrid hosts
- ✅ MudBlazor 8.0.0 as the underlying UI framework
- ✅ Shared contracts via `TentMan.Contracts`

### Components Included

#### Layouts
- **MainLayout.razor** – Application shell with MudBlazor providers, drawer navigation, and authentication-aware toolbar
  - Handles theme updates through `IThemeTokenService`
  - Provides toggleable navigation drawer and snackbar notifications

#### Navigation & Routing
- **Components/Navigation/NavMenu** – Drawer menu that shows authenticated or anonymous links using `AuthorizeView`
- **Components/Routing/RedirectToLogin** – Redirect helper that preserves the requested URL when unauthenticated users hit protected pages

#### Pages
- **Pages/Index** – Landing page for authenticated users
- **Pages/Login** – Email/password login workflow with snackbar feedback and return URL support
- **Pages/Register** – Registration form with password confirmation
- **Pages/Products** – Authenticated data listing that calls `IProductsApiClient`
- **Pages/Counter** – Sample counter to validate the template
- **Pages/FetchData** – Weather forecast demo using `HttpClient`

### Service Extensions
- **UiServiceCollectionExtensions.cs** – `AddArchuUi()` registers theming services, MudBlazor defaults, and static asset manifest

### Styling
- **wwwroot/tentman-ui.css** – Entry point that imports the generated theme token CSS and styles shared elements

### Documentation
- **README.md** – Component overview and integration walkthrough
- **INTEGRATION.md** – Platform-specific setup instructions and usage notes
- **docs/tentman-ui/** – DocFX configuration for generating API documentation directly from XML comments

### Dependencies
- Microsoft.AspNetCore.Components.Web 9.0.1
- MudBlazor 8.0.0
- TentMan.Contracts (project reference)

### File Structure (Excerpt)
```
src/TentMan.Ui/
├── TentMan.Ui.csproj
├── _Imports.razor
├── UiServiceCollectionExtensions.cs
├── README.md
├── INTEGRATION.md
├── CHANGELOG.md
├── Layouts/
│   ├── MainLayout.razor
│   └── MainLayout.razor.cs
├── Components/
│   ├── Navigation/
│   │   ├── NavMenu.razor
│   │   └── NavMenu.razor.cs
│   └── Routing/
│       ├── RedirectToLogin.razor
│       └── RedirectToLogin.razor.cs
├── Pages/
│   ├── Counter.razor
│   ├── Counter.razor.cs
│   ├── FetchData.razor
│   ├── FetchData.razor.cs
│   ├── Index.razor
│   ├── Index.razor.cs
│   ├── Login.razor
│   ├── Login.razor.cs
│   ├── Products.razor
│   ├── Products.razor.cs
│   ├── Register.razor
│   └── Register.razor.cs
└── wwwroot/
    └── tentman-ui.css
```

### Usage Example
```csharp
// Program.cs
builder.Services.AddArchuUi();
```

```razor
@* App.razor *@
<CascadingAuthenticationState>
    <Router AppAssembly="typeof(Program).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)">
                <NotAuthorized>
                    <RedirectToLogin />
                </NotAuthorized>
            </AuthorizeRouteView>
        </Found>
        <NotFound>
            <LayoutView Layout="typeof(MainLayout)">
                <p role="alert">Sorry, there's nothing here.</p>
            </LayoutView>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

### Roadmap
- [ ] Expand the component set beyond layout/navigation helpers
- [ ] Add dedicated documentation samples for each page
- [ ] Publish Storybook-style gallery or playground
- [ ] Package for NuGet distribution
