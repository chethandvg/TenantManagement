# Archu.Domain

The Domain layer is the core of the Archu application, containing all business logic, entities, and domain rules. This layer has zero external dependencies and represents the heart of Clean Architecture.

## 📋 Overview

**Target Framework**: .NET 9  
**Layer**: Domain (Core/Innermost Layer)  
**Dependencies**: None (zero external dependencies)

## 🎯 Purpose

The Domain layer is responsible for:
- **Business Entities**: Core domain models (Product, User, Role)
- **Business Logic**: Domain rules and invariants
- **Value Objects**: Immutable domain concepts
- **Domain Abstractions**: Interfaces that define domain contracts
- **Enums & Constants**: Domain-specific enumerations and constants
- **Domain Extensions**: Helper methods for domain operations

## 🏗️ Architecture Principle

> **Independence**: The Domain layer should have **zero dependencies** on other layers, frameworks, or infrastructure concerns. It represents pure business logic.

```
┌─────────────────────────────┐
│     Archu.Domain            │
│  (No Dependencies)          │
│  - Pure business logic      │
│  - Framework-independent    │
└─────────────────────────────┘
        ↑ depends on
        │
┌───────┴─────────────────────┐
│   Archu.Application         │
│   (Uses domain entities)    │
└─────────────────────────────┘
    ↑ depends on
        │
┌───────┴─────────────────────┐
│   Archu.Infrastructure    │
│   (Persists domain entities)│
└─────────────────────────────┘
```

## 📦 Project Structure

```
Archu.Domain/
├── Abstractions/      # Domain interfaces
│   ├── Identity/
│   │   ├── IHasOwner.cs
│   │   ├── IHasSharedAccess.cs
│   │   ├── IPermissionService.cs
│   │   └── IRoleService.cs
│   ├── IAuditable.cs
│   └── ISoftDeletable.cs
├── Common/# Base classes
│   └── BaseEntity.cs
├── Constants/   # Domain constants
│   ├── CustomClaimTypes.cs
│   ├── PermissionNames.cs
│   ├── RoleNames.cs
│   ├── RolePermissions.cs
│   └── RolePermissionClaims.cs
├── Entities/          # Domain entities
│   ├── Identity/
│   │   ├── ApplicationRole.cs
│ │   ├── ApplicationUser.cs
│   │   ├── UserRole.cs
│   │   └── UserTokens.cs
│   └── Product.cs
├── Enums/             # Domain enumerations
│   ├── Permission.cs
│   └── SystemRole.cs
├── Extensions/      # Domain extension methods
│   └── PermissionExtensions.cs
└── ValueObjects/      # Value objects
    └── PasswordPolicyOptions.cs
```

## 🔧 Key Components

### 1. Base Entity

**BaseEntity** provides common functionality for all domain entities:

```csharp
public abstract class BaseEntity : IAuditable, ISoftDeletable
{
  // Primary Key
    public Guid Id { get; set; } = Guid.NewGuid();

    // Auditing (IAuditable)
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }

    // Soft Delete (ISoftDeletable)
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    // Concurrency Control
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
```

**Key Features**:
- **Guid Primary Key**: Globally unique identifiers
- **Automatic Auditing**: Tracks who created/modified entities and when
- **Soft Delete**: Preserves data history instead of physical deletion
- **Optimistic Concurrency**: Prevents lost updates using `rowversion`

**All entities inherit from BaseEntity**:
```csharp
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

### 2. Domain Abstractions

#### IAuditable
Entities that need audit tracking:
```csharp
public interface IAuditable
{
    DateTime CreatedAtUtc { get; set; }
    string? CreatedBy { get; set; }
    DateTime? ModifiedAtUtc { get; set; }
    string? ModifiedBy { get; set; }
}
```

**Automatically handled by**:
- `SaveChangesInterceptor` in Infrastructure layer
- Populated on entity creation/modification

#### ISoftDeletable
Entities that support soft delete:
```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
    string? DeletedBy { get; set; }
}
```

**Features**:
- Marks entities as deleted without removing from database
- Preserves data history for auditing
- Automatically filtered in queries via global query filter

#### IHasOwner
Entities owned by a specific user:
```csharp
public interface IHasOwner
{
    Guid OwnerId { get; set; }
    bool IsOwnedBy(Guid userId);
}
```

**Example**:
```csharp
public class Product : BaseEntity, IHasOwner
{
    public Guid OwnerId { get; set; }
    
    public bool IsOwnedBy(Guid userId) => OwnerId == userId;
}
```

**Used for**:
- Authorization (users can only modify their own data)
- Multi-tenancy (data isolation by user)

#### IHasSharedAccess
Entities that can be shared with other users:
```csharp
public interface IHasSharedAccess
{
    ICollection<Guid> SharedWithUserIds { get; set; }
    bool IsSharedWith(Guid userId);
}
```

**Future Use Cases**:
- Collaborative features
- Document sharing
- Team-based access control

### 3. Identity Entities

#### ApplicationUser
Represents an authenticated user:

```csharp
public class ApplicationUser : BaseEntity
{
    // Core Identity
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public string SecurityStamp { get; set; } = string.Empty;

    // JWT Refresh Token
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

  // Account Lockout
    public int AccessFailedCount { get; set; }
    public bool LockoutEnabled { get; set; }
public DateTime? LockoutEnd { get; set; }

    // Contact Info
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }

    // Security
    public bool TwoFactorEnabled { get; set; }

    // Relationships
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    // Business Logic
    public bool IsLockedOut => IsLockedOutAt(DateTime.UtcNow);
    
    public bool IsLockedOutAt(DateTime currentTime)
    {
    return LockoutEnabled && LockoutEnd.HasValue && LockoutEnd.Value > currentTime;
    }
}
```

**Key Features**:
- Custom user entity (not using ASP.NET Core Identity)
- JWT refresh token support
- Account lockout protection
- Email confirmation
- Testable lockout logic (accepts time parameter)

#### ApplicationRole
Represents a user role with permissions:

```csharp
public class ApplicationRole : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
 
    // Comma-separated permission list
    public string Permissions { get; set; } = string.Empty;

    // Relationships
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    // Business Logic
    public bool HasPermission(Permission permission)
    {
        if (string.IsNullOrWhiteSpace(Permissions))
            return false;

        var permissionList = Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return permissionList.Contains(permission.ToString());
    }

    public void AddPermission(Permission permission)
    {
        var permissions = Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        
        if (!permissions.Contains(permission.ToString()))
        {
            permissions.Add(permission.ToString());
     Permissions = string.Join(',', permissions);
        }
    }

    public void RemovePermission(Permission permission)
    {
        var permissions = Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
  permissions.Remove(permission.ToString());
        Permissions = string.Join(',', permissions);
 }
}
```

**Key Features**:
- Role-based access control (RBAC)
- Permission management
- Built-in permission checking logic
- Efficient comma-separated permission storage

#### UserRole
Many-to-many join entity:

```csharp
public class UserRole : BaseEntity
{
 public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public Guid RoleId { get; set; }
    public ApplicationRole Role { get; set; } = null!;
}
```

**Purpose**:
- Links users to roles
- Supports many-to-many relationship
- Allows audit tracking of role assignments

#### UserTokens
Stores password reset and email confirmation tokens:

```csharp
public class UserTokens : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    public string? EmailConfirmationToken { get; set; }
    public DateTime? EmailConfirmationTokenExpiry { get; set; }

    // Business Logic
    public bool IsPasswordResetTokenValid(string token)
    {
   return !string.IsNullOrEmpty(PasswordResetToken)
 && PasswordResetToken == token
        && PasswordResetTokenExpiry.HasValue
            && PasswordResetTokenExpiry.Value > DateTime.UtcNow;
    }

    public bool IsEmailConfirmationTokenValid(string token)
    {
        return !string.IsNullOrEmpty(EmailConfirmationToken)
        && EmailConfirmationToken == token
   && EmailConfirmationTokenExpiry.HasValue
   && EmailConfirmationTokenExpiry.Value > DateTime.UtcNow;
    }
}
```

**Features**:
- Separate token storage for security
- Token expiration support
- Validation logic in domain

### 4. Product Entity

```csharp
public class Product : BaseEntity, IHasOwner
{
    public Product()
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = "System";
        OwnerId = Guid.NewGuid();
    }

    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Guid OwnerId { get; set; }

    public bool IsOwnedBy(Guid userId) => OwnerId == userId;
}
```

**Key Features**:
- Example domain entity for product catalog
- Ownership tracking via `IHasOwner`
- Inherits auditing and soft delete from `BaseEntity`

### 5. Enumerations

#### SystemRole
System-level roles:

```csharp
public enum SystemRole
{
  Guest = 0,
 User = 1,
    Manager = 2,
    Administrator = 3,
    SuperAdmin = 4
}
```

#### Permission
Fine-grained permissions:

```csharp
[Flags]
public enum Permission : long
{
    None = 0,

    // User Management (1-99)
    ViewUsers = 1L << 0,
    CreateUsers = 1L << 1,
    EditUsers = 1L << 2,
    DeleteUsers = 1L << 3,

    // Role Management (100-199)
    ViewRoles = 1L << 10,
    CreateRoles = 1L << 11,
    EditRoles = 1L << 12,
    DeleteRoles = 1L << 13,
    AssignRoles = 1L << 14,

    // Product Management (200-299)
    ViewProducts = 1L << 20,
    CreateProducts = 1L << 21,
  EditProducts = 1L << 22,
    DeleteProducts = 1L << 23,

    // System (1000+)
    ViewAuditLogs = 1L << 30,
    SystemConfiguration = 1L << 31
}
```

**Features**:
- Flag-based enumeration (supports combinations)
- Hierarchical organization by feature area
- Extensible for new permissions

### 6. Constants

#### RoleNames
```csharp
public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";
    public const string User = "User";
    public const string Guest = "Guest";
}
```

#### PermissionNames
```csharp
public static class PermissionNames
{
    public const string ViewUsers = "Users.View";
 public const string CreateUsers = "Users.Create";
    public const string EditUsers = "Users.Edit";
    public const string DeleteUsers = "Users.Delete";
    // ... more permissions
}
```

#### RolePermissions
Defines default permissions for each role:

```csharp
public static class RolePermissions
{
    public static class SuperAdmin
    {
        public static Permission GetPermissions() =>
   Permission.ViewUsers | Permission.CreateUsers | Permission.EditUsers |
            Permission.DeleteUsers | Permission.ViewRoles | Permission.CreateRoles |
            Permission.EditRoles | Permission.DeleteRoles | Permission.AssignRoles |
            Permission.ViewProducts | Permission.CreateProducts | 
      Permission.EditProducts | Permission.DeleteProducts |
      Permission.ViewAuditLogs | Permission.SystemConfiguration;
    }

    public static class Administrator
  {
        public static Permission GetPermissions() =>
     Permission.ViewUsers | Permission.CreateUsers | Permission.EditUsers |
            Permission.ViewRoles | Permission.AssignRoles |
   Permission.ViewProducts | Permission.CreateProducts | 
 Permission.EditProducts | Permission.DeleteProducts;
    }

    // ... more roles
}
```

### 7. Value Objects

#### PasswordPolicyOptions
```csharp
public class PasswordPolicyOptions
{
    public int MinimumLength { get; set; } = 8;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialCharacter { get; set; } = true;
    public int MaxFailedAccessAttempts { get; set; } = 5;
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);
}
```

**Used By**:
- Password validation in Application layer
- Account lockout logic

### 8. Domain Extensions

#### PermissionExtensions
```csharp
public static class PermissionExtensions
{
    public static bool HasPermission(this Permission userPermissions, Permission permission)
    {
        return (userPermissions & permission) == permission;
    }

public static Permission AddPermission(this Permission userPermissions, Permission permission)
    {
        return userPermissions | permission;
    }

    public static Permission RemovePermission(this Permission userPermissions, Permission permission)
  {
        return userPermissions & ~permission;
    }

    public static IEnumerable<string> GetPermissionNames(this Permission permissions)
    {
        return Enum.GetValues<Permission>()
   .Where(p => p != Permission.None && permissions.HasPermission(p))
            .Select(p => p.ToString());
    }
}
```

**Usage**:
```csharp
var userPermissions = Permission.ViewProducts | Permission.EditProducts;

// Check permission
if (userPermissions.HasPermission(Permission.ViewProducts))
{
    // User can view products
}

// Add permission
userPermissions = userPermissions.AddPermission(Permission.DeleteProducts);

// Get all permission names
var permissionNames = userPermissions.GetPermissionNames();
// Returns: ["ViewProducts", "EditProducts", "DeleteProducts"]
```

## 📋 Domain Rules & Invariants

### Entity Creation Rules

1. **Always initialize required fields**:
```csharp
public Product()
{
    CreatedAtUtc = DateTime.UtcNow;
    CreatedBy = "System";
    OwnerId = Guid.NewGuid(); // Must be set
}
```

2. **Use default values for safety**:
```csharp
public string Name { get; set; } = string.Empty; // Never null
public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
```

### Business Logic in Entities

✅ **DO** - Put domain logic in entities:
```csharp
public bool IsOwnedBy(Guid userId) => OwnerId == userId;
public bool HasPermission(Permission permission) { /* logic */ }
public bool IsLockedOut => IsLockedOutAt(DateTime.UtcNow);
```

❌ **DON'T** - Put infrastructure concerns in domain:
```csharp
// ❌ NO - Database queries in domain
public async Task<List<Product>> GetUserProducts(Guid userId)
{
  return await _dbContext.Products.Where(p => p.OwnerId == userId).ToListAsync();
}

// ✅ YES - Move to repository in Infrastructure layer
```

### Concurrency Control

All entities inherit `RowVersion` from `BaseEntity`:
```csharp
[Timestamp]
public byte[] RowVersion { get; set; } = Array.Empty<byte>();
```

**How it works**:
1. Entity is loaded with current `RowVersion`
2. Update is attempted with original `RowVersion`
3. Database checks if `RowVersion` matches
4. If changed, throws `DbUpdateConcurrencyException`

**Handled in Application/Infrastructure layers**.

### Soft Delete

Entities are never physically deleted:
```csharp
// ❌ NO - Physical delete
dbContext.Products.Remove(product);

// ✅ YES - Soft delete
product.IsDeleted = true;
product.DeletedAtUtc = DateTime.UtcNow;
product.DeletedBy = currentUser.Email;
```

**Automatically filtered in queries via global query filter**.

## 🧪 Domain Testing

### Entity Tests
```csharp
public class ProductTests
{
    [Fact]
    public void Product_Constructor_InitializesDefaults()
    {
        // Arrange & Act
    var product = new Product();

  // Assert
        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.NotEqual(Guid.Empty, product.OwnerId);
    Assert.NotEqual(default, product.CreatedAtUtc);
  Assert.Equal("System", product.CreatedBy);
    }

    [Fact]
    public void IsOwnedBy_MatchingUserId_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
     var product = new Product { OwnerId = userId };

     // Act
var result = product.IsOwnedBy(userId);

        // Assert
        Assert.True(result);
    }
}
```

### Business Logic Tests
```csharp
public class ApplicationRoleTests
{
    [Fact]
    public void HasPermission_ExistingPermission_ReturnsTrue()
    {
        // Arrange
  var role = new ApplicationRole 
     { 
            Permissions = "ViewProducts,EditProducts" 
     };

        // Act
        var result = role.HasPermission(Permission.ViewProducts);

    // Assert
      Assert.True(result);
    }

    [Fact]
    public void AddPermission_NewPermission_AddsSuccessfully()
    {
     // Arrange
    var role = new ApplicationRole { Permissions = "ViewProducts" };

        // Act
        role.AddPermission(Permission.EditProducts);

        // Assert
 Assert.Contains("EditProducts", role.Permissions);
    Assert.True(role.HasPermission(Permission.EditProducts));
    }
}
```

### Lockout Logic Tests
```csharp
public class ApplicationUserTests
{
    [Fact]
    public void IsLockedOutAt_ActiveLockout_ReturnsTrue()
    {
        // Arrange
        var user = new ApplicationUser
        {
   LockoutEnabled = true,
       LockoutEnd = DateTime.UtcNow.AddMinutes(10)
   };
        var currentTime = DateTime.UtcNow;

        // Act
        var result = user.IsLockedOutAt(currentTime);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsLockedOutAt_ExpiredLockout_ReturnsFalse()
    {
   // Arrange
        var user = new ApplicationUser
        {
   LockoutEnabled = true,
        LockoutEnd = DateTime.UtcNow.AddMinutes(-10)
        };
        var currentTime = DateTime.UtcNow;

        // Act
        var result = user.IsLockedOutAt(currentTime);

 // Assert
      Assert.False(result);
    }
}
```

## 📋 Best Practices

✅ **DO**:
- Keep domain entities framework-independent
- Put business logic in entities
- Use value objects for complex concepts
- Make constructors initialize required fields
- Use domain events for cross-aggregate communication (future)
- Test business logic thoroughly
- Use descriptive property names
- Validate domain invariants in constructors/methods

❌ **DON'T**:
- Add dependencies to other layers
- Put data access logic in entities
- Use database-specific attributes (except `[Timestamp]`)
- Expose collections as mutable (use `IReadOnlyCollection<T>` for read-only)
- Skip initialization in constructors
- Use primitive obsession (wrap in value objects)

## 🔗 Related Documentation

- [Architecture Guide](../../docs/ARCHITECTURE.md) - Clean Architecture overview
- [Application Layer](../Archu.Application/README.md) - Use cases and CQRS
- [Infrastructure Layer](../Archu.Infrastructure/README.md) - Persistence
- [Concurrency Guide](../../docs/CONCURRENCY_GUIDE.md) - Optimistic concurrency

## 🤝 Contributing

When adding new domain entities:

1. **Inherit from BaseEntity**: `public class MyEntity : BaseEntity`
2. **Implement domain interfaces**: `IHasOwner`, `IHasSharedAccess`, etc.
3. **Initialize in constructor**: Set required fields
4. **Add business methods**: Put domain logic in the entity
5. **Add to DbContext**: Configure in Infrastructure layer
6. **Write tests**: Test business logic and invariants
7. **Update README**: Document new entities

## 📊 Domain Statistics

| Category | Count | Description |
|----------|-------|-------------|
| **Entities** | 5 | Product, User, Role, UserRole, UserTokens |
| **Abstractions** | 6 | IAuditable, ISoftDeletable, IHasOwner, etc. |
| **Enums** | 2 | SystemRole, Permission |
| **Constants** | 5 | RoleNames, PermissionNames, etc. |
| **Value Objects** | 1 | PasswordPolicyOptions |
| **Extensions** | 1 | PermissionExtensions |

## 🔄 Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-01-24 | Initial domain documentation |

---

**Maintained by**: Archu Development Team  
**Last Updated**: 2025-01-24
