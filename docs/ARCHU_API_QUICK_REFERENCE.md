# Archu API - Quick Reference Guide

## 🚀 Base URLs

```
HTTPS: https://localhost:7123
HTTP:  http://localhost:5268
```

---

## 🔐 Authentication Endpoints

### Register
```http
POST /api/v1/authentication/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "userName": "johndoe"
}
```
**Auth Required:** ❌ No  
**Response:** JWT tokens + user info

---

### Login
```http
POST /api/v1/authentication/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```
**Auth Required:** ❌ No  
**Response:** JWT tokens + user info

---

### Refresh Token
```http
POST /api/v1/authentication/refresh-token
Content-Type: application/json

{
  "refreshToken": "your-refresh-token"
}
```
**Auth Required:** ❌ No  
**Response:** New JWT tokens

---

### Logout
```http
POST /api/v1/authentication/logout
Authorization: Bearer {token}
```
**Auth Required:** ✅ Yes  
**Response:** Success message

---

### Change Password
```http
POST /api/v1/authentication/change-password
Authorization: Bearer {token}
Content-Type: application/json

{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword456!"
}
```
**Auth Required:** ✅ Yes  
**Response:** Success message

---

### Forgot Password
```http
POST /api/v1/authentication/forgot-password
Content-Type: application/json

{
  "email": "user@example.com"
}
```
**Auth Required:** ❌ No  
**Response:** Always success (security)

---

### Reset Password
```http
POST /api/v1/authentication/reset-password
Content-Type: application/json

{
  "email": "user@example.com",
  "resetToken": "token-from-email",
  "newPassword": "NewPassword123!"
}
```
**Auth Required:** ❌ No  
**Response:** Success message

---

### Confirm Email
```http
POST /api/v1/authentication/confirm-email
Content-Type: application/json

{
  "userId": "guid-here",
  "confirmationToken": "token-from-email"
}
```
**Auth Required:** ❌ No  
**Response:** Success message

---

## 📦 Product Endpoints

### Get All Products
```http
GET /api/v1/products
Authorization: Bearer {token}
```
**Auth Required:** ✅ Yes  
**Required Role:** User, Manager, Admin  
**Response:** Array of products

---

### Get Product by ID
```http
GET /api/v1/products/{id}
Authorization: Bearer {token}
```
**Auth Required:** ✅ Yes  
**Required Role:** User, Manager, Admin  
**Response:** Single product

---

### Create Product
```http
POST /api/v1/products
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Product Name",
  "price": 19.99
}
```
**Auth Required:** ✅ Yes  
**Required Role:** Manager, Admin  
**Response:** Created product (201)

---

### Update Product
```http
PUT /api/v1/products/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "id": "guid-here",
  "name": "Updated Name",
  "price": 24.99,
  "rowVersion": "AAAAAAAAAAA="
}
```
**Auth Required:** ✅ Yes  
**Required Role:** Manager, Admin  
**Response:** Updated product  
**Note:** Must provide current RowVersion

---

### Delete Product
```http
DELETE /api/v1/products/{id}
Authorization: Bearer {token}
```
**Auth Required:** ✅ Yes  
**Required Role:** Admin only  
**Response:** Success message

---

## 🏥 Health Check Endpoints

### Full Health Status
```http
GET /health
```
**Auth Required:** ❌ No  
**Response:** Complete health report with all checks

---

### Readiness Check
```http
GET /health/ready
```
**Auth Required:** ❌ No  
**Response:** Ready status

---

### Liveness Check
```http
GET /health/live
```
**Auth Required:** ❌ No  
**Response:** Live status

---

## 🔑 Token Information

### Access Token
- **Lifetime:** 1 hour (default)
- **Usage:** Include in `Authorization: Bearer {token}` header
- **Format:** JWT (JSON Web Token)

### Refresh Token
- **Lifetime:** 7 days (default)
- **Usage:** Used to get new access token when expired
- **Storage:** Secure storage (not localStorage)

---

## 🛡️ Authorization Policies

| Policy | Roles | Operations |
|--------|-------|------------|
| `CanReadProducts` | User, Manager, Admin | GET products |
| `CanCreateProducts` | Manager, Admin | POST products |
| `CanUpdateProducts` | Manager, Admin | PUT products |
| `CanDeleteProducts` | Admin | DELETE products |

---

## 📊 Response Format

### Success (200 OK)
```json
{
  "success": true,
  "message": "Operation successful",
  "data": { /* result data */ }
}
```

### Error (4xx, 5xx)
```json
{
  "success": false,
  "message": "Error description",
  "data": null
}
```

---

## 🚨 Common Status Codes

| Code | Meaning | Common Cause |
|------|---------|--------------|
| 200 | OK | Successful request |
| 201 | Created | Resource created |
| 400 | Bad Request | Validation error |
| 401 | Unauthorized | Missing/invalid token |
| 403 | Forbidden | Insufficient permissions |
| 404 | Not Found | Resource doesn't exist |
| 409 | Conflict | Concurrency conflict |
| 500 | Server Error | Internal error |

---

## 🔄 Common Workflows

### 1. Complete Registration Flow
```
1. POST /api/v1/authentication/register
   → Get tokens immediately
2. (Optional) POST /api/v1/authentication/confirm-email
   → Verify email
3. Use access token for API calls
```

### 2. Login Flow
```
1. POST /api/v1/authentication/login
   → Get access token + refresh token
2. Use access token in Authorization header
3. When token expires (1 hour):
   POST /api/v1/authentication/refresh-token
   → Get new tokens
```

### 3. Password Reset Flow
```
1. POST /api/v1/authentication/forgot-password
   → Request reset token
2. Check email for token
3. POST /api/v1/authentication/reset-password
   → Reset with token and new password
```

### 4. Token Refresh Flow
```
1. API returns 401 Unauthorized
2. POST /api/v1/authentication/refresh-token
   → Get new access token
3. Retry original request with new token
```

### 5. Product Management Flow
```
1. Login to get token
2. GET /api/v1/products
   → View all products
3. POST /api/v1/products (Manager/Admin)
   → Create new product
4. PUT /api/v1/products/{id} (Manager/Admin)
   → Update product (with RowVersion)
5. DELETE /api/v1/products/{id} (Admin only)
   → Delete product
```

---

## 🧪 Testing with Archu.Api.http

Located at: `src/Archu.Api/Archu.Api.http`

**Contains 40+ Example Requests:**
- 10 Authentication requests
- 3 Password management requests
- 1 Email verification request
- 5 Testing scenarios
- 11 Error scenarios
- 5 Product API examples
- 2 Protocol tests
- 4 Bulk registration examples

**Usage:**
1. Open in Visual Studio
2. Update variables at top (tokens, IDs)
3. Click "Send Request" links
4. View responses in Response pane

---

## 📖 Documentation Resources

- **Scalar UI**: https://localhost:7123/scalar/v1
- **OpenAPI JSON**: https://localhost:7123/openapi/v1.json
- **Full Documentation**: `/docs/ARCHU_API_DOCUMENTATION.md`
- **HTTP Examples**: `/src/Archu.Api/Archu.Api.http`
- **Admin API Guide**: `/docs/ADMIN_API_QUICK_REFERENCE.md`

---

## 💡 Tips & Best Practices

### Token Management
✅ Store tokens securely (HttpOnly cookies, secure storage)  
✅ Refresh tokens before expiration  
✅ Clear tokens on logout  
✅ Use HTTPS in production  

❌ Don't store tokens in localStorage  
❌ Don't share tokens across devices  
❌ Don't send tokens in URL parameters  

### Error Handling
```csharp
// Check for 401 and refresh token
if (response.StatusCode == 401)
{
    await RefreshTokenAsync();
    // Retry request
}

// Check for 409 and reload data
if (response.StatusCode == 409)
{
    await ReloadProductAsync();
    // Retry with new RowVersion
}
```

### Concurrency Control
```
1. GET product → Save RowVersion
2. Modify data locally
3. PUT with original RowVersion
4. If 409 Conflict:
   → GET fresh data
   → Merge changes
   → Retry with new RowVersion
```

---

## 🔧 Configuration

### appsettings.json
```json
{
  "Jwt": {
    "Secret": "your-secret-key-min-32-chars",
    "Issuer": "Archu.Api",
    "Audience": "Archu.Clients",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "ConnectionStrings": {
    "archudb": "Server=(localdb)\\mssqllocaldb;Database=ArchuDb;..."
  }
}
```

---

## 🎯 Quick Start Checklist

- [ ] Start Archu.AppHost project
- [ ] Verify APIs running (check health endpoints)
- [ ] Register new user
- [ ] Copy access token
- [ ] Test product endpoints with token
- [ ] Try token refresh flow
- [ ] Test password change
- [ ] Explore Scalar UI documentation

---

## 📞 Support

**GitHub**: https://github.com/chethandvg/archu  
**Issues**: https://github.com/chethandvg/archu/issues  
**Email**: support@archu.com  

---

**Version:** 1.0  
**Last Updated:** 2025-01-22  
**Status:** ✅ Production Ready

---

## 🎓 Example: Complete API Flow

```http
### 1. Register
POST https://localhost:7123/api/v1/authentication/register
Content-Type: application/json

{
  "email": "demo@example.com",
  "password": "Demo123!",
  "userName": "demouser"
}

# Save token from response: eyJhbGciOiJIUzI1NiIs...

### 2. Get Products
GET https://localhost:7123/api/v1/products
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...

### 3. Create Product (need Manager/Admin role)
POST https://localhost:7123/api/v1/products
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "name": "Demo Product",
  "price": 99.99
}

### 4. Update Product
PUT https://localhost:7123/api/v1/products/product-id-here
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "id": "product-id-here",
  "name": "Updated Demo Product",
  "price": 89.99,
  "rowVersion": "AAAAAAAAAAA="
}

### 5. Logout
POST https://localhost:7123/api/v1/authentication/logout
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

Happy Coding! 🚀
