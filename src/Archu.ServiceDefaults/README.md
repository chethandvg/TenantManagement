# Archu.ServiceDefaults - .NET Aspire Shared Configuration

**Shared service defaults for observability, resilience, and service discovery**

## Overview

`Archu.ServiceDefaults` is a .NET Aspire project that provides common configuration for all services in the Archu application. It centralizes:

- 🔭 **OpenTelemetry Integration** - Distributed tracing, metrics, and structured logging
- 💪 **Resilience Patterns** - HTTP retry, circuit breakers, and timeout policies
- 🔍 **Service Discovery** - Automatic service-to-service communication
- 🏥 **Health Checks** - Liveness and readiness probes
- 📊 **Observability Exports** - OTLP and Azure Monitor integration

## Quick Start

### Add to Your Service

**1. Add Project Reference**

```xml
<!-- In your API/Web project -->
<ItemGroup>
  <ProjectReference Include="..\Archu.ServiceDefaults\Archu.ServiceDefaults.csproj" />
</ItemGroup>
```

**2. Configure in Program.cs**

```csharp
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add all service defaults (recommended)
builder.AddServiceDefaults();

// ... other service configuration ...

var app = builder.Build();

// Map health check endpoints
app.MapDefaultEndpoints();

app.Run();
```

That's it! Your service now has full observability, resilience, and service discovery.

## What Gets Configured

### 1. OpenTelemetry (Observability)

**Metrics:**
- ASP.NET Core request metrics (request duration, status codes)
- HTTP client metrics (outbound request tracking)
- .NET runtime metrics (GC, thread pool, memory)

**Tracing:**
- ASP.NET Core distributed tracing
- HTTP client distributed tracing
- Request/response correlation

**Logging:**
- Structured logging with OpenTelemetry
- Formatted messages included
- Scopes enabled for context

### 2. Service Discovery

**What it does:**
- Enables services to find each other by name (e.g., "api", "admin-api")
- Resolves service URLs automatically
- Works with Aspire orchestration

**Example:**
```csharp
// In Archu.Web, find the API automatically
var apiClient = httpClientFactory.CreateClient("api");
// No hardcoded URLs needed!
```

### 3. HTTP Client Resilience

**Automatic retry policy:**
- Exponential backoff
- Transient error detection (5xx, timeouts)
- Circuit breaker pattern
- Timeout handling

**Default configuration:**
- Max 3 retries
- 2 second base delay
- 30 second timeout

### 4. Health Checks

**Liveness probe (`/alive`):**
- Checks if the application is responsive
- Used by orchestrators to restart unhealthy instances

**Readiness probe (`/health`):**
- Checks if the application is ready to serve traffic
- Includes database, external services, etc.

## Usage Examples

### Basic Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// ✅ Add service defaults
builder.AddServiceDefaults();

// Add your services
builder.Services.AddControllers();
builder.Services.AddDbContext<MyDbContext>();

var app = builder.Build();

// ✅ Map health check endpoints
app.MapDefaultEndpoints();

app.MapControllers();
app.Run();
```

### Custom Health Checks

```csharp
builder.AddServiceDefaults();

// Add custom health checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<ExternalApiHealthCheck>("external-api");

var app = builder.Build();
app.MapDefaultEndpoints(); // Now includes your custom checks
```

### Configure HTTP Clients

```csharp
builder.AddServiceDefaults();

// HTTP clients automatically get resilience + service discovery
builder.Services.AddHttpClient<IProductsApiClient, ProductsApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api");  // 👈 Resolved via service discovery
});
```

### Environment-Specific Telemetry

```csharp
builder.AddServiceDefaults();

// Add Azure Monitor in production
if (builder.Environment.IsProduction())
{
    builder.Services.AddOpenTelemetry()
        .UseAzureMonitor();
}
```

## Configuration Details

### OpenTelemetry Setup

**What `ConfigureOpenTelemetry()` does:**

1. **Adds structured logging:**
   ```csharp
   builder.Logging.AddOpenTelemetry(logging =>
   {
       logging.IncludeFormattedMessage = true;
       logging.IncludeScopes = true;
   });
   ```

2. **Configures metrics:**
   ```csharp
   metrics.AddAspNetCoreInstrumentation()
       .AddHttpClientInstrumentation()
.AddRuntimeInstrumentation();
   ```

3. **Configures tracing:**
   ```csharp
   tracing.AddAspNetCoreInstrumentation()
       .AddHttpClientInstrumentation();
   ```

4. **Sets up exporters** (OTLP, Azure Monitor)

### HTTP Client Defaults

**What `ConfigureHttpClientDefaults()` does:**

```csharp
builder.Services.ConfigureHttpClientDefaults(http =>
{
    // ✅ Add retry, circuit breaker, timeout policies
    http.AddStandardResilienceHandler();

    // ✅ Enable service discovery for all HTTP clients
    http.AddServiceDiscovery();
});
```

**Resilience policies applied:**
- **Retry:** Exponential backoff for transient failures
- **Circuit Breaker:** Prevents cascading failures
- **Timeout:** Per-request and total timeouts
- **Hedging:** (Optional) Parallel requests for critical operations

### Health Check Endpoints

**What `MapDefaultEndpoints()` does:**

```csharp
// Only in Development environment (security best practice)
if (app.Environment.IsDevelopment())
{
    // All health checks must pass
    app.MapHealthChecks("/health");

    // Only "live" tagged checks must pass
    app.MapHealthChecks("/alive", new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("live")
    });
}
```

**Health check results:**
```json
// GET /health
{
  "status": "Healthy",
  "results": {
    "self": { "status": "Healthy" },
    "database": { "status": "Healthy" }
  }
}
```

## Project Structure

```
src/Archu.ServiceDefaults/
├── Archu.ServiceDefaults.csproj  # Project file
├── Extensions.cs  # Extension methods
└── README.md   # This file
```

### Key Components

#### `Extensions.cs`

Contains all extension methods:

| Method | Description |
|--------|-------------|
| `AddServiceDefaults()` | Main entry point - adds all defaults |
| `ConfigureOpenTelemetry()` | Sets up tracing, metrics, logging |
| `AddDefaultHealthChecks()` | Adds liveness health check |
| `MapDefaultEndpoints()` | Maps health check endpoints |

## OpenTelemetry Exporters

### OTLP Exporter

**When it's used:**
- If `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable is set
- Sends telemetry to OpenTelemetry Collector

**Configuration:**
```bash
# Environment variable
export OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4317"
```

### Azure Monitor Exporter

**How to enable:**

Uncomment this section in `Extensions.cs`:

```csharp
if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry()
        .UseAzureMonitor();
}
```

**Then set connection string:**
```bash
export APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=...;IngestionEndpoint=..."
```

## Service Discovery

### How It Works

1. **Aspire orchestrator** assigns service names (e.g., "api", "admin-api")
2. **ServiceDefaults** enables discovery for all HTTP clients
3. **HTTP clients** resolve service names to actual URLs

### Example Flow

```
Archu.Web needs to call Archu.Api:

1. Web creates HTTP client with base address: "https://api"
2. Service discovery intercepts the request
3. Resolves "api" to "https://localhost:7123" (from Aspire config)
4. HTTP client makes request to actual URL
```

### Allowed Schemes

By default, all schemes (http, https) are allowed. To restrict:

```csharp
builder.Services.Configure<ServiceDiscoveryOptions>(options =>
{
    options.AllowedSchemes = ["https"];  // Only HTTPS
});
```

## Resilience Policies

### Standard Resilience Handler

**What it includes:**

| Policy | Configuration |
|--------|---------------|
| **Retry** | 3 attempts, exponential backoff |
| **Circuit Breaker** | Opens after 5 failures in 30 seconds |
| **Timeout** | 30 seconds per request |
| **Rate Limiter** | (Optional) Per-endpoint limits |

### Customizing Resilience

```csharp
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler(options =>
    {
      options.Retry.MaxRetryAttempts = 5;
      options.Retry.Delay = TimeSpan.FromSeconds(1);
     options.CircuitBreaker.FailureRatio = 0.5;
    });
});
```

## Health Checks

### Built-in Checks

**Self check:**
- Always returns `Healthy`
- Tagged with "live"
- Ensures the process is responsive

### Adding Custom Checks

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbContext _dbContext;

    public async Task<HealthCheckResult> CheckHealthAsync(
     HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
    try
        {
         await _dbContext.Database.CanConnectAsync(cancellationToken);
            return HealthCheckResult.Healthy("Database is reachable");
        }
        catch (Exception ex)
        {
      return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}

// Register
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" });
```

## Observability Best Practices

### 1. Use Structured Logging

```csharp
// ✅ Good - structured
_logger.LogInformation("User {UserId} created order {OrderId}", userId, orderId);

// ❌ Bad - string interpolation
_logger.LogInformation($"User {userId} created order {orderId}");
```

### 2. Add Custom Metrics

```csharp
var meter = new Meter("Archu.Api");
var orderCounter = meter.CreateCounter<int>("orders.created");

// Increment when order is created
orderCounter.Add(1, new KeyValuePair<string, object?>("user_id", userId));
```

### 3. Add Custom Traces

```csharp
using var activity = Activity.StartActivity("ProcessOrder");
activity?.SetTag("order_id", orderId);
activity?.SetTag("user_id", userId);

// Your code here...

activity?.SetStatus(ActivityStatusCode.Ok);
```

### 4. Tag Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("critical-service", () => ..., tags: new[] { "live", "ready" })
    .AddCheck("optional-service", () => ..., tags: new[] { "ready" });
```

## Integration with Aspire Dashboard

### What You See

When services use `ServiceDefaults`, the Aspire Dashboard shows:

**Resources Tab:**
- Service health status (from `/health` endpoint)
- Resource metrics (CPU, memory)

**Console Logs Tab:**
- All log entries with structured data
- Filterable by service, level, message

**Traces Tab:**
- Request traces across services
- Timing breakdowns
- Error traces highlighted

**Metrics Tab:**
- HTTP request rate and duration
- .NET runtime metrics
- Custom metrics (if added)

## Deployment Considerations

### Development

Service defaults work automatically with Aspire orchestration. No configuration needed.

### Production (Azure Container Apps)

**Health checks:**
- Azure uses `/health` for readiness probes
- Azure uses `/alive` for liveness probes
- Ensure endpoints are accessible (not blocked by auth)

**Telemetry:**
- Set `APPLICATIONINSIGHTS_CONNECTION_STRING` for Azure Monitor
- Or use `OTEL_EXPORTER_OTLP_ENDPOINT` for custom collector

**Service discovery:**
- Azure Container Apps provides built-in service discovery
- No code changes needed

### Production (Kubernetes)

**Readiness probe:**
```yaml
readinessProbe:
  httpGet:
    path: /health
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 10
```

**Liveness probe:**
```yaml
livenessProbe:
  httpGet:
    path: /alive
    port: 8080
  initialDelaySeconds: 15
  periodSeconds: 20
```

## Troubleshooting

### Health Checks Not Appearing

**Problem:** `/health` endpoint returns 404

**Solutions:**
1. Ensure `app.MapDefaultEndpoints()` is called
2. Check if running in Development mode (endpoints only exposed in dev by default)
3. To enable in production, modify `MapDefaultEndpoints()` method

### Service Discovery Not Working

**Problem:** HTTP client can't resolve service name

**Solutions:**
1. Verify `AddServiceDefaults()` is called before `AddHttpClient()`
2. Check service name matches Aspire configuration
3. Ensure running under Aspire orchestration

### Telemetry Not Appearing

**Problem:** No traces/metrics in Aspire Dashboard

**Solutions:**
1. Verify `AddServiceDefaults()` is called
2. Check `OTEL_EXPORTER_OTLP_ENDPOINT` is set (if using OTLP)
3. Look for OpenTelemetry startup logs
4. Ensure Aspire dashboard is running

## Customization Examples

### Disable Specific Features

```csharp
// Add only health checks and service discovery (no telemetry)
builder.AddDefaultHealthChecks();
builder.Services.AddServiceDiscovery();

builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler();
    http.AddServiceDiscovery();
});
```

### Production-Only Telemetry

```csharp
if (builder.Environment.IsProduction())
{
    builder.ConfigureOpenTelemetry();
}
else
{
    // Minimal telemetry in dev
    builder.Logging.AddConsole();
}
```

### Custom HTTP Client Configuration

```csharp
builder.AddServiceDefaults();

// Override defaults for specific client
builder.Services.AddHttpClient<ICriticalApiClient, CriticalApiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);  // Longer timeout
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 5;  // More retries
});
```

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Aspire.Microsoft.Extensions.ServiceDiscovery` | 9.5.1 | Service discovery |
| `Microsoft.Extensions.Http.Resilience` | 9.0.0 | HTTP resilience policies |
| `Microsoft.Extensions.Diagnostics.HealthChecks` | 9.0.1 | Health checks |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | 1.10.0 | OTLP exporter |
| `OpenTelemetry.Extensions.Hosting` | 1.10.0 | OpenTelemetry integration |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.10.0 | ASP.NET Core telemetry |
| `OpenTelemetry.Instrumentation.Http` | 1.10.0 | HTTP client telemetry |
| `OpenTelemetry.Instrumentation.Runtime` | 1.10.0 | .NET runtime metrics |

## Related Documentation

- **[.NET Aspire Service Defaults](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/service-defaults)** - Official Microsoft docs
- **[OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)** - OpenTelemetry documentation
- **[Resilience in .NET](https://learn.microsoft.com/en-us/dotnet/core/resilience/)** - HTTP resilience patterns
- **[Health Checks in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)** - Health check docs
- **[Archu.AppHost](../Archu.AppHost/README.md)** - Orchestration documentation
- **[Architecture Guide](../../docs/ARCHITECTURE.md)** - Solution architecture

## Version Information

| Property | Value |
|----------|-------|
| **Target Framework** | .NET 9.0 |
| **Language Version** | C# 13 |
| **Nullable** | Enabled |
| **Implicit Usings** | Enabled |

## Quick Reference

### Essential Methods

```csharp
// Add all defaults (recommended)
builder.AddServiceDefaults();

// Map health check endpoints
app.MapDefaultEndpoints();

// Individual components
builder.ConfigureOpenTelemetry();
builder.AddDefaultHealthChecks();
builder.Services.AddServiceDiscovery();
```

### Environment Variables

| Variable | Purpose | Example |
|----------|---------|---------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OTLP collector URL | `http://localhost:4317` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Azure Monitor | `InstrumentationKey=...` |
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Development`, `Production` |

### Health Check URLs

| Endpoint | Purpose | Tags |
|----------|---------|------|
| `/health` | Full health status | All checks |
| `/alive` | Liveness probe | Only "live" tagged |

---

**Last Updated**: 2025-01-23  
**Version**: 1.0  
**Maintainer**: Archu Development Team  
**Questions?** See [docs/README.md](../../docs/README.md) for help resources
