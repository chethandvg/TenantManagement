# Admin API Endpoints Quick Reference

## 🔗 Base URL
- **Development (HTTPS)**: `https://localhost:7290`
- **Development (HTTP)**: `http://localhost:5290`

## 📚 Documentation
- **Scalar UI**: https://localhost:7290/scalar/v1
- **OpenAPI JSON**: https://localhost:7290/openapi/v1.json

---

## 🔐 Authentication
All endpoints (except initialization) require JWT Bearer token:
```
Authorization: Bearer <your-jwt-token>
```

---

## 🎯 Endpoints

### 1️⃣ Initialization

#### Initialize System
```http
POST /api/v1/admin/initialization/initialize
Content-Type: application/json

{
  "userName": "superadmin",
  "email": "admin@example.com",
  "password": "SuperAdmin123!"
}
```
- ✅ Anonymous access (one-time only)
- Creates 5 roles + SuperAdmin user

---

### 2️⃣ Users

#### Get All Users
```http
GET /api/v1/admin/users?pageNumber=1&pageSize=10
Authorization: Bearer <token>
```
- 🎭 **Roles**: SuperAdmin, Administrator, Manager
- 📄 Pagination supported

#### Create User
```http
POST /api/v1/admin/users
Authorization: Bearer <token>
Content-Type: application/json

{
  "userName": "john.doe",
  "email": "john@example.com",
  "password": "SecurePass123!",
  "phoneNumber": "+1234567890",
  "emailConfirmed": false,
  "twoFactorEnabled": false
}
```
- 🎭 **Roles**: SuperAdmin, Administrator, Manager
- 🔒 Password: min 8 chars, uppercase, lowercase, digit, special char

#### Delete User
```http
DELETE /api/v1/admin/users/{userId}
Authorization: Bearer <token>
```
- 🎭 **Roles**: SuperAdmin, Administrator
- ❌ Cannot delete yourself
- ❌ Cannot delete last SuperAdmin

---

### 3️⃣ Roles

#### Get All Roles
```http
GET /api/v1/admin/roles
Authorization: Bearer <token>
```
- 🎭 **Roles**: SuperAdmin, Administrator, Manager
- Returns all system + custom roles

#### Create Role
```http
POST /api/v1/admin/roles
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "ContentEditor",
  "description": "Can edit content"
}
```
- 🎭 **Roles**: SuperAdmin, Administrator
- Name: 3-50 chars, unique

---

### 4️⃣ User Roles

#### Get User Roles
```http
GET /api/v1/admin/user-roles/{userId}
Authorization: Bearer <token>
```
- 🎭 **Roles**: SuperAdmin, Administrator, Manager

#### Assign Role
```http
POST /api/v1/admin/user-roles/assign
Authorization: Bearer <token>
Content-Type: application/json

{
  "userId": "user-guid",
  "roleId": "role-guid"
}
```
- 🎭 **Roles**: SuperAdmin, Administrator
- **SuperAdmin** can assign: Any role ✅
- **Administrator** can assign: User, Manager, Guest ✅
- **Administrator** cannot assign: SuperAdmin ❌, Administrator ❌

#### Remove Role
```http
DELETE /api/v1/admin/user-roles/{userId}/roles/{roleId}
Authorization: Bearer <token>
```
- 🎭 **Roles**: SuperAdmin, Administrator
- **SuperAdmin** can remove: Any role ✅ (except own SuperAdmin, last SuperAdmin)
- **Administrator** can remove: User, Manager, Guest ✅
- **Administrator** cannot remove: SuperAdmin ❌, Administrator ❌
- ❌ Cannot remove your own privileged roles
- ❌ Cannot remove last SuperAdmin

---

### 5️⃣ Health

#### Health Check
```http
GET /health
```
- ✅ No authentication required

#### Ready Check
```http
GET /health/ready
```

#### Live Check
```http
GET /health/live
```

---

## 🎭 System Roles

| Role | Description | Can Manage |
|------|-------------|------------|
| **SuperAdmin** | Full system access | Everything including SuperAdmin/Administrator |
| **Administrator** | Full access except SuperAdmin management | Users, Roles (User/Manager/Guest) |
| **Manager** | Team management | View users/roles |
| **User** | Standard access | Basic features |
| **Guest** | Read-only | Limited viewing |

---

## 🔒 Permission Matrix

### Role Assignment

| Actor | SuperAdmin | Administrator | Manager | User | Guest |
|-------|-----------|--------------|---------|------|-------|
| **SuperAdmin** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Administrator** | ❌ | ❌ | ✅ | ✅ | ✅ |

### Role Removal

| Actor | SuperAdmin | Administrator | Manager | User | Guest |
|-------|-----------|--------------|---------|------|-------|
| **SuperAdmin** | ✅* | ✅ | ✅ | ✅ | ✅ |
| **Administrator** | ❌ | ❌** | ✅ | ✅ | ✅ |

*Except own or last SuperAdmin  
**Except own Administrator

---

## 📊 Response Codes

| Code | Meaning |
|------|---------|
| **200** | Success |
| **201** | Created |
| **400** | Bad Request (validation, business rules) |
| **401** | Unauthorized (no/invalid token) |
| **403** | Forbidden (insufficient permissions) |
| **404** | Not Found |

---

## 🚀 Quick Start

### 1. Initialize System
```bash
curl -X POST https://localhost:7290/api/v1/admin/initialization/initialize \
  -H "Content-Type: application/json" \
  -d '{"userName":"superadmin","email":"admin@test.com","password":"SuperAdmin123!"}'
```

### 2. Login (Auth API - separate)
```bash
# Get JWT token from Auth API
```

### 3. Create User
```bash
curl -X POST https://localhost:7290/api/v1/admin/users \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"userName":"john","email":"john@test.com","password":"Pass123!","phoneNumber":"+1234567890","emailConfirmed":false,"twoFactorEnabled":false}'
```

### 4. Assign Manager Role
```bash
curl -X POST https://localhost:7290/api/v1/admin/user-roles/assign \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"userId":"<user-id>","roleId":"<manager-role-id>"}'
```

---

## 💡 Tips

- 🔐 Always use HTTPS in production
- 🔑 Store JWT tokens securely
- 🗓️ Tokens expire after 1 hour (configurable)
- 🔄 Use refresh tokens for long sessions
- 📝 Check Scalar UI for interactive testing
- 🐛 Enable debug logs in Development
- ✅ Validate input before sending requests
- 🔒 Never commit passwords/tokens to git

---

## 📞 Support

- 📖 [Full Documentation](docs/)
- 🐛 [Report Issues](https://github.com/chethandvg/archu/issues)
- 💬 [Discussions](https://github.com/chethandvg/archu/discussions)

---

**Version:** 1.0  
**Last Updated:** 2025-01-22
