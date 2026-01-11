# TentMan.Api

The main REST API for the TentMan Tenant Management System, built with ASP.NET Core.

---

## 📁 Folder Structure

```
TentMan.Api/
├── Authorization/             # Authorization policies
├── Controllers/               # API controllers
│   ├── AuthenticationController.cs
│   ├── ProductsController.cs
│   ├── BuildingsController.cs
│   ├── TenantManagement/      # Tenant and Lease controllers
│   │   ├── TenantsController.cs
│   │   └── LeasesController.cs
│   ├── TenantPortal/          # Tenant portal endpoints
│   │   └── TenantPortalController.cs
│   └── ...
├── Health/                    # Health check endpoints
├── Middleware/                # Custom middleware
│   └── GlobalExceptionHandlerMiddleware.cs
├── Properties/                # Launch settings
├── Program.cs                 # Application entry point
├── appsettings.json          # Configuration
└── TentMan.Api.http           # HTTP request examples
```

---

## 🎯 Purpose

The API layer:
- Exposes REST endpoints
- Handles HTTP requests/responses
- Manages authentication/authorization
- Provides API documentation via Scalar

---

## 📋 Coding Guidelines

### Controller Structure

```csharp
using TentMan.Shared.Constants.Authorization;

namespace TentMan.Api.Controllers;

/// <summary>
/// Manages product operations.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductsController> _logger;
    
    public ProductsController(
        IMediator mediator,
        ILogger<ProductsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    
    /// <summary>
    /// Gets all products.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = PolicyNames.Products.View)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductDto>>), 200)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductsQuery(pageNumber, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        
        return Ok(ApiResponse<IEnumerable<ProductDto>>.Ok(result));
    }
}
```

### File Size Limits

| File Type | Limit | Action |
|-----------|-------|--------|
| Controller | 300 lines | Use partial classes |
| Program.cs | 300 lines | Extract to extension methods |

### Partial Controller Example

When a controller exceeds 300 lines:

```
Controllers/
├── ProductsController.cs           # Core CRUD operations
├── ProductsController.Search.cs    # Search endpoints
└── ProductsController.Export.cs    # Export endpoints
```

```csharp
// ProductsController.cs
public partial class ProductsController : ControllerBase
{
    // GET, POST, PUT, DELETE endpoints
}

// ProductsController.Search.cs
public partial class ProductsController
{
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] SearchQuery query)
    {
        // Search logic
    }
}
```

### Response Pattern

Always wrap responses in `ApiResponse<T>`:

```csharp
// Success
return Ok(ApiResponse<ProductDto>.Ok(product));

// Created
return CreatedAtAction(
    nameof(GetProduct),
    new { id = product.Id },
    ApiResponse<ProductDto>.Ok(product, "Product created"));

// Not Found
return NotFound(ApiResponse<ProductDto>.Fail("Product not found"));

// Bad Request
return BadRequest(ApiResponse<ProductDto>.Fail(errors));
```

### Authorization Patterns

TentMan uses **policy-based authorization** for consistent access control across all endpoints.

#### Policy-Based Authorization (Recommended)

```csharp
using TentMan.Shared.Constants.Authorization;

// Role-based policies
[Authorize(Policy = PolicyNames.RequireAdminRole)]
[Authorize(Policy = PolicyNames.RequireManagerRole)]
[Authorize(Policy = PolicyNames.RequireUserRole)]
[Authorize(Policy = PolicyNames.RequireTenantRole)]

// Permission-based policies
[Authorize(Policy = PolicyNames.Products.Create)]
[Authorize(Policy = PolicyNames.Products.View)]
[Authorize(Policy = PolicyNames.CanViewTenantPortal)]

// Anonymous access
[AllowAnonymous]
```

#### Authorization Constants Location

All authorization constants are centralized in `TentMan.Shared.Constants.Authorization`:
- **Policy Names**: `PolicyNames` class
- **Role Names**: `RoleNames` class  
- **Permission Values**: `PermissionValues` class
- **Claim Types**: `ClaimTypes` class

**Example Usage**:
```csharp
using TentMan.Shared.Constants.Authorization;

[Authorize(Policy = PolicyNames.RequireManagerRole)]
public class AuditLogsController : ControllerBase
{
    // Manager, Administrator, and SuperAdmin can access
}
```

#### Role Hierarchy

The system implements role hierarchy where higher roles inherit permissions from lower roles:
- **RequireUserRole**: Allows User, Manager, Administrator, SuperAdmin
- **RequireManagerRole**: Allows Manager, Administrator, SuperAdmin
- **RequireAdminRole**: Allows Administrator, SuperAdmin
- **RequireSuperAdminRole**: Allows SuperAdmin only
- **RequireTenantRole**: Allows Tenant only

See [AUTHORIZATION_GUIDE.md](../../docs/AUTHORIZATION_GUIDE.md) for complete details.

---

## 🔗 Dependencies

- **TentMan.Application**: MediatR handlers
- **TentMan.Infrastructure**: Service implementations
- **TentMan.Contracts**: DTOs
- **MediatR**: Request handling
- **Scalar**: API documentation

---

## 📚 API Documentation

Interactive documentation available at:
- **Scalar UI**: https://localhost:7123/scalar/v1
- **OpenAPI spec**: https://localhost:7123/openapi/v1.json

### HTTP Request Examples

See `TentMan.Api.http` for ready-to-use examples.

---

## 🚀 Running the API

```bash
cd src/TentMan.Api
dotnet run
```

Or with Aspire:
```bash
cd src/TentMan.AppHost
dotnet run
```

---

## 📝 Tenant Portal Endpoints

The Tenant Portal provides read-only access for tenants to view their lease information.

### GET /api/v1/tenant-portal/lease-summary

Gets the current tenant's active lease summary with complete details.

**Authorization**: Requires Tenant role (via policy `PolicyNames.RequireTenantRole`)

**Response**: `ApiResponse<TenantLeaseSummaryResponse>`

**Returns**:
- Lease information (number, dates, status, terms)
- Unit details (number, building, address)
- Current financial terms (rent, deposit, charges)
- Deposit transaction history
- Other lease parties (roommates, occupants)
- Historical rent terms (timeline)
- Late fee policy
- Payment method information

**Status Codes**:
- `200 OK`: Lease summary retrieved successfully
- `404 Not Found`: No active lease found for the tenant (returns structured `ApiResponse` with error message)
- `401 Unauthorized`: Invalid authentication or missing Tenant role

**Example**:
```bash
GET /api/v1/tenant-portal/lease-summary
Authorization: Bearer {tenant-jwt-token}
```

---

## 📧 Tenant Invite Endpoints

Owners can generate and manage invite tokens for tenants to create their accounts and access the portal.

### POST /api/v1/organizations/{orgId}/tenants/{tenantId}/invites

Generates an invite token for a tenant.

**Authorization**: Requires authentication

**Request Body**: `GenerateInviteRequest`
```json
{
  "tenantId": "guid",
  "expiryDays": 7
}
```

**Response**: `ApiResponse<TenantInviteDto>`

**Status Codes**:
- `201 Created`: Invite generated successfully
- `400 Bad Request`: Validation error (tenant not found, missing phone, etc.)
- `401 Unauthorized`: Not authenticated

### GET /api/v1/organizations/{orgId}/tenants/{tenantId}/invites

Gets all invites for a specific tenant.

**Authorization**: Requires authentication

**Response**: `ApiResponse<IEnumerable<TenantInviteDto>>`

**Status Codes**:
- `200 OK`: Invites retrieved successfully
- `400 Bad Request`: Tenant not found or doesn't belong to organization
- `401 Unauthorized`: Not authenticated

### DELETE /api/v1/organizations/{orgId}/invites/{inviteId}

Cancels an existing invite.

**Authorization**: Requires authentication

**Response**: `ApiResponse<object>`

**Status Codes**:
- `200 OK`: Invite canceled successfully
- `400 Bad Request`: Invite not found, already used, or doesn't belong to organization
- `401 Unauthorized`: Not authenticated

### GET /api/v1/invites/validate

Validates an invite token (public endpoint).

**Authorization**: None (anonymous)

**Query Parameters**:
- `token`: The invite token to validate

**Response**: `ApiResponse<ValidateInviteResponse>`

**Status Codes**:
- `200 OK`: Validation complete (check `IsValid` property in response)

### POST /api/v1/invites/accept

Accepts an invite and creates a user account.

**Authorization**: None (anonymous)

**Request Body**: `AcceptInviteRequest`
```json
{
  "inviteToken": "string",
  "userName": "string",
  "email": "string",
  "password": "string"
}
```

**Response**: `ApiResponse<AuthenticationResponse>`

**Status Codes**:
- `200 OK`: Invite accepted and user created successfully
- `400 Bad Request`: Invalid token, expired, already used, or validation errors

---

## ✅ Checklist for New Endpoints

- [ ] Create controller in Controllers folder
- [ ] Add proper authorization attributes
- [ ] Use MediatR for all operations
- [ ] Return ApiResponse<T> wrapper
- [ ] Add XML documentation
- [ ] Add ProducesResponseType attributes
- [ ] File size under 300 lines
- [ ] Add integration tests

---

**Last Updated**: 2026-01-09  
**Maintainer**: TentMan Development Team
