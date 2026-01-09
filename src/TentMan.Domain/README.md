# TentMan.Domain

The Domain layer contains the core business entities, value objects, and domain logic. This is the innermost layer of the Clean Architecture and has **no dependencies** on other projects.

---

## 📁 Folder Structure

```
TentMan.Domain/
├── Abstractions/           # Base classes and interfaces
│   ├── Entity.cs          # Base entity with Id
│   ├── AuditableEntity.cs # Adds audit tracking
│   └── ISoftDelete.cs     # Soft delete interface
├── Common/                 # Shared domain types
│   └── Result.cs          # Result pattern implementation
├── Constants/              # Domain constants
├── Entities/               # Domain entities
│   ├── Identity/          # User, Role entities
│   ├── Product.cs
│   ├── Building.cs
│   ├── Unit.cs
│   ├── Owner.cs
│   ├── Organization.cs
│   ├── Tenant.cs          # Tenant management
│   ├── TenantAddress.cs
│   ├── TenantEmergencyContact.cs
│   ├── TenantDocument.cs
│   ├── TenantInvite.cs    # Tenant invite system
│   ├── Lease.cs           # Lease management
│   ├── LeaseParty.cs
│   ├── LeaseTerm.cs
│   ├── DepositTransaction.cs
│   ├── UnitHandover.cs
│   ├── HandoverChecklistItem.cs
│   ├── MeterReading.cs
│   └── UnitOccupancy.cs
├── Enums/                  # Domain enumerations
├── Extensions/             # Domain extension methods
└── ValueObjects/           # Value object types
```

---

## 🎯 Purpose

The Domain layer:
- Defines business entities and their behavior
- Contains business rules and validation
- Is framework-agnostic (no EF Core, no ASP.NET dependencies)
- Represents the core business model

---

## 📋 Coding Guidelines

### Entity Structure

Each entity should follow this pattern:

```csharp
namespace TentMan.Domain.Entities;

/// <summary>
/// Represents a [entity description].
/// </summary>
public class MyEntity : AuditableEntity, ISoftDelete
{
    // Properties (public setters for EF Core, or private with methods)
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Navigation properties
    public Guid OwnerId { get; set; }
    public Owner Owner { get; set; } = null!;
    
    // ISoftDelete implementation
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    // Business methods (if any)
    public void Archive() => IsDeleted = true;
}
```

### File Size Limits

| Rule | Limit |
|------|-------|
| Entity file | 300 lines max |
| Use partial classes if exceeded | Split by responsibility |

### Naming Conventions

| Item | Convention | Example |
|------|------------|---------|
| Entity | PascalCase, singular | `Product`, `Building` |
| Value Object | PascalCase, descriptive | `Address`, `Money` |
| Enum | PascalCase, singular | `BuildingType`, `UnitType` |
| Interface | `I` prefix | `ISoftDelete` |

### Entity Best Practices

1. **Immutability**: Use private setters when possible
2. **Validation**: Validate in constructors or factory methods
3. **Encapsulation**: Expose behavior through methods
4. **No logic dependencies**: Don't inject services into entities

---

## 🔗 Dependencies

**This project has NO external dependencies** - it's pure C# code.

---

## 📚 Key Abstractions

### AuditableEntity

Base class providing:
- `Id` (Guid)
- `CreatedAt` (DateTime)
- `CreatedBy` (string)
- `ModifiedAt` (DateTime?)
- `ModifiedBy` (string?)
- `RowVersion` (byte[]) for concurrency

### ISoftDelete

Interface for soft-deletable entities:
- `IsDeleted` (bool)
- `DeletedAt` (DateTime?)

### Result Pattern

Used for operation outcomes:
```csharp
Result.Success()
Result<T>.Success(data)
Result.Failure("Error message")
```

---

## ✅ Checklist for New Entities

- [ ] Entity inherits from `AuditableEntity`
- [ ] Entity implements `ISoftDelete` if soft-deletable
- [ ] XML documentation on class and public members
- [ ] File size under 300 lines
- [ ] Placed in appropriate subfolder
- [ ] No framework dependencies

---

**Last Updated**: 2026-01-09  
**Maintainer**: TentMan Development Team
