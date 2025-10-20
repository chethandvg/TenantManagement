# Authorization Policies Implementation

## Overview
Comprehensive authorization system for the Archu API using role-based, claim-based, and permission-based policies.

## What Was Implemented

### 1. Authorization Policy Infrastructure

#### Files Created
- ✅ `src/Archu.Api/Authorization/AuthorizationPolicies.cs` - Policy name constants and definitions
- ✅ `src/Archu.Api/Authorization/Requirements/AuthorizationRequirements.cs` - Custom authorization requirements and handlers
- ✅ `src/Archu.Api/Authorization/AuthorizationPolicyExtensions.cs` - Policy configuration extension methods

### 2. Authorization Types

#### Role-Based Authorization
Standard role-based access control using predefined roles.

**Roles Defined:**
- `Admin` - Full system access
- `Manager` - Elevated access for managing resources
- `User` - Standard user access

**Policies:**
- `RequireAdminRole` - Requires Admin role
- `RequireManagerRole` - Requires Manager role
- `RequireUserRole` - Requires User role
- `RequireAdminOrManager` - Requires Admin OR Manager role

#### Permission-Based Authorization
Fine-grained access control using permission claims.

**Permission Format:** `{resource}:{action}`

**Product Permissions:**
- `products:create` - Can create products
- `products:read` - Can read products
- `products:update` - Can update products
- `products:delete` - Can delete products
- `products:manage` - Can perform all product operations

**Policies:**
- `CanCreateProducts` - Permission to create products
- `CanReadProducts` - Permission to read products
- `CanUpdateProducts` - Permission to update products
- `CanDeleteProducts` - Permission to delete products
- `CanManageProducts` - Permission to manage all product operations

#### Claim-Based Authorization
Authorization based on specific user claims.

**Custom Claims:**
- `email_verified` - Email verification status
- `two_factor_enabled` - 2FA enablement status
- `permission` - Permission claims
- `department` - User department
- `employee_id` - Employee identifier

**Policies:**
- `RequireEmailVerified` - Requires verified email
- `RequireTwoFactorEnabled` - Requires 2FA enabled
- `AuthenticatedWithVerifiedEmail` - Authenticated + verified email

### 3. Custom Authorization Requirements

#### PermissionRequirement
Checks if user has a specific permission claim.

```csharp
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
}
```

**Handler:** `PermissionRequirementHandler`
- Checks for permission claim
- Logs authorization attempts
- Succeeds if permission claim matches

#### EmailVerifiedRequirement
Checks if user's email is verified.

```csharp
public class EmailVerifiedRequirement : IAuthorizationRequirement
{
}
```

**Handler:** `EmailVerifiedRequirementHandler`
- Checks `email_verified` claim
- Requires boolean true value

#### TwoFactorEnabledRequirement
Checks if user has 2FA enabled.

```csharp
public class TwoFactorEnabledRequirement : IAuthorizationRequirement
{
}
```

**Handler:** `TwoFactorEnabledRequirementHandler`
- Checks `two_factor_enabled` claim
- Requires boolean true value

#### MinimumRoleRequirement
Checks if user has any of the specified roles.

```csharp
public class MinimumRoleRequirement : IAuthorizationRequirement
{
    public string[] Roles { get; }
}
```

**Handler:** `MinimumRoleRequirementHandler`
- Checks multiple roles
- Succeeds if user has at least one role

## Usage Examples

### 1. Protect Controller Endpoints

#### Basic Role-Based Authorization
```csharp
[HttpDelete("{id:guid}")]
[Authorize(Policy = AuthorizationPolicies.RequireAdminRole)]
public async Task<IActionResult> DeleteProduct(Guid id)
{
    // Only admins can delete
}
```

#### Permission-Based Authorization
```csharp
[HttpPost]
[Authorize(Policy = AuthorizationPolicies.CanCreateProducts)]
public async Task<IActionResult> CreateProduct(CreateProductRequest request)
{
    // Requires products:create permission
}
```

#### Multiple Roles
```csharp
[HttpGet("reports")]
[Authorize(Policy = AuthorizationPolicies.RequireAdminOrManager)]
public async Task<IActionResult> GetReports()
{
    // Admins OR Managers can access
}
```

### 2. Check Authorization in Code

#### Using IAuthorizationService
```csharp
public class ProductService
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUser _currentUser;

    public async Task<bool> CanDeleteProduct(Guid productId)
    {
        var user = // get ClaimsPrincipal
        var resource = // get product
        
        var result = await _authorizationService.AuthorizeAsync(
            user,
            resource,
            AuthorizationPolicies.CanDeleteProducts);
        
        return result.Succeeded;
    }
}
```

#### Using ICurrentUser
```csharp
public class ProductController
{
    private readonly ICurrentUser _currentUser;

    public async Task<IActionResult> UpdateProduct()
    {
        if (!_currentUser.IsInRole(Roles.Admin))
        {
            return Forbid();
        }

        // Update logic
    }
}
```

### 3. Conditional Authorization
```csharp
[HttpPut("{id:guid}")]
[Authorize]
public async Task<IActionResult> UpdateProduct(Guid id, UpdateProductRequest request)
{
    var product = await _productRepository.GetByIdAsync(id);
    
    // Users can update their own products, but admins can update any
    if (!_currentUser.IsInRole(Roles.Admin) && 
        product.CreatedBy != _currentUser.UserId)
    {
        return Forbid("You can only update your own products");
    }

    // Update logic
}
```

## Adding Claims to JWT Tokens

To use permission-based and claim-based authorization, add claims when generating tokens:

### In JwtTokenService (Already Updated)
```csharp
public string GenerateAccessToken(
    string userId,
    string email,
    string userName,
    IEnumerable<string> roles,
    IEnumerable<string> permissions = null,
    bool emailVerified = false,
    bool twoFactorEnabled = false)
{
    var claims = new List<Claim>
    {
        // Standard claims
        new(JwtRegisteredClaimNames.Sub, userId),
        new(JwtRegisteredClaimNames.Email, email),
        new(ClaimTypes.NameIdentifier, userId),
        new(ClaimTypes.Name, userName),
        new(ClaimTypes.Email, email),
    };

    // Add role claims
    foreach (var role in roles)
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
        claims.Add(new Claim("role", role));
    }

    // Add permission claims
    if (permissions != null)
    {
        foreach (var permission in permissions)
        {
            claims.Add(new Claim(CustomClaimTypes.Permission, permission));
        }
    }

    // Add custom claims
    claims.Add(new Claim(CustomClaimTypes.EmailVerified, emailVerified.ToString()));
    claims.Add(new Claim(CustomClaimTypes.TwoFactorEnabled, twoFactorEnabled.ToString()));

    // Token generation logic...
}
```

### In AuthenticationService
```csharp
public async Task<Result<AuthenticationResult>> LoginAsync(
    string email,
    string password,
    CancellationToken cancellationToken = default)
{
    var user = await _userRepository.GetByEmailAsync(email);
    
    // Get user roles
    var roles = await _userRoleRepository.GetRolesByUserIdAsync(user.Id);
    
    // Get user permissions (implement this)
    var permissions = await GetUserPermissions(user.Id);
    
    // Generate token with roles, permissions, and claims
    var accessToken = _jwtTokenService.GenerateAccessToken(
        user.Id.ToString(),
        user.Email,
        user.UserName,
        roles.Select(r => r.Name),
        permissions,
        user.EmailConfirmed,
        user.TwoFactorEnabled);
    
    // Return authentication result
}
```

## Permission Management

### Assigning Permissions to Users

#### Option 1: Direct User Permissions
```csharp
public async Task AssignPermissionToUser(string userId, string permission)
{
    // Add permission claim to user
    var userClaim = new UserClaim
    {
        UserId = userId,
        ClaimType = CustomClaimTypes.Permission,
        ClaimValue = permission
    };
    
    await _userClaimRepository.AddAsync(userClaim);
    await _unitOfWork.SaveChangesAsync();
}
```

#### Option 2: Role-Based Permissions
```csharp
public async Task AssignPermissionsToRole(string roleName, IEnumerable<string> permissions)
{
    var role = await _roleRepository.GetByNameAsync(roleName);
    
    foreach (var permission in permissions)
    {
        var roleClaim = new RoleClaim
        {
            RoleId = role.Id,
            ClaimType = CustomClaimTypes.Permission,
            ClaimValue = permission
        };
        
        await _roleClaimRepository.AddAsync(roleClaim);
    }
    
    await _unitOfWork.SaveChangesAsync();
}

// Example: Assign all product permissions to Admin role
await AssignPermissionsToRole(Roles.Admin, new[]
{
    Permissions.ProductsCreate,
    Permissions.ProductsRead,
    Permissions.ProductsUpdate,
    Permissions.ProductsDelete,
    Permissions.ProductsManage
});
```

## Testing Authorization

### Using Scalar UI
1. Login and get JWT token
2. Click "Authorize" button
3. Enter: `Bearer {your-token}`
4. Test protected endpoints

### Using curl
```bash
# Login
TOKEN=$(curl -X POST https://localhost:7001/api/v1/authentication/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Admin123!"}' \
  | jq -r '.data.accessToken')

# Test protected endpoint
curl -X GET https://localhost:7001/api/v1/products \
  -H "Authorization: Bearer $TOKEN"

# Test forbidden endpoint (should return 403)
curl -X DELETE https://localhost:7001/api/v1/products/some-guid \
  -H "Authorization: Bearer $TOKEN"
```

## Authorization Flow

```
1. Request arrives with JWT token
   ↓
2. JWT Bearer Authentication validates token
   ↓
3. Claims extracted from token
   ↓
4. Authorization policy evaluated
   ↓
5. Custom requirement handlers check claims
   ↓
6. Authorization succeeds or fails
   ↓
7. 200 OK or 403 Forbidden
```

## Best Practices

### 1. Use Policy Names, Not Magic Strings
```csharp
// ✅ Good
[Authorize(Policy = AuthorizationPolicies.CanCreateProducts)]

// ❌ Bad
[Authorize(Policy = "CanCreateProducts")]
```

### 2. Separate Concerns
- **Authentication** - Who you are (JWT)
- **Authorization** - What you can do (Policies)

### 3. Principle of Least Privilege
- Assign minimum required permissions
- Use specific permissions over broad roles
- Regularly audit user permissions

### 4. Composite Policies
Combine multiple requirements:
```csharp
options.AddPolicy("SensitiveOperation", policy =>
{
    policy.RequireAuthenticatedUser();
    policy.RequireRole(Roles.Admin);
    policy.AddRequirements(new EmailVerifiedRequirement());
    policy.AddRequirements(new TwoFactorEnabledRequirement());
});
```

### 5. Resource-Based Authorization
For resource-specific checks:
```csharp
public class ProductOwnerRequirement : IAuthorizationRequirement { }

public class ProductOwnerHandler : AuthorizationHandler<ProductOwnerRequirement, Product>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProductOwnerRequirement requirement,
        Product product)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (product.CreatedBy == userId || context.User.IsInRole(Roles.Admin))
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
}
```

## Extending Authorization

### Add New Policy
1. Add policy name constant to `AuthorizationPolicies`
2. Add permission constant to `Permissions` (if needed)
3. Configure policy in `AuthorizationPolicyExtensions`
4. Register handler in `AddAuthorizationHandlers` (if custom requirement)
5. Apply to controllers with `[Authorize(Policy = "...")]`

### Example: Add Department-Based Authorization
```csharp
// 1. Add requirement
public class DepartmentRequirement : IAuthorizationRequirement
{
    public string Department { get; }
    public DepartmentRequirement(string department) => Department = department;
}

// 2. Add handler
public class DepartmentHandler : AuthorizationHandler<DepartmentRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DepartmentRequirement requirement)
    {
        var department = context.User.FindFirst(CustomClaimTypes.Department)?.Value;
        
        if (department == requirement.Department)
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
}

// 3. Add policy
public const string RequiresSalesDepartment = "RequiresSalesDepartment";

options.AddPolicy(RequiresSalesDepartment, policy =>
{
    policy.RequireAuthenticatedUser();
    policy.AddRequirements(new DepartmentRequirement("Sales"));
});

// 4. Register handler
services.AddScoped<IAuthorizationHandler, DepartmentHandler>();

// 5. Use in controller
[Authorize(Policy = AuthorizationPolicies.RequiresSalesDepartment)]
public IActionResult SalesReport() { }
```

## Security Considerations

### 1. Token Validation
- Tokens validated on every request
- Expired tokens rejected
- Claims extracted from validated tokens only

### 2. Authorization Bypass Prevention
- Always use `[Authorize]` attribute or policies
- Never trust client-side role checks
- Validate permissions on server

### 3. Logging
- Log authorization failures
- Monitor unusual access patterns
- Track permission changes

### 4. Testing
- Test with different roles
- Test expired tokens
- Test missing permissions
- Test edge cases

## Troubleshooting

### "403 Forbidden" Even With Valid Token
- Check token contains required role/permission claims
- Verify policy configuration matches claims
- Check authorization handler logs

### Policies Not Working
- Ensure `AddAuthorizationHandlers()` called in Program.cs
- Verify `UseAuthentication()` before `UseAuthorization()`
- Check policy names match exactly

### Claims Not in Token
- Verify claims added during token generation
- Check JWT token in jwt.io to see actual claims
- Ensure IAuthenticationService adds all required claims

## Documentation References

- [ASP.NET Core Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/introduction)
- [Policy-Based Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies)
- [Claims-Based Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/claims)
- [Resource-Based Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resourcebased)

---

**Implementation Date**: 2025-01-22  
**Author**: GitHub Copilot  
**Status**: ✅ Complete and Ready for Use
