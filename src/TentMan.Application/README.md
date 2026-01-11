# TentMan.Application

The Application layer contains use cases, business rules, and application logic implemented using the CQRS pattern with MediatR.

---

## 📁 Folder Structure

```
TentMan.Application/
├── Abstractions/              # Application interfaces
│   ├── Billing/              # Billing service interfaces
│   ├── IRepository.cs        # Base repository interface
│   ├── IUnitOfWork.cs        # Unit of work pattern
│   └── Messaging/            # CQRS base types
├── Admin/                     # Admin-related features
│   ├── Commands/             # Create, Update, Delete
│   └── Queries/              # Read operations
├── Auth/                      # Authentication features
├── BackgroundJobs/            # Background job definitions
│   ├── MonthlyRentGenerationJob.cs
│   └── UtilityBillingJob.cs
├── Billing/                   # Billing and calculation services
│   └── Services/             # Proration, rent, utility calculators
├── Common/                    # Shared application services
├── Products/                  # Product feature module
│   ├── Commands/
│   │   ├── CreateProduct/
│   │   │   ├── CreateProductCommand.cs
│   │   │   ├── CreateProductCommandHandler.cs
│   │   │   └── CreateProductCommandValidator.cs
│   │   └── UpdateProduct/
│   └── Queries/
│       └── GetProducts/
│           ├── GetProductsQuery.cs
│           └── GetProductsQueryHandler.cs
├── PropertyManagement/        # Property management feature
│   ├── Buildings/
│   ├── Units/
│   ├── Owners/
│   └── Organizations/
├── TenantManagement/          # Tenant and lease management
│   ├── Common/                # Shared mappers (LeaseMapper, TenantMapper)
│   ├── Tenants/
│   │   ├── Commands/         # CreateTenant, UpdateTenant
│   │   └── Queries/          # GetTenants, GetTenantById
│   ├── TenantInvites/         # Tenant invite system
│   │   ├── Commands/
│   │   │   ├── GenerateInvite/
│   │   │   │   ├── GenerateInviteCommand.cs
│   │   │   │   ├── GenerateInviteCommandHandler.cs
│   │   │   │   └── GenerateInviteCommandValidator.cs
│   │   │   ├── AcceptInvite/
│   │   │   │   ├── AcceptInviteCommand.cs
│   │   │   │   ├── AcceptInviteCommandHandler.cs
│   │   │   │   └── AcceptInviteCommandValidator.cs
│   │   │   └── CancelInvite/
│   │   │       ├── CancelInviteCommand.cs
│   │   │       ├── CancelInviteCommandHandler.cs
│   │   │       └── CancelInviteCommandValidator.cs
│   │   └── Queries/
│   │       ├── ValidateInvite/
│   │       │   ├── ValidateInviteQuery.cs
│   │       │   ├── ValidateInviteQueryHandler.cs
│   │       │   └── ValidateInviteQueryValidator.cs
│   │       └── GetInvitesByTenant/
│   │           ├── GetInvitesByTenantQuery.cs
│   │           ├── GetInvitesByTenantQueryHandler.cs
│   │           └── GetInvitesByTenantQueryValidator.cs
│   └── Leases/
│       ├── Commands/         # CreateLease, AddParty, AddTerm, Activate
│       └── Queries/          # GetLeaseById, GetLeasesByUnit
└── docs/                      # Application layer documentation
```

---

## 🎯 Purpose

The Application layer:
- Implements business use cases
- Orchestrates domain operations
- Defines repository interfaces
- Contains validation logic
- Handles cross-cutting concerns

---

## 📋 Coding Guidelines

### CQRS Structure

Each feature should follow this folder structure:

```
FeatureName/
├── Commands/
│   ├── CreateFeature/
│   │   ├── CreateFeatureCommand.cs          # Command record
│   │   ├── CreateFeatureCommandHandler.cs   # Handler implementation
│   │   └── CreateFeatureCommandValidator.cs # Validation rules
│   └── UpdateFeature/
│       └── ...
└── Queries/
    ├── GetFeature/
    │   ├── GetFeatureQuery.cs
    │   └── GetFeatureQueryHandler.cs
    └── GetFeatures/
        └── ...
```

### Command Pattern

```csharp
namespace TentMan.Application.Products.Commands.CreateProduct;

/// <summary>
/// Command to create a new product.
/// </summary>
public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price) : ICommand<ProductDto>;
```

### Command Handler Pattern

```csharp
namespace TentMan.Application.Products.Commands.CreateProduct;

/// <summary>
/// Handles product creation.
/// </summary>
public sealed class CreateProductCommandHandler 
    : ICommandHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<CreateProductCommandHandler> _logger;
    
    public CreateProductCommandHandler(
        IProductRepository repository,
        ILogger<CreateProductCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<ProductDto> Handle(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating product: {Name}", command.Name);
        
        var product = new Product
        {
            Name = command.Name,
            Description = command.Description,
            Price = command.Price
        };
        
        await _repository.AddAsync(product, cancellationToken);
        
        return product.ToDto();
    }
}
```

### Query Pattern

```csharp
namespace TentMan.Application.Products.Queries.GetProducts;

/// <summary>
/// Query to retrieve all products.
/// </summary>
public sealed record GetProductsQuery(
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResult<ProductDto>>;
```

### File Size Limits

| Rule | Limit | Action |
|------|-------|--------|
| Handler file | 300 lines max | Extract to partial classes |
| Validator file | 150 lines max | Group related rules |

### When to Use Partial Classes

```
CreateProductCommandHandler.cs           # Main handler
CreateProductCommandHandler.Mapping.cs   # Mapping logic
CreateProductCommandHandler.Validation.cs # Additional validation
```

### Validation Pattern

```csharp
namespace TentMan.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandValidator 
    : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
            
        RuleFor(x => x.Price)
            .GreaterThan(0);
    }
}
```

---

## 🔗 Dependencies

- **TentMan.Domain**: Entity references
- **MediatR**: CQRS implementation
- **FluentValidation**: Validation framework
- **Microsoft.Extensions.Logging**: Logging abstractions

---

## 📚 Key Abstractions

### ICommand / IQuery

```csharp
public interface ICommand<TResponse> : IRequest<TResponse> { }
public interface IQuery<TResponse> : IRequest<TResponse> { }
```

### IRepository<T>

```csharp
public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
}
```

---

## ✅ Checklist for New Features

- [ ] Create feature folder under Application
- [ ] Implement Commands and Queries separately
- [ ] Add validators for all commands
- [ ] Add proper logging
- [ ] XML documentation on public types
- [ ] File size under 300 lines
- [ ] Unit tests in TentMan.UnitTests

---

**Last Updated**: 2026-01-09  
**Maintainer**: TentMan Development Team
