# Archu.Web - Blazor WebAssembly Application

A modern Blazor WebAssembly application built with MudBlazor and the Archu.UI component library, following clean architecture principles.

## Features

- ✅ **Blazor WebAssembly** - Client-side web application
- ✅ **MudBlazor Integration** - Modern Material Design components
- ✅ **Archu.UI Components** - Reusable, platform-agnostic UI components
- ✅ **Clean Architecture** - Separation of concerns with service layer
- ✅ **Dependency Injection** - Properly configured DI container
- ✅ **Progressive Web App (PWA)** - Installable with service worker
- ✅ **Modern C#** - Using .NET 9 and C# 13

## Project Structure

```
Archu.Web/
├── Components/          # Shared UI components
│   └── ConfirmationDialog.razor
├── Layout/             # Layout components
│   ├── MainLayout.razor
│   └── NavMenu.razor
├── Pages/              # Page components (routes)
│   ├── Home.razor
│   ├── Counter.razor
│   ├── Weather.razor
│   └── Products.razor
├── Services/           # Business logic services
│   ├── IProductService.cs
│   └── ProductService.cs
├── wwwroot/           # Static assets
│   ├── css/
│   ├── index.html
│   └── appsettings.json
├── _Imports.razor     # Global using statements
├── App.razor          # Root component
└── Program.cs         # Application entry point
```

## Architecture

### Service Layer Pattern

The application follows clean architecture with a clear separation between:

- **Presentation Layer** (`Pages/`, `Components/`) - UI components
- **Service Layer** (`Services/`) - Business logic and HTTP communication
- **Shared Layer** (`Archu.Contracts`) - DTOs and contracts

### Dependency Injection

Services are registered in `Program.cs`:

```csharp
builder.Services.AddScoped<IProductService, ProductService>();
```

And injected into components:

```razor
@inject IProductService ProductService
```

## Running the Application

### Development

```bash
dotnet run
```

### Build for Production

```bash
dotnet publish -c Release
```

## Configuration

### API Base URL

Configure the API base URL in `wwwroot/appsettings.json`:

```json
{
  "ApiBaseUrl": "https://localhost:7001"
}
```

### Environment-Specific Settings

- `appsettings.json` - Production settings
- `appsettings.Development.json` - Development settings

## Pages

### Home (`/`)
Landing page with application overview and features.

### Counter (`/counter`)
Interactive counter demonstration with statistics.

### Weather (`/weather`)
Weather forecast with data table, sorting, and pagination.

### Products (`/products`)
Product management with CRUD operations using Archu.UI components.

## Components

### Custom Components

- **ConfirmationDialog** - Reusable confirmation dialog for destructive actions

### Archu.UI Components Used

- `ProductGrid` - Grid display for products
- `LoadingContainer` - Loading state management
- `PageHeading` - Consistent page headers
- Various MudBlazor components

## MudBlazor Configuration

MudBlazor is configured via the `AddArchuUi()` extension method which sets up:

- Theme provider
- Popover provider
- Dialog provider
- Snackbar provider (with custom configuration)

## Best Practices

1. **Use Services** - Keep HTTP logic in service classes, not components
2. **Dependency Injection** - Use DI for service resolution
3. **Error Handling** - Proper try-catch with user feedback via Snackbar
4. **Loading States** - Use `LoadingContainer` for async operations
5. **Confirmation Dialogs** - Ask before destructive operations
6. **Search/Filter** - Client-side filtering for better UX

## Progressive Web App (PWA)

The application includes PWA support:

- Offline functionality via service worker
- Install prompt for desktop/mobile
- App manifest for metadata
- Icons for various platforms

## Future Enhancements

- [ ] Authentication and authorization
- [ ] State management (e.g., Fluxor)
- [ ] Advanced error handling and logging
- [ ] Unit and integration tests
- [ ] More CRUD operations for Products
- [ ] Form validation with FluentValidation
- [ ] Real-time updates with SignalR

## Technologies

- .NET 9
- Blazor WebAssembly
- MudBlazor 8.0.0
- Archu.UI (custom component library)
- Archu.Contracts (shared DTOs)

## Related Projects

- **Archu.UI** - Reusable component library
- **Archu.Api** - Backend API
- **Archu.Contracts** - Shared contracts

## License

Same as parent project.
