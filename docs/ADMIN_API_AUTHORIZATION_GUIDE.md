# Admin API Authorization Guide

## Overview

This guide explains the authorization system implemented for the Archu Admin API. The authorization system uses **role-based access control (RBAC)** to secure administrative endpoints for user, role, and user-role management operations.

---

## Table of Contents

1. [Authorization Architecture](#authorization-architecture)
2. [Role Hierarchy](#role-hierarchy)
3. [Authorization Policies](#authorization-policies)
4. [Permission Matrix](#permission-matrix)
5. [Usage Examples](#usage-examples)
6. [Testing Authorization](#testing-authorization)
7. [Troubleshooting](#troubleshooting)

---

## Authorization Architecture

### Components

The Admin API authorization system consists of the following components:

#### 1. **Policy Names** (`AdminPolicyNames.cs`)
Centralized constants for all authorization policies:
- `AdminPolicyNames.Roles.*` - Role management policies
- `AdminPolicyNames.Users.*` - User management policies
- `AdminPolicyNames.UserRoles.*` - Role assignment policies
- `AdminPolicyNames.RequireAdminAccess` - Base admin access

#### 2. **Requirements** (`AdminRoleRequirement.cs`)
Authorization requirements that define:
- Required roles for operations
- Operation descriptions (for logging)

#### 3. **Handlers** (`AdminRoleRequirementHandler.cs`)
Validates user roles against requirements:
- Checks if user is authenticated
- Verifies user has required roles
- Logs authorization decisions

#### 4. **Policy Configuration** (`AdminAuthorizationPolicyExtensions.cs`)
Configures all policies with their requirements:
- Role management policies
- User management policies
- User-role assignment policies

---

## Role Hierarchy

The Admin API recognizes the following role hierarchy (highest to lowest):

```
SuperAdmin (Highest)
    ↓
Administrator
    ↓
Manager
    ↓
User (Lowest - No admin access)
```

### Role Descriptions

| Role | Description | Admin Access |
|------|-------------|-------------|
| **SuperAdmin** | System administrator with unrestricted access to all operations | ✅ Full |
| **Administrator** | Administrator with full system access except system-critical operations | ✅ Most operations |
| **Manager** | Manager with elevated permissions for viewing and basic management | ✅ View/Read operations |
| **User** | Standard user with no administrative privileges | ❌ No access |

---

## Authorization Policies

### Base Policy

#### `AdminPolicyNames.RequireAdminAccess`
- **Description:** Minimum requirement for accessing any admin endpoint
- **Required Roles:** SuperAdmin, Administrator, Manager
- **Applied To:** All Admin API controllers (base policy)

### Role Management Policies

#### `AdminPolicyNames.Roles.View`
- **Description:** View/read all roles in the system
- **Required Roles:** SuperAdmin, Administrator, Manager
- **Endpoints:** `GET /api/v1/admin/roles`

#### `AdminPolicyNames.Roles.Create`
- **Description:** Create new roles
- **Required Roles:** SuperAdmin, Administrator
- **Endpoints:** `POST /api/v1/admin/roles`

#### `AdminPolicyNames.Roles.Update`
- **Description:** Update existing roles
- **Required Roles:** SuperAdmin, Administrator
- **Endpoints:** `PUT /api/v1/admin/roles/{id}`

#### `AdminPolicyNames.Roles.Delete`
- **Description:** Delete roles (system-critical operation)
- **Required Roles:** SuperAdmin (only)
- **Endpoints:** `DELETE /api/v1/admin/roles/{id}`

#### `AdminPolicyNames.Roles.Manage`
- **Description:** Full role management (CRUD)
- **Required Roles:** SuperAdmin
- **Endpoints:** All role endpoints

### User Management Policies

#### `AdminPolicyNames.Users.View`
- **Description:** View/read all users in the system
- **Required Roles:** SuperAdmin, Administrator, Manager
- **Endpoints:** `GET /api/v1/admin/users`

#### `AdminPolicyNames.Users.Create`
- **Description:** Create new users
- **Required Roles:** SuperAdmin, Administrator, Manager
- **Endpoints:** `POST /api/v1/admin/users`

#### `AdminPolicyNames.Users.Update`
- **Description:** Update existing users
- **Required Roles:** SuperAdmin, Administrator, Manager
- **Endpoints:** `PUT /api/v1/admin/users/{id}`

#### `AdminPolicyNames.Users.Delete`
- **Description:** Delete users
- **Required Roles:** SuperAdmin, Administrator
- **Endpoints:** `DELETE /api/v1/admin/users/{id}`

#### `AdminPolicyNames.Users.Manage`
- **Description:** Full user management (CRUD)
- **Required Roles:** SuperAdmin, Administrator
- **Endpoints:** All user endpoints

### User-Role Assignment Policies

#### `AdminPolicyNames.UserRoles.View`
- **Description:** View roles assigned to users
- **Required Roles:** SuperAdmin, Administrator, Manager
- **Endpoints:** `GET /api/v1/admin/user-roles/{userId}`

#### `AdminPolicyNames.UserRoles.Assign`
- **Description:** Assign roles to users
- **Required Roles:** SuperAdmin, Administrator
- **Endpoints:** `POST /api/v1/admin/user-roles/assign`

#### `AdminPolicyNames.UserRoles.Remove`
- **Description:** Remove roles from users
- **Required Roles:** SuperAdmin, Administrator
- **Endpoints:** `DELETE /api/v1/admin/user-roles/{userId}/roles/{roleId}`

#### `AdminPolicyNames.UserRoles.Manage`
- **Description:** Full user-role management
- **Required Roles:** SuperAdmin
- **Endpoints:** All user-role endpoints

---

## Permission Matrix

### Role Management Endpoints

| Endpoint | SuperAdmin | Administrator | Manager | User |
|----------|-----------|--------------|---------|------|
| `GET /api/v1/admin/roles` | ✅ | ✅ | ✅ | ❌ |
| `POST /api/v1/admin/roles` | ✅ | ✅ | ❌ | ❌ |
| `PUT /api/v1/admin/roles/{id}` | ✅ | ✅ | ❌ | ❌ |
| `DELETE /api/v1/admin/roles/{id}` | ✅ | ❌ | ❌ | ❌ |

### User Management Endpoints

| Endpoint | SuperAdmin | Administrator | Manager | User |
|----------|-----------|--------------|---------|------|
| `GET /api/v1/admin/users` | ✅ | ✅ | ✅ | ❌ |
| `POST /api/v1/admin/users` | ✅ | ✅ | ✅ | ❌ |
| `PUT /api/v1/admin/users/{id}` | ✅ | ✅ | ✅ | ❌ |
| `DELETE /api/v1/admin/users/{id}` | ✅ | ✅ | ❌ | ❌ |

### User-Role Assignment Endpoints

| Endpoint | SuperAdmin | Administrator | Manager | User |
|----------|-----------|--------------|---------|------|
| `GET /api/v1/admin/user-roles/{userId}` | ✅ | ✅ | ✅ | ❌ |
| `POST /api/v1/admin/user-roles/assign` | ✅ | ✅ | ❌ | ❌ |
| `DELETE /api/v1/admin/user-roles/{userId}/roles/{roleId}` | ✅ | ✅ | ❌ | ❌ |

### Legend
- ✅ **Allowed** - User can access the endpoint
- ❌ **Forbidden** - User cannot access the endpoint (403 Forbidden)

---

## Usage Examples

### Example 1: Viewing Roles (Manager)

**Request:**
```http
GET /api/v1/admin/roles HTTP/1.1
Host: localhost:5001
Authorization: Bearer <manager-jwt-token>
```

**Response:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "success": true,
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Manager",
      "normalizedName": "MANAGER",
      "description": "Manager role with elevated permissions"
    }
  ],
  "message": "Roles retrieved successfully",
  "timestamp": "2025-01-22T10:30:00Z"
}
```

### Example 2: Creating a Role (Unauthorized - Manager)

**Request:**
```http
POST /api/v1/admin/roles HTTP/1.1
Host: localhost:5001
Authorization: Bearer <manager-jwt-token>
Content-Type: application/json

{
  "name": "ContentEditor",
  "description": "Can edit content"
}
```

**Response:**
```http
HTTP/1.1 403 Forbidden
Content-Type: application/json

{
  "success": false,
  "message": "User does not have permission to perform this operation",
  "timestamp": "2025-01-22T10:30:00Z"
}
```

### Example 3: Creating a Role (Authorized - Administrator)

**Request:**
```http
POST /api/v1/admin/roles HTTP/1.1
Host: localhost:5001
Authorization: Bearer <administrator-jwt-token>
Content-Type: application/json

{
  "name": "ContentEditor",
  "description": "Can edit content"
}
```

**Response:**
```http
HTTP/1.1 201 Created
Content-Type: application/json

{
  "success": true,
  "data": {
    "id": "7ba85f64-5717-4562-b3fc-2c963f66afa9",
    "name": "ContentEditor",
    "normalizedName": "CONTENTEDITOR",
    "description": "Can edit content"
  },
  "message": "Role created successfully",
  "timestamp": "2025-01-22T10:30:00Z"
}
```

### Example 4: Assigning a Role to User

**Request:**
```http
POST /api/v1/admin/user-roles/assign HTTP/1.1
Host: localhost:5001
Authorization: Bearer <administrator-jwt-token>
Content-Type: application/json

{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "roleId": "7ba85f64-5717-4562-b3fc-2c963f66afa9"
}
```

**Response:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "success": true,
  "data": {},
  "message": "Role assigned successfully",
  "timestamp": "2025-01-22T10:30:00Z"
}
```

---

## Testing Authorization

### Manual Testing Steps

#### 1. Create Test Users

Use the Admin API to create users with different roles:

```bash
# SuperAdmin user
POST /api/v1/admin/users
{
  "userName": "superadmin",
  "email": "superadmin@archu.com",
  "password": "SuperAdmin123!",
  "emailConfirmed": true
}

# Administrator user
POST /api/v1/admin/users
{
  "userName": "admin",
  "email": "admin@archu.com",
  "password": "Admin123!",
  "emailConfirmed": true
}

# Manager user
POST /api/v1/admin/users
{
  "userName": "manager",
  "email": "manager@archu.com",
  "password": "Manager123!",
  "emailConfirmed": true
}
```

#### 2. Assign Roles

```bash
# Assign SuperAdmin role
POST /api/v1/admin/user-roles/assign
{
  "userId": "<superadmin-user-id>",
  "roleId": "<superadmin-role-id>"
}

# Assign Administrator role
POST /api/v1/admin/user-roles/assign
{
  "userId": "<admin-user-id>",
  "roleId": "<administrator-role-id>"
}

# Assign Manager role
POST /api/v1/admin/user-roles/assign
{
  "userId": "<manager-user-id>",
  "roleId": "<manager-role-id>"
}
```

#### 3. Test Each Endpoint

For each test user:
1. Login to get JWT token
2. Try accessing each endpoint with the token
3. Verify response matches the permission matrix

### Using Postman Collection

Import the Admin API collection and:
1. Set environment variables for each user's token
2. Run the collection tests for each user role
3. Verify 200 OK for allowed operations
4. Verify 403 Forbidden for denied operations

---

## Troubleshooting

### Issue: Always Getting 401 Unauthorized

**Cause:** JWT token is missing or invalid.

**Solution:**
1. Ensure you're including the JWT token in the Authorization header
2. Verify the token hasn't expired
3. Check JWT configuration in `appsettings.json`

```http
Authorization: Bearer <your-jwt-token>
```

### Issue: Getting 403 Forbidden for Expected Operations

**Cause:** User doesn't have the required role.

**Solution:**
1. Verify user's roles via `GET /api/v1/admin/user-roles/{userId}`
2. Check the permission matrix to confirm required role
3. Assign the correct role if needed

### Issue: SuperAdmin Getting 403 Forbidden

**Cause:** Authorization handler not correctly checking for SuperAdmin.

**Solution:**
1. Check logs for authorization decisions
2. Verify `ICurrentUser` is correctly identifying roles
3. Ensure JWT contains role claims

### Issue: Authorization Policies Not Working

**Cause:** Authorization services not registered in `Program.cs`.

**Solution:**
Ensure these lines are in `Program.cs`:
```csharp
builder.Services.AddAdminAuthorizationHandlers();
builder.Services.AddAuthorization(options =>
{
    options.ConfigureAdminPolicies();
});
```

### Issue: Logs Show "User not authenticated"

**Cause:** Authentication middleware not properly configured.

**Solution:**
1. Verify `UseAuthentication()` is called before `UseAuthorization()`
2. Check JWT Bearer configuration in Infrastructure layer
3. Ensure JWT secret is configured correctly

---

## Security Best Practices

### 1. **Principle of Least Privilege**
- Assign users the minimum role required for their job function
- Avoid assigning SuperAdmin role unless absolutely necessary
- Regularly audit user roles and remove unnecessary permissions

### 2. **Role Assignment Control**
- Only SuperAdmin and Administrator roles can assign roles
- Log all role assignment and removal operations
- Implement approval workflows for sensitive role assignments

### 3. **JWT Token Security**
- Use strong secret keys (minimum 256 bits)
- Set appropriate token expiration times
- Implement refresh token rotation
- Store tokens securely (never in localStorage)

### 4. **Audit Logging**
- All authorization decisions are logged
- Monitor failed authorization attempts
- Set up alerts for suspicious activity patterns

### 5. **Regular Security Reviews**
- Periodically review role assignments
- Update permission matrix as requirements change
- Document all authorization changes

---

## Configuration

### Customizing Permissions

To customize permissions for your organization, modify the following files:

#### 1. Update Policy Names
Edit `Archu.AdminApi/Authorization/AdminPolicyNames.cs`:
```csharp
public static class AdminPolicyNames
{
    public static class Roles
    {
        public const string View = "Admin.Roles.View";
        // Add custom policies here
    }
}
```

#### 2. Update Policy Configuration
Edit `Archu.AdminApi/Authorization/AdminAuthorizationPolicyExtensions.cs`:
```csharp
private static void ConfigureRolePolicies(AuthorizationOptions options)
{
    options.AddPolicy(AdminPolicyNames.Roles.View, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new AdminRoleRequirement(
            "View Roles",
            RoleNames.SuperAdmin,
            RoleNames.Administrator,
            RoleNames.Manager
            // Add/remove roles as needed
        ));
    });
}
```

#### 3. Apply to Controllers
Update controller attributes:
```csharp
[HttpGet]
[Authorize(Policy = AdminPolicyNames.Roles.View)]
public async Task<ActionResult<ApiResponse<IEnumerable<RoleDto>>>> GetRoles(...)
{
    // Implementation
}
```

---

## Related Documentation

- [Admin API Quick Start](ADMIN_API_QUICK_START.md)
- [Admin API Implementation Summary](ADMIN_API_IMPLEMENTATION_SUMMARY.md)
- [JWT Configuration Guide](JWT_CONFIGURATION_GUIDE.md)
- [Architecture Guide](ARCHITECTURE.md)

---

## Appendix

### Complete Policy List

```csharp
// Base admin access
AdminPolicyNames.RequireAdminAccess

// Role management
AdminPolicyNames.Roles.View
AdminPolicyNames.Roles.Create
AdminPolicyNames.Roles.Update
AdminPolicyNames.Roles.Delete
AdminPolicyNames.Roles.Manage

// User management
AdminPolicyNames.Users.View
AdminPolicyNames.Users.Create
AdminPolicyNames.Users.Update
AdminPolicyNames.Users.Delete
AdminPolicyNames.Users.Manage

// User-role assignment
AdminPolicyNames.UserRoles.View
AdminPolicyNames.UserRoles.Assign
AdminPolicyNames.UserRoles.Remove
AdminPolicyNames.UserRoles.Manage
```

---

**Last Updated:** 2025-01-22  
**Version:** 1.0  
**Maintainer:** Archu Development Team

