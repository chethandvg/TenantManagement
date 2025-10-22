# Database Seeding Guide

This guide explains how to configure and use the database seeding feature to initialize roles and create an admin user in the Archu application.

---

## Table of Contents

1. [Overview](#overview)
2. [Quick Start](#quick-start)
3. [Configuration Options](#configuration-options)
4. [Security Best Practices](#security-best-practices)
5. [Seeded Data](#seeded-data)
6. [Customization](#customization)
7. [Troubleshooting](#troubleshooting)

---

## Overview

The database seeder automatically creates:
- ✅ **System Roles**: Guest, User, Manager, Administrator, SuperAdmin
- ✅ **Admin User**: A default admin account with full system access
- ✅ **Role Assignments**: Assigns specified roles to the admin user

**When It Runs:**
- Automatically on application startup
- Only creates data that doesn't already exist (idempotent)
- Can be disabled per environment

---

## Quick Start

### Option 1: Use Setup Scripts (Recommended)

**Windows (PowerShell):**
```powershell
cd src/Archu.Api
../../scripts/setup-database-seeding.ps1
```

**Linux/macOS:**
```bash
cd src/Archu.Api
chmod +x ../../scripts/setup-database-seeding.sh
../../scripts/setup-database-seeding.sh
```

The script will:
1. Initialize User Secrets (if needed)
2. Prompt for admin credentials
3. Configure seeding settings securely
4. Save everything outside source control

### Option 2: Manual Configuration

#### Step 1: Configure via User Secrets

```bash
cd src/Archu.Api
dotnet user-secrets init

# Basic configuration
dotnet user-secrets set "DatabaseSeeding:Enabled" "true"
dotnet user-secrets set "DatabaseSeeding:SeedRoles" "true"
dotnet user-secrets set "DatabaseSeeding:SeedAdminUser" "true"

# Admin credentials (CHANGE THESE!)
dotnet user-secrets set "DatabaseSeeding:AdminEmail" "admin@yourcompany.com"
dotnet user-secrets set "DatabaseSeeding:AdminUserName" "admin"
dotnet user-secrets set "DatabaseSeeding:AdminPassword" "YourSecurePassword@123"

# Admin roles
dotnet user-secrets set "DatabaseSeeding:AdminRoles:0" "SuperAdmin"
dotnet user-secrets set "DatabaseSeeding:AdminRoles:1" "Administrator"
```

#### Step 2: Run the Application

```bash
dotnet run
```

The database will be seeded automatically on startup.

---

## Configuration Options

### DatabaseSeederOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `true` (Dev), `false` (Prod) | Enable/disable seeding |
| `SeedRoles` | `bool` | `true` | Seed system roles |
| `SeedAdminUser` | `bool` | `true` | Seed admin user |
| `AdminEmail` | `string` | `admin@archu.com` | Admin email address |
| `AdminUserName` | `string` | `admin` | Admin username |
| `AdminPassword` | `string` | `Admin@123` | Admin password |
| `AdminRoles` | `string[]` | `["SuperAdmin", "Administrator"]` | Roles to assign |

### Configuration Locations

#### Development (appsettings.Development.json)
```json
{
  "DatabaseSeeding": {
    "Enabled": true,
    "SeedRoles": true,
    "SeedAdminUser": true,
    "AdminEmail": "admin@archu.com",
    "AdminUserName": "admin",
    "AdminPassword": "Admin@123",
    "AdminRoles": ["SuperAdmin", "Administrator"]
  }
}
```

#### Staging (appsettings.Staging.json)
```json
{
  "DatabaseSeeding": {
    "Enabled": false,
    "Comment": "Seeding disabled in staging. Use migration scripts or manual seeding."
  }
}
```

#### Production (appsettings.Production.json)
```json
{
  "DatabaseSeeding": {
    "Enabled": false,
    "Comment": "Seeding disabled in production for security. Initialize data through secure deployment pipelines."
  }
}
```

---

## Security Best Practices

### ⚠️ Never Commit Secrets to Source Control

**DON'T DO THIS:**
```json
{
  "DatabaseSeeding": {
    "AdminPassword": "MyPassword123!"  // ❌ NEVER commit passwords to Git
  }
}
```

### ✅ Use User Secrets for Development

**Windows:**
```powershell
dotnet user-secrets set "DatabaseSeeding:AdminPassword" "YourSecurePassword@123"
```

**Linux/macOS:**
```bash
dotnet user-secrets set "DatabaseSeeding:AdminPassword" "YourSecurePassword@123"
```

Secrets are stored at:
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`
- **Linux/macOS**: `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`

### ✅ Use Environment Variables for Production

**Azure App Service:**
```bash
az webapp config appsettings set --name archu-api --resource-group archu-rg \
  --settings DatabaseSeeding__AdminPassword="YourSecurePassword@123"
```

**Docker:**
```yaml
environment:
  - DatabaseSeeding__AdminPassword=YourSecurePassword@123
```

### ✅ Use Azure Key Vault for Production

See [JWT Configuration Guide](JWT_CONFIGURATION_GUIDE.md#option-3-azure-key-vault-production) for Azure Key Vault setup.

### Password Requirements

✅ **Minimum Requirements:**
- At least 8 characters
- At least one uppercase letter (A-Z)
- At least one lowercase letter (a-z)
- At least one number (0-9)
- At least one special character (!@#$%^&*)

✅ **Recommended:**
- 12+ characters
- Mix of all character types
- Unique (not reused from other systems)
- Changed regularly

---

## Seeded Data

### System Roles

The seeder creates the following roles:

| Role | Description | Use Case |
|------|-------------|----------|
| **Guest** | Minimal read-only access | Public/unauthenticated users |
| **User** | Basic application access | Standard authenticated users |
| **Manager** | Elevated permissions for team management | Team leads, department managers |
| **Administrator** | Full system access | System administrators |
| **SuperAdmin** | Unrestricted access | Root-level administrators |

These roles are defined in `Archu.Domain.Constants.RoleNames`.

### Admin User

The seeder creates one admin user with:
- ✅ Specified email and username
- ✅ Hashed password (using BCrypt)
- ✅ Email auto-confirmed
- ✅ Lockout disabled
- ✅ Assigned roles (SuperAdmin, Administrator by default)

**Default Credentials (Development):**
- **Email**: `admin@archu.com`
- **Username**: `admin`
- **Password**: `Admin@123`
- **Roles**: SuperAdmin, Administrator

⚠️ **Change these credentials in production!**

---

## Customization

### Adding Custom Roles

To add custom roles to the seeder:

#### Step 1: Define Role in Constants
```csharp
// Archu.Domain/Constants/RoleNames.cs
public static class RoleNames
{
    public const string CustomRole = "CustomRole";
    
    public static readonly string[] All = 
    [
        Guest,
        User,
        Manager,
        Administrator,
        SuperAdmin,
        CustomRole  // Add your role here
    ];
}
```

#### Step 2: Add to Seeder
```csharp
// Archu.Infrastructure/Persistence/Seeding/DatabaseSeeder.cs
private async Task SeedRolesAsync(CancellationToken cancellationToken)
{
    var rolesToSeed = new[]
    {
        // ...existing roles...
        new { Name = RoleNames.CustomRole, Description = "Custom role description" }
    };
    
    // ...rest of seeding logic...
}
```

### Seeding Additional Data

To seed additional data (e.g., default categories, settings):

#### Step 1: Add Method to DatabaseSeeder
```csharp
// Archu.Infrastructure/Persistence/Seeding/DatabaseSeeder.cs
public async Task SeedAsync(CancellationToken cancellationToken = default)
{
    // ...existing seeding...
    
    // Add custom seeding
    await SeedDefaultCategoriesAsync(cancellationToken);
}

private async Task SeedDefaultCategoriesAsync(CancellationToken cancellationToken)
{
    var categories = new[] { "Electronics", "Books", "Clothing" };
    
    foreach (var categoryName in categories)
    {
        var exists = await _context.Categories
            .AnyAsync(c => c.Name == categoryName, cancellationToken);
            
        if (!exists)
        {
            await _context.Categories.AddAsync(
                new Category { Name = categoryName },
                cancellationToken);
        }
    }
    
    await _context.SaveChangesAsync(cancellationToken);
}
```

### Conditional Seeding

To seed different data based on environment:

```csharp
public async Task SeedAsync(CancellationToken cancellationToken = default)
{
    if (_environment.IsDevelopment())
    {
        await SeedDevelopmentDataAsync(cancellationToken);
    }
    else if (_environment.IsStaging())
    {
        await SeedStagingDataAsync(cancellationToken);
    }
    // Production seeding is typically disabled
}
```

---

## Troubleshooting

### Seeding Not Running

**Check 1: Is seeding enabled?**
```bash
dotnet user-secrets list | grep DatabaseSeeding:Enabled
```

Should show: `DatabaseSeeding:Enabled = true`

**Check 2: Check logs**
Look for these log messages on startup:
```
info: Starting database seeding process...
info: Seeding roles...
info: Seeding admin user...
info: Database seeding completed successfully
```

**Check 3: Verify configuration**
```bash
dotnet user-secrets list
```

### Admin User Already Exists

If the admin user already exists, the seeder will:
1. Skip creating a new user
2. Check if user has all required roles
3. Add any missing roles

To reset the admin user:
```sql
-- Delete existing admin user (BE CAREFUL!)
DELETE FROM UserRoles WHERE UserId IN (SELECT Id FROM Users WHERE Email = 'admin@archu.com');
DELETE FROM Users WHERE Email = 'admin@archu.com';

-- Then restart the application to re-seed
```

### Password Validation Errors

**Error**: "Admin password must be at least 8 characters long"

**Fix**: Update the password in User Secrets:
```bash
dotnet user-secrets set "DatabaseSeeding:AdminPassword" "YourStrongerPassword@123"
```

### Roles Not Found

**Error**: "Cannot assign roles. The following roles do not exist: SuperAdmin"

**Cause**: Roles seeding is disabled or failed.

**Fix**: Enable role seeding:
```bash
dotnet user-secrets set "DatabaseSeeding:SeedRoles" "true"
```

### Seeding Fails in Production

**Error**: Various seeding errors in production

**Cause**: Seeding is typically disabled in production for security.

**Fix**: For production, use one of these approaches:

#### Option 1: Migration-Based Seeding
Create an EF Core migration with seed data:
```bash
dotnet ef migrations add SeedInitialData
```

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.InsertData(
        table: "Roles",
        columns: new[] { "Id", "Name", "NormalizedName", "Description" },
        values: new object[] { Guid.NewGuid(), "Administrator", "ADMINISTRATOR", "Administrator role" });
}
```

#### Option 2: Manual Seeding Script
Create a separate seeding executable:
```bash
dotnet run --project src/Archu.Infrastructure.Seeding
```

#### Option 3: Azure DevOps / GitHub Actions
Run seeding as part of deployment pipeline:
```yaml
- name: Seed Database
  run: |
    dotnet run --project src/Archu.Api -- seed-database
  env:
    DatabaseSeeding__Enabled: true
    DatabaseSeeding__AdminPassword: ${{ secrets.ADMIN_PASSWORD }}
```

---

## Environment-Specific Configuration

### Development

**Goal**: Easy local development with default credentials

```json
{
  "DatabaseSeeding": {
    "Enabled": true,
    "AdminEmail": "admin@archu.com",
    "AdminPassword": "Admin@123"
  }
}
```

### Staging

**Goal**: Test deployment without auto-seeding

```json
{
  "DatabaseSeeding": {
    "Enabled": false
  }
}
```

Seed manually or through deployment pipeline.

### Production

**Goal**: Maximum security, no auto-seeding

```json
{
  "DatabaseSeeding": {
    "Enabled": false
  }
}
```

Use migration scripts or secure deployment pipelines.

---

## Related Documentation

- 📖 [JWT Configuration Guide](JWT_CONFIGURATION_GUIDE.md) - Secure secret management
- 📖 [Architecture Guide](ARCHITECTURE.md) - System architecture
- 📖 [Authentication Infrastructure](../src/Archu.Infrastructure/Authentication/README.md) - Authentication system

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Maintainer**: Archu Development Team
