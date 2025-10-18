# Archu.ServiceDefaults

## Overview
The ServiceDefaults project contains shared configuration and extensions for .NET Aspire applications. It provides consistent service defaults across all Aspire-enabled projects.

## Target Framework
- .NET 9.0

## Responsibilities
- **Telemetry Configuration**: OpenTelemetry setup for distributed tracing and metrics
- **Health Checks**: Standard health check configuration
- **Service Discovery**: Aspire service discovery configuration
- **Resilience**: Default retry policies and circuit breakers

## Key Components

### Extensions
- **Service Configuration Extensions**: Methods to add common Aspire features to services

## Features Provided
- **OpenTelemetry Integration**: Automatic instrumentation for HTTP, SQL, and custom metrics
- **Health Checks**: Liveness and readiness endpoints
- **Service Discovery**: Automatic service-to-service communication
- **HTTP Client Configuration**: Resilient HTTP clients with retry policies

## Usage
Add service defaults to any Aspire project:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

var app = builder.Build();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();
```

## Technologies
- **.NET Aspire**: Cloud-native application framework
- **OpenTelemetry**: Observability and telemetry
- **Microsoft.Extensions.ServiceDiscovery**: Service discovery

## Dependencies
This project is referenced by:
- `Archu.Api` - for API service defaults
- Any other Aspire-enabled services in the solution

## Observability
The service defaults automatically configure:
- **Distributed Tracing**: Track requests across services
- **Metrics**: Performance and usage metrics
- **Logging**: Structured logging with correlation

## Best Practices
- Keep service defaults consistent across all services
- Configure environment-specific settings in appsettings
- Use health checks for monitoring service availability
- Leverage OpenTelemetry for production observability

## Related Resources
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [OpenTelemetry for .NET](https://opentelemetry.io/docs/instrumentation/net/)
