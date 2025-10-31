# Archu.Contracts

The Contracts layer defines Data Transfer Objects (DTOs) and API request/response models for the Archu system. This layer provides a stable API surface that decouples external communication from internal domain models.

## 📋 Overview

**Target Framework**: .NET 9  
**Layer**: Contracts (API Boundary)  
**Dependencies**: None (zero external dependencies)

## 🎯 Purpose

The Contracts layer is responsible for:
- **API Contracts**: Request and response models for REST APIs
- **DTOs**: Data Transfer Objects for external communication
- **Decoupling**: Separation between API shape and domain entities
- **Versioning**: Stable API contracts independent of domain changes
- **Validation Attributes**: Data annotations for basic validation

## 🏗️ Architecture Principle

> **Separation of Concerns**: API contracts are separate from domain entities to prevent over-posting, enable API versioning, and provide flexibility in data exposure.

```
External Clients (Frontend, Mobile, etc.)
  ↓ uses
Archu.Contracts (API shape)
  ↓ mapped by
Archu.Application (DTOs ↔ Domain Entities)
  ↓ contains
Archu.Domain (Business entities)
```

## 📦 Project Structure

```
Archu.Contracts/
├── Admin/         # Admin API contracts
│   ├── AssignRoleRequest.cs
│   ├── CreateRoleRequest.cs
│   ├── CreateUserRequest.cs
│   ├── InitializeSystemRequest.cs
│   ├── RoleDto.cs
│   └── UserDto.cs
├── Authentication/            # Authentication contracts
│ ├── AuthenticationContracts.cs
│   └── AuthenticationResponse.cs
├── Common/        # Shared contracts
│   ├── ApiResponse.cs
│   └── PagedResult.cs
└── Products/      # Product management contracts
├── CreateProductRequest.cs
    ├── ProductDto.cs
    └── UpdateProductRequest.cs
```

## 🔧 Key Components

### 1. Common Response Wrappers

#### ApiResponse<T>
Standardized response wrapper for all API endpoints:

```csharp
public record ApiResponse<T>
{
    public bool Success { get; init; }
 public T? Data { get; init; }
    public string? Message { get; init; }
    public IEnumerable<string>? Errors { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string? message = null);
    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null);
}
```

**Usage Examples**:
```csharp
// Success response
return Ok(ApiResponse<ProductDto>.Ok(product, "Product created successfully"));

// Error response
return NotFound(ApiResponse<object>.Fail("Product not found"));

// Validation error response
return BadRequest(ApiResponse<object>.Fail(
    "Validation failed", 
    new[] { "Name is required", "Price must be positive" }));
```

#### PagedResult<T>
Pagination wrapper for list endpoints:

```csharp
public record PagedResult<T>
{
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}
```

**Usage**:
```csharp
var pagedProducts = new PagedResult<ProductDto>
{
    Items = products,
    PageNumber = 1,
    PageSize = 10,
    TotalCount = 50,
    TotalPages = 5,
    HasNextPage = true,
    HasPreviousPage = false
};

return Ok(ApiResponse<PagedResult<ProductDto>>.Ok(pagedProducts));
```

### 2. Product Contracts

#### ProductDto
Read model for product data:

```csharp
public sealed class ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
```

**Key Features**:
- `Id`: Unique identifier
- `Name`: Product name
- `Price`: Product price
- `RowVersion`: Concurrency control token

#### CreateProductRequest
Request model for creating products:

```csharp
public sealed class CreateProductRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue)]
  public decimal Price { get; init; }
}
```

**Validation**:
- Name: Required, 1-200 characters
- Price: Required, must be positive

#### UpdateProductRequest
Request model for updating products:

```csharp
public sealed class UpdateProductRequest
{
    [Required]
  public Guid Id { get; init; }

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

 [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; init; }

[Required]
    [MinLength(1)]
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
```

**Key Features**:
- Includes `RowVersion` for optimistic concurrency control
- All fields required to prevent partial updates

### 3. Authentication Contracts

#### Login Request & Response
```csharp
public record LoginRequest(
    [Required] string Email,
    [Required] string Password);

public record RegisterRequest(
    [Required] string UserName,
    [Required] [EmailAddress] string Email,
    [Required] string Password,
    [Required] string ConfirmPassword);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required] string NewPassword,
    [Required] string ConfirmPassword);

public record RefreshTokenRequest(
    [Required] string AccessToken,
    [Required] string RefreshToken);
```

#### AuthenticationResponse
```csharp
public sealed class AuthenticationResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public UserDto User { get; init; } = null!;
}
```

**Contains**:
- `AccessToken`: JWT token for API authentication (short-lived)
- `RefreshToken`: Token for renewing access token (long-lived)
- `ExpiresAt`: Token expiration timestamp
- `User`: Basic user information

### 4. Admin Contracts

#### UserDto
User information for admin operations:

```csharp
public sealed class UserDto
{
 public Guid Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool EmailConfirmed { get; init; }
 public bool LockoutEnabled { get; init; }
    public DateTime? LockoutEnd { get; init; }
    public IEnumerable<string> Roles { get; init; } = Enumerable.Empty<string>();
    public DateTime CreatedAtUtc { get; init; }
  public DateTime? ModifiedAtUtc { get; init; }
}
```

#### RoleDto
Role information:

```csharp
public sealed class RoleDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IEnumerable<string> Permissions { get; init; } = Enumerable.Empty<string>();
    public int UserCount { get; init; }
}
```

#### Admin Requests
```csharp
public record CreateUserRequest(
    [Required] string UserName,
    [Required] [EmailAddress] string Email,
    [Required] string Password,
    string[]? Roles = null);

public record AssignRoleRequest(
    [Required] Guid UserId,
    [Required] string RoleName);

public record CreateRoleRequest(
    [Required] string Name,
    string? Description = null,
    string[]? Permissions = null);

public record InitializeSystemRequest(
    [Required] string AdminEmail,
    [Required] string AdminPassword);
```

## 📋 Contract Design Principles

### 1. Immutability
All contracts use `init` accessors for immutability:
```csharp
public sealed class ProductDto
{
 public Guid Id { get; init; }  // Can only be set during initialization
    public string Name { get; init; } = string.Empty;
}
```

### 2. Records for Requests
Use `record` types for request models:
```csharp
public record CreateProductRequest(
    [Required] string Name,
    [Required] decimal Price);
```

**Benefits**:
- Concise syntax
- Value-based equality
- Immutable by default
- Built-in deconstruction

### 3. Sealed Classes for DTOs
Use `sealed class` for data transfer objects:
```csharp
public sealed class ProductDto
{
    // Properties...
}
```

**Benefits**:
- Prevents inheritance
- Slight performance improvement
- Clear intent (not meant to be extended)

### 4. Default Values
Always provide sensible defaults:
```csharp
public string Name { get; init; } = string.Empty;  // Never null
public byte[] RowVersion { get; init; } = Array.Empty<byte>();
public IEnumerable<string> Roles { get; init; } = Enumerable.Empty<string>();
```

### 5. Validation Attributes
Use Data Annotations for basic validation:
```csharp
[Required]
[StringLength(200, MinimumLength = 1)]
public string Name { get; init; } = string.Empty;

[Required]
[Range(0.01, double.MaxValue)]
public decimal Price { get; init; }

[EmailAddress]
public string Email { get; init; } = string.Empty;
```

## 🔄 Mapping Between Layers

### Domain Entity → DTO (in Application Layer)
```csharp
// In GetProductsQueryHandler
var dto = new ProductDto
{
    Id = product.Id,
    Name = product.Name,
    Price = product.Price,
    RowVersion = product.RowVersion
};
```

### Request → Command/Query (in API Layer)
```csharp
// In ProductsController
[HttpPost]
public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct(
    CreateProductRequest request)
{
  var command = new CreateProductCommand(request.Name, request.Price);
    var result = await _mediator.Send(command);
    
    return Ok(ApiResponse<ProductDto>.Ok(result.Value!, "Product created"));
}
```

### Result<T> → ApiResponse<T> (in API Layer)
```csharp
var result = await _mediator.Send(command);

if (!result.IsSuccess)
    return NotFound(ApiResponse<object>.Fail(result.Error!));
    
return Ok(ApiResponse<ProductDto>.Ok(result.Value!, "Success"));
```

## 🎯 API Response Patterns

### Success Responses

**Single Item**:
```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Product Name",
    "price": 29.99,
    "rowVersion": "AAAAAAAAB9E="
  },
  "message": "Product created successfully",
  "errors": null,
  "timestamp": "2025-01-24T10:30:00Z"
}
```

**List**:
```json
{
  "success": true,
  "data": [
    { "id": "...", "name": "Product 1", "price": 10.00 },
    { "id": "...", "name": "Product 2", "price": 20.00 }
  ],
  "message": null,
  "errors": null,
  "timestamp": "2025-01-24T10:30:00Z"
}
```

**Paged List**:
```json
{
  "success": true,
  "data": {
    "items": [...],
 "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 50,
    "totalPages": 5,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "message": null,
  "errors": null,
  "timestamp": "2025-01-24T10:30:00Z"
}
```

### Error Responses

**Not Found**:
```json
{
  "success": false,
  "data": null,
  "message": "Product not found",
  "errors": null,
  "timestamp": "2025-01-24T10:30:00Z"
}
```

**Validation Error**:
```json
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errors": [
    "Name is required",
    "Price must be greater than 0"
  ],
  "timestamp": "2025-01-24T10:30:00Z"
}
```

**Concurrency Conflict**:
```json
{
  "success": false,
  "data": null,
  "message": "The product has been modified by another user. Please refresh and try again.",
  "errors": null,
  "timestamp": "2025-01-24T10:30:00Z"
}
```

## 🧪 Testing Contracts

### Contract Validation Tests
```csharp
public class CreateProductRequestTests
{
  [Fact]
    public void CreateProductRequest_ValidData_PassesValidation()
    {
        // Arrange
        var request = new CreateProductRequest
        {
    Name = "Test Product",
            Price = 10.00m
        };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        
  // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);
  
      // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("", 10.00)]  // Empty name
    [InlineData("Valid Name", 0)]  // Zero price
    [InlineData("Valid Name", -5)]  // Negative price
    public void CreateProductRequest_InvalidData_FailsValidation(string name, decimal price)
{
        // Arrange
  var request = new CreateProductRequest { Name = name, Price = price };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        
   // Act
var isValid = Validator.TryValidateObject(request, context, results, true);
        
      // Assert
        Assert.False(isValid);
        Assert.NotEmpty(results);
    }
}
```

### JSON Serialization Tests
```csharp
public class ApiResponseSerializationTests
{
[Fact]
    public void ApiResponse_SerializesToJson_Correctly()
    {
        // Arrange
      var response = ApiResponse<ProductDto>.Ok(
            new ProductDto { Id = Guid.NewGuid(), Name = "Test", Price = 10 },
            "Success");
        
    // Act
        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<ApiResponse<ProductDto>>(json);
        
        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.Equal("Test", deserialized.Data?.Name);
    }
}
```

## 📋 Best Practices

✅ **DO**:
- Use `record` for immutable request models
- Use `sealed class` for DTOs
- Provide default values for all properties
- Add data annotations for basic validation
- Use `ApiResponse<T>` wrapper for all endpoints
- Include concurrency tokens (RowVersion) in update requests
- Use nullable reference types (`string?`)
- Keep contracts simple and flat

❌ **DON'T**:
- Include domain logic in contracts
- Use domain entities as API responses
- Skip validation attributes
- Return raw objects without `ApiResponse<T>` wrapper
- Expose sensitive data (password hashes, internal IDs)
- Add dependencies to other layers
- Use circular references

## 🔒 Security Considerations

### Sensitive Data Exclusion
Never expose:
- Password hashes
- Security stamps
- Refresh tokens (except in authentication response)
- Internal audit fields (unless needed)

### Example - UserDto (Safe):
```csharp
public sealed class UserDto
{
    public Guid Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
 // ❌ NO: PasswordHash, SecurityStamp, RefreshToken
}
```

### Over-Posting Prevention
Use separate request models for Create vs Update:
```csharp
// Create - No ID, no RowVersion
public record CreateProductRequest(string Name, decimal Price);

// Update - Requires ID and RowVersion
public record UpdateProductRequest(Guid Id, string Name, decimal Price, byte[] RowVersion);
```

## 🔗 Related Documentation

- [Application Layer](../Archu.Application/README.md) - CQRS handlers and mapping
- [Domain Layer](../Archu.Domain/README.md) - Business entities
- [API Documentation](../Archu.Api/README.md) - REST endpoints
- [Architecture Guide](../../docs/ARCHITECTURE.md) - Clean Architecture overview

## 🤝 Contributing

When adding new contracts:

1. **Create folder by feature**: `Products/`, `Orders/`, etc.
2. **Define DTOs**: Use `sealed class` with `init` accessors
3. **Define requests**: Use `record` types
4. **Add validation**: Use Data Annotations
5. **Document**: Add XML comments for public APIs
6. **Test**: Write validation tests
7. **Update README**: Document new contracts

## 🔄 Versioning Strategy

When API contracts need to change:

1. **Backward-compatible changes**: Add new properties with defaults
2. **Breaking changes**: Create new version (`ProductDtoV2`)
3. **Deprecation**: Mark old contracts with `[Obsolete]`
4. **API Versioning**: Use URL versioning (`/api/v1/products`, `/api/v2/products`)

**Example**:
```csharp
// V1 - Original
public sealed class ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

// V2 - Added fields (backward compatible)
public sealed class ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }  // New field with default
}
```

## 📊 Contract Statistics

| Category | Count | Description |
|----------|-------|-------------|
| **DTOs** | 3 | ProductDto, UserDto, RoleDto |
| **Requests** | 10+ | Create/Update requests across features |
| **Common** | 2 | ApiResponse, PagedResult |
| **Auth** | 6 | Login, Register, ChangePassword, etc. |
| **Admin** | 4 | User/Role management requests |

## 🔄 Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-01-24 | Initial contracts documentation |

---

**Maintained by**: Archu Development Team  
**Last Updated**: 2025-01-24
