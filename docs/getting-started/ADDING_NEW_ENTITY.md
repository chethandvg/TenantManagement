# Adding a New Entity - Step-by-Step Guide

This guide shows you how to add a new entity to TentMan with full CRUD operations, concurrency control, soft delete, and audit tracking.

## Example: Adding an Order Entity

### Prerequisites

- Understanding of Clean Architecture layers
- Familiarity with Entity Framework Core
- Basic knowledge of CQRS pattern with MediatR

---

## Step 1: Create the Domain Entity

**Location**: `src/TentMan.Domain/Entities/Order.cs`

```csharp
using TentMan.Domain.Common;

namespace TentMan.Domain.Entities;

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

---

## Step 2: Create Repository Interface

**Location**: `src/TentMan.Application/Abstractions/IOrderRepository.cs`

```csharp
using TentMan.Domain.Entities;

namespace TentMan.Application.Abstractions;

public interface IOrderRepository
{
    Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an order with concurrency control.
    /// </summary>
    /// <param name="order">The order to update</param>
    /// <param name="originalRowVersion">The client's RowVersion for concurrency detection</param>
    Task UpdateAsync(Order order, byte[] originalRowVersion, CancellationToken cancellationToken = default);
    
    Task DeleteAsync(Order order, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
```

⚠️ **Important**: Notice the `originalRowVersion` parameter in `UpdateAsync` - this enables concurrency control.

---

## Step 3: Implement Repository

**Location**: `src/TentMan.Infrastructure/Repositories/OrderRepository.cs`

```csharp
using TentMan.Application.Abstractions;
using TentMan.Domain.Entities;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

public class OrderRepository : BaseRepository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Order>> GetAllAsync(CancellationToken ct = default)
    {
        return await DbSet.AsNoTracking().ToListAsync(ct);
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await DbSet.FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public Task<Order> AddAsync(Order order, CancellationToken ct = default)
    {
        DbSet.Add(order);
        return Task.FromResult(order);
    }

    public Task UpdateAsync(Order order, byte[] originalRowVersion, CancellationToken ct = default)
    {
        SetOriginalRowVersion(order, originalRowVersion); // From BaseRepository
        DbSet.Update(order);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Order order, CancellationToken ct = default)
    {
        SoftDelete(order); // From BaseRepository
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return await DbSet.AsNoTracking().AnyAsync(o => o.Id == id, ct);
    }
}
```

✅ **Key Points**:
- Inherit from `BaseRepository<Order>` to get common functionality
- Use `SetOriginalRowVersion()` in `UpdateAsync` for concurrency
- Use `SoftDelete()` in `DeleteAsync` for soft delete

---

## Step 4: Add to Unit of Work

**Update**: `src/TentMan.Application/Abstractions/IUnitOfWork.cs`

```csharp
public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    IOrderRepository Orders { get; } // Add this line
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

**Update**: `src/TentMan.Infrastructure/Repositories/UnitOfWork.cs`

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _currentTransaction;
    private IProductRepository? _productRepository;
    private IOrderRepository? _orderRepository; // Add this

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IProductRepository Products => _productRepository ??= new ProductRepository(_context);
    public IOrderRepository Orders => _orderRepository ??= new OrderRepository(_context); // Add this
    
    // ... rest of implementation
}
```

---

## Step 5: Register Repository in DI

**Update**: `src/TentMan.Api/Program.cs`

```csharp
// Register repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>(); // Add this line
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

---

## Step 6: Create DTOs

**Location**: `src/TentMan.Contracts/Orders/OrderDto.cs`

```csharp
namespace TentMan.Contracts.Orders;

public sealed class OrderDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime OrderDate { get; init; }
    
    // ⚠️ CRITICAL: Include RowVersion for concurrency control
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
```

**Location**: `src/TentMan.Contracts/Orders/CreateOrderRequest.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace TentMan.Contracts.Orders;

public sealed class CreateOrderRequest
{
    [Required, MaxLength(50)]
    public string OrderNumber { get; init; } = string.Empty;
    
    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; init; }
    
    [Required]
    public string Status { get; init; } = string.Empty;
    
    public DateTime OrderDate { get; init; }
}
```

**Location**: `src/TentMan.Contracts/Orders/UpdateOrderRequest.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace TentMan.Contracts.Orders;

public sealed class UpdateOrderRequest
{
    [Required]
    public Guid Id { get; init; }
    
    [Required, MaxLength(50)]
    public string OrderNumber { get; init; } = string.Empty;
    
    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; init; }
    
    [Required]
    public string Status { get; init; } = string.Empty;
    
    // ⚠️ CRITICAL: Require RowVersion from client
    [Required, MinLength(1)]
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
```

---

## Step 7: Create Commands & Queries

### Create Command

**Location**: `src/TentMan.Application/Orders/Commands/CreateOrder/CreateOrderCommand.cs`

```csharp
using TentMan.Application.Common;
using TentMan.Contracts.Orders;
using MediatR;

namespace TentMan.Application.Orders.Commands.CreateOrder;

public record CreateOrderCommand(
    string OrderNumber,
    decimal TotalAmount,
    string Status,
    DateTime OrderDate
) : IRequest<Result<OrderDto>>;
```

**Location**: `src/TentMan.Application/Orders/Commands/CreateOrder/CreateOrderCommandHandler.cs`

```csharp
using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Contracts.Orders;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(IUnitOfWork unitOfWork, ILogger<CreateOrderCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Creating order: {OrderNumber}", request.OrderNumber);

        var order = new Order
        {
            OrderNumber = request.OrderNumber,
            TotalAmount = request.TotalAmount,
            Status = Enum.Parse<OrderStatus>(request.Status),
            OrderDate = request.OrderDate
        };

        var createdOrder = await _unitOfWork.Orders.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Order created with ID: {OrderId}", createdOrder.Id);

        return Result<OrderDto>.Success(new OrderDto
        {
            Id = createdOrder.Id,
            OrderNumber = createdOrder.OrderNumber,
            TotalAmount = createdOrder.TotalAmount,
            Status = createdOrder.Status.ToString(),
            OrderDate = createdOrder.OrderDate,
            RowVersion = createdOrder.RowVersion // SQL Server generated this
        });
    }
}
```

### Update Command

**Location**: `src/TentMan.Application/Orders/Commands/UpdateOrder/UpdateOrderCommand.cs`

```csharp
using TentMan.Application.Common;
using TentMan.Contracts.Orders;
using MediatR;

namespace TentMan.Application.Orders.Commands.UpdateOrder;

public record UpdateOrderCommand(
    Guid Id,
    string OrderNumber,
    decimal TotalAmount,
    string Status,
    byte[] RowVersion // ⚠️ Include RowVersion
) : IRequest<Result<OrderDto>>;
```

**Location**: `src/TentMan.Application/Orders/Commands/UpdateOrder/UpdateOrderCommandHandler.cs`

```csharp
using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Contracts.Orders;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.Orders.Commands.UpdateOrder;

public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand, Result<OrderDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateOrderCommandHandler> _logger;

    public UpdateOrderCommandHandler(IUnitOfWork unitOfWork, ILogger<UpdateOrderCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> Handle(UpdateOrderCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Updating order with ID: {OrderId}", request.Id);

        var order = await _unitOfWork.Orders.GetByIdAsync(request.Id, ct);

        if (order is null)
        {
            _logger.LogWarning("Order with ID {OrderId} not found", request.Id);
            return Result<OrderDto>.Failure("Order not found");
        }

        // Update properties
        order.OrderNumber = request.OrderNumber;
        order.TotalAmount = request.TotalAmount;
        order.Status = Enum.Parse<OrderStatus>(request.Status);

        try
        {
            // ⚠️ CRITICAL: Pass the client's RowVersion for concurrency detection
            await _unitOfWork.Orders.UpdateAsync(order, request.RowVersion, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Order with ID {OrderId} updated successfully", request.Id);

            // ✅ Return the NEW RowVersion to the client
            return Result<OrderDto>.Success(new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                OrderDate = order.OrderDate,
                RowVersion = order.RowVersion // SQL Server generated new version
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _unitOfWork.Orders.ExistsAsync(request.Id, ct))
            {
                _logger.LogWarning("Order with ID {OrderId} not found during concurrency check", request.Id);
                return Result<OrderDto>.Failure("Order not found");
            }

            _logger.LogWarning("Concurrency conflict updating order with ID {OrderId}", request.Id);
            return Result<OrderDto>.Failure("The order was modified by another user. Please refresh and try again.");
        }
    }
}
```

### Delete Command

**Location**: `src/TentMan.Application/Orders/Commands/DeleteOrder/DeleteOrderCommand.cs`

```csharp
using TentMan.Application.Common;
using MediatR;

namespace TentMan.Application.Orders.Commands.DeleteOrder;

public record DeleteOrderCommand(Guid Id) : IRequest<Result>;
```

**Location**: `src/TentMan.Application/Orders/Commands/DeleteOrder/DeleteOrderCommandHandler.cs`

```csharp
using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.Orders.Commands.DeleteOrder;

public class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteOrderCommandHandler> _logger;

    public DeleteOrderCommandHandler(IUnitOfWork unitOfWork, ILogger<DeleteOrderCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteOrderCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Deleting order with ID: {OrderId}", request.Id);

        var order = await _unitOfWork.Orders.GetByIdAsync(request.Id, ct);

        if (order is null)
        {
            _logger.LogWarning("Order with ID {OrderId} not found", request.Id);
            return Result.Failure("Order not found");
        }

        await _unitOfWork.Orders.DeleteAsync(order, ct); // Soft delete
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Order with ID {OrderId} deleted successfully", request.Id);
        return Result.Success();
    }
}
```

---

## Step 8: Create Entity Configuration

**Location**: `src/TentMan.Infrastructure/Persistence/Configurations/OrderConfiguration.cs`

```csharp
using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.HasIndex(o => o.OrderNumber);
        
        // RowVersion is already configured by BaseEntity [Timestamp] attribute
        // Auditing and soft delete properties are handled by ApplicationDbContext
    }
}
```

---

## Step 9: Add DbSet to ApplicationDbContext

**Update**: `src/TentMan.Infrastructure/Persistence/ApplicationDbContext.cs`

```csharp
public class ApplicationDbContext : DbContext
{
    // DbSets
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>(); // Add this line
    
    // ... rest of implementation
}
```

---

## Step 10: Create Migration

```bash
cd src/TentMan.Infrastructure
dotnet ef migrations add AddOrderEntity
dotnet ef database update
```

---

## Step 11: Create API Controller

**Location**: `src/TentMan.Api/Controllers/OrdersController.cs`

```csharp
using TentMan.Application.Orders.Commands.CreateOrder;
using TentMan.Application.Orders.Commands.DeleteOrder;
using TentMan.Application.Orders.Commands.UpdateOrder;
using TentMan.Contracts.Common;
using TentMan.Contracts.Orders;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<OrderDto>>> CreateOrder(
        CreateOrderRequest request,
        CancellationToken ct)
    {
        var command = new CreateOrderCommand(
            request.OrderNumber,
            request.TotalAmount,
            request.Status,
            request.OrderDate);
            
        var result = await _mediator.Send(command, ct);

        return CreatedAtAction(
            nameof(GetOrderById),
            new { id = result.Value!.Id },
            ApiResponse<OrderDto>.Ok(result.Value, "Order created successfully"));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrderById(Guid id, CancellationToken ct)
    {
        // Implement GetOrderByIdQuery similar to Products
        // Left as exercise
        throw new NotImplementedException();
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<OrderDto>>> UpdateOrder(
        Guid id,
        UpdateOrderRequest request,
        CancellationToken ct)
    {
        if (id != request.Id)
            return BadRequest(ApiResponse<OrderDto>.Fail("Route ID mismatch"));

        var command = new UpdateOrderCommand(
            request.Id,
            request.OrderNumber,
            request.TotalAmount,
            request.Status,
            request.RowVersion); // ⚠️ Pass RowVersion
            
        var result = await _mediator.Send(command, ct);

        if (!result.IsSuccess)
            return NotFound(ApiResponse<object>.Fail(result.Error!));

        return Ok(ApiResponse<OrderDto>.Ok(result.Value!, "Order updated successfully"));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteOrder(Guid id, CancellationToken ct)
    {
        var command = new DeleteOrderCommand(id);
        var result = await _mediator.Send(command, ct);

        if (!result.IsSuccess)
            return NotFound(ApiResponse<object>.Fail(result.Error!));

        return Ok(ApiResponse<object>.Ok(null, "Order deleted successfully"));
    }
}
```

---

## What You Get Automatically

### ✅ Optimistic Concurrency Control
- Client sends RowVersion with every update
- Server detects conflicts and returns 409 Conflict
- No lost updates

### ✅ Soft Delete
- Records marked as deleted, not removed
- Automatically excluded from all queries
- Audit history preserved

### ✅ Audit Tracking
- `CreatedAtUtc`, `CreatedBy` on INSERT
- `ModifiedAtUtc`, `ModifiedBy` on UPDATE
- `DeletedAtUtc`, `DeletedBy` on DELETE

### ✅ Automatic RowVersion Updates
- SQL Server generates RowVersion on every INSERT/UPDATE
- No manual management required

---

## Testing Your Implementation

```bash
# 1. Create order
POST /api/v1/orders
{
  "orderNumber": "ORD-001",
  "totalAmount": 100.00,
  "status": "Pending",
  "orderDate": "2025-01-22T10:00:00Z"
}

# 2. Update order (with RowVersion from step 1)
PUT /api/v1/orders/{id}
{
  "id": "{id}",
  "orderNumber": "ORD-001-UPDATED",
  "totalAmount": 150.00,
  "status": "Confirmed",
  "rowVersion": "AAAAAAAA="
}

# 3. Try updating with stale RowVersion (should fail with 409)

# 4. Delete order
DELETE /api/v1/orders/{id}

# 5. Verify soft delete (should return 404)
GET /api/v1/orders/{id}
```

---

## Checklist

Before you commit, verify:

- [ ] Entity inherits from `BaseEntity`
- [ ] Repository inherits from `BaseRepository<T>`
- [ ] Repository uses `SetOriginalRowVersion()` in `UpdateAsync`
- [ ] Repository uses `SoftDelete()` in `DeleteAsync`
- [ ] All DTOs include `RowVersion`
- [ ] Update request requires `RowVersion` with `[Required, MinLength(1)]`
- [ ] Command handlers pass `request.RowVersion` to repository
- [ ] Command handlers return `entity.RowVersion` after save
- [ ] Command handlers catch `DbUpdateConcurrencyException`
- [ ] Repository registered in DI
- [ ] Added to UnitOfWork
- [ ] DbSet added to ApplicationDbContext
- [ ] Migration created and applied
- [ ] Controller endpoints implemented
- [ ] Tested all CRUD operations
- [ ] Tested concurrency scenarios

---

## Common Mistakes to Avoid

❌ **Forgetting RowVersion in DTO**
❌ **Not passing RowVersion to repository**
❌ **Not setting OriginalValue in repository**
❌ **Returning stale RowVersion to client**
❌ **Not handling DbUpdateConcurrencyException**
❌ **Physical delete instead of soft delete**

---

For complete details, see: [Concurrency Guide](../docs/CONCURRENCY_GUIDE.md)

---

**Last Updated**: 2025-01-22  
**Version**: 1.0 (Consolidated)
