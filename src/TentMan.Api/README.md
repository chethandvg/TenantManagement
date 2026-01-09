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
    [Authorize(Roles = "User,Manager,Administrator")]
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

```csharp
// Role-based
[Authorize(Roles = "Administrator,SuperAdmin")]

// Policy-based
[Authorize(Policy = "RequireAdminRole")]

// Anonymous
[AllowAnonymous]
```

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

**Authorization**: Requires `Tenant` role

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
