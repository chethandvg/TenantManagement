# Archu

A modern, cloud-native .NET application built with Clean Architecture principles and .NET Aspire orchestration.

[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=.net)](https://dotnet.microsoft.com/)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/chethandvg/archu)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Overview

Archu is a product catalog management system demonstrating best practices in modern .NET development:

- ✅ **Clean Architecture** with clear separation of concerns
- ✅ **CQRS Pattern** with MediatR for command/query separation
- ✅ **Unit of Work Pattern** for transaction management
- ✅ **Repository Pattern** for data access abstraction
- ✅ **.NET Aspire** for cloud-native orchestration
- ✅ **Entity Framework Core** with SQL Server
- ✅ **RESTful API** design with OpenAPI documentation
- ✅ **Comprehensive telemetry** and observability

## Architecture

This solution follows Clean Architecture principles with the following layers:

```
┌──────────────────────────────────────────────┐
│              Presentation                     │
│             (Archu.Api)                       │
├──────────────────────────────────────────────┤
│              Contracts                        │
│           (Archu.Contracts)                   │
├──────────────────────────────────────────────┤
│             Infrastructure                    │
│  (Archu.Infrastructure + ServiceDefaults)    │
├──────────────────────────────────────────────┤
│             Application                       │
│          (Archu.Application)                  │
├──────────────────────────────────────────────┤
│               Domain                          │
│            (Archu.Domain)                     │
└──────────────────────────────────────────────┘
```

**Key Architectural Decisions:**
- **Dependency Flow**: Inner layers (Domain) never depend on outer layers
- **Repository Pattern**: Properly implemented with Unit of Work for transaction control
- **CQRS**: Commands and queries separated for better scalability
- **Validation**: FluentValidation with pipeline behaviors

For detailed architecture documentation, see:
- [Architecture Guide](ARCHITECTURE_GUIDE.md) - Comprehensive improvements guide
- [Architecture Documentation](README_architecture.md) - Detailed architecture overview

## Projects

| Project | Description | Target Framework |
|---------|-------------|------------------|
| **Archu.Domain** | Core business logic and domain models | .NET 9.0 |
| **Archu.Application** | Use cases, CQRS handlers, and abstractions | .NET 9.0 |
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
- Start the Archu.Api web service
- Launch the Aspire Dashboard

> **Note:** Database migrations are not applied automatically. Apply migrations manually using the [Database Migrations](#database-migrations) section below.

### 3. Access the Application

- **API**: Displayed in Aspire Dashboard (typically `https://localhost:7xxx`)
- **Aspire Dashboard**: Displayed in console output (typically `http://localhost:15xxx`)
- **Scalar API Docs**: `https://localhost:7xxx/scalar/v1` (in Development mode)

## Database Migrations

### Create a New Migration
```bash
dotnet ef migrations add <MigrationName> \
  --project src/Archu.Infrastructure \
  --startup-project src/Archu.Api
```

### Update Database
```bash
dotnet ef database update \
  --project src/Archu.Infrastructure \
  --startup-project src/Archu.Api
```

### Remove Last Migration
```bash
dotnet ef migrations remove \
  --project src/Archu.Infrastructure \
  --startup-project src/Archu.Api
```

## API Endpoints

### Products (v1)
- `GET /api/v1/products` - List all products
- `GET /api/v1/products/{id}` - Get product by ID
- `POST /api/v1/products` - Create a new product
- `PUT /api/v1/products/{id}` - Update a product
- `DELETE /api/v1/products/{id}` - Delete a product (soft delete)

### Health Checks
- `GET /health` - Comprehensive health check with detailed status
- `GET /health/ready` - Readiness probe (for Kubernetes)
- `GET /health/live` - Liveness probe (for Kubernetes)

See `src/Archu.Api/Archu.Api.http` for example requests.

## Features

### ✅ Current Features
- ✅ **Product CRUD operations** with CQRS pattern
- ✅ **SQL Server** with Entity Framework Core 9
- ✅ **Unit of Work pattern** for transaction management
- ✅ **Repository pattern** properly implemented
- ✅ **Optimistic concurrency control** with row versioning
- ✅ **Soft delete support** with global query filters
- ✅ **Audit tracking** (Created/Modified timestamps)
- ✅ **MediatR** for CQRS implementation
- ✅ **FluentValidation** with pipeline behaviors
- ✅ **Performance tracking** behavior
- ✅ **API versioning** (URL-based)
- ✅ **OpenAPI/Scalar documentation**
- ✅ **Global exception handling** middleware
- ✅ **.NET Aspire orchestration**
- ✅ **Distributed tracing** with OpenTelemetry
- ✅ **Health checks** (database, application)
- ✅ **Structured logging** with LoggerMessage source generators

### 🔄 Planned Features
- ⏳ **Unit & Integration Tests** (xUnit, FluentAssertions)
- ⏳ **JWT Authentication & Authorization**
- ⏳ **Redis Caching** (distributed cache)
- ⏳ **Rate Limiting** (fixed window, sliding window)
- ⏳ **Response Compression** (Gzip, Brotli)
- ⏳ **Domain Events** (event-driven architecture)
- ⏳ **Specification Pattern** (complex queries)
- ⏳ **AutoMapper** (DTO mapping)
- ⏳ **Outbox Pattern** (reliable event publishing)
- ⏳ **Pagination** (API paging support)

See [ARCHITECTURE_GUIDE.md](ARCHITECTURE_GUIDE.md) for detailed implementation roadmap.

## Recent Improvements

### 🔴 Critical Fix (2025-01-19)
**Repository Pattern Corrected** - Removed `SaveChangesAsync` from repositories. Persistence is now properly managed by Unit of Work pattern in command handlers.

**Before:**
```csharp
// ❌ Wrong: Repository saving changes
await _repository.AddAsync(product);
```

**After:**
```csharp
// ✅ Correct: Unit of Work controls persistence
await _unitOfWork.Products.AddAsync(product);
await _unitOfWork.SaveChangesAsync(); // Explicit transaction control
```

## Development

### Project Structure
Each project contains its own README.md with specific details:
- [Archu.Domain](src/Archu.Domain/README.md) - Core entities and domain logic
- [Archu.Application](src/Archu.Application/README.md) - CQRS handlers and behaviors
- [Archu.Infrastructure](src/Archu.Infrastructure/README.md) - Data access and external services
- [Archu.Contracts](src/Archu.Contracts/README.md) - API DTOs and requests
- [Archu.Api](src/Archu.Api/README.md) - REST API controllers
- [Archu.ServiceDefaults](src/Archu.ServiceDefaults/README.md) - Aspire defaults
- [Archu.AppHost](src/Archu.AppHost/README.md) - Orchestration

### Building the Solution
```bash
dotnet build
```

### Running Tests
```bash
dotnet test
```
> **Note:** Test projects coming soon. See [ARCHITECTURE_GUIDE.md](ARCHITECTURE_GUIDE.md) for test project setup.

### Code Quality
```bash
# Format code
dotnet format

# Analyze code
dotnet build /p:EnforceCodeStyleInBuild=true
```

## Technologies

### Core Stack
- **.NET 9** - Latest .NET runtime and libraries
- **C# 13** - Latest language features
- **ASP.NET Core 9** - Web API framework
- **Entity Framework Core 9** - ORM and data access

### Architecture & Patterns
- **Clean Architecture** - Separation of concerns
- **CQRS** - Command Query Responsibility Segregation
- **MediatR** - Mediator pattern implementation
- **FluentValidation** - Request validation
- **Repository Pattern** - Data access abstraction
- **Unit of Work Pattern** - Transaction management

### Infrastructure
- **.NET Aspire** - Cloud-native orchestration
- **SQL Server** - Relational database
- **Docker** - Containerization
- **OpenTelemetry** - Distributed tracing and metrics

### Documentation & Testing
- **OpenAPI/Swagger** - API specification
- **Scalar** - Modern API documentation UI
- **xUnit** (planned) - Unit testing framework
- **FluentAssertions** (planned) - Fluent test assertions

## Best Practices Implemented

- ✅ **Nullable Reference Types** enabled across all projects
- ✅ **Async/await** throughout for scalability
- ✅ **CancellationToken** support for graceful cancellation
- ✅ **Compile-time logging** with source generators
- ✅ **Global query filters** for soft delete
- ✅ **Row versioning** for optimistic concurrency
- ✅ **API versioning** for evolution
- ✅ **Health checks** for monitoring
- ✅ **Structured logging** for observability

## Contributing

We welcome contributions! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Coding Standards
- Follow Clean Architecture principles
- Write unit tests for new features
- Use async/await for I/O operations
- Add XML documentation for public APIs
- Follow existing code style

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- **Issues**: [GitHub Issues](https://github.com/chethandvg/archu/issues)
- **Discussions**: [GitHub Discussions](https://github.com/chethandvg/archu/discussions)
- **Documentation**: [Architecture Guide](ARCHITECTURE_GUIDE.md)

## Resources

### Architecture
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) - Uncle Bob Martin
- [Domain-Driven Design](https://martinfowler.com/bliki/DomainDrivenDesign.html) - Martin Fowler
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html) - Martin Fowler

### .NET & Aspire
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/)
- [ASP.NET Core Web API](https://learn.microsoft.com/aspnet/core/web-api/)
- [MediatR Documentation](https://github.com/jbogard/MediatR/wiki)

### Learning
- [Nick Chapsas](https://www.youtube.com/@nickchapsas) - .NET best practices
- [Milan Jovanović](https://www.youtube.com/@MilanJovanovicTech) - Clean Architecture & DDD
- [Jason Taylor's Clean Architecture Template](https://github.com/jasontaylordev/CleanArchitecture)

---

**Built with ❤️ using Clean Architecture and .NET 9**

**Last Updated:** 2025-01-19
