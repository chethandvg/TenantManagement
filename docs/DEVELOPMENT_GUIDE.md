# Archu Development Guide

Complete guide for developing features, following patterns, and maintaining code quality in Archu.

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

**See**: [Adding New Entities Tutorial](#adding-new-entities---step-by-step-tutorial) below for complete step-by-step guide

**Quick Summary**:
1. Create entity in `Archu.Domain`
2. Create repository interface in `Archu.Application`
3. Implement repository in `Archu.Infrastructure`
4. Create DTOs in `Archu.Contracts`
5. Create commands/queries in `Archu.Application`
6. Create controller in `Archu.Api`
7. Create migration
8. Test

### Project Structure

```
Archu.Domain/              # Business entities
  └─ Entities/
      └─ Product.cs

Archu.Application/         # Use cases
  └─ Products/
      ├─ Commands/
      │   ├─ CreateProduct/
      │   └─ UpdateProduct/
      └─ Queries/
          └─ GetProducts/

Archu.Infrastructure/      # Data access
  └─ Repositories/
      └─ ProductRepository.cs

Archu.Contracts/           # DTOs
  └─ Products/
      ├─ ProductDto.cs
      └─ CreateProductRequest.cs

Archu.Api/                 # REST API
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

**Implementation** (Archu.Infrastructure/DependencyInjection.cs):
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
- **[API_GUIDE.md](API_GUIDE.md)** - API reference
- **[DATABASE_GUIDE.md](DATABASE_GUIDE.md)** - Database patterns
- **[AUTHENTICATION_GUIDE.md](AUTHENTICATION_GUIDE.md)** - Authentication setup

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Maintainer**: Archu Development Team

---

## 📝 Adding New Entities - Step-by-Step Tutorial

This comprehensive tutorial shows you how to add a new entity to Archu with full CRUD operations, concurrency control, soft delete, and audit tracking.

### Example: Adding an Order Entity

#### Prerequisites

- Understanding of Clean Architecture layers
- Familiarity with Entity Framework Core
- Basic knowledge of CQRS pattern with MediatR

#### Step 1: Create the Domain Entity

**Location**: `src/Archu.Domain/Entities/Order.cs`

```csharp
using Archu.Domain.Common;

namespace Archu.Domain.Entities;

/// <summary>
/// Represents a customer order.
/// Inherits BaseEntity which provides:
/// - Id, RowVersion (concurrency control)
/// - CreatedAtUtc, ModifiedAtUtc, CreatedBy, ModifiedBy (auditing)
/// - IsDeleted, DeletedAtUtc, DeletedBy (soft delete)
/// </summary>
public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime OrderDate { get; set; }
}

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}
```

✅ **That's it!** By inheriting from `BaseEntity`, you automatically get concurrency control, soft delete, and audit tracking.

#### Step 2: Create Entity Configuration

**Location**: `src/Archu.Infrastructure/Persistence/Configurations/OrderConfiguration.cs`

```csharp
using Archu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Archu.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(o => o.OrderNumber)
            .IsUnique();

        builder.Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(o => o.OrderDate)
            .IsRequired();
    }
}
```

#### Step 3: Add DbSet to ApplicationDbContext

**Location**: `src/Archu.Infrastructure/Persistence/ApplicationDbContext.cs`

```csharp
public DbSet<Order> Orders => Set<Order>();
```

#### Step 4: Create and Apply Migration

```bash
cd src/Archu.Infrastructure
dotnet ef migrations add AddOrderEntity --startup-project ../Archu.Api
dotnet ef database update --startup-project ../Archu.Api
```

#### Step 5: Create Repository Interface

**Location**: `src/Archu.Application/Abstractions/Repositories/IOrderRepository.cs`

```csharp
using Archu.Domain.Entities;

namespace Archu.Application.Abstractions.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default);
    Task<IEnumerable<Order>> GetAllAsync(CancellationToken ct = default);
    Task<Order> AddAsync(Order order, CancellationToken ct = default);
    Task UpdateAsync(Order order, byte[] originalRowVersion, CancellationToken ct = default);
    Task DeleteAsync(Order order, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}
```

#### Step 6: Implement Repository

**Location**: `src/Archu.Infrastructure/Repositories/OrderRepository.cs`

```csharp
using Archu.Application.Abstractions.Repositories;
using Archu.Domain.Entities;
using Archu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Archu.Infrastructure.Repositories;

public class OrderRepository : BaseRepository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await DbSet.FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default)
    {
        return await DbSet.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);
    }

    public async Task<IEnumerable<Order>> GetAllAsync(CancellationToken ct = default)
    {
        return await DbSet.ToListAsync(ct);
    }

    public async Task<Order> AddAsync(Order order, CancellationToken ct = default)
    {
        await DbSet.AddAsync(order, ct);
        return order;
    }

    public Task UpdateAsync(Order order, byte[] originalRowVersion, CancellationToken ct = default)
    {
        SetOriginalRowVersion(order, originalRowVersion);
        DbSet.Update(order);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Order order, CancellationToken ct = default)
    {
        SoftDelete(order);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return await DbSet.AnyAsync(o => o.Id == id, ct);
    }
}
```

#### Step 7: Register Repository in DI

**Location**: `src/Archu.Infrastructure/DependencyInjection.cs`

```csharp
services.AddScoped<IOrderRepository, OrderRepository>();
```

#### Step 8: Create DTOs

**Location**: `src/Archu.Contracts/Orders/OrderDto.cs`

```csharp
namespace Archu.Contracts.Orders;

public sealed class OrderDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public int Status { get; init; }
    public DateTime OrderDate { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
```

#### Step 9: Create Commands and Queries

**Create Order Command:**

**Location**: `src/Archu.Application/Orders/Commands/CreateOrder/CreateOrderCommand.cs`

```csharp
using Archu.Application.Abstractions;
using Archu.Contracts.Orders;
using MediatR;

namespace Archu.Application.Orders.Commands.CreateOrder;

public sealed record CreateOrderCommand(
    string OrderNumber,
    decimal TotalAmount,
    int Status
) : IRequest<Result<OrderDto>>;
```

**Command Handler:**

```csharp
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = request.OrderNumber,
            TotalAmount = request.TotalAmount,
            Status = (OrderStatus)request.Status,
            OrderDate = DateTime.UtcNow
        };

        await _unitOfWork.Orders.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Created order {OrderId}", order.Id);

        return Result<OrderDto>.Success(new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            TotalAmount = order.TotalAmount,
            Status = (int)order.Status,
            OrderDate = order.OrderDate,
            RowVersion = order.RowVersion
        });
    }
}
```

**Update Order Command:**

**Location**: `src/Archu.Application/Orders/Commands/UpdateOrder/UpdateOrderCommand.cs`

```csharp
public sealed record UpdateOrderCommand(
    Guid Id,
    string OrderNumber,
    decimal TotalAmount,
    int Status,
    byte[] RowVersion
) : IRequest<Result<OrderDto>>;
```

**Command Handler:**

```csharp
public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand, Result<OrderDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateOrderCommandHandler> _logger;

    public async Task<Result<OrderDto>> Handle(UpdateOrderCommand request, CancellationToken ct)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(request.Id, ct);

        if (order is null)
            return Result<OrderDto>.Failure("Order not found");

        // Concurrency validation
        if (!order.RowVersion.SequenceEqual(request.RowVersion))
        {
            _logger.LogWarning("Concurrency conflict for order {OrderId}", request.Id);
            return Result<OrderDto>.Failure(
                "The order was modified by another user. Please refresh and try again.");
        }

        order.OrderNumber = request.OrderNumber;
        order.TotalAmount = request.TotalAmount;
        order.Status = (OrderStatus)request.Status;

        try
        {
            await _unitOfWork.Orders.UpdateAsync(order, request.RowVersion, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<OrderDto>.Success(new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                TotalAmount = order.TotalAmount,
                Status = (int)order.Status,
                OrderDate = order.OrderDate,
                RowVersion = order.RowVersion
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _unitOfWork.Orders.ExistsAsync(request.Id, ct))
                return Result<OrderDto>.Failure("Order not found");

            return Result<OrderDto>.Failure(
                "The order was modified by another user. Please refresh and try again.");
        }
    }
}
```

#### Step 10: Create API Controller

**Location**: `src/Archu.Api/Controllers/OrdersController.cs`

```csharp
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<OrderDto>>>> GetOrders(
        CancellationToken cancellationToken)
    {
        var query = new GetOrdersQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse<IEnumerable<OrderDto>>.SuccessResponse(result.Value));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrder(
        Guid id, 
        CancellationToken cancellationToken)
    {
        var query = new GetOrderByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(ApiResponse<OrderDto>.ErrorResponse(result.Error));

        return Ok(ApiResponse<OrderDto>.SuccessResponse(result.Value));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderDto>>> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand(
            request.OrderNumber,
            request.TotalAmount,
            request.Status);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<OrderDto>.ErrorResponse(result.Error));

        return CreatedAtAction(
            nameof(GetOrder),
            new { id = result.Value!.Id },
            ApiResponse<OrderDto>.SuccessResponse(result.Value));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> UpdateOrder(
        Guid id,
        [FromBody] UpdateOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (id != request.Id)
            return BadRequest(ApiResponse<OrderDto>.ErrorResponse("ID mismatch"));

        var command = new UpdateOrderCommand(
            request.Id,
            request.OrderNumber,
            request.TotalAmount,
            request.Status,
            request.RowVersion);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("modified by another user"))
                return Conflict(ApiResponse<OrderDto>.ErrorResponse(result.Error));

            return BadRequest(ApiResponse<OrderDto>.ErrorResponse(result.Error));
        }

        return Ok(ApiResponse<OrderDto>.SuccessResponse(result.Value));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteOrder(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteOrderCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(ApiResponse<object>.ErrorResponse(result.Error));

        return NoContent();
    }
}
```

#### Step 11: Test with HTTP Requests

**Location**: `src/Archu.Api/Archu.Api.http` (or create `Orders.http`)

```http
### Create Order
POST https://localhost:7123/api/v1/orders
Content-Type: application/json
Authorization: Bearer {{access_token}}

{
  "orderNumber": "ORD-2025-001",
  "totalAmount": 999.99,
  "status": 0
}

### Get All Orders
GET https://localhost:7123/api/v1/orders
Authorization: Bearer {{access_token}}

### Get Order by ID
GET https://localhost:7123/api/v1/orders/{{order_id}}
Authorization: Bearer {{access_token}}

### Update Order
PUT https://localhost:7123/api/v1/orders/{{order_id}}
Content-Type: application/json
Authorization: Bearer {{access_token}}

{
  "id": "{{order_id}}",
  "orderNumber": "ORD-2025-001-UPDATED",
  "totalAmount": 1299.99,
  "status": 1,
  "rowVersion": "{{row_version}}"
}

### Delete Order
DELETE https://localhost:7123/api/v1/orders/{{order_id}}
Authorization: Bearer {{access_token}}
```

#### Quick Checklist

- [ ] Entity inherits from `BaseEntity`
- [ ] Entity configuration created
- [ ] DbSet added to ApplicationDbContext
- [ ] Migration created and applied
- [ ] Repository interface created
- [ ] Repository implementation created
- [ ] Repository registered in DI
- [ ] DTOs created with RowVersion
- [ ] Commands/Queries created
- [ ] Command handlers implement concurrency validation
- [ ] Controller created with all CRUD endpoints
- [ ] HTTP tests created
- [ ] Integration tests written

### What You Get Automatically

By following this pattern, you automatically get:

✅ **Concurrency Control**
- RowVersion prevents lost updates
- Two-level validation (application + database)
- User-friendly error messages

✅ **Soft Delete**
- Records marked as deleted, not removed
- Audit history preserved
- Global query filter excludes deleted records

✅ **Audit Tracking**
- CreatedAt, CreatedBy on insert
- ModifiedAt, ModifiedBy on update
- DeletedAt, DeletedBy on soft delete

✅ **Clean Architecture**
- Domain entities in Domain layer
- Repositories in Infrastructure layer
- Commands/Queries in Application layer
- Controllers in API layer

✅ **CQRS Pattern**
- Separate commands and queries
- MediatR for request handling
- Result pattern for error handling

---
