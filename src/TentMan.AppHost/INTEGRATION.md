# TentMan.Web Integration with Aspire AppHost

## Overview

TentMan.Web (Blazor WebAssembly) has been successfully integrated into the Aspire AppHost orchestration, enabling seamless development and deployment with automatic service discovery and configuration.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    TentMan.AppHost (Aspire)                    │
│                                                               │
│  ┌───────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │  SQL Server   │  │  TentMan.Api   │  │ TentMan.Web    │     │
│  │  (Docker)     │◄─┤  (Backend)   │◄─┤ (Frontend)   │     │
│  │               │  │              │  │              │     │
│  └───────────────┘  └──────────────┘  └──────────────┘     │
│         │                   │                  │            │
│         │                   │                  │            │
│         └───────────────────┴──────────────────┘            │
│              Automatic Service Discovery                    │
└─────────────────────────────────────────────────────────────┘
```

## What Was Changed

### 1. **AppHost Project** (`TentMan.AppHost.csproj`)

Added project reference to TentMan.Web:

```xml
<ProjectReference Include="..\TentMan.Web\TentMan.Web.csproj" />
```

### 2. **AppHost Program.cs**

Added Blazor WebAssembly app configuration:

```csharp
// Blazor WebAssembly - references the API
var web = builder.AddProject<Projects.Archu_Web>("web")
    .WithReference(api)  // This configures the API URL for the Web app
    .WithExternalHttpEndpoints();
```

**Key Features:**
- ✅ Automatic API URL injection via service discovery
- ✅ External HTTP endpoints for browser access
- ✅ Proper health monitoring integration
- ✅ Supports both Docker and local database modes

### 3. **TentMan.Web Program.cs**

Updated to use Aspire service discovery:

```csharp
// Configure the API base URL from configuration
// When running in Aspire, the API URL will be injected via service discovery
var apiUrl = builder.Configuration["services:api:https:0"] 
             ?? builder.Configuration["services:api:http:0"]
             ?? builder.Configuration["ApiClient:BaseUrl"]
             ?? builder.HostEnvironment.BaseAddress;

options.BaseUrl = apiUrl;
```

**Configuration Priority:**
1. Aspire service discovery (HTTPS)
2. Aspire service discovery (HTTP)
3. appsettings.json configuration
4. Host environment base address (fallback)

## Running the Application

### Using Aspire AppHost (Recommended)

**Start the entire application stack:**

```sh
dotnet run --project src\TentMan.AppHost
```

This will start:
- 🐳 SQL Server (Docker container with persistent volume)
- 🔧 TentMan.Api (Backend API on dynamic port)
- 👨‍💼 TentMan.AdminApi (Admin API on dynamic port)
- 🌐 TentMan.Web (Blazor WebAssembly on dynamic port)

**Access the application:**
- **Aspire Dashboard**: http://localhost:15001 (or shown in console)
- **TentMan.Web**: Check Aspire Dashboard for the Web app URL
- **TentMan.Api**: Check Aspire Dashboard for the API URL

### Using Local SQL Server

Set the environment variable to use local database:

```sh
# PowerShell
$env:ARCHU_USE_LOCAL_DB="true"
dotnet run --project src\TentMan.AppHost

# Command Prompt
set ARCHU_USE_LOCAL_DB=true
dotnet run --project src\TentMan.AppHost

# Bash/Linux
export ARCHU_USE_LOCAL_DB=true
dotnet run --project src/TentMan.AppHost
```

This will:
- ❌ Skip Docker SQL Server container
- ✅ Use connection strings from `appsettings.Development.json`
- ✅ Still provide service discovery for Web → API communication

### Running Standalone (Without Aspire)

If you need to run TentMan.Web independently:

```sh
dotnet run --project src\TentMan.Web
```

**Note:** 
- You must manually ensure TentMan.Api is running on https://localhost:7123
- Configure `ApiClient:BaseUrl` in `wwwroot/appsettings.json`
- No automatic service discovery

## Service Discovery

### How It Works

When running in Aspire, the AppHost automatically:

1. **Starts all services** with dynamic ports
2. **Injects configuration** into each service via environment variables
3. **Provides service discovery** through configuration keys

### Configuration Keys

Aspire injects these configuration keys into TentMan.Web:

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
  }
}
```

The Web app reads these values in this format:
- `services:api:https:0` - HTTPS endpoint (preferred)
- `services:api:http:0` - HTTP endpoint (fallback)

## Configuration Files

### TentMan.Web Configuration

**`wwwroot/appsettings.json`** (Production/Standalone):
```json
{
  "ApiClient": {
    "BaseUrl": "https://localhost:7123",
    "TimeoutSeconds": 30,
    "EnableRetryPolicy": true,
    "RetryCount": 3
  }
}
```

**`wwwroot/appsettings.Development.json`** (Development):
```json
{
  "ApiClient": {
    "BaseUrl": "https://localhost:7123",
    "TimeoutSeconds": 60,
    "EnableRetryPolicy": false
  }
}
```

**Note:** When running in Aspire, `BaseUrl` is overridden by service discovery.

## Benefits of Aspire Integration

### ✅ Development Experience

1. **Single Command Startup**
   - Start entire application stack with one command
   - No manual port management
   - No manual service coordination

2. **Automatic Configuration**
   - Services automatically discover each other
   - No hardcoded URLs in development
   - Dynamic port allocation

3. **Aspire Dashboard**
   - Real-time monitoring of all services
   - View logs from all services in one place
   - See resource utilization (CPU, memory)
   - Track HTTP requests across services

### ✅ Service Orchestration

1. **Dependency Management**
   - Automatic startup order (Database → API → Web)
   - Health check coordination
   - Graceful shutdown handling

2. **Resource Management**
   - Shared SQL Server database
   - Persistent Docker volumes
   - Efficient resource allocation

### ✅ Production Readiness

1. **Container Support**
   - Easy Docker containerization
   - Kubernetes deployment ready
   - Azure Container Apps integration

2. **Observability**
   - Distributed tracing ready
   - Centralized logging
   - Metrics collection

## Troubleshooting

### Web App Can't Connect to API

**Symptom:** Network errors, 404s, or connection timeouts

**Solutions:**

1. **Check Aspire Dashboard**
   ```
   http://localhost:15001
   ```
   - Verify API is running and healthy
   - Check the API endpoint URL
   - Look for errors in API logs

2. **Verify Service Discovery**
   - Check Web app logs for resolved API URL
   - Should see: `Using API URL: https://localhost:XXXXX`

3. **Check Browser Console**
   - Open browser Developer Tools (F12)
   - Look for CORS errors
   - Verify API requests are using correct URL

### SQL Server Connection Issues

**Symptom:** Database connection errors in API logs

**Solutions:**

1. **Verify Docker is Running**
   ```sh
   docker ps
   ```
   Should see SQL Server container

2. **Check Connection String**
   - In Aspire Dashboard, check API environment variables
   - Look for `ConnectionStrings__archudb`

3. **Switch to Local Database**
   ```sh
   $env:ARCHU_USE_LOCAL_DB="true"
   dotnet run --project src\TentMan.AppHost
   ```

### Authentication Issues

**Symptom:** Login fails or tokens not working

**Solutions:**

1. **Check API CORS Configuration**
   - API must allow Web app origin
   - Check API logs for CORS errors

2. **Verify Token Storage**
   - Check browser Developer Tools → Application → Local Storage
   - Look for stored authentication tokens

3. **Check API Authentication Endpoints**
   - Test: `POST https://<api-url>/api/v1/authentication/login`
   - Verify returns 200 OK with tokens

### Port Conflicts

**Symptom:** "Address already in use" errors

**Solutions:**

1. **Stop Conflicting Services**
   ```sh
   # Stop all dotnet processes
   taskkill /F /IM dotnet.exe
   ```

2. **Check Port Usage**
   ```sh
   # Windows
   netstat -ano | findstr :<port>
   
   # Linux/Mac
   lsof -i :<port>
   ```

3. **Let Aspire Choose Ports**
   - Remove any hardcoded ports
   - Aspire will allocate dynamically

## Testing the Integration

### 1. Start the Application

```sh
dotnet run --project src\TentMan.AppHost
```

### 2. Access Aspire Dashboard

Navigate to: http://localhost:15001

**Verify:**
- ✅ All services are running (green status)
- ✅ No errors in logs
- ✅ Database is connected

### 3. Access Web Application

From Aspire Dashboard, click the Web app URL.

**Verify:**
- ✅ Web app loads successfully
- ✅ Home page displays
- ✅ No console errors

### 4. Test Authentication

1. Navigate to `/register`
2. Create a new account
3. Verify successful registration and auto-login
4. Check Aspire Dashboard for API request logs

### 5. Test Protected Routes

1. Navigate to `/products`
2. Verify products load from API
3. Try creating a new product
4. Check request flow in Aspire Dashboard

## Next Steps

### Production Deployment

1. **Containerize for Azure Container Apps**
   ```sh
   dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer
   ```

2. **Deploy to Azure**
   - Use Aspire deployment tools
   - Configure Azure SQL Database
   - Set up Application Insights

3. **Configure Production Settings**
   - Update `appsettings.Production.json`
   - Set up proper CORS policies
   - Configure production authentication

### Enhancements

1. **Add Telemetry**
   - OpenTelemetry integration
   - Application Insights
   - Custom metrics

2. **Add More Services**
   - Redis cache
   - Message queue (RabbitMQ/Azure Service Bus)
   - Background workers

3. **Improve Resilience**
   - Add health check endpoints
   - Configure retry policies
   - Implement circuit breakers

## Reference

### Aspire Documentation
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Service Discovery](https://learn.microsoft.com/en-us/dotnet/aspire/service-discovery/overview)
- [Blazor WASM with Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/client-integrations/blazor-web-app)

### Related Documentation
- [Authentication Implementation](../TentMan.Web/AUTHENTICATION.md)
- [ApiClient README](../TentMan.ApiClient/README.md)
- [API Documentation](../TentMan.Api/README.md)

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Integration Status**: ✅ Complete
