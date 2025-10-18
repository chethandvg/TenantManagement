# Archu.Api

## Overview
The API project is the presentation layer that exposes HTTP endpoints for the Archu application. Built with ASP.NET Core, it handles HTTP requests, authentication, and API documentation.

## Target Framework
- .NET 9.0

## Technologies
- **ASP.NET Core**: Web API framework
- **Entity Framework Core**: Data access via ApplicationDbContext
- **OpenAPI**: API documentation (Scalar)
- **.NET Aspire**: Service defaults for telemetry, health checks, and service discovery

## Key Components

### Controllers
- **ProductsController**: CRUD endpoints for product catalog management
  - `GET /api/products` - List all products
  - `GET /api/products/{id}` - Get product by ID
  - `POST /api/products` - Create new product
  - `PUT /api/products/{id}` - Update existing product
  - `DELETE /api/products/{id}` - Delete product (soft delete)

### Authentication
- **HttpContextCurrentUser**: Implementation of `ICurrentUser` for accessing current user information

### Configuration
- Uses SQL Server connection string: `archudb` (fallback: `Sql`)
- HTTPS redirection enabled
- OpenAPI/Scalar documentation available in Development mode

## API Documentation
When running in Development mode:
- **OpenAPI**: Available at `/openapi/v1.json`
- **Scalar UI**: Interactive API documentation at `/scalar/v1`

## Features
- **Concurrency Control**: Uses row versioning for optimistic concurrency
- **Soft Delete**: Products can be logically deleted
- **Validation**: Model state validation on requests
- **Health Checks**: Aspire service defaults
- **Telemetry**: Built-in OpenTelemetry support

## Running the Application
```bash
# Run the API directly
dotnet run --project src/Archu.Api

# Or run through Aspire AppHost (recommended)
dotnet run --project src/Archu.AppHost
```

## Dependencies
- `Archu.Domain` - for entity models
- `Archu.Application` - for abstractions
- `Archu.Infrastructure` - for database context
- `Archu.Contracts` - for DTOs and requests
- `Archu.ServiceDefaults` - for Aspire configuration

## Configuration Files
- `appsettings.json` - Production settings
- `appsettings.Development.json` - Development overrides
- `Archu.Api.http` - HTTP request examples for testing

## Best Practices
- Keep controllers thin - delegate to application services
- Use async/await for all I/O operations
- Implement proper error handling and logging
- Use action filters for cross-cutting concerns
- Follow REST conventions for API design
