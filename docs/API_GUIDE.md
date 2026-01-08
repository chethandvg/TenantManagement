# TentMan API Guide

Complete reference for TentMan's two complementary APIs: **Main API** (TentMan.Api) and **Admin API** (TentMan.AdminApi).

---

## 📚 Table of Contents

- [Overview](#overview)
- [Main API (TentMan.Api)](#main-api-tentmanapi)
- [Admin API (TentMan.AdminApi)](#admin-api-tentmanadminapi)
- [Authentication](#authentication)
- [Common Workflows](#common-workflows)
- [HTTP Examples](#http-examples)
- [OpenAPI Documentation](#openapi-documentation)
- [Error Handling](#error-handling)

---

## 🎯 Overview

### Two APIs, One System

TentMan provides **two complementary APIs** that work together:

| Aspect | Main API (TentMan.Api) | Admin API (TentMan.AdminApi) |
|--------|---------------------|---------------------------|
| **Purpose** | Public-facing API | Administrative operations |
| **Port** | 7123 (HTTPS) | 7290 (HTTPS) |
| **Authentication** | Self-registration + Login | Requires pre-existing account |
| **Primary Users** | End users, frontend apps | Administrators, system managers |
| **Endpoints** | 16 endpoints | 12 endpoints |
| **Features** | Auth, Products, Account | Users, Roles, Assignments |

### Shared Infrastructure

Both APIs share:
- ✅ **Same JWT Secret** - Tokens work across both APIs
- ✅ **Same Database** - Unified user/role management
- ✅ **Same Identity System** - ASP.NET Core Identity
- ✅ **Consistent Error Responses** - Standardized error format

---

## 🌐 Main API (TentMan.Api)

**Base URL**: `https://localhost:7123`  
**API Docs**: `https://localhost:7123/scalar/v1`

### Purpose

Public-facing API for:
- User authentication (register, login, logout)
- Product catalog operations (CRUD)
- Password management (change, reset)
- Email confirmation
- Account management

### Key Features

- ✅ **Self-registration** - Users can create accounts
- ✅ **JWT authentication** - Secure token-based auth
- ✅ **Role-based access** - Different permissions per role
- ✅ **Refresh tokens** - Seamless re-authentication
- ✅ **OpenAPI docs** - Interactive Scalar UI

### Endpoint Categories

#### 1. Authentication (`/api/v1/authentication`)

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/register` | POST | ❌ No | Create new account |
| `/login` | POST | ❌ No | Login and get JWT tokens |
| `/refresh` | POST | ❌ No | Refresh access token |
| `/logout` | POST | ✅ Yes | Logout (invalidate tokens) |
| `/confirm-email` | POST | ❌ No | Confirm email address |

**Example - Register**:
```json
POST /api/v1/authentication/register
{
  "userName": "johndoe",
  "email": "john@example.com",
  "password": "SecurePass123!"
}

Response 200 OK:
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "abc123...",
  "expiresIn": 3600
}
```

**Example - Login**:
```json
POST /api/v1/authentication/login
{
  "email": "john@example.com",
  "password": "SecurePass123!"
}

Response 200 OK:
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "abc123...",
  "expiresIn": 3600
}
```

#### 2. Products (`/api/v1/products`)

| Endpoint | Method | Auth | Roles | Description |
|----------|--------|------|-------|-------------|
| `/` | GET | ✅ Yes | User+ | List all products |
| `/{id}` | GET | ✅ Yes | User+ | Get product by ID |
| `/` | POST | ✅ Yes | Manager+ | Create product |
| `/{id}` | PUT | ✅ Yes | Manager+ | Update product |
| `/{id}` | DELETE | ✅ Yes | Admin+ | Delete product |

**Example - Create Product**:
```json
POST /api/v1/products
Authorization: Bearer <token>

{
  "name": "Laptop",
  "price": 1299.99
}

Response 201 Created:
{
  "id": "guid",
  "name": "Laptop",
  "price": 1299.99,
  "rowVersion": "AAAAAAAAB9E="
}
```

#### 3. Account Management (`/api/v1/account`)

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/change-password` | POST | ✅ Yes | Change your password |
| `/reset-password-request` | POST | ❌ No | Request password reset |
| `/reset-password` | POST | ❌ No | Reset password with token |

**Example - Change Password**:
```json
POST /api/v1/account/change-password
Authorization: Bearer <token>

{
  "currentPassword": "OldPass123!",
  "newPassword": "NewPass456!",
  "confirmNewPassword": "NewPass456!"
}

Response 200 OK:
{
  "success": true,
  "message": "Password changed successfully"
}
```

#### 4. Health Checks

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Overall health |
| `/health/ready` | GET | Readiness check |
| `/health/live` | GET | Liveness check |

### Role Requirements (Main API)

| Operation | Public | User | Manager | Administrator |
|-----------|--------|------|---------|---------------|
| Register/Login | ✅ | ✅ | ✅ | ✅ |
| Password Mgmt | ✅ | ✅ | ✅ | ✅ |
| Read Products | ❌ | ✅ | ✅ | ✅ |
| Create Products | ❌ | ❌ | ✅ | ✅ |
| Update Products | ❌ | ❌ | ✅ | ✅ |
| Delete Products | ❌ | ❌ | ❌ | ✅ |

---

## 🛡️ Admin API (TentMan.AdminApi)

**Base URL**: `https://localhost:7290`  
**API Docs**: `https://localhost:7290/scalar/v1`

### Purpose

Administrative API for:
- User management (create, delete, list)
- Role management (create, assign, remove)
- System initialization
- Security restrictions enforcement

### Key Features

- ✅ **Centralized user management** - Admins control users
- ✅ **Role-based administration** - Fine-grained permissions
- ✅ **Security restrictions** - Prevent dangerous operations
- ✅ **System initialization** - One-time setup
- ✅ **Pagination support** - Handle large datasets

### Endpoint Categories

#### 1. System Initialization (`/api/v1/admin/initialization`)

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/initialize` | POST | ❌ No (once) | Initialize system with SuperAdmin |

**⚠️ ONE-TIME OPERATION**: Can only be called when no users exist.

**Example**:
```json
POST /api/v1/admin/initialization/initialize

{
  "userName": "superadmin",
  "email": "admin@company.com",
  "password": "SuperSecure123!"
}

Response 200 OK:
{
  "userId": "guid",
  "userName": "superadmin",
  "email": "admin@company.com",
  "roles": ["SuperAdmin", "Administrator", "User"]
}
```

**What it creates**:
- 5 system roles: Guest, User, Manager, Administrator, SuperAdmin
- 1 SuperAdmin user with credentials you specify

#### 2. User Management (`/api/v1/admin/users`)

| Endpoint | Method | Auth | Roles | Description |
|----------|--------|------|-------|-------------|
| `/` | GET | ✅ Yes | Manager+ | List all users (paginated) |
| `/` | POST | ✅ Yes | Manager+ | Create new user |
| `/{id}` | DELETE | ✅ Yes | SuperAdmin+ | Delete user |

**Example - List Users**:
```json
GET /api/v1/admin/users?pageNumber=1&pageSize=10
Authorization: Bearer <token>

Response 200 OK:
{
  "items": [
    {
      "id": "guid",
      "userName": "johndoe",
      "email": "john@example.com",
      "emailConfirmed": true,
      "roles": ["User"]
    }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 25,
  "totalPages": 3
}
```

**Example - Create User**:
```json
POST /api/v1/admin/users
Authorization: Bearer <token>

{
  "userName": "janedoe",
  "email": "jane@example.com",
  "password": "TempPass123!",
  "emailConfirmed": false
}

Response 201 Created:
{
  "id": "guid",
  "userName": "janedoe",
  "email": "jane@example.com",
  "emailConfirmed": false
}
```

**Example - Delete User**:
```json
DELETE /api/v1/admin/users/{userId}
Authorization: Bearer <token>

Response 200 OK
```

**Security Restrictions**:
- ❌ Cannot delete yourself
- ❌ Cannot delete the last SuperAdmin

#### 3. Role Management (`/api/v1/admin/roles`)

| Endpoint | Method | Auth | Roles | Description |
|----------|--------|------|-------|-------------|
| `/` | GET | ✅ Yes | Manager+ | List all roles |
| `/` | POST | ✅ Yes | SuperAdmin+ | Create custom role |

**Example - List Roles**:
```json
GET /api/v1/admin/roles
Authorization: Bearer <token>

Response 200 OK:
[
  {
    "id": "guid",
    "name": "SuperAdmin",
    "description": "Super administrator with full access"
  },
  {
    "id": "guid",
    "name": "User",
    "description": "Standard user role"
  }
]
```

**Example - Create Role**:
```json
POST /api/v1/admin/roles
Authorization: Bearer <token>

{
  "name": "ContentEditor",
  "description": "Can edit content but not manage users"
}

Response 201 Created:
{
  "id": "guid",
  "name": "ContentEditor",
  "description": "Can edit content but not manage users"
}
```

#### 4. User Role Management (`/api/v1/admin/user-roles`)

| Endpoint | Method | Auth | Roles | Description |
|----------|--------|------|-------|-------------|
| `/{userId}` | GET | ✅ Yes | Manager+ | Get user's roles |
| `/assign` | POST | ✅ Yes | SuperAdmin+ | Assign role to user |
| `/{userId}/roles/{roleId}` | DELETE | ✅ Yes | SuperAdmin+ | Remove role from user |

**Example - Get User Roles**:
```json
GET /api/v1/admin/user-roles/{userId}
Authorization: Bearer <token>

Response 200 OK:
[
  {
    "id": "guid",
    "name": "User"
  },
  {
    "id": "guid",
    "name": "Manager"
  }
]
```

**Example - Assign Role**:
```json
POST /api/v1/admin/user-roles/assign
Authorization: Bearer <token>

{
  "userId": "user-guid",
  "roleId": "role-guid"
}

Response 200 OK:
{
  "success": true,
  "message": "Role assigned successfully"
}
```

**Example - Remove Role**:
```json
DELETE /api/v1/admin/user-roles/{userId}/roles/{roleId}
Authorization: Bearer <token>

Response 200 OK
```

**Security Restrictions**:
- ❌ Only SuperAdmin can assign SuperAdmin/Administrator roles
- ❌ Administrators cannot assign SuperAdmin role
- ❌ Cannot remove your own privileged roles (SuperAdmin, Administrator)
- ❌ Cannot remove roles from the last SuperAdmin

#### 5. Health Checks

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Overall health |
| `/health/ready` | GET | Readiness check |
| `/health/live` | GET | Liveness check |

### Role Requirements (Admin API)

| Operation | Manager | Administrator | SuperAdmin |
|-----------|---------|---------------|------------|
| View Users | ✅ | ✅ | ✅ |
| Create Users | ✅ | ✅ | ✅ |
| Delete Users | ❌ | ❌ | ✅ |
| View Roles | ✅ | ✅ | ✅ |
| Create Roles | ❌ | ❌ | ✅ |
| Assign User/Guest roles | ❌ | ✅ | ✅ |
| Assign Manager role | ❌ | ✅ | ✅ |
| Assign Administrator role | ❌ | ❌ | ✅ |
| Assign SuperAdmin role | ❌ | ❌ | ✅ |

---

## 🔐 Authentication

### Obtaining a JWT Token

**Option 1: Register (Main API)**
```json
POST https://localhost:7123/api/v1/authentication/register
{
  "userName": "johndoe",
  "email": "john@example.com",
  "password": "SecurePass123!"
}
```

**Option 2: Login (Main API)**
```json
POST https://localhost:7123/api/v1/authentication/login
{
  "email": "john@example.com",
  "password": "SecurePass123!"
}
```

**Response**:
```json
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "abc123...",
  "expiresIn": 3600
}
```

### Using the Token

**All authenticated requests require the `Authorization` header**:

```http
Authorization: Bearer eyJhbGc...
```

### Token Expiration

- **Access Token**: 60 minutes (configurable)
- **Refresh Token**: 7 days (configurable)

### Refreshing Tokens

When access token expires:

```json
POST https://localhost:7123/api/v1/authentication/refresh
{
  "refreshToken": "abc123..."
}

Response 200 OK:
{
  "accessToken": "newToken...",
  "refreshToken": "newRefreshToken...",
  "expiresIn": 3600
}
```

### Token Interoperability

✅ **Tokens from Main API work on Admin API**  
✅ **Both APIs share the same JWT secret**  
✅ **No need to login separately for each API**

---

## 🔄 Common Workflows

### Workflow 1: New User Registration

```
1. User: POST /api/v1/authentication/register (Main API)
   → Get JWT tokens immediately

2. User: POST /api/v1/authentication/confirm-email (Main API)
   → Verify email (optional)

3. User: Use tokens to access protected endpoints
```

### Workflow 2: Admin Creates User

```
1. Admin: Login to Main API
   → Get JWT token

2. Admin: POST /api/v1/admin/users (Admin API)
   → Create user account

3. Admin: POST /api/v1/admin/user-roles/assign (Admin API)
   → Assign appropriate roles

4. User: Login through Main API
   → Start using the system
```

### Workflow 3: Complete System Setup

```
1. POST /api/v1/admin/initialization/initialize (Admin API)
   → Create SuperAdmin and system roles

2. SuperAdmin: Login (Main API)
   → Get JWT token

3. SuperAdmin: Create Manager account (Admin API)
   → POST /api/v1/admin/users

4. SuperAdmin: Assign Manager role (Admin API)
   → POST /api/v1/admin/user-roles/assign

5. Manager: Create products (Main API)
   → POST /api/v1/products

6. Public: Register as users (Main API)
   → POST /api/v1/authentication/register

7. Users: View/use products (Main API)
   → GET /api/v1/products
```

---

## 📝 HTTP Examples

### Main API Examples

**File**: `src/TentMan.Api/TentMan.Api.http`  
**Total**: 40+ request examples

**Categories**:
- Health checks (3 examples)
- Authentication (5 examples)
  - Register
  - Login
  - Refresh token
  - Logout
  - Confirm email
- Products (10+ examples)
  - List products
  - Get product by ID
  - Create product
  - Update product
  - Delete product
- Account management (5+ examples)
  - Change password
  - Reset password request
  - Reset password

**Usage in Visual Studio**:
1. Open `TentMan.Api.http`
2. Update `jwt_token` variable
3. Click "Send Request" next to any example

### Admin API Examples

**File**: `TentMan.AdminApi/TentMan.AdminApi.http`  
**Total**: 31 request examples

**Categories**:
- Health checks (3 examples)
- System initialization (1 example)
- User management (10+ examples)
  - List users (with pagination)
  - Create user
  - Delete user
  - Security restrictions
- Role management (5+ examples)
  - List roles
  - Create custom role
- User role management (10+ examples)
  - Get user roles
  - Assign role
  - Remove role
  - Security restrictions

**Usage in Visual Studio**:
1. Open `TentMan.AdminApi.http`
2. Update `jwt_token` variable
3. Click "Send Request" next to any example

---

## 📖 OpenAPI Documentation

### Interactive Documentation (Scalar UI)

**Main API**:
- URL: https://localhost:7123/scalar/v1
- Theme: DeepSpace
- Features:
  - Try-it-out functionality
  - JWT authorization
  - Request/response examples
  - Schema browsing
  - Code generation

**Admin API**:
- URL: https://localhost:7290/scalar/v1
- Theme: DeepSpace
- Features: Same as Main API

### OpenAPI Specifications

**Main API JSON**: https://localhost:7123/openapi/v1.json  
**Admin API JSON**: https://localhost:7290/openapi/v1.json

### Features

✅ **Comprehensive endpoint documentation**  
✅ **JWT Bearer authentication configured**  
✅ **Request/response schemas**  
✅ **Example values**  
✅ **Security requirements**  
✅ **Server URLs**  
✅ **API versioning** (v1)

---

## ⚠️ Error Handling

### Standard Error Response

All APIs return errors in a consistent format:

```json
{
  "success": false,
  "message": "Error description",
  "errors": [
    {
      "code": "ErrorCode",
      "message": "Detailed error message"
    }
  ]
}
```

### HTTP Status Codes

| Code | Meaning | Common Causes |
|------|---------|---------------|
| **200** | OK | Successful GET, PUT, DELETE |
| **201** | Created | Successful POST (resource created) |
| **400** | Bad Request | Validation error, business rule violation |
| **401** | Unauthorized | Missing/expired/invalid token |
| **403** | Forbidden | Insufficient permissions |
| **404** | Not Found | Resource doesn't exist |
| **409** | Conflict | Concurrency conflict (stale data) |
| **500** | Internal Server Error | Unexpected server error |

### Common Error Scenarios

#### 400 Bad Request
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    {
      "field": "Password",
      "message": "Password must be at least 8 characters"
    }
  ]
}
```

#### 401 Unauthorized
```json
{
  "success": false,
  "message": "Unauthorized"
}
```

**Causes**:
- Missing `Authorization` header
- Expired access token
- Invalid token

**Fix**: Obtain new token via login or refresh

#### 403 Forbidden
```json
{
  "success": false,
  "message": "You do not have permission to perform this action"
}
```

**Causes**:
- User lacks required role
- Security restriction violated

**Fix**: Ensure user has appropriate role

#### 409 Conflict
```json
{
  "success": false,
  "message": "The record has been modified by another user. Please refresh and try again."
}
```

**Cause**: Optimistic concurrency conflict

**Fix**: Refresh data and retry operation

---

## 🎯 Best Practices

### API Usage

✅ **Always include Authorization header** for protected endpoints  
✅ **Handle token expiration** gracefully (use refresh token)  
✅ **Check role requirements** before calling endpoints  
✅ **Use pagination** for large datasets  
✅ **Handle errors** appropriately  
✅ **Validate input** before sending requests  
✅ **Use HTTPS** in production  

### Security

✅ **Store tokens securely** (never in localStorage)  
✅ **Use short-lived access tokens** (15-60 minutes)  
✅ **Rotate refresh tokens** regularly  
✅ **Validate JWT signatures**  
✅ **Enforce HTTPS** in production  
✅ **Implement rate limiting** for authentication endpoints  
✅ **Monitor admin actions** through logs  

### Performance

✅ **Use pagination** for list endpoints  
✅ **Cache frequently accessed data**  
✅ **Minimize payload size**  
✅ **Use compression** (gzip)  
✅ **Implement retries** with exponential backoff  

---

## 📚 Related Documentation

- **[GETTING_STARTED.md](GETTING_STARTED.md)** - Initial setup guide
- **[AUTHENTICATION_GUIDE.md](AUTHENTICATION_GUIDE.md)** - JWT and authentication details
- **[AUTHORIZATION_GUIDE.md](AUTHORIZATION_GUIDE.md)** - Role-based authorization
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - System architecture
- **[Main API README](../src/TentMan.Api/README.md)** - Main API project details
- **[Admin API README](../TentMan.AdminApi/README.md)** - Admin API project details

---

**Last Updated**: 2025-01-22  
**Version**: 1.0  
**Total Endpoints**: 28 (16 Main + 12 Admin)  
**Maintainer**: TentMan Development Team
