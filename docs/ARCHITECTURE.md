# TentMan Architecture Guide

## Overview

TentMan is built using **Clean Architecture** principles with **.NET Aspire** for cloud-native orchestration. This document explains the solution topology, project responsibilities, and architectural patterns.

---

## 🏗️ Architecture Layers

```
┌──────────────────────────────────────────┐
│         TentMan.AppHost                    │
│    (.NET Aspire Orchestration)           │
└─────────────┬────────────────────────────┘
              │
       ┌──────┴──────┐
       │             │
┌──────▼─────┐ ┌────▼──────────┐
│  TentMan.Api │ │ SQL Server DB │
│   (API)    │ │ (via Aspire)  │
└──────┬─────┘ └───────────────┘
       │
       │ References
       │
       ├─ TentMan.ServiceDefaults
       ├─ TentMan.Contracts
       ├─ TentMan.Infrastructure
       │     └─ TentMan.Application
       │           └─ TentMan.Domain
       │
┌──────▼────────────────┐
│     TentMan.Ui          │
│ (Blazor Components)   │
└───────────────────────┘
```

---

## 📦 Project Structure

### **TentMan.Domain** (Core Layer)
**Target Framework**: .NET 9

**Purpose**: Core business logic and entities - the heart of your application.

**Dependencies**: None (zero external dependencies)

**Contents**:
- `Entities/` - Domain entities (e.g., `Product`)
- `Common/` - Base classes (e.g., `BaseEntity`)
- `Abstractions/` - Domain interfaces (e.g., `ISoftDeletable`, `IAuditable`)

**Key Principle**: This layer should have **no dependencies** on other projects or frameworks. Pure business logic only.

**Example**:
```csharp
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    // + IAuditable and ISoftDeletable properties
}
```

---

### **TentMan.Application** (Application Layer)
**Target Framework**: .NET 9

**Purpose**: Use cases, application services, CQRS handlers, and abstractions.

**Dependencies**: 
- `TentMan.Domain`
- MediatR, FluentValidation

**Contents**:
- `Abstractions/` - Application interfaces (e.g., `ICurrentUser`, `ITimeProvider`, `IProductRepository`)
- `Products/Commands/` - Command handlers (Create, Update, Delete)
- `Products/Queries/` - Query handlers (GetAll, GetById)
- `Products/Validators/` - FluentValidation validators
- `Common/` - Result pattern, exceptions

**Key Principle**: Defines **what** the application does without caring **how** it's implemented.

**Example**:
```csharp
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(Product product, byte[] originalRowVersion, CancellationToken ct = default);
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(...)
    {
        var product = await _repository.GetByIdAsync(request.Id);
        product.Name = request.Name;
        await _repository.UpdateAsync(product, request.RowVersion);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success(new ProductDto { ... });
    }
}
```

---

### **TentMan.Infrastructure** (Infrastructure Layer)
**Target Framework**: .NET 9

**Purpose**: External concerns (database, time, caching, external APIs).

**Dependencies**:
- `TentMan.Domain`
- `TentMan.Application`
- Entity Framework Core 9
- SQL Server provider

**Contents**:
- `Persistence/` - EF Core `DbContext`, configurations, migrations
- `Repositories/` - Repository implementations (`ProductRepository`, `BaseRepository`)
- `Time/` - `SystemTimeProvider` implementation

**Key Principle**: Implements abstractions defined in `Application` layer. Handles all I/O operations.

**Example**:
```csharp
public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task UpdateAsync(Product product, byte[] originalRowVersion, CancellationToken ct = default)
    {
        SetOriginalRowVersion(product, originalRowVersion); // From BaseRepository
        DbSet.Update(product);
        return Task.CompletedTask;
    }
}
```

---

### **TentMan.Contracts** (API Contracts)
**Target Framework**: .NET 9

**Purpose**: Data Transfer Objects (DTOs) for API requests/responses.

**Dependencies**: None (or minimal - DataAnnotations)

**Contents**:
- `Products/` - `ProductDto`, `CreateProductRequest`, `UpdateProductRequest`
- `Common/` - `ApiResponse<T>`, `PagedResult<T>`

**Key Principle**: Keeps API contracts separate from domain entities. Prevents over-posting and provides API versioning flexibility.

**Example**:
```csharp
public sealed class ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}

public sealed class UpdateProductRequest
{
    [Required] public Guid Id { get; init; }
    [Required] public string Name { get; init; } = string.Empty;
    [Range(0, double.MaxValue)] public decimal Price { get; init; }
    [Required, MinLength(1)] public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
```

---

### **TentMan.Api** (Presentation Layer)
**Target Framework**: .NET 9

**Purpose**: ASP.NET Core Web API.

**Dependencies**:
- `TentMan.Contracts`
- `TentMan.Application`
- `TentMan.Infrastructure`
- `TentMan.ServiceDefaults`
- Scalar.AspNetCore (API documentation)

**Contents**:
- `Controllers/` - REST API endpoints
- `Auth/` - Authentication/authorization implementations
- `Program.cs` - Dependency injection, middleware pipeline

**Key Features**:
- OpenAPI/Swagger documentation via Scalar UI
- Service defaults integration (telemetry, health checks)
- SQL Server connection with retry logic
- API versioning

**Example**:
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class ProductsController : ControllerBase
{
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(
        Guid id, UpdateProductRequest request)
    {
        var command = new UpdateProductCommand(request.Id, request.Name, request.Price, request.RowVersion);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
            return NotFound(ApiResponse<object>.Fail(result.Error!));
            
        return Ok(ApiResponse<ProductDto>.Ok(result.Value!, "Product updated successfully"));
    }
}
```

---

### **TentMan.Ui** (Component Library)
**Target Framework**: .NET 9

**Purpose**: Reusable Blazor component library with MudBlazor.

**Platform Support**: 
- Blazor Server
- Blazor WebAssembly
- Blazor Hybrid (MAUI)

**Key Features**:
- Platform-agnostic (no ASP.NET Core dependencies)
- MudBlazor-based components
- CSS isolation
- Easy service registration with `AddTentManUi()`

---

### **TentMan.ServiceDefaults** (.NET Aspire Shared Defaults)
**Target Framework**: .NET 9

**Purpose**: Shared Aspire service configurations.

**Dependencies**: Aspire packages

**Contents**:
- `Extensions.cs` - Service defaults registration (telemetry, health checks, service discovery)

**Key Features**:
- OpenTelemetry integration
- Health checks
- Service discovery for Aspire orchestration
- Resilient HTTP clients

---

### **TentMan.AppHost** (.NET Aspire Orchestrator)
**Target Framework**: .NET 8 (Aspire requirement)

**Purpose**: Local development orchestration and deployment.

**Dependencies**:
- Aspire.Hosting.AppHost 9.5.1
- Aspire.Hosting.SqlServer 9.5.1

**Contents**:
- `Program.cs` - Resource definitions (API, SQL Server)
- `ResourceBuilderExtensions.cs` - Custom Aspire extensions

**Key Features**:
- Automatic SQL Server container provisioning
- Service orchestration for local development
- Dashboard for monitoring services
- Custom Scalar API documentation command

---

## 🔀 Dependency Flow

```
TentMan.Domain (no dependencies)
      ↑
TentMan.Application
      ↑
TentMan.Infrastructure
      ↑
TentMan.Api ← TentMan.Contracts
      ↑
TentMan.ServiceDefaults
      ↑
TentMan.AppHost
```

**Dependency Rule**: Inner layers (Domain) should **never** depend on outer layers (API, Infrastructure). Dependencies point **inward**.

---

## 🎯 Architectural Patterns

### **Clean Architecture**
- **Core Principle**: Business logic is independent of frameworks, UI, and databases
- **Benefits**:
  - Testability (domain logic can be tested without database)
  - Maintainability (changes to UI don't affect business logic)
  - Flexibility (swap EF Core for Dapper, or SQL Server for PostgreSQL)

### **Dependency Inversion Principle (DIP)**
- Interfaces defined in `Application` layer
- Implementations in `Infrastructure` layer
- Example: `ITimeProvider` → `SystemTimeProvider`

### **CQRS (Command Query Responsibility Segregation)**
- **Commands**: Mutate state (CreateProduct, UpdateProduct, DeleteProduct)
- **Queries**: Read state (GetProducts, GetProductById)
- Clear separation of read/write operations
- Uses MediatR for request handling

### **Repository Pattern**
- Abstractions in `Application` layer (e.g., `IProductRepository`)
- Implementations in `Infrastructure` layer
- Provides testability and flexibility
- `BaseRepository<T>` for common functionality

### **Unit of Work Pattern**
- Implemented via `IUnitOfWork` interface
- Encapsulates transaction management
- Groups multiple repository operations

---

## 🌐 .NET Aspire Integration

### **What is .NET Aspire?**
.NET Aspire is an opinionated, cloud-ready stack for building observable, production-ready, distributed applications.

### **Features**:
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
When running `TentMan.AppHost`, you get:
- Real-time service status
- Logs from all services
- Distributed tracing
- Resource management (SQL Server, APIs)

---

## 🗄️ Database Strategy

### **Entity Framework Core**
- **Version**: 9.0.10
- **Provider**: SQL Server
- **Migrations**: Code-first migrations managed in `TentMan.Infrastructure`

### **Key Features**:
1. **Optimistic Concurrency Control** via `rowversion` column
2. **Soft Delete** with global query filters
3. **Automatic Audit Tracking** (CreatedAt, ModifiedAt, etc.)

### **Applying Migrations**:

```bash
# Development (automatic on startup)
# Enabled in Program.cs during development

# CI/CD Pipeline
dotnet ef database update --project src/TentMan.Infrastructure

# Create new migration
dotnet ef migrations add YourMigrationName --project src/TentMan.Infrastructure
```

---

## 🧪 Testing Strategy

### **Unit Tests**
- **Target**: `TentMan.Domain`, `TentMan.Application`
- **Tools**: xUnit, FluentAssertions
- **Focus**: Business logic without dependencies

### **Integration Tests**
- **Target**: `TentMan.Infrastructure`, `TentMan.Api`
- **Tools**: xUnit, Testcontainers (for SQL Server), WebApplicationFactory
- **Focus**: Database interactions, API endpoints

### **End-to-End Tests**
- **Target**: Full application via Aspire orchestration
- **Tools**: Playwright, SpecFlow
- **Focus**: User workflows

---

## 🔒 Security Considerations

### **Current State**
- `ICurrentUser` abstraction exists
- `HttpContextCurrentUser` implementation (placeholder)

### **Future Enhancements**
- **Authentication**: Add ASP.NET Core Identity or JWT Bearer tokens
- **Authorization**: Policy-based authorization with `[Authorize]` attributes
- **API Keys**: For machine-to-machine communication

---

## 🚀 Deployment

### **Local Development**
```bash
cd src/TentMan.AppHost
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

## 📋 Best Practices

### **When Adding New Features**
1. **Domain entities** go in `TentMan.Domain/Entities/`
2. **Application interfaces** go in `TentMan.Application/Abstractions/`
3. **Infrastructure implementations** go in `TentMan.Infrastructure/`
4. **API contracts** go in `TentMan.Contracts/`
5. **Controllers** go in `TentMan.Api/Controllers/`

Always respect the dependency flow: **Domain ← Application ← Infrastructure ← API**.

### **Code Quality**
- Follow SOLID principles
- Use dependency injection
- Write unit tests for business logic
- Use integration tests for API endpoints
- Keep controllers thin (delegate to MediatR)

---

## 📚 Further Reading

- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Entity Framework Core Best Practices](https://learn.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Security](https://learn.microsoft.com/en-us/aspnet/core/security/)

---

**Last Updated**: 2025-01-22  
**Version**: 2.0  
**Maintainer**: TentMan Development Team
