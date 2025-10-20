# Authorization Quick Reference

## 🔐 Available Policies

### Role-Based
| Policy | Required Role | Use Case |
|--------|--------------|----------|
| `RequireAdminRole` | Admin | Full system access |
| `RequireManagerRole` | Manager | Management operations |
| `RequireUserRole` | User | Standard user access |
| `RequireAdminOrManager` | Admin OR Manager | Elevated operations |

### Permission-Based (Products)
| Policy | Permission | Use Case |
|--------|-----------|----------|
| `CanCreateProducts` | products:create | Create products |
| `CanReadProducts` | products:read | View products |
| `CanUpdateProducts` | products:update | Modify products |
| `CanDeleteProducts` | products:delete | Delete products |
| `CanManageProducts` | products:manage | Full product CRUD |

### Claim-Based
| Policy | Claim | Use Case |
|--------|-------|----------|
| `RequireEmailVerified` | email_verified=true | Email confirmed operations |
| `RequireTwoFactorEnabled` | two_factor_enabled=true | Sensitive operations |

## 💻 Quick Usage

### Basic Authorization
```csharp
[Authorize(Policy = AuthorizationPolicies.RequireAdminRole)]
public IActionResult AdminOnlyEndpoint() { }
```

### Multiple Policies
```csharp
[Authorize(Policy = AuthorizationPolicies.CanUpdateProducts)]
[Authorize(Policy = AuthorizationPolicies.RequireEmailVerified)]
public IActionResult UpdateProduct() { }
```

### Check in Code
```csharp
if (!_currentUser.IsInRole(Roles.Admin))
{
    return Forbid();
}
```

## 🔧 Adding Claims to Token

### Update JwtTokenService.GenerateAccessToken
```csharp
// Add role claims
foreach (var role in roles)
{
    claims.Add(new Claim(ClaimTypes.Role, role));
}

// Add permission claims
foreach (var permission in permissions)
{
    claims.Add(new Claim(CustomClaimTypes.Permission, permission));
}

// Add custom claims
claims.Add(new Claim(CustomClaimTypes.EmailVerified, emailVerified.ToString()));
```

## 📋 Common Patterns

### Admin OR Owner
```csharp
if (!_currentUser.IsInRole(Roles.Admin) && 
    resource.OwnerId != _currentUser.UserId)
{
    return Forbid();
}
```

### Verified Email Required
```csharp
[Authorize(Policy = AuthorizationPolicies.RequireEmailVerified)]
public IActionResult SensitiveOperation() { }
```

## 🚀 Testing

### Get Token
```bash
TOKEN=$(curl -X POST http://localhost:5000/api/v1/authentication/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Admin123!"}' \
  | jq -r '.data.accessToken')
```

### Test Endpoint
```bash
curl -X GET http://localhost:5000/api/v1/products \
  -H "Authorization: Bearer $TOKEN"
```

## ⚠️ Common Issues

| Issue | Solution |
|-------|----------|
| 403 Forbidden | Check token has required role/permission claims |
| 401 Unauthorized | Token missing or expired |
| Policy not working | Verify `UseAuthentication()` before `UseAuthorization()` |

## 📚 Full Documentation
See [AUTHORIZATION_POLICIES.md](./AUTHORIZATION_POLICIES.md) for complete details.

---

**Quick Tip**: Use policy constants, never magic strings!
```csharp
// ✅ Good
[Authorize(Policy = AuthorizationPolicies.CanCreateProducts)]

// ❌ Bad
[Authorize(Policy = "CanCreateProducts")]
```
