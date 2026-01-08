# TentMan Development Guide

Complete guide for developing features, following patterns, and maintaining code quality in TentMan.

---

## 📚 Table of Contents

- [Overview](#overview)
- [Development Workflow](#development-workflow)
- [Code Quality](#code-quality)
- [Common Patterns](#common-patterns)
- [Testing](#testing)
- [Best Practices](#best-practices)

---

## 🎯 Overview

### Development Stack

- **.NET 9** - Latest .NET version
- **Clean Architecture** - Separation of concerns
- **CQRS** - Command Query Responsibility Segregation
- **MediatR** - Request/response pipeline
- **Entity Framework Core 9** - ORM
- **FluentValidation** - Input validation
- **.NET Aspire** - Cloud-native development

---

## 🔄 Development Workflow

### Adding a New Feature

**See**: [../src/README_NEW_ENTITY.md](../src/README_NEW_ENTITY.md) for step-by-step guide

**Quick Summary**:
1. Create entity in `TentMan.Domain`
2. Create repository interface in `TentMan.Application`
3. Implement repository in `TentMan.Infrastructure`
4. Create DTOs in `TentMan.Contracts`
5. Create commands/queries in `TentMan.Application`
6. Create controller in `TentMan.Api`
7. Create migration
8. Test

### Project Structure

```
TentMan.Domain/              # Business entities
  └─ Entities/
      └─ Product.cs

TentMan.Application/         # Use cases
  └─ Products/
      ├─ Commands/
      │   ├─ CreateProduct/
      │   └─ UpdateProduct/
      └─ Queries/
          └─ GetProducts/

TentMan.Infrastructure/      # Data access
  └─ Repositories/
      └─ ProductRepository.cs

TentMan.Contracts/           # DTOs
  └─ Products/
      ├─ ProductDto.cs
      └─ CreateProductRequest.cs

TentMan.Api/                 # REST API
  └─ Controllers/
      └─ ProductsController.cs
```

---

## 📝 Code Quality

### BaseCommandHandler Pattern

**Purpose**: Reduce code duplication in command handlers

**Before** (Duplicated code):
```csharp
public class CreateProductHandler : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var product = new Product { Name = request.Name };
        await _repository.AddAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        var dto = _mapper.Map<ProductDto>(product);
        return Result<ProductDto>.Success(dto);
    }
}

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await _repository.GetByIdAsync(request.Id, ct);
        product.Name = request.Name;
        await _repository.UpdateAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        var dto = _mapper.Map<ProductDto>(product);
        return Result<ProductDto>.Success(dto);
    }
}
```

**After** (Using BaseCommandHandler):
```csharp
public abstract class BaseCommandHandler<TCommand, TResponse>
    : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : IRequest<Result<TResponse>>
{
    protected readonly IUnitOfWork UnitOfWork;
    protected readonly IMapper Mapper;

    protected BaseCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        UnitOfWork = unitOfWork;
        Mapper = mapper;
    }

    public async Task<Result<TResponse>> Handle(TCommand request, CancellationToken ct)
    {
        try
        {
            var result = await ExecuteAsync(request, ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return result;
        }
        catch (Exception ex)
        {
            return Result<TResponse>.Failure(ex.Message);
        }
    }

    protected abstract Task<Result<TResponse>> ExecuteAsync(TCommand request, CancellationToken ct);
}
```

**Usage**:
```csharp
public class CreateProductHandler : BaseCommandHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _repository;

    public CreateProductHandler(
        IProductRepository repository,
        IUnitOfWork unitOfWork,
        IMapper mapper) : base(unitOfWork, mapper)
    {
        _repository = repository;
    }

    protected override async Task<Result<ProductDto>> ExecuteAsync(
        CreateProductCommand request, CancellationToken ct)
    {
        var product = new Product { Name = request.Name };
        await _repository.AddAsync(product, ct);
        var dto = Mapper.Map<ProductDto>(product);
        return Result<ProductDto>.Success(dto);
    }
}
```

**Benefits**:
- ✅ Eliminates boilerplate code
- ✅ Consistent error handling
- ✅ Automatic transaction management (UnitOfWork)
- ✅ Easier to maintain

### Dependency Injection Improvements

**Clean Service Registration**:

**Before** (Program.cs):
```csharp
// Infrastructure
builder.Services.AddDbContext<ApplicationDbContext>(...);
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddAuthentication(...);
// ... 20+ lines of service registration
```

**After** (Using DependencyInjection.cs):
```csharp
// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// Application
builder.Services.AddApplication();
```

**Implementation** (TentMan.Infrastructure/DependencyInjection.cs):
```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(...);
        
        // Repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Authentication
        services.AddAuthentication(...);
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        
        // Other services
        services.AddScoped<ICurrentUser, CurrentUser>();
        
        return services;
    }
}
```

**Benefits**:
- ✅ Cleaner Program.cs
- ✅ Better organization
- ✅ Layer-specific registration
- ✅ Easier to maintain

---

## 🎨 Common Patterns

### 1. Repository Pattern

**Interface** (Application layer):
```csharp
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Product>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task DeleteAsync(Product product, CancellationToken ct = default);
}
```

**Implementation** (Infrastructure layer):
```csharp
public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<List<Product>> GetAllAsync(CancellationToken ct = default)
        => await DbSet.ToListAsync(ct);
}
```

### 2. CQRS Pattern

**Command** (Write operation):
```csharp
public record CreateProductCommand(string Name, decimal Price) 
    : IRequest<Result<ProductDto>>;

public class CreateProductHandler : BaseCommandHandler<CreateProductCommand, ProductDto>
{
    protected override async Task<Result<ProductDto>> ExecuteAsync(
        CreateProductCommand request, CancellationToken ct)
    {
        var product = new Product
        {
            Name = request.Name,
            Price = request.Price
        };

        await _repository.AddAsync(product, ct);
        var dto = Mapper.Map<ProductDto>(product);
        return Result<ProductDto>.Success(dto);
    }
}
```

**Query** (Read operation):
```csharp
public record GetProductsQuery() : IRequest<Result<List<ProductDto>>>;

public class GetProductsHandler : IRequestHandler<GetProductsQuery, Result<List<ProductDto>>>
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;

    public async Task<Result<List<ProductDto>>> Handle(
        GetProductsQuery request, CancellationToken ct)
    {
        var products = await _repository.GetAllAsync(ct);
        var dtos = _mapper.Map<List<ProductDto>>(products);
        return Result<List<ProductDto>>.Success(dtos);
    }
}
```

### 3. Result Pattern

**Purpose**: Explicit success/failure handling without exceptions

```csharp
public class Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }

    public static Result<T> Success(T value) => 
        new() { IsSuccess = true, Value = value };

    public static Result<T> Failure(string error) => 
        new() { IsSuccess = false, Error = error };
}
```

**Usage**:
```csharp
var result = await _mediator.Send(new CreateProductCommand("Laptop", 1299.99));

if (result.IsSuccess)
    return Ok(result.Value);
else
    return BadRequest(new { error = result.Error });
```

### 4. Unit of Work Pattern

**Interface**:
```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

**Implementation**:
```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}
```

---

## 🧪 Testing

### Unit Testing

**Test Command Handler**:
```csharp
public class CreateProductHandlerTests
{
    [Fact]
    public async Task Handle_ValidProduct_ReturnsSuccess()
    {
        // Arrange
        var repository = new Mock<IProductRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var mapper = new Mock<IMapper>();
        
        var handler = new CreateProductHandler(repository.Object, unitOfWork.Object, mapper.Object);
        var command = new CreateProductCommand("Laptop", 1299.99m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        repository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Integration Testing

**Test API Endpoint**:
```csharp
public class ProductsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ProductsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/products");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotNull(content);
    }
}
```

---

## ✅ Best Practices

### Code Organization

✅ **DO**:
- Follow Clean Architecture layers
- Keep controllers thin (delegate to MediatR)
- Use CQRS for clear separation
- Implement repository pattern
- Use dependency injection

❌ **DON'T**:
- Put business logic in controllers
- Access DbContext directly from controllers
- Mix read/write operations
- Use static classes for services

### Error Handling

✅ **DO**:
- Use Result pattern
- Return meaningful error messages
- Log errors appropriately
- Handle concurrency conflicts
- Validate input with FluentValidation

❌ **DON'T**:
- Swallow exceptions
- Return generic error messages
- Expose internal details
- Skip validation

### Performance

✅ **DO**:
- Use async/await consistently
- Implement pagination for large datasets
- Use projection (Select) to reduce data
- Add appropriate indexes
- Use caching where appropriate

❌ **DON'T**:
- Use blocking calls (`.Result`, `.Wait()`)
- Load unnecessary data
- Skip indexing on frequently queried columns
- Over-cache (stale data issues)

### Security

✅ **DO**:
- Validate all input
- Use parameterized queries (EF Core does this)
- Implement authorization checks
- Sanitize output
- Use HTTPS in production

❌ **DON'T**:
- Trust client input
- Skip authorization
- Expose sensitive data
- Use string concatenation for SQL

---

## 📚 Related Documentation

- **[ARCHITECTURE.md](ARCHITECTURE.md)** - System architecture
- **[../src/README_NEW_ENTITY.md](../src/README_NEW_ENTITY.md)** - Step-by-step feature guide
- **[API_GUIDE.md](API_GUIDE.md)** - API reference
- **[DATABASE_GUIDE.md](DATABASE_GUIDE.md)** - Database patterns

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Maintainer**: TentMan Development Team
