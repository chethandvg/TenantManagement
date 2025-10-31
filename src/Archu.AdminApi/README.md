# Archu Admin API

Administrative API for managing users, roles, and system configuration in the Archu application.

## Overview

The Admin API provides secure endpoints for system administration tasks:

- **🚀 System Initialization** - Bootstrap system with roles and super admin
- **👥 User Management** - Create, read, update, and delete users
- **🎭 Role Management** - Manage system roles and permissions
- **🔗 Role Assignment** - Assign and remove roles from users with security restrictions
- **🔐 JWT Authentication** - Secure token-based authentication
- **🛡️ Security Controls** - Privilege escalation protection and role hierarchy enforcement

## Getting Started

### Prerequisites

- .NET 9 SDK
- SQL Server (local or Docker)
- Completed database migrations
- JWT secret configured (shared with Archu.Api)

### Quick Start

1. **Run Database Migrations**

   ```bash
   cd src/Archu.Infrastructure
   dotnet ef database update --startup-project ../Archu.AdminApi
   ```

2. **Verify JWT Configuration**

   Check `appsettings.Development.json` (already configured):
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

3. **Start the Admin API**

   ```bash
   cd src/Archu.AdminApi
   dotnet run
   ```

4. **Initialize the System**

   Use Scalar UI at `https://localhost:7290/scalar/v1` or HTTP request:

   ```bash
   POST /api/v1/admin/initialization/initialize
   Content-Type: application/json

   {
     "userName": "superadmin",
     "email": "admin@yourcompany.com",
     "password": "YourSecurePassword123!"
   }
   ```

   **What gets created:**
   - ✅ 5 System Roles (Guest, User, Manager, Administrator, SuperAdmin)
   - ✅ Super admin user with provided credentials
   - ✅ SuperAdmin role assigned to the user

5. **Get JWT Token**

   Login via Archu.Api to get access token:

   ```bash
   POST https://localhost:7123/api/v1/authentication/login
   Content-Type: application/json

   {
     "email": "admin@yourcompany.com",
     "password": "YourSecurePassword123!"
   }
   ```

   Use the returned `accessToken` for all Admin API requests.

## API Endpoints

All endpoints (except initialization) require JWT authentication.

### Initialization

| Method | Endpoint | Auth Required | Description |
|--------|----------|---------------|-------------|
| POST | `/api/v1/admin/initialization/initialize` | ❌ No | Initialize system (one-time only) |

### User Management

| Method | Endpoint | Required Role | Description |
|--------|----------|---------------|-------------|
| GET | `/api/v1/admin/users` | Admin+ | List all users (paginated) |
| POST | `/api/v1/admin/users` | Admin+ | Create new user |
| DELETE | `/api/v1/admin/users/{id}` | SuperAdmin+ | Delete user |

### Role Management

| Method | Endpoint | Required Role | Description |
|--------|----------|---------------|-------------|
| GET | `/api/v1/admin/roles` | Admin+ | List all roles |
| POST | `/api/v1/admin/roles` | Admin+ | Create custom role |

### User-Role Assignment

| Method | Endpoint | Required Role | Description |
|--------|----------|---------------|-------------|
| POST | `/api/v1/admin/userroles/assign` | Admin+ | Assign role to user |
| DELETE | `/api/v1/admin/userroles/{userId}/roles/{roleId}` | Admin+ | Remove role from user |

### Health Monitoring

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Full health status |
| GET | `/health/ready` | Readiness probe |
| GET | `/health/live` | Liveness probe |

## Authentication

Include JWT token in all requests (except initialization):

```bash
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## Role Hierarchy & Security

### Role Levels

| Role | Level | Permissions |
|------|-------|-------------|
| **SuperAdmin** | 5 | Full system access, can assign any role |
| **Administrator** | 4 | Can manage users, assign User/Manager/Guest roles only |
| **Manager** | 3 | Read-only access to users and roles |
| **User** | 2 | Standard application access |
| **Guest** | 1 | Read-only access |

### Security Restrictions

1. **Privilege Escalation Protection**
   - SuperAdmin role can only be assigned by another SuperAdmin
   - Administrator role can only be assigned by SuperAdmin
   - Cannot elevate your own privileges

2. **Self-Modification Protection**
   - Cannot delete yourself
   - Cannot remove your own privileged roles (SuperAdmin/Administrator)

3. **Last SuperAdmin Protection**
   - Cannot delete the last SuperAdmin in the system
   - Ensures system always has at least one administrator

## Common Workflows

### Creating Additional Administrators

```bash
# 1. Login as SuperAdmin (get token from Archu.Api)
POST https://localhost:7123/api/v1/authentication/login

# 2. Create user
POST /api/v1/admin/users
Authorization: Bearer <token>

{
  "userName": "newadmin",
  "email": "newadmin@company.com",
  "password": "SecurePassword123!",
  "emailConfirmed": true
}

# 3. Get Administrator role ID
GET /api/v1/admin/roles
Authorization: Bearer <token>

# 4. Assign Administrator role
POST /api/v1/admin/userroles/assign
Authorization: Bearer <token>

{
  "userId": "<user-id-from-step-2>",
  "roleId": "<admin-role-id-from-step-3>"
}
```

### Creating Standard Users

Same workflow as above, but assign "User" role instead of "Administrator".

## Architecture

Follows Clean Architecture with CQRS pattern:

```
Archu.AdminApi (Presentation)
    ↓ Controllers
Archu.Application (Business Logic)
    ↓ CQRS Commands/Queries
Archu.Infrastructure (Data Access)
    ↓ Repositories, DbContext
Archu.Domain (Core Entities)
```

### Project Structure

```
Archu.AdminApi/
├── Controllers/
│   ├── InitializationController.cs    # System setup
│   ├── UsersController.cs       # User CRUD
│   ├── RolesController.cs     # Role management
│   └── UserRolesController.cs  # Role assignment
├── Authorization/
│   ├── AdminAuthorizationHandlerExtensions.cs
│   ├── AdminAuthorizationPolicyExtensions.cs
│   ├── AdminPolicyNames.cs
│   ├── Requirements/
│   │   └── AdminRoleRequirement.cs
│   └── Handlers/
│       └── AdminRoleRequirementHandler.cs
├── Middleware/
│   └── GlobalExceptionHandlerMiddleware.cs
├── Program.cs
├── appsettings.json
├── Archu.AdminApi.http         # HTTP request examples
└── README.md    # This file
```

## Testing the API

### Using Scalar UI

1. Navigate to `https://localhost:7290/scalar/v1`
2. Click "Authorize" and paste JWT token
3. Test endpoints interactively

### Using HTTP Files

The repository includes `Archu.AdminApi.http` with example requests:

```bash
# Open in Visual Studio
# File location: src/Archu.AdminApi/Archu.AdminApi.http
# Execute requests using "Send Request"
```

## Configuration

### JWT Settings

Located in `appsettings.json` (shared with Archu.Api):

```json
{
  "Jwt": {
    "Secret": "your-secret-key-at-least-32-characters",
    "Issuer": "https://localhost:7001",
    "Audience": "https://localhost:7001"
  }
}
```

**Production:** Use Azure Key Vault or environment variables:
```bash
export Jwt__Secret="production-secret-here"
export ConnectionStrings__archudb="production-connection-string"
```

## Security Best Practices

### 1. Protect Super Admin Credentials
- Never commit credentials to source control
- Store in password manager
- Use strong passwords (minimum 12 characters)
- Rotate passwords regularly

### 2. Limit SuperAdmin Accounts
- Only create when absolutely necessary
- Most admin tasks can be done with Administrator role
- Consider temporary elevated access patterns

### 3. Production Deployment
- Use HTTPS only
- Store secrets in Azure Key Vault
- Implement IP whitelisting
- Enable audit logging
- Monitor failed authorization attempts

### 4. Disable Initialization in Production

After setup, secure or disable the initialization endpoint:

```csharp
// In Program.cs
if (app.Environment.IsProduction())
{
    // Don't map initialization controller
}
```

## Troubleshooting

### "System is already initialized"

**Cause:** Initialization endpoint already called (users exist in database).

**Solution:** Use super admin credentials to login. Initialization is one-time only.

### "JWT Secret is not configured"

**Cause:** Missing JWT secret in configuration.

**Solution:** Verify `appsettings.Development.json` has JWT secret configured (should be preset).

### "401 Unauthorized"

**Solutions:**
1. Include JWT token in Authorization header
2. Verify token hasn't expired (15 minutes default)
3. Get new token via Archu.Api login endpoint

### "403 Forbidden"

**Cause:** User lacks required role (SuperAdmin or Administrator).

**Solution:** Verify role assignment via `/api/v1/admin/userroles/{userId}`.

## API Response Format

Standard response wrapper:

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": {
    // Response data
  }
}
```

**Pagination Response:**
```json
{
  "success": true,
  "data": {
    "items": [...],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 50,
    "totalPages": 5
  }
}
```

## Related Projects

- **Archu.Api** - Main API with authentication endpoints
- **Archu.Application** - Business logic and CQRS handlers
- **Archu.Infrastructure** - Data access and repository implementations
- **Archu.Domain** - Core entities (User, Role, UserRole)
- **Archu.Contracts** - DTOs and request/response models

## Additional Resources

- **Scalar UI**: `https://localhost:7290/scalar/v1` (when running)
- **OpenAPI Spec**: `https://localhost:7290/openapi/v1.json`
- **HTTP Examples**: `Archu.AdminApi.http` (30+ requests)
- **GitHub**: [https://github.com/chethandvg/archu](https://github.com/chethandvg/archu)

---

**Version**: 1.2  
**Last Updated**: 2025-01-23  
**Target Framework**: .NET 9.0  
**License**: MIT
