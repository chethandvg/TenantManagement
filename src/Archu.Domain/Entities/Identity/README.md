# Identity Domain Entities

This directory contains the core authentication and authorization domain entities.

## Entities

### ApplicationUser
Represents an authenticated user in the system. Inherits from `BaseEntity` to automatically get:
- **Id**: Unique identifier (GUID)
- **Auditing**: CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy
- **Soft Delete**: IsDeleted, DeletedAtUtc, DeletedBy
- **Concurrency Control**: RowVersion

#### Key Properties
- **UserName**: Unique username
- **Email/NormalizedEmail**: Email address for login and notifications
- **PasswordHash**: Bcrypt/Argon2 hashed password (never plain text)
- **EmailConfirmed**: Email verification status
- **SecurityStamp**: Used to invalidate tokens when credentials change
- **RefreshToken/RefreshTokenExpiryTime**: JWT refresh token mechanism
- **AccessFailedCount**: Failed login attempts counter
- **LockoutEnabled/LockoutEnd**: Account lockout mechanism
- **PhoneNumber/PhoneNumberConfirmed**: Optional phone verification
- **TwoFactorEnabled**: 2FA status
- **UserRoles**: Navigation property for assigned roles

#### Features
- ✅ Email-based authentication
- ✅ Password hashing support
- ✅ JWT refresh token mechanism
- ✅ Account lockout protection
- ✅ Two-factor authentication ready
- ✅ Phone number verification support
- ✅ Security stamp for token invalidation

### ApplicationRole
Represents a security role (e.g., Admin, User, Manager).

#### Key Properties
- **Name/NormalizedName**: Role name (e.g., "Admin")
- **Description**: Optional role description
- **UserRoles**: Navigation property for users in this role

#### Standard Roles
Consider implementing these standard roles:
- **Admin**: Full system access
- **User**: Standard user access
- **Manager**: Elevated permissions
- **Guest**: Read-only access

### UserRole (Junction Entity)
Manages the many-to-many relationship between users and roles.

#### Key Properties
- **UserId**: Foreign key to ApplicationUser
- **RoleId**: Foreign key to ApplicationRole
- **AssignedAtUtc**: When the role was assigned (audit)
- **AssignedBy**: Who assigned the role (audit)

## Design Decisions

### Why Not Use ASP.NET Core Identity?
While ASP.NET Core Identity is a robust framework, this custom implementation provides:
1. **Clean Architecture Compliance**: Domain entities with no framework dependencies
2. **Flexibility**: Full control over properties and behavior
3. **Simplicity**: Only the features you need
4. **Testability**: Pure domain objects without infrastructure concerns

### Why BaseEntity Inheritance?
- **DRY Principle**: Avoid repeating audit/soft-delete logic
- **Consistency**: All entities follow the same patterns
- **Concurrency Control**: Automatic optimistic locking via RowVersion
- **Audit Trail**: Automatic tracking of who/when changes occurred

### Security Considerations
- ✅ **Password Hashing**: Never store plain text passwords (use BCrypt, Argon2, or ASP.NET Identity PasswordHasher)
- ✅ **Security Stamp**: Invalidate tokens when credentials change
- ✅ **Lockout Protection**: Prevent brute force attacks
- ✅ **Email Normalization**: Case-insensitive email lookups
- ✅ **Refresh Tokens**: Secure JWT token renewal
- ✅ **Soft Delete**: User data retained for audit compliance

## Next Steps

### 1. Application Layer (Abstractions)
Create interfaces in `Archu.Application/Abstractions/Identity/`:
- `IUserRepository`
- `IRoleRepository`
- `IAuthenticationService`
- `ITokenService`

### 2. Infrastructure Layer
Implement in `Archu.Infrastructure/`:
- Entity configurations (EF Core)
- Repository implementations
- Password hashing service
- JWT token service
- Database migrations

### 3. Enhance ICurrentUser
Update `ICurrentUser` interface to include:
```csharp
public interface ICurrentUser
{
    string? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    IEnumerable<string> GetRoles();
}
```

### 4. API Layer
- JWT authentication middleware
- Authorization policies
- Authentication controllers (login, register, refresh)

## Example Usage

```csharp
// Creating a user
var user = new ApplicationUser
{
    UserName = "john.doe",
    Email = "john.doe@example.com",
    NormalizedEmail = "JOHN.DOE@EXAMPLE.COM",
    EmailConfirmed = false,
    LockoutEnabled = true
};

// Assigning a role
var adminRole = await roleRepository.GetByNameAsync("Admin");
var userRole = new UserRole
{
    UserId = user.Id,
    RoleId = adminRole.Id,
    AssignedBy = currentUser.UserId
};
user.UserRoles.Add(userRole);

// Checking lockout
if (user.IsLockedOut)
{
    return Result.Failure("Account is locked");
}
```

## Database Schema

The entities will generate this schema:

```sql
-- ApplicationUsers table
CREATE TABLE ApplicationUsers (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserName NVARCHAR(256) NOT NULL,
    Email NVARCHAR(256) NOT NULL,
    NormalizedEmail NVARCHAR(256) NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    EmailConfirmed BIT NOT NULL,
    SecurityStamp NVARCHAR(MAX) NOT NULL,
    RefreshToken NVARCHAR(MAX) NULL,
    RefreshTokenExpiryTime DATETIME2 NULL,
    AccessFailedCount INT NOT NULL,
    LockoutEnabled BIT NOT NULL,
    LockoutEnd DATETIME2 NULL,
    PhoneNumber NVARCHAR(50) NULL,
    PhoneNumberConfirmed BIT NOT NULL,
    TwoFactorEnabled BIT NOT NULL,
    -- Audit fields
    CreatedAtUtc DATETIME2 NOT NULL,
    CreatedBy NVARCHAR(MAX) NULL,
    ModifiedAtUtc DATETIME2 NULL,
    ModifiedBy NVARCHAR(MAX) NULL,
    -- Soft delete
    IsDeleted BIT NOT NULL,
    DeletedAtUtc DATETIME2 NULL,
    DeletedBy NVARCHAR(MAX) NULL,
    -- Concurrency
    RowVersion ROWVERSION NOT NULL
);

-- ApplicationRoles table
CREATE TABLE ApplicationRoles (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(256) NOT NULL,
    NormalizedName NVARCHAR(256) NOT NULL,
    Description NVARCHAR(500) NULL,
    -- Audit fields...
    RowVersion ROWVERSION NOT NULL
);

-- UserRoles junction table
CREATE TABLE UserRoles (
    UserId UNIQUEIDENTIFIER NOT NULL,
    RoleId UNIQUEIDENTIFIER NOT NULL,
    AssignedAtUtc DATETIME2 NOT NULL,
    AssignedBy NVARCHAR(MAX) NULL,
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES ApplicationUsers(Id),
    FOREIGN KEY (RoleId) REFERENCES ApplicationRoles(Id)
);

-- Indexes for performance
CREATE UNIQUE INDEX IX_ApplicationUsers_NormalizedEmail ON ApplicationUsers(NormalizedEmail);
CREATE UNIQUE INDEX IX_ApplicationUsers_UserName ON ApplicationUsers(UserName);
CREATE UNIQUE INDEX IX_ApplicationRoles_NormalizedName ON ApplicationRoles(NormalizedName);
```

## References
- [OWASP Password Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)
- [JWT Best Practices](https://datatracker.ietf.org/doc/html/rfc8725)
- [ASP.NET Core Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)

---

**Version**: 1.0  
**Last Updated**: 2025-01-22  
**Author**: Archu Development Team
