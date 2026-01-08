# TentMan Project Structure

Complete guide to all projects in the TentMan solution.

## Solution Overview

TentMan follows **Clean Architecture** with **.NET Aspire** orchestration.

```
TentMan/
├── src/
│   ├── TentMan.Domain/              # Domain Layer (Core Business Logic)
│   ├── TentMan.Application/         # Application Layer (Use Cases)
│   ├── TentMan.Infrastructure/      # Infrastructure Layer (Data Access)
│   ├── TentMan.Contracts/           # API Contracts (DTOs)
│   ├── TentMan.Api/                 # Presentation Layer (Web API)
│   ├── TentMan.ApiClient/           # API Client Library
│   ├── TentMan.Ui/                  # Blazor Component Library
│   ├── TentMan.AppHost/             # .NET Aspire Orchestrator
│   └── TentMan.ServiceDefaults/     # Aspire Service Defaults
└── docs/                          # Documentation
```

---

## Core Projects

### TentMan.Domain
**Target**: .NET 9  
**Dependencies**: None (zero dependencies)

Domain entities and business logic - the heart of the application.

**Contents**:
- `Entities/` - Domain entities (Product, ApplicationUser, ApplicationRole)
- `Common/` - Base classes (BaseEntity)
- `Abstractions/` - Domain interfaces (ISoftDeletable, IAuditable)

**Key Types**:
```csharp
// Base entity with concurrency, auditing, soft delete
public abstract class BaseEntity : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; }
    // Auditing: CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
    // Soft Delete: IsDeleted, DeletedAtUtc, DeletedBy
}

// Business entities
public class Product : BaseEntity { }
public class ApplicationUser : BaseEntity { }
public class ApplicationRole : BaseEntity { }
```

**Principle**: Zero dependencies. Pure business logic only.

---

### TentMan.Application
**Target**: .NET 9  
**Dependencies**: TentMan.Domain, MediatR, FluentValidation

Application layer with use cases, CQRS handlers, and abstractions.

**Contents**:
- `Abstractions/` - Interfaces (ICurrentUser, IUnitOfWork, IProductRepository)
- `Products/Commands/` - Commands (Create, Update, Delete)
- `Products/Queries/` - Queries (GetAll, GetById)
- `Common/Behaviors/` - MediatR behaviors (Validation, Performance)

**Key Patterns**:
```csharp
// Command
public record CreateProductCommand(string Name, decimal Price) 
    : IRequest<Result<ProductDto>>;

// Query
public record GetProductByIdQuery(Guid Id) 
    : IRequest<Result<ProductDto>>;

// Repository abstraction
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(Product product, byte[] originalRowVersion, CancellationToken ct = default);
}
```

**Principle**: Defines **what** the application does, not **how**.

---

### TentMan.Infrastructure
**Target**: .NET 9  
**Dependencies**: TentMan.Domain, TentMan.Application, EF Core 9, SQL Server

Infrastructure implementations of application interfaces.

**Contents**:
- `Persistence/` - DbContext, configurations, migrations
- `Repositories/` - Repository implementations (ProductRepository, UserRepository)
- `Authentication/` - JWT services, password hashing, current user
- `Time/` - SystemTimeProvider

**Key Implementations**:
```csharp
// Repository
public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    public Task UpdateAsync(Product product, byte[] originalRowVersion, CancellationToken ct)
    {
        SetOriginalRowVersion(product, originalRowVersion);
        DbSet.Update(product);
        return Task.CompletedTask;
    }
}

// Authentication
public class JwtTokenService : IJwtTokenService { }
public class PasswordHasher : IPasswordHasher { }
public class HttpContextCurrentUser : ICurrentUser { }
```

**Principle**: Implements abstractions from Application layer.

---

### TentMan.Contracts
**Target**: .NET 9  
**Dependencies**: None (or minimal - DataAnnotations)

Data Transfer Objects (DTOs) for API requests and responses.

**Contents**:
- `Products/` - ProductDto, CreateProductRequest, UpdateProductRequest
- `Common/` - ApiResponse<T>, PagedResult<T>

**Key Types**:
```csharp
public sealed class ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>(); // Concurrency
}

public sealed class UpdateProductRequest
{
    [Required] public Guid Id { get; init; }
    [Required] public string Name { get; init; } = string.Empty;
    [Range(0, double.MaxValue)] public decimal Price { get; init; }
    [Required, MinLength(1)] public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
```

**Principle**: Prevents over-posting, enables API versioning.

---

### TentMan.Api
**Target**: .NET 9  
**Dependencies**: All above projects, Scalar.AspNetCore

ASP.NET Core Web API with controllers and middleware.

**Contents**:
- `Controllers/` - REST API endpoints (ProductsController)
- `Auth/` - Authentication/authorization implementations
- `Middleware/` - Global exception handler
- `Program.cs` - DI configuration, middleware pipeline

**Features**:
- OpenAPI/Swagger via Scalar UI
- Service defaults integration
- SQL Server with retry logic
- API versioning
- JWT authentication

**Example Controller**:
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
        var command = new UpdateProductCommand(
            request.Id, request.Name, request.Price, request.RowVersion);
        var result = await _mediator.Send(command);
        
        return result.IsSuccess 
            ? Ok(ApiResponse<ProductDto>.Ok(result.Value!))
            : NotFound(ApiResponse<object>.Fail(result.Error!));
    }
}
```

---

## Supporting Projects

### TentMan.ApiClient
**Target**: .NET 9  
**Platform**: Cross-platform (works with any .NET 9 app)

Strongly-typed HTTP client for consuming the TentMan API.

**Features**:
- Type-safe API calls
- Automatic error handling
- Built-in retry policies
- Support for authentication

**Usage**:
```csharp
// Register client
builder.Services.AddTentManApiClient(options => 
{
    options.BaseAddress = "https://api.tentman.com";
});

// Use client
var response = await _productsClient.GetProductByIdAsync(id);
if (response.Success)
{
    var product = response.Data;
}
```

**Documentation**: See `src/TentMan.ApiClient/README.md` and `RESILIENCE.md`

---

### TentMan.Ui
**Target**: .NET 9  
**Platform**: Blazor (Server, WebAssembly, Hybrid)

Reusable Blazor component library with MudBlazor.

**Features**:
- Platform-agnostic components
- MudBlazor-based UI
- CSS isolation
- Easy integration

**Usage**:
```csharp
// Register services
builder.Services.AddTentManUi();

// Use components
<ProductCard Product="@product" OnEdit="HandleEdit" />
```

**Documentation**: See `src/TentMan.Ui/README.md`, `CHANGELOG.md`, `INTEGRATION.md`

---

### TentMan.AppHost
**Target**: .NET 8 (Aspire requirement)  
**Type**: .NET Aspire Orchestrator

Local development orchestration and deployment.

**Features**:
- Automatic SQL Server provisioning
- Service orchestration
- Dashboard for monitoring
- Custom Scalar API documentation command

**Usage**:
```bash
cd src/TentMan.AppHost
dotnet run
```

Opens Aspire dashboard at https://localhost:17001

**Resources Managed**:
- TentMan.Api (Web API)
- SQL Server database
- Service discovery
- Telemetry collection

---

### TentMan.ServiceDefaults
**Target**: .NET 9  
**Type**: Shared Aspire Configuration

Common Aspire service configurations.

**Features**:
- OpenTelemetry integration
- Health checks
- Service discovery
- Resilient HTTP clients

**Usage**:
```csharp
builder.AddServiceDefaults();
```

---

## Dependency Flow

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

**Rule**: Dependencies point **inward**. Inner layers never depend on outer layers.

---

## Key Technologies

| Layer | Technologies |
|-------|-------------|
| Domain | .NET 9, C# 13 |
| Application | MediatR 12.4, FluentValidation 11.11 |
| Infrastructure | EF Core 9.0, SQL Server, ASP.NET Core Identity 2.2 |
| API | ASP.NET Core 9.0, Scalar.AspNetCore, JWT Bearer |
| Orchestration | .NET Aspire 9.5 |

---

## Project Templates

### Adding a New Entity

See [docs/getting-started/ADDING_NEW_ENTITY.md](../docs/getting-started/ADDING_NEW_ENTITY.md)

**Quick Steps**:
1. Create entity in Domain
2. Create repository interface in Application
3. Implement repository in Infrastructure
4. Add to Unit of Work
5. Create commands/queries
6. Add API controller

---

## Build & Run

### Build Solution
```bash
dotnet build
```

### Run with Aspire
```bash
cd src/TentMan.AppHost
dotnet run
```

### Run API Only
```bash
cd src/TentMan.Api
dotnet run
```

### Run Tests
```bash
dotnet test
```

---

## Documentation

- **[Main Documentation](../docs/README.md)** - Documentation hub
- **[Architecture Guide](../docs/ARCHITECTURE.md)** - Detailed architecture
- **[Authentication](../docs/authentication/)** - Auth system
- **[Database](../docs/database/)** - Data access patterns

---

**Last Updated**: 2025-01-22  
**Version**: 2.0 (Consolidated)
