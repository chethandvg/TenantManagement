# Archu.AdminApi.http - HTTP Requests Guide

## 📋 Overview

The `Archu.AdminApi.http` file contains comprehensive HTTP request examples for testing and interacting with the Archu Admin API using Visual Studio's built-in HTTP client.

---

## 🚀 Quick Start

### 1. **Prerequisites**
- Visual Studio 2022 or later
- Archu.AppHost running (starts all services)
- Database initialized (use initialization endpoint)

### 2. **Setup**
```bash
# Start the application
cd src/Archu.AppHost
dotnet run

# Admin API will be available at:
# HTTPS: https://localhost:7290
# HTTP:  http://localhost:5290
```

### 3. **Configure Variables**
Open `Archu.AdminApi.http` and update these variables:

```http
@jwt_token = your-actual-jwt-token-here
@user_id = actual-user-guid
@role_id = actual-role-guid
```

### 4. **Execute Requests**
- Click the "Send Request" link above each request
- View response in the Response pane
- Check status codes and response bodies

---

## 📚 Request Categories

### **1. Initialization (1 request)**
- ✅ Initialize System - One-time setup

### **2. Health Checks (3 requests)**
- ✅ Full Health Status
- ✅ Ready Check
- ✅ Live Check

### **3. Users (6 requests)**
- ✅ Get All Users (Paginated)
- ✅ Create User (Full)
- ✅ Create User (Minimal)
- ✅ Delete User

### **4. Roles (3 requests)**
- ✅ Get All Roles
- ✅ Create Custom Role (Full)
- ✅ Create Custom Role (Minimal)

### **5. User Roles (3 requests)**
- ✅ Get User Roles
- ✅ Assign Role to User
- ✅ Remove Role from User

### **6. Testing Scenarios (3 requests)**
- ✅ Create User and Assign Manager Role
- ✅ Get Manager Role ID
- ✅ Assign Manager Role

### **7. Error Scenarios (5 requests)**
- ✅ Administrator tries to assign SuperAdmin
- ✅ Try to delete yourself
- ✅ Remove your own SuperAdmin role
- ✅ Invalid authentication
- ✅ Missing authentication

### **8. Pagination Examples (4 requests)**
- ✅ First Page (10 items)
- ✅ Second Page
- ✅ Large Page Size
- ✅ Small Page Size

### **9. Advanced Scenarios (3 requests)**
- ✅ Bulk User Creation

### **10. HTTP Protocol Testing (2 requests)**
- ✅ HTTPS Endpoint
- ✅ HTTP Endpoint

**Total:** 31 comprehensive HTTP requests

---

## 🔐 Authentication Flow

### Step 1: Initialize System
```http
POST https://localhost:7290/api/v1/admin/initialization/initialize
Content-Type: application/json

{
  "userName": "superadmin",
  "email": "admin@example.com",
  "password": "SuperAdmin123!"
}
```

### Step 2: Get JWT Token
**Note:** Authentication is handled by a separate Auth API (not included in this file).

Example using Auth API:
```http
POST https://localhost:5001/api/v1/auth/login
Content-Type: application/json

{
  "email": "admin@example.com",
  "password": "SuperAdmin123!"
}

Response:
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2025-01-22T11:30:00Z"
  }
}
```

### Step 3: Update Token Variable
```http
@jwt_token = eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Step 4: Execute Authenticated Requests
```http
GET https://localhost:7290/api/v1/admin/users
Authorization: Bearer {{jwt_token}}
```

---

## 📖 Example Workflows

### Workflow 1: Create User and Assign Role

#### Step 1: Get All Roles (to find role IDs)
```http
### Request #9
GET https://localhost:7290/api/v1/admin/roles
Authorization: Bearer {{jwt_token}}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "manager-role-guid",
      "name": "Manager",
      "normalizedName": "MANAGER"
    }
  ]
}
```

#### Step 2: Create User
```http
### Request #6
POST https://localhost:7290/api/v1/admin/users
Authorization: Bearer {{jwt_token}}
Content-Type: application/json

{
  "userName": "john.doe",
  "email": "john@example.com",
  "password": "SecurePassword123!"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "new-user-guid",
    "userName": "john.doe",
    "email": "john@example.com"
  }
}
```

#### Step 3: Assign Manager Role
```http
### Request #13
POST https://localhost:7290/api/v1/admin/user-roles/assign
Authorization: Bearer {{jwt_token}}
Content-Type: application/json

{
  "userId": "new-user-guid",
  "roleId": "manager-role-guid"
}
```

---

### Workflow 2: Test Security Restrictions

#### Test 1: Administrator tries to assign SuperAdmin (Should Fail)
```http
### Request #18
POST https://localhost:7290/api/v1/admin/user-roles/assign
Authorization: Bearer {{administrator_token}}
Content-Type: application/json

{
  "userId": "some-user-guid",
  "roleId": "superadmin-role-guid"
}
```

**Expected Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Permission denied: Only SuperAdmin can assign the 'SuperAdmin' role..."
}
```

#### Test 2: Try to delete yourself (Should Fail)
```http
### Request #19
DELETE https://localhost:7290/api/v1/admin/users/your-own-user-id
Authorization: Bearer {{jwt_token}}
```

**Expected Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Security restriction: You cannot delete your own account..."
}
```

---

## 🎯 Tips & Best Practices

### **Variable Usage**
- Define common values at the top
- Reuse variables across requests
- Update tokens when they expire

### **Request Organization**
- Requests are grouped by functionality
- Comments explain each request
- Separators (`###`) between requests

### **Response Handling**
- Check status codes first
- Validate response body structure
- Look for error messages in failed requests

### **Testing Strategy**
1. Start with health checks
2. Initialize system if needed
3. Test read operations first (GET)
4. Then test write operations (POST, DELETE)
5. Verify security restrictions
6. Test edge cases and error scenarios

### **Common Issues**

#### Issue: 401 Unauthorized
- **Cause:** Missing or expired JWT token
- **Solution:** Get new token from Auth API

#### Issue: 403 Forbidden
- **Cause:** Insufficient permissions
- **Solution:** Check user role (SuperAdmin, Administrator, Manager)

#### Issue: 400 Bad Request
- **Cause:** Validation error or business rule violation
- **Solution:** Check error message, fix request body

#### Issue: 404 Not Found
- **Cause:** Resource doesn't exist (user, role)
- **Solution:** Verify IDs are correct, check if resource was deleted

---

## 📊 Response Status Codes

| Code | Meaning | Common Causes |
|------|---------|---------------|
| **200** | OK | Successful GET, DELETE |
| **201** | Created | Successful POST (user, role created) |
| **400** | Bad Request | Validation error, business rule violation, security restriction |
| **401** | Unauthorized | Missing token, expired token, invalid token |
| **403** | Forbidden | User doesn't have required role/permission |
| **404** | Not Found | User, role, or resource doesn't exist |
| **500** | Internal Server Error | Unexpected server error (check logs) |

---

## 🔍 Debugging Tips

### Enable Detailed Logging
In `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

### View Request/Response Details
Visual Studio shows:
- Request headers
- Request body
- Response status code
- Response headers
- Response body
- Response time

### Check Application Logs
```bash
# In AppHost terminal
# Look for:
# - "Admin {AdminUserId} attempting to assign role..."
# - "Role assignment denied: Admin..."
# - "User created with ID: {UserId}"
```

---

## 🔗 Related Resources

### Documentation
- **OpenAPI/Scalar UI**: https://localhost:7290/scalar/v1
- **OpenAPI JSON**: https://localhost:7290/openapi/v1.json
- **Full Documentation**: `/docs/OPENAPI_DOCUMENTATION_UPDATE.md`
- **Quick Reference**: `/docs/ADMIN_API_QUICK_REFERENCE.md`

### Source Code
- **Controllers**: `Archu.AdminApi/Controllers/`
- **Commands**: `Archu.Application/Admin/Commands/`
- **Contracts**: `Archu.Contracts/Admin/`

---

## 📝 Request Template

Use this template to create new requests:

```http
### Description of the request
# Required Role: SuperAdmin, Administrator, etc.
# Notes: Any special requirements or restrictions
GET/POST/DELETE {{Archu.AdminApi_HostAddress}}/api/v1/admin/endpoint
Authorization: Bearer {{jwt_token}}
Content-Type: application/json

{
  "property": "value"
}

###
```

---

## ✅ Testing Checklist

- [ ] System initialized successfully
- [ ] JWT token obtained and configured
- [ ] Health checks return 200 OK
- [ ] Can retrieve users list
- [ ] Can create new user
- [ ] Can retrieve roles list
- [ ] Can create custom role
- [ ] Can get user's roles
- [ ] Can assign role to user
- [ ] Can remove role from user
- [ ] Security restrictions work (Administrator cannot assign SuperAdmin)
- [ ] Cannot delete yourself
- [ ] Cannot delete last SuperAdmin
- [ ] Cannot remove own privileged roles
- [ ] Pagination works correctly
- [ ] Error responses are informative

---

## 🎓 Learning Resources

### Visual Studio HTTP Client
- [.http files documentation](https://learn.microsoft.com/en-us/aspnet/core/test/http-files)
- Variables and environments
- Request chaining
- Response validation

### REST API Testing
- HTTP methods (GET, POST, PUT, DELETE)
- Status codes and their meanings
- Headers (Authorization, Content-Type)
- Request/Response bodies (JSON)

---

**Version:** 1.0  
**Last Updated:** 2025-01-22  
**Total Requests:** 31  
**Status:** ✅ Complete

Happy Testing! 🚀
