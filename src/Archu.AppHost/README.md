# Archu.AppHost - .NET Aspire Orchestrator

**Aspire-powered orchestration for the Archu application stack**

## Overview

`Archu.AppHost` is the .NET Aspire orchestrator that manages local development and cloud deployment of the Archu application. It provides:

- 🚀 **One-Command Startup** - Run the entire application stack with a single command
- 🐳 **Automatic Service Provisioning** - SQL Server, APIs, and web apps with service discovery
- 📊 **Aspire Dashboard** - Real-time monitoring, logs, and distributed tracing
- 🔧 **Environment Flexibility** - Docker SQL Server or local SQL Server support
- 📖 **Integrated API Documentation** - One-click access to Scalar API docs
- ☁️ **Cloud-Ready** - Seamless deployment to Azure Container Apps

## Quick Start

### Start the Application

```bash
cd src/Archu.AppHost
dotnet run
```

This single command:
- ✅ Starts SQL Server container (or connects to local database)
- ✅ Launches Archu.Api (Main API) on a dynamic port
- ✅ Launches Archu.AdminApi (Admin API) on a dynamic port
- ✅ Launches Archu.Web (Blazor WebAssembly) on a dynamic port
- ✅ Configures automatic service discovery
- ✅ Opens the Aspire Dashboard in your browser

### Access Points

After startup, the Aspire Dashboard will display URLs for:

| Service | Description | Typical URL |
|---------|-------------|-------------|
| **Aspire Dashboard** | Application monitoring & logs | `http://localhost:15001` |
| **Archu.Api** | Main REST API | `https://localhost:<dynamic>` |
| **Archu.AdminApi** | Admin REST API | `https://localhost:<dynamic>` |
| **Archu.Web** | Blazor WebAssembly UI | `https://localhost:<dynamic>` |
| **SQL Server** | Database (Docker) | `localhost:<dynamic>` |

> 💡 **Tip:** Check the Aspire Dashboard for exact URLs as ports are allocated dynamically.

## Architecture

### Service Orchestration

```
┌──────────────────────────────────────────────────────────────┐
│      Archu.AppHost (Orchestrator)        │
│       │
│  ┌──────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │ SQL Server   │  │ Archu.Api   │  │ Archu.Web   │         │
│  │ (Docker)     │◄─┤ (Backend)   │◄─┤ (Frontend)  │         │
│  │        │  │        │  │    │       │
│  └──────────────┘  └─────────────┘  └─────────────┘         │
│         │    │  │        │
│      └─────────────────┴─────────────────┘       │
│           Service Discovery & Configuration      │
│    │
│  ┌──────────────┐                │
│  │ Admin API    │        │
│  │ (Admin)      │◄─ Shares SQL Database           │
│  │            │       │
│  └──────────────┘ │
└──────────────────────────────────────────────────────────────┘
```

### Managed Resources

| Resource | Type | Purpose | Configuration |
|----------|------|---------|---------------|
| **archudb** | SQL Server | Application database | Docker container with persistent volume |
| **api** | ASP.NET Core | Main REST API | HTTPS endpoint, OpenAPI/Scalar docs |
| **admin-api** | ASP.NET Core | Admin REST API | HTTPS endpoint, user/role management |
| **web** | Blazor WASM | Frontend UI | Consumes API via service discovery |

## Configuration Options

### Database Mode Selection

The AppHost supports two database modes:

#### 1. Docker SQL Server (Default)

Automatically provisions a SQL Server container with:
- ✅ Persistent data volume
- ✅ Automatic connection string injection
- ✅ No local SQL Server installation required

**Usage:**
```bash
dotnet run --project src/Archu.AppHost
```

#### 2. Local SQL Server

Use an existing SQL Server installation:

**Windows PowerShell:**
```powershell
$env:ARCHU_USE_LOCAL_DB="true"
dotnet run --project src/Archu.AppHost
```

**Command Prompt:**
```cmd
set ARCHU_USE_LOCAL_DB=true
dotnet run --project src/Archu.AppHost
```

**Linux/macOS:**
```bash
export ARCHU_USE_LOCAL_DB=true
dotnet run --project src/Archu.AppHost
```

**What happens:**
- ❌ Docker SQL Server container is skipped
- ✅ APIs use connection strings from `appsettings.Development.json`
- ✅ Service discovery still works for API → Web communication

### Environment Variables

| Variable | Values | Default | Description |
|----------|--------|---------|-------------|
| `ARCHU_USE_LOCAL_DB` | `true`/`false` | `false` | Use local SQL Server instead of Docker |
| `ASPNETCORE_ENVIRONMENT` | Any | `Development` | ASP.NET Core environment name |

## Service Discovery

### How It Works

Aspire automatically:

1. **Allocates Dynamic Ports** - Each service gets a unique port at startup
2. **Injects Configuration** - Services receive URLs via environment variables
3. **Enables Service References** - Services discover each other using logical names

### Example: Web → API Communication

**In AppHost (`Program.cs`):**
```csharp
var api = builder.AddProject<Projects.Archu_Api>("api");

var web = builder.AddProject<Projects.Archu_Web>("web")
    .WithReference(api);  // 👈 Creates service discovery link
```

**In Archu.Web (`Program.cs`):**
```csharp
// Aspire injects the API URL via configuration
var apiUrl = builder.Configuration["services:api:https:0"] 
  ?? builder.Configuration["services:api:http:0"]
    ?? builder.HostEnvironment.BaseAddress;
```

**Result:**
- No hardcoded URLs
- Automatic port resolution
- Works in development, Docker, and Azure

### Configuration Keys Injected

```json
{
  "services": {
    "api": {
   "https": {
        "0": "https://localhost:<dynamic-port>"
      },
    "http": {
        "0": "http://localhost:<dynamic-port>"
      }
    }
  },
  "ConnectionStrings": {
    "archudb": "Server=localhost,<port>;Database=archudb;..."
  }
}
```

## Features

### 1. Integrated API Documentation

Each API includes a custom Scalar UI command for easy access to OpenAPI docs.

**Implementation:** `ResourceBuilderExtensions.cs`

```csharp
var api = builder.AddProject<Projects.Archu_Api>("api")
    .WithScalar();  // 👈 Adds "Scalar API Documentation" command
```

**Usage:**
1. Open Aspire Dashboard
2. Find "api" or "admin-api" resource
3. Click **"Scalar API Documentation"** button
4. Browser opens to `/scalar/v1` endpoint

### 2. Persistent Database Storage

Docker SQL Server uses a named volume for data persistence:

```csharp
var sql = builder.AddSqlServer("sql")
    .WithDataVolume();  // 👈 Creates persistent volume
```

**Benefits:**
- Data survives container restarts
- Consistent database state across sessions
- Easy volume management with Docker commands

### 3. External HTTP Endpoints

Services are accessible from external clients (browsers, Postman):

```csharp
var web = builder.AddProject<Projects.Archu_Web>("web")
    .WithExternalHttpEndpoints();  // 👈 Exposes HTTP/HTTPS endpoints
```

**Use Cases:**
- Access Blazor WebAssembly from browser
- Test APIs with Postman or curl
- Integration with external tools

### 4. Shared Database

Multiple APIs share the same SQL Server database:

```csharp
var sql = builder.AddSqlServer("sql").AddDatabase("archudb");

var api = builder.AddProject<Projects.Archu_Api>("api")
    .WithReference(sql);

var adminApi = builder.AddProject<Projects.Archu_AdminApi>("admin-api")
    .WithReference(sql);  // 👈 Same database, different API
```

## Project Structure

```
src/Archu.AppHost/
├── Archu.AppHost.csproj    # Project file (.NET 8 for Aspire compatibility)
├── Program.cs # Orchestration configuration
├── ResourceBuilderExtensions.cs  # Custom Aspire extensions (Scalar UI)
└── README.md          # This file
```

### Key Files

#### `Program.cs`

Main orchestration logic:

- Defines all application resources (SQL, API, Web)
- Configures service references and dependencies
- Sets environment variables
- Controls database mode (Docker vs Local)

#### `ResourceBuilderExtensions.cs`

Custom Aspire commands:

- **`WithScalar()`** - Adds Scalar API documentation command
- Opens browser to `/scalar/v1` endpoint
- Smart endpoint resolution (prefers HTTPS)

## Aspire Dashboard

### Features

The Aspire Dashboard (`http://localhost:15001`) provides:

| Tab | Description |
|-----|-------------|
| **Resources** | View all services, health status, endpoints |
| **Console Logs** | Real-time logs from all services |
| **Traces** | Distributed tracing (OpenTelemetry) |
| **Metrics** | CPU, memory, HTTP request metrics |
| **Structured Logs** | Searchable, filterable log entries |

### Monitoring Services

**Resource Status:**
- 🟢 **Running** - Service is healthy
- 🟡 **Starting** - Service is initializing
- 🔴 **Failed** - Service encountered an error

**Quick Actions:**
- **View Logs** - Click resource → Console tab
- **Restart Service** - Stop and start from dashboard
- **Open Endpoint** - Click HTTPS/HTTP URLs
- **Open Scalar Docs** - Click "Scalar API Documentation" button

## Development Workflows

### Daily Development

**Start the stack:**
```bash
cd src/Archu.AppHost
dotnet run
```

**Make changes to any project** (hot reload supported):
- Edit code in `Archu.Api`, `Archu.Web`, etc.
- Save changes
- Services automatically reload
- Check Aspire Dashboard for updates

**Stop all services:**
- Press `Ctrl+C` in the terminal
- All services and containers shut down gracefully

### Database Migrations

**Create a migration:**
```bash
cd src/Archu.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../Archu.Api
```

**Apply migration:**
```bash
dotnet ef database update --startup-project ../Archu.Api
```

**Or:** Migrations run automatically on API startup (Development mode).

### Testing API Endpoints

**Option 1: Scalar UI** (Recommended)
1. Run AppHost
2. Click "Scalar API Documentation" in Aspire Dashboard
3. Authenticate with JWT token
4. Test endpoints interactively

**Option 2: HTTP Files**
1. Open `src/Archu.Api/Archu.Api.http` in Visual Studio
2. Click "Send Request" next to any request
3. View responses inline

**Option 3: curl/Postman**
- Get API URL from Aspire Dashboard
- Make HTTP requests manually

## Deployment

### Local Development

Already covered - just run `dotnet run` in `Archu.AppHost`.

### Azure Container Apps

**Deploy with Azure Developer CLI:**

```bash
# Initialize (first time only)
azd init

# Deploy to Azure
azd up
```

**What happens:**
- Aspire generates Azure infrastructure (Bicep)
- Provisions Azure Container Apps
- Provisions Azure SQL Database
- Deploys all services
- Configures Application Insights

**Post-deployment:**
- Access URLs provided by `azd up`
- Configure production secrets (JWT, etc.)
- Update CORS policies for production domains

### Docker Compose (Alternative)

**Generate manifest:**
```bash
dotnet run --publisher manifest --output-path aspire-manifest.json
```

**Convert to Docker Compose:**
```bash
# Use Aspire tooling or manual conversion
docker-compose up
```

## Troubleshooting

### "Port Already in Use"

**Symptoms:**
- Error: `Address already in use`
- Services fail to start

**Solutions:**

1. **Stop conflicting processes:**
   ```bash
   # Windows
   taskkill /F /IM dotnet.exe

   # Linux/macOS
   killall dotnet
   ```

2. **Check port usage:**
   ```bash
   # Windows
   netstat -ano | findstr :7123

   # Linux/macOS
   lsof -i :7123
   ```

3. **Let Aspire allocate ports dynamically** (default behavior)

### "Database Connection Failed"

**Symptoms:**
- APIs can't connect to database
- Health checks fail

**Solutions:**

1. **Verify Docker is running:**
   ```bash
   docker ps
   ```
   Should show SQL Server container.

2. **Check connection strings** in Aspire Dashboard:
   - Go to "api" resource → Environment
   - Look for `ConnectionStrings__archudb`

3. **Switch to local database:**
   ```bash
   $env:ARCHU_USE_LOCAL_DB="true"
   dotnet run
   ```

4. **Restart the stack:**
   - Press `Ctrl+C`
   - Run `dotnet run` again

### "Service Won't Start"

**Symptoms:**
- Resource shows "Failed" status
- No logs appear

**Solutions:**

1. **Check service logs:**
   - Open Aspire Dashboard
   - Click failed resource → Console tab
   - Look for error messages

2. **Verify project builds:**
   ```bash
   cd src/Archu.Api  # or other project
   dotnet build
   ```

3. **Check configuration:**
   - Ensure `appsettings.Development.json` exists
   - Verify no missing environment variables

### "Aspire Dashboard Won't Open"

**Symptoms:**
- Dashboard URL doesn't load
- Browser connection refused

**Solutions:**

1. **Check console output** for actual dashboard URL:
   ```
   Aspire dashboard listening on: http://localhost:15001
   ```

2. **Verify firewall** isn't blocking localhost connections

3. **Try alternative port** if 15001 is taken (Aspire will auto-select)

## Extension Points

### Adding New Services

**Example: Adding a Redis cache**

```csharp
// Program.cs
var redis = builder.AddRedis("cache");

var api = builder.AddProject<Projects.Archu_Api>("api")
    .WithReference(sql)
    .WithReference(redis);  // 👈 API can now use Redis
```

### Custom Health Checks

Services can add health checks that appear in Aspire Dashboard:

```csharp
// In Archu.Api Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<ExternalApiHealthCheck>("external-api");
```

### Environment-Specific Configuration

```csharp
var environment = builder.Configuration["ASPNETCORE_ENVIRONMENT"];

if (environment == "Production")
{
    // Production-specific configuration
}
else
{
    // Development-specific configuration
}
```

## Best Practices

### ✅ Do's

- **Use service discovery** instead of hardcoded URLs
- **Let Aspire allocate ports** dynamically
- **Monitor Aspire Dashboard** for real-time feedback
- **Use environment variables** for configuration
- **Check health endpoints** before deployment

### ❌ Don'ts

- **Don't hardcode ports** in service configurations
- **Don't bypass service references** (use `WithReference()`)
- **Don't ignore dashboard warnings** (red/yellow status)
- **Don't commit secrets** to `appsettings.json`

## Benefits of Aspire Orchestration

### Development Experience

- ✅ **Single Command Startup** - No manual service coordination
- ✅ **Automatic Configuration** - Services discover each other
- ✅ **Hot Reload** - Fast iteration without restarts
- ✅ **Centralized Monitoring** - All logs in one place

### Production Readiness

- ✅ **Container Support** - Docker/Kubernetes ready
- ✅ **Cloud Deployment** - Azure Container Apps integration
- ✅ **Observability** - Built-in telemetry and tracing
- ✅ **Service Mesh Ready** - Microservices architecture support

### Team Collaboration

- ✅ **Consistent Environment** - Same setup for all developers
- ✅ **Easy Onboarding** - New developers productive in minutes
- ✅ **Dependency Management** - Service startup order handled
- ✅ **Debugging** - Distributed tracing across services

## Related Documentation

- **[.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)** - Official Microsoft docs
- **[Service Discovery](https://learn.microsoft.com/en-us/dotnet/aspire/service-discovery/overview)** - How service discovery works
- **[Archu.ServiceDefaults](../Archu.ServiceDefaults/README.md)** - Shared service configuration
- **[Archu.Api](../Archu.Api/README.md)** - Main API documentation
- **[Archu.Web](../Archu.Web/README.md)** - Blazor WebAssembly documentation
- **[Getting Started Guide](../../docs/GETTING_STARTED.md)** - Complete setup walkthrough
- **[Architecture Guide](../../docs/ARCHITECTURE.md)** - Solution architecture

## Version Information

| Property | Value |
|----------|-------|
| **Target Framework** | .NET 8.0 (Aspire requirement) |
| **Aspire Version** | 9.5.1 |
| **Language Version** | C# 12 |
| **Nullable** | Enabled |

## Quick Reference

### Start Commands

```bash
# Standard startup
dotnet run --project src/Archu.AppHost

# With local database
$env:ARCHU_USE_LOCAL_DB="true"; dotnet run --project src/Archu.AppHost

# Generate deployment manifest
dotnet run --project src/Archu.AppHost --publisher manifest --output-path manifest.json
```

### Docker Commands

```bash
# List Aspire containers
docker ps

# View SQL Server logs
docker logs <sql-container-id>

# Remove all containers
docker-compose down

# Remove volumes (data reset)
docker volume prune
```

### Aspire Dashboard Shortcuts

| Action | Shortcut |
|--------|----------|
| **Refresh Resources** | F5 |
| **Filter Logs** | Type in search box |
| **Copy Endpoint URL** | Click URL, auto-copies |
| **View Trace Details** | Click trace ID |

---

**Last Updated**: 2025-01-23  
**Version**: 1.0  
**Maintainer**: Archu Development Team  
**Questions?** See [docs/README.md](../../docs/README.md) for help resources
