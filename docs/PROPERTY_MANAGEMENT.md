# Property Management Module

Complete guide to the Property Management module in TentMan - a comprehensive system for managing multi-tenant properties, buildings, units, and ownership tracking.

---

## 📚 Table of Contents

- [Overview](#overview)
- [Domain Model](#domain-model)
- [API Endpoints](#api-endpoints)
- [Business Rules](#business-rules)
- [Architecture](#architecture)
- [Usage Examples](#usage-examples)
- [Database Schema](#database-schema)

---

## 🎯 Overview

The Property Management module provides foundational capabilities for managing:
- **Organizations** - Multi-tenant organization entities
- **Buildings** - Properties with addresses and metadata
- **Units** - Individual flats, houses, shops, or offices within buildings
- **Owners** - Property owners (individuals or companies)
- **Ownership Tracking** - Share-based ownership with historical tracking
- **Utility Meters** - Electricity, water, and gas meter tracking
- **File Management** - Document and photo attachments (metadata only)

### Key Features

✅ **Multi-tenant Architecture** - Full organization isolation  
✅ **Ownership Validation** - Automated validation ensuring shares sum to 100%  
✅ **Soft Delete** - Data preservation with audit trails  
✅ **Optimistic Concurrency** - Row version-based conflict detection  
✅ **Clean Architecture** - CQRS pattern with proper separation of concerns  

---

## 🏗️ Domain Model

### Core Entities

#### Organization
Multi-tenant root entity for data isolation.

```csharp
public class Organization : BaseEntity
{
    public string Name { get; set; }
    public string TimeZone { get; set; }  // Default: "Asia/Kolkata"
    public bool IsActive { get; set; }
}
```

#### Building
Represents a property (apartment complex, commercial building, etc.).

```csharp
public class Building : BaseEntity
{
    public Guid OrgId { get; set; }
    public string BuildingCode { get; set; }  // Unique per org
    public string Name { get; set; }
    public PropertyType PropertyType { get; set; }
    public int TotalFloors { get; set; }
    public bool HasLift { get; set; }
    public string? Notes { get; set; }
    
    // Navigation properties
    public BuildingAddress? Address { get; set; }
    public ICollection<Unit> Units { get; set; }
    public ICollection<BuildingOwnershipShare> OwnershipShares { get; set; }
}
```

#### Unit
Individual unit within a building (flat, shop, office, etc.).

```csharp
public class Unit : BaseEntity
{
    public Guid BuildingId { get; set; }
    public string UnitNumber { get; set; }  // Unique per building
    public int Floor { get; set; }
    public UnitType UnitType { get; set; }
    public decimal AreaSqFt { get; set; }
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public Furnishing Furnishing { get; set; }
    public int ParkingSlots { get; set; }
    public OccupancyStatus OccupancyStatus { get; set; }
    public bool HasUnitOwnershipOverride { get; set; }
}
```

#### Owner
Property owner (individual or company).

```csharp
public class Owner : BaseEntity
{
    public Guid OrgId { get; set; }
    public OwnerType OwnerType { get; set; }
    public string DisplayName { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string? Pan { get; set; }    // Indian PAN
    public string? Gstin { get; set; }  // Indian GSTIN
    public Guid? LinkedUserId { get; set; }
}
```

### Enums

Located in `TentMan.Contracts.Enums`:

- **PropertyType**: Apartment, IndependentHouse, Commercial, MixedUse
- **UnitType**: OneBHK, TwoBHK, ThreeBHK, FourBHK, Shop, Office, Warehouse, Studio, Penthouse
- **Furnishing**: Unfurnished, SemiFurnished, FullyFurnished
- **OccupancyStatus**: Vacant, Occupied, Blocked
- **OwnerType**: Individual, Company
- **UtilityType**: Electricity, Water, Gas
- **StorageProvider**: Local, AzureBlob, S3
- **FileTag**: Photo, Document, Other

---

## 🌐 API Endpoints

### Organizations

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/v1/organizations` | POST | Create a new organization |

**Example Request**:
```json
POST /api/v1/organizations
{
  "name": "Prestige Estates",
  "timeZone": "Asia/Kolkata"
}

Response 201 Created:
{
  "success": true,
  "data": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "message": "Organization created successfully"
}
```

### Buildings

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/v1/organizations/{orgId}/buildings` | POST | Create a building |
| `/api/v1/organizations/{orgId}/buildings` | GET | List all buildings |

**Example Request - Create Building**:
```json
POST /api/v1/organizations/{orgId}/buildings
{
  "buildingCode": "PRS-001",
  "name": "Prestige Lakeside Habitat",
  "propertyType": 1,  // Apartment
  "totalFloors": 25,
  "hasLift": true,
  "notes": "Premium apartments with lake view"
}

Response 201 Created:
{
  "success": true,
  "data": {
    "id": "building-guid",
    "buildingCode": "PRS-001",
    "name": "Prestige Lakeside Habitat",
    "propertyType": 1,
    "totalFloors": 25,
    "hasLift": true,
    "notes": "Premium apartments with lake view",
    "rowVersion": "AAAAAAAAB9E="
  }
}
```

### Units

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/v1/buildings/{buildingId}/units` | POST | Create a unit |
| `/api/v1/buildings/{buildingId}/units` | GET | List all units in a building |

**Example Request - Create Unit**:
```json
POST /api/v1/buildings/{buildingId}/units
{
  "unitNumber": "A-402",
  "floor": 4,
  "unitType": 2,  // TwoBHK
  "areaSqFt": 1250.50,
  "bedrooms": 2,
  "bathrooms": 2,
  "furnishing": 2,  // SemiFurnished
  "parkingSlots": 1
}

Response 201 Created:
{
  "success": true,
  "data": {
    "id": "unit-guid",
    "unitNumber": "A-402",
    "floor": 4,
    "unitType": 2,
    "areaSqFt": 1250.50,
    "bedrooms": 2,
    "bathrooms": 2,
    "furnishing": 2,
    "occupancyStatus": 1,  // Vacant
    "rowVersion": "AAAAAAAAB9E="
  }
}
```

### Owners

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/v1/organizations/{orgId}/owners` | POST | Create an owner |
| `/api/v1/organizations/{orgId}/owners` | GET | List all owners |

**Example Request - Create Owner**:
```json
POST /api/v1/organizations/{orgId}/owners
{
  "ownerType": 1,  // Individual
  "displayName": "Rajesh Kumar",
  "phone": "+91-9876543210",
  "email": "rajesh.kumar@example.com",
  "pan": "ABCDE1234F",
  "gstin": "29ABCDE1234F1Z5"
}

Response 201 Created:
{
  "success": true,
  "data": {
    "id": "owner-guid",
    "ownerType": 1,
    "displayName": "Rajesh Kumar",
    "phone": "+91-9876543210",
    "email": "rajesh.kumar@example.com",
    "pan": "ABCDE1234F",
    "gstin": "29ABCDE1234F1Z5",
    "rowVersion": "AAAAAAAAB9E="
  }
}
```

---

## 📋 Business Rules

### Ownership Validation

The system enforces strict ownership validation rules:

1. **Sum Must Equal 100%**: All ownership shares for a building or unit must sum to exactly 100.00%
2. **Tolerance**: A tolerance of ±0.01% is allowed for floating-point precision
3. **Validation Point**: Validation occurs when setting ownership shares

**Example**:
```csharp
// Valid - sums to 100%
shares = [50.00%, 30.00%, 20.00%]  ✅

// Valid - within tolerance
shares = [33.335%, 33.335%, 33.335%]  ✅ (sum: 100.005%, within 0.01 tolerance)

// Invalid - does not sum to 100%
shares = [50.00%, 30.00%, 15.00%]  ❌ (sum: 95%)
```

**Implementation**:
```csharp
public class OwnershipService : IOwnershipService
{
    private const decimal REQUIRED_TOTAL = 100.00m;
    private const decimal DEFAULT_TOLERANCE = 0.01m;

    public bool ValidateOwnershipShares(IEnumerable<decimal> shares, decimal tolerance = DEFAULT_TOLERANCE)
    {
        var total = shares.Sum();
        var diff = Math.Abs(total - REQUIRED_TOTAL);
        return diff <= tolerance;
    }
}
```

### Unique Constraints

- **BuildingCode**: Must be unique within an organization
- **UnitNumber**: Must be unique within a building

### Soft Delete

All entities support soft delete:
- `IsDeleted` flag is set to `true`
- Audit fields (`DeletedBy`, `DeletedAtUtc`) are populated
- Records remain in database for audit trails
- All queries automatically filter out deleted records

### Concurrency Control

All entities use optimistic concurrency with `RowVersion`:
- Timestamp-based versioning
- Automatic conflict detection
- Returns 409 Conflict on version mismatch

---

## 🏛️ Architecture

### Clean Architecture Layers

```
┌─────────────────────────────────────────┐
│         API Layer (Controllers)          │
│   - OrganizationsController              │
│   - BuildingsController                  │
│   - UnitsController                      │
│   - OwnersController                     │
└────────────┬────────────────────────────┘
             │
┌────────────▼────────────────────────────┐
│    Application Layer (CQRS)              │
│   Commands:                              │
│   - CreateOrganization                   │
│   - CreateBuilding                       │
│   - CreateUnit                           │
│   - CreateOwner                          │
│                                          │
│   Queries:                               │
│   - GetBuildings                         │
│   - GetUnits                             │
│   - GetOwners                            │
│                                          │
│   Services:                              │
│   - OwnershipService                     │
└────────────┬────────────────────────────┘
             │
┌────────────▼────────────────────────────┐
│    Infrastructure Layer                  │
│   Repositories:                          │
│   - OrganizationRepository               │
│   - BuildingRepository                   │
│   - UnitRepository                       │
│   - OwnerRepository                      │
│                                          │
│   - UnitOfWork                           │
│   - ApplicationDbContext                 │
└────────────┬────────────────────────────┘
             │
┌────────────▼────────────────────────────┐
│         Domain Layer                     │
│   - Entities                             │
│   - Base classes                         │
└──────────────────────────────────────────┘
             │
┌────────────▼────────────────────────────┐
│       Contracts Layer                    │
│   - DTOs                                 │
│   - Enums                                │
│   - Request/Response models              │
└──────────────────────────────────────────┘
```

### Repository Pattern

All repositories inherit from `BaseRepository<TEntity>`:

```csharp
public class OwnerRepository : BaseRepository<Owner>, IOwnerRepository
{
    public OwnerRepository(ApplicationDbContext context) : base(context) { }
    
    public Task UpdateAsync(Owner owner, byte[] originalRowVersion, ...)
    {
        SetOriginalRowVersion(owner, originalRowVersion);  // Proper concurrency control
        DbSet.Update(owner);
        return Task.CompletedTask;
    }
}
```

**Key Benefits**:
- Proper concurrency control via `SetOriginalRowVersion()`
- Soft delete support via `SoftDelete()`
- Consistent query filtering (excludes deleted records)
- Reduced code duplication

---

## 💡 Usage Examples

### Complete Workflow

```bash
# 1. Create Organization
curl -X POST https://localhost:7123/api/v1/organizations \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Prestige Group",
    "timeZone": "Asia/Kolkata"
  }'

# 2. Create Building
curl -X POST https://localhost:7123/api/v1/organizations/{orgId}/buildings \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "buildingCode": "PRS-LH-001",
    "name": "Prestige Lakeside Habitat",
    "propertyType": 1,
    "totalFloors": 25,
    "hasLift": true
  }'

# 3. Create Units (bulk)
for unit in A-101 A-102 A-103; do
  curl -X POST https://localhost:7123/api/v1/buildings/{buildingId}/units \
    -H "Authorization: Bearer {token}" \
    -H "Content-Type: application/json" \
    -d "{
      \"unitNumber\": \"$unit\",
      \"floor\": 1,
      \"unitType\": 2,
      \"areaSqFt\": 1200,
      \"bedrooms\": 2,
      \"bathrooms\": 2,
      \"furnishing\": 1,
      \"parkingSlots\": 1
    }"
done

# 4. Create Owner
curl -X POST https://localhost:7123/api/v1/organizations/{orgId}/owners \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "ownerType": 1,
    "displayName": "Rajesh Kumar",
    "phone": "+91-9876543210",
    "email": "rajesh@example.com",
    "pan": "ABCDE1234F"
  }'

# 5. List Buildings
curl -X GET https://localhost:7123/api/v1/organizations/{orgId}/buildings \
  -H "Authorization: Bearer {token}"

# 6. List Units
curl -X GET https://localhost:7123/api/v1/buildings/{buildingId}/units \
  -H "Authorization: Bearer {token}"
```

---

## 🗄️ Database Schema

### Tables

1. **Organizations** - Multi-tenant root entities
2. **Buildings** - Property structures  
3. **BuildingAddresses** - 1:1 relationship with Buildings
4. **Units** - Individual units within buildings
5. **Owners** - Property owners
6. **BuildingOwnershipShares** - Building-level ownership
7. **UnitOwnershipShares** - Unit-level ownership overrides
8. **UnitMeters** - Utility meter tracking
9. **Files** - File metadata
10. **BuildingFiles** - Building-File junction
11. **UnitFiles** - Unit-File junction

### Key Indexes

```sql
-- Unique constraints
CREATE UNIQUE INDEX IX_Buildings_OrgId_BuildingCode 
  ON Buildings(OrgId, BuildingCode);

CREATE UNIQUE INDEX IX_Units_BuildingId_UnitNumber 
  ON Units(BuildingId, UnitNumber);

-- Performance indexes
CREATE INDEX IX_Buildings_OrgId ON Buildings(OrgId);
CREATE INDEX IX_Units_BuildingId ON Units(BuildingId);
CREATE INDEX IX_Owners_OrgId ON Owners(OrgId);
```

### Migration

The database migration is: `20260108130627_AddPropertyAndUnitManagement`

Apply migration:
```bash
cd src/TentMan.Infrastructure
dotnet ef database update
```

---

## 🔒 Security

### Authorization

All endpoints require authentication via JWT token:
```http
Authorization: Bearer {your-jwt-token}
```

### Data Isolation

- All entities are scoped to an Organization (`OrgId`)
- Cross-tenant data access is prevented at the database level
- Soft delete ensures audit trail preservation

---

## 🧪 Testing

### Unit Tests

Ownership validation tests:
```csharp
[Fact]
public void ValidateOwnershipShares_WhenSumIs100_ReturnsTrue()
{
    var shares = new[] { 50.00m, 30.00m, 20.00m };
    var result = _service.ValidateOwnershipShares(shares);
    Assert.True(result);
}
```

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~OwnershipServiceTests"
```

---

## 📝 Future Enhancements

Features planned for future releases:
- [ ] File upload implementation (metadata structure ready)
- [ ] Building address management endpoints
- [ ] Ownership share management endpoints
- [ ] Ownership resolution API (building vs unit-level)
- [ ] Tenant/lease management
- [ ] Rent collection and payments
- [ ] Utility bill management
- [ ] Maintenance request tracking

---

## 🤝 Contributing

When adding new features to the property management module:

1. Follow the existing CQRS pattern
2. Inherit repositories from `BaseRepository<T>`
3. Use `SetOriginalRowVersion()` for concurrency control
4. Add appropriate validation in command handlers
5. Filter soft-deleted records in all queries
6. Update this documentation

---

## 📚 Related Documentation

- [Architecture Guide](ARCHITECTURE.md)
- [API Guide](API_GUIDE.md)
- [Database Guide](DATABASE_GUIDE.md)
- [Development Guide](DEVELOPMENT_GUIDE.md)
