# Archu.Infrastructure

The Infrastructure layer implements external concerns and data persistence for the Archu application. This layer contains all framework-specific implementations and database access logic.

## 📋 Overview

**Target Framework**: .NET 9  
**Layer**: Infrastructure (Clean Architecture)  
**Dependencies**: `Archu.Domain`, `Archu.Application`

## 🎯 Purpose

The Infrastructure layer is responsible for:
- **Database Access**: Entity Framework Core DbContext and migrations
- **Repository Implementations**: Concrete implementations of repository interfaces
- **Authentication**: JWT token generation, password hashing, and validation
- **External Services**: Time providers, HTTP context access, and other infrastructure concerns

## 🏗️ Architecture Principle

> **Dependency Inversion**: This layer **implements** abstractions defined in `Archu.Application`. The Application layer depends on interfaces, and Infrastructure provides the concrete implementations.

```
Archu.Application (defines IProductRepository)
  ↑
        │ (implements)
        │
Archu.Infrastructure (provides ProductRepository)
```

## 📦 Project Structure

```
Archu.Infrastructure/
├── Authentication/          # JWT, password hashing, validation
│   ├── AuthenticationService.cs
│   ├── JwtTokenService.cs
│   ├── JwtOptions.cs
│   ├── PasswordHasher.cs
│   ├── PasswordValidator.cs
│   ├── RefreshTokenHandler.cs
│   ├── ClaimsPrincipalExtensions.cs
│   ├── HttpContextCurrentUser.cs
│   └── DesignTimeCurrentUser.cs
├── Persistence/       # Database configuration
│   ├── ApplicationDbContext.cs
│   ├── DesignTimeDbContextFactory.cs
│   ├── Configurations/      # Entity type configurations
│   │   ├── ApplicationUserConfiguration.cs
│   │   ├── ApplicationRoleConfiguration.cs
│   │ ├── UserRoleConfiguration.cs
│   │   └── ProductConfiguration.cs
│   └── Migrations/          # EF Core migrations
│  ├── 20251024121023_InitialCreate.cs
│     └── ApplicationDbContextModelSnapshot.cs
├── Repositories/   # Repository implementations
│   ├── BaseRepository.cs
│   ├── ProductRepository.cs
│   ├── UserRepository.cs
│   ├── RoleRepository.cs
│   ├── UserRoleRepository.cs
│   ├── PasswordResetTokenRepository.cs
│   ├── EmailConfirmationTokenRepository.cs
│   └── UnitOfWork.cs
├── Time/   # Time provider implementation
│   └── SystemTimeProvider.cs
└── DependencyInjection.cs   # Service registration
```

## 🔧 Key Components

### 1. Database Context

**`ApplicationDbContext.cs`**
- Inherits from `DbContext`
- Defines all `DbSet<>` properties for entities
- Configures entity relationships and constraints
- Implements soft delete query filters
- Handles automatic audit field updates (CreatedAt, ModifiedAt, etc.)

**Features**:
- ✅ Global query filter for soft-deleted entities
- ✅ Automatic audit tracking on SaveChanges
- ✅ Optimistic concurrency control with rowversion
- ✅ Custom entity type configurations

### 2. Repository Pattern

**`BaseRepository<T>`** - Generic base class providing:
- CRUD operations (Add, Update, Delete, GetById, GetAll)
- Soft delete support
- Optimistic concurrency handling with `SetOriginalRowVersion()`
- Async/await patterns
- Query filters

**Concrete Repositories**:
- `ProductRepository` - Product management
- `UserRepository` - User account management
- `RoleRepository` - Role management
- `UserRoleRepository` - User-role relationships
- `PasswordResetTokenRepository` - Password reset tokens
- `EmailConfirmationTokenRepository` - Email confirmation tokens

### 3. Unit of Work

**`UnitOfWork.cs`**
- Implements `IUnitOfWork` from Application layer
- Manages database transactions
- Coordinates multiple repository operations
- Ensures atomic saves with `SaveChangesAsync()`

### 4. Authentication Services

**`AuthenticationService`**
- User authentication and authorization
- Login/logout workflows
- Token validation

**`JwtTokenService`**
- JWT access token generation
- Token validation and parsing
- Claims management
- Token expiration handling

**`PasswordHasher`**
- Secure password hashing using BCrypt/PBKDF2
- Password verification
- Salt generation

**`PasswordValidator`**
- Password complexity rules
- Minimum/maximum length validation
- Character requirement checks
- Custom validation logic

**`RefreshTokenHandler`**
- Refresh token generation
- Token rotation
- Expiration management

### 5. Configuration

**`JwtOptions`**
```csharp
{
    "Jwt": {
        "Secret": "your-secret-key-here",
        "Issuer": "Archu.Api",
"Audience": "Archu.Client",
    "AccessTokenExpirationMinutes": 60,
        "RefreshTokenExpirationDays": 7
    }
}
```

**`PasswordPolicyOptions`**
```csharp
{
    "PasswordPolicy": {
        "MinimumLength": 8,
        "MaximumLength": 100,
        "RequireDigit": true,
        "RequireLowercase": true,
        "RequireUppercase": true,
      "RequireNonAlphanumeric": true
 }
}
```

### 6. Dependency Injection

**`DependencyInjection.cs`** - Extension methods for service registration:

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration,
    IHostEnvironment environment)
{
    // Database
    services.AddDatabase(configuration);
    
    // Authentication
    services.AddAuthenticationServices(configuration, environment);
    
    // Repositories
    services.AddRepositories();
 
    // Infrastructure services
    services.AddInfrastructureServices();
    
    return services;
}
```

**Registered Services**:
- Database Context (SQL Server with retry logic)
- JWT Authentication (Bearer token validation)
- All repository implementations
- Password hashing and validation
- Current user context (`ICurrentUser`)
- Time provider (`ITimeProvider`)

## 📊 Database Features

### Optimistic Concurrency Control

All entities include a `RowVersion` column:

```csharp
[Timestamp]
public byte[] RowVersion { get; set; } = Array.Empty<byte>();
```

The `BaseRepository` provides concurrency handling:

```csharp
protected void SetOriginalRowVersion(T entity, byte[] originalRowVersion)
{
    _context.Entry(entity).Property(nameof(BaseEntity.RowVersion))
        .OriginalValue = originalRowVersion;
}
```

### Soft Delete

Entities implementing `ISoftDeletable` are automatically filtered:

```csharp
modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
```

### Automatic Auditing

On `SaveChangesAsync()`, the context automatically updates:
- `CreatedAt` - When entity is first added
- `CreatedBy` - User who created the entity
- `ModifiedAt` - When entity is last updated
- `ModifiedBy` - User who last modified the entity
- `DeletedAt` - When entity is soft deleted
- `DeletedBy` - User who deleted the entity

## 🗄️ Entity Framework Core

### Migrations

**Creating a Migration**:
```bash
cd src/Archu.Infrastructure
dotnet ef migrations add YourMigrationName
```

**Applying Migrations**:
```bash
dotnet ef database update
```

**Reverting a Migration**:
```bash
dotnet ef migrations remove
```

### Design-Time Services

**`DesignTimeDbContextFactory.cs`** - Enables EF Core tooling:
- Reads connection strings from appsettings.json
- Supports environment variable overrides
- Used by `dotnet ef` commands

### Connection String Configuration

The infrastructure layer supports multiple connection string names:
1. `"Sql"` - Preferred for .NET Aspire integration
2. `"archudb"` - Legacy/fallback name

**Example** (`appsettings.json`):
```json
{
  "ConnectionStrings": {
    "Sql": "Server=localhost;Database=ArchuDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

## 🔐 Authentication Features

### JWT Token Generation

```csharp
var claims = new[]
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim(ClaimTypes.Email, user.Email),
    new Claim(ClaimTypes.Name, user.UserName)
};

var token = _jwtTokenService.GenerateAccessToken(claims);
```

### Password Security

- **Hashing Algorithm**: BCrypt or PBKDF2 (configurable)
- **Salt**: Auto-generated per password
- **Work Factor**: Configurable iterations
- **Validation**: Enforced password complexity rules

### Current User Context

**`ICurrentUser`** provides access to the authenticated user:
- `UserId` - Unique identifier
- `Email` - User's email address
- `Roles` - User's roles
- `IsAuthenticated` - Authentication status

**Implementations**:
- `HttpContextCurrentUser` - ASP.NET Core HTTP context
- `DesignTimeCurrentUser` - For EF Core migrations and tooling

## 🕒 Time Provider

**`ITimeProvider`** abstraction for testable time-dependent logic:

```csharp
public interface ITimeProvider
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
}
```

**Implementation**: `SystemTimeProvider` wraps `TimeProvider.System`

**Usage**:
```csharp
public class MyService
{
    private readonly ITimeProvider _timeProvider;
    
    public MyService(ITimeProvider timeProvider)
  {
        _timeProvider = timeProvider;
    }
    
    public void DoSomething()
    {
        var timestamp = _timeProvider.UtcNow;
        // ...
    }
}
```

## 📦 NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.EntityFrameworkCore | 9.0.10 | Core EF functionality |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.10 | SQL Server provider |
| Microsoft.EntityFrameworkCore.Design | 9.0.10 | Migration tooling |
| Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.10 | JWT authentication |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 9.0.0 | Identity integration |
| System.IdentityModel.Tokens.Jwt | 8.2.1 | JWT token handling |

## 🔧 Configuration Examples

### Program.cs Integration

```csharp
using Archu.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure services
builder.Services.AddInfrastructure(
    builder.Configuration, 
    builder.Environment);

var app = builder.Build();
app.Run();
```

### SQL Server Retry Configuration

The infrastructure automatically configures:
- **Max Retry Count**: 5 attempts
- **Max Retry Delay**: 30 seconds
- **Command Timeout**: 30 seconds
- **Retry on Failure**: Enabled for transient errors

## 🧪 Testing Considerations

### Repository Testing

Use an in-memory database or test containers:

```csharp
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
  .UseInMemoryDatabase(databaseName: "TestDb")
    .Options;

using var context = new ApplicationDbContext(options);
var repository = new ProductRepository(context);
```

### Mock Authentication

For unit tests, mock `ICurrentUser`:

```csharp
var mockCurrentUser = new Mock<ICurrentUser>();
mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid());
mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
```

## 🚨 Error Handling

### Concurrency Conflicts

`DbUpdateConcurrencyException` is thrown when:
- RowVersion mismatch detected
- Another user modified the same entity

**Handling**:
```csharp
try
{
    await _repository.UpdateAsync(product, originalRowVersion);
    await _unitOfWork.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException)
{
    return Result.Fail("Entity was modified by another user");
}
```

### Authentication Failures

`JwtSecurityTokenException` - Invalid or expired token  
`UnauthorizedAccessException` - User not authenticated  
`SecurityTokenException` - Token validation failure

## 🔍 Logging

All infrastructure services use `ILogger<T>`:
- Authentication events (login, logout, token validation)
- Database connection failures
- Retry attempts
- Concurrency conflicts
- Migration errors

## 📚 Related Documentation

- [Architecture Guide](../../docs/ARCHITECTURE.md) - Clean Architecture overview
- [Concurrency Guide](../../docs/CONCURRENCY_GUIDE.md) - Data integrity patterns
- [Application Layer](../Archu.Application/README.md) - Application abstractions
- [API Documentation](../Archu.Api/README.md) - REST API endpoints

## 🤝 Contributing

When adding infrastructure features:

1. **Define abstractions first** in `Archu.Application/Abstractions/`
2. **Implement in Infrastructure** following existing patterns
3. **Register services** in `DependencyInjection.cs`
4. **Add configurations** to `Persistence/Configurations/` for new entities
5. **Create migrations** after model changes
6. **Update this README** with new features

## 📝 Best Practices

✅ **DO**:
- Keep business logic in Domain/Application layers
- Use dependency injection for all services
- Write integration tests for repositories
- Use async/await for all database operations
- Apply query filters for soft delete
- Validate configuration on startup (e.g., `JwtOptions.Validate()`)

❌ **DON'T**:
- Reference presentation layer (API)
- Put business logic in repositories
- Use static dependencies
- Expose `DbContext` outside this layer
- Skip migration testing before deployment

## 🔄 Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-01-24 | Initial release with EF Core 9 |

---

**Maintained by**: Archu Development Team  
**Last Updated**: 2025-01-24
