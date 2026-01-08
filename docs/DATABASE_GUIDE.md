# TentMan Database Guide

Complete guide to database setup, migrations, seeding, and best practices.

---

## 📚 Table of Contents

- [Overview](#overview)
- [Database Setup](#database-setup)
- [Database Seeding](#database-seeding)
- [Migrations](#migrations)
- [Retry Strategy](#retry-strategy)
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
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TentManDb;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

**Production** (`appsettings.Production.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=TentManDb;User Id=your-user;Password=your-password;TrustServerCertificate=true;"
  }
}
```

### Aspire Configuration

**AppHost** (`Program.cs`):
```csharp
var sqlServer = builder.AddSqlServer("sql")
    .WithDataVolume()
    .AddDatabase("tentmandatabase");

var api = builder.AddProject<Projects.TentMan_Api>("api")
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
   - Email: admin@tentman.com (default)
   - Password: Admin@123 (default)
   - Roles: SuperAdmin, Administrator, User

### Seeding Configuration

**Enable Seeding** (User Secrets):
```bash
cd src/TentMan.Api
dotnet user-secrets set "DatabaseSeeding:Enabled" "true"
dotnet user-secrets set "DatabaseSeeding:SeedRoles" "true"
dotnet user-secrets set "DatabaseSeeding:SeedAdminUser" "true"
dotnet user-secrets set "DatabaseSeeding:AdminEmail" "admin@tentman.com"
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
SELECT * FROM AspNetUsers WHERE Email = 'admin@tentman.com';

-- Check role assignments
SELECT u.Email, r.Name
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'admin@tentman.com';
```

---

## 🔄 Migrations

### Creating Migrations

```bash
cd src/TentMan.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../TentMan.Api
```

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
dotnet ef database update --startup-project ../TentMan.Api
```

### Removing Migrations

```bash
dotnet ef migrations remove --startup-project ../TentMan.Api
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

TentMan implements automatic retry for transient SQL Server failures:

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
**Maintainer**: TentMan Development Team
