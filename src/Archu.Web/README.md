# Archu.Web - Blazor WebAssembly Application

Blazor WebAssembly frontend application for the Archu platform, providing a modern, interactive user interface with authentication, product management, and feature toggles.

## 🌟 Overview

Archu.Web is the primary client application built with **Blazor WebAssembly** that connects to the Archu API. It leverages:

- ✅ **Blazor WebAssembly** - Client-side SPA running entirely in the browser
- ✅ **Archu.ApiClient** - Strongly-typed API client with automatic JWT authentication
- ✅ **Archu.Ui** - Shared Blazor component library with MudBlazor
- ✅ **Feature Management** - Runtime feature toggles via Microsoft.FeatureManagement
- ✅ **JWT Authentication** - Secure API access with token management
- ✅ **Service Discovery** - .NET Aspire integration for dynamic API endpoint resolution

## 🚀 Quick Start

### Prerequisites

- .NET 9 SDK
- Running instance of Archu.Api (default: `https://localhost:7123`)
- Modern web browser (Chrome, Edge, Firefox, Safari)

### Running the Application

**Option 1: Using .NET Aspire (Recommended)**
```bash
cd src/Archu.AppHost
dotnet run
```
This will orchestrate both the API and Web app automatically.

**Option 2: Standalone**
```bash
# Terminal 1 - Start API
cd src/Archu.Api
dotnet run

# Terminal 2 - Start Web App
cd src/Archu.Web
dotnet run
```

**Access the application:**
- **WebAssembly App**: `https://localhost:5001` (or port shown in console)
- **API**: `https://localhost:7123`

### Initial Login

Use the default seeded account:
- **Email**: `admin@archu.com`
- **Password**: `Admin123!`

## 📦 Project Structure

```
Archu.Web/
├── Configuration/
│   └── ApiClientOptionsPostConfigure.cs   # Aspire service discovery integration
├── Services/
│   └── ClientFeatureService.cs       # Feature flag service implementation
├── wwwroot/
│   ├── appsettings.json         # Production configuration
│   ├── appsettings.Development.json       # Development overrides
│   ├── index.html   # App shell
│   └── css/     # Stylesheets
├── Program.cs      # Application startup
├── App.razor      # Root component
├── _Imports.razor        # Global using statements
└── Archu.Web.csproj     # Project file
```

## ⚙️ Configuration

### appsettings.json

**Production Configuration:**
```json
{
  "ApiClient": {
    "BaseUrl": "",  // Resolved via Aspire service discovery
    "TimeoutSeconds": 30,
    "RetryCount": 3,
    "ApiVersion": "v1",
    "EnableDetailedLogging": false,
    "CircuitBreakerFailureThreshold": 10,
    "CircuitBreakerDurationSeconds": 30,
    "RetryBaseDelaySeconds": 1.0,
    "EnableCircuitBreaker": true,
    "EnableRetryPolicy": true
  },
  "Authentication": {
  "AutoAttachToken": true,
    "TokenExpirationBufferSeconds": 60,
    "AutoRefreshToken": false,
    "TokenRefreshThresholdSeconds": 300,
    "AuthenticationEndpoint": "api/v1/authentication/login",
    "RefreshTokenEndpoint": "api/v1/authentication/refresh-token",
    "UseBrowserStorage": true
  },
  "FeatureManagement": {
    "DarkMode": true,
    "AdvancedFeatures": false,
  "BetaFeatures": false
  }
}
```

**Development Configuration (appsettings.Development.json):**
```json
{
  "ApiClient": {
    "BaseUrl": "https://localhost:7123",  // Direct API URL for local development
    "EnableDetailedLogging": true
  },
  "Authentication": {
    "UseBrowserStorage": true  // Persist tokens across page refreshes
  },
  "FeatureManagement": {
    "BetaFeatures": true  // Enable beta features in development
  }
}
```

### Service Discovery

The app uses **ApiClientOptionsPostConfigure** to integrate with .NET Aspire service discovery:

```csharp
// Automatically resolves API URL when running under Aspire
builder.Services.AddTransient<IPostConfigureOptions<ApiClientOptions>, 
  ApiClientOptionsPostConfigure>();
```

**How it works:**
1. If `ApiClient:BaseUrl` is empty/null in config
2. AND the app is running under Aspire orchestration
3. THEN the service discovery resolver finds the API endpoint dynamically

## 🔐 Authentication

### JWT Token Management

The app uses `Archu.ApiClient` for authentication:

**Platform Configuration:**
```csharp
// Blazor WebAssembly uses SINGLETON token storage
builder.Services.AddApiClientForWasm(builder.Configuration);
```

**Why Singleton?**
- WebAssembly runs in a single-user context (one user per browser instance)
- Singleton storage ensures tokens persist across component re-renders
- Browser localStorage can optionally persist tokens across page refreshes

### Token Storage Options

**Option 1: In-Memory (Session-Only)**
```json
{
  "Authentication": {
    "UseBrowserStorage": false// Lost on page refresh
  }
}
```

**Option 2: Browser localStorage (Persistent)**
```json
{
  "Authentication": {
    "UseBrowserStorage": true  // Survives page refreshes
  }
}
```

⚠️ **Security Note:** Browser localStorage is accessible to JavaScript. For highly sensitive applications, use in-memory storage and require re-authentication on page refresh.

### Protected Routes

Use Blazor's `[Authorize]` attribute to protect pages:

```razor
@page "/products"
@attribute [Authorize]

<AuthorizeView>
    <Authorized>
        <ProductList />
 </Authorized>
    <NotAuthorized>
     <RedirectToLogin />
    </NotAuthorized>
</AuthorizeView>
```

## 🎛️ Feature Management

### Feature Flags

The app uses Microsoft.FeatureManagement for runtime feature toggles:

**Registering Feature Management:**
```csharp
builder.Services.AddFeatureManagement();
builder.Services.AddScoped<IClientFeatureService, ClientFeatureService>();
```

**Checking Features in Code:**
```csharp
@inject IClientFeatureService Features

@if (await Features.IsEnabledAsync("DarkMode"))
{
    <DarkModeToggle />
}
```

**Available Features:**
| Feature | Default | Description |
|---------|---------|-------------|
| `DarkMode` | `true` | Enable dark mode theme toggle |
| `AdvancedFeatures` | `false` | Advanced product management features |
| `BetaFeatures` | `false` | Experimental features (dev only) |

### Adding New Features

1. **Add feature to appsettings.json:**
```json
{
"FeatureManagement": {
    "MyNewFeature": true
  }
}
```

2. **Use in components:**
```csharp
@inject IClientFeatureService Features

@if (await Features.IsEnabledAsync("MyNewFeature"))
{
    <MyNewComponent />
}
```

## 🎨 UI Components

The app uses **Archu.Ui** component library, which includes:

- ✅ `MainLayout` - Application shell with navigation
- ✅ `NavMenu` - Authentication-aware navigation
- ✅ `Login` / `Register` pages - User authentication
- ✅ `Products` page - Product catalog with CRUD
- ✅ `BusyBoundary` - Loading states and error handling
- ✅ `RedirectToLogin` - Automatic login redirection

See **[Archu.Ui README](../Archu.Ui/README.md)** for complete component documentation.

## 🛠️ Development

### Adding New Pages

1. **Create Razor component** in `Archu.Ui/Pages/`:
```razor
@page "/my-page"
@attribute [Authorize]

<h3>My Page</h3>

@code {
    // Component logic
}
```

2. **Add navigation link** in `NavMenu.razor`:
```razor
<MudNavLink Href="/my-page" Icon="@Icons.Material.Filled.Home">
    My Page
</MudNavLink>
```

### API Integration

Use the injected API clients:

```csharp
@inject IProductsApiClient ProductsClient
@inject ILogger<MyComponent> Logger

private async Task LoadProductsAsync()
{
    try
    {
   var response = await ProductsClient.GetProductsAsync(pageNumber: 1, pageSize: 20);

        if (response.Success && response.Data != null)
        {
            products = response.Data.Items.ToList();
        }
        else
  {
   Logger.LogWarning("Failed to load products: {Message}", response.Message);
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Error loading products");
  }
}
```

### State Management

Use the `UiState` service from Archu.Ui:

```csharp
@inject UiState State

private async Task SaveProductAsync()
{
    State.Busy.SetBusy("Saving product...");
    
    try
    {
        await ProductsClient.CreateProductAsync(newProduct);
        State.Busy.SetSuccess("Product saved successfully!");
    }
    catch (Exception ex)
    {
     State.Busy.SetError("Failed to save product", ex.Message);
 }
}
```

## 🧪 Testing

### Running Tests

```bash
cd tests/Archu.Ui.Tests
dotnet test
```

### Test Coverage

- Component rendering tests
- Authentication flow tests
- API client integration tests
- Feature flag tests

## 🚀 Deployment

### Building for Production

```bash
cd src/Archu.Web
dotnet publish -c Release -o ./publish
```

**Output:**
- Static files in `./publish/wwwroot/`
- Deploy to any static hosting (Azure Static Web Apps, GitHub Pages, Netlify, etc.)

### Azure Static Web Apps

```bash
# Install Azure Static Web Apps CLI
npm install -g @azure/static-web-apps-cli

# Build and deploy
dotnet publish -c Release
swa deploy ./publish/wwwroot --env production
```

### Docker

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Archu.Web/Archu.Web.csproj", "Archu.Web/"]
COPY ["src/Archu.ApiClient/Archu.ApiClient.csproj", "Archu.ApiClient/"]
COPY ["src/Archu.Ui/Archu.Ui.csproj", "Archu.Ui/"]
COPY ["src/Archu.Contracts/Archu.Contracts.csproj", "Archu.Contracts/"]
RUN dotnet restore "Archu.Web/Archu.Web.csproj"
COPY src/ .
RUN dotnet publish "Archu.Web/Archu.Web.csproj" -c Release -o /app/publish

# Runtime stage (nginx)
FROM nginx:alpine
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html/
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
```

### Environment-Specific Configuration

**Production (appsettings.json):**
```json
{
  "ApiClient": {
    "BaseUrl": "https://api.archu.com",
    "EnableDetailedLogging": false
  }
}
```

**Staging (appsettings.Staging.json):**
```json
{
  "ApiClient": {
    "BaseUrl": "https://api-staging.archu.com",
    "EnableDetailedLogging": true
  }
}
```

## 📊 Performance

### Bundle Size Optimization

The app uses Blazor WebAssembly's built-in optimizations:

- **Ahead-of-Time (AOT) Compilation** (optional):
  ```xml
  <PropertyGroup>
    <RunAOTCompilation>true</RunAOTCompilation>
  </PropertyGroup>
  ```

- **Trimming** (enabled by default):
  ```xml
  <PropertyGroup>
    <PublishTrimmed>true</PublishTrimmed>
</PropertyGroup>
  ```

### Lazy Loading

Consider implementing lazy loading for large features:

```csharp
@page "/admin"
@using Microsoft.AspNetCore.Components.WebAssembly.Services

@inject LazyAssemblyLoader AssemblyLoader

@code {
    protected override async Task OnInitializedAsync()
    {
      await AssemblyLoader.LoadAssembliesAsync(new[] { "Archu.Admin.dll" });
    }
}
```

## 🐛 Troubleshooting

### Issue: "Failed to fetch" on API calls

**Symptoms:** All API requests fail with network errors

**Solutions:**
1. Verify API is running: `https://localhost:7123/health`
2. Check `ApiClient:BaseUrl` in `appsettings.Development.json`
3. Ensure CORS is configured on the API
4. Inspect browser console for specific errors

### Issue: Authentication state not persisting

**Symptoms:** User logged out on page refresh

**Solutions:**
1. Enable browser storage:
   ```json
   {
     "Authentication": {
       "UseBrowserStorage": true
     }
   }
 ```
2. Check browser console for localStorage errors
3. Verify cookies/localStorage are not blocked

### Issue: Service discovery not working

**Symptoms:** API calls fail when running under Aspire

**Solutions:**
1. Ensure `ApiClientOptionsPostConfigure` is registered
2. Verify Aspire orchestration is running (`Archu.AppHost`)
3. Check Aspire dashboard for service endpoints

### Issue: Feature flags not working

**Symptoms:** Features not enabling/disabling as expected

**Solutions:**
1. Verify `appsettings.json` has correct feature flags
2. Check `IClientFeatureService` is injected correctly
3. Ensure `AddFeatureManagement()` is called in `Program.cs`

## 📚 Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.AspNetCore.Components.WebAssembly` | 9.0.10 | Blazor WebAssembly runtime |
| `Microsoft.AspNetCore.Components.Authorization` | 9.0.10 | Authentication/authorization |
| `Microsoft.FeatureManagement` | 3.2.0 | Feature toggles |
| `MudBlazor` | 8.0.0 | UI component library |
| `Archu.ApiClient` | - | API client with authentication |
| `Archu.Ui` | - | Shared UI components |

## 🔗 Related Projects

- **[Archu.Api](../Archu.Api/README.md)** - Backend API
- **[Archu.ApiClient](../Archu.ApiClient/README.md)** - HTTP client library
- **[Archu.Ui](../Archu.Ui/README.md)** - Shared component library
- **[Archu.AppHost](../Archu.AppHost/)** - .NET Aspire orchestration

## 📖 Additional Resources

- **[Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)**
- **[.NET Aspire Service Discovery](https://learn.microsoft.com/en-us/dotnet/aspire/service-discovery/overview)**
- **[Microsoft.FeatureManagement](https://learn.microsoft.com/en-us/azure/azure-app-configuration/use-feature-flags-dotnet-core)**
- **[MudBlazor Documentation](https://mudblazor.com/)**

---

**Platform**: Blazor WebAssembly  
**Target Framework**: .NET 9.0  
**Version**: 1.0  
**Last Updated**: 2025-01-23  
**Maintainer**: Archu Development Team
