# Archu.Application

## Overview
The Application layer contains the business use cases and orchestrates the flow of data between the Domain and Infrastructure layers. This layer defines interfaces that are implemented by the outer layers.

## Target Framework
- .NET 9.0

## Responsibilities
- **Abstractions**: Application-level interfaces for cross-cutting concerns
- **Use Cases**: Business workflows and application services via CQRS with MediatR
- **Commands & Queries**: CQRS pattern implementation for business operations
- **Validators**: FluentValidation validators for request validation
- **Behaviors**: MediatR pipeline behaviors (validation, performance monitoring)
- **Common**: Shared application logic, result patterns, and constants

## Key Components

### Abstractions

#### Authentication & Authorization
- **ICurrentUser**: Enhanced interface for accessing authenticated user information and authorization
  - `string? UserId`: Gets the current user's identifier
  - `bool IsAuthenticated`: Checks if the user is authenticated
  - `bool IsInRole(string role)`: Checks if user belongs to a specific role
  - `bool HasAnyRole(params string[] roles)`: Checks if user belongs to any of the specified roles
  - `IEnumerable<string> GetRoles()`: Gets all roles assigned to the current user
  
  📖 [Authentication Documentation](./Abstractions/Authentication/README.md)  
  📖 [Authentication Examples](./Abstractions/Authentication/EXAMPLES.md)

#### Time & System
- **ITimeProvider**: Interface for abstracting system time operations (testability, time zone handling)

#### Data Access
- **IUnitOfWork**: Manages transactions and repositories
- **IProductRepository**: Product-specific data access operations

### Common

#### Result Pattern
- **Result<T>**: Standardized result type for operation outcomes (success/failure)
- Provides consistent error handling across the application

#### Role Constants
- **ApplicationRoles**: Centralized role name constants
  - Standard roles: Admin, User, Manager, Supervisor, ProductManager, Editor, Viewer
  - Role groups: Administrative, ProductManagement, Approvers, ReadOnly
  - Helper methods: `IsValid()`, `IsAdministrative()`
  
  📖 [ApplicationRoles.cs](./Common/ApplicationRoles.cs)

### CQRS Implementation

#### Products
- **Commands**: CreateProduct, UpdateProduct, DeleteProduct
- **Queries**: GetProducts, GetProductById
- **Validators**: CreateProductCommandValidator, UpdateProductCommandValidator

#### Pipeline Behaviors
- **ValidationBehavior**: Automatic FluentValidation execution
- **PerformanceBehavior**: Performance monitoring and logging

## Architecture Patterns

### CQRS (Command Query Responsibility Segregation)
Separates read operations (queries) from write operations (commands) for clarity and scalability.

**Commands**: Mutate state
```csharp
public record CreateProductCommand(string Name, decimal Price) : IRequest<Result<ProductDto>>;
```

**Queries**: Read state
```csharp
public record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductDto>>;
```

### Repository Pattern
Abstractions define data access contracts; implementations live in Infrastructure layer.

```csharp
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(Product product, byte[] originalRowVersion, CancellationToken ct = default);
}
```

### Unit of Work Pattern
Encapsulates transaction management and coordinates repository operations.

```csharp
public interface IUnitOfWork
{
    IProductRepository Products { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

## Authentication & Authorization

### Usage in Command Handlers
```csharp
public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        // Check authentication
        if (!_currentUser.IsAuthenticated)
            return Result.Failure("Authentication required");
        
        // Check authorization
        if (!_currentUser.IsInRole(ApplicationRoles.Admin))
            return Result.Failure("Only administrators can delete products");
        
        // Proceed with deletion
    }
}
```

### Role-Based Authorization
```csharp
public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    private readonly ICurrentUser _currentUser;
    
    public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        // Allow multiple roles
        if (!_currentUser.HasAnyRole(ApplicationRoles.ProductManagement.ToArray()))
            return Result<ProductDto>.Failure("Insufficient permissions");
        
        // Proceed with update
    }
}
```

📖 **Complete Examples**: [Authentication Examples](./Abstractions/Authentication/EXAMPLES.md)

## Dependencies
- `Archu.Domain` - Domain entities and abstractions
- `MediatR` - CQRS pattern implementation
- `FluentValidation` - Request validation

## Design Principles
- **Dependency Inversion**: Defines interfaces that infrastructure implements
- **Use Case Driven**: Each command/query represents a single business operation
- **Framework Agnostic**: No direct dependencies on web frameworks or databases
- **SOLID Principles**: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion
- **Clean Architecture**: Application layer orchestrates use cases without knowing implementation details

## Usage
This project is referenced by:
- `Archu.Infrastructure` - to implement abstractions (repositories, time providers)
- `Archu.Api` - to invoke commands/queries and access abstractions

## Testing

### Unit Testing Application Services
```csharp
[Fact]
public async Task Handle_UserNotAuthenticated_ReturnsFailure()
{
    // Arrange
    var mockCurrentUser = new Mock<ICurrentUser>();
    mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);
    
    var handler = new DeleteProductCommandHandler(
        Mock.Of<IUnitOfWork>(),
        mockCurrentUser.Object,
        Mock.Of<ILogger<DeleteProductCommandHandler>>());
    
    // Act
    var result = await handler.Handle(command, CancellationToken.None);
    
    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal("Authentication required", result.Error);
}
```

## Best Practices

### ✅ Do
- Use CQRS pattern for clear separation of concerns
- Validate all commands with FluentValidation
- Use `ICurrentUser` for authentication/authorization checks
- Use `ApplicationRoles` constants instead of magic strings
- Return `Result<T>` for consistent error handling
- Log authorization failures for security auditing
- Use dependency injection for all dependencies

### ❌ Don't
- Put infrastructure concerns in application layer
- Hardcode role names - use `ApplicationRoles` constants
- Skip authentication/authorization checks on sensitive operations
- Return null - use `Result<T>` pattern instead
- Perform data access directly - use repositories via `IUnitOfWork`

## Documentation

### Core Documentation
- 📖 [Authentication Implementation Summary](./AUTHENTICATION_IMPLEMENTATION_SUMMARY.md)
- 📖 [ICurrentUser API Reference](./Abstractions/Authentication/README.md)
- 📖 [Authentication Examples](./Abstractions/Authentication/EXAMPLES.md)

### Related Documentation
- [Domain Layer](../Archu.Domain/README.md)
- [Domain Identity Entities](../Archu.Domain/Entities/Identity/README.md)
- [RBAC Implementation Guide](../Archu.Domain/Abstractions/Identity/IMPLEMENTATION_SUMMARY_RBAC.md)
- [Clean Architecture Guidelines](../../docs/ARCHITECTURE.md)

## Version History

### v1.1.0 (2025-01-22)
- ✨ Enhanced `ICurrentUser` interface with authentication and role-based authorization
- ✨ Added `ApplicationRoles` constants for type-safe role names
- ✨ Comprehensive authentication documentation and examples
- ✨ Updated all implementations (`HttpContextCurrentUser`, `DesignTimeCurrentUser`)

### v1.0.0
- ✅ CQRS pattern with MediatR
- ✅ FluentValidation for request validation
- ✅ Repository and Unit of Work patterns
- ✅ Pipeline behaviors for validation and performance monitoring

**Maintainer**: Archu Development Team  
**Last Updated**: 2025-01-22
