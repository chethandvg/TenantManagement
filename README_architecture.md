# Archu Architecture Documentation

## Overview

Archu is built using **Clean Architecture** principles with **.NET Aspire** for cloud-native orchestration. This document explains the solution topology, project responsibilities, and architectural patterns.

---

## ??? Architecture Layers

```
???????????????????????????????????????????????????????????????
?                      Archu.AppHost                          ?
?              (.NET Aspire Orchestration)                    ?
???????????????????????????????????????????????????????????????
                             ?
                ???????????????????????????
                ?                         ?
????????????????????????????????  ????????????????????????????
?        Archu.Api             ?  ?   SQL Server Database    ?
?    (ASP.NET Core Web API)    ?  ?   (Orchestrated by       ?
?    ????????????????????????  ?  ?    Aspire)               ?
?    ?  Archu.ServiceDefaults? ?  ????????????????????????????
?    ????????????????????????  ?
?    ????????????????????????  ?
?    ?  Archu.Contracts     ?  ?
?    ????????????????????????  ?
?    ????????????????????????  ?
?    ?  Archu.Infrastructure?  ?
?    ?  (EF Core, Providers)?  ?
?    ????????????????????????  ?
?    ????????????????????????  ?
?    ?  Archu.Application   ?  ?
?    ????????????????????????  ?
?    ????????????????????????  ?
?    ?  Archu.Domain        ?  ?
?    ????????????????????????  ?
????????????????????????????????
```

---

## ?? Project Structure

### **Archu.Domain** (Core Layer)
- **Purpose**: Core business logic and entities
- **Target Framework**: .NET 9
- **Dependencies**: None (zero external dependencies)
- **Contents**:
  - `Entities/` - Domain entities (e.g., `Product`)
  - `Common/` - Base classes (e.g., `BaseEntity`)
  - `Abstractions/` - Domain interfaces (e.g., `ISoftDeletable`, `IAuditable`)

**Key Principle**: This layer should have **no dependencies** on other projects or frameworks. It represents pure business logic.

---

### **Archu.Application** (Application Layer)
- **Purpose**: Use cases, application services, and abstractions
- **Target Framework**: .NET 9
- **Dependencies**: 
  - `Archu.Domain`
- **Contents**:
  - `Abstractions/` - Application interfaces (e.g., `ICurrentUser`, `ITimeProvider`)
  - Future: Command/Query handlers (CQRS with MediatR)
  - Future: DTOs, Validators, Mappers

**Key Principle**: Defines **what** the application does without caring **how** it's implemented.

---

### **Archu.Infrastructure** (Infrastructure Layer)
- **Purpose**: External concerns (database, time, caching, etc.)
- **Target Framework**: .NET 9
- **Dependencies**:
  - `Archu.Domain`
  - `Archu.Application`
  - Entity Framework Core 9.0.10
  - SQL Server provider
- **Contents**:
  - `Persistence/` - EF Core `DbContext`, configurations, migrations
  - `Time/` - `SystemTimeProvider` implementation
  - Future: External API clients, email services, etc.

**Key Principle**: Implements abstractions defined in `Application` layer. Handles all I/O operations.

---

### **Archu.Contracts** (API Contracts)
- **Purpose**: Data Transfer Objects (DTOs) for API requests/responses
- **Target Framework**: .NET 9
- **Dependencies**: None
- **Contents**:
  - `Products/` - `ProductDto`, `CreateProductRequest`, `UpdateProductRequest`
  - Future: Validation attributes, JSON serialization settings

**Key Principle**: Keeps API contracts separate from domain entities. Prevents over-posting and provides API versioning flexibility.

---

### **Archu.Api** (Presentation Layer)
- **Purpose**: ASP.NET Core Web API
- **Target Framework**: .NET 9
- **Dependencies**:
  - `Archu.Contracts`
  - `Archu.Application`
  - `Archu.Infrastructure`
  - `Archu.ServiceDefaults`
  - Scalar.AspNetCore 2.9.0 (API documentation)
- **Contents**:
  - `Controllers/` - REST API endpoints
  - `Auth/` - Authentication/authorization implementations
  - `Program.cs` - Dependency injection, middleware pipeline

**Key Features**:
- OpenAPI/Swagger documentation via Scalar UI
- Service defaults integration (telemetry, health checks)
- SQL Server connection with retry logic

---

### **Archu.ServiceDefaults** (.NET Aspire Shared Defaults)
- **Purpose**: Shared Aspire service configurations
- **Target Framework**: .NET 9
- **Dependencies**: Aspire packages
- **Contents**:
  - `Extensions.cs` - Service defaults registration (telemetry, health checks, service discovery)

**Key Features**:
- OpenTelemetry integration
- Health checks
- Service discovery for Aspire orchestration

---

### **Archu.AppHost** (.NET Aspire Orchestrator)
- **Purpose**: Local development orchestration and deployment
- **Target Framework**: .NET 8 (Aspire requirement)
- **Dependencies**:
  - Aspire.Hosting.AppHost 9.5.1
  - Aspire.Hosting.SqlServer 9.5.1
- **Contents**:
  - `Program.cs` - Resource definitions (API, SQL Server)
  - `ResourceBuilderExtensions.cs` - Custom Aspire extensions (Scalar UI integration)

**Key Features**:
- Automatic SQL Server container provisioning
- Service orchestration for local development
- Dashboard for monitoring services
- Custom Scalar API documentation command

---

## ?? Dependency Flow

```
Archu.Domain (no dependencies)
      ?
Archu.Application
      ?
Archu.Infrastructure
      ?
Archu.Api ? Archu.Contracts
      ?
Archu.ServiceDefaults
      ?
Archu.AppHost
```

**Dependency Rule**: Inner layers (Domain) should **never** depend on outer layers (API, Infrastructure). Dependencies point **inward**.

---

## ?? Architectural Patterns

### **Clean Architecture**
- **Core Principle**: Business logic is independent of frameworks, UI, and databases
- **Benefits**:
  - Testability (domain logic can be tested without database)
  - Maintainability (changes to UI don't affect business logic)
  - Flexibility (swap EF Core for Dapper, or SQL Server for PostgreSQL)

### **Dependency Inversion Principle (DIP)**
- Interfaces defined in `Application` layer
- Implementations in `Infrastructure` layer
- Example: `ITimeProvider` ? `SystemTimeProvider`

### **CQRS Preparation**
- Structure supports future CQRS implementation with MediatR
- Commands/Queries can be added to `Application` layer
- Handlers would live in `Application`, repositories in `Infrastructure`

---

## ?? .NET Aspire Integration

### **What is .NET Aspire?**
.NET Aspire is an opinionated, cloud-ready stack for building observable, production-ready, distributed applications. It provides:
- **Service orchestration** (local development)
- **Built-in telemetry** (OpenTelemetry)
- **Service discovery**
- **Health checks**
- **Deployment abstractions**

### **Why Aspire?**
- **Local Development**: Automatically spins up SQL Server, APIs, and dependencies
- **Observability**: Built-in structured logging, distributed tracing, and metrics
- **Cloud-Ready**: Seamless deployment to Azure Container Apps or Kubernetes
- **Developer Experience**: Dashboard for monitoring services during development

### **Aspire Dashboard**
When running `Archu.AppHost`, you get:
- Real-time service status
- Logs from all services
- Distributed tracing
- Resource management (SQL Server, APIs)

---

## ??? Database Strategy

### **Entity Framework Core**
- **Version**: 9.0.10
- **Provider**: SQL Server
- **Migrations**: Code-first migrations managed in `Archu.Infrastructure`

### **Applying Migrations**

#### **Option 1: Development (via Aspire)**
Migrations are applied automatically on startup in development (currently commented out in `Program.cs`):

```csharp
// Apply migrations in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}
```

#### **Option 2: CI/CD Pipeline**
```bash
dotnet ef database update --project src/Archu.Infrastructure
```

### **Design-Time DbContext Factory**
`DesignTimeDbContextFactory` allows EF Core tools to create migrations without running the full application.

---

## ?? Design Patterns

### **Repository Pattern** (Future)
- Abstractions in `Application` layer (e.g., `IProductRepository`)
- Implementations in `Infrastructure` layer
- Provides testability and flexibility

### **Unit of Work Pattern** (Future)
- Can be implemented via EF Core's `DbContext`
- Encapsulates transaction management

### **Options Pattern**
- Configuration bound to strongly-typed classes
- Example: Database connection strings, API settings

---

## ?? Testing Strategy (Recommended)

### **Unit Tests**
- **Target**: `Archu.Domain`, `Archu.Application`
- **Tools**: xUnit, FluentAssertions
- **Focus**: Business logic without dependencies

### **Integration Tests**
- **Target**: `Archu.Infrastructure`, `Archu.Api`
- **Tools**: xUnit, Testcontainers (for SQL Server), WebApplicationFactory
- **Focus**: Database interactions, API endpoints

### **End-to-End Tests**
- **Target**: Full application via Aspire orchestration
- **Tools**: Playwright, SpecFlow
- **Focus**: User workflows

---

## ?? Security Considerations

### **Current State**
- `ICurrentUser` abstraction exists
- `HttpContextCurrentUser` implementation (placeholder)

### **Future Enhancements**
- **Authentication**: Add ASP.NET Core Identity or JWT Bearer tokens
- **Authorization**: Policy-based authorization with `[Authorize]` attributes
- **API Keys**: For machine-to-machine communication

---

## ?? Deployment

### **Local Development**
```bash
cd src/Archu.AppHost
dotnet run
```
Opens Aspire dashboard and spins up all services.

### **Azure Deployment (via Aspire)**
```bash
azd init
azd up
```
Deploys to Azure Container Apps with:
- Managed SQL Server database
- Application Insights for telemetry
- Auto-scaling and load balancing

### **Docker/Kubernetes**
Aspire generates deployment manifests:
```bash
dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer
```

---

## ?? Further Reading

- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Entity Framework Core Best Practices](https://learn.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Security](https://learn.microsoft.com/en-us/aspnet/core/security/)

---

## ?? Contributing

When adding new features:
1. **Domain entities** go in `Archu.Domain/Entities/`
2. **Application interfaces** go in `Archu.Application/Abstractions/`
3. **Infrastructure implementations** go in `Archu.Infrastructure/`
4. **API contracts** go in `Archu.Contracts/`
5. **Controllers** go in `Archu.Api/Controllers/`

Always respect the dependency flow: **Domain ? Application ? Infrastructure ? API**.

---

## ?? Support

For architecture questions or issues, please:
- Review this document
- Check individual project `README.md` files
- Consult the team lead

---

**Last Updated**: 2025-01-10  
**Architecture Version**: 1.0  
**Maintainer**: Archu Development Team
