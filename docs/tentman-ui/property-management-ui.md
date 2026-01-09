# Property Management UI Components

This guide covers the Blazor WASM frontend components for the Property Management module, built with MudBlazor.

---

## 📚 Table of Contents

- [Overview](#overview)
- [Pages](#pages)
- [Tenant Management Pages](#tenant-management-pages)
- [Reusable Components](#reusable-components)
- [API Clients](#api-clients)
- [Usage Patterns](#usage-patterns)

---

## 🎯 Overview

The Property Management UI provides a complete frontend experience for managing buildings, units, owners, tenants, and leases. All components follow the code-behind pattern with separate `.razor` and `.razor.cs` files.

### Navigation

The UI adds navigation groups under the authenticated section:

```
📁 Property Management
├── 🏢 Buildings
└── 👥 Owners

📁 Tenant Management
├── 👥 Tenants
└── 📝 Create Lease
```

---

## 📄 Pages

### Buildings List (`/buildings`)

**File**: `Pages/Buildings/BuildingsList.razor`

Displays all buildings for the current organization with search and filter capabilities.

**Features:**
- Search by name, building code, or city
- Filter by property type (Apartment, Independent House, Commercial, Mixed Use)
- Toggle between card and grid views
- Quick actions: View Details, Add Unit

**Usage:**
```razor
@page "/buildings"
@inject IBuildingsApiClient BuildingsClient
@inject UiState UiState

<BuildingsList />
```

### Create Building Wizard (`/buildings/create`)

**File**: `Pages/Buildings/CreateBuilding.razor`

A 5-step wizard for creating new buildings with comprehensive validation.

**Steps:**
1. **Building Info**: Code, Name, Property Type, Total Floors, Has Lift, Notes
2. **Address**: Full address with locality, city, district, state, postal code
3. **Units**: Add individual units or bulk add with pattern
4. **Ownership**: Add owners with share percentages (must sum to 100%)
5. **Documents**: Upload photos and documents with tagging

**Validation Rules:**
- Building code and name are required
- Address fields (Line1, Locality, City, District, State, Postal Code) are required
- Ownership shares must sum to 100% (±0.01% tolerance)
- No duplicate owners allowed

### Building Details (`/buildings/{id}`)

**File**: `Pages/Buildings/BuildingDetails.razor`

Tabbed interface for managing building details.

**Tabs:**
1. **Units**: DataGrid with add/bulk-add functionality
2. **Ownership**: Share percentage editor with validation
3. **Documents**: File management with drag-and-drop

**Example:**
```razor
@page "/buildings/{BuildingId:guid}"
@inject IBuildingsApiClient BuildingsClient
@inject IOwnersApiClient OwnersClient

<BuildingDetails BuildingId="@BuildingId" />
```

### Owners List (`/owners`)

**File**: `Pages/Owners/OwnersList.razor`

Displays all owners for the current organization with search and add/edit capabilities.

**Features:**
- Search by display name, email, or phone
- Add new owner dialog
- Edit existing owner dialog
- Owner type badges (Individual/Company)

---

## 👥 Tenant Management Pages

### Tenants List (`/tenants`)

**File**: `Pages/Tenants/TenantsList.razor`

Displays all tenants for the current organization with search and add/edit capabilities.

**Features:**
- Search by name or phone (600ms debounce to reduce API calls)
- Add new tenant dialog
- Edit existing tenant dialog
- Active/Inactive status badges
- Link to view tenant details

**Usage:**
```razor
@page "/tenants"
@inject ITenantsApiClient TenantsClient
@inject UiState UiState

<TenantsList />
```

### Tenant Details (`/tenants/{id}`)

**File**: `Pages/Tenants/TenantDetails.razor`

Tabbed interface for managing tenant details.

**Tabs:**
1. **Profile**: Tenant personal information (name, phone, email, date of birth, gender)
2. **Addresses**: View and add tenant addresses
3. **Documents**: Upload and preview tenant documents (ID proofs, address proofs)
4. **Leases**: View tenant's lease history with status badges

**Example:**
```razor
@page "/tenants/{TenantId:guid}"
@inject ITenantsApiClient TenantsClient
@inject ILeasesApiClient LeasesClient

<TenantDetails TenantId="@TenantId" />
```

### Create Lease Wizard (`/leases/create`)

**File**: `Pages/Leases/CreateLease.razor`

A 7-step wizard for creating new leases with comprehensive validation.

**Steps:**
1. **Select Unit**: Dropdown with unit summary (type, bedrooms, bathrooms, area)
2. **Dates & Rules**: Start date, end date, rent due day (1-28), grace days, notice period, late fee settings, auto-renew option
3. **Add Parties**: Search existing tenant by phone/name or create tenant inline, set role (Primary/Co-Tenant/Occupant/Guarantor), mark as responsible for payment
4. **Financial Terms**: Monthly rent, security deposit, maintenance charge, other fixed charges, escalation type and value
5. **Documents**: Upload lease agreement, ID proofs, address proofs using FileUploader component
6. **Move-in Handover**: Checklist with condition tracking, initial meter readings, handover photos
7. **Review & Activate**: Validation summary panel, complete lease summary, activate button

**Validation Rules:**
- Unit selection is required
- Start date is required
- At least one party is required
- At least one party must be a Primary Tenant
- At least one party must be responsible for payment
- Monthly rent must be greater than zero
- Security deposit cannot be negative

**Example:**
```razor
@page "/leases/create"
@page "/leases/create/{UnitId:guid}"
@inject ILeasesApiClient LeasesClient
@inject ITenantsApiClient TenantsClient
@inject IBuildingsApiClient BuildingsClient

<CreateLease UnitId="@UnitId" />
```

---

## 🧩 Reusable Components

### OwnershipEditor

**File**: `Components/Common/OwnershipEditor.razor`

Shared component for editing ownership shares.

**Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `Title` | `string` | Section title |
| `Shares` | `List<OwnershipShareModel>` | Current ownership shares |
| `AvailableOwners` | `IEnumerable<OwnerDto>` | List of available owners |
| `ReadOnly` | `bool` | Whether the editor is read-only |
| `ShowEffectiveDate` | `bool` | Whether to show effective date picker |
| `EffectiveDate` | `DateTime?` | The effective date for ownership |
| `SharesChanged` | `EventCallback<List<OwnershipShareModel>>` | Callback when shares change |

**Validation:**
- Running total displayed (must equal 100%)
- Error chip shown if total != 100%
- Duplicate owner detection
- Share must be > 0

**Example:**
```razor
<OwnershipEditor
    Title="Building Ownership"
    Shares="@_ownershipShares"
    AvailableOwners="@_owners"
    ShowEffectiveDate="true"
    @bind-EffectiveDate="@_effectiveDate"
    SharesChanged="@OnSharesChanged" />

@code {
    private List<OwnershipShareModel> _ownershipShares = new();
    private IEnumerable<OwnerDto> _owners = new List<OwnerDto>();
    private DateTime? _effectiveDate = DateTime.UtcNow.Date;

    private void OnSharesChanged(List<OwnershipShareModel> shares)
    {
        _ownershipShares = shares;
    }
}
```

### SearchFilterPanel

**File**: `Components/Common/SearchFilterPanel.razor`

Configurable search and filter controls.

**Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `SearchText` | `string` | Current search text |
| `SearchTextChanged` | `EventCallback<string>` | Callback when search text changes |
| `SearchPlaceholder` | `string` | Placeholder text for search input |
| `Filters` | `List<FilterOption>` | List of filter configurations |
| `FiltersChanged` | `EventCallback` | Callback when any filter changes |

**Example:**
```razor
<SearchFilterPanel
    SearchText="@_searchText"
    SearchTextChanged="@OnSearchChanged"
    SearchPlaceholder="Search by name or code..."
    Filters="@_filterOptions"
    FiltersChanged="@ApplyFilters" />

@code {
    private string _searchText = string.Empty;
    private List<FilterOption> _filterOptions = new()
    {
        new FilterOption
        {
            Label = "Property Type",
            Options = Enum.GetValues<PropertyType>()
                .Select(pt => new FilterChoice { Text = pt.ToString(), Value = pt.ToString() })
                .ToList()
        }
    };
}
```

### FileUploader

**File**: `Components/Common/FileUploader.razor`

Drag-and-drop file upload with tagging.

**Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `Files` | `List<FileUploadModel>` | Current uploaded files |
| `FilesChanged` | `EventCallback<List<FileUploadModel>>` | Callback when files change |
| `OnFileUploaded` | `EventCallback<IBrowserFile>` | Callback when a file is uploaded |
| `MaxFileSize` | `long` | Maximum file size in bytes |
| `Accept` | `string` | Accepted file types |

**Example:**
```razor
<FileUploader
    Files="@_uploadedFiles"
    FilesChanged="@OnFilesChanged"
    OnFileUploaded="@HandleFileUpload"
    MaxFileSize="10485760"
    Accept="image/*,application/pdf" />

@code {
    private List<FileUploadModel> _uploadedFiles = new();

    private void OnFilesChanged(List<FileUploadModel> files)
    {
        _uploadedFiles = files;
    }

    private async Task HandleFileUpload(IBrowserFile file)
    {
        // Handle file upload to storage
    }
}
```

### WizardStepper

**File**: `Components/Common/WizardStepper.razor`

Multi-step form wizard with validation.

**Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `Steps` | `List<WizardStep>` | List of wizard steps |
| `ActiveStep` | `int` | Current active step index |
| `ActiveStepChanged` | `EventCallback<int>` | Callback when step changes |
| `OnComplete` | `EventCallback` | Callback when wizard completes |
| `Linear` | `bool` | Whether steps must be completed in order |

**Example:**
```razor
<WizardStepper
    Steps="@_steps"
    @bind-ActiveStep="@_activeStep"
    Linear="true"
    OnComplete="@SubmitAsync">
    @switch (_activeStep)
    {
        case 0:
            <Step1Content />
            break;
        case 1:
            <Step2Content />
            break;
    }
</WizardStepper>
```

---

## 🌐 API Clients

### IBuildingsApiClient

Typed HTTP client for buildings and units operations.

**Methods:**
```csharp
// Buildings
Task<ApiResponse<IEnumerable<BuildingListDto>>> GetBuildingsAsync(Guid orgId, CancellationToken ct = default);
Task<ApiResponse<BuildingDetailDto>> GetBuildingAsync(Guid buildingId, CancellationToken ct = default);
Task<ApiResponse<BuildingDetailDto>> CreateBuildingAsync(CreateBuildingRequest request, CancellationToken ct = default);
Task<ApiResponse<BuildingDetailDto>> UpdateBuildingAsync(Guid buildingId, UpdateBuildingRequest request, CancellationToken ct = default);
Task<ApiResponse<BuildingDetailDto>> SetBuildingAddressAsync(Guid buildingId, SetBuildingAddressRequest request, CancellationToken ct = default);
Task<ApiResponse<BuildingDetailDto>> SetBuildingOwnershipAsync(Guid buildingId, SetBuildingOwnershipRequest request, CancellationToken ct = default);

// Units
Task<ApiResponse<IEnumerable<UnitListDto>>> GetUnitsAsync(Guid buildingId, CancellationToken ct = default);
Task<ApiResponse<UnitListDto>> CreateUnitAsync(Guid buildingId, CreateUnitRequest request, CancellationToken ct = default);
Task<ApiResponse<List<UnitListDto>>> BulkCreateUnitsAsync(Guid buildingId, BulkCreateUnitsRequest request, CancellationToken ct = default);
```

### IOwnersApiClient

Typed HTTP client for owner operations.

**Methods:**
```csharp
Task<ApiResponse<IEnumerable<OwnerDto>>> GetOwnersAsync(Guid orgId, CancellationToken ct = default);
Task<ApiResponse<OwnerDto>> GetOwnerAsync(Guid orgId, Guid ownerId, CancellationToken ct = default);
Task<ApiResponse<OwnerDto>> CreateOwnerAsync(Guid orgId, CreateOwnerRequest request, CancellationToken ct = default);
```

### ITenantsApiClient

Typed HTTP client for tenant operations.

**Methods:**
```csharp
// Tenants
Task<ApiResponse<IEnumerable<TenantListDto>>> GetTenantsAsync(Guid orgId, string? search = null, CancellationToken ct = default);
Task<ApiResponse<TenantDetailDto>> GetTenantAsync(Guid tenantId, CancellationToken ct = default);
Task<ApiResponse<TenantListDto>> CreateTenantAsync(Guid orgId, CreateTenantRequest request, CancellationToken ct = default);
Task<ApiResponse<TenantDetailDto>> UpdateTenantAsync(Guid tenantId, UpdateTenantRequest request, CancellationToken ct = default);
```

### ILeasesApiClient

Typed HTTP client for lease operations.

**Methods:**
```csharp
// Leases
Task<ApiResponse<LeaseListDto>> CreateLeaseAsync(Guid orgId, CreateLeaseRequest request, CancellationToken ct = default);
Task<ApiResponse<LeaseDetailDto>> GetLeaseAsync(Guid leaseId, CancellationToken ct = default);
Task<ApiResponse<IEnumerable<LeaseListDto>>> GetLeasesByUnitAsync(Guid unitId, CancellationToken ct = default);

// Lease Parties
Task<ApiResponse<LeaseDetailDto>> AddLeasePartyAsync(Guid leaseId, AddLeasePartyRequest request, CancellationToken ct = default);

// Lease Terms
Task<ApiResponse<LeaseDetailDto>> AddLeaseTermAsync(Guid leaseId, AddLeaseTermRequest request, CancellationToken ct = default);

// Activation
Task<ApiResponse<LeaseDetailDto>> ActivateLeaseAsync(Guid leaseId, ActivateLeaseRequest request, CancellationToken ct = default);
```

---

## 🎨 Usage Patterns

### Loading State Management

All pages use `UiState` and `BusyBoundary` for consistent loading states:

```csharp
private async Task LoadBuildingsAsync()
{
    using var busyScope = UiState.Busy.Begin("Loading buildings...");
    UiState.Busy.ClearError();

    try
    {
        var response = await BuildingsClient.GetBuildingsAsync(OrgId);

        if (response.Success && response.Data != null)
        {
            _buildings = response.Data;
            ApplyFilters();
        }
        else
        {
            UiState.Busy.SetError(response.Message ?? "Failed to load buildings.");
        }
    }
    catch (Exception ex)
    {
        UiState.Busy.SetError($"Error loading buildings: {ex.Message}");
    }
}
```

### Null-Safe Filtering

Search filters handle null values defensively:

```csharp
filtered = filtered.Where(b =>
    (!string.IsNullOrEmpty(b.Name) && b.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
    (!string.IsNullOrEmpty(b.BuildingCode) && b.BuildingCode.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
    (!string.IsNullOrEmpty(b.City) && b.City.Contains(search, StringComparison.OrdinalIgnoreCase)));
```

### Ownership Validation

Ownership shares are validated before submission:

```csharp
private bool ValidateOwnership()
{
    if (!_ownershipShares.Any())
    {
        return true; // Ownership is optional
    }

    var total = _ownershipShares.Sum(s => s.SharePercent);
    if (Math.Abs(total - 100) > 0.01M)
    {
        Snackbar.Add($"Ownership shares must total 100%. Current total: {total:F2}%", Severity.Error);
        return false;
    }

    var duplicates = _ownershipShares.GroupBy(s => s.OwnerId).Where(g => g.Count() > 1).ToList();
    if (duplicates.Any())
    {
        Snackbar.Add("Each owner can only appear once.", Severity.Error);
        return false;
    }

    return true;
}
```

---

## 📚 Related Documentation

- [Property Management Guide](../PROPERTY_MANAGEMENT.md)
- [Tenant & Lease Management Guide](../TENANT_LEASE_MANAGEMENT.md)
- [Busy & Error Workflow](loading-boundaries.md)
- [Contributing Guide](../../CONTRIBUTING.md)
