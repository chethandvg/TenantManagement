# Archu.Application

The Application layer contains the business use cases and application logic for the Archu system. This layer orchestrates the flow of data and implements CQRS patterns using MediatR.

## 📋 Overview

**Target Framework**: .NET 9  
**Layer**: Application (Clean Architecture)  
**Dependencies**: `Archu.Domain`, `Archu.Contracts`

## 🎯 Purpose

The Application layer is responsible for:
- **Use Cases**: Implementing business workflows and application logic
- **CQRS**: Commands (mutations) and Queries (reads) using MediatR
- **Validation**: Input validation using FluentValidation
- **Abstractions**: Defining interfaces for infrastructure services
- **DTOs & Mapping**: Converting between domain entities and contracts

## 🏗️ Architecture Principle

> **Dependency Inversion**: This layer defines **what** the application needs (interfaces) without caring **how** it's implemented. The Infrastructure layer provides the concrete implementations.

```
Archu.Application (defines IProductRepository)
  ↓ depends on
Archu.Domain (entities: Product, User, etc.)

  ↑ implements
Archu.Infrastructure (provides ProductRepository)
```

## 📦 Project Structure

```
Archu.Application/
├── Abstractions/          # Interfaces for infrastructure
│   ├── Authentication/
│   │   ├── IAuthenticationService.cs
│   │   ├── IPasswordHasher.cs
│   │   ├── IPasswordValidator.cs
│   │   ├── IJwtTokenService.cs
│   │   ├── ITokenService.cs
│   │   └── PasswordValidationResult.cs
│   ├── Repositories/
│   │   ├── IUserRepository.cs
│   │   ├── IRoleRepository.cs
│   │   ├── IUserRoleRepository.cs
│   │   └── IUserTokenRepositories.cs
│   ├── ICurrentUser.cs
│   ├── IProductRepository.cs
│   ├── ITimeProvider.cs
│   └── IUnitOfWork.cs
├── Admin/          # Admin operations
│   ├── Commands/
│   │   ├── AssignRole/
│   │   ├── CreateRole/
│   │ ├── CreateUser/
│   │   ├── DeleteUser/
│   │   ├── InitializeSystem/
│   │   └── RemoveRole/
│   └── Queries/
│       ├── GetRoles/
│       └── GetUsers/
├── Auth/          # Authentication & authorization
│   ├── Commands/
│   │   ├── ChangePassword/
│   │   ├── ConfirmEmail/
│   │   ├── ForgotPassword/
│   │   ├── Login/
│   │   ├── Logout/
│   │   ├── RefreshToken/
│   │   ├── Register/
│   │   └── ResetPassword/
│   ├── Queries/
│   │   └── ValidateToken/
│   └── Validators/
│       ├── ChangePasswordRequestValidator.cs
│       ├── RegisterUserRequestValidator.cs
│       └── ResetPasswordRequestPasswordValidator.cs
├── Products/      # Product management
│   ├── Commands/
│   │   ├── CreateProduct/
│   │   │   ├── CreateProductCommand.cs
│   │   │   └── CreateProductCommandHandler.cs
│   │   ├── UpdateProduct/
│   │   │   ├── UpdateProductCommand.cs
│   │   │   └── UpdateProductCommandHandler.cs
│   │   └── DeleteProduct/
│   │       ├── DeleteProductCommand.cs
│   │       └── DeleteProductCommandHandler.cs
│   ├── Queries/
│   │   ├── GetProducts/
│   │   │├── GetProductsQuery.cs
│   │   │   └── GetProductsQueryHandler.cs
│   │ └── GetProductById/
│   │       ├── GetProductByIdQuery.cs
│   │       └── GetProductByIdQueryHandler.cs
│   └── Validators/
│  ├── CreateProductCommandValidator.cs
│       └── UpdateProductCommandValidator.cs
├── Common/       # Shared application components
│   ├── Behaviors/
│   │ ├── ValidationBehavior.cs
│   │   └── PerformanceBehavior.cs
│   ├── ApplicationRoles.cs
│   ├── BaseCommandHandler.cs
│   └── Result.cs
└── AssemblyReference.cs   # Assembly marker for scanning
```

## 🔧 Key Components

### 1. CQRS Pattern

**Commands** - Modify state:
```csharp
public record CreateProductCommand(
    string Name,
    decimal Price) : IRequest<Result<ProductDto>>;

public class CreateProductCommandHandler 
    : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(
        CreateProductCommand request, 
CancellationToken cancellationToken)
    {
        var product = new Product 
     { 
         Name = request.Name, 
            Price = request.Price 
     };
        
        await _repository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
  return Result.Success(new ProductDto 
        { 
     Id = product.Id, 
       Name = product.Name,
            Price = product.Price 
        });
    }
}
```

**Queries** - Read state:
```csharp
public record GetProductsQuery : IRequest<Result<List<ProductDto>>>;

public class GetProductsQueryHandler 
    : IRequestHandler<GetProductsQuery, Result<List<ProductDto>>>
{
    public async Task<Result<List<ProductDto>>> Handle(
        GetProductsQuery request, 
  CancellationToken cancellationToken)
  {
        var products = await _repository.GetAllAsync(cancellationToken);
        
      var dtos = products.Select(p => new ProductDto 
        { 
     Id = p.Id, 
   Name = p.Name,
            Price = p.Price,
        RowVersion = p.RowVersion
   }).ToList();
  
        return Result.Success(dtos);
    }
}
```

### 2. Validation with FluentValidation

**Validator Classes**:
```csharp
public class CreateProductCommandValidator 
    : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
    .NotEmpty().WithMessage("Product name is required")
   .MaximumLength(200).WithMessage("Name too long");
  
     RuleFor(x => x.Price)
          .GreaterThan(0).WithMessage("Price must be positive");
    }
}
```

**Validation Behavior** - Automatic validation pipeline:
```csharp
public class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var validationResults = await Task.WhenAll(
 _validators.Select(v => v.ValidateAsync(request)));
     
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
          .ToList();
   
        if (failures.Any())
        {
         throw new ValidationException(failures);
 }
        
        return await next();
    }
}
```

### 3. Result Pattern

Explicit success/failure handling without exceptions:

```csharp
public class Result<T>
{
 public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    
  public static Result<T> Success(T value) => new() 
    { 
        IsSuccess = true, 
        Value = value 
    };
    
    public static Result<T> Fail(string error) => new() 
    { 
        IsSuccess = false, 
    Error = error 
    };
}
```

**Usage**:
```csharp
var result = await _mediator.Send(new GetProductByIdQuery(id));

if (!result.IsSuccess)
    return NotFound(result.Error);
    
return Ok(result.Value);
```

### 4. Abstractions

**Repository Interfaces** (`Abstractions/Repositories/`):
```csharp
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Product>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, byte[] originalRowVersion, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
```

**Authentication Interfaces** (`Abstractions/Authentication/`):
```csharp
public interface IPasswordValidator
{
    PasswordValidationResult Validate(string password);
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string hashedPassword, string providedPassword);
}

public interface IJwtTokenService
{
    string GenerateAccessToken(IEnumerable<Claim> claims);
    ClaimsPrincipal? ValidateToken(string token);
}
```

**Infrastructure Services**:
```csharp
public interface ICurrentUser
{
    Guid UserId { get; }
    string Email { get; }
    bool IsAuthenticated { get; }
    IEnumerable<string> Roles { get; }
}

public interface ITimeProvider
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

### 5. MediatR Behaviors

**Performance Monitoring**:
```csharp
public class PerformanceBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<TRequest> _logger;
    private readonly Stopwatch _timer;
    
    public async Task<TResponse> Handle(...)
    {
        _timer.Restart();
      
        var response = await next();

        _timer.Stop();
        
        if (_timer.ElapsedMilliseconds > 500)
        {
            _logger.LogWarning(
      "Long Running Request: {Name} ({ElapsedMilliseconds} ms)",
    typeof(TRequest).Name,
             _timer.ElapsedMilliseconds);
   }
   
        return response;
    }
}
```

### 6. Base Command Handler

Provides common functionality for command handlers:

```csharp
public abstract class BaseCommandHandler
{
    protected readonly IUnitOfWork _unitOfWork;
 protected readonly ICurrentUser _currentUser;
    protected readonly ITimeProvider _timeProvider;
  
    protected BaseCommandHandler(
        IUnitOfWork unitOfWork,
      ICurrentUser currentUser,
        ITimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
      _currentUser = currentUser;
        _timeProvider = timeProvider;
    }
}
```

## 📂 Feature Organization

Each feature follows a consistent folder structure:

```
Products/
├── Commands/
│   ├── CreateProduct/
│   │   ├── CreateProductCommand.cs       # Command definition
│   │ └── CreateProductCommandHandler.cs # Handler implementation
│   ├── UpdateProduct/
│   └── DeleteProduct/
├── Queries/
│   ├── GetProducts/
│   │   ├── GetProductsQuery.cs
│   │   └── GetProductsQueryHandler.cs
│   └── GetProductById/
└── Validators/
    ├── CreateProductCommandValidator.cs
    └── UpdateProductCommandValidator.cs
```

## 🔐 Authentication & Authorization

### Application Roles

Defined in `Common/ApplicationRoles.cs`:
```csharp
public static class ApplicationRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string User = "User";
}
```

### Authentication Commands

- **Login** - Authenticate user and return tokens
- **Register** - Create new user account
- **RefreshToken** - Renew access token
- **ChangePassword** - Update user password (authenticated)
- **ForgotPassword** - Request password reset token
- **ResetPassword** - Reset password with token
- **ConfirmEmail** - Verify email address
- **Logout** - Revoke refresh token

### Admin Operations

- **InitializeSystem** - Bootstrap admin user and roles
- **CreateUser** - Admin creates user account
- **DeleteUser** - Soft delete user
- **AssignRole** - Add role to user
- **RemoveRole** - Remove role from user
- **GetUsers** - List all users
- **GetRoles** - List all roles

## 🧩 MediatR Integration

### Service Registration

```csharp
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
 typeof(AssemblyReference).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
});
```

### Request Flow

```
Controller receives request
  ↓
MediatR dispatcher
  ↓
ValidationBehavior (validates request)
  ↓
PerformanceBehavior (monitors execution time)
  ↓
Command/Query Handler (executes business logic)
  ↓
Returns Result<T>
  ↓
Controller maps to ApiResponse<T>
```

## 📦 NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| MediatR | 13.0.0 | CQRS pattern implementation |
| FluentValidation | 12.0.0 | Validation rules |
| FluentValidation.DependencyInjectionExtensions | 12.0.0 | DI integration |
| Microsoft.Extensions.Logging.Abstractions | 9.0.10 | Logging |

## 🔧 Usage Examples

### Creating a New Feature

**1. Define the Command**:
```csharp
// Products/Commands/CreateProduct/CreateProductCommand.cs
public record CreateProductCommand(
    string Name,
    decimal Price) : IRequest<Result<ProductDto>>;
```

**2. Create the Handler**:
```csharp
// Products/Commands/CreateProduct/CreateProductCommandHandler.cs
public class CreateProductCommandHandler 
    : BaseCommandHandler, 
      IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IProductRepository _repository;
    
    public CreateProductCommandHandler(
        IProductRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ITimeProvider timeProvider)
  : base(unitOfWork, currentUser, timeProvider)
    {
        _repository = repository;
    }
    
    public async Task<Result<ProductDto>> Handle(
   CreateProductCommand request,
     CancellationToken cancellationToken)
    {
        var product = new Product
        {
 Name = request.Name,
     Price = request.Price
        };
        
        await _repository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        var dto = new ProductDto
  {
            Id = product.Id,
     Name = product.Name,
            Price = product.Price,
      RowVersion = product.RowVersion
        };
 
     return Result.Success(dto);
    }
}
```

**3. Add Validation**:
```csharp
// Products/Validators/CreateProductCommandValidator.cs
public class CreateProductCommandValidator 
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

**4. Use in Controller**:
```csharp
[HttpPost]
public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct(
    CreateProductRequest request)
{
    var command = new CreateProductCommand(request.Name, request.Price);
    var result = await _mediator.Send(command);
    
    if (!result.IsSuccess)
        return BadRequest(ApiResponse<object>.Fail(result.Error!));
        
    return CreatedAtAction(
    nameof(GetProduct),
        new { id = result.Value!.Id },
        ApiResponse<ProductDto>.Ok(result.Value, "Product created"));
}
```

## 🧪 Testing Strategies

### Unit Testing Handlers

```csharp
public class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesProduct()
    {
        // Arrange
 var mockRepo = new Mock<IProductRepository>();
        var mockUow = new Mock<IUnitOfWork>();
     var mockUser = new Mock<ICurrentUser>();
    var mockTime = new Mock<ITimeProvider>();
    
      var handler = new CreateProductCommandHandler(
         mockRepo.Object,
            mockUow.Object,
       mockUser.Object,
          mockTime.Object);
        
        var command = new CreateProductCommand("Test", 10.00m);
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.True(result.IsSuccess);
        mockRepo.Verify(x => x.AddAsync(
       It.IsAny<Product>(), 
It.IsAny<CancellationToken>()), Times.Once);
        mockUow.Verify(x => x.SaveChangesAsync(
       It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Validation Testing

```csharp
public class CreateProductCommandValidatorTests
{
    [Fact]
    public void Validate_EmptyName_ReturnsError()
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand("", 10.00m);
        
        // Act
   var result = validator.Validate(command);
 
        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }
}
```

## 📚 Design Patterns

### CQRS (Command Query Responsibility Segregation)
- **Commands**: Mutate state, return `Result<T>`
- **Queries**: Read state, never modify
- Clear separation of concerns

### Repository Pattern
- Abstractions defined in Application layer
- Implementations in Infrastructure layer
- Testable without database

### Unit of Work
- Groups repository operations
- Single transaction boundary
- Explicit SaveChanges

### Result Pattern
- No exceptions for business failures
- Explicit success/failure handling
- Type-safe error messages

### Mediator Pattern
- Decouples request from handler
- Pipeline behaviors (validation, logging)
- Single responsibility per handler

## 🚨 Error Handling

### Validation Errors
```csharp
FluentValidation.ValidationException
```
Caught by `ValidationBehavior` and converted to validation failure response.

### Business Logic Errors
```csharp
return Result.Fail("Product not found");
```
Returned as structured error response.

### Infrastructure Errors
```csharp
DbUpdateConcurrencyException
```
Handled in Infrastructure layer, returned as concurrency conflict.

## 📋 Best Practices

✅ **DO**:
- Keep handlers focused on single responsibility
- Use `Result<T>` for business logic outcomes
- Write validators for all commands
- Leverage `BaseCommandHandler` for common dependencies
- Use `CancellationToken` for async operations
- Return DTOs, never domain entities

❌ **DON'T**:
- Put infrastructure code in handlers
- Return domain entities from handlers
- Skip validation for any command
- Use exceptions for business logic failures
- Directly access DbContext (use repositories)

## 🔗 Related Documentation

- [Architecture Guide](../../docs/ARCHITECTURE.md) - Clean Architecture overview
- [Infrastructure Layer](../Archu.Infrastructure/README.md) - Repository implementations
- [Domain Layer](../Archu.Domain/README.md) - Business entities
- [API Documentation](../Archu.Api/README.md) - REST endpoints

## 🤝 Contributing

When adding new use cases:

1. **Create folder structure**: `Feature/Commands|Queries/CommandName/`
2. **Define command/query**: Record with `IRequest<Result<T>>`
3. **Implement handler**: Class implementing `IRequestHandler<,>`
4. **Add validator**: FluentValidation rules
5. **Write tests**: Unit tests for handler and validator
6. **Update documentation**: Add to this README

## 🔄 Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-01-24 | Initial release with CQRS pattern |

---

**Maintained by**: Archu Development Team  
**Last Updated**: 2025-01-24
