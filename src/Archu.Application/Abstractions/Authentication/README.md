# Authentication Abstractions

## Overview
This directory contains interfaces and abstractions for authentication and authorization in the Application layer. These interfaces follow the **Dependency Inversion Principle**, allowing the Application layer to define contracts that are implemented by the Infrastructure and Presentation layers.

## ICurrentUser Interface

### Purpose
`ICurrentUser` provides access to the currently authenticated user's information and authorization context within application services, command handlers, and query handlers.

### Properties

#### `string? UserId`
Gets the unique identifier of the current user. Returns `null` if no user is authenticated.

**Usage:**
```csharp
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly ICurrentUser _currentUser;
    
    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId == null)
            return Result<ProductDto>.Failure("User must be authenticated");
        
        // Use userId for audit tracking or ownership
        var product = new Product { CreatedBy = userId };
        // ...
    }
}
```

#### `bool IsAuthenticated`
Indicates whether the current user is authenticated.

**Usage:**
```csharp
public class GetPrivateDataQueryHandler : IRequestHandler<GetPrivateDataQuery, Result<PrivateData>>
{
    private readonly ICurrentUser _currentUser;
    
    public async Task<Result<PrivateData>> Handle(GetPrivateDataQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            return Result<PrivateData>.Failure("Authentication required");
        
        // Proceed with authenticated operation
    }
}
```

### Methods

#### `bool IsInRole(string role)`
Checks if the current user belongs to a specific role.

**Parameters:**
- `role`: The role name to check (e.g., "Admin", "Manager")

**Returns:** `true` if the user is in the role; otherwise, `false`

**Usage:**
```csharp
public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Result.Failure("Only administrators can delete products");
        
        // Proceed with deletion
    }
}
```

#### `bool HasAnyRole(params string[] roles)`
Checks if the current user belongs to any of the specified roles.

**Parameters:**
- `roles`: Variable number of role names to check

**Returns:** `true` if the user is in at least one of the specified roles; otherwise, `false`

**Usage:**
```csharp
public class ApproveOrderCommandHandler : IRequestHandler<ApproveOrderCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    
    public async Task<Result> Handle(ApproveOrderCommand request, CancellationToken ct)
    {
        if (!_currentUser.HasAnyRole("Admin", "Manager", "Supervisor"))
            return Result.Failure("Insufficient permissions to approve orders");
        
        // Proceed with approval
    }
}
```

#### `IEnumerable<string> GetRoles()`
Gets all roles assigned to the current user.

**Returns:** A collection of role names, or an empty collection if not authenticated

**Usage:**
```csharp
public class GetUserDashboardQueryHandler : IRequestHandler<GetUserDashboardQuery, Result<DashboardDto>>
{
    private readonly ICurrentUser _currentUser;
    
    public async Task<Result<DashboardDto>> Handle(GetUserDashboardQuery request, CancellationToken ct)
    {
        var roles = _currentUser.GetRoles().ToList();
        
        // Customize dashboard based on roles
        var dashboard = new DashboardDto
        {
            CanManageUsers = roles.Contains("Admin"),
            CanViewReports = roles.Any(r => r is "Admin" or "Manager"),
            CanApproveOrders = roles.Any(r => r is "Admin" or "Manager" or "Supervisor")
        };
        
        return Result<DashboardDto>.Success(dashboard);
    }
}
```

## Implementation Details

### HttpContextCurrentUser (API Layer)
The primary implementation in `Archu.Api` extracts user information from ASP.NET Core's `HttpContext.User` claims.

**Key Features:**
- Supports multiple claim types (standard, OIDC, Azure AD)
- Extracts roles from `ClaimTypes.Role` and `"role"` claims
- Returns empty collections/false values for unauthenticated users
- Thread-safe through `IHttpContextAccessor`

### DesignTimeCurrentUser (Infrastructure Layer)
A minimal implementation for EF Core design-time tools (migrations).

**Characteristics:**
- Always authenticated (for migration auditing)
- Fixed UserId: `"design-time"`
- No roles assigned
- Used only during `dotnet ef` commands

## Best Practices

### ✅ Do
- Check `IsAuthenticated` before accessing protected resources
- Use `IsInRole()` for single-role checks
- Use `HasAnyRole()` for multiple-role checks
- Cache role lookups if checking multiple times in a handler
- Return descriptive error messages for authorization failures
- Log authorization failures for security auditing

### ❌ Don't
- Perform authorization in the Domain layer (keep it infrastructure-agnostic)
- Cache `ICurrentUser` instances (they're scoped per request)
- Hardcode role names - use constants or enums
- Skip authentication checks in sensitive operations
- Assume `UserId` is never null - always validate

## Example: Complete Authorization Pattern

```csharp
public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UpdateProductCommandHandler> _logger;
    
    // Role constants for type safety
    private static class Roles
    {
        public const string Admin = "Admin";
        public const string ProductManager = "ProductManager";
        public const string Editor = "Editor";
    }

    public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        // 1. Authentication check
        if (!_currentUser.IsAuthenticated)
        {
            _logger.LogWarning("Unauthenticated user attempted to update product {ProductId}", request.Id);
            return Result<ProductDto>.Failure("Authentication required");
        }

        // 2. Authorization check
        if (!_currentUser.HasAnyRole(Roles.Admin, Roles.ProductManager, Roles.Editor))
        {
            _logger.LogWarning(
                "User {UserId} with roles [{Roles}] attempted to update product {ProductId}",
                _currentUser.UserId,
                string.Join(", ", _currentUser.GetRoles()),
                request.Id);
            
            return Result<ProductDto>.Failure("Insufficient permissions to update products");
        }

        // 3. Retrieve entity
        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, ct);
        if (product == null)
            return Result<ProductDto>.Failure("Product not found");

        // 4. Additional ownership check (if needed)
        if (!_currentUser.IsInRole(Roles.Admin) && product.CreatedBy != _currentUser.UserId)
        {
            return Result<ProductDto>.Failure("You can only edit your own products");
        }

        // 5. Update and save
        product.Name = request.Name;
        product.Price = request.Price;
        
        await _unitOfWork.Products.UpdateAsync(product, request.RowVersion, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "User {UserId} updated product {ProductId}",
            _currentUser.UserId,
            product.Id);

        return Result<ProductDto>.Success(new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            RowVersion = product.RowVersion
        });
    }
}
```

## Role Constants Pattern

Create a shared constants file for role names:

```csharp
namespace Archu.Application.Common;

/// <summary>
/// Defines standard role names used throughout the application.
/// </summary>
public static class ApplicationRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Manager = "Manager";
    public const string Supervisor = "Supervisor";
    public const string ProductManager = "ProductManager";
    public const string Editor = "Editor";
    public const string Viewer = "Viewer";
    
    /// <summary>
    /// Gets all defined role names.
    /// </summary>
    public static IEnumerable<string> All => new[]
    {
        Admin,
        User,
        Manager,
        Supervisor,
        ProductManager,
        Editor,
        Viewer
    };
    
    /// <summary>
    /// Gets roles that have administrative privileges.
    /// </summary>
    public static IEnumerable<string> Administrative => new[]
    {
        Admin,
        Manager
    };
    
    /// <summary>
    /// Gets roles that can manage products.
    /// </summary>
    public static IEnumerable<string> ProductManagement => new[]
    {
        Admin,
        ProductManager,
        Editor
    };
}
```

**Usage:**
```csharp
if (_currentUser.HasAnyRole(ApplicationRoles.ProductManagement.ToArray()))
{
    // Allow product management
}
```

## Testing

### Unit Testing with Mock ICurrentUser
```csharp
[Fact]
public async Task Handle_UnauthenticatedUser_ReturnsFailure()
{
    // Arrange
    var mockCurrentUser = new Mock<ICurrentUser>();
    mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);
    mockCurrentUser.Setup(x => x.UserId).Returns((string?)null);
    
    var handler = new UpdateProductCommandHandler(
        mockUnitOfWork.Object,
        mockCurrentUser.Object,
        mockLogger.Object);
    
    // Act
    var result = await handler.Handle(command, CancellationToken.None);
    
    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal("Authentication required", result.Error);
}

[Fact]
public async Task Handle_UserWithoutRole_ReturnsFailure()
{
    // Arrange
    var mockCurrentUser = new Mock<ICurrentUser>();
    mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
    mockCurrentUser.Setup(x => x.UserId).Returns("user-123");
    mockCurrentUser.Setup(x => x.IsInRole(It.IsAny<string>())).Returns(false);
    mockCurrentUser.Setup(x => x.HasAnyRole(It.IsAny<string[]>())).Returns(false);
    mockCurrentUser.Setup(x => x.GetRoles()).Returns(new[] { "Viewer" });
    
    var handler = new UpdateProductCommandHandler(
        mockUnitOfWork.Object,
        mockCurrentUser.Object,
        mockLogger.Object);
    
    // Act
    var result = await handler.Handle(command, CancellationToken.None);
    
    // Assert
    Assert.False(result.IsSuccess);
    Assert.Contains("Insufficient permissions", result.Error);
}
```

## Security Considerations

### 🔒 Authentication vs Authorization
- **Authentication**: Verifying identity (who you are) - `IsAuthenticated`, `UserId`
- **Authorization**: Verifying permissions (what you can do) - `IsInRole`, `HasAnyRole`

### 🔒 Defense in Depth
Always implement authorization at multiple layers:
1. **API Layer**: Attribute-based authorization (`[Authorize(Roles = "Admin")]`)
2. **Application Layer**: Handler-based authorization (using `ICurrentUser`)
3. **Domain Layer**: Business rule validation (independent of auth)

### 🔒 Principle of Least Privilege
- Grant users the minimum roles needed
- Use specific roles over broad ones
- Regularly audit role assignments

## Related Documentation
- [Domain Identity Entities](../../../Archu.Domain/Entities/Identity/README.md)
- [RBAC Implementation Guide](../../../Archu.Domain/Abstractions/Identity/IMPLEMENTATION_SUMMARY_RBAC.md)
- [Clean Architecture Guidelines](../../../../docs/ARCHITECTURE.md)

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Maintainer**: Archu Development Team
