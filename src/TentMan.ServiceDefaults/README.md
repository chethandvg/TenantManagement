# TentMan.ServiceDefaults

The ServiceDefaults project provides shared .NET Aspire service defaults for all projects in the solution.

---

## 📁 Folder Structure

```
TentMan.ServiceDefaults/
├── Extensions.cs              # Service extension methods
└── TentMan.ServiceDefaults.csproj
```

---

## 🎯 Purpose

The ServiceDefaults project:
- Configures OpenTelemetry for observability
- Sets up health checks
- Provides consistent service configuration
- Enables .NET Aspire dashboard integration

---

## 📋 Usage

### Register in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add service defaults
builder.AddServiceDefaults();

// ... other services

var app = builder.Build();

// Map default endpoints
app.MapDefaultEndpoints();

app.Run();
```

---

## 📋 Coding Guidelines

### Extension Method Pattern

```csharp
namespace TentMan.ServiceDefaults;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(
        this IHostApplicationBuilder builder)
    {
        // OpenTelemetry
        builder.ConfigureOpenTelemetry();
        
        // Health checks
        builder.AddDefaultHealthChecks();
        
        // Service discovery
        builder.Services.AddServiceDiscovery();
        
        return builder;
    }
    
    public static WebApplication MapDefaultEndpoints(
        this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = _ => false
        });
        
        return app;
    }
}
```

### File Size Limits

| File Type | Limit |
|-----------|-------|
| Extensions.cs | 300 lines max |

If the Extensions.cs file exceeds 300 lines, split into:
- `Extensions.cs` - Main extension methods
- `Extensions.OpenTelemetry.cs` - Telemetry configuration
- `Extensions.HealthChecks.cs` - Health check configuration

---

## 🔗 Dependencies

- **Microsoft.Extensions.Hosting**: Hosting abstractions
- **OpenTelemetry**: Observability
- **Microsoft.Extensions.Diagnostics.HealthChecks**: Health checks

---

## ✅ Checklist for Changes

- [ ] All projects use AddServiceDefaults()
- [ ] All apps map default endpoints
- [ ] File size under 300 lines

---

**Last Updated**: 2026-01-08  
**Maintainer**: TentMan Development Team
