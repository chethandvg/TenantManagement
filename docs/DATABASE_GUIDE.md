# Archu Database Guide

Complete guide to database setup, migrations, seeding, and best practices.

---

## 📚 Table of Contents

- [Overview](#overview)
- [Database Setup](#database-setup)
- [Database Seeding](#database-seeding)
- [Migrations](#migrations)
- [Retry Strategy](#retry-strategy)
- [Concurrency Control](#concurrency-control)
- [Soft Delete](#soft-delete)
- [Automatic Audit Tracking](#automatic-audit-tracking)
- [Best Practices](#best-practices)

---

## 🎯 Overview

### Database Stack

- **Database**: SQL Server
- **ORM**: Entity Framework Core 9
- **Provider**: Microsoft.EntityFrameworkCore.SqlServer
- **Migrations**: Code-first migrations
- **Seeding**: Automatic system initialization

### Key Features

✅ **Optimistic Concurrency Control** - `rowversion` column  
✅ **Soft Delete** - Data preservation with `IsDeleted` flag  
✅ **Automatic Auditing** - CreatedAt, ModifiedAt tracking  
✅ **Retry Strategy** - Transient failure handling  
✅ **Database Seeding** - Automatic roles and admin user creation

---

## 🔧 Database Setup

### Connection String

**Development** (`appsettings.Development.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ArchuDb;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

**Production** (`appsettings.Production.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=ArchuDb;User Id=your-user;Password=your-password;TrustServerCertificate=true;"
  }
}
```

### Aspire Configuration

**AppHost** (`Program.cs`):
```csharp
var sqlServer = builder.AddSqlServer("sql")
    .WithDataVolume()
    .AddDatabase("archudatabase");

var api = builder.AddProject<Projects.Archu_Api>("api")
    .WithReference(sqlServer);
```

### DbContext Configuration

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
});
```

---

## 🌱 Database Seeding

### What is Seeded

Automatic initialization creates:
1. **5 System Roles**:
   - Guest
   - User
   - Manager
   - Administrator
   - SuperAdmin

2. **1 Admin User** (configurable):
   - Email: admin@archu.com (default)
   - Password: Admin@123 (default)
   - Roles: SuperAdmin, Administrator, User

### Seeding Configuration

**Enable Seeding** (User Secrets):
```bash
cd src/Archu.Api
dotnet user-secrets set "DatabaseSeeding:Enabled" "true"
dotnet user-secrets set "DatabaseSeeding:SeedRoles" "true"
dotnet user-secrets set "DatabaseSeeding:SeedAdminUser" "true"
dotnet user-secrets set "DatabaseSeeding:AdminEmail" "admin@archu.com"
dotnet user-secrets set "DatabaseSeeding:AdminPassword" "Admin@123"
dotnet user-secrets set "DatabaseSeeding:AdminRoles" "SuperAdmin,Administrator,User"
```

### Seeding Implementation

**DatabaseSeeder.cs**:
```csharp
public class DatabaseSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly DatabaseSeederOptions _options;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return;

        if (_options.SeedRoles)
            await SeedRolesAsync(cancellationToken);

        if (_options.SeedAdminUser)
            await SeedAdminUserAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        var roles = new[] { "Guest", "User", "Manager", "Administrator", "SuperAdmin" };
        
        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        var adminUser = await _userManager.FindByEmailAsync(_options.AdminEmail);
        
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = _options.AdminEmail,
                EmailConfirmed = true
            };

            await _userManager.CreateAsync(adminUser, _options.AdminPassword);
        }

        // Assign roles
        foreach (var roleName in _options.AdminRoles)
        {
            if (!await _userManager.IsInRoleAsync(adminUser, roleName))
            {
                await _userManager.AddToRoleAsync(adminUser, roleName);
            }
        }
    }
}
```

### Seeding Verification

**Check Logs**:
```
info: Starting database seeding process...
info: Seeding roles...
info: Seeding admin user...
info: Database seeding completed successfully
```

**Verify in Database**:
```sql
-- Check roles
SELECT * FROM AspNetRoles;

-- Check admin user
SELECT * FROM AspNetUsers WHERE Email = 'admin@archu.com';

-- Check role assignments
SELECT u.Email, r.Name
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'admin@archu.com';
```

---

## 🔄 Migrations

### Creating Migrations

```bash
cd src/Archu.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../Archu.Api
```

#### Regenerating Permission Tables

When resetting the database and removing prior migrations, recreate the permissions schema and tables with:

```bash
dotnet ef migrations add AddPermissionsSchema --project src/Archu.Infrastructure --startup-project src/Archu.Api --output-dir Persistence/Migrations --context ApplicationDbContext

dotnet ef database update --project src/Archu.Infrastructure --startup-project src/Archu.Api --context ApplicationDbContext
```

The migration will create the `Permissions`, `RolePermissions`, and `UserPermissions` tables with their configured keys,
unique constraints, and foreign keys. Run the update command against your development database after deleting any
previous databases to ensure the initialization workflow can seed the new tables successfully.

### Applying Migrations

**Development** (automatic on startup):
```csharp
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}
```

**Production** (manual or CI/CD):
```bash
dotnet ef database update --startup-project ../Archu.Api
```

### Removing Migrations

```bash
dotnet ef migrations remove --startup-project ../Archu.Api
```

### Migration Best Practices

✅ **DO**:
- Create descriptive migration names
- Review migration code before applying
- Test migrations on staging first
- Keep migrations small and focused
- Back up database before applying

❌ **DON'T**:
- Modify existing migrations after deployment
- Delete migrations that have been applied
- Skip migration testing

---

## 🔁 Retry Strategy

### Configuration

Archu implements automatic retry for transient SQL Server failures:

```csharp
services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        connectionString,
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
});
```

### Retry Parameters

| Parameter | Value | Description |
|-----------|-------|-------------|
| `maxRetryCount` | 5 | Maximum retry attempts |
| `maxRetryDelay` | 30 seconds | Maximum delay between retries |
| `errorNumbersToAdd` | null | Use default transient errors |

### Transient Errors Handled

- Network connectivity issues
- Timeout errors
- Deadlock errors
- Database unavailable
- Connection pool exhaustion

### Retry Behavior

```
Attempt 1 → Fail (transient error)
  ↓ Wait 1s
Attempt 2 → Fail
  ↓ Wait 2s (exponential backoff)
Attempt 3 → Fail
  ↓ Wait 4s
Attempt 4 → Fail
  ↓ Wait 8s
Attempt 5 → Success ✅
```

---

## 🔐 Concurrency Control

### Overview

Archu implements **optimistic concurrency control** to prevent lost updates when multiple users modify the same data simultaneously.

| Feature | Implementation | Benefit |
|---------|----------------|---------|
| **Concurrency Token** | SQL Server `rowversion` | Automatic version tracking |
| **Conflict Detection** | EF Core + Application layer | Two-level validation |
| **Error Handling** | 409 Conflict response | User-friendly feedback |

### How It Works

All entities inherit from `BaseEntity` which includes a `RowVersion` property:

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    
    [Timestamp]  // EF Core concurrency token
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // Other properties...
}
```

SQL Server automatically updates `RowVersion` on every INSERT or UPDATE.

### Client-Side Concurrency Flow

```
1. Client GETs product → RowVersion: v1
2. Client edits locally
3. Client PUTs update with RowVersion: v1
4. Server checks: Is database still v1?
   ✅ YES → Save succeeds, return v2
   ❌ NO  → Return 409 Conflict
```

### Two-Level Validation

**Level 1: Early Validation (Application Layer)**
```csharp
if (!product.RowVersion.SequenceEqual(request.RowVersion))
{
    return Result<ProductDto>.Failure(
        "The product was modified by another user. Please refresh and try again.");
}
```

**Level 2: Database Validation (EF Core)**
```csharp
Context.Entry(product).Property(e => e.RowVersion).OriginalValue = request.RowVersion;
await SaveChangesAsync();  // Throws DbUpdateConcurrencyException if conflict
```

### Repository Implementation

```csharp
public class ProductRepository : BaseRepository<Product>
{
    public Task UpdateAsync(Product product, byte[] originalRowVersion, CancellationToken ct = default)
    {
        // Set original RowVersion for concurrency detection
        SetOriginalRowVersion(product, originalRowVersion);
        DbSet.Update(product);
        return Task.CompletedTask;
    }
}
```

### DTOs and Requests

**Always include RowVersion in DTOs:**
```csharp
public sealed class ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}

public sealed class UpdateProductRequest
{
    [Required] public Guid Id { get; init; }
    [Required] public string Name { get; init; } = string.Empty;
    [Required] public decimal Price { get; init; }
    [Required, MinLength(1)] public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
```

### Error Handling

```csharp
try
{
    await _unitOfWork.Products.UpdateAsync(product, request.RowVersion, ct);
    await _unitOfWork.SaveChangesAsync(ct);
}
catch (DbUpdateConcurrencyException)
{
    return Result<ProductDto>.Failure(
        "The product was modified by another user. Please refresh and try again.");
}
```

---

## 🗑️ Soft Delete

### Overview

Instead of physically deleting records, Archu marks them as deleted while preserving the data for audit history.

### Implementation

All entities inherit `ISoftDeletable`:

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
    string? DeletedBy { get; set; }
}
```

### Global Query Filter

DbContext automatically excludes soft-deleted records from all queries:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var filter = Expression.Lambda(Expression.Not(property), parameter);
            
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }
}
```

### Soft Delete Transform

When you call `DbSet.Remove()`, the DbContext transforms it to an UPDATE:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    ApplyAuditing();
    ApplySoftDeleteTransform();
    return await base.SaveChangesAsync(ct);
}

private void ApplySoftDeleteTransform()
{
    var now = _time.UtcNow;
    var user = _currentUser.UserId;

    foreach (var entry in ChangeTracker.Entries<ISoftDeletable>()
                 .Where(e => e.State == EntityState.Deleted))
    {
        entry.State = EntityState.Modified;
        entry.Entity.IsDeleted = true;
        entry.Entity.DeletedAtUtc = now;
        entry.Entity.DeletedBy = user;
    }
}
```

### Querying Deleted Records

To include soft-deleted records (e.g., for admin views):

```csharp
var allProducts = await _context.Products
    .IgnoreQueryFilters()
    .ToListAsync();
```

---

## 📝 Automatic Audit Tracking

### Overview

All entities automatically track creation, modification, and deletion timestamps along with the user who performed the action.

### Audit Fields

| Field | Populated On | Value |
|-------|-------------|-------|
| `CreatedAtUtc` | INSERT | Current UTC time |
| `CreatedBy` | INSERT | Current user ID |
| `ModifiedAtUtc` | UPDATE | Current UTC time |
| `ModifiedBy` | UPDATE | Current user ID |
| `DeletedAtUtc` | Soft DELETE | Current UTC time |
| `DeletedBy` | Soft DELETE | Current user ID |

### Implementation

```csharp
private void ApplyAuditing()
{
    var now = _time.UtcNow;
    var user = _currentUser.UserId;

    foreach (var entry in ChangeTracker.Entries<IAuditable>())
    {
        if (entry.State == EntityState.Added)
        {
            entry.Entity.CreatedAtUtc = now;
            entry.Entity.CreatedBy = user;
        }

        if (entry.State == EntityState.Modified)
        {
            entry.Entity.ModifiedAtUtc = now;
            entry.Entity.ModifiedBy = user;
        }
    }
}
```

### Benefits

- ✅ Complete audit trail
- ✅ Automatic (no manual tracking)
- ✅ Consistent across all entities
- ✅ Supports compliance requirements

---

## ✅ Best Practices

### Connection Strings

✅ **DO**:
- Use User Secrets for development
- Use Azure Key Vault for production
- Enable `TrustServerCertificate` only in development
- Use connection pooling (default in EF Core)

❌ **DON'T**:
- Commit connection strings to source control
- Use sa account in production
- Disable SSL/TLS in production

### Database Design

✅ **DO**:
- Use GUIDs for primary keys
- Implement soft delete for important data
- Use optimistic concurrency (`rowversion`)
- Add proper indexes
- Use appropriate data types

❌ **DON'T**:
- Use auto-increment IDs (distributed systems)
- Hard delete important data
- Skip concurrency checks
- Over-index (performance impact)

### Migrations

✅ **DO**:
- Review generated SQL before applying
- Test on staging environment
- Back up production database
- Apply during maintenance windows
- Keep migration scripts in source control

❌ **DON'T**:
- Apply untested migrations to production
- Modify existing migrations
- Skip database backups

### Seeding

✅ **DO**:
- Use seeding for system data only
- Make seeding idempotent (safe to re-run)
- Use User Secrets for credentials
- Disable in production (use migrations)

❌ **DON'T**:
- Seed large amounts of test data
- Use weak credentials
- Enable auto-seeding in production

---

## 📚 Related Documentation

- **[GETTING_STARTED.md](GETTING_STARTED.md)** - Initial setup
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - System architecture
- **[DEVELOPMENT_GUIDE.md](DEVELOPMENT_GUIDE.md)** - Development workflows

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Maintainer**: Archu Development Team
