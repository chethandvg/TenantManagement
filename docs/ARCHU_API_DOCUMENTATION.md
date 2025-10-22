# Archu API - Comprehensive Documentation

## 📋 Overview

The Archu API is a comprehensive RESTful API built with .NET 9 that provides authentication, user management, and product catalog functionality. It features JWT-based authentication, role-based authorization, and follows modern API design principles.

---

## 🚀 Quick Start

### Prerequisites
- .NET 9 SDK
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 or VS Code
- Aspire Workload (for development)

### Running the API

```bash
# Clone the repository
git clone https://github.com/chethandvg/archu
cd archu

# Start all services using Aspire
cd src/Archu.AppHost
dotnet run

# The API will be available at:
# HTTPS: https://localhost:7123
# HTTP:  http://localhost:5268
```

### Access Documentation
- **Scalar UI**: https://localhost:7123/scalar/v1
- **OpenAPI JSON**: https://localhost:7123/openapi/v1.json
- **Health Check**: https://localhost:7123/health

---

## 🔐 Authentication

### Authentication Flow

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant Database
    
    Client->>API: POST /api/v1/authentication/register
    API->>Database: Create User
    Database-->>API: User Created
    API-->>Client: JWT Tokens + User Info
    
    Client->>API: GET /api/Products (with token)
    API->>API: Validate JWT Token
    API->>Database: Query Products
    Database-->>API: Products Data
    API-->>Client: Products Response
    
    Note over Client,API: Token expires after 1 hour
    
    Client->>API: POST /api/v1/authentication/refresh-token
    API->>Database: Validate Refresh Token
    Database-->>API: Token Valid
    API-->>Client: New JWT Tokens
```

### Register New User

**Endpoint:** `POST /api/v1/authentication/register`

**Request:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "userName": "johndoe"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Registration successful",
  "data": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userName": "johndoe",
    "email": "user@example.com",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "base64-encoded-refresh-token",
    "expiresAt": "2025-01-22T12:00:00Z"
  }
}
```

**Validation Rules:**
- Email: Required, valid email format
- Password: Min 8 characters, max 100 characters
- UserName: Required, unique

### Login

**Endpoint:** `POST /api/v1/authentication/login`

**Request:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userName": "johndoe",
    "email": "user@example.com",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "base64-encoded-refresh-token",
    "expiresAt": "2025-01-22T12:00:00Z"
  }
}
```

**Error Response (401 Unauthorized):**
```json
{
  "success": false,
  "message": "Invalid email or password",
  "data": null
}
```

### Refresh Token

**Endpoint:** `POST /api/v1/authentication/refresh-token`

**Request:**
```json
{
  "refreshToken": "base64-encoded-refresh-token"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Token refreshed successfully",
  "data": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userName": "johndoe",
    "email": "user@example.com",
    "token": "new-jwt-access-token",
    "refreshToken": "new-refresh-token",
    "expiresAt": "2025-01-22T13:00:00Z"
  }
}
```

**Notes:**
- Access tokens expire after 1 hour (configurable)
- Refresh tokens expire after 7 days (configurable)
- Old refresh token is invalidated after successful refresh
- Use before access token expires to avoid 401 errors

### Logout

**Endpoint:** `POST /api/v1/authentication/logout`

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Logged out successfully",
  "data": {}
}
```

**Effects:**
- Revokes refresh token in database
- Client should discard both access and refresh tokens
- User must login again to get new tokens

---

## 🔑 Password Management

### Change Password (Authenticated)

**Endpoint:** `POST /api/v1/authentication/change-password`

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Request:**
```json
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewSecurePassword456!"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Password changed successfully",
  "data": {}
}
```

**Error Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Current password is incorrect",
  "data": null
}
```

### Forgot Password

**Endpoint:** `POST /api/v1/authentication/forgot-password`

**Request:**
```json
{
  "email": "user@example.com"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "If your email exists in our system, you will receive a password reset link.",
  "data": {}
}
```

**Security Notes:**
- Always returns success to prevent email enumeration
- Reset token sent via email (if email exists)
- Token expires after 1 hour
- Token is single-use

### Reset Password

**Endpoint:** `POST /api/v1/authentication/reset-password`

**Request:**
```json
{
  "email": "user@example.com",
  "resetToken": "token-from-email",
  "newPassword": "NewPassword123!"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Password reset successfully",
  "data": {}
}
```

**Error Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Invalid or expired reset token",
  "data": null
}
```

---

## ✉️ Email Verification

### Confirm Email

**Endpoint:** `POST /api/v1/authentication/confirm-email`

**Request:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "confirmationToken": "token-from-email"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Email confirmed successfully",
  "data": {}
}
```

---

## 📦 Product Management

### Get All Products

**Endpoint:** `GET /api/v1/products`

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Required Role:** User, Manager, or Admin

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Products retrieved successfully",
  "data": [
    {
      "id": "7ba85f64-5717-4562-b3fc-2c963f66afa9",
      "name": "Product 1",
      "price": 19.99,
      "rowVersion": "AAAAAAAAAAA=",
      "createdAt": "2025-01-22T10:00:00Z",
      "updatedAt": "2025-01-22T10:00:00Z"
    },
    {
      "id": "8ca85f64-5717-4562-b3fc-2c963f66afb0",
      "name": "Product 2",
      "price": 29.99,
      "rowVersion": "AAAAAAAAAAB=",
      "createdAt": "2025-01-22T10:30:00Z",
      "updatedAt": "2025-01-22T10:30:00Z"
    }
  ]
}
```

### Get Product by ID

**Endpoint:** `GET /api/v1/products/{id}`

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Required Role:** User, Manager, or Admin

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Product retrieved successfully",
  "data": {
    "id": "7ba85f64-5717-4562-b3fc-2c963f66afa9",
    "name": "Product 1",
    "price": 19.99,
    "rowVersion": "AAAAAAAAAAA=",
    "createdAt": "2025-01-22T10:00:00Z",
    "updatedAt": "2025-01-22T10:00:00Z"
  }
}
```

**Error Response (404 Not Found):**
```json
{
  "success": false,
  "message": "Product with ID 7ba85f64-5717-4562-b3fc-2c963f66afa9 was not found",
  "data": null
}
```

### Create Product

**Endpoint:** `POST /api/v1/products`

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Required Role:** Manager or Admin

**Request:**
```json
{
  "name": "New Product",
  "price": 39.99
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "message": "Product created successfully",
  "data": {
    "id": "9da85f64-5717-4562-b3fc-2c963f66afc1",
    "name": "New Product",
    "price": 39.99,
    "rowVersion": "AAAAAAAAAAA=",
    "createdAt": "2025-01-22T11:00:00Z",
    "updatedAt": "2025-01-22T11:00:00Z"
  }
}
```

**Validation Rules:**
- Name: Required, 1-200 characters
- Price: Required, must be positive

### Update Product

**Endpoint:** `PUT /api/v1/products/{id}`

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Required Role:** Manager or Admin

**Request:**
```json
{
  "id": "7ba85f64-5717-4562-b3fc-2c963f66afa9",
  "name": "Updated Product",
  "price": 24.99,
  "rowVersion": "AAAAAAAAAAA="
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Product updated successfully",
  "data": {
    "id": "7ba85f64-5717-4562-b3fc-2c963f66afa9",
    "name": "Updated Product",
    "price": 24.99,
    "rowVersion": "AAAAAAAAAAB=",
    "createdAt": "2025-01-22T10:00:00Z",
    "updatedAt": "2025-01-22T11:30:00Z"
  }
}
```

**Error Response (409 Conflict):**
```json
{
  "success": false,
  "message": "The product was modified by another user. Please refresh and try again.",
  "data": null
}
```

**Notes:**
- Optimistic concurrency control using RowVersion
- Must provide current RowVersion from GET request
- New RowVersion returned in response

### Delete Product

**Endpoint:** `DELETE /api/v1/products/{id}`

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Required Role:** Admin only

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Product deleted successfully",
  "data": {}
}
```

**Error Response (404 Not Found):**
```json
{
  "success": false,
  "message": "Product with ID 7ba85f64-5717-4562-b3fc-2c963f66afa9 was not found",
  "data": null
}
```

**Notes:**
- Soft delete implementation (IsDeleted flag)
- Product not permanently removed from database
- Only Admin role can delete products

---

## 🏥 Health Checks

### Full Health Status

**Endpoint:** `GET /health`

**Response (200 OK):**
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "sql-server",
      "status": "Healthy",
      "description": "SQL Server connection",
      "duration": "00:00:00.0123456",
      "exception": null,
      "data": {}
    },
    {
      "name": "application-db-context",
      "status": "Healthy",
      "description": "Entity Framework DbContext",
      "duration": "00:00:00.0098765",
      "exception": null,
      "data": {}
    }
  ],
  "totalDuration": "00:00:00.0222221"
}
```

**Response (503 Service Unavailable):**
```json
{
  "status": "Unhealthy",
  "checks": [
    {
      "name": "sql-server",
      "status": "Unhealthy",
      "description": "SQL Server connection",
      "duration": "00:00:05.0000000",
      "exception": "Connection timeout expired",
      "data": {}
    }
  ],
  "totalDuration": "00:00:05.0123456"
}
```

### Readiness Check

**Endpoint:** `GET /health/ready`

**Purpose:** Indicates if application is ready to accept traffic
**Use Case:** Kubernetes readiness probes, load balancer health checks

### Liveness Check

**Endpoint:** `GET /health/live`

**Purpose:** Indicates if application is running (minimal check)
**Use Case:** Kubernetes liveness probes

---

## 🛡️ Authorization & Policies

### Role Hierarchy

```
Admin
  ├── Full access to all operations
  ├── Can delete products
  ├── Can manage users (via Admin API)
  └── Can assign roles (via Admin API)

Manager
  ├── Can create and update products
  ├── Can read all products
  └── Cannot delete products

User
  ├── Can read all products
  └── Read-only access

Guest
  └── Limited read access
```

### Authorization Policies

| Policy | Required Role | Description |
|--------|--------------|-------------|
| `CanReadProducts` | User, Manager, Admin | View products |
| `CanCreateProducts` | Manager, Admin | Create new products |
| `CanUpdateProducts` | Manager, Admin | Modify existing products |
| `CanDeleteProducts` | Admin | Delete products (soft delete) |

### Policy Configuration

Policies are defined in `AuthorizationPolicyExtensions.cs`:

```csharp
public static void ConfigureArchuPolicies(this AuthorizationOptions options)
{
    // Read products - All authenticated users
    options.AddPolicy(AuthorizationPolicies.CanReadProducts, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("User", "Manager", "Admin"));

    // Create/Update products - Manager and Admin
    options.AddPolicy(AuthorizationPolicies.CanCreateProducts, policy =>
        policy.RequireRole("Manager", "Admin"));

    options.AddPolicy(AuthorizationPolicies.CanUpdateProducts, policy =>
        policy.RequireRole("Manager", "Admin"));

    // Delete products - Admin only
    options.AddPolicy(AuthorizationPolicies.CanDeleteProducts, policy =>
        policy.RequireRole("Admin"));
}
```

---

## 📊 Response Format

### Success Response

All successful responses follow this structure:

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": {
    // Response data here
  }
}
```

### Error Response

All error responses follow this structure:

```json
{
  "success": false,
  "message": "Error description",
  "data": null
}
```

### Standard HTTP Status Codes

| Code | Meaning | When Used |
|------|---------|-----------|
| **200** | OK | Successful GET, PUT, DELETE |
| **201** | Created | Successful POST (resource created) |
| **400** | Bad Request | Validation error, invalid request |
| **401** | Unauthorized | Missing/invalid token, wrong credentials |
| **403** | Forbidden | Insufficient permissions |
| **404** | Not Found | Resource doesn't exist |
| **409** | Conflict | Concurrency conflict (RowVersion mismatch) |
| **500** | Internal Server Error | Unexpected server error |

---

## 🔄 API Versioning

### Current Version: v1

All endpoints are prefixed with `/api/v1/`:

```
https://localhost:7123/api/v1/authentication/login
https://localhost:7123/api/v1/products
```

### Version Header

API version is reported in response headers:

```
api-supported-versions: 1.0
```

### Future Versions

When v2 is released:
- v1 endpoints remain available at `/api/v1/`
- v2 endpoints available at `/api/v2/`
- Clients specify version in URL

---

## 🧪 Testing

### Using HTTP File

The project includes `Archu.Api.http` with 40+ example requests:

```http
### Login
POST https://localhost:7123/api/v1/authentication/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}

### Get Products (with auth)
GET https://localhost:7123/api/v1/products
Authorization: Bearer {{jwt_token}}
```

**Location:** `src/Archu.Api/Archu.Api.http`

### Using Scalar UI

1. Start the application
2. Navigate to https://localhost:7123/scalar/v1
3. Explore endpoints, try requests, view schemas
4. Click "Authorize" to add JWT token

### Using Postman/Insomnia

1. Import OpenAPI JSON: https://localhost:7123/openapi/v1.json
2. Set up environment variables for tokens
3. Use collection to test all endpoints

---

## 🔧 Configuration

### JWT Settings

Configure in `appsettings.json`:

```json
{
  "Jwt": {
    "Secret": "your-secret-key-min-32-chars",
    "Issuer": "Archu.Api",
    "Audience": "Archu.Clients",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

### Database Connection

```json
{
  "ConnectionStrings": {
    "archudb": "Server=(localdb)\\mssqllocaldb;Database=ArchuDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### CORS (for frontend integration)

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://localhost:7000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

app.UseCors("AllowFrontend");
```

---

## 📝 Best Practices

### Token Management

✅ **DO:**
- Store tokens securely (HttpOnly cookies, secure storage)
- Refresh tokens before expiration
- Clear tokens on logout
- Use HTTPS in production

❌ **DON'T:**
- Store tokens in localStorage (XSS vulnerability)
- Share tokens across devices/sessions
- Use expired tokens
- Send tokens in URL query parameters

### Error Handling

```csharp
try
{
    var response = await httpClient.GetAsync("/api/v1/products");
    
    if (response.StatusCode == HttpStatusCode.Unauthorized)
    {
        // Token expired - refresh token
        await RefreshTokenAsync();
        // Retry request
    }
    
    response.EnsureSuccessStatusCode();
    var data = await response.Content.ReadFromJsonAsync<ApiResponse<Product[]>>();
}
catch (HttpRequestException ex)
{
    // Handle network errors
}
```

### Pagination (Future Enhancement)

Currently, all products are returned. For large datasets, implement pagination:

```http
GET /api/v1/products?pageNumber=1&pageSize=20
```

---

## 🚨 Common Issues & Solutions

### Issue: 401 Unauthorized

**Cause:** Missing or expired token

**Solution:**
1. Check if token is included in Authorization header
2. Verify token format: `Bearer <token>`
3. Check token expiration
4. Use refresh token to get new access token

### Issue: 403 Forbidden

**Cause:** Insufficient permissions

**Solution:**
1. Verify user has required role (User, Manager, Admin)
2. Check authorization policy requirements
3. Contact admin to assign appropriate roles

### Issue: 409 Conflict (Product Update)

**Cause:** Optimistic concurrency conflict

**Solution:**
1. GET the product again to get latest RowVersion
2. Apply your changes to the new data
3. Retry PUT with updated RowVersion

### Issue: 400 Bad Request (Validation)

**Cause:** Invalid request data

**Solution:**
1. Check error message for specific validation failures
2. Verify required fields are present
3. Check data types and formats
4. Review password requirements

---

## 🔗 Related Documentation

- **Admin API Documentation**: `/docs/ADMIN_API_QUICK_REFERENCE.md`
- **HTTP Requests Guide**: `/docs/HTTP_REQUESTS_GUIDE.md`
- **OpenAPI Update Docs**: `/docs/OPENAPI_DOCUMENTATION_UPDATE.md`
- **Architecture**: `/docs/ARCHITECTURE.md`
- **GitHub Repository**: https://github.com/chethandvg/archu

---

## 📞 Support

### Questions or Issues?
- **GitHub Issues**: https://github.com/chethandvg/archu/issues
- **Email**: support@archu.com
- **Documentation**: Check `/docs` folder

### Contributing
Contributions are welcome! Please see `CONTRIBUTING.md` for guidelines.

---

**Version:** 1.0  
**Last Updated:** 2025-01-22  
**Status:** ✅ Production Ready

---

## 🎓 Quick Reference

### Authentication Endpoints
- `POST /api/v1/authentication/register` - Register new user
- `POST /api/v1/authentication/login` - Login
- `POST /api/v1/authentication/logout` - Logout
- `POST /api/v1/authentication/refresh-token` - Refresh tokens
- `POST /api/v1/authentication/change-password` - Change password
- `POST /api/v1/authentication/forgot-password` - Request password reset
- `POST /api/v1/authentication/reset-password` - Reset password
- `POST /api/v1/authentication/confirm-email` - Confirm email

### Product Endpoints
- `GET /api/v1/products` - Get all products
- `GET /api/v1/products/{id}` - Get product by ID
- `POST /api/v1/products` - Create product (Manager/Admin)
- `PUT /api/v1/products/{id}` - Update product (Manager/Admin)
- `DELETE /api/v1/products/{id}` - Delete product (Admin only)

### Health Endpoints
- `GET /health` - Full health status
- `GET /health/ready` - Readiness check
- `GET /health/live` - Liveness check

Happy Coding! 🚀
