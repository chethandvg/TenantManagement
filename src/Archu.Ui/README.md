# Archu.Ui - Platform-Agnostic Blazor Component Library

A reusable Razor Class Library containing UI components built with MudBlazor. This library is designed to be platform-agnostic and works seamlessly with:

- **Blazor Server**
- **Blazor WebAssembly**
- **Blazor Hybrid (MAUI)**

## Features

- ✔️ Pre-styled components with CSS isolation
- ✔️ MudBlazor-based components for consistent design
- ✔️ Platform-agnostic (no ASP.NET Core dependencies)
- ✔️ Easy service registration with `AddArchuUi()`
- ✔️ Type-safe component parameters
- ✔️ Accessibility-focused

## Installation

### 1. Add Project Reference

```bash
dotnet add reference ..\Archu.Ui\Archu.Ui.csproj
```

### 2. Register Services

In your `Program.cs` (works for Server, WASM, or MAUI):

```csharp
using Archu.Ui;

builder.Services.AddArchuUi();
```

### 3. Add to _Imports.razor

```razor
@using Archu.Ui
@using Archu.Ui.Components
@using Archu.Ui.Components.Common
@using Archu.Ui.Components.Forms
@using Archu.Ui.Components.Inputs
@using Archu.Ui.Components.Products
@using Archu.Ui.Components.Typography
@using Archu.Ui.Layouts
```

### 4. Add MudBlazor CSS and JS (in your host/index.html or App.razor)

```html
<link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />

<script src="_content/MudBlazor/MudBlazor.min.js"></script>
```

## Component Overview

### Layout Components

#### MainLayout
A complete application layout with app bar, drawer, and main content area.

```razor
@inherits LayoutComponentBase
@layout Archu.Ui.Layouts.MainLayout

<MainLayout AppTitle="My Application">
    <TopBarContent>
        <MudIconButton Icon="@Icons.Material.Filled.Notifications" Color="Color.Inherit" />
    </TopBarContent>
    
    <DrawerContent>
        <MudNavMenu>
            <MudNavLink Href="/" Icon="@Icons.Material.Filled.Home">Home</MudNavLink>
            <MudNavLink Href="/products" Icon="@Icons.Material.Filled.ShoppingCart">Products</MudNavLink>
        </MudNavMenu>
    </DrawerContent>
    
    @Body
</MainLayout>
```

### Typography Components

#### PageHeading
Displays a consistent page heading.

```razor
<PageHeading Color="Color.Primary">Products</PageHeading>
```

#### ArchuText
A flexible text component wrapper around MudText.

```razor
<ArchuText Typo="Typo.h6" Color="Color.Secondary">
    This is some text
</ArchuText>
```

### Input Components

#### ArchuTextField
Generic text input field.

```razor
<ArchuTextField @bind-Value="model.Name" 
                Label="Product Name" 
                Required="true" />
```

#### ArchuNumericField
Numeric input field with type safety.

```razor
<ArchuNumericField @bind-Value="model.Price" 
                   Label="Price" 
                   Min="0M"
                   Step="0.01M" />
```

#### ArchuButton
Styled button component.

```razor
<ArchuButton Color="Color.Primary" 
             StartIcon="@Icons.Material.Filled.Add"
             OnClick="HandleClick">
    Add Product
</ArchuButton>
```

### Product Components

#### ProductCard
Displays a single product with optional actions.

```razor
<ProductCard Product="@product"
            ShowActions="true"
            OnEdit="HandleEdit"
            OnDelete="HandleDelete" />
```

#### ProductGrid
Displays a responsive grid of products.

```razor
<ProductGrid Products="@products"
            ShowActions="true"
            OnEdit="HandleEdit"
            OnDelete="HandleDelete"
            EmptyMessage="No products found." />
```

### Common Components

#### LoadingContainer
Shows loading indicator or content.

```razor
<LoadingContainer IsLoading="@isLoading" Message="Loading products...">
    <ProductGrid Products="@products" />
</LoadingContainer>
```

#### ArchuAlert
Display alerts and messages.

```razor
<ArchuAlert Severity="Severity.Success" Message="Product saved successfully!" />
```

### Form Components

#### ArchuForm
Complete form wrapper with validation.

```razor
<ArchuForm Model="@createRequest"
          Title="Create Product"
          OnValidSubmit="HandleSubmit"
          IsSubmitting="@isSubmitting"
          ShowCancelButton="true"
          OnCancel="HandleCancel">
    
    <ArchuTextField @bind-Value="createRequest.Name" Label="Name" Required="true" />
    <ArchuNumericField @bind-Value="createRequest.Price" Label="Price" Required="true" />
    
</ArchuForm>
```

## Example Usage in Blazor Server/WASM

```razor
@page "/products"
@using Archu.Contracts.Products
@inject HttpClient Http

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
        products = await Http.GetFromJsonAsync<List<ProductDto>>("api/products") ?? new();
        loading = false;
    }

    private void EditProduct(ProductDto product)
    {
        // Handle edit
    }

    private void DeleteProduct(ProductDto product)
    {
        // Handle delete
    }
}
```

## Example Usage in MAUI Blazor Hybrid

The same components work in MAUI! Just register services in `MauiProgram.cs`:

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
        builder.Services.AddArchuUi(); // ? Same registration!
        
        return builder.Build();
    }
}
```

## Customization

### Override MudBlazor Configuration

You can customize MudBlazor configuration by calling `AddMudServices` separately:

```csharp
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;
});
```

### CSS Isolation

All components use CSS isolation (scoped styles). To override styles:

```css
/* In your wwwroot/css/app.css */
::deep .product-card {
    border-radius: 12px;
}
```

## Dependencies

- **Microsoft.AspNetCore.Components.Web** (9.0.1)
- **MudBlazor** (8.0.0)
- **Archu.Contracts** (project reference)

## Architecture Notes

- ? No platform-specific dependencies (works everywhere)
- ? No HTTP or API logic (purely presentational)
- ? Components accept data via parameters
- ? Events are exposed via `EventCallback`
- ? Follows separation of concerns

## Contributing

When adding new components:
1. Place them in appropriate namespace folders
2. Add CSS isolation files (`.razor.css`)
3. Use `EditorRequired` for mandatory parameters
4. Expose events via `EventCallback`
5. Keep components platform-agnostic

## License

Same as parent project.
