# TentMan.Ui Integration Guide

TentMan.Ui ships as a Razor Class Library so it can be reused by Blazor Server, WebAssembly, and MAUI hybrid hosts. The following guide walks through the minimal setup required for each platform and highlights how to take advantage of the provided layout, navigation menu, and redirect helper.

## Quick Start by Host Type

### Blazor Server

1. **Reference the project**
   ```bash
   dotnet add reference ../TentMan.Ui/TentMan.Ui.csproj
   ```
2. **Register services** in `Program.cs`
   ```csharp
   using TentMan.Ui;

   var builder = WebApplication.CreateBuilder(args);
   builder.Services.AddRazorPages();
   builder.Services.AddServerSideBlazor();
   builder.Services.AddTentManUi();

   var app = builder.Build();
   app.MapBlazorHub();
   app.MapFallbackToPage("/_Host");
   app.Run();
   ```
3. **Bring in the styles/scripts** in `_Host.cshtml`
   ```html
   <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
   <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
   <link href="_content/TentMan.Ui/tentman-ui.css" rel="stylesheet" />

   <script src="_content/MudBlazor/MudBlazor.min.js"></script>
   ```

### Blazor WebAssembly

1. **Reference the project in the Client app**
   ```bash
   dotnet add reference ../TentMan.Ui/TentMan.Ui.csproj
   ```
2. **Register services** in `Program.cs`
   ```csharp
   using TentMan.Ui;

   var builder = WebAssemblyHostBuilder.CreateDefault(args);
   builder.RootComponents.Add<App>("#app");
   builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
   builder.Services.AddTentManUi();

   await builder.Build().RunAsync();
   ```
3. **Update `wwwroot/index.html`**
   ```html
   <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
   <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
   <link href="_content/TentMan.Ui/tentman-ui.css" rel="stylesheet" />

   <script src="_content/MudBlazor/MudBlazor.min.js"></script>
   ```

### MAUI Blazor Hybrid

1. **Reference the project**
   ```bash
   dotnet add reference ../TentMan.Ui/TentMan.Ui.csproj
   ```
2. **Configure `MauiProgram.cs`**
   ```csharp
   using TentMan.Ui;

   public static class MauiProgram
   {
       public static MauiApp CreateMauiApp()
       {
           var builder = MauiApp.CreateBuilder();
           builder
               .UseMauiApp<App>()
               .ConfigureFonts(fonts =>
               {
                   fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
               });

           builder.Services.AddMauiBlazorWebView();
#if DEBUG
           builder.Services.AddBlazorWebViewDeveloperTools();
#endif
           builder.Services.AddTentManUi();

           return builder.Build();
       }
   }
   ```
3. **Ensure the web view layout imports the CSS**
   ```html
   <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
   <link href="_content/TentMan.Ui/tentman-ui.css" rel="stylesheet" />
   ```

## Common `_Imports.razor`

Add these namespaces so components resolve cleanly:

```razor
@using TentMan.Ui
@using TentMan.Ui.Components.Navigation
@using TentMan.Ui.Components.Routing
@using TentMan.Ui.Layouts
@using MudBlazor
```

## Layout Setup

### Use the Built-in `MainLayout`

The simplest approach is to adopt the provided layout across your app:

```razor
@layout MainLayout
```

`MainLayout` already renders `NavMenu`, wires MudBlazor providers, and displays authentication-aware controls. Set the directive in `_Imports.razor` or in individual pages depending on your needs.

### Custom Layout With TentMan Services

If you need a bespoke shell you can still take advantage of the registered services:

```razor
@inherits LayoutComponentBase

<MudThemeProvider />
<MudDialogProvider />
<MudSnackbarProvider />
<MudPopoverProvider />

<MudLayout>
    <MudAppBar>
        <!-- custom content -->
    </MudAppBar>
    <MudDrawer Open="true">
        <NavMenu />
    </MudDrawer>
    <MudMainContent>
        @Body
    </MudMainContent>
</MudLayout>
```

## Protecting Routes

For pages that require authentication, pair the standard `[Authorize]` attribute with the redirect helper so anonymous visitors are sent to the login screen with their original destination preserved:

```razor
@page "/secure-page"
@attribute [Authorize]

<AuthorizeView>
    <Authorized>
        <!-- secure content -->
    </Authorized>
    <NotAuthorized>
        <RedirectToLogin />
    </NotAuthorized>
</AuthorizeView>
```

## Products Page Example

The library includes a concrete `Products` page that calls `IProductsApiClient`, displays a loading indicator, and reports any errors through MudBlazor snackbars. Use it as a reference implementation when wiring your own data-driven pages. The backing logic lives in `Pages/Products.razor.cs` and demonstrates:

- Injecting API clients and snackbar services via `[Inject]`
- Using XML documentation comments so DocFX can surface the component API
- Managing loading and error state with nullable fields

## Troubleshooting

### MudBlazor styles are missing
Ensure the MudBlazor CSS link is present before `_content/TentMan.Ui/tentman-ui.css` so the TentMan styles can piggyback on MudBlazor variables.

### Components fail to resolve
Run `dotnet add reference` to include the project and confirm the namespaces above are imported in `_Imports.razor`.

### Redirect loop when anonymous
Double-check that the hosting app exposes a `/login` endpoint (the provided login page in TentMan.Ui can be registered with `MapFallbackToPage` in Server apps or included via routing for WebAssembly/Hybrid).

## Next Steps

1. Call `AddTentManUi()` during startup.
2. Import the namespaces listed above.
3. Set the default layout to `MainLayout` and drop `<NavMenu />` where appropriate.
4. Use `<RedirectToLogin />` inside `NotAuthorized` views to streamline authentication.
5. Document any new public parameters so the DocFX pipeline surfaces them automatically.
