# Migration Guide: Using BaseCommandHandler

## Overview
This guide explains how to migrate existing command handlers to use the new `BaseCommandHandler` class for consistent user authentication handling.

---

## Benefits of Using BaseCommandHandler

✅ **Eliminates code duplication** - No need to repeat user ID extraction logic  
✅ **Consistent error handling** - Standardized authentication error messages  
✅ **Easier maintenance** - Update authentication logic in one place  
✅ **Better testability** - Common base class simplifies testing  
✅ **Type safety** - Returns strongly-typed Guid instead of string  

---

## Step-by-Step Migration

### Step 1: Update Class Declaration

**Before:**
```csharp
public class YourCommandHandler : IRequestHandler<YourCommand, YourResult>
{
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<YourCommandHandler> _logger;
    // ... other dependencies
}
```

**After:**
```csharp
public class YourCommandHandler : BaseCommandHandler, IRequestHandler<YourCommand, YourResult>
{
    // Remove _currentUser and _logger fields - they're in the base class now
    // ... other dependencies
}
```

---

### Step 2: Update Constructor

**Before:**
```csharp
public YourCommandHandler(
    IYourDependency dependency,
    ICurrentUser currentUser,
    ILogger<YourCommandHandler> logger)
{
    _dependency = dependency;
    _currentUser = currentUser;
    _logger = logger;
}
```

**After:**
```csharp
public YourCommandHandler(
    IYourDependency dependency,
    ICurrentUser currentUser,
    ILogger<YourCommandHandler> logger)
    : base(currentUser, logger)  // ✅ Pass to base class
{
    _dependency = dependency;
    // Don't assign _currentUser or _logger - base class handles them
}
```

---

### Step 3: Replace User ID Extraction

**Before:**
```csharp
public async Task<YourResult> Handle(YourCommand request, CancellationToken cancellationToken)
{
    var userId = _currentUser.UserId;
    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
    {
        _logger.LogError("User ID not found or invalid");
        throw new InvalidOperationException("User must be authenticated");
    }

    _logger.LogInformation("User {UserId} performing operation", userIdGuid);
    // ... rest of implementation
}
```

**After:**
```csharp
public async Task<YourResult> Handle(YourCommand request, CancellationToken cancellationToken)
{
    var userIdGuid = GetCurrentUserId("your operation name");  // ✅ One line!

    Logger.LogInformation("User {UserId} performing operation", userIdGuid);
    // ... rest of implementation
}
```

---

### Step 4: Update Logger References

**Before:**
```csharp
_logger.LogInformation("Operation started");
_logger.LogError(ex, "Operation failed");
```

**After:**
```csharp
Logger.LogInformation("Operation started");  // ✅ Use base class property
Logger.LogError(ex, "Operation failed");
```

---

## Real-World Example: CreateProductCommandHandler

### Before Migration
```csharp
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<CreateProductCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var ownerIdGuid))
        {
            _logger.LogError("Cannot create product: User ID not found or invalid");
            throw new InvalidOperationException("User must be authenticated to create products");
        }

        _logger.LogInformation("User {UserId} creating product: {ProductName}", userId, request.Name);

        var product = new Product
        {
            Name = request.Name,
            Price = request.Price,
            OwnerId = ownerIdGuid
        };

        var createdProduct = await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product created with ID: {ProductId} by User: {UserId}", createdProduct.Id, userId);

        return new ProductDto
        {
            Id = createdProduct.Id,
            Name = createdProduct.Name,
            Price = createdProduct.Price,
            RowVersion = createdProduct.RowVersion
        };
    }
}
```

### After Migration
```csharp
public class CreateProductCommandHandler : BaseCommandHandler, IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<CreateProductCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var ownerIdGuid = GetCurrentUserId("create products");  // ✅ Simplified!

        Logger.LogInformation("User {UserId} creating product: {ProductName}", ownerIdGuid, request.Name);

        var product = new Product
        {
            Name = request.Name,
            Price = request.Price,
            OwnerId = ownerIdGuid
        };

        var createdProduct = await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Product created with ID: {ProductId} by User: {UserId}", createdProduct.Id, ownerIdGuid);

        return new ProductDto
        {
            Id = createdProduct.Id,
            Name = createdProduct.Name,
            Price = createdProduct.Price,
            RowVersion = createdProduct.RowVersion
        };
    }
}
```

**Lines of Code Reduced:** 12 lines → Cleaner and more maintainable! 🎉

---

## Advanced Usage

### Using TryGetCurrentUserId (Non-Throwing Variant)

Use this when you want to handle unauthenticated users gracefully:

```csharp
public async Task<Result<ProductDto>> Handle(YourCommand request, CancellationToken cancellationToken)
{
    if (!TryGetCurrentUserId(out var userId))
    {
        Logger.LogWarning("Attempted operation without authentication");
        return Result<ProductDto>.Failure("Authentication required");
    }

    // Proceed with authenticated operation
    Logger.LogInformation("User {UserId} performing operation", userId);
    // ... rest of implementation
}
```

### Accessing CurrentUser Directly

You can still access the `ICurrentUser` service if needed:

```csharp
public async Task<Result> Handle(YourCommand request, CancellationToken cancellationToken)
{
    var userId = GetCurrentUserId("your operation");
    
    // Check roles
    if (CurrentUser.IsInRole("Admin"))
    {
        // Admin-specific logic
    }
    
    // Check multiple roles
    if (CurrentUser.HasAnyRole("Manager", "Admin"))
    {
        // Manager or Admin logic
    }
}
```

---

## Handlers That Should Be Migrated

### High Priority (Use authentication)
- ✅ `CreateProductCommandHandler` - Already migrated
- ✅ `UpdateProductCommandHandler` - Already migrated
- ✅ `DeleteProductCommandHandler` - Already migrated
- ⏳ `CreateOrderCommandHandler` - Should be migrated (if exists)
- ⏳ Other command handlers that require authentication

### Not Applicable (Public operations)
- ❌ `LoginCommandHandler` - User not authenticated yet
- ❌ `RegisterCommandHandler` - User not authenticated yet
- ❌ `ForgotPasswordCommandHandler` - Public operation
- ❌ `ResetPasswordCommandHandler` - Uses token, not current user

---

## Testing Considerations

### Unit Tests for Handlers

**Before Migration:**
```csharp
[Fact]
public async Task Handle_Should_Throw_When_User_Not_Authenticated()
{
    // Arrange
    var mockCurrentUser = Mock.Of<ICurrentUser>(u => u.UserId == null);
    var handler = new YourCommandHandler(..., mockCurrentUser, ...);

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(
        () => handler.Handle(request, CancellationToken.None));
}
```

**After Migration:**
```csharp
[Fact]
public async Task Handle_Should_Throw_When_User_Not_Authenticated()
{
    // Arrange - Same setup
    var mockCurrentUser = Mock.Of<ICurrentUser>(u => u.UserId == null);
    var handler = new YourCommandHandler(..., mockCurrentUser, ...);

    // Act & Assert - Same assertion
    await Assert.ThrowsAsync<InvalidOperationException>(
        () => handler.Handle(request, CancellationToken.None));
}
```

The tests remain the same! The base class handles the logic consistently.

---

## Common Pitfalls

### ❌ DON'T: Forget to call base constructor
```csharp
public YourCommandHandler(ICurrentUser currentUser, ILogger<YourCommandHandler> logger)
{
    // Missing: : base(currentUser, logger)
    _dependency = dependency;
}
```

### ❌ DON'T: Keep duplicate fields
```csharp
public class YourCommandHandler : BaseCommandHandler, ...
{
    private readonly ICurrentUser _currentUser;  // ❌ Don't do this!
    private readonly ILogger _logger;            // ❌ Don't do this!
}
```

### ✅ DO: Use base class properties
```csharp
public class YourCommandHandler : BaseCommandHandler, ...
{
    // ✅ No duplicate fields
    private readonly IUnitOfWork _unitOfWork;
    
    public YourCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<YourCommandHandler> logger)
        : base(currentUser, logger)  // ✅ Pass to base
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<Result> Handle(...)
    {
        var userId = GetCurrentUserId("operation");  // ✅ Use base method
        Logger.LogInformation("...");                 // ✅ Use base property
    }
}
```

---

## Summary Checklist

When migrating a command handler:

- [ ] Inherit from `BaseCommandHandler`
- [ ] Remove `_currentUser` and `_logger` fields
- [ ] Call `base(currentUser, logger)` in constructor
- [ ] Replace user ID extraction with `GetCurrentUserId()`
- [ ] Replace `_logger` references with `Logger`
- [ ] Update unit tests if needed (usually not required)
- [ ] Test the handler to ensure it works correctly

---

## Questions?

If you have questions about migrating a specific command handler, refer to:
- `src/Archu.Application/Common/BaseCommandHandler.cs` - Base class implementation
- `src/Archu.Application/Products/Commands/CreateProduct/CreateProductCommandHandler.cs` - Example migration
- `docs/CODE_QUALITY_IMPROVEMENTS_IMPLEMENTATION.md` - Detailed documentation

---

**Happy Coding! 🚀**
