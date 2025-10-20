# Application Layer Authentication Implementation Examples

This directory contains example implementations demonstrating how to use the enhanced `ICurrentUser` interface in your command and query handlers.

## Example 1: Basic Authentication Check

```csharp
using Archu.Application.Abstractions;
using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Examples;

public class CreateSecureResourceCommand : IRequest<Result<SecureResourceDto>>
{
    public string Name { get; init; } = string.Empty;
    public string Data { get; init; } = string.Empty;
}

public class CreateSecureResourceCommandHandler 
    : IRequestHandler<CreateSecureResourceCommand, Result<SecureResourceDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CreateSecureResourceCommandHandler> _logger;

    public CreateSecureResourceCommandHandler(
        ICurrentUser currentUser,
        ILogger<CreateSecureResourceCommandHandler> logger)
    {
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<SecureResourceDto>> Handle(
        CreateSecureResourceCommand request, 
        CancellationToken ct)
    {
        // Basic authentication check
        if (!_currentUser.IsAuthenticated)
        {
            _logger.LogWarning("Unauthenticated user attempted to create secure resource");
            return Result<SecureResourceDto>.Failure("Authentication required");
        }

        _logger.LogInformation(
            "User {UserId} creating secure resource: {Name}",
            _currentUser.UserId,
            request.Name);

        // Proceed with resource creation...
        var resource = new SecureResource
        {
            Name = request.Name,
            Data = request.Data,
            CreatedBy = _currentUser.UserId
        };

        // Save and return...
        return Result<SecureResourceDto>.Success(dto);
    }
}
```

## Example 2: Single Role Authorization

```csharp
using Archu.Application.Abstractions;
using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Examples;

public class DeleteProductCommand : IRequest<Result>
{
    public Guid ProductId { get; init; }
}

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    public DeleteProductCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<DeleteProductCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        // Require Admin role for deletion
        if (!_currentUser.IsInRole(ApplicationRoles.Admin))
        {
            _logger.LogWarning(
                "User {UserId} attempted to delete product without Admin role",
                _currentUser.UserId);
            
            return Result.Failure("Only administrators can delete products");
        }

        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, ct);
        if (product == null)
            return Result.Failure("Product not found");

        await _unitOfWork.Products.DeleteAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Admin {UserId} deleted product {ProductId}",
            _currentUser.UserId,
            request.ProductId);

        return Result.Success();
    }
}
```

## Example 3: Multiple Role Authorization

```csharp
using Archu.Application.Abstractions;
using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Examples;

public class UpdateProductCommand : IRequest<Result<ProductDto>>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}

public class UpdateProductCommandHandler 
    : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UpdateProductCommandHandler> _logger;

    public UpdateProductCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<UpdateProductCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> Handle(
        UpdateProductCommand request, 
        CancellationToken ct)
    {
        // Allow Admin, ProductManager, or Editor roles
        if (!_currentUser.HasAnyRole(ApplicationRoles.ProductManagement.ToArray()))
        {
            _logger.LogWarning(
                "User {UserId} with roles [{Roles}] attempted to update product {ProductId}",
                _currentUser.UserId,
                string.Join(", ", _currentUser.GetRoles()),
                request.Id);
            
            return Result<ProductDto>.Failure(
                "Insufficient permissions. Requires Admin, ProductManager, or Editor role.");
        }

        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, ct);
        if (product == null)
            return Result<ProductDto>.Failure("Product not found");

        // Update product
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

## Example 4: Ownership-Based Authorization

```csharp
using Archu.Application.Abstractions;
using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Examples;

public class UpdateUserProfileCommand : IRequest<Result<UserProfileDto>>
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Bio { get; init; } = string.Empty;
}

public class UpdateUserProfileCommandHandler 
    : IRequestHandler<UpdateUserProfileCommand, Result<UserProfileDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UpdateUserProfileCommandHandler> _logger;

    public async Task<Result<UserProfileDto>> Handle(
        UpdateUserProfileCommand request, 
        CancellationToken ct)
    {
        // Users can only update their own profile unless they're an Admin
        var isOwner = _currentUser.UserId == request.UserId.ToString();
        var isAdmin = _currentUser.IsInRole(ApplicationRoles.Admin);

        if (!isOwner && !isAdmin)
        {
            _logger.LogWarning(
                "User {UserId} attempted to update profile of user {TargetUserId}",
                _currentUser.UserId,
                request.UserId);
            
            return Result<UserProfileDto>.Failure(
                "You can only update your own profile");
        }

        // Proceed with update...
        _logger.LogInformation(
            "User {UserId} updating profile {TargetUserId}",
            _currentUser.UserId,
            request.UserId);

        return Result<UserProfileDto>.Success(dto);
    }
}
```

## Example 5: Role-Based Query Filtering

```csharp
using Archu.Application.Abstractions;
using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Examples;

public class GetProductsQuery : IRequest<Result<IEnumerable<ProductDto>>>
{
    public bool IncludeDeleted { get; init; }
}

public class GetProductsQueryHandler 
    : IRequestHandler<GetProductsQuery, Result<IEnumerable<ProductDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetProductsQueryHandler> _logger;

    public async Task<Result<IEnumerable<ProductDto>>> Handle(
        GetProductsQuery request, 
        CancellationToken ct)
    {
        // Only admins can view deleted products
        if (request.IncludeDeleted && !_currentUser.IsInRole(ApplicationRoles.Admin))
        {
            _logger.LogWarning(
                "Non-admin user {UserId} attempted to view deleted products",
                _currentUser.UserId);
            
            return Result<IEnumerable<ProductDto>>.Failure(
                "Only administrators can view deleted products");
        }

        var products = await _unitOfWork.Products.GetAllAsync(ct);

        var productDtos = products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            RowVersion = p.RowVersion
        });

        return Result<IEnumerable<ProductDto>>.Success(productDtos);
    }
}
```

## Example 6: Dynamic Authorization Based on Roles

```csharp
using Archu.Application.Abstractions;
using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Examples;

public class GetDashboardQuery : IRequest<Result<DashboardDto>>
{
}

public class GetDashboardQueryHandler 
    : IRequestHandler<GetDashboardQuery, Result<DashboardDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetDashboardQueryHandler> _logger;

    public async Task<Result<DashboardDto>> Handle(
        GetDashboardQuery request, 
        CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            return Result<DashboardDto>.Failure("Authentication required");

        // Get all user roles
        var roles = _currentUser.GetRoles().ToList();

        // Customize dashboard based on roles
        var dashboard = new DashboardDto
        {
            UserId = _currentUser.UserId!,
            UserName = "Current User", // Fetch from repository
            Roles = roles,
            
            // Feature flags based on roles
            CanManageUsers = roles.Contains(ApplicationRoles.Admin),
            CanManageProducts = _currentUser.HasAnyRole(ApplicationRoles.ProductManagement.ToArray()),
            CanViewReports = _currentUser.HasAnyRole(
                ApplicationRoles.Admin, 
                ApplicationRoles.Manager),
            CanApproveOrders = _currentUser.HasAnyRole(ApplicationRoles.Approvers.ToArray()),
            IsReadOnly = roles.Contains(ApplicationRoles.Viewer) && roles.Count == 1
        };

        _logger.LogInformation(
            "Generated dashboard for user {UserId} with roles [{Roles}]",
            _currentUser.UserId,
            string.Join(", ", roles));

        return Result<DashboardDto>.Success(dashboard);
    }
}

public class DashboardDto
{
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
    public bool CanManageUsers { get; init; }
    public bool CanManageProducts { get; init; }
    public bool CanViewReports { get; init; }
    public bool CanApproveOrders { get; init; }
    public bool IsReadOnly { get; init; }
}
```

## Example 7: Authorization with Audit Logging

```csharp
using Archu.Application.Abstractions;
using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Examples;

public class PerformSensitiveOperationCommand : IRequest<Result>
{
    public string OperationDetails { get; init; } = string.Empty;
}

public class PerformSensitiveOperationCommandHandler 
    : IRequestHandler<PerformSensitiveOperationCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<PerformSensitiveOperationCommandHandler> _logger;

    public async Task<Result> Handle(
        PerformSensitiveOperationCommand request, 
        CancellationToken ct)
    {
        // Require authentication
        if (!_currentUser.IsAuthenticated)
        {
            _logger.LogWarning("Unauthenticated access attempt to sensitive operation");
            return Result.Failure("Authentication required");
        }

        // Require Admin or Manager role
        if (!_currentUser.HasAnyRole(ApplicationRoles.Admin, ApplicationRoles.Manager))
        {
            _logger.LogWarning(
                "SECURITY ALERT: User {UserId} with roles [{Roles}] attempted sensitive operation",
                _currentUser.UserId,
                string.Join(", ", _currentUser.GetRoles()));
            
            return Result.Failure("Insufficient permissions for this sensitive operation");
        }

        // Log the operation for audit trail
        _logger.LogInformation(
            "AUDIT: User {UserId} with roles [{Roles}] performed sensitive operation: {Details}",
            _currentUser.UserId,
            string.Join(", ", _currentUser.GetRoles()),
            request.OperationDetails);

        // Perform the sensitive operation...
        await Task.CompletedTask;

        return Result.Success();
    }
}
```

## Testing Examples

### Unit Test with Moq

```csharp
using Archu.Application.Abstractions;
using Moq;
using Xunit;

namespace Archu.Application.Tests.Examples;

public class UpdateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_UserNotAuthenticated_ReturnsFailure()
    {
        // Arrange
        var mockCurrentUser = new Mock<ICurrentUser>();
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);
        mockCurrentUser.Setup(x => x.UserId).Returns((string?)null);
        
        var handler = new UpdateProductCommandHandler(
            Mock.Of<IUnitOfWork>(),
            mockCurrentUser.Object,
            Mock.Of<ILogger<UpdateProductCommandHandler>>());
        
        var command = new UpdateProductCommand
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Price = 99.99m,
            RowVersion = new byte[] { 1, 2, 3 }
        };
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Authentication required", result.Error);
    }
    
    [Fact]
    public async Task Handle_UserWithoutRequiredRole_ReturnsFailure()
    {
        // Arrange
        var mockCurrentUser = new Mock<ICurrentUser>();
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        mockCurrentUser.Setup(x => x.UserId).Returns("user-123");
        mockCurrentUser.Setup(x => x.HasAnyRole(It.IsAny<string[]>())).Returns(false);
        mockCurrentUser.Setup(x => x.GetRoles()).Returns(new[] { ApplicationRoles.Viewer });
        
        var handler = new UpdateProductCommandHandler(
            Mock.Of<IUnitOfWork>(),
            mockCurrentUser.Object,
            Mock.Of<ILogger<UpdateProductCommandHandler>>());
        
        var command = new UpdateProductCommand
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Price = 99.99m,
            RowVersion = new byte[] { 1, 2, 3 }
        };
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Insufficient permissions", result.Error);
    }
}
```

## Best Practices Summary

1. **Always check `IsAuthenticated` first** before accessing user-specific data
2. **Use `ApplicationRoles` constants** instead of hardcoding role names
3. **Log authorization failures** for security auditing
4. **Return descriptive error messages** to help users understand permission issues
5. **Combine role checks with ownership checks** when appropriate
6. **Cache role lookups** if checking multiple times in the same handler
7. **Use `HasAnyRole()` with role groups** from `ApplicationRoles` class

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Maintainer**: Archu Development Team
