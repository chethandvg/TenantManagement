# Archu.AppHost

## Overview
The AppHost project is the .NET Aspire orchestrator that manages the configuration and lifecycle of all services in the Archu application. It provides a unified development experience with service discovery, telemetry, and resource management.

## Target Framework
- .NET 8.0

## Responsibilities
- **Service Orchestration**: Define and configure all application services
- **Resource Management**: Manage databases, caching, messaging, and other resources
- **Local Development**: Provide a consistent development environment
- **Service Dependencies**: Wire up connections between services

## Key Components

### Program.cs
Main orchestration configuration:
- **SQL Server**: SQL Server container with persistent volume
- **archudb Database**: Application database
- **Archu.Api**: Web API project with external HTTP endpoints and Scalar documentation

### ResourceBuilderExtensions
Custom extension methods for configuring resources and services.

## Running the Application
```bash
# Start the entire application stack
dotnet run --project src/Archu.AppHost
```

This will start:
1. SQL Server container (with data volume for persistence)
2. Archu.Api web service
3. Aspire Dashboard for monitoring

## Aspire Dashboard
Once running, access the Aspire Dashboard (URL shown in console) to:
- View all running services
- Monitor logs in real-time
- View traces and metrics
- Inspect service dependencies
- Check health status

## Configuration

### Services
- **api**: The main Web API
  - External HTTP endpoints enabled
  - Scalar API documentation integrated
  - Environment: Development

### Resources
- **sql**: SQL Server container
  - Data volume for persistence
  - **archudb**: Application database

## Features
- **Service Discovery**: Automatic service-to-service communication
- **Telemetry**: Distributed tracing and metrics
- **Health Checks**: Monitor service health
- **Resource Provisioning**: Automatic container management

## Configuration Files
- `appsettings.json` - Production orchestration settings
- `appsettings.Development.json` - Development overrides

## Dependencies
- `Archu.Api` - Web API project reference
- `Archu.ServiceDefaults` - Shared Aspire configuration

## Custom Extensions
The `ResourceBuilderExtensions` class provides custom extension methods to:
- Add common configurations
- Simplify resource setup
- Apply consistent patterns

## Best Practices
- Keep AppHost configuration declarative
- Use environment variables for configuration
- Leverage Aspire Dashboard for debugging
- Configure health checks for all services
- Use data volumes for database persistence

## Deployment
While AppHost is primarily for local development, Aspire can generate:
- **Docker Compose** files for container orchestration
- **Kubernetes manifests** for cloud deployment
- **Azure Container Apps** configuration

## Related Resources
- [.NET Aspire Overview](https://learn.microsoft.com/dotnet/aspire/get-started/aspire-overview)
- [Aspire Dashboard](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard)
- [Service Discovery](https://learn.microsoft.com/dotnet/aspire/service-discovery/overview)
