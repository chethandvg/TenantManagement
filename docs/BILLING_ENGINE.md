# Billing Engine - TentMan

## Overview

The Billing Engine is a comprehensive invoicing and billing system for managing recurring charges, utility billing, and invoice generation for leases in TentMan. It supports both automated batch billing and manual invoice creation with flexible charge types and payment tracking.

## Features

### 🎯 Core Capabilities

- **Flexible Charge Types**: System-defined and custom charge types per organization
- **Recurring Charges**: Monthly, quarterly, yearly, and one-time charges
- **Utility Billing**: Support for both meter-based and amount-based utility billing
- **Rate Plans**: Configurable utility rate plans with tiered slabs
- **Invoice Generation**: Automated batch billing and manual invoice creation
- **Credit Notes**: Issue credit notes for refunds, adjustments, and corrections
- **Payment Tracking**: Track paid, partial, and overdue invoices
- **Batch Processing**: Invoice runs for generating invoices across multiple leases

## Database Schema

### Core Tables

#### ChargeTypes
System-defined and organization-specific charge types.

**System Charge Types** (Seeded):
- `RENT` (1) - Monthly rent charge
- `MAINT` (2) - Maintenance charge
- `ELEC` (3) - Electricity utility charge
- `WATER` (4) - Water utility charge
- `GAS` (5) - Gas utility charge
- `LATE_FEE` (6) - Late payment fee
- `ADJUSTMENT` (7) - Manual adjustment

**Columns**:
- `Id` (PK)
- `OrgId` (FK, nullable) - Null for system-wide, specific for org-custom
- `Code` (enum) - ChargeTypeCode
- `Name` (string, 100)
- `Description` (string, 500)
- `IsActive` (bool)
- `IsSystemDefined` (bool)
- `IsTaxable` (bool)
- `DefaultAmount` (decimal, nullable)

#### LeaseBillingSettings
Billing configuration per lease.

**Columns**:
- `Id` (PK)
- `LeaseId` (FK, unique) - One setting per lease
- `BillingDay` (byte, 1-28) - Day of month to generate invoice
- `PaymentTermDays` (short) - Payment due days after invoice
- `GenerateInvoiceAutomatically` (bool)
- `InvoicePrefix` (string, 20) - Custom invoice number prefix
- `PaymentInstructions` (string, 1000)
- `Notes` (string, 500)

#### LeaseRecurringCharges
Recurring charges associated with a lease.

**Columns**:
- `Id` (PK)
- `LeaseId` (FK)
- `ChargeTypeId` (FK)
- `Description` (string, 200)
- `Amount` (decimal 18,2)
- `Frequency` (enum) - OneTime, Monthly, Quarterly, Yearly
- `StartDate` (date)
- `EndDate` (date, nullable) - Null = no end date
- `IsActive` (bool)
- `Notes` (string, 500)

### Utility Billing Tables

#### UtilityRatePlans
Utility rate plans for calculating charges.

**Columns**:
- `Id` (PK)
- `OrgId` (FK)
- `UtilityType` (enum) - Electricity, Water, Gas
- `Name` (string, 100)
- `Description` (string, 500)
- `EffectiveFrom` (date)
- `EffectiveTo` (date, nullable)
- `IsActive` (bool)

#### UtilityRateSlabs
Tiered pricing slabs for utility rate plans.

**Columns**:
- `Id` (PK)
- `UtilityRatePlanId` (FK)
- `SlabOrder` (int) - 1, 2, 3, etc.
- `FromUnits` (decimal 18,2)
- `ToUnits` (decimal 18,2, nullable) - Null = unlimited
- `RatePerUnit` (decimal 18,4)
- `FixedCharge` (decimal 18,2, nullable)

**Example**: Electricity tariff
```
Slab 1: 0-100 units @ $0.10/unit
Slab 2: 101-200 units @ $0.15/unit
Slab 3: 201+ units @ $0.20/unit
```

#### UtilityStatements
Utility bills for leases (meter-based or amount-based).

**Columns**:
- `Id` (PK)
- `LeaseId` (FK)
- `UtilityType` (enum)
- `BillingPeriodStart` (date)
- `BillingPeriodEnd` (date)
- `IsMeterBased` (bool)

**Meter-based fields**:
- `UtilityRatePlanId` (FK, nullable)
- `PreviousReading` (decimal, nullable)
- `CurrentReading` (decimal, nullable)
- `UnitsConsumed` (decimal, nullable)
- `CalculatedAmount` (decimal, nullable)

**Amount-based fields**:
- `DirectBillAmount` (decimal, nullable)

**Common**:
- `TotalAmount` (decimal 18,2)
- `Notes` (string, 1000)
- `InvoiceLineId` (FK, nullable) - Link when billed

### Invoice Tables

#### Invoices
Main invoice records for leases.

**Columns**:
- `Id` (PK)
- `OrgId` (FK)
- `LeaseId` (FK)
- `InvoiceNumber` (string, 50, unique)
- `InvoiceDate` (date)
- `DueDate` (date)
- `Status` (enum) - Draft, Issued, PartiallyPaid, Paid, Overdue, Cancelled, WrittenOff
- `BillingPeriodStart` (date)
- `BillingPeriodEnd` (date)
- `SubTotal` (decimal 18,2)
- `TaxAmount` (decimal 18,2)
- `TotalAmount` (decimal 18,2)
- `PaidAmount` (decimal 18,2)
- `BalanceAmount` (decimal 18,2)
- `IssuedAtUtc` (datetime, nullable)
- `PaidAtUtc` (datetime, nullable)
- `Notes` (string, 1000)
- `PaymentInstructions` (string, 1000)

#### InvoiceLines
Line items on invoices.

**Columns**:
- `Id` (PK)
- `InvoiceId` (FK)
- `ChargeTypeId` (FK)
- `LineNumber` (int) - Sequential within invoice
- `Description` (string, 200)
- `Quantity` (decimal 18,2)
- `UnitPrice` (decimal 18,2)
- `Amount` (decimal 18,2) - Quantity × UnitPrice
- `TaxRate` (decimal 5,2) - Percentage
- `TaxAmount` (decimal 18,2)
- `TotalAmount` (decimal 18,2) - Amount + TaxAmount
- `Notes` (string, 500)

### Credit Note Tables

#### CreditNotes
Credit notes issued against invoices.

**Columns**:
- `Id` (PK)
- `OrgId` (FK)
- `InvoiceId` (FK)
- `CreditNoteNumber` (string, 50, unique)
- `CreditNoteDate` (date)
- `Reason` (enum) - InvoiceError, Discount, Refund, Goodwill, Adjustment, Other
- `TotalAmount` (decimal 18,2)
- `Notes` (string, 1000)
- `AppliedAtUtc` (datetime, nullable)

#### CreditNoteLines
Line items on credit notes.

**Columns**:
- `Id` (PK)
- `CreditNoteId` (FK)
- `InvoiceLineId` (FK) - References original invoice line
- `LineNumber` (int)
- `Description` (string, 200)
- `Quantity` (decimal 18,2)
- `UnitPrice` (decimal 18,2)
- `Amount` (decimal 18,2)
- `TaxAmount` (decimal 18,2)
- `TotalAmount` (decimal 18,2)
- `Notes` (string, 500)

### Batch Billing Tables

#### InvoiceRuns
Batch billing runs for generating multiple invoices.

**Columns**:
- `Id` (PK)
- `OrgId` (FK)
- `RunNumber` (string, 50, unique)
- `BillingPeriodStart` (date)
- `BillingPeriodEnd` (date)
- `Status` (enum) - Pending, InProgress, Completed, CompletedWithErrors, Failed, Cancelled
- `StartedAtUtc` (datetime, nullable)
- `CompletedAtUtc` (datetime, nullable)
- `TotalLeases` (int)
- `SuccessCount` (int)
- `FailureCount` (int)
- `ErrorMessage` (string, 2000)
- `Notes` (string, 1000)

#### InvoiceRunItems
Individual items (leases) processed in an invoice run.

**Columns**:
- `Id` (PK)
- `InvoiceRunId` (FK)
- `LeaseId` (FK)
- `InvoiceId` (FK, nullable) - Null if failed
- `IsSuccess` (bool)
- `ErrorMessage` (string, 1000)
- `ProcessedAtUtc` (datetime, nullable)

## Enums

### ChargeTypeCode
```csharp
public enum ChargeTypeCode
{
    RENT = 1,
    MAINT = 2,
    ELEC = 3,
    WATER = 4,
    GAS = 5,
    LATE_FEE = 6,
    ADJUSTMENT = 7
}
```

### BillingFrequency
```csharp
public enum BillingFrequency
{
    OneTime = 1,
    Monthly = 2,
    Quarterly = 3,
    Yearly = 4
}
```

### InvoiceStatus
```csharp
public enum InvoiceStatus
{
    Draft = 1,
    Issued = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Overdue = 5,
    Cancelled = 6,
    WrittenOff = 7
}
```

### CreditNoteReason
```csharp
public enum CreditNoteReason
{
    InvoiceError = 1,
    Discount = 2,
    Refund = 3,
    Goodwill = 4,
    Adjustment = 5,
    Other = 99
}
```

### InvoiceRunStatus
```csharp
public enum InvoiceRunStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    CompletedWithErrors = 4,
    Failed = 5,
    Cancelled = 6
}
```

## Database Indexes

### Performance Indexes

**ChargeTypes**:
- `IX_ChargeTypes_Code`
- `IX_ChargeTypes_OrgId_Code`
- `IX_ChargeTypes_IsActive`
- `IX_ChargeTypes_IsSystemDefined`

**Invoices**:
- `IX_Invoices_InvoiceNumber` (unique)
- `IX_Invoices_OrgId`
- `IX_Invoices_LeaseId`
- `IX_Invoices_Status`
- `IX_Invoices_InvoiceDate`
- `IX_Invoices_DueDate`
- `IX_Invoices_OrgId_InvoiceDate`

**InvoiceLines**:
- `IX_InvoiceLines_InvoiceId`
- `IX_InvoiceLines_ChargeTypeId`
- `IX_InvoiceLines_InvoiceId_LineNumber` (unique)

**LeaseRecurringCharges**:
- `IX_LeaseRecurringCharges_LeaseId`
- `IX_LeaseRecurringCharges_ChargeTypeId`
- `IX_LeaseRecurringCharges_IsActive`
- `IX_LeaseRecurringCharges_LeaseId_ChargeTypeId`

**UtilityStatements**:
- `IX_UtilityStatements_LeaseId`
- `IX_UtilityStatements_UtilityType`
- `IX_UtilityStatements_LeaseId_BillingPeriodStart_BillingPeriodEnd`

## Entity Relationships

```
Organization
  └─ ChargeTypes (1:many)
  └─ UtilityRatePlans (1:many)
  └─ Invoices (1:many)
  └─ CreditNotes (1:many)
  └─ InvoiceRuns (1:many)

Lease
  └─ LeaseBillingSetting (1:1)
  └─ LeaseRecurringCharges (1:many)
  └─ UtilityStatements (1:many)
  └─ Invoices (1:many)
  └─ InvoiceRunItems (1:many)

ChargeType
  └─ LeaseRecurringCharges (1:many)
  └─ InvoiceLines (1:many)

UtilityRatePlan
  └─ UtilityRateSlabs (1:many)
  └─ UtilityStatements (1:many)

Invoice
  └─ InvoiceLines (1:many)
  └─ CreditNotes (1:many)

InvoiceLine
  └─ UtilityStatements (1:many)
  └─ CreditNoteLines (1:many)

InvoiceRun
  └─ InvoiceRunItems (1:many)
```

## Data Integrity

### Concurrency Control
All entities use optimistic concurrency with SQL Server `rowversion`:
```csharp
[Timestamp]
public byte[] RowVersion { get; set; }
```

### Soft Delete
All entities implement soft delete via `ISoftDeletable`:
- `IsDeleted` (bool)
- `DeletedAtUtc` (datetime)
- `DeletedBy` (string)

Global query filter automatically excludes soft-deleted records.

### Audit Tracking
All entities implement audit tracking via `IAuditable`:
- `CreatedAtUtc` (datetime)
- `CreatedBy` (string)
- `ModifiedAtUtc` (datetime)
- `ModifiedBy` (string)

## Migrations

### Apply Migrations
```bash
cd src/TentMan.Infrastructure
dotnet ef database update
```

### Create New Migration
```bash
cd src/TentMan.Infrastructure
dotnet ef migrations add YourMigrationName
```

### Migration History
1. **20260110092448_AddBillingEngineTables** - Initial billing engine schema
   - ChargeTypes, LeaseBillingSettings, LeaseRecurringCharges
   - UtilityRatePlans, UtilityRateSlabs, UtilityStatements
   - Invoices, InvoiceLines
   - CreditNotes, CreditNoteLines
   - InvoiceRuns, InvoiceRunItems

2. **20260110092521_SeedChargeTypeSystemRecords** - Seed system charge types
   - RENT, MAINT, ELEC, WATER, GAS, LATE_FEE, ADJUSTMENT

## Usage Examples

### Example 1: Monthly Rent Billing

**Setup Lease Billing**:
1. Create lease
2. Add `LeaseBillingSetting` with `BillingDay = 1`
3. Add `LeaseRecurringCharge` with:
   - `ChargeTypeId` = RENT
   - `Amount` = 1500
   - `Frequency` = Monthly
   - `StartDate` = lease start date

**Monthly Invoice Run**:
1. Create `InvoiceRun` for billing period
2. For each active lease:
   - Create `Invoice`
   - Add `InvoiceLine` for rent charge
   - Calculate tax if applicable
3. Mark run as completed

### Example 2: Utility Billing with Meter Reading

**Setup**:
1. Create `UtilityRatePlan` for Electricity
2. Add `UtilityRateSlab` entries:
   - Slab 1: 0-100 @ $0.10/unit
   - Slab 2: 101-200 @ $0.15/unit
   - Slab 3: 201+ @ $0.20/unit

**Bill Utility**:
1. Capture meter readings (previous: 1000, current: 1250)
2. Create `UtilityStatement`:
   - `IsMeterBased` = true
   - `PreviousReading` = 1000
   - `CurrentReading` = 1250
   - `UnitsConsumed` = 250
3. Calculate amount using rate plan:
   - First 100 units: 100 × $0.10 = $10
   - Next 100 units: 100 × $0.15 = $15
   - Remaining 50 units: 50 × $0.20 = $10
   - `CalculatedAmount` = $35
   - `TotalAmount` = $35
4. Link to invoice line when billing

### Example 3: Credit Note for Overcharge

1. Identify invoice to credit
2. Create `CreditNote`:
   - `InvoiceId` = original invoice
   - `Reason` = InvoiceError
   - `CreditNoteDate` = today
3. Add `CreditNoteLine`:
   - `InvoiceLineId` = line to credit
   - `Amount` = amount to credit
4. Update invoice `PaidAmount` and `BalanceAmount`

## Best Practices

### Invoice Numbering
Use sequential numbering with organization prefix:
```
Format: {OrgPrefix}-INV-{Year}{Month}-{Sequence}
Example: ABC-INV-202601-0001
```

### Billing Period
- Standard monthly billing: 1st to last day of month
- Pro-rated billing for partial months
- Use `BillingPeriodStart` and `BillingPeriodEnd` for clarity

### Payment Tracking
- Update `PaidAmount` when payment received
- Calculate `BalanceAmount` = `TotalAmount` - `PaidAmount`
- Change status: Draft → Issued → PartiallyPaid → Paid
- Handle overdue: Check `DueDate` vs current date

### Utility Billing
- **Meter-based**: Capture readings, calculate using rate plan
- **Amount-based**: Direct bill from provider invoice
- Link to invoice line for complete audit trail

### Error Handling
- Use `InvoiceRunItem.ErrorMessage` for detailed failures
- Mark run status as `CompletedWithErrors` if some fail
- Retry failed items individually

## Implemented Services

### Invoice Generation Service
The `InvoiceGenerationService` handles creating individual invoices for leases with the following features:

**Key Features**:
- Generates invoices for a specific lease and billing period
- Implements idempotency by updating existing draft invoices
- Automatically generates rent line items using `RentCalculationService`
- Automatically generates recurring charge line items using `RecurringChargeCalculationService`
- Calculates due dates based on `LeaseBillingSetting.PaymentTermDays`
- Auto-generates unique invoice numbers with format: `{Prefix}-{YYYYMM}-{NNNNNN}`
- Calculates subtotals, tax, and total amounts
- Sets initial status to `Draft`

**Usage Example**:
```csharp
var result = await invoiceGenerationService.GenerateInvoiceAsync(
    leaseId: leaseGuid,
    billingPeriodStart: new DateOnly(2024, 1, 1),
    billingPeriodEnd: new DateOnly(2024, 1, 31),
    prorationMethod: ProrationMethod.ActualDaysInMonth
);

if (result.IsSuccess)
{
    Console.WriteLine($"Invoice {result.Invoice.InvoiceNumber} created");
    Console.WriteLine($"Amount: ${result.Invoice.TotalAmount}");
}
```

**Idempotency**:
The service checks for existing draft invoices with the same lease and billing period. If found, it updates the existing invoice rather than creating a duplicate.

### Invoice Run Service
The `InvoiceRunService` orchestrates batch invoice generation across multiple leases:

**Key Features**:
- Executes monthly rent invoice runs for all active leases in an organization
- Creates `InvoiceRun` record to track batch execution
- Generates `InvoiceRunItem` for each lease processed
- Handles partial failures gracefully (continues processing remaining leases)
- Tracks success/failure counts and error messages
- Sets run status based on results:
  - `Completed`: All invoices generated successfully
  - `CompletedWithErrors`: Some invoices failed
  - `Failed`: All invoices failed

**Usage Example**:
```csharp
var result = await invoiceRunService.ExecuteMonthlyRentRunAsync(
    orgId: organizationGuid,
    billingPeriodStart: new DateOnly(2024, 1, 1),
    billingPeriodEnd: new DateOnly(2024, 1, 31),
    prorationMethod: ProrationMethod.ActualDaysInMonth
);

Console.WriteLine($"Processed {result.TotalLeases} leases");
Console.WriteLine($"Success: {result.SuccessCount}, Failed: {result.FailureCount}");
```

### Supporting Repositories
The following repositories have been implemented to support invoice services:

- **InvoiceRepository**: CRUD operations for invoices, including draft lookup
- **InvoiceRunRepository**: CRUD operations for invoice runs
- **ChargeTypeRepository**: Manages charge types (RENT, MAINT, ELEC, etc.)
- **LeaseBillingSettingRepository**: Retrieves billing settings per lease
- **LeaseRecurringChargeRepository**: Retrieves active recurring charges
- **UtilityRatePlanRepository**: Retrieves utility rate plans and slabs
- **NumberSequenceRepository**: Generates unique sequence numbers (simplified implementation)

### Testing
Unit tests have been implemented for both services:

**InvoiceGenerationService Tests** (4 tests):
- Generate new invoice for active lease
- Update existing draft invoice (idempotency)
- Reject inactive lease
- Handle lease not found

**InvoiceRunService Tests** (4 tests):
- Generate invoices for multiple active leases
- Handle no active leases gracefully
- Handle partial failures
- Handle all failures

Run tests:
```bash
dotnet test --filter "Feature=Billing"
```

## Frontend UI Components

The billing engine includes a comprehensive Blazor-based frontend interface for managing all billing operations. All UI components are located in `TentMan.Ui/Pages/Billing/` and require Owner, Administrator, or Manager roles.

### UI Architecture

**Technology Stack**:
- **Framework**: Blazor WebAssembly
- **UI Library**: MudBlazor (Material Design components)
- **Pattern**: Code-behind pattern with `.razor` and `.razor.cs` files
- **Authorization**: Policy-based using `PolicyNames.RequireManagerRole`

**Navigation**: All billing screens are accessible via `/billing/*` routes

### Component Overview

#### BillingDashboard (`/billing/dashboard`)
**Purpose**: Central hub providing an overview of billing operations

**Key Features**:
- **Statistics Cards**: 
  - Invoices due this month (count + total amount)
  - Overdue invoices (count + overdue amount)
  - Draft invoices count
  - Total invoices count
- **Quick Actions**: Run invoice generation, view all invoices
- **Filtering**: Filter invoices by status, date range
- **Real-time Updates**: Auto-refresh statistics

**API Endpoints Used**: `GET /api/invoices/stats`, `GET /api/invoices`

---

#### LeaseBillingSettings (`/billing/lease-settings/{leaseId}`)
**Purpose**: Configure billing behavior and settings for individual leases

**Key Settings**:
- **Billing Day**: Day of month to generate invoices (1-28)
- **Payment Terms**: Number of days after invoice date for payment due
- **Auto-generation**: Enable/disable automatic monthly invoice generation
- **Invoice Prefix**: Custom prefix for invoice numbers
- **Payment Instructions**: Custom instructions shown on invoices
- **Proration Rules**: Configure partial month billing

**Features**:
- Settings preview before saving
- Validation of business rules
- Concurrency control for updates

**API Endpoints Used**: 
- `GET /api/leases/{leaseId}/billing-settings`
- `PUT /api/leases/{leaseId}/billing-settings`

---

#### RecurringCharges (`/billing/recurring-charges/{leaseId}`)
**Purpose**: Manage all recurring charges associated with a lease

**Supported Charge Types**:
- RENT (monthly rent)
- MAINT (maintenance charges)
- ELEC, WATER, GAS (utility charges)
- Custom charges

**Operations**:
- **Create**: Add new recurring charge with frequency (Monthly, Quarterly, Yearly, OneTime)
- **Update**: Modify amount, frequency, dates
- **Delete**: Remove recurring charge
- **Activate/Deactivate**: Enable/disable charges without deletion

**Features**:
- Charge type selection from system-defined and custom types
- Start/End date configuration
- Active status tracking
- Amount validation

**API Endpoints Used**: 
- `GET /api/leases/{leaseId}/recurring-charges`
- `POST /api/leases/{leaseId}/recurring-charges`
- `PUT /api/recurring-charges/{chargeId}`
- `DELETE /api/recurring-charges/{chargeId}`

---

#### UtilityStatements (`/billing/utility-statements/{leaseId}`)
**Purpose**: Record and manage utility consumption and billing

**Input Modes**:
1. **Meter-Based**: 
   - Enter previous and current meter readings
   - System calculates consumption using utility rate plans
   - Supports tiered pricing (slabs)
   - Utility types: Electricity, Water, Gas

2. **Amount-Based**: 
   - Direct entry of utility bill amount
   - Used when provider invoice is available
   - No rate plan calculation

**Features**:
- Billing period selection (start/end dates)
- File upload support for utility bills (future)
- Calculated vs. actual amount display
- Link to invoice line items

**API Endpoints Used**: 
- `GET /api/leases/{leaseId}/utility-statements`
- `POST /api/leases/{leaseId}/utility-statements`
- `GET /api/utility-rate-plans` (for meter-based calculations)

---

#### InvoiceList (`/billing/invoices`)
**Purpose**: Browse, search, and filter all invoices in the organization

**Features**:
- **Status Filtering**: Draft, Issued, PartiallyPaid, Paid, Overdue, Cancelled, WrittenOff
- **Date Range**: Filter by invoice date or due date
- **Search**: Search by invoice number or lease
- **Sorting**: Sort by date, amount, status
- **Pagination**: Handle large datasets efficiently
- **Status Badges**: Color-coded status indicators
- **Quick Actions**: Navigate to invoice details, void invoice

**Display Columns**:
- Invoice number, Date, Due date
- Lease/Unit information
- Total amount, Paid amount, Balance
- Status (with color-coded chip)
- Actions (view, void, create credit note)

**API Endpoints Used**: `GET /api/invoices?status={status}&from={date}&to={date}`

---

#### InvoiceDetail (`/billing/invoices/{invoiceId}`)
**Purpose**: View complete invoice details and perform invoice actions

**Sections**:
1. **Header**: Invoice number, date, status, amounts
2. **Lease Information**: Tenant details, unit, lease dates
3. **Line Items**: Detailed breakdown of charges
   - Charge type, Description
   - Quantity, Unit price
   - Amount, Tax, Total
4. **Totals**: Subtotal, Tax amount, Total, Paid, Balance
5. **Actions**: Issue, Void, Create Credit Note

**Available Actions**:
- **Issue Invoice**: Change from Draft to Issued status
- **Void Invoice**: Cancel issued invoice
- **Create Credit Note**: Issue partial or full credit
- **Record Payment**: Mark invoice as paid (future)
- **Download PDF**: Generate invoice PDF (future)

**API Endpoints Used**: 
- `GET /api/invoices/{invoiceId}`
- `POST /api/invoices/{invoiceId}/issue`
- `POST /api/invoices/{invoiceId}/void`
- `POST /api/invoices/{invoiceId}/credit-notes`

---

#### InvoiceRuns (`/billing/invoice-runs`)
**Purpose**: Execute batch invoice generation across multiple leases

**Features**:
- **Run History**: View past invoice runs with status
- **Execution Stats**: Total leases, success count, failure count
- **Status Tracking**: Pending, InProgress, Completed, CompletedWithErrors, Failed, Cancelled
- **Initiate New Run**: Start batch billing for a billing period
- **View Details**: Navigate to run detail for per-lease results

**Run Information**:
- Run number, Billing period (start/end)
- Execution timestamps (started, completed)
- Result summary (success/failure counts)
- Error messages (if any)

**API Endpoints Used**: 
- `GET /api/invoice-runs`
- `POST /api/invoice-runs` (initiate new run)
- `GET /api/invoice-runs/{runId}`

---

#### InvoiceRunDetail (`/billing/invoice-runs/{runId}`)
**Purpose**: View detailed results of a specific invoice run execution

**Features**:
- **Run Summary**: Status, billing period, execution time, totals
- **Per-Lease Results**: 
  - Lease number and tenant name
  - Success/Failure status
  - Generated invoice number (if successful)
  - Error message (if failed)
- **Retry Failed**: Re-run failed invoice generations (future)

**API Endpoints Used**: `GET /api/invoice-runs/{runId}/items`

---

### Authorization & Security

**Required Roles**: All billing screens require one of:
- `Manager` role
- `Administrator` role  
- `SuperAdmin` role

**Policy**: Components use `[Authorize(Policy = PolicyNames.RequireManagerRole)]`

**Navigation Guard**: Billing menu items only appear for users with appropriate roles

**API Authorization**: All billing API endpoints enforce the same role requirements

### Navigation Structure

```
/billing
├── /dashboard              (Billing Dashboard)
├── /lease-settings/{id}   (Lease Billing Settings)
├── /recurring-charges/{id} (Recurring Charges)
├── /utility-statements/{id} (Utility Statements)
├── /invoices              (Invoice List)
├── /invoices/{id}         (Invoice Detail)
├── /invoice-runs          (Invoice Runs)
└── /invoice-runs/{id}     (Invoice Run Detail)
```

### Integration with Backend

**API Base Path**: `/api/v1/`

**Controller Mapping**:
- `BillingSettingsController` → Lease billing settings
- `RecurringChargesController` → Recurring charge management
- `UtilityStatementsController` → Utility billing
- `InvoicesController` → Invoice operations
- `InvoiceRunsController` → Batch billing runs
- `CreditNotesController` → Credit note operations
- `ChargeTypesController` → Charge type lookup

**Error Handling**: All components use MudBlazor's `BusyBoundary` for graceful error handling and retry logic

**State Management**: Components use local state with reload on navigation

---

## Background Jobs & Automation

TentMan uses **Hangfire** for automated background processing of billing tasks. This ensures reliable execution of recurring billing operations without manual intervention.

### Automated Jobs

#### Monthly Rent Generation
- **Schedule**: Runs on the 26th of each month at 2:00 AM UTC
- **Purpose**: Automatically generates rent invoices for all active leases for the upcoming month
- **Multi-Tenant**: Processes all organizations automatically
- **Lead Time**: 5 days before the billing period starts (allows time for review)
- **Process**: 
  - Fetches all organizations from the database
  - Calculates next month's billing period
  - Generates invoices for all active leases per organization
  - Uses `ActualDaysInMonth` proration method
  - Logs successes and failures per organization
  - Supports partial failures (continues if some organizations/leases fail)

#### Utility Billing
- **Schedule**: Runs every Monday at 3:00 AM UTC
- **Purpose**: Generates utility invoices for leases with finalized utility statements
- **Multi-Tenant**: Processes all organizations automatically
- **Process**:
  - Fetches all organizations from the database
  - Fetches finalized utility statements not yet billed per organization
  - Generates utility invoice lines
  - Logs execution results per organization
  - **Note**: Full implementation pending UtilityStatementRepository

### Monitoring & Management

- **Dashboard**: Access at `/hangfire` (requires Admin role in production)
- **Features**:
  - View job execution history
  - Monitor success/failure rates
  - Manually trigger jobs
  - View detailed logs and errors
  - Retry failed jobs
- **Idempotency**: Jobs are designed to be safely retried without creating duplicate invoices
- **Error Handling**: Automatic retries with exponential backoff for transient failures

### Configuration

Jobs are configured in `Program.cs` using cron expressions:
```csharp
// Monthly rent: 26th at 2:00 AM UTC
RecurringJob.AddOrUpdate<MonthlyRentGenerationJob>(
    "monthly-rent-generation",
    job => job.ExecuteForNextMonthAsync(...),
    "0 2 26 * *");

// Utility billing: Every Monday at 3:00 AM UTC
RecurringJob.AddOrUpdate<UtilityBillingJob>(
    "utility-billing",
    job => job.ExecuteForCurrentPeriodAsync(...),
    "0 3 * * 1");
```

For detailed information, see [Background Jobs Documentation](BACKGROUND_JOBS.md).

---

## Edge Case Handling

The billing engine includes comprehensive edge case handling for production-ready billing operations.

### Implemented Edge Cases

#### Lease Lifecycle
- ✅ **Lease starts mid-month**: Automatic proration based on actual days used
- ✅ **Lease ends mid-month**: Final prorated invoice generation
- ✅ **Rent changes mid-month**: Split invoice lines for each term period
- ✅ **Multiple lease terms**: Proper handling of overlapping rental periods

#### Billing Day Validation
- ✅ **Invalid billing days**: Restricted to 1-28 to ensure February compatibility
- ✅ **February handling**: Correctly handles 28-day and 29-day (leap year) months
- ✅ **Varying month lengths**: Proration accounts for different month lengths (28-31 days)

#### Invoice Immutability
- ✅ **Draft invoices**: Can be regenerated and updated
- ✅ **Issued invoices**: Immutable, cannot be regenerated
- ✅ **Paid/Partially Paid invoices**: Immutable, require credit notes for adjustments
- ✅ **Voided invoices**: Terminal state with reason tracking (VoidedAtUtc, VoidReason)
- ✅ **State transitions**: All transitions tracked with UTC timestamps

#### Utility Statement Versioning
- ✅ **Version tracking**: Each statement has Version field (1, 2, 3, etc.)
- ✅ **Multiple corrections**: Allows creating draft versions before finalization
- ✅ **Final statements**: Database-enforced uniqueness (one final per lease/utility/period)
- ✅ **Late billing**: Can add statements for past periods
- ✅ **Multiple utility types**: Supports multiple utilities for same billing period

#### Idempotency & Concurrency
- ✅ **Duplicate prevention**: Regenerating invoice updates existing draft (not creates new)
- ✅ **Concurrent safety**: Optimistic concurrency control with RowVersion
- ✅ **Unique constraints**: Database-level unique indexes prevent data corruption

#### Source Traceability
- ✅ **Invoice line tracking**: Each line has Source and SourceRefId fields
- ✅ **Rent lines**: Source = "Rent", SourceRefId = LeaseTermId
- ✅ **Recurring charge lines**: Source = "RecurringCharge", SourceRefId = ChargeId
- ✅ **Utility lines**: Links to UtilityStatementId when applicable
- ✅ **Audit trail**: Full traceability from charge to invoice line

### Database Schema Updates

#### InvoiceLines
New fields for traceability:
- `Source` (string, 50) - Source type (Rent, RecurringCharge, Utility)
- `SourceRefId` (guid, nullable) - Reference to source entity

#### LeaseBillingSettings
New field for proration method:
- `ProrationMethod` (int) - ActualDaysInMonth (1) or ThirtyDayMonth (2)

#### UtilityStatements
New fields for versioning:
- `Version` (int) - Version number (1, 2, 3, etc.)
- `IsFinal` (bool) - Whether this is the final statement
- Unique index on (LeaseId, UtilityType, BillingPeriodStart, BillingPeriodEnd, IsFinal) where IsFinal = 1

### Testing Coverage

Comprehensive test suites validate all edge cases:
- **BillingEdgeCasesTests**: 11 tests covering lease lifecycle, proration, and edge cases
- **UtilityStatementVersioningTests**: 6 tests for versioning and corrections
- **InvoiceImmutabilityTests**: 14 tests for state transitions and immutability

Run edge case tests:
```bash
dotnet test --filter "TestType=EdgeCases|TestType=Versioning|TestType=Immutability"
```

---

## Future Enhancements

- [ ] Payment transaction tracking
- [ ] Invoice templates and PDF generation
- [ ] Email invoice delivery
- [ ] Payment gateway integration
- [ ] Dunning management for overdue invoices
- [ ] Recurring invoice automation
- [ ] Multi-currency support
- [ ] Tax calculation rules engine
- [ ] Invoice approval workflow
- [ ] Payment plans and installments

## Related Documentation

- [Background Jobs](BACKGROUND_JOBS.md) - Automated billing job processing with Hangfire
- [Billing UI Guide](BILLING_UI_GUIDE.md) - Complete frontend component documentation
- [Tenant & Lease Management](TENANT_LEASE_MANAGEMENT.md)
- [Database Guide](DATABASE_GUIDE.md)
- [Architecture Guide](ARCHITECTURE.md)
- [API Guide](API_GUIDE.md)

## Support

For questions or issues related to the billing engine:
- Check the [Database Guide](DATABASE_GUIDE.md) for migration help
- See [Getting Started](GETTING_STARTED.md) for setup instructions
- Report issues on [GitHub Issues](https://github.com/chethandvg/TenantManagement/issues)

---

**Last Updated**: 2026-01-11  
**Part of**: TentMan Billing Engine Feature (#45, #51)  
**Edge Cases Implemented**: 2026-01-11
