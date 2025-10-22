# Archu APIs - Comparison Guide

## 📋 Overview

This guide compares the two Archu APIs and helps you choose which one to use for your needs.

---

## 🎯 API Overview

### Archu.Api (Main API)
**Base URL:** https://localhost:7268  
**Purpose:** Public-facing API for application functionality  
**Primary Use:** User authentication, product management, application features

### Archu.AdminApi (Administration API)
**Base URL:** https://localhost:7290  
**Purpose:** Administrative operations and system management  
**Primary Use:** User administration, role management, system initialization

---

## 🔐 Authentication Comparison

| Feature | Archu.Api | Archu.AdminApi |
|---------|-----------|----------------|
| **Registration** | ✅ Yes | ❌ No (users created by admins) |
| **Login** | ✅ Yes | ✅ Yes (same JWT system) |
| **JWT Tokens** | ✅ Yes | ✅ Yes (shared authentication) |
| **Password Management** | ✅ Full (change, reset, forgot) | ❌ No |
| **Email Verification** | ✅ Yes | ❌ No |
| **Public Access** | ✅ Some endpoints | ❌ All require auth |

**Note:** Both APIs share the same JWT authentication system and database. A user registered in Archu.Api can login to Archu.AdminApi if they have admin roles.

---

## 📊 Endpoint Comparison

### Archu.Api Endpoints

| Endpoint | Method | Auth Required | Description |
|----------|--------|---------------|-------------|
| `/api/v1/authentication/register` | POST | ❌ No | Register new user account |
| `/api/v1/authentication/login` | POST | ❌ No | Login with credentials |
| `/api/v1/authentication/logout` | POST | ✅ Yes | Logout and revoke token |
| `/api/v1/authentication/refresh-token` | POST | ❌ No | Refresh expired token |
| `/api/v1/authentication/change-password` | POST | ✅ Yes | Change user password |
| `/api/v1/authentication/forgot-password` | POST | ❌ No | Request password reset |
| `/api/v1/authentication/reset-password` | POST | ❌ No | Reset password with token |
| `/api/v1/authentication/confirm-email` | POST | ❌ No | Confirm email address |
| `/api/v1/products` | GET | ✅ Yes | Get all products |
| `/api/v1/products/{id}` | GET | ✅ Yes | Get product by ID |
| `/api/v1/products` | POST | ✅ Yes (Manager/Admin) | Create product |
| `/api/v1/products/{id}` | PUT | ✅ Yes (Manager/Admin) | Update product |
| `/api/v1/products/{id}` | DELETE | ✅ Yes (Admin) | Delete product |
| `/health` | GET | ❌ No | Health check |
| `/health/ready` | GET | ❌ No | Readiness check |
| `/health/live` | GET | ❌ No | Liveness check |

**Total:** 16 endpoints

---

### Archu.AdminApi Endpoints

| Endpoint | Method | Auth Required | Description |
|----------|--------|---------------|-------------|
| `/api/v1/admin/initialization/initialize` | POST | ❌ No (one-time) | Initialize system |
| `/api/v1/admin/users` | GET | ✅ Yes | Get all users (paginated) |
| `/api/v1/admin/users` | POST | ✅ Yes | Create new user |
| `/api/v1/admin/users/{id}` | DELETE | ✅ Yes | Delete user |
| `/api/v1/admin/roles` | GET | ✅ Yes | Get all roles |
| `/api/v1/admin/roles` | POST | ✅ Yes | Create custom role |
| `/api/v1/admin/user-roles/{userId}` | GET | ✅ Yes | Get user's roles |
| `/api/v1/admin/user-roles/assign` | POST | ✅ Yes | Assign role to user |
| `/api/v1/admin/user-roles/{userId}/roles/{roleId}` | DELETE | ✅ Yes | Remove role from user |
| `/health` | GET | ❌ No | Health check |
| `/health/ready` | GET | ❌ No | Readiness check |
| `/health/live` | GET | ❌ No | Liveness check |

**Total:** 12 endpoints

---

## 🛡️ Role Requirements

### Archu.Api Role Requirements

| Operation | Public | User | Manager | Admin |
|-----------|--------|------|---------|-------|
| Register/Login | ✅ | ✅ | ✅ | ✅ |
| Password Mgmt | ✅ | ✅ | ✅ | ✅ |
| Read Products | ❌ | ✅ | ✅ | ✅ |
| Create Products | ❌ | ❌ | ✅ | ✅ |
| Update Products | ❌ | ❌ | ✅ | ✅ |
| Delete Products | ❌ | ❌ | ❌ | ✅ |

---

### Archu.AdminApi Role Requirements

| Operation | Public | User | Manager | SuperAdmin | Admin |
|-----------|--------|------|---------|------------|-------|
| Initialize System | ✅ (once) | ❌ | ❌ | ❌ | ❌ |
| View Users | ❌ | ❌ | ✅ | ✅ | ✅ |
| Create Users | ❌ | ❌ | ✅ | ✅ | ✅ |
| Delete Users | ❌ | ❌ | ❌ | ✅ | ✅ |
| View Roles | ❌ | ❌ | ✅ | ✅ | ✅ |
| Create Roles | ❌ | ❌ | ❌ | ✅ | ✅ |
| Assign Roles (User/Guest) | ❌ | ❌ | ❌ | ✅ | ✅ |
| Assign Roles (Manager) | ❌ | ❌ | ❌ | ✅ | ✅ |
| Assign Roles (Admin) | ❌ | ❌ | ❌ | ✅ | ❌ |
| Assign Roles (SuperAdmin) | ❌ | ❌ | ❌ | ✅ | ❌ |

**Security Notes:**
- Only SuperAdmin can assign SuperAdmin/Administrator roles
- Cannot delete last SuperAdmin
- Cannot delete yourself
- Cannot remove own privileged roles

---

## 🔄 Common Workflows

### Workflow 1: New User Registration & First Login

**Using Archu.Api:**
```
1. POST /api/v1/authentication/register
   → Get JWT tokens immediately
2. (Optional) POST /api/v1/authentication/confirm-email
   → Verify email
3. Use tokens to access protected endpoints
```

**Using Archu.AdminApi:**
```
1. Admin: POST /api/v1/admin/users (create user)
   → User created by admin
2. User: Login through Archu.Api or other authentication
3. Admin: POST /api/v1/admin/user-roles/assign
   → Assign appropriate roles
```

---

### Workflow 2: User Management

**For Self-Service (Archu.Api):**
```
User manages their own account:
- Change password
- Reset forgotten password
- Confirm email
- Logout
```

**For Admin Management (Archu.AdminApi):**
```
Admin manages other users:
- Create users
- Assign roles
- Remove roles
- Delete users
- View all users
```

---

### Workflow 3: Complete System Setup

**Step-by-Step:**
```
1. AdminApi: POST /api/v1/admin/initialization/initialize
   → Create SuperAdmin and default roles

2. AdminApi: Login as SuperAdmin
   → Get JWT token

3. AdminApi: POST /api/v1/admin/users (create manager)
   → Create manager account

4. AdminApi: POST /api/v1/admin/user-roles/assign
   → Assign Manager role

5. Api: Manager can now create products
   → POST /api/v1/products

6. Api: Public users register
   → POST /api/v1/authentication/register

7. Api: Users view products
   → GET /api/v1/products
```

---

## 📖 Documentation Comparison

### Archu.Api Documentation

| Resource | Location | Description |
|----------|----------|-------------|
| **OpenAPI UI** | https://localhost:7268/scalar/v1 | Interactive API explorer |
| **OpenAPI Spec** | https://localhost:7268/openapi/v1.json | Machine-readable spec |
| **HTTP Examples** | `src/Archu.Api/Archu.Api.http` | 40+ request examples |
| **Full Guide** | `/docs/ARCHU_API_DOCUMENTATION.md` | Comprehensive guide |
| **Quick Reference** | `/docs/ARCHU_API_QUICK_REFERENCE.md` | Developer cheat sheet |

---

### Archu.AdminApi Documentation

| Resource | Location | Description |
|----------|----------|-------------|
| **OpenAPI UI** | https://localhost:7290/scalar/v1 | Interactive API explorer |
| **OpenAPI Spec** | https://localhost:7290/openapi/v1.json | Machine-readable spec |
| **HTTP Examples** | `Archu.AdminApi/Archu.AdminApi.http` | 31 request examples |
| **Quick Reference** | `/docs/ADMIN_API_QUICK_REFERENCE.md` | Developer cheat sheet |
| **Update Docs** | `/docs/OPENAPI_DOCUMENTATION_UPDATE.md` | Implementation details |
| **HTTP Guide** | `/docs/HTTP_REQUESTS_GUIDE.md` | Testing guide |

---

## 🎨 UI Themes

| API | Theme | Dark Mode |
|-----|-------|-----------|
| **Archu.Api** | DeepSpace | ✅ Enabled |
| **Archu.AdminApi** | Purple | ❌ Disabled |

Both UIs provide:
- Try-it-out functionality
- Authentication support
- Schema browsing
- Request/response examples
- Code generation

---

## 🔧 Configuration Comparison

### Shared Configuration

Both APIs share:
- **Database:** Same SQL Server database
- **JWT Settings:** Same authentication system
- **Connection Strings:** Same database connection
- **Identity:** Same user/role tables

### Separate Configuration

Each API has:
- **Port Numbers:** Different (7268 vs 7290)
- **OpenAPI Docs:** Separate specifications
- **Authorization Policies:** Different policy sets
- **Endpoints:** Non-overlapping functionality

---

## 🚀 When to Use Each API

### Use Archu.Api When:

✅ **User Registration**
- Public user sign-up
- Self-service account creation
- Email verification needed

✅ **User Authentication**
- Login/logout
- Token refresh
- Password management

✅ **Application Features**
- Product browsing
- Product management (for managers)
- Business operations

✅ **Public Access**
- Any endpoint that doesn't require authentication
- Registration flows
- Password reset

---

### Use Archu.AdminApi When:

✅ **System Administration**
- Initial system setup
- Bootstrap with SuperAdmin
- Create default roles

✅ **User Management**
- Create users as admin
- Assign/remove roles
- Delete users
- View all users

✅ **Role Management**
- Create custom roles
- Manage role hierarchy
- View all roles

✅ **Administrative Operations**
- Bulk user operations
- Security management
- System configuration

---

## 🔄 Integration Patterns

### Pattern 1: Frontend with Both APIs

**Frontend Application:**
```
┌─────────────────────────┐
│   Frontend (Blazor)     │
│                         │
│  ┌──────────────────┐  │
│  │  User Features   │──┼──→ Archu.Api
│  │  - Login         │  │    (Port 7268)
│  │  - Products      │  │
│  │  - Profile       │  │
│  └──────────────────┘  │
│                         │
│  ┌──────────────────┐  │
│  │  Admin Panel     │──┼──→ Archu.AdminApi
│  │  - Users         │  │    (Port 7290)
│  │  - Roles         │  │
│  │  - System        │  │
│  └──────────────────┘  │
└─────────────────────────┘
```

**Implementation:**
```csharp
// Configure both API clients
builder.Services.AddHttpClient("ArchuApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7268");
});

builder.Services.AddHttpClient("ArchuAdminApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7290");
});

// Use same JWT token for both APIs
```

---

### Pattern 2: Microservices Architecture

```
┌─────────────┐
│   Gateway   │
└──────┬──────┘
       │
       ├──────────────┐
       │              │
       ▼              ▼
┌─────────────┐  ┌─────────────┐
│ Archu.Api   │  │AdminApi     │
│             │  │             │
│ - Auth      │  │ - Users     │
│ - Products  │  │ - Roles     │
└──────┬──────┘  └──────┬──────┘
       │                │
       └────────┬───────┘
                ▼
        ┌───────────────┐
        │   Database    │
        │   (Shared)    │
        └───────────────┘
```

---

### Pattern 3: Mobile App + Web Admin

```
┌──────────────┐              ┌──────────────┐
│  Mobile App  │──────────────│  Archu.Api   │
│              │              │              │
│ - Login      │  JWT Tokens  │ - Auth       │
│ - Products   │◄─────────────│ - Products   │
│ - Profile    │              │              │
└──────────────┘              └──────────────┘

┌──────────────┐              ┌──────────────┐
│  Web Admin   │──────────────│ AdminApi     │
│              │              │              │
│ - Dashboard  │  JWT Tokens  │ - Users      │
│ - Users      │◄─────────────│ - Roles      │
│ - Settings   │              │              │
└──────────────┘              └──────────────┘
```

---

## 🧪 Testing Both APIs

### Test Scenario: Complete User Journey

**1. System Setup (AdminApi):**
```http
POST https://localhost:7290/api/v1/admin/initialization/initialize
{
  "userName": "superadmin",
  "email": "admin@example.com",
  "password": "Admin123!"
}
```

**2. Create Manager (AdminApi):**
```http
POST https://localhost:7290/api/v1/admin/users
Authorization: Bearer {admin-token}
{
  "userName": "manager1",
  "email": "manager@example.com",
  "password": "Manager123!"
}
```

**3. Assign Manager Role (AdminApi):**
```http
POST https://localhost:7290/api/v1/admin/user-roles/assign
Authorization: Bearer {admin-token}
{
  "userId": "{manager-user-id}",
  "roleId": "{manager-role-id}"
}
```

**4. User Self-Registration (Api):**
```http
POST https://localhost:7268/api/v1/authentication/register
{
  "userName": "user1",
  "email": "user@example.com",
  "password": "User123!"
}
```

**5. Manager Creates Product (Api):**
```http
POST https://localhost:7268/api/v1/products
Authorization: Bearer {manager-token}
{
  "name": "New Product",
  "price": 29.99
}
```

**6. User Views Products (Api):**
```http
GET https://localhost:7268/api/v1/products
Authorization: Bearer {user-token}
```

---

## 📊 Feature Matrix

| Feature | Archu.Api | Archu.AdminApi |
|---------|-----------|----------------|
| **User Registration** | ✅ Self-service | ❌ Admin creates |
| **User Login** | ✅ Yes | ✅ Yes |
| **JWT Authentication** | ✅ Yes | ✅ Yes |
| **Token Refresh** | ✅ Yes | ✅ Yes (same system) |
| **Password Change** | ✅ Self-service | ❌ No |
| **Password Reset** | ✅ Self-service | ❌ No |
| **Email Confirmation** | ✅ Yes | ❌ No |
| **User Management** | ❌ No | ✅ Full CRUD |
| **Role Management** | ❌ No | ✅ Full CRUD |
| **Role Assignment** | ❌ No | ✅ Yes |
| **System Initialization** | ❌ No | ✅ Yes |
| **Product Management** | ✅ Full CRUD | ❌ No |
| **Health Checks** | ✅ Yes | ✅ Yes |
| **OpenAPI Docs** | ✅ Complete | ✅ Complete |
| **Pagination** | ❌ Not yet | ✅ Users endpoint |
| **Optimistic Locking** | ✅ Products | ❌ No |

---

## 💡 Best Practices

### Token Management

**Shared JWT System:**
- Both APIs use the same JWT tokens
- Login through either API
- Token works on both APIs
- Same token expiration (1 hour)
- Same refresh token logic (7 days)

**Recommended Approach:**
```csharp
// Single token for both APIs
var token = await GetJwtTokenAsync(); // From either API

// Use in both API calls
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);

await ArchuApiClient.GetProductsAsync();
await AdminApiClient.GetUsersAsync();
```

---

### Security Considerations

**Archu.Api:**
- ✅ Expose publicly (with proper security)
- ✅ Allow user registration
- ✅ Implement rate limiting
- ✅ Use HTTPS in production

**Archu.AdminApi:**
- ⚠️ **Do NOT** expose publicly
- ⚠️ Restrict to internal network/VPN
- ⚠️ Additional firewall rules
- ⚠️ Monitor admin operations
- ✅ Use HTTPS always

---

### Error Handling

Both APIs use same response format:

**Success:**
```json
{
  "success": true,
  "message": "Operation successful",
  "data": { /* results */ }
}
```

**Error:**
```json
{
  "success": false,
  "message": "Error description",
  "data": null
}
```

**Unified Error Handler:**
```csharp
public async Task<T> HandleApiCall<T>(Func<Task<ApiResponse<T>>> apiCall)
{
    try
    {
        var response = await apiCall();
        
        if (!response.Success)
            throw new ApiException(response.Message);
            
        return response.Data;
    }
    catch (HttpRequestException ex)
    {
        // Handle network errors
    }
}
```

---

## 📞 Support & Resources

### Documentation

**Archu.Api:**
- Full Guide: `/docs/ARCHU_API_DOCUMENTATION.md`
- Quick Reference: `/docs/ARCHU_API_QUICK_REFERENCE.md`
- HTTP Examples: `src/Archu.Api/Archu.Api.http`

**Archu.AdminApi:**
- Quick Reference: `/docs/ADMIN_API_QUICK_REFERENCE.md`
- HTTP Guide: `/docs/HTTP_REQUESTS_GUIDE.md`
- HTTP Examples: `Archu.AdminApi/Archu.AdminApi.http`

**Both APIs:**
- Update Summary: `/docs/OPENAPI_UPDATE_SUMMARY.md`
- This Comparison: `/docs/API_COMPARISON_GUIDE.md`

### Interactive Documentation

- **Archu.Api Scalar UI**: https://localhost:7268/scalar/v1
- **AdminApi Scalar UI**: https://localhost:7290/scalar/v1

### Contact

- **GitHub**: https://github.com/chethandvg/archu
- **Issues**: https://github.com/chethandvg/archu/issues
- **Email**: support@archu.com

---

## ✅ Quick Decision Matrix

**Choose Archu.Api for:**
- ✅ Public-facing features
- ✅ User self-service
- ✅ Mobile/web app integration
- ✅ Product catalog
- ✅ Authentication flows

**Choose Archu.AdminApi for:**
- ✅ Administrative tasks
- ✅ User management
- ✅ Role management
- ✅ System configuration
- ✅ Internal operations

**Use Both When:**
- ✅ Building full application
- ✅ Need admin panel + user interface
- ✅ Different access levels required
- ✅ Microservices architecture

---

**Version:** 1.0  
**Last Updated:** 2025-01-22  
**Status:** ✅ Complete

Happy Coding! 🚀
