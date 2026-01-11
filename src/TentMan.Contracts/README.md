# TentMan.Contracts

The Contracts project contains DTOs, request models, and response models shared across the application.

---

## 📁 Folder Structure

```
TentMan.Contracts/
├── Admin/                     # Admin API contracts
│   ├── CreateUserRequest.cs
│   ├── UserDto.cs
│   └── ...
├── Authentication/            # Auth contracts
│   ├── LoginRequest.cs
│   ├── LoginResponse.cs
│   └── TokenResponse.cs
├── Buildings/                 # Building contracts
│   ├── BuildingDto.cs
│   ├── CreateBuildingRequest.cs
│   └── UpdateBuildingRequest.cs
├── Common/                    # Shared contracts
│   ├── ApiResponse.cs
│   ├── PagedResult.cs
│   └── ErrorResponse.cs
├── Enums/                     # Shared enumerations
│   ├── LeaseStatus.cs         # Lease statuses
│   ├── LeasePartyRole.cs      # Tenant roles in lease
│   ├── LateFeeType.cs
│   ├── DepositTransactionType.cs
│   ├── AddressType.cs
│   ├── Gender.cs
│   ├── ChargeTypeCode.cs      # Billing engine enums
│   ├── BillingFrequency.cs
│   ├── InvoiceStatus.cs
│   ├── CreditNoteReason.cs
│   ├── InvoiceRunStatus.cs
│   └── ...
├── Leases/                    # Lease contracts
│   ├── LeaseDetailDto.cs
│   ├── LeaseListDto.cs
│   ├── CreateLeaseRequest.cs
│   ├── AddLeasePartyRequest.cs
│   ├── AddLeaseTermRequest.cs
│   └── ActivateLeaseRequest.cs
├── Tenants/                   # Tenant contracts
│   ├── TenantDetailDto.cs
│   ├── TenantListDto.cs
│   ├── CreateTenantRequest.cs
│   ├── UpdateTenantRequest.cs
│   └── AddTenantAddressRequest.cs
├── TenantInvites/             # Tenant invite contracts
│   ├── TenantInviteDto.cs
│   ├── GenerateInviteRequest.cs
│   ├── AcceptInviteRequest.cs
│   └── ValidateInviteResponse.cs
├── Files/                     # File metadata contracts
│   └── FileMetadataDto.cs
├── Owners/                    # Owner contracts
├── Products/                  # Product contracts
│   ├── ProductDto.cs
│   ├── CreateProductRequest.cs
│   └── UpdateProductRequest.cs
└── Units/                     # Unit contracts
```

---

## 🎯 Purpose

The Contracts project:
- Defines API data transfer objects (DTOs)
- Provides request/response models
- Ensures consistent API contracts
- Shares models between API and clients

---

## 💳 Billing Enums

### InvoiceStatus
Represents the lifecycle status of an invoice.

**Values**:
- `Draft` (1): Invoice is being drafted and can be edited
- `Issued` (2): Invoice is issued and awaiting payment (immutable)
- `PartiallyPaid` (3): Invoice is partially paid
- `Paid` (4): Invoice is fully paid
- `Overdue` (5): Invoice is overdue
- `Cancelled` (6): Invoice is cancelled
- `WrittenOff` (7): Invoice is written off
- `Voided` (8): Invoice is voided and cannot be modified

**State Transitions**:
- Draft → Issued (via IssueInvoice)
- Issued → Voided (via VoidInvoice, only if unpaid)
- Voided is a terminal state

### CreditNoteReason
Represents the reason for issuing a credit note.

**Values**:
- `InvoiceError` (1): Invoice error or overpayment
- `Discount` (2): Discount applied
- `Refund` (3): Refund for returned goods/services
- `Goodwill` (4): Goodwill gesture
- `Adjustment` (5): Adjustment for errors
- `Other` (99): Other reason

---

## 📋 Coding Guidelines

### DTO Pattern

```csharp
namespace TentMan.Contracts.Products;

/// <summary>
/// Product data transfer object.
/// </summary>
public sealed record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    DateTime CreatedAt,
    string RowVersion);
```

### Request Pattern

```csharp
namespace TentMan.Contracts.Products;

/// <summary>
/// Request to create a new product.
/// </summary>
public sealed record CreateProductRequest(
    [Required]
    [StringLength(200, MinimumLength = 1)]
    string Name,
    
    [StringLength(1000)]
    string? Description,
    
    [Required]
    [Range(0.01, double.MaxValue)]
    decimal Price);
```

### Response Pattern

```csharp
namespace TentMan.Contracts.Products;

/// <summary>
/// Response containing product details.
/// </summary>
public sealed record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    bool Success = true,
    string? Message = null);
```

### File Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| DTO | `{Entity}Dto.cs` | `ProductDto.cs` |
| Create Request | `Create{Entity}Request.cs` | `CreateProductRequest.cs` |
| Update Request | `Update{Entity}Request.cs` | `UpdateProductRequest.cs` |
| Response | `{Entity}Response.cs` | `ProductResponse.cs` |

### File Size Limits

| File Type | Limit | Guidance |
|-----------|-------|----------|
| Single DTO | 50 lines max | One record per file |
| Request | 50 lines max | One record per file |
| Response | 50 lines max | One record per file |

### Common Patterns

#### ApiResponse Wrapper

```csharp
namespace TentMan.Contracts.Common;

/// <summary>
/// Standard API response wrapper.
/// </summary>
public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Message,
    IEnumerable<string>? Errors,
    DateTime Timestamp)
{
    public static ApiResponse<T> Ok(T data, string? message = null)
        => new(true, data, message, null, DateTime.UtcNow);
        
    public static ApiResponse<T> Fail(string message)
        => new(false, default, message, null, DateTime.UtcNow);
        
    public static ApiResponse<T> Fail(IEnumerable<string> errors)
        => new(false, default, "Validation failed", errors, DateTime.UtcNow);
}
```

#### PagedResult

```csharp
namespace TentMan.Contracts.Common;

/// <summary>
/// Paginated result set.
/// </summary>
public sealed record PagedResult<T>(
    IEnumerable<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
```

---

## 🔗 Dependencies

- **System.ComponentModel.DataAnnotations**: Validation attributes

---

## ✅ Checklist for New Contracts

- [ ] Use record types for immutability
- [ ] Add validation attributes where applicable
- [ ] Add XML documentation
- [ ] One type per file
- [ ] Place in appropriate feature folder
- [ ] File size under 50 lines

---

**Last Updated**: 2026-01-09  
**Maintainer**: TentMan Development Team
