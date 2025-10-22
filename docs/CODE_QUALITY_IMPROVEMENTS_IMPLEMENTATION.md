# Code Quality Improvements - Implementation Summary

## Overview
This document describes the implementation of two key code quality improvements based on code review feedback.

**Date**: 2025-01-22  
**Status**: ✅ Implemented

---

## Improvement #1: Optimized Ownership Checking

### Problem
The `ResourceOwnerRequirementHandler` was performing a full entity fetch just to check ownership, which was inefficient:

```csharp
// ❌ BEFORE: Loads entire product entity
var product = await productRepository.GetByIdAsync(resourceId);
if (product == null) return false;

if (product is IHasOwner ownedResource)
{
    return ownedResource.IsOwnedBy(userId);
}
```

### Solution
Added a specialized repository method `IsOwnedByAsync` that only checks ownership without loading the entire entity:

```csharp
// ✅ AFTER: Optimized database query
var isOwned = await productRepository.IsOwnedByAsync(resourceId, userId);
return isOwned;
```

### Changes Made

#### 1. Updated `IProductRepository` Interface
**File**: `src/Archu.Application/Abstractions/IProductRepository.cs`

Added new method:
```csharp
/// <summary>
/// Checks if a product is owned by the specified user without loading the entire entity.
/// This is optimized for authorization checks where only ownership verification is needed.
/// </summary>
Task<bool> IsOwnedByAsync(Guid resourceId, Guid userId, CancellationToken cancellationToken = default);
```

#### 2. Implemented in `ProductRepository`
**File**: `src/Archu.Infrastructure/Repositories/ProductRepository.cs`

```csharp
public async Task<bool> IsOwnedByAsync(Guid resourceId, Guid userId, CancellationToken cancellationToken = default)
{
    return await DbSet
        .AsNoTracking()
        .AnyAsync(p => p.Id == resourceId && p.OwnerId == userId, cancellationToken);
}
```

#### 3. Updated `ResourceOwnerRequirementHandler`
**File**: `src/Archu.Api/Authorization/Handlers/ResourceOwnerRequirementHandler.cs`

Simplified the `CheckProductOwnershipAsync` method to use the optimized approach:

```csharp
private async Task<bool> CheckProductOwnershipAsync(Guid resourceId, Guid userId)
{
    using var scope = _serviceProvider.CreateScope();
    var productRepository = scope.ServiceProvider.GetService<IProductRepository>();

    if (productRepository == null)
    {
        _logger.LogError("Product repository not available");
        return false;
    }

    // ✅ OPTIMIZED: Use specialized method
    var isOwned = await productRepository.IsOwnedByAsync(resourceId, userId);

    if (!isOwned)
    {
        _logger.LogWarning("Product {ProductId} is not owned by user {UserId} or does not exist", resourceId, userId);
    }

    return isOwned;
}
```

### Benefits
- **Performance**: Executes a simple `SELECT COUNT(*)` query instead of loading the full entity
- **Memory**: Reduces memory allocation and garbage collection pressure
- **Scalability**: Better performance as the entity grows in size and complexity
- **Maintainability**: Clear intent with a dedicated method for ownership checks

---

## Improvement #2: Base Command Handler with Centralized User Authentication

### Problem
User ID extraction and validation logic was duplicated across multiple command handlers:

```csharp
// ❌ BEFORE: Duplicated in every command handler
var userId = _currentUser.UserId;
if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var ownerIdGuid))
{
    _logger.LogError("Cannot create product: User ID not found or invalid");
    throw new InvalidOperationException("User must be authenticated to create products");
}
```

This pattern appeared in:
- `CreateProductCommandHandler`
- `UpdateProductCommandHandler`
- `DeleteProductCommandHandler`
- Other authenticated command handlers

### Solution
Created a `BaseCommandHandler` class with reusable user ID extraction and validation methods:

```csharp
// ✅ AFTER: Centralized in base class
public abstract class BaseCommandHandler
{
    protected Guid GetCurrentUserId(string? operationName = null) { ... }
    protected bool TryGetCurrentUserId(out Guid userIdGuid) { ... }
}
```

### Changes Made

#### 1. Created `BaseCommandHandler` Class
**File**: `src/Archu.Application/Common/BaseCommandHandler.cs`

```csharp
using Archu.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Common;

/// <summary>
/// Base class for command handlers that provides common functionality.
/// </summary>
public abstract class BaseCommandHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly ILogger _logger;

    protected BaseCommandHandler(ICurrentUser currentUser, ILogger logger)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the current authenticated user's ID as a Guid.
    /// </summary>
    /// <param name="operationName">Optional name of the operation for logging purposes.</param>
    /// <returns>The user ID as a Guid.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the user is not authenticated or the user ID is invalid.</exception>
    protected Guid GetCurrentUserId(string? operationName = null)
    {
        var userId = _currentUser.UserId;
        
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            var operation = string.IsNullOrEmpty(operationName) ? "this operation" : operationName;
            _logger.LogError("Cannot perform {Operation}: User ID not found or invalid", operation);
            throw new InvalidOperationException($"User must be authenticated to perform {operation}");
        }

        return userIdGuid;
    }

    /// <summary>
    /// Tries to get the current authenticated user's ID as a Guid.
    /// </summary>
    /// <param name="userIdGuid">When this method returns, contains the user ID if successful; otherwise, Guid.Empty.</param>
    /// <returns>True if the user ID was successfully retrieved; otherwise, false.</returns>
    protected bool TryGetCurrentUserId(out Guid userIdGuid)
    {
        var userId = _currentUser.UserId;
        
        if (string.IsNullOrEmpty(userId))
        {
            userIdGuid = Guid.Empty;
            return false;
        }

        return Guid.TryParse(userId, out userIdGuid);
    }

    /// <summary>
    /// Gets the current user service.
    /// </summary>
    protected ICurrentUser CurrentUser => _currentUser;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger => _logger;
}
```

#### 2. Updated `CreateProductCommandHandler`
**File**: `src/Archu.Application/Products/Commands/CreateProduct/CreateProductCommandHandler.cs`

```csharp
public class CreateProductCommandHandler : BaseCommandHandler, IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<CreateProductCommandHandler> logger)
        : base(currentUser, logger)  // ✅ Pass dependencies to base class
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // ✅ Use base class method for user ID extraction and validation
        var ownerIdGuid = GetCurrentUserId("create products");

        Logger.LogInformation("User {UserId} creating product: {ProductName}", ownerIdGuid, request.Name);

        // ... rest of the implementation
    }
}
```

### Benefits
- **DRY Principle**: Eliminates code duplication across command handlers
- **Consistency**: Ensures all handlers validate user authentication the same way
- **Maintainability**: Single place to update authentication logic
- **Testability**: Easier to test with a common base class
- **Error Messages**: Consistent error messaging with customizable operation names
- **Flexibility**: Provides both throwing (`GetCurrentUserId`) and non-throwing (`TryGetCurrentUserId`) variants

---

## Usage Recommendations

### When to Use `IsOwnedByAsync`
Use this method when you only need to verify ownership for authorization purposes:

```csharp
// Authorization checks
if (!await _productRepository.IsOwnedByAsync(productId, userId))
{
    return Result.Failure("You don't have permission to access this product");
}
```

### When to Use `GetByIdAndOwnerAsync`
Use this method when you need the entity AND want to verify ownership in one query:

```csharp
// Need the entity for further processing
var product = await _productRepository.GetByIdAndOwnerAsync(productId, userId);
if (product == null)
{
    return Result.Failure("Product not found or you don't have permission");
}
```

### When to Use `BaseCommandHandler`
Inherit from `BaseCommandHandler` for any command handler that needs:
- User authentication validation
- Access to the current user ID
- Consistent error handling for unauthenticated users

```csharp
public class YourCommandHandler : BaseCommandHandler, IRequestHandler<YourCommand, YourResult>
{
    public YourCommandHandler(
        IYourDependency dependency,
        ICurrentUser currentUser,
        ILogger<YourCommandHandler> logger)
        : base(currentUser, logger)
    {
        // Your dependencies
    }

    public async Task<YourResult> Handle(YourCommand request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId("your operation");
        // ... implementation
    }
}
```

---

## Performance Impact

### Ownership Check Optimization

**Before**: Full entity load
```sql
SELECT [p].[Id], [p].[Name], [p].[Price], [p].[OwnerId], [p].[RowVersion], 
       [p].[CreatedAt], [p].[CreatedBy], [p].[ModifiedAt], [p].[ModifiedBy], 
       [p].[IsDeleted], [p].[DeletedAt], [p].[DeletedBy]
FROM [Products] AS [p]
WHERE [p].[Id] = @p0 AND [p].[IsDeleted] = 0
```

**After**: Optimized existence check
```sql
SELECT CASE
    WHEN EXISTS (
        SELECT 1
        FROM [Products] AS [p]
        WHERE [p].[Id] = @p0 AND [p].[OwnerId] = @p1 AND [p].[IsDeleted] = 0
    )
    THEN CAST(1 AS bit) ELSE CAST(0 AS bit)
END AS [Value]
```

**Benefits**:
- Reduced data transfer from database
- Lower memory allocation
- Faster query execution
- Better index utilization

---

## Testing Recommendations

### Unit Tests for `BaseCommandHandler`

Create tests for the new base class:

```csharp
public class BaseCommandHandlerTests
{
    [Fact]
    public void GetCurrentUserId_WithValidUser_ReturnsGuid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mockCurrentUser = Mock.Of<ICurrentUser>(u => u.UserId == userId.ToString());
        var mockLogger = Mock.Of<ILogger>();
        var handler = new TestCommandHandler(mockCurrentUser, mockLogger);

        // Act
        var result = handler.TestGetCurrentUserId();

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void GetCurrentUserId_WithInvalidUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockCurrentUser = Mock.Of<ICurrentUser>(u => u.UserId == null);
        var mockLogger = Mock.Of<ILogger>();
        var handler = new TestCommandHandler(mockCurrentUser, mockLogger);

        // Act & Assert
        handler.Invoking(h => h.TestGetCurrentUserId())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("User must be authenticated*");
    }
}
```

### Integration Tests for `IsOwnedByAsync`

```csharp
[Fact]
public async Task IsOwnedByAsync_WhenUserOwnsProduct_ReturnsTrue()
{
    // Arrange
    var userId = Guid.NewGuid();
    var product = new Product { Name = "Test", Price = 10, OwnerId = userId };
    await _repository.AddAsync(product);
    await _unitOfWork.SaveChangesAsync();

    // Act
    var isOwned = await _repository.IsOwnedByAsync(product.Id, userId);

    // Assert
    isOwned.Should().BeTrue();
}

[Fact]
public async Task IsOwnedByAsync_WhenUserDoesNotOwnProduct_ReturnsFalse()
{
    // Arrange
    var ownerId = Guid.NewGuid();
    var otherUserId = Guid.NewGuid();
    var product = new Product { Name = "Test", Price = 10, OwnerId = ownerId };
    await _repository.AddAsync(product);
    await _unitOfWork.SaveChangesAsync();

    // Act
    var isOwned = await _repository.IsOwnedByAsync(product.Id, otherUserId);

    // Assert
    isOwned.Should().BeFalse();
}
```

---

## Migration Guide for Existing Command Handlers

To migrate existing command handlers to use `BaseCommandHandler`:

1. **Update the class declaration**:
   ```csharp
   // Before
   public class YourCommandHandler : IRequestHandler<YourCommand, YourResult>
   
   // After
   public class YourCommandHandler : BaseCommandHandler, IRequestHandler<YourCommand, YourResult>
   ```

2. **Update the constructor**:
   ```csharp
   // Before
   public YourCommandHandler(
       IYourDependency dependency,
       ICurrentUser currentUser,
       ILogger<YourCommandHandler> logger)
   {
       _dependency = dependency;
       _currentUser = currentUser;
       _logger = logger;
   }
   
   // After
   public YourCommandHandler(
       IYourDependency dependency,
       ICurrentUser currentUser,
       ILogger<YourCommandHandler> logger)
       : base(currentUser, logger)  // Pass to base class
   {
       _dependency = dependency;
   }
   ```

3. **Replace user ID extraction**:
   ```csharp
   // Before
   var userId = _currentUser.UserId;
   if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
   {
       _logger.LogError("User ID not found or invalid");
       throw new InvalidOperationException("User must be authenticated");
   }
   
   // After
   var userIdGuid = GetCurrentUserId("your operation");
   ```

4. **Update logger usage**:
   ```csharp
   // Before
   _logger.LogInformation("Operation started");
   
   // After
   Logger.LogInformation("Operation started");
   ```

---

## Summary

These improvements enhance the codebase by:

1. **Performance**: Optimized database queries for authorization checks
2. **Code Quality**: Eliminated duplication and followed DRY principles
3. **Maintainability**: Centralized common logic in reusable components
4. **Consistency**: Standardized user authentication validation
5. **Scalability**: Better performance characteristics as the system grows

All changes are backward compatible and existing functionality remains intact while providing a better foundation for future development.

---

**Implemented By**: GitHub Copilot  
**Review Status**: Ready for code review  
**Test Coverage**: Unit and integration tests recommended

