# Authentication & Authorization Guide

## 🔐 Authentication (JWT)

### Quick Setup

**1. Configuration (appsettings.json)**
```json
{
  "Jwt": {
    "Secret": "Your-Secret-Key-At-Least-32-Characters-Long!",
    "Issuer": "https://localhost:7001",
    "Audience": "https://localhost:7001",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

**2. Authentication Endpoints**

| Endpoint | Method | Auth | Purpose |
|----------|--------|------|---------|
| `/api/v1/authentication/register` | POST | No | Create account |
| `/api/v1/authentication/login` | POST | No | Get JWT tokens |
| `/api/v1/authentication/refresh-token` | POST | No | Renew token |
| `/api/v1/authentication/logout` | POST | Yes | Revoke token |
| `/api/v1/authentication/change-password` | POST | Yes | Change password |

**3. Quick Start**
```bash
# Register
curl -X POST http://localhost:5000/api/v1/authentication/register \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Pass123!","userName":"john"}'

# Login
curl -X POST http://localhost:5000/api/v1/authentication/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Pass123!"}'

# Use token in requests
curl -X GET http://localhost:5000/api/v1/products \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

---

## 🛡️ Authorization (Roles & Policies)

### Role Hierarchy

| Role | Read | Create | Update | Delete |
|------|------|--------|--------|--------|
| **Admin** | ✅ | ✅ | ✅ | ✅ |
| **Manager** | ✅ | ✅ | ✅ | ❌ |
| **User** | ✅ | ❌ | ❌ | ❌ |

### Using Roles in Controllers

```csharp
// Require specific role
[Authorize(Roles = Roles.Admin)]
public IActionResult AdminOnly() { }

// Multiple roles (OR logic)
[Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
public IActionResult ManagementOnly() { }

// Role + Permission (AND logic)
[Authorize(Roles = Roles.Admin)]
[Authorize(Policy = AuthorizationPolicies.CanDeleteProducts)]
public IActionResult DeleteProduct() { }
```

### Available Policies

**Role-Based:**
- `RequireAdminRole`
- `RequireManagerRole`
- `RequireUserRole`
- `RequireAdminOrManager`

**Permission-Based:**
- `CanReadProducts`
- `CanCreateProducts`
- `CanUpdateProducts`
- `CanDeleteProducts`
- `CanManageProducts`

**Claim-Based:**
- `RequireEmailVerified`
- `RequireTwoFactorEnabled`

---

## 🔧 Implementation Details

### JWT Token Structure
```json
{
  "sub": "user-id",
  "email": "user@example.com",
  "unique_name": "username",
  "role": ["User", "Manager"],
  "permission": ["products:read", "products:create"],
  "email_verified": "true",
  "exp": 1706000000
}
```

### Adding Claims to Tokens
```csharp
// In JwtTokenService.GenerateAccessToken
var claims = new List<Claim>
{
    new(ClaimTypes.NameIdentifier, userId),
    new(ClaimTypes.Email, email),
    new(ClaimTypes.Role, role),
    new(CustomClaimTypes.Permission, permission),
    new(CustomClaimTypes.EmailVerified, emailVerified.ToString())
};
```

### Custom Authorization Requirements
```csharp
// Check permission
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
}

// Handler
public class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim(c => 
            c.Type == "permission" && c.Value == requirement.Permission))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
```

---

## 🧪 Testing

### Test with Different Roles

**User Role (Read Only):**
```bash
# ✅ Can read
curl -X GET http://localhost:5000/api/v1/products \
  -H "Authorization: Bearer $USER_TOKEN"

# ❌ Cannot create (403)
curl -X POST http://localhost:5000/api/v1/products \
  -H "Authorization: Bearer $USER_TOKEN"
```

**Manager Role:**
```bash
# ✅ Can create
curl -X POST http://localhost:5000/api/v1/products \
  -H "Authorization: Bearer $MANAGER_TOKEN"

# ❌ Cannot delete (403)
curl -X DELETE http://localhost:5000/api/v1/products/id \
  -H "Authorization: Bearer $MANAGER_TOKEN"
```

**Admin Role (Full Access):**
```bash
# ✅ Can delete
curl -X DELETE http://localhost:5000/api/v1/products/id \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

---

## 🔍 Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| 401 Unauthorized | No/invalid token | Login again |
| 403 Forbidden | Insufficient role/permission | Request role upgrade |
| Token expired | Access token expired (60min) | Use refresh token |
| Invalid signature | Secret mismatch | Check JWT config |

---

## 📋 Security Checklist

- [x] JWT secret is 32+ characters
- [x] HTTPS enabled in production
- [x] Tokens expire appropriately
- [x] Refresh tokens stored securely
- [x] Role-based access implemented
- [x] Permission-based policies configured
- [ ] Rate limiting on auth endpoints
- [ ] Account lockout after failed attempts
- [ ] Two-factor authentication (optional)

---

## 🎯 Best Practices

1. **Principle of Least Privilege** - Assign minimum required roles
2. **Defense in Depth** - Use role + permission authorization
3. **Short Token Lifetime** - Keep access tokens short-lived (60 min)
4. **Secure Storage** - Store JWT secret in Azure Key Vault
5. **Audit Logging** - Log all auth failures
6. **Regular Reviews** - Audit user roles periodically

---

## 📚 Related Files

**Configuration:**
- `src/Archu.Api/Program.cs` - JWT & Authorization setup
- `src/Archu.Api/appsettings.json` - JWT configuration

**Controllers:**
- `src/Archu.Api/Controllers/AuthenticationController.cs` - Auth endpoints
- `src/Archu.Api/Controllers/ProductsController.cs` - Protected endpoints

**Authorization:**
- `src/Archu.Api/Authorization/AuthorizationPolicies.cs` - Policy definitions
- `src/Archu.Api/Authorization/Requirements/` - Custom requirements

**Infrastructure:**
- `src/Archu.Infrastructure/Authentication/JwtTokenService.cs` - Token generation
- `src/Archu.Infrastructure/Authentication/PasswordHasher.cs` - Password hashing

---

**Last Updated**: 2025-01-22
