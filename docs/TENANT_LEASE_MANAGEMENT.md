# Tenant and Lease Management Module

Complete guide to the Tenant and Lease Management module in TentMan - a comprehensive system for managing tenants, leases, and rental agreements within properties.

---

## 📚 Table of Contents

- [Overview](#overview)
- [Domain Model](#domain-model)
- [API Endpoints](#api-endpoints)
- [Business Rules](#business-rules)
- [Lease Activation Validation](#lease-activation-validation)
- [Architecture](#architecture)
- [Usage Examples](#usage-examples)
- [Database Schema](#database-schema)
- [Move-In Handover Checklist Feature](#move-in-handover-checklist-feature)
- [Related Documentation](#related-documentation)

---

## 🎯 Overview

The Tenant and Lease Management module provides capabilities for managing:
- **Tenants** - Tenant profiles with contact information and documents
- **Leases** - Rental contracts with financial terms
- **Lease Parties** - Multiple tenants per lease (primary, co-tenant, occupant, guarantor)
- **Lease Terms** - Versioned, append-only financial terms for rent and deposit tracking
- **Deposit Transactions** - Ledger for deposit collection, refunds, and deductions
- **Unit Handover** - Move-in/move-out state tracking with checklists
- **Meter Readings** - Utility meter readings for billing
- **Unit Occupancy History** - Historical tracking of unit occupancy

### Key Features

✅ **One Active Lease Per Unit** - Enforced via filtered unique database index  
✅ **Multi-Tenant Lease Support** - Primary tenant, co-tenants, occupants, and guarantors  
✅ **Immutable Financial Terms** - Append-only versioned lease terms  
✅ **Deposit Ledger** - Full transaction history for deposit handling  
✅ **Comprehensive Validation** - Lease activation gate with business rule enforcement  
✅ **Soft Delete** - Data preservation with audit trails  
✅ **Optimistic Concurrency** - Row version-based conflict detection  

---

## 🏗️ Domain Model

### Core Entities

#### Tenant
Master profile for tenants with unique phone/email per organization.

```csharp
public class Tenant : BaseEntity
{
    public Guid OrgId { get; set; }
    public string FullName { get; set; }
    public string Phone { get; set; }  // Unique per org, E.164 format
    public string? Email { get; set; }  // Unique per org (if provided)
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public bool IsActive { get; set; }
    
    // Navigation properties
    public ICollection<TenantAddress> Addresses { get; set; }
    public ICollection<TenantEmergencyContact> EmergencyContacts { get; set; }
    public ICollection<TenantDocument> Documents { get; set; }
}
```

#### TenantAddress
Multiple addresses per tenant with type classification.

```csharp
public class TenantAddress : BaseEntity
{
    public Guid TenantId { get; set; }
    public AddressType Type { get; set; }  // Current, Permanent, Office, Other
    public string Line1 { get; set; }
    public string? Line2 { get; set; }
    public string City { get; set; }
    public string? District { get; set; }
    public string State { get; set; }
    public string Pincode { get; set; }
    public string Country { get; set; }  // Default: "IN"
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public bool IsPrimary { get; set; }
}
```

#### Lease
Contract header with status, dates, and rental terms.

```csharp
public class Lease : BaseEntity
{
    public Guid OrgId { get; set; }
    public Guid UnitId { get; set; }
    public string? LeaseNumber { get; set; }  // Human-friendly identifier
    public LeaseStatus Status { get; set; }  // Draft, Active, NoticeGiven, Ended, Cancelled
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }  // Null = open-ended
    public byte RentDueDay { get; set; }  // 1-28 recommended
    public byte GraceDays { get; set; }
    public short? NoticePeriodDays { get; set; }
    public LateFeeType LateFeeType { get; set; }
    public decimal? LateFeeValue { get; set; }
    public string? PaymentMethodNote { get; set; }
    public string? TermsText { get; set; }
    public bool IsAutoRenew { get; set; }
    
    // Navigation properties
    public ICollection<LeaseParty> Parties { get; set; }
    public ICollection<LeaseTerm> Terms { get; set; }
    public ICollection<DepositTransaction> DepositTransactions { get; set; }
    public ICollection<UnitOccupancy> Occupancies { get; set; }
    public ICollection<UnitHandover> Handovers { get; set; }
}
```

#### LeaseParty
Join table for multiple tenants per lease with role designation.

```csharp
public class LeaseParty : BaseEntity
{
    public Guid LeaseId { get; set; }
    public Guid TenantId { get; set; }
    public LeasePartyRole Role { get; set; }  // PrimaryTenant, CoTenant, Occupant, Guarantor
    public bool IsResponsibleForPayment { get; set; }
    public DateOnly? MoveInDate { get; set; }
    public DateOnly? MoveOutDate { get; set; }
}
```

#### LeaseTerm
Versioned, append-only financial terms.

```csharp
public class LeaseTerm : BaseEntity
{
    public Guid LeaseId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal? MaintenanceCharge { get; set; }
    public decimal? OtherFixedCharge { get; set; }
    public EscalationType EscalationType { get; set; }  // None, Fixed, Percent
    public decimal? EscalationValue { get; set; }
    public short? EscalationEveryMonths { get; set; }
    public string? Notes { get; set; }
}
```

#### DepositTransaction
Ledger for tracking deposit movements.

```csharp
public class DepositTransaction : BaseEntity
{
    public Guid LeaseId { get; set; }
    public DepositTransactionType TxnType { get; set; }  // Collected, Refund, Deduction, Adjustment
    public decimal Amount { get; set; }  // Always positive
    public DateOnly TxnDate { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}
```

### Supporting Entities

- **TenantEmergencyContact** - Emergency contact details for tenants
- **TenantDocument** - Document metadata with masked document numbers
- **UnitHandover** - Move-in/move-out handover records
- **HandoverChecklistItem** - Checklist items with condition tracking
- **MeterReading** - Utility meter readings
- **UnitOccupancy** - Occupancy history for reporting

### Enums

Located in `TentMan.Contracts.Enums`:

| Enum | Values |
|------|--------|
| **Gender** | Male, Female, Other |
| **AddressType** | Current, Permanent, Office, Other |
| **LeaseStatus** | Draft, Active, NoticeGiven, Ended, Cancelled |
| **LeasePartyRole** | PrimaryTenant, CoTenant, Occupant, Guarantor |
| **LateFeeType** | None, Flat, PerDay, Percent |
| **EscalationType** | None, Fixed, Percent |
| **DepositTransactionType** | Collected, Refund, Deduction, Adjustment |
| **HandoverType** | MoveIn, MoveOut |
| **ItemCondition** | Good, Ok, Bad, Missing |
| **MeterType** | Electricity, Water, Gas |
| **DocumentType** | IDProof, AddressProof, Photo, PoliceVerification, Agreement, Other |
| **UnitOccupancyHistoryStatus** | Occupied, Vacant |

---

## 🌐 API Endpoints

### Tenants

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/v1/organizations/{orgId}/tenants` | POST | Create a new tenant |
| `/api/v1/organizations/{orgId}/tenants` | GET | List/search tenants (with `?search=` param) |
| `/api/tenants/{tenantId}` | GET | Get tenant details |
| `/api/tenants/{tenantId}` | PUT | Update tenant |

**Example - Create Tenant**:
```json
POST /api/v1/organizations/{orgId}/tenants
Authorization: Bearer {token}

{
  "fullName": "Rajesh Kumar",
  "phone": "+919876543210",
  "email": "rajesh.kumar@example.com",
  "dateOfBirth": "1990-01-15",
  "gender": 0  // Male
}

Response 201 Created:
{
  "success": true,
  "data": {
    "id": "tenant-guid",
    "orgId": "org-guid",
    "fullName": "Rajesh Kumar",
    "phone": "+919876543210",
    "email": "rajesh.kumar@example.com",
    "dateOfBirth": "1990-01-15",
    "gender": 0,
    "isActive": true,
    "createdAtUtc": "2026-01-09T07:00:00Z"
  },
  "message": "Tenant created successfully"
}
```

**Example - Search Tenants**:
```json
GET /api/v1/organizations/{orgId}/tenants?search=rajesh
Authorization: Bearer {token}

Response 200 OK:
{
  "success": true,
  "data": [
    {
      "id": "tenant-guid",
      "fullName": "Rajesh Kumar",
      "phone": "+919876543210",
      "email": "rajesh.kumar@example.com",
      "isActive": true
    }
  ],
  "message": "Tenants retrieved successfully"
}
```

### Leases

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/v1/organizations/{orgId}/leases` | POST | Create a draft lease |
| `/api/leases/{leaseId}` | GET | Get lease details |
| `/api/units/{unitId}/leases` | GET | Get lease history for a unit |
| `/api/leases/{leaseId}/parties` | POST | Add tenant/occupant to lease |
| `/api/leases/{leaseId}/terms` | POST | Add versioned financial terms |
| `/api/leases/{leaseId}/activate` | POST | Activate lease (validation gate) |

**Example - Create Draft Lease**:
```json
POST /api/v1/organizations/{orgId}/leases
Authorization: Bearer {token}

{
  "unitId": "unit-guid",
  "leaseNumber": "LSE-2026-001",
  "startDate": "2026-02-01",
  "endDate": "2027-01-31",
  "rentDueDay": 5,
  "graceDays": 3,
  "noticePeriodDays": 30,
  "lateFeeType": 1,  // Flat
  "lateFeeValue": 500.00,
  "isAutoRenew": false
}

Response 201 Created:
{
  "success": true,
  "data": {
    "id": "lease-guid",
    "orgId": "org-guid",
    "unitId": "unit-guid",
    "leaseNumber": "LSE-2026-001",
    "status": 0,  // Draft
    "startDate": "2026-02-01",
    "endDate": "2027-01-31",
    "rentDueDay": 5,
    "graceDays": 3,
    "noticePeriodDays": 30,
    "lateFeeType": 1,
    "lateFeeValue": 500.00,
    "isAutoRenew": false
  },
  "message": "Lease created successfully"
}
```

**Example - Add Party to Lease**:
```json
POST /api/leases/{leaseId}/parties
Authorization: Bearer {token}

{
  "tenantId": "tenant-guid",
  "role": 0,  // PrimaryTenant
  "isResponsibleForPayment": true,
  "moveInDate": "2026-02-01"
}

Response 201 Created:
{
  "success": true,
  "data": { /* LeaseDetailDto with parties */ },
  "message": "Party added to lease successfully"
}
```

**Example - Add Lease Term**:
```json
POST /api/leases/{leaseId}/terms
Authorization: Bearer {token}

{
  "effectiveFrom": "2026-02-01",
  "effectiveTo": null,
  "monthlyRent": 25000.00,
  "securityDeposit": 75000.00,
  "maintenanceCharge": 2500.00,
  "escalationType": 2,  // Percent
  "escalationValue": 5.00,
  "escalationEveryMonths": 12,
  "notes": "Initial lease terms"
}

Response 201 Created:
{
  "success": true,
  "data": { /* LeaseDetailDto with terms */ },
  "message": "Term added to lease successfully"
}
```

### Tenant Portal

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/v1/tenant-portal/lease-summary` | GET | Get current tenant's active lease summary |
| `/api/v1/tenant-portal/documents` | GET | Get all documents for current tenant |
| `/api/v1/tenant-portal/documents` | POST | Upload a document for current tenant |

**Authentication**: All tenant portal endpoints require authentication with the "Tenant" role. The tenant is identified by the linked user ID from the JWT token.

**Example - Get Documents**:
```json
GET /api/v1/tenant-portal/documents
Authorization: Bearer {token}

Response 200 OK:
{
  "success": true,
  "data": [
    {
      "id": "document-guid",
      "docType": 1,  // IDProof
      "docNumberMasked": "XXXX1234",
      "issueDate": "2020-01-15",
      "expiryDate": null,
      "fileId": "file-guid",
      "fileName": "aadhar_card.pdf",
      "notes": "Aadhar card for verification"
    }
  ],
  "message": "Documents retrieved successfully"
}
```

**Example - Upload Document**:
```http
POST /api/v1/tenant-portal/documents
Authorization: Bearer {token}
Content-Type: multipart/form-data

file: [binary file data]
docType: 1  // IDProof
docNumberMasked: XXXX1234
issueDate: 2020-01-15
notes: Aadhar card for verification

Response 201 Created:
{
  "success": true,
  "data": {
    "id": "document-guid",
    "docType": 1,
    "docNumberMasked": "XXXX1234",
    "issueDate": "2020-01-15",
    "expiryDate": null,
    "fileId": "file-guid",
    "fileName": "aadhar_card.pdf",
    "notes": "Aadhar card for verification"
  },
  "message": "Document uploaded successfully"
}
```

**File Upload Validation**:
- Maximum file size: 10MB
- Allowed file types: PDF, JPEG, PNG, DOC, DOCX
- Document type is required
- Files are stored in **Azure Blob Storage** in the `tenant-documents` container
- SHA256 hash is computed for file integrity
- Storage key format: `container-name/blob-name`

**Example - Activate Lease**:
```json
POST /api/leases/{leaseId}/activate
Authorization: Bearer {token}

{
  "rowVersion": "AAAAAAAAB9E="
}

Response 200 OK:
{
  "success": true,
  "data": {
    "id": "lease-guid",
    "status": 1,  // Active
    /* ... other lease details */
  },
  "message": "Lease activated successfully"
}
```

---

## 📋 Business Rules

### One Active Lease Per Unit

The system enforces a critical business rule that only **one active lease** can exist per unit at any time:

- Implemented via a filtered unique database index:
```sql
CREATE UNIQUE NONCLUSTERED INDEX [IX_Leases_OrgId_UnitId_Active] 
ON [Leases] ([OrgId], [UnitId])
WHERE [Status] IN (1, 2) AND [IsDeleted] = 0
-- Status 1 = Active, Status 2 = NoticeGiven
```

- Attempting to activate a second lease for the same unit will fail with:
```json
{
  "success": false,
  "message": "This unit already has an active lease. Only one active lease per unit is allowed."
}
```

### Tenant Uniqueness

- **Phone**: Must be unique per organization (excluding soft-deleted tenants)
- **Email**: Must be unique per organization if provided (excluding soft-deleted tenants)

### Lease Term Immutability

Financial terms are **append-only**:
- Never update existing terms; always add new versions
- Each term has `EffectiveFrom` and optional `EffectiveTo` dates
- Historical terms are preserved for audit and billing reconciliation

### Deposit Ledger

All deposit movements are tracked as transactions:
- **Collected**: Initial deposit or additional amounts
- **Refund**: Return of deposit to tenant
- **Deduction**: Deductions for damages, unpaid rent, etc.
- **Adjustment**: Corrections or adjustments

The system calculates:
- **TotalDepositCollected**: Sum of all "Collected" transactions
- **DepositBalance**: TotalCollected - (Refunds + Deductions)

---

## ✅ Lease Activation Validation

When activating a lease, the system performs a comprehensive validation gate:

| Rule | Description |
|------|-------------|
| **Draft Status** | Lease must be in Draft status to activate |
| **No Existing Active Lease** | Unit must not have another Active or NoticeGiven lease |
| **Valid Date Range** | If EndDate is specified, it must be after StartDate |
| **Primary Tenant Required** | At least one LeaseParty with role PrimaryTenant |
| **Term Coverage** | At least one LeaseTerm covering the StartDate |
| **Valid RentDueDay** | RentDueDay must be between 1 and 28 |

**Validation is atomic**: If any rule fails, no changes are made.

**Example Validation Errors**:
```json
// Missing Primary Tenant
{
  "success": false,
  "message": "Lease must have at least one Primary Tenant"
}

// No term covering start date
{
  "success": false,
  "message": "Lease must have at least one term covering the start date"
}

// Invalid RentDueDay
{
  "success": false,
  "message": "RentDueDay must be between 1 and 28"
}
```

---

## 🏛️ Architecture

### Clean Architecture Layers

```
┌─────────────────────────────────────────┐
│         API Layer (Controllers)          │
│   - TenantsController                    │
│   - LeasesController                     │
└────────────┬────────────────────────────┘
             │
┌────────────▼────────────────────────────┐
│    Application Layer (CQRS)              │
│   Commands:                              │
│   - CreateTenantCommand                  │
│   - UpdateTenantCommand                  │
│   - CreateLeaseCommand                   │
│   - AddLeasePartyCommand                 │
│   - AddLeaseTermCommand                  │
│   - ActivateLeaseCommand                 │
│                                          │
│   Queries:                               │
│   - GetTenantsQuery                      │
│   - GetTenantByIdQuery                   │
│   - GetLeasesByUnitQuery                 │
│   - GetLeaseByIdQuery                    │
│                                          │
│   Mappers (Shared):                      │
│   - TenantMapper.ToDetailDto()           │
│   - LeaseMapper.ToDetailDto()            │
└────────────┬────────────────────────────┘
             │
┌────────────▼────────────────────────────┐
│    Infrastructure Layer                  │
│   Repositories:                          │
│   - TenantRepository                     │
│   - LeaseRepository                      │
│   - FileMetadataRepository               │
│                                          │
│   - UnitOfWork (extended)                │
│   - ApplicationDbContext (DbSets added)  │
└────────────┬────────────────────────────┘
             │
┌────────────▼────────────────────────────┐
│         Domain Layer                     │
│   - Tenant, TenantAddress, etc.          │
│   - Lease, LeaseParty, LeaseTerm, etc.   │
│   - DepositTransaction, UnitHandover     │
└──────────────────────────────────────────┘
```

### Repository Pattern

```csharp
public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Tenant?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Tenant>> GetByOrgIdAsync(Guid orgId, string? search = null, CancellationToken ct = default);
    Task<Tenant> AddAsync(Tenant tenant, CancellationToken ct = default);
    Task UpdateAsync(Tenant tenant, byte[] originalRowVersion, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> PhoneExistsAsync(Guid orgId, string phone, Guid? excludeTenantId = null, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(Guid orgId, string email, Guid? excludeTenantId = null, CancellationToken ct = default);
}

public interface ILeaseRepository
{
    Task<Lease?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Lease?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Lease>> GetByUnitIdAsync(Guid unitId, CancellationToken ct = default);
    Task<Lease> AddAsync(Lease lease, CancellationToken ct = default);
    Task UpdateAsync(Lease lease, byte[] originalRowVersion, CancellationToken ct = default);
    Task<bool> HasActiveLeaseAsync(Guid unitId, Guid? excludeLeaseId = null, CancellationToken ct = default);
}
```

---

## 💡 Usage Examples

### Complete Workflow

```bash
# 1. Create a Tenant
curl -X POST https://localhost:7123/api/v1/organizations/{orgId}/tenants \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Rajesh Kumar",
    "phone": "+919876543210",
    "email": "rajesh@example.com"
  }'

# 2. Create a Draft Lease
curl -X POST https://localhost:7123/api/v1/organizations/{orgId}/leases \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "unitId": "unit-guid",
    "leaseNumber": "LSE-2026-001",
    "startDate": "2026-02-01",
    "endDate": "2027-01-31",
    "rentDueDay": 5,
    "graceDays": 3
  }'

# 3. Add Primary Tenant to Lease
curl -X POST https://localhost:7123/api/leases/{leaseId}/parties \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": "tenant-guid",
    "role": 0,
    "isResponsibleForPayment": true,
    "moveInDate": "2026-02-01"
  }'

# 4. Add Lease Terms
curl -X POST https://localhost:7123/api/leases/{leaseId}/terms \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "effectiveFrom": "2026-02-01",
    "monthlyRent": 25000.00,
    "securityDeposit": 75000.00,
    "maintenanceCharge": 2500.00
  }'

# 5. Activate Lease
curl -X POST https://localhost:7123/api/leases/{leaseId}/activate \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "rowVersion": "AAAAAAAAB9E="
  }'
```

---

## 🗄️ Database Schema

### New Tables

| Table | Description |
|-------|-------------|
| **Tenants** | Tenant master profiles |
| **TenantAddresses** | Multiple addresses per tenant |
| **TenantEmergencyContacts** | Emergency contact information |
| **TenantDocuments** | Document metadata with masked numbers |
| **Leases** | Lease contract headers |
| **LeaseParties** | Join table for multiple tenants per lease |
| **LeaseTerms** | Versioned financial terms |
| **DepositTransactions** | Deposit ledger |
| **UnitHandovers** | Move-in/move-out records |
| **HandoverChecklistItems** | Handover checklist details |
| **MeterReadings** | Utility meter readings |
| **UnitOccupancies** | Occupancy history |

### Key Indexes

```sql
-- Unique constraint: one active lease per unit
CREATE UNIQUE NONCLUSTERED INDEX [IX_Leases_OrgId_UnitId_Active] 
ON [Leases] ([OrgId], [UnitId])
WHERE [Status] IN (1, 2) AND [IsDeleted] = 0;

-- Unique phone per organization (excluding deleted)
CREATE UNIQUE NONCLUSTERED INDEX [IX_Tenants_OrgId_Phone] 
ON [Tenants] ([OrgId], [Phone])
WHERE [IsDeleted] = 0;

-- Unique lease party per lease-tenant combination
CREATE UNIQUE NONCLUSTERED INDEX [IX_LeaseParties_LeaseId_TenantId] 
ON [LeaseParties] ([LeaseId], [TenantId]);

-- Lease term effective date index
CREATE INDEX [IX_LeaseTerms_LeaseId_EffectiveFrom] 
ON [LeaseTerms] ([LeaseId], [EffectiveFrom] DESC);
```

---

## 🧪 Testing

### Unit Tests

The module includes comprehensive unit tests:

**LeaseMapperTests** (5 tests):
- Basic property mapping
- Filtering deleted parties
- Filtering deleted terms
- Deposit balance calculation
- Active term identification

**TenantMapperTests** (5 tests):
- Basic property mapping
- Address mapping
- Emergency contact mapping
- Document mapping
- Empty collection handling

**ActivateLeaseCommandHandlerTests** (6 tests):
- Draft status validation
- Active lease conflict detection
- Primary tenant requirement
- Term coverage validation
- RentDueDay range validation
- EndDate after StartDate validation

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~TenantManagement"
```

---

## 📝 Future Enhancements

Features planned for future releases:
- [x] Blazor WASM frontend screens for tenant and lease management ✅
- [x] Move-in handover checklist with digital signature ✅
- [ ] End lease workflow with deposit settlement
- [ ] Rent collection and payment tracking
- [ ] Utility bill management
- [ ] Maintenance request tracking
- [ ] Lease renewal automation
- [ ] Tenant document upload to storage (API integration)
- [ ] Lease document storage

---

## 🖥️ Blazor UI Pages

The Tenant and Lease Management module includes complete Blazor WASM frontend screens.

### Navigation

The UI adds a "Tenant Management" navigation group:

```
📁 Tenant Management
├── 👥 Tenants
└── 📝 Create Lease
```

### Tenants List (`/tenants`)

**File**: `Pages/Tenants/TenantsList.razor`

Displays all tenants with search and add/edit capabilities.

**Features:**
- 🔍 **Search**: Filter by name or phone (600ms debounce to reduce API calls)
- ➕ **Add Tenant Dialog**: Create new tenants with profile information
- ✏️ **Edit Tenant Dialog**: Update existing tenant details
- 🏷️ **Status Badges**: Color-coded Active/Inactive chips
- 👁️ **View Details**: Navigate to tenant details page

### Tenant Details (`/tenants/{id}`)

**File**: `Pages/Tenants/TenantDetails.razor`

Tabbed interface for managing tenant details.

**Tabs:**

| Tab | Features |
|-----|----------|
| **Profile** | View tenant personal information (name, phone, email, DOB, gender) |
| **Addresses** | View and add tenant addresses (Current, Permanent, etc.) |
| **Documents** | Upload and preview documents (ID proofs, address proofs) with FileUploader |
| **Leases** | View tenant's lease history with status badges and count |

### Create Lease Wizard (`/leases/create`)

**File**: `Pages/Leases/CreateLease.razor`

A 7-step wizard for creating new leases:

| Step | Name | Description |
|------|------|-------------|
| 1 | **Select Unit** | Dropdown with unit summary (type, area, bedrooms, bathrooms) |
| 2 | **Dates & Rules** | Start date, end date, rent due day (1-28), grace days, notice period, late fee settings, auto-renew |
| 3 | **Add Parties** | Search existing tenant by phone/name or create inline; set role and payment responsibility |
| 4 | **Financial Terms** | Monthly rent, security deposit, maintenance, other charges, escalation rules |
| 5 | **Documents** | Upload lease agreement, ID proofs, address proofs using FileUploader |
| 6 | **Move-in Handover** | Checklist items with condition, meter readings, handover photos |
| 7 | **Review & Activate** | Validation summary panel, lease summary, activate button |

**Validation Rules Enforced:**
- Unit must be selected
- Start date is required
- At least one party is required
- At least one party must be Primary Tenant
- At least one party must be responsible for payment
- Monthly rent must be greater than zero
- Security deposit cannot be negative

### API Clients

Two API clients are provided for the Blazor frontend:

#### ITenantsApiClient

```csharp
// List/search tenants
var response = await TenantsClient.GetTenantsAsync(orgId, search: "9876543210");

// Get tenant details
var response = await TenantsClient.GetTenantAsync(tenantId);

// Create tenant
var response = await TenantsClient.CreateTenantAsync(orgId, request);

// Update tenant
var response = await TenantsClient.UpdateTenantAsync(tenantId, request);
```

#### ILeasesApiClient

```csharp
// Create draft lease
var response = await LeasesClient.CreateLeaseAsync(orgId, request);

// Get lease details
var response = await LeasesClient.GetLeaseAsync(leaseId);

// Get leases for unit
var response = await LeasesClient.GetLeasesByUnitAsync(unitId);

// Add party to lease
var response = await LeasesClient.AddLeasePartyAsync(leaseId, request);

// Add financial terms
var response = await LeasesClient.AddLeaseTermAsync(leaseId, request);

// Activate lease
var response = await LeasesClient.ActivateLeaseAsync(leaseId, request);
```

---

## 📋 Move-In Handover Checklist Feature

The Move-In Handover Checklist provides a digital, signature-based system for documenting property condition at tenant move-in.

### Overview

When a lease is created, a move-in handover record is automatically generated with checklist items documenting the condition of the property. Tenants can access this through their portal, review the checklist, and submit their digital signature to confirm the property condition.

### Key Features

✅ **Digital Signature** - HTML5 Canvas-based signature pad for capturing tenant signatures  
✅ **Checklist Items** - Categorized items (Electrical, Plumbing, Furniture, etc.) with condition tracking  
✅ **Condition States** - Good, Ok, Bad, Missing  
✅ **Meter Readings** - Initial utility meter readings (Electricity, Water, Gas)  
✅ **Photo Attachments** - Support for attaching photos to checklist items  
✅ **Immutable After Submission** - Read-only view once tenant signs  
✅ **Secure Storage** - Signatures stored in Azure Blob Storage with SHA256 verification  

### Data Model

#### UnitHandover Entity

```csharp
public class UnitHandover : BaseEntity
{
    public Guid LeaseId { get; set; }
    public HandoverType Type { get; set; }  // MoveIn or MoveOut
    public DateOnly Date { get; set; }
    public string? Notes { get; set; }
    public bool SignedByTenant { get; set; }
    public bool SignedByOwner { get; set; }
    public Guid? SignatureTenantFileId { get; set; }
    public Guid? SignatureOwnerFileId { get; set; }
    
    // Navigation properties
    public Lease Lease { get; set; }
    public FileMetadata? SignatureTenantFile { get; set; }
    public FileMetadata? SignatureOwnerFile { get; set; }
    public ICollection<HandoverChecklistItem> ChecklistItems { get; set; }
}
```

#### HandoverChecklistItem Entity

```csharp
public class HandoverChecklistItem : BaseEntity
{
    public Guid HandoverId { get; set; }
    public string Category { get; set; }        // Electrical, Plumbing, Furniture
    public string ItemName { get; set; }
    public ItemCondition Condition { get; set; } // Good, Ok, Bad, Missing
    public string? Remarks { get; set; }
    public Guid? PhotoFileId { get; set; }
    
    // Navigation properties
    public UnitHandover Handover { get; set; }
    public FileMetadata? PhotoFile { get; set; }
}
```

### Tenant Portal API Endpoints

#### Get Move-In Handover

```http
GET /api/v1/tenant-portal/move-in-handover
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "handoverId": "guid",
    "leaseId": "guid",
    "unitNumber": "A-101",
    "buildingName": "Building A",
    "date": "2026-01-09",
    "isCompleted": false,
    "notes": "Additional notes",
    "checklistItems": [
      {
        "id": "guid",
        "category": "Electrical",
        "itemName": "Living Room - Light Fixtures",
        "condition": "Good",
        "remarks": "All working",
        "photoFileId": null,
        "photoFileName": null
      }
    ],
    "meterReadings": [
      {
        "meterId": "guid",
        "meterType": "Electricity",
        "reading": 1234.5,
        "readingDate": "2026-01-09"
      }
    ]
  }
}
```

#### Submit Move-In Handover

```http
POST /api/v1/tenant-portal/move-in-handover/submit
Authorization: Bearer {token}
Content-Type: multipart/form-data
```

**Request (Form Data):**
- `handoverId` (guid) - ID of the handover record
- `notes` (string, optional) - Additional notes
- `signatureImage` (file) - PNG/JPEG signature image (max 2MB)
- `checklistItems` (JSON array) - Updated checklist items
- `meterReadings` (JSON array) - Updated meter readings

**Response:** Same as GET response with `isCompleted: true`

### Blazor UI Components

#### Tenant Portal Page

**Route**: `/tenant/move-in`  
**File**: `Pages/Tenant/MoveInHandover.razor`

**Features:**
- 📋 **Checklist Display** - Shows all checklist items grouped by category
- 🎨 **Digital Signature Pad** - HTML5 Canvas for drawing signature
- 📊 **Meter Readings** - Display of utility meter readings
- 📝 **Notes Section** - Additional remarks field
- ✅ **Validation** - Ensures signature is provided before submission
- 🔒 **Read-Only After Submission** - Locks UI once signed
- 📱 **Touch Support** - Works on mobile devices

#### Signature Pad Implementation

**File**: `wwwroot/signature-pad.js`

Uses HTML5 Canvas API with:
- Mouse event handling for desktop
- Touch event handling for mobile devices
- Base64 PNG export for signature image
- Clear functionality to restart signature
- JavaScript Interop with Blazor component

**Usage:**
```javascript
// Initialize signature pad
await JS.InvokeVoidAsync("initSignaturePad", DotNetObjectReference.Create(this));

// Clear signature
await JS.InvokeVoidAsync("clearSignaturePad");

// Signature data is automatically sent to Blazor via JSInvokable callback
[JSInvokable]
public void SetSignatureData(string dataUrl) { ... }
```

### Workflow

1. **Lease Creation** - Handover record created automatically with Step 6 of Create Lease Wizard
2. **Tenant Access** - Tenant navigates to `/tenant/move-in` in tenant portal
3. **Review Checklist** - Tenant reviews pre-filled checklist items and meter readings
4. **Add Remarks** - Tenant can add notes/remarks to any checklist item
5. **Draw Signature** - Tenant draws signature on canvas pad
6. **Submit** - Signature converted to PNG and uploaded with checklist data
7. **Lock** - Handover becomes read-only after submission

### Business Rules

- ✅ Tenant must be logged in and linked to an active lease
- ✅ Only one move-in handover per lease (move-out is separate)
- ✅ Signature image is required for submission
- ✅ Signature must be PNG or JPEG format
- ✅ Signature file size limited to 2MB
- ✅ Once signed by tenant, handover cannot be modified
- ✅ Signature stored securely in Azure Blob Storage with SHA256 hash
- ✅ Handover snapshot saved in lease record for audit trail

### Security

- 🔒 **Authorization** - Tenant can only access their own lease handover
- 🔒 **Validation** - Server-side validation of all submitted data
- 🔒 **Immutability** - Signed handovers cannot be modified
- 🔒 **Audit Trail** - All changes tracked with user ID and timestamp
- 🔒 **File Integrity** - SHA256 hash verification for signature images
- 🔒 **Secure Storage** - Azure Blob Storage with access controls

### Testing

Unit tests are provided for the handover query and command handlers:

**File**: `tests/TentMan.UnitTests/.../GetMoveInHandoverQueryHandlerTests.cs`

```csharp
[Fact]
public async Task Handle_ValidHandover_ReturnsHandoverResponse()
{
    // Test that valid handover data is returned correctly
}

[Fact]
public async Task Handle_NoTenantFound_ReturnsNull()
{
    // Test that null is returned when tenant not found
}
```

---

## 📚 Related Documentation

- [Property Management Guide](PROPERTY_MANAGEMENT.md)
- [Architecture Guide](ARCHITECTURE.md)
- [API Guide](API_GUIDE.md)
- [Database Guide](DATABASE_GUIDE.md)
- [Development Guide](DEVELOPMENT_GUIDE.md)

---

**Last Updated**: 2026-01-09  
**Version**: 1.0  
**Maintainer**: TentMan Development Team
