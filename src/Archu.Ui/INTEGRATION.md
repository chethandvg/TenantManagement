# Archu.Ui Integration Guide

## Quick Start for Different Platforms

### Blazor Server Integration

#### 1. Add Project Reference
```bash
dotnet add reference ../Archu.Ui/Archu.Ui.csproj
```

#### 2. Program.cs
```csharp
using Archu.Ui;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddArchuUi(); // ? Add this line

var app = builder.Build();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();
```

#### 3. _Host.cshtml or App.razor
```html
<!DOCTYPE html>
<html>
<head>
    <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <link href="_content/Archu.Ui/archu-ui.css" rel="stylesheet" />
</head>
<body>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
</body>
</html>
```

---

### Blazor WebAssembly Integration

#### 1. Add Project Reference (in Client project)
```bash
dotnet add reference ../Archu.Ui/Archu.Ui.csproj
```

#### 2. Program.cs
```csharp
using Archu.Ui;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddArchuUi(); // ? Add this line

await builder.Build().RunAsync();
```

#### 3. wwwroot/index.html
```html
<!DOCTYPE html>
<html>
<head>
    <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <link href="_content/Archu.Ui/archu-ui.css" rel="stylesheet" />
</head>
<body>
    <div id="app">Loading...</div>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
    <script src="_framework/blazor.webassembly.js"></script>
</body>
</html>
```

---

### MAUI Blazor Hybrid Integration

#### 1. Add Project Reference
```bash
dotnet add reference ../Archu.Ui/Archu.Ui.csproj
```

#### 2. MauiProgram.cs
```csharp
using Archu.Ui;

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
        
        builder.Services.AddArchuUi(); // ? Add this line
        
        return builder.Build();
    }
}
```

#### 3. wwwroot/index.html
```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Your MAUI App</title>
    <base href="/" />
    <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <link href="_content/Archu.Ui/archu-ui.css" rel="stylesheet" />
    <link href="css/app.css" rel="stylesheet" />
</head>
<body>
    <div id="app">Loading...</div>
    <script src="_framework/blazor.webview.js" autostart="false"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
</body>
</html>
```

---

## Common _Imports.razor Setup

Add these to your `_Imports.razor` file:

```razor
@using Archu.Ui
@using Archu.Ui.Components
@using Archu.Ui.Components.Common
@using Archu.Ui.Components.Forms
@using Archu.Ui.Components.Inputs
@using Archu.Ui.Components.Products
@using Archu.Ui.Components.Typography
@using Archu.Ui.Layouts
@using MudBlazor
```

---

## Layout Setup

### Option 1: Use MainLayout directly

In your `MainLayout.razor`:

```razor
@inherits LayoutComponentBase
@using Archu.Ui.Layouts

<Archu.Ui.Layouts.MainLayout AppTitle="My Application">
    <TopBarContent>
        <MudIconButton Icon="@Icons.Material.Filled.Notifications" Color="Color.Inherit" />
        <MudIconButton Icon="@Icons.Material.Filled.AccountCircle" Color="Color.Inherit" />
    </TopBarContent>
    
    <DrawerContent>
        <MudNavMenu>
            <MudNavLink Href="/" Match="NavLinkMatch.All" Icon="@Icons.Material.Filled.Home">Home</MudNavLink>
            <MudNavLink Href="/products" Icon="@Icons.Material.Filled.ShoppingCart">Products</MudNavLink>
        </MudNavMenu>
    </DrawerContent>
    
    @Body
</Archu.Ui.Layouts.MainLayout>
```

### Option 2: Create Custom Layout

```razor
@inherits LayoutComponentBase

<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <!-- Your custom layout -->
    @Body
</MudLayout>
```

---

## API Integration Example

```razor
@page "/products"
@inject HttpClient Http
@inject ISnackbar Snackbar

<PageHeading>Products</PageHeading>

<LoadingContainer IsLoading="@loading">
    <ProductGrid Products="@products"
                ShowActions="true"
                OnEdit="EditProduct"
                OnDelete="DeleteProduct" />
</LoadingContainer>

@code {
    private bool loading = true;
    private List<ProductDto> products = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            products = await Http.GetFromJsonAsync<List<ProductDto>>("api/products") ?? new();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading products: {ex.Message}", Severity.Error);
        }
        finally
        {
            loading = false;
        }
    }

    private async Task EditProduct(ProductDto product)
    {
        // Navigate to edit page or show dialog
    }

    private async Task DeleteProduct(ProductDto product)
    {
        bool? confirm = await DialogService.ShowMessageBox(
            "Confirm Delete",
            $"Are you sure you want to delete '{product.Name}'?",
            yesText: "Delete",
            cancelText: "Cancel");

        if (confirm == true)
        {
            await Http.DeleteAsync($"api/products/{product.Id}");
            products.Remove(product);
            Snackbar.Add("Product deleted successfully", Severity.Success);
        }
    }
}
```

---

## Troubleshooting

### Issue: MudBlazor styles not loading

**Solution:** Ensure MudBlazor CSS is referenced before Archu.Ui CSS:
```html
<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
<link href="_content/Archu.Ui/archu-ui.css" rel="stylesheet" />
```

### Issue: Components not found

**Solution:** Check that `AddArchuUi()` is called and namespaces are imported in `_Imports.razor`.

### Issue: MAUI hot reload not working

**Solution:** Rebuild the Archu.Ui project and restart the MAUI app.

---

## Advanced Configuration

### Custom MudBlazor Theme

```csharp
services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
});
```

### Override Component Styles

Use CSS isolation in your consuming project:

```css
/* In your component's .razor.css file */
::deep .product-card {
    border: 2px solid var(--mud-palette-primary);
    border-radius: 16px;
}
```

---

## Next Steps

1. ? Reference the Archu.Ui project
2. ? Call `AddArchuUi()` in your startup
3. ? Add CSS/JS references
4. ? Import namespaces in `_Imports.razor`
5. ? Start using components!

Check out `Examples/ProductsExamplePage.razor` for a complete working example.
