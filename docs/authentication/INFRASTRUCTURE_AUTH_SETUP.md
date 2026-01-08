# Infrastructure Layer Authentication Setup - Complete Guide

## Overview

This document describes the authentication infrastructure implementation for the TentMan application. The infrastructure layer provides database persistence for authentication entities using Entity Framework Core.

**Date**: 2025-01-22  
**Version**: 1.0  
**Status**: ✅ Completed

---

## 📋 Implementation Summary

### What Was Implemented

1. ✅ **NuGet Package Installation**
   - Installed `Microsoft.AspNetCore.Identity.EntityFrameworkCore` v9.0.0
   
2. ✅ **Entity Configurations**
   - Created `ApplicationUserConfiguration.cs`
   - Created `ApplicationRoleConfiguration.cs`
   - Created `UserRoleConfiguration.cs`

3. ✅ **ApplicationDbContext Updates**
   - Added `Users` DbSet
   - Added `Roles` DbSet
   - Added `UserRoles` DbSet

4. ✅ **Database Migration**
   - Created migration: `20251020183350_AddIdentityEntities`
   - Includes `Users`, `Roles`, and `UserRoles` tables
   - All necessary indexes and foreign keys configured

---

## 🗄️ Database Schema

### Tables Created

#### 1. Users Table
Stores user authentication and account information.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | uniqueidentifier | PK | User unique identifier (GUID) |
| `UserName` | nvarchar(256) | NOT NULL, UNIQUE | Unique username |
| `Email` | nvarchar(256) | NOT NULL, UNIQUE | User email address |
| `NormalizedEmail` | nvarchar(256) | NOT NULL, UNIQUE | Normalized email for lookups |
| `PasswordHash` | nvarchar(512) | NOT NULL | Hashed password |
| `EmailConfirmed` | bit | NOT NULL | Email verification status |
| `SecurityStamp` | nvarchar(256) | NOT NULL | Token invalidation stamp |
| `RefreshToken` | nvarchar(512) | NULL | JWT refresh token |
| `RefreshTokenExpiryTime` | datetime2 | NULL | Token expiry timestamp |
| `AccessFailedCount` | int | NOT NULL | Failed login attempts |
| `LockoutEnabled` | bit | NOT NULL | Lockout feature flag |
| `LockoutEnd` | datetime2 | NULL | Lockout expiry timestamp |
| `PhoneNumber` | nvarchar(50) | NULL | User phone number |
| `PhoneNumberConfirmed` | bit | NOT NULL | Phone verification status |
| `TwoFactorEnabled` | bit | NOT NULL | 2FA enablement flag |
| `CreatedAtUtc` | datetime2 | NOT NULL | Record creation timestamp |
| `CreatedBy` | nvarchar(max) | NULL | User who created record |
| `ModifiedAtUtc` | datetime2 | NULL | Last modification timestamp |
| `ModifiedBy` | nvarchar(max) | NULL | User who modified record |
| `IsDeleted` | bit | NOT NULL | Soft delete flag |
| `DeletedAtUtc` | datetime2 | NULL | Deletion timestamp |
| `DeletedBy` | nvarchar(max) | NULL | User who deleted record |
| `RowVersion` | rowversion | NOT NULL | Concurrency token |

**Indexes:**
- `IX_Users_Email` (unique)
- `IX_Users_NormalizedEmail` (unique)
- `IX_Users_UserName` (unique)

---

#### 2. Roles Table
Stores security roles for role-based access control.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | uniqueidentifier | PK | Role unique identifier (GUID) |
| `Name` | nvarchar(256) | NOT NULL, UNIQUE | Role name (e.g., "Admin") |
| `NormalizedName` | nvarchar(256) | NOT NULL, UNIQUE | Normalized role name |
| `Description` | nvarchar(500) | NULL | Role description |
| `CreatedAtUtc` | datetime2 | NOT NULL | Record creation timestamp |
| `CreatedBy` | nvarchar(max) | NULL | User who created record |
| `ModifiedAtUtc` | datetime2 | NULL | Last modification timestamp |
| `ModifiedBy` | nvarchar(max) | NULL | User who modified record |
| `IsDeleted` | bit | NOT NULL | Soft delete flag |
| `DeletedAtUtc` | datetime2 | NULL | Deletion timestamp |
| `DeletedBy` | nvarchar(max) | NULL | User who deleted record |
| `RowVersion` | rowversion | NOT NULL | Concurrency token |

**Indexes:**
- `IX_Roles_Name` (unique)
- `IX_Roles_NormalizedName` (unique)

---

#### 3. UserRoles Table (Junction Table)
Many-to-many relationship between Users and Roles.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `UserId` | uniqueidentifier | PK, FK | Reference to Users table |
| `RoleId` | uniqueidentifier | PK, FK | Reference to Roles table |
| `AssignedAtUtc` | datetime2 | NOT NULL, DEFAULT GETUTCDATE() | Assignment timestamp |
| `AssignedBy` | nvarchar(256) | NULL | User who assigned role |

**Primary Key:** Composite (`UserId`, `RoleId`)

**Foreign Keys:**
- `FK_UserRoles_Users_UserId` → `Users.Id` (Cascade Delete)
- `FK_UserRoles_Roles_RoleId` → `Roles.Id` (Cascade Delete)

**Indexes:**
- `IX_UserRoles_UserId`
- `IX_UserRoles_RoleId`

---

## 📁 Files Created/Modified

### New Files

#### 1. `ApplicationUserConfiguration.cs`
**Location**: `src/TentMan.Infrastructure/Persistence/Configurations/ApplicationUserConfiguration.cs`

**Purpose**: Entity Framework Core configuration for `ApplicationUser` entity.

**Key Features:**
- Table name: `Users`
- String length constraints for all fields
- Unique indexes on `Email`, `NormalizedEmail`, and `UserName`
- Concurrency control via `RowVersion`
- One-to-many relationship with `UserRoles`

```csharp
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        
        // Property configurations
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);
            
        // Unique indexes
        builder.HasIndex(u => u.Email)
            .IsUnique();
            
        // Relationships
        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

#### 2. `ApplicationRoleConfiguration.cs`
**Location**: `src/TentMan.Infrastructure/Persistence/Configurations/ApplicationRoleConfiguration.cs`

**Purpose**: Entity Framework Core configuration for `ApplicationRole` entity.

**Key Features:**
- Table name: `Roles`
- Unique indexes on `Name` and `NormalizedName`
- Optional description field
- One-to-many relationship with `UserRoles`

```csharp
public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);
        
        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(256);
            
        builder.HasIndex(r => r.Name)
            .IsUnique();
    }
}
```

---

#### 3. `UserRoleConfiguration.cs`
**Location**: `src/TentMan.Infrastructure/Persistence/Configurations/UserRoleConfiguration.cs`

**Purpose**: Entity Framework Core configuration for `UserRole` junction entity.

**Key Features:**
- Table name: `UserRoles`
- Composite primary key (`UserId`, `RoleId`)
- Default value for `AssignedAtUtc` using SQL Server's `GETUTCDATE()`
- Audit tracking with `AssignedBy` field

```csharp
public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        
        // Composite primary key
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });
        
        builder.Property(ur => ur.AssignedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
```

---

### Modified Files

#### `ApplicationDbContext.cs`
**Location**: `src/TentMan.Infrastructure/Persistence/ApplicationDbContext.cs`

**Changes:**
- Added `using TentMan.Domain.Entities.Identity;`
- Added three new DbSets:
  - `public DbSet<ApplicationUser> Users => Set<ApplicationUser>();`
  - `public DbSet<ApplicationRole> Roles => Set<ApplicationRole>();`
  - `public DbSet<UserRole> UserRoles => Set<UserRole>();`

**Impact:**
- DbContext now tracks identity entities
- Configurations automatically applied via `ApplyConfigurationsFromAssembly()`
- Soft delete query filter applies to `Users` and `Roles` (via `BaseEntity`)
- Audit tracking applies to `Users` and `Roles` (via `IAuditable`)

---

## 🔄 Migration Details

### Migration File
**Name**: `20251020183350_AddIdentityEntities`  
**Location**: `src/TentMan.Infrastructure/Persistence/Migrations/20251020183350_AddIdentityEntities.cs`

### Migration Operations

**Up Migration:**
1. Creates `Roles` table
2. Creates `Users` table
3. Creates `UserRoles` junction table
4. Creates all indexes:
   - `IX_Roles_Name` (unique)
   - `IX_Roles_NormalizedName` (unique)
   - `IX_Users_Email` (unique)
   - `IX_Users_NormalizedEmail` (unique)
   - `IX_Users_UserName` (unique)
   - `IX_UserRoles_UserId`
   - `IX_UserRoles_RoleId`
5. Configures foreign key relationships

**Down Migration:**
1. Drops `UserRoles` table (cascade removes relationships)
2. Drops `Roles` table
3. Drops `Users` table

---

## 🚀 Next Steps

### 1. Apply the Migration

**Development Environment:**
```bash
cd src/TentMan.Infrastructure
dotnet ef database update --startup-project ../TentMan.Api --context ApplicationDbContext
```

**Production/CI/CD:**
```bash
# Generate idempotent SQL script
dotnet ef migrations script \
  --project src/TentMan.Infrastructure \
  --startup-project src/TentMan.Api \
  --idempotent \
  -o artifacts/auth-migrations.sql \
  --context ApplicationDbContext
```

### 2. Seed Default Roles (Recommended)

Create a database seeder to populate initial roles:

```csharp
// Example seeder (implement in Infrastructure layer)
public static class IdentityDbSeeder
{
    public static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        var roles = new[]
        {
            new ApplicationRole 
            { 
                Id = Guid.NewGuid(),
                Name = ApplicationRoles.Admin, 
                NormalizedName = ApplicationRoles.Admin.ToUpperInvariant(),
                Description = "Full system access"
            },
            new ApplicationRole 
            { 
                Id = Guid.NewGuid(),
                Name = ApplicationRoles.User, 
                NormalizedName = ApplicationRoles.User.ToUpperInvariant(),
                Description = "Standard user access"
            }
        };

        foreach (var role in roles)
        {
            if (!await context.Roles.AnyAsync(r => r.NormalizedName == role.NormalizedName))
            {
                await context.Roles.AddAsync(role);
            }
        }

        await context.SaveChangesAsync();
    }
}
```

### 3. Implement Repository Pattern (Optional)

If you want to abstract EF Core from the application layer:

```csharp
// Application Layer
public interface IUserRepository
{
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApplicationUser> AddAsync(ApplicationUser user, CancellationToken ct = default);
    // ... other methods
}

// Infrastructure Layer
public class UserRepository : BaseRepository<ApplicationUser>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }
    
    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await DbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant(), ct);
    }
}
```

### 4. Update Unit of Work (If Using)

Add user and role repositories to `IUnitOfWork`:

```csharp
public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

---

## ✅ Verification Checklist

Before moving forward, verify:

- [x] NuGet package `Microsoft.AspNetCore.Identity.EntityFrameworkCore` installed
- [x] Entity configurations created for all identity entities
- [x] `ApplicationDbContext` updated with identity DbSets
- [x] Migration created successfully
- [x] Solution builds without errors
- [ ] Migration applied to development database
- [ ] Tables created with correct schema
- [ ] Indexes created and verified
- [ ] Foreign key relationships working
- [ ] Default roles seeded (optional)
- [ ] Repository implementations created (optional)

---

## 🔒 Security Features Provided

### Automatic Features (via BaseEntity)

1. **Optimistic Concurrency Control**
   - `RowVersion` column prevents lost updates
   - SQL Server automatically increments on every change

2. **Audit Tracking**
   - `CreatedAtUtc`, `CreatedBy` on insert
   - `ModifiedAtUtc`, `ModifiedBy` on update
   - `DeletedAtUtc`, `DeletedBy` on soft delete

3. **Soft Delete**
   - `IsDeleted` flag preserves data
   - Global query filter automatically excludes deleted records
   - Historical data retained for compliance

### Authentication Features (via ApplicationUser)

1. **Password Security**
   - `PasswordHash` stores hashed passwords (never plain text)
   - Use ASP.NET Core Identity's password hasher

2. **JWT Refresh Tokens**
   - `RefreshToken` field for secure token renewal
   - `RefreshTokenExpiryTime` for automatic expiry

3. **Account Lockout**
   - `AccessFailedCount` tracks failed login attempts
   - `LockoutEnabled` flag for lockout feature
   - `LockoutEnd` timestamp for temporary lockouts

4. **Email Verification**
   - `EmailConfirmed` flag for account activation
   - Prevents unauthorized access

5. **Security Stamp**
   - Invalidates tokens when credentials change
   - Forces re-authentication on password change

---

## 📚 Related Documentation

- [Domain Layer Authentication](../src/TentMan.Domain/Entities/Identity/README.md)
- [Application Layer Authentication](../src/TentMan.Application/docs/Authentication/README.md)
- [Authentication Guide](../src/TentMan.Application/docs/Authentication/README.md)
- [Concurrency Guide](./CONCURRENCY_GUIDE.md)
- [Architecture Overview](./ARCHITECTURE.md)

---

## 🛠️ Troubleshooting

### Migration Issues

**Problem**: "Build failed" when creating migration
**Solution**: Ensure solution builds successfully before creating migrations

**Problem**: "DbContext not found"
**Solution**: Verify `--startup-project` and `--project` paths are correct

**Problem**: "Connection string not found"
**Solution**: Check `appsettings.Development.json` in API project

### Database Issues

**Problem**: Foreign key constraint violations
**Solution**: Ensure parent records (Users/Roles) exist before inserting UserRoles

**Problem**: Unique constraint violations
**Solution**: Check for duplicate emails, usernames, or role names

**Problem**: RowVersion concurrency conflicts
**Solution**: See [Concurrency Guide](./CONCURRENCY_GUIDE.md)

---

## 📊 Performance Considerations

### Indexes Created

All necessary indexes are automatically created:

1. **Unique Indexes** (prevent duplicates):
   - Email, NormalizedEmail, UserName (Users)
   - Name, NormalizedName (Roles)

2. **Foreign Key Indexes** (speed up joins):
   - UserId, RoleId (UserRoles)

### Query Performance Tips

1. **Always use normalized fields for lookups:**
   ```csharp
   // ✅ Good - uses index
   var user = await context.Users
       .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant());
   
   // ❌ Bad - case-insensitive comparison is slow
   var user = await context.Users
       .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
   ```

2. **Include related data when needed:**
   ```csharp
   var user = await context.Users
       .Include(u => u.UserRoles)
       .ThenInclude(ur => ur.Role)
       .FirstOrDefaultAsync(u => u.Id == userId);
   ```

3. **Use AsNoTracking for read-only queries:**
   ```csharp
   var users = await context.Users
       .AsNoTracking()
       .ToListAsync();
   ```

---

## 🎯 Best Practices

1. ✅ **Always hash passwords** - Never store plain text
2. ✅ **Use normalized fields** for case-insensitive lookups
3. ✅ **Validate email** before setting `EmailConfirmed = true`
4. ✅ **Implement lockout** to prevent brute force attacks
5. ✅ **Rotate security stamps** when credentials change
6. ✅ **Set refresh token expiry** to reasonable values (7-30 days)
7. ✅ **Check `IsLockedOut`** property before authentication
8. ✅ **Soft delete users** instead of physical deletion
9. ✅ **Include RowVersion** in update operations for concurrency
10. ✅ **Log security events** (failed logins, lockouts, etc.)

---

**Last Updated**: 2025-01-22  
**Implemented By**: Infrastructure Team  
**Reviewed By**: Pending  
**Status**: ✅ Ready for Testing

---

## Summary

This infrastructure setup provides a complete authentication database schema with:

- ✅ Secure user authentication storage
- ✅ Role-based authorization
- ✅ Optimistic concurrency control
- ✅ Automatic audit tracking
- ✅ Soft delete support
- ✅ Performance-optimized indexes
- ✅ Best practice security features

The next step is to implement the authentication services in the Application layer and expose authentication endpoints in the API layer.
