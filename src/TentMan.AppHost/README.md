# TentMan.AppHost

The .NET Aspire App Host that orchestrates all services in the TentMan solution.

---

## 📁 Folder Structure

```
TentMan.AppHost/
├── Program.cs                 # Service orchestration
├── ResourceBuilderExtensions.cs  # Custom extensions
├── INTEGRATION.md             # Integration guide
├── appsettings.json          # Configuration
├── appsettings.Development.json
└── Properties/
    └── launchSettings.json
```

---

## 🎯 Purpose

The AppHost project:
- Orchestrates all microservices
- Manages service dependencies
- Provides the Aspire Dashboard
- Handles configuration and secrets

---

## 📋 Service Configuration

### Program.cs Structure

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Database
var sql = builder.AddSqlServer("sql")
    .AddDatabase("tentmandb");

// Main API
var api = builder.AddProject<Projects.TentMan_Api>("api")
    .WithReference(sql);

// Admin API
var adminApi = builder.AddProject<Projects.TentMan_AdminApi>("adminapi")
    .WithReference(sql);

// Web Frontend
builder.AddProject<Projects.TentMan_Web>("web")
    .WithReference(api);

builder.Build().Run();
```

---

## 📋 Coding Guidelines

### File Size Limits

| File Type | Limit | Action |
|-----------|-------|--------|
| Program.cs | 300 lines max | Extract to extension methods |
| Extensions | 200 lines max | Split by concern |

### Extension Method Pattern

When Program.cs grows large:

```csharp
// ResourceBuilderExtensions.cs
public static class ResourceBuilderExtensions
{
    public static IResourceBuilder<ProjectResource> AddTentManApi(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<SqlServerDatabaseResource> database)
    {
        return builder.AddProject<Projects.TentMan_Api>("api")
            .WithReference(database);
    }
}

// Program.cs
var builder = DistributedApplication.CreateBuilder(args);
var sql = builder.AddSqlServer("sql").AddDatabase("tentmandb");
builder.AddTentManApi(sql);
builder.Build().Run();
```

---

## 🚀 Running the Application

```bash
cd src/TentMan.AppHost
dotnet run
```

### Access Points

| Service | URL |
|---------|-----|
| Aspire Dashboard | https://localhost:15XXX (see console) |
| Main API | https://localhost:7123 |
| Admin API | https://localhost:7290 |
| Web UI | https://localhost:7001 |
| API Docs | https://localhost:7123/scalar/v1 |

---

## 🔗 Dependencies

- **Aspire.Hosting**: Orchestration
- **Aspire.Hosting.SqlServer**: SQL Server support
- All TentMan projects

---

## ✅ Checklist for New Services

- [ ] Add project reference to AppHost
- [ ] Register in Program.cs with dependencies
- [ ] Configure service discovery if needed
- [ ] Test dashboard visibility
- [ ] File size under 300 lines

---

**Last Updated**: 2026-01-08  
**Maintainer**: TentMan Development Team
