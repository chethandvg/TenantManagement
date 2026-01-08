# TentMan.AdminApi

Administrative API for managing users, roles, and system initialization in the TentMan application.

## Overview

The Admin API provides endpoints for:
- **System Initialization** - One-time setup of roles and super admin user
- **User Management** - Create, list, and manage application users
- **Role Management** - Create and manage system roles
- **User-Role Assignment** - Assign and remove roles from users

## 🚀 Getting Started

### Prerequisites

1. .NET 9 SDK installed
2. SQL Server (local or Docker)
3. Completed database migrations
4. **JWT Secret configured** (already done in appsettings.Development.json)

### Step 1: Run Database Migrations

Before initializing the system, ensure the database schema is created:

```bash
cd src/TentMan.Infrastructure
dotnet ef database update --startup-project ../../TentMan.AdminApi
```

Or let the application create it automatically on startup if using EF migrations.

### Step 2: Verify JWT Configuration

**✅ Already Configured!** The AdminApi is pre-configured with JWT settings in `appsettings.Development.json`.

If you need to change the JWT secret or understand configuration options, see: **[JWT Configuration Guide](../docs/ADMINAPI_JWT_CONFIGURATION.md)**

**Quick Check:**
```bash
cd TentMan.AdminApi
dotnet run
```

If it starts without errors, JWT is configured correctly!

### Step 3: Initialize the System

**IMPORTANT**: This step must be performed **ONCE** before any other operations.

#### Option A: Using Scalar UI (Recommended for Development)

1. Start the AdminApi:
   ```bash
   cd TentMan.AdminApi
   dotnet run
   ```

2. Open Scalar UI in your browser: `https://localhost:7002/scalar/v1` (or the port shown in console)

3. Navigate to the **Initialization** section

4. Execute the `POST /api/v1/admin/initialization/initialize` endpoint with your super admin credentials:

```json
{
  "userName": "superadmin",
  "email": "admin@yourcompany.com",
  "password": "YourSecurePassword123!"
}
```

#### Option B: Using curl

```bash
curl -X POST https://localhost:7002/api/v1/admin/initialization/initialize \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "superadmin",
    "email": "admin@yourcompany.com",
    "password": "YourSecurePassword123!"
  }' -k
```

#### Option C: Using PowerShell

```powershell
$body = @{
    userName = "superadmin"
    email = "admin@yourcompany.com"
    password = "YourSecurePassword123!"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:7002/api/v1/admin/initialization/initialize" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body `
    -SkipCertificateCheck
```

#### Initialization Response

On success, you'll receive:

```json
{
  "success": true,
  "data": {
    "rolesCreated": true,
    "rolesCount": 5,
    "userCreated": true,
    "userId": "guid-of-created-user",
    "message": "System initialized successfully. Created 5 roles and super admin user 'superadmin'."
  },
  "message": "System initialized successfully. You can now login with the super admin credentials.",
  "timestamp": "2025-01-22T10:00:00Z"
}
```

**What gets created during initialization:**

✅ **5 System Roles:**
- `Guest` - Minimal read-only access
- `User` - Basic application access
- `Manager` - Elevated permissions for team management
- `Administrator` - Full system access
- `SuperAdmin` - Unrestricted access (for system administration)

✅ **1 Super Admin User** with the credentials you provided

✅ **Role Assignment** - SuperAdmin role assigned to the created user

### Step 4: Login and Get Access Token

After initialization, login using the super admin credentials to get a JWT token:

```bash
curl -X POST https://localhost:7001/api/v1/authentication/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@yourcompany.com",
    "password": "YourSecurePassword123!"
  }' -k
```

Response:
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "...",
    "expiresAt": "2025-01-22T10:15:00Z"
  }
}
```

**Store the `accessToken` securely** - you'll need it for all subsequent Admin API calls.

## 📚 API Endpoints

All endpoints (except initialization) require authentication and SuperAdmin or Administrator role.

### Authorization Header

Include the JWT token in all requests:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Initialization

#### POST `/api/v1/admin/initialization/initialize`
- **Auth**: None (Anonymous) - Only works when no users exist
- **Purpose**: Initialize system with roles and super admin
- **Use Once**: Can only be called when the database has no users

### Roles Management

#### GET `/api/v1/admin/roles`
- **Auth**: SuperAdmin, Administrator
- **Purpose**: List all system roles

#### POST `/api/v1/admin/roles`
- **Auth**: SuperAdmin, Administrator
- **Purpose**: Create a new role
- **Body**:
  ```json
  {
    "name": "CustomRole",
    "description": "Description of the role"
  }
  ```

### Users Management

#### GET `/api/v1/admin/users?pageNumber=1&pageSize=10`
- **Auth**: SuperAdmin, Administrator
- **Purpose**: List all users with pagination
- **Query Parameters**:
  - `pageNumber` (optional, default: 1)
  - `pageSize` (optional, default: 10)

#### POST `/api/v1/admin/users`
- **Auth**: SuperAdmin, Administrator
- **Purpose**: Create a new user
- **Body**:
  ```json
  {
    "userName": "johndoe",
    "email": "john@example.com",
    "password": "SecurePassword123!",
    "phoneNumber": "+1234567890",
    "emailConfirmed": false,
    "twoFactorEnabled": false
  }
  ```

### User-Role Assignment

#### POST `/api/v1/admin/userroles/assign`
- **Auth**: SuperAdmin, Administrator
- **Purpose**: Assign a role to a user
- **Body**:
  ```json
  {
    "userId": "user-guid",
    "roleId": "role-guid"
  }
  ```

#### DELETE `/api/v1/admin/userroles/{userId}/roles/{roleId}`
- **Auth**: SuperAdmin, Administrator
- **Purpose**: Remove a role from a user

## 🔐 Configuration

### JWT Settings (Already Configured)

The AdminApi shares JWT configuration with the Main API for token compatibility:

```json
{
  "Jwt": {
    "Secret": "ArchuDevelopmentSecretKeyForJwtTokensThisIsAtLeast32CharactersLong!",
    "Issuer": "https://localhost:7001",
    "Audience": "https://localhost:7001",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

**For production or alternative configuration options**, see: **[JWT Configuration Guide](../docs/ADMINAPI_JWT_CONFIGURATION.md)**

## 🔒 Security Best Practices

### 1. Protect Super Admin Credentials

- **Never commit** super admin credentials to source control
- **Store securely** in a password manager
- **Rotate regularly** - Change the password periodically
- **Use strong passwords** - Minimum 12 characters with complexity

### 2. JWT Secret Management

- ✅ **Development**: Use appsettings.Development.json (already configured)
- ✅ **Production**: Use Azure Key Vault, AWS Secrets Manager, or environment variables
- ⚠️ **Never commit production secrets** to source control

See **[JWT Configuration Guide](../docs/ADMINAPI_JWT_CONFIGURATION.md)** for detailed options.

### 3. Restrict Admin API Access

In production, consider:

```csharp
// Add IP whitelisting
builder.Services.AddControllers(options =>
{
    options.Filters.Add(new IpWhitelistFilter(new[] 
    { 
        "192.168.1.0/24",  // Internal network
        "10.0.0.0/8"       // VPN range
    }));
});

// Or use API Gateway with IP restrictions
```

### 4. Audit All Admin Actions

All admin operations are logged. Monitor logs for:
- User creation/deletion
- Role assignments
- Failed authorization attempts

### 5. Limit SuperAdmin Accounts

- Only create SuperAdmin accounts when absolutely necessary
- Most admin tasks can be done with the Administrator role
- Consider using temporary elevated access instead of permanent SuperAdmin

### 6. Disable Initialization Endpoint in Production

After initial setup, consider removing or securing the initialization endpoint:

```csharp
// In Program.cs
if (app.Environment.IsProduction())
{
    // Map only non-initialization controllers
    app.MapControllers();
}
```

## 🎯 Common Workflows

### Creating Additional Administrators

1. **Login as SuperAdmin**
2. **Create the user** via POST `/api/v1/admin/users`
3. **Get the Administrator role ID** via GET `/api/v1/admin/roles`
4. **Assign the role** via POST `/api/v1/admin/userroles/assign`

Example:

```bash
# 1. Login (get token)
TOKEN=$(curl -X POST https://localhost:7001/api/v1/authentication/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@yourcompany.com","password":"YourSecurePassword123!"}' -k \
  | jq -r '.data.accessToken')

# 2. Create user
USER_RESPONSE=$(curl -X POST https://localhost:7002/api/v1/admin/users \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "newadmin",
    "email": "newadmin@yourcompany.com",
    "password": "SecurePassword123!",
    "emailConfirmed": true
  }' -k)

USER_ID=$(echo $USER_RESPONSE | jq -r '.data.id')

# 3. Get Administrator role ID
ROLES=$(curl -X GET https://localhost:7002/api/v1/admin/roles \
  -H "Authorization: Bearer $TOKEN" -k)

ADMIN_ROLE_ID=$(echo $ROLES | jq -r '.data[] | select(.name=="Administrator") | .id')

# 4. Assign role
curl -X POST https://localhost:7002/api/v1/admin/userroles/assign \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"userId\": \"$USER_ID\",
    \"roleId\": \"$ADMIN_ROLE_ID\"
  }" -k
```

### Setting Up Standard Users

For regular application users, assign them to the `User` role instead of `Administrator`.

## 🏗️ Architecture

The Admin API follows Clean Architecture principles:

```
AdminApi (Presentation)
    ↓
Application Layer (CQRS Commands/Queries)
    ↓
Infrastructure Layer (Repositories, DbContext)
    ↓
Domain Layer (Entities)
```

### Project Structure

```
TentMan.AdminApi/
├── Controllers/
│   ├── InitializationController.cs    # System setup
│   ├── RolesController.cs             # Role management
│   ├── UsersController.cs             # User management
│   └── UserRolesController.cs         # Role assignment
├── Middleware/
│   └── GlobalExceptionHandlerMiddleware.cs
└── Program.cs
```

### Related Projects

- `TentMan.Contracts/Admin/` - DTOs and request models
- `TentMan.Application/Admin/` - CQRS commands and queries
- `TentMan.Infrastructure/` - Repository implementations
- `TentMan.Domain/Entities/Identity/` - User and role entities

## 🔧 Configuration Files

### appsettings.json

```json
{
  "ConnectionStrings": {
    "archudb": "",
    "Sql": ""
  },
  "Jwt": {
    "Issuer": "https://localhost:7002",
    "Audience": "https://localhost:7002",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Environment Variables (Production)

```bash
export ConnectionStrings__archudb="Server=prod-db;Database=ArchuDb;..."
export Jwt__Secret="your-production-secret-here"
```

## 🚨 Troubleshooting

### "System is already initialized"

**Problem**: Initialization endpoint returns error saying users already exist.

**Solution**: The system is already initialized. Use the super admin credentials to login.

### "JWT Secret is not configured"

**Problem**: Application fails to start with JWT configuration error.

**Solution**: See **[JWT Configuration Guide](../docs/ADMINAPI_JWT_CONFIGURATION.md)** for setup options.

The error should already be fixed as JWT secret is in `appsettings.Development.json`.

### "Unauthorized" on Admin Endpoints

**Problem**: 401 Unauthorized error when calling admin endpoints.

**Solutions**:
1. Ensure you're including the JWT token in the Authorization header
2. Verify the token hasn't expired (15 minutes default)
3. Refresh the token if needed

### "Forbidden" on Admin Endpoints

**Problem**: 403 Forbidden error when calling admin endpoints.

**Solution**: The logged-in user doesn't have SuperAdmin or Administrator role. Verify role assignment.

### Cannot Connect to Database

**Problem**: Connection string errors or cannot reach SQL Server.

**Solutions**:
1. Verify SQL Server is running
2. Check connection string in appsettings.json
3. Ensure firewall allows connections
4. Verify database exists (run migrations)

## 📖 Additional Resources

- **[JWT Configuration Guide](../docs/ADMINAPI_JWT_CONFIGURATION.md)** - Detailed JWT setup options
- [Clean Architecture Guide](../docs/ARCHITECTURE.md)
- [JWT Authentication Guide](../docs/JWT_CONFIGURATION_GUIDE.md)
- [API Documentation](https://localhost:7002/scalar/v1) (when running)

## 🤝 Contributing

Follow the existing patterns when adding new admin endpoints:
1. Create DTOs in `TentMan.Contracts/Admin/`
2. Create commands/queries in `TentMan.Application/Admin/`
3. Create controller endpoints in `TentMan.AdminApi/Controllers/`
4. Require SuperAdmin or Administrator role authorization

---

**Last Updated**: 2025-01-22  
**Version**: 1.1  
**Maintainer**: TentMan Development Team
