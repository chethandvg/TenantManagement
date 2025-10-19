# Archu.Ui - Changelog

## Initial Release (v1.0.0)

### Project Structure
- ? Created new Razor Class Library targeting .NET 9.0
- ? Configured for platform-agnostic usage (Blazor Server, WASM, MAUI)
- ? Added MudBlazor 7.24.0 as core UI framework
- ? Reference to Archu.Contracts for ProductDto

### Components Created

#### Layout Components
- **MainLayout.razor** - Complete app layout with app bar, drawer, and main content
  - Configurable app title
  - Customizable top bar content
  - Customizable drawer content
  - Responsive drawer toggle

#### Typography Components
- **ArchuText.razor** - Flexible text wrapper around MudText
- **PageHeading.razor** - Consistent page heading component
  - CSS isolation for styling

#### Input Components
- **ArchuTextField<T>.razor** - Generic text input field
  - Validation support
  - Adornment icons
  - Helper text and error messages
- **ArchuNumericField<T>.razor** - Type-safe numeric input
  - Min/Max validation
  - Step configuration
  - Hide/show spin buttons
- **ArchuButton.razor** - Styled button component
  - Icon support (start/end)
  - Multiple variants and colors
  - Size options

#### Product Components
- **ProductCard.razor** - Single product display card
  - Optional action buttons (edit, delete)
  - Optional view details button
  - Hover effects with CSS isolation
- **ProductGrid.razor** - Responsive product grid
  - Responsive breakpoints (xs, sm, md, lg)
  - Empty state message
  - Event callbacks for actions

#### Common Components
- **LoadingContainer.razor** - Loading state wrapper
  - Customizable loading message
  - Shows progress indicator while loading
- **ArchuAlert.razor** - Alert/notification component
  - Multiple severity levels
  - Optional close button
  - Customizable variant

#### Form Components
- **ArchuForm<TModel>.razor** - Complete form wrapper
  - Built-in validation support
  - Submitting state handling
  - Optional cancel button
  - Custom form actions support
  - Validation summary

### Service Extensions
- **UiServiceCollectionExtensions.cs**
  - `AddArchuUi()` method for service registration
  - Configures MudBlazor with sensible defaults
  - Platform-agnostic (no ASP.NET Core dependencies)

### Styling
- **CSS Isolation** - All components have scoped styles
- **wwwroot/archu-ui.css** - Global utility styles and CSS variables

### Documentation
- **README.md** - Complete component documentation with examples
- **INTEGRATION.md** - Platform-specific integration guides
  - Blazor Server setup
  - Blazor WebAssembly setup
  - MAUI Blazor Hybrid setup
- **Examples/ProductsExamplePage.razor** - Working example page

### Features
- ? Platform-agnostic design
- ? Type-safe generic components
- ? EventCallback-based event handling
- ? CSS isolation for all components
- ? Fully documented with examples
- ? MudBlazor integration
- ? No platform-specific dependencies
- ? Works with Blazor Server, WASM, and MAUI

### Dependencies
- Microsoft.AspNetCore.Components.Web 9.0.1
- MudBlazor 8.0.0
- Archu.Contracts (project reference)

### File Structure
```
src/Archu.Ui/
??? Archu.Ui.csproj
??? _Imports.razor
??? UiServiceCollectionExtensions.cs
??? README.md
??? INTEGRATION.md
??? Layouts/
?   ??? MainLayout.razor
?   ??? MainLayout.razor.css
??? Components/
?   ??? Typography/
?   ?   ??? ArchuText.razor
?   ?   ??? PageHeading.razor
?   ?   ??? PageHeading.razor.css
?   ??? Inputs/
?   ?   ??? ArchuTextField.razor
?   ?   ??? ArchuNumericField.razor
?   ?   ??? ArchuButton.razor
?   ??? Products/
?   ?   ??? ProductCard.razor
?   ?   ??? ProductCard.razor.css
?   ?   ??? ProductGrid.razor
?   ?   ??? ProductGrid.razor.css
?   ??? Common/
?   ?   ??? LoadingContainer.razor
?   ?   ??? LoadingContainer.razor.css
?   ?   ??? ArchuAlert.razor
?   ??? Forms/
?       ??? ArchuForm.razor
?       ??? ArchuForm.razor.css
??? Examples/
?   ??? ProductsExamplePage.razor
??? wwwroot/
    ??? archu-ui.css
```

### Usage Example
```csharp
// Program.cs
builder.Services.AddArchuUi();
```

```razor
@* YourPage.razor *@
@using Archu.Ui.Components.Products

<PageHeading>Products</PageHeading>
<ProductGrid Products="@products" OnEdit="HandleEdit" OnDelete="HandleDelete" />
```

### Next Steps / Roadmap
- [ ] Add dialog components
- [ ] Add table component with sorting/filtering
- [ ] Add pagination component
- [ ] Add breadcrumb navigation
- [ ] Add theme customization
- [ ] Unit tests for components
- [ ] Storybook/component showcase
- [ ] NuGet package publication

---

**Author:** Archu Development Team  
**Date:** 2024  
**License:** See parent project license
