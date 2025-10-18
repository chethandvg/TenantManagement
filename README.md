# Archu

A modern, cloud-native .NET application built with Clean Architecture principles and .NET Aspire orchestration.

## Overview
Archu is a product catalog management system demonstrating best practices in .NET 9 development, including:
- Clean Architecture with clear separation of concerns
- .NET Aspire for cloud-native orchestration
- Entity Framework Core with SQL Server
- RESTful API design with OpenAPI documentation
- Comprehensive telemetry and observability

## Architecture

This solution follows Clean Architecture principles with the following layers:

```
???????????????????????????????????????????????????????
?                    Presentation                      ?
?                   (Archu.Api)                        ?
???????????????????????????????????????????????????????
?                    Contracts                         ?
?                  (Archu.Contracts)                   ?
???????????????????????????????????????????????????????
?                   Infrastructure                     ?
?         (Archu.Infrastructure + ServiceDefaults)     ?
???????????????????????????????????????????????????????
?                   Application                        ?
?                (Archu.Application)                   ?
???????????????????????????????????????????????????????
?                     Domain                           ?
?                  (Archu.Domain)                      ?
???????????????????????????????????????????????????????
```

## Projects

| Project | Description | Target Framework |
|---------|-------------|------------------|
| **Archu.Domain** | Core business logic and domain models | .NET 9.0 |
| **Archu.Application** | Use cases and application abstractions | .NET 9.0 |
| **Archu.Infrastructure** | Data access, EF Core, and external services | .NET 9.0 |
| **Archu.Contracts** | DTOs and API contracts | .NET 9.0 |
| **Archu.Api** | REST API endpoints and controllers | .NET 9.0 |
| **Archu.ServiceDefaults** | Aspire service defaults and telemetry | .NET 9.0 |
| **Archu.AppHost** | Aspire orchestration and service management | .NET 8.0 |

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for AppHost)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for SQL Server container)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/) with C# extension

## Getting Started

### 1. Clone the Repository
```bash
git clone https://github.com/chethandvg/archu.git
cd archu
```

### 2. Run with Aspire AppHost
The easiest way to run the entire application stack:

```bash
dotnet run --project src/Archu.AppHost
```

This will:
- Start SQL Server in a Docker container
- Apply database migrations
- Start the Archu.Api web service
- Launch the Aspire Dashboard

### 3. Access the Application

- **API**: Displayed in Aspire Dashboard (typically `https://localhost:7xxx`)
- **Aspire Dashboard**: Displayed in console output (typically `http://localhost:15xxx`)
- **Scalar API Docs**: `https://localhost:7xxx/scalar/v1` (in Development mode)

## Database Migrations

### Create a New Migration
```bash
dotnet ef migrations add <MigrationName> --project src/Archu.Infrastructure --startup-project src/Archu.Api
```

### Update Database
```bash
dotnet ef database update --project src/Archu.Infrastructure --startup-project src/Archu.Api
```

### Remove Last Migration
```bash
dotnet ef migrations remove --project src/Archu.Infrastructure --startup-project src/Archu.Api
```

## API Endpoints

### Products
- `GET /api/products` - List all products
- `GET /api/products/{id}` - Get product by ID
- `POST /api/products` - Create a new product
- `PUT /api/products/{id}` - Update a product
- `DELETE /api/products/{id}` - Delete a product

See `src/Archu.Api/Archu.Api.http` for example requests.

## Features

### Current Features
- ? Product CRUD operations
- ? SQL Server with Entity Framework Core
- ? Optimistic concurrency control
- ? Soft delete support
- ? Audit tracking (Created/Modified timestamps)
- ? OpenAPI/Scalar documentation
- ? .NET Aspire orchestration
- ? Distributed tracing and telemetry
- ? Health checks

### Planned Features
- ?? Authentication & Authorization
- ?? CQRS with MediatR
- ?? FluentValidation
- ?? AutoMapper
- ?? Rate limiting
- ?? Response caching
- ?? API versioning

## Development

### Project Structure
Each project contains its own README.md with specific details:
- [Archu.Domain](src/Archu.Domain/README.md)
- [Archu.Application](src/Archu.Application/README.md)
- [Archu.Infrastructure](src/Archu.Infrastructure/README.md)
- [Archu.Contracts](src/Archu.Contracts/README.md)
- [Archu.Api](src/Archu.Api/README.md)
- [Archu.ServiceDefaults](src/Archu.ServiceDefaults/README.md)
- [Archu.AppHost](src/Archu.AppHost/README.md)

### Building the Solution
```bash
dotnet build
```

### Running Tests
```bash
dotnet test
```

### Code Quality
```bash
# Format code
dotnet format

# Analyze code
dotnet build /p:EnforceCodeStyleInBuild=true
```

## Technologies

- **.NET 9** - Latest .NET runtime and libraries
- **.NET Aspire** - Cloud-native orchestration
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - ORM
- **SQL Server** - Relational database
- **OpenTelemetry** - Distributed tracing
- **Scalar** - API documentation
- **Docker** - Containerization

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues, questions, or contributions, please open an issue on GitHub.

## Resources

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/)
- [ASP.NET Core Web API](https://learn.microsoft.com/aspnet/core/web-api/)