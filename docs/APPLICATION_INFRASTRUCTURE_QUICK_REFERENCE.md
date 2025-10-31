# Archu.Application & Archu.Infrastructure - Quick Reference

This document provides a concise overview of the Application and Infrastructure layers.

## 📊 Layer Comparison

| Aspect | **Archu.Application** | **Archu.Infrastructure** |
|--------|----------------------|--------------------------|
| **Purpose** | Business use cases & orchestration | External concerns & data access |
| **Dependencies** | Domain, Contracts | Domain, Application |
| **Framework** | .NET 9 | .NET 9 |
| **Key Pattern** | CQRS (MediatR) | Repository Pattern |
| **Defines** | Interfaces (abstractions) | Concrete implementations |
| **Contains** | Commands, Queries, Validators | DbContext, Repositories, Auth |

## 🏗️ Dependency Flow

```
    ┌──────────────────┐
   │  Archu.Domain    │
    │  (Entities)      │
         └────────┬─────────┘
         │
         ┌────────▼─────────┐
            │ Archu.Application│
   │ (Use Cases)      │
              │ Defines: I*      │
           └────────┬─────────┘
     │
      ┌────────▼─────────┐
    │Archu.Infrastructure│
 │ Implements: I*   │
        └──────────────────┘
```

## 📦 Project Structure Overview

### Archu.Application
```
Application/
├── Abstractions/          # Interfaces for infra
│   ├── Authentication/    # Auth interfaces
│   ├── Repositories/  # Repository interfaces
│   ├── ICurrentUser.cs
│   ├── ITimeProvider.cs
│   └── IUnitOfWork.cs
├── Admin/      # Admin operations
├── Auth/          # Authentication
├── Products/   # Product management
│   ├── Commands/         # Create, Update, Delete
│   ├── Queries/      # GetAll, GetById
│   └── Validators/       # FluentValidation
├── Common/
│   ├── Behaviors/        # MediatR pipeline
│   ├── Result.cs         # Result pattern
│   └── ApplicationRoles.cs
└── AssemblyReference.cs
```

### Archu.Infrastructure
```
Infrastructure/
├── Authentication/ # JWT, password hashing
│   ├── JwtTokenService.cs
│   ├── PasswordHasher.cs
│   └── PasswordValidator.cs
├── Persistence/          # EF Core
│   ├── ApplicationDbContext.cs
│   ├── Configurations/   # Entity configs
│   └── Migrations/     # EF migrations
├── Repositories/         # Repository impls
│   ├── BaseRepository.cs
│   ├── ProductRepository.cs
│   └── UnitOfWork.cs
├── Time/
│   └── SystemTimeProvider.cs
└── DependencyInjection.cs
```

## 🔑 Key Abstractions & Implementations

| Interface (Application) | Implementation (Infrastructure) | Purpose |
|------------------------|----------------------------------|---------|
| `IProductRepository` | `ProductRepository` | Product CRUD operations |
| `IUserRepository` | `UserRepository` | User management |
| `IUnitOfWork` | `UnitOfWork` | Transaction management |
| `IPasswordHasher` | `PasswordHasher` | Password hashing/verification |
| `IPasswordValidator` | `PasswordValidator` | Password complexity rules |
| `IJwtTokenService` | `JwtTokenService` | JWT token generation |
| `ICurrentUser` | `HttpContextCurrentUser` | Current user context |
| `ITimeProvider` | `SystemTimeProvider` | Testable time access |

## 🔄 CQRS Flow Example

### Command (Create Product)

**1. Application Layer** - Define command:
```csharp
// Archu.Application/Products/Commands/CreateProduct/CreateProductCommand.cs
public record CreateProductCommand(
    string Name,
    decimal Price) : IRequest<Result<ProductDto>>;
```

**2. Application Layer** - Handler:
```csharp
// Archu.Application/Products/Commands/CreateProduct/CreateProductCommandHandler.cs
public class CreateProductCommandHandler 
    : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IProductRepository _repository;  // ← Interface from Application
    private readonly IUnitOfWork _unitOfWork;
    
 public async Task<Result<ProductDto>> Handle(...)
    {
        var product = new Product { Name = request.Name, Price = request.Price };
        await _repository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success(new ProductDto { ... });
    }
}
```

**3. Infrastructure Layer** - Implementation:
```csharp
// Archu.Infrastructure/Repositories/ProductRepository.cs
public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        await DbSet.AddAsync(product, ct);
    }
}
```

**4. API Layer** - Controller:
```csharp
// Archu.Api/Controllers/ProductsController.cs
[HttpPost]
public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct(
    CreateProductRequest request)
{
    var command = new CreateProductCommand(request.Name, request.Price);
    var result = await _mediator.Send(command);
    
    if (!result.IsSuccess)
        return BadRequest(ApiResponse<object>.Fail(result.Error!));
        
    return Ok(ApiResponse<ProductDto>.Ok(result.Value!, "Product created"));
}
```

## 📚 Common Patterns

### Result Pattern (Application)
```csharp
public class Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    
    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Fail(string error) => new() { IsSuccess = false, Error = error };
}
```

### Repository Pattern (Infrastructure)
```csharp
// Application defines interface
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
  Task UpdateAsync(Product product, byte[] originalRowVersion, CancellationToken ct = default);
}

// Infrastructure implements
public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(p => p.Id == id, ct);
}
```

### Validation Pattern (Application)
```csharp
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
 {
     RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
 }
}
```

## 🔧 Service Registration

### Application Layer Registration (in API)
```csharp
// Program.cs
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(AssemblyReference).Assembly);
```

### Infrastructure Layer Registration
```csharp
// Program.cs
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// What it registers:
// - Database (SQL Server with retry logic)
// - JWT Authentication
// - All repository implementations (IProductRepository → ProductRepository)
// - Infrastructure services (ICurrentUser, ITimeProvider)
// - Password services (IPasswordHasher, IPasswordValidator)
```

## 📋 Checklist: Adding a New Entity

### ✅ Application Layer Tasks
- [ ] Create command/query records in `{Entity}/Commands|Queries/`
- [ ] Implement handlers with `IRequestHandler<,>`
- [ ] Add FluentValidation validators
- [ ] Define repository interface in `Abstractions/`
- [ ] Write unit tests

### ✅ Infrastructure Layer Tasks
- [ ] Create repository implementation extending `BaseRepository<T>`
- [ ] Add entity configuration in `Persistence/Configurations/`
- [ ] Add `DbSet<Entity>` to `ApplicationDbContext`
- [ ] Register repository in `DependencyInjection.cs`
- [ ] Create and apply EF migration
- [ ] Write integration tests

## 🎯 Key Takeaways

### Application Layer
- ✅ Contains **business logic** (what to do)
- ✅ Defines **interfaces** (abstractions)
- ✅ Uses **CQRS** pattern with MediatR
- ✅ Validates with **FluentValidation**
- ✅ Returns **Result<T>** for explicit error handling
- ❌ **Never** references infrastructure implementations

### Infrastructure Layer
- ✅ Implements **data access** (how to do it)
- ✅ Provides **concrete implementations** of Application interfaces
- ✅ Manages **database** with EF Core
- ✅ Handles **authentication** (JWT, passwords)
- ✅ Implements **repository pattern**
- ❌ **Never** contains business logic

## 📖 Quick Commands

### Create Migration
```bash
cd src/Archu.Infrastructure
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

### Run Tests
```bash
dotnet test
```

### Check References
```bash
# Application should only reference Domain and Contracts
# Infrastructure should reference Domain and Application
dotnet list src/Archu.Application/Archu.Application.csproj reference
dotnet list src/Archu.Infrastructure/Archu.Infrastructure.csproj reference
```

## 🔍 Troubleshooting

| Issue | Solution |
|-------|----------|
| Circular dependency error | Check that Application doesn't reference Infrastructure |
| Repository not found | Register in `DependencyInjection.cs` |
| Validation not working | Ensure validator is in Application assembly |
| DbContext errors | Check connection string in appsettings.json |
| Migration fails | Verify entity configuration in `Configurations/` |

## 📚 Full Documentation

- **Application**: [src/Archu.Application/README.md](../Archu.Application/README.md)
- **Infrastructure**: [src/Archu.Infrastructure/README.md](../Archu.Infrastructure/README.md)
- **Architecture**: [docs/ARCHITECTURE.md](../../docs/ARCHITECTURE.md)
- **Concurrency**: [docs/CONCURRENCY_GUIDE.md](../../docs/CONCURRENCY_GUIDE.md)

---

**Last Updated**: 2025-01-24  
**Version**: 1.0
