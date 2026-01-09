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
| `/api/buildings` | GET | List all buildings (with orgId query param) |
| `/api/buildings` | POST | Create a building |
| `/api/buildings/{id}` | GET | Get a building by ID |
| `/api/buildings/{id}` | PUT | Update a building |
| `/api/buildings/{id}/address` | PUT | Set/update building address |
| `/api/buildings/{id}/units` | POST | Create a unit in a building |
| `/api/buildings/{id}/units/bulk` | POST | Bulk create units in a building |
| `/api/buildings/{id}/ownership-shares` | PUT | Set building ownership shares |
| `/api/buildings/{id}/files` | POST | Add a file to a building |

**Example Request - Create Building**:
```json
POST /api/buildings
{
  "orgId": "org-guid",
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
    "orgId": "org-guid",
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

**Example Request - Update Building**:
```json
PUT /api/buildings/{id}
{
  "name": "Prestige Lakeside Habitat Tower A",
  "propertyType": 1,
  "totalFloors": 30,
  "hasLift": true,
  "notes": "Updated premium apartments",
  "rowVersion": "AAAAAAAAB9E="
}

Response 200 OK:
{
  "success": true,
  "data": { ... },
  "message": "Building updated successfully"
}
```

**Example Request - Set Building Address**:
```json
PUT /api/buildings/{id}/address
{
  "line1": "123 Lake View Road",
  "line2": "Tower A",
  "locality": "Whitefield",
  "city": "Bangalore",
  "district": "Bangalore Urban",
  "state": "Karnataka",
  "postalCode": "560066",
  "landmark": "Near IT Park"
}

Response 200 OK:
{
  "success": true,
  "data": { ... },
  "message": "Building address updated successfully"
}
```

**Example Request - Set Building Ownership**:
```json
PUT /api/buildings/{id}/ownership-shares
{
  "shares": [
    { "ownerId": "owner1-guid", "sharePercent": 60.00 },
    { "ownerId": "owner2-guid", "sharePercent": 40.00 }
  ],
  "effectiveFrom": "2026-01-01T00:00:00Z"
}

Response 200 OK:
{
  "success": true,
  "data": { ... },
  "message": "Building ownership shares updated successfully"
}
```

**Example Request - Bulk Create Units**:
```json
POST /api/buildings/{id}/units/bulk
{
  "units": [
    {
      "unitNumber": "A-101",
      "floor": 1,
      "unitType": 2,
      "areaSqFt": 1200,
      "bedrooms": 2,
      "bathrooms": 2,
      "furnishing": 1,
      "parkingSlots": 1
    },
    {
      "unitNumber": "A-102",
      "floor": 1,
      "unitType": 3,
      "areaSqFt": 1500,
      "bedrooms": 3,
      "bathrooms": 2,
      "furnishing": 2,
      "parkingSlots": 2
    }
  ]
}

Response 201 Created:
{
  "success": true,
  "data": [ ... ],
  "message": "2 units created successfully"
}
```

### Units

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/units/{id}` | PUT | Update a unit |
| `/api/units/{id}/ownership-shares` | PUT | Set unit ownership shares (overrides building) |
| `/api/units/{id}/files` | POST | Add a file to a unit |

**Example Request - Update Unit**:
```json
PUT /api/units/{id}
{
  "floor": 4,
  "unitType": 2,
  "areaSqFt": 1250.50,
  "bedrooms": 2,
  "bathrooms": 2,
  "furnishing": 2,
  "parkingSlots": 1,
  "occupancyStatus": 1,
  "rowVersion": "AAAAAAAAB9E="
}

Response 200 OK:
{
  "success": true,
  "data": { ... },
  "message": "Unit updated successfully"
}
```

**Example Request - Set Unit Ownership**:
```json
PUT /api/units/{id}/ownership-shares
{
  "shares": [
    { "ownerId": "owner1-guid", "sharePercent": 100.00 }
  ],
  "effectiveFrom": "2026-01-01T00:00:00Z"
}

Response 200 OK:
{
  "success": true,
  "data": { ... },
  "message": "Unit ownership shares updated successfully"
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
3. **Each Owner Once**: Each owner can only appear once per ownership set
4. **Positive Shares**: All share percentages must be greater than 0
5. **At Least One Owner**: At least one ownership share is required
6. **Owner Exists**: All referenced owners must exist in the system

**Example**:
```csharp
// Valid - sums to 100%
shares = [50.00%, 30.00%, 20.00%]  ✅

// Valid - within tolerance
shares = [33.335%, 33.335%, 33.335%]  ✅ (sum: 100.005%, within 0.01 tolerance)

// Invalid - does not sum to 100%
shares = [50.00%, 30.00%, 15.00%]  ❌ (sum: 95%)

// Invalid - duplicate owner
shares = [owner1: 50%, owner1: 50%]  ❌ (owner appears twice)

// Invalid - zero share
shares = [owner1: 100%, owner2: 0%]  ❌ (share must be > 0)
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

// Additional validations in command handlers:
// - Check for duplicate owners
// - Check all shares are positive (> 0)
// - Verify all owners exist
```

### Unique Constraints

- **BuildingCode**: Must be unique within an organization
- **UnitNumber**: Must be unique within a building

### Unit Deletion Protection (Future)

The system is designed to support lease management in the future:
- When a unit has an active lease, it cannot be deleted
- This protects tenant data and rental agreements
- The `HasUnitOwnershipOverride` flag indicates if the unit has custom ownership different from building-level ownership

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

## 🖥️ Blazor WASM Frontend

The Property Management module includes a complete Blazor WASM frontend built with MudBlazor components.

### Navigation Structure

The frontend adds a "Property Management" navigation group with the following pages:

| Route | Page | Description |
|-------|------|-------------|
| `/buildings` | Buildings List | View and search all buildings |
| `/buildings/create` | Create Building | 5-step wizard for building creation |
| `/buildings/{id}` | Building Details | Tabbed interface for building management |
| `/owners` | Owners List | View and manage property owners |

### Buildings List (`/buildings`)

**Features:**
- 🔍 **Search**: Filter by name, building code, or city
- 🏠 **Property Type Filter**: Apartment, Independent House, Commercial, Mixed Use
- 📊 **View Toggle**: Switch between card and grid views
- ⚡ **Quick Actions**: View details, add unit buttons

**Screenshot Elements:**
- Search panel with property type dropdown
- Card/grid toggle buttons
- Building cards showing name, code, city/state, unit count
- Quick action buttons

### Create Building Wizard (`/buildings/create`)

A 5-step wizard that avoids the "big form from hell" pattern:

| Step | Name | Fields |
|------|------|--------|
| 1 | Building Info | Code, Name, Property Type, Total Floors, Has Lift, Notes |
| 2 | Address | Line1, Line2, Locality, City, District, State, Postal Code, Landmark |
| 3 | Units | Add individual units or bulk add with pattern (prefix, start, end, floor) |
| 4 | Ownership | Add owners with share percentages (must sum to 100%) |
| 5 | Documents | Upload photos and documents with tagging |

**Wizard Features:**
- Linear navigation (must complete each step before proceeding)
- Validation at each step
- Step icons and status indicators
- Previous/Next navigation buttons

### Building Details (`/buildings/{id}`)

A tabbed interface for managing building details:

#### Units Tab
- **DataGrid** showing: Unit Number, Floor, Type, Furnishing, Status, Ownership Override badge
- **Add Unit Modal**: Create individual units with full details
- **Bulk Add Dialog**: Pattern-based unit creation
  - Prefix (e.g., "A-")
  - Start and end numbers
  - Default floor
  - Default unit type
- **Row Click**: Navigate to unit details

#### Ownership Tab
- **Building Ownership Editor**:
  - Add/remove owner rows
  - Owner dropdown selection
  - Share percentage input (0.01% to 100%, step 0.01%)
  - Running total display
  - Validation: sum must equal 100% (±0.01% tolerance)
  - Duplicate owner prevention
  - Effective date picker
- **Save/Reset buttons** (enabled only when changes exist)

#### Documents Tab
- **Drag & Drop Upload**: File upload zone
- **Thumbnails**: Image preview for photos
- **File List**: List view for documents
- **Tagging**: Photo, Document, Other
- **Delete**: Soft-delete with confirmation

### Owners List (`/owners`)

**Features:**
- 🔍 **Search**: Filter by display name, email, or phone
- ➕ **Add Owner Dialog**: Create new owners with:
  - Display Name
  - Owner Type (Individual/Company)
  - Phone
  - Email
  - PAN (optional)
  - GSTIN (optional)
- ✏️ **Edit Owner Dialog**: Update existing owner details
- 🏷️ **Owner Type Badge**: Color-coded chips (Individual/Company)

### Reusable Components

The following modular components are available in `TentMan.Ui/Components/Common/`:

#### OwnershipEditor
Shared editor for building and unit ownership.

```razor
<OwnershipEditor
    Title="Building Ownership"
    Shares="@_ownershipShares"
    AvailableOwners="@_owners"
    ShowEffectiveDate="true"
    @bind-EffectiveDate="@_effectiveDate"
    SharesChanged="@OnSharesChanged" />
```

**Validations:**
- Sum must equal 100% (shows error chip if not)
- No duplicate owners
- Share percentage must be > 0

#### SearchFilterPanel
Configurable search and filter controls.

```razor
<SearchFilterPanel
    SearchText="@_searchText"
    SearchTextChanged="@OnSearchChanged"
    Filters="@_filterOptions"
    FiltersChanged="@ApplyFilters" />
```

#### FileUploader
Drag-and-drop file upload with tagging.

```razor
<FileUploader
    Files="@_uploadedFiles"
    FilesChanged="@OnFilesChanged"
    OnFileUploaded="@HandleFileUpload" />
```

#### WizardStepper
Multi-step form wizard with validation.

```razor
<WizardStepper Steps="@_steps" @bind-ActiveStep="@_activeStep">
    <StepContent>@_stepContents[_activeStep]</StepContent>
</WizardStepper>
```

### API Clients

Two typed HTTP clients are registered for frontend use:

#### BuildingsApiClient (`IBuildingsApiClient`)

```csharp
// List buildings
var response = await BuildingsClient.GetBuildingsAsync(orgId);

// Get building details
var response = await BuildingsClient.GetBuildingAsync(buildingId);

// Create building
var response = await BuildingsClient.CreateBuildingAsync(request);

// Update building
var response = await BuildingsClient.UpdateBuildingAsync(buildingId, request);

// Set address
var response = await BuildingsClient.SetBuildingAddressAsync(buildingId, request);

// Set ownership
var response = await BuildingsClient.SetBuildingOwnershipAsync(buildingId, request);

// Get units
var response = await BuildingsClient.GetUnitsAsync(buildingId);

// Create unit
var response = await BuildingsClient.CreateUnitAsync(buildingId, request);

// Bulk create units
var response = await BuildingsClient.BulkCreateUnitsAsync(buildingId, request);
```

#### OwnersApiClient (`IOwnersApiClient`)

```csharp
// List owners
var response = await OwnersClient.GetOwnersAsync(orgId);

// Get owner details
var response = await OwnersClient.GetOwnerAsync(orgId, ownerId);

// Create owner
var response = await OwnersClient.CreateOwnerAsync(orgId, request);
```

### Client Registration

The API clients are automatically registered when adding TentMan services:

```csharp
// In Program.cs
builder.Services.AddTentManApiClients(options =>
{
    options.BaseUrl = "https://localhost:7123";
});
```

This registers:
- `IBuildingsApiClient` / `BuildingsApiClient`
- `IOwnersApiClient` / `OwnersApiClient`

---

## 📝 Future Enhancements

Features planned for future releases:
- [x] Blazor WASM frontend screens ✅ Implemented
- [x] Tenant/lease management ✅ Implemented - See [Tenant and Lease Management Guide](TENANT_LEASE_MANAGEMENT.md)
- [ ] File upload implementation (UI ready, storage integration pending)
- [ ] Block unit deletion when active lease exists
- [ ] Rent collection and payments
- [ ] Utility bill management
- [ ] Maintenance request tracking
- [ ] Ownership history reports
- [ ] Multi-building ownership analytics
- [ ] Unit details page
- [ ] Owner linking to user accounts

---

## 🤝 Contributing

When adding new features to the property management module:

1. Follow the existing CQRS pattern
2. Inherit repositories from `BaseRepository<T>`
3. Use `SetOriginalRowVersion()` for concurrency control
4. Add appropriate validation in command handlers
5. Filter soft-deleted records in all queries
6. Update this documentation
7. **For UI changes**: Use code-behind pattern (`.razor` + `.razor.cs`)
8. **For UI changes**: Keep components modular and reusable
9. **For UI changes**: Follow MudBlazor component patterns

---

## 📚 Related Documentation

- [Tenant and Lease Management Guide](TENANT_LEASE_MANAGEMENT.md)
- [Architecture Guide](ARCHITECTURE.md)
- [API Guide](API_GUIDE.md)
- [Database Guide](DATABASE_GUIDE.md)
- [Development Guide](DEVELOPMENT_GUIDE.md)
- [UI Loading Boundaries](tentman-ui/loading-boundaries.md)
