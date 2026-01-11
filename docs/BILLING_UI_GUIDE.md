# Billing UI Guide - TentMan

Complete guide to the Billing & Invoicing user interface for administrators, managers, and tenants.

---

## 📚 Table of Contents

- [Overview](#overview)
- [Access Requirements](#access-requirements)
- [Component Documentation](#component-documentation)
  - [Billing Dashboard](#billing-dashboard)
  - [Lease Billing Settings](#lease-billing-settings)
  - [Recurring Charges Management](#recurring-charges-management)
  - [Utility Statements](#utility-statements)
  - [Invoice Management](#invoice-management)
  - [Invoice Runs](#invoice-runs)
  - [Credit Notes](#credit-notes)
- [Tenant Portal - My Bills](#tenant-portal---my-bills)
  - [My Bills List](#my-bills-list)
  - [Bill Details](#bill-details)
- [Navigation](#navigation)
- [Authorization](#authorization)
- [Common Workflows](#common-workflows)

---

## Overview

### Purpose

The Billing UI provides a comprehensive web-based interface for administrators, owners, and managers to:

- **Monitor billing operations** via dashboard with key metrics
- **Configure lease billing** with custom settings per lease
- **Manage recurring charges** for rent, maintenance, and utilities
- **Record utility consumption** using meter readings or direct amounts
- **Generate and manage invoices** with full lifecycle support
- **Execute batch billing** across multiple leases
- **Issue credit notes** for refunds and adjustments

### Technology Stack

- **Framework**: Blazor WebAssembly (.NET 9)
- **UI Library**: MudBlazor (Material Design)
- **Pattern**: Code-behind with `.razor` and `.razor.cs` files
- **State Management**: Component-level state with API integration

### Access Requirements

**Required Roles**: One of the following:
- **Manager** - Can view and manage all billing operations
- **Administrator** - Full access to billing features
- **SuperAdmin** - Complete system access including billing

**Authorization Policy**: `PolicyNames.RequireManagerRole`

**Navigation**: Billing menu items only visible to users with appropriate roles

---

## Component Documentation

### Billing Dashboard

**Route**: `/billing/dashboard`  
**Component**: `BillingDashboard.razor`

#### Purpose

Central hub providing real-time overview of billing operations and quick access to common actions.

#### Key Features

**Statistics Cards**:
- **Due This Month**: Count and total amount of invoices due in current month
- **Overdue**: Count and total amount of overdue invoices
- **Draft Invoices**: Count of invoices in draft status
- **Total Invoices**: Overall invoice count

**Quick Actions**:
- **Run Invoice Generation**: Initiate batch billing for all active leases
- **View All Invoices**: Navigate to invoice list with filters
- **Generate Single Invoice**: Create invoice for specific lease

**Filtering**:
- Filter by invoice status (Draft, Issued, Paid, Overdue)
- Date range selection (invoice date or due date)
- Search by invoice number

**Visual Indicators**:
- Color-coded statistics (success, warning, error)
- Material Design icons for visual clarity
- Responsive card layout

#### Usage Example

```razor
@page "/billing/dashboard"
@attribute [Authorize(Policy = PolicyNames.RequireManagerRole)]

<!-- Dashboard automatically loads on navigation -->
<!-- Statistics refresh on component initialization -->
```

#### API Integration

- `GET /api/invoices/stats` - Fetch invoice statistics
- `GET /api/invoices` - List invoices for display

---

### Lease Billing Settings

**Route**: `/billing/lease-settings/{leaseId}`  
**Component**: `LeaseBillingSettings.razor`

#### Purpose

Configure billing behavior and automation settings for individual leases.

#### Configuration Options

**Billing Schedule**:
- **Billing Day** (1-28): Day of month to generate invoices
- **Payment Terms** (days): Number of days after invoice date for payment due
  - Example: Billing Day = 1, Payment Terms = 5 → Invoice on 1st, Due on 5th

**Automation**:
- **Auto-Generation**: Enable automatic monthly invoice creation
- **Invoice Prefix**: Custom prefix for invoice numbers (e.g., "INV", "BILL")

**Instructions**:
- **Payment Instructions**: Custom text displayed on all invoices
  - Example: "Please pay via bank transfer to Account #12345"
- **Notes**: Internal notes about billing configuration

**Proration Settings**:
- Configure partial month billing rules
- Used when lease starts/ends mid-month

#### Features

**Settings Preview**:
- Preview how settings affect invoice generation
- Sample invoice number generation
- Due date calculation preview

**Validation**:
- Billing day must be 1-28 (to handle all months)
- Payment terms must be positive
- Invoice prefix format validation

**Concurrency Control**:
- Optimistic concurrency using `rowversion`
- Prevents conflicting updates

#### Usage Workflow

1. Navigate to lease detail page
2. Click "Billing Settings" tab or button
3. Modify desired settings
4. Preview changes
5. Save settings
6. Confirmation message displayed

#### API Integration

- `GET /api/leases/{leaseId}/billing-settings` - Load current settings
- `PUT /api/leases/{leaseId}/billing-settings` - Save updated settings

#### Code Example

```csharp
// LeaseBillingSettings.razor.cs
private async Task SaveSettingsAsync()
{
    var request = new UpdateBillingSettingsRequest
    {
        BillingDay = _settings.BillingDay,
        PaymentTermDays = _settings.PaymentTermDays,
        GenerateInvoiceAutomatically = _settings.GenerateInvoiceAutomatically,
        InvoicePrefix = _settings.InvoicePrefix,
        PaymentInstructions = _settings.PaymentInstructions,
        RowVersion = _settings.RowVersion
    };
    
    var result = await _apiClient.UpdateBillingSettingsAsync(LeaseId.Value, request);
    // Handle result...
}
```

---

### Recurring Charges Management

**Route**: `/billing/recurring-charges/{leaseId}`  
**Component**: `RecurringCharges.razor`

#### Purpose

Manage all recurring charges associated with a lease, including rent, maintenance, and other periodic charges.

#### Supported Charge Types

**System-Defined**:
- **RENT** - Monthly rent charge
- **MAINT** - Maintenance/association charges
- **ELEC** - Electricity charges
- **WATER** - Water charges
- **GAS** - Gas charges
- **LATE_FEE** - Late payment penalties
- **ADJUSTMENT** - Manual adjustments

**Custom Types**: Organizations can define additional charge types

#### Operations

**Create Recurring Charge**:
- Select charge type from dropdown
- Enter description (e.g., "Monthly Rent - Unit A-101")
- Specify amount
- Choose frequency: OneTime, Monthly, Quarterly, Yearly
- Set start date (required)
- Set end date (optional - null means no end)
- Set active status

**Update Charge**:
- Modify amount, frequency, or dates
- Activate/deactivate without deletion
- Update description and notes

**Delete Charge**:
- Soft delete for audit trail
- Confirmation dialog before deletion
- Cannot delete if referenced in invoices

#### Features

**Data Grid Display**:
- Charge type, Description
- Amount (formatted as currency)
- Frequency (display text: "Monthly", "Quarterly", etc.)
- Start/End dates
- Active status (color-coded chip)
- Actions (Edit, Delete)

**Filtering & Sorting**:
- Filter by charge type
- Filter by active/inactive status
- Sort by amount, date, or type

**Validation**:
- Amount must be positive
- Start date cannot be in the past (for new charges)
- End date must be after start date

#### Usage Workflow

**Adding a Charge**:
1. Navigate to lease recurring charges
2. Click "Add Recurring Charge" button
3. Fill in dialog form
4. Preview charge details
5. Click "Save"
6. Charge appears in grid immediately

**Editing a Charge**:
1. Click edit icon in actions column
2. Modify fields in dialog
3. Save changes
4. Grid refreshes with updated data

#### API Integration

- `GET /api/leases/{leaseId}/recurring-charges` - Load all charges
- `POST /api/leases/{leaseId}/recurring-charges` - Create new charge
- `PUT /api/recurring-charges/{chargeId}` - Update existing charge
- `DELETE /api/recurring-charges/{chargeId}` - Delete charge

#### Code Example

```csharp
// RecurringCharges.razor.cs
private async Task CreateChargeAsync()
{
    var request = new CreateRecurringChargeRequest
    {
        LeaseId = LeaseId.Value,
        ChargeTypeId = _selectedChargeType.Id,
        Description = _newCharge.Description,
        Amount = _newCharge.Amount,
        Frequency = _newCharge.Frequency,
        StartDate = _newCharge.StartDate,
        EndDate = _newCharge.EndDate,
        IsActive = true
    };
    
    var result = await _apiClient.CreateRecurringChargeAsync(request);
    await LoadRecurringChargesAsync(); // Refresh grid
}
```

---

### Utility Statements

**Route**: `/billing/utility-statements/{leaseId}`  
**Component**: `UtilityStatements.razor`

#### Purpose

Record utility consumption and costs for electricity, water, and gas services.

#### Input Modes

**1. Meter-Based Billing**:

Used when you have meter readings and want system to calculate charges.

**Fields**:
- **Utility Type**: Electricity, Water, or Gas
- **Billing Period**: Start and end dates
- **Utility Rate Plan**: Select applicable rate plan
- **Previous Reading**: Last meter reading (numeric)
- **Current Reading**: Current meter reading (numeric)
- **Notes**: Optional notes (e.g., "January 2026 reading")

**Calculation**:
- System calculates: Units Consumed = Current - Previous
- Applies rate plan with tiered slabs
- Calculates total amount based on consumption

**Example Rate Plan**:
```
Electricity - Residential
  Slab 1: 0-100 units @ $0.10/unit
  Slab 2: 101-200 units @ $0.15/unit
  Slab 3: 201+ units @ $0.20/unit
```

**Example Calculation**:
```
Previous: 1000, Current: 1250
Consumed: 250 units

Calculation:
  100 units × $0.10 = $10.00
  100 units × $0.15 = $15.00
   50 units × $0.20 = $10.00
Total: $35.00
```

**2. Amount-Based Billing**:

Used when you have the provider's bill and want to record the amount directly.

**Fields**:
- **Utility Type**: Electricity, Water, or Gas
- **Billing Period**: Start and end dates
- **Direct Bill Amount**: Total amount from provider invoice
- **Notes**: Optional notes (e.g., "January electric bill from provider")

**No calculation needed** - amount is recorded as-is.

#### Features

**Statement List**:
- Display all utility statements for lease
- Filter by utility type
- Sort by billing period
- Show consumption and amounts

**File Upload** (Future):
- Attach scanned utility bills
- PDF/image support
- Store in Azure Blob Storage

**Link to Invoices**:
- Track which invoice includes each utility statement
- View invoice line reference

#### Usage Workflow

**Record Meter-Based Utility**:
1. Click "Record Utility Statement"
2. Select "Meter-Based"
3. Choose utility type
4. Select billing period
5. Choose rate plan
6. Enter previous and current readings
7. System calculates and displays amount
8. Add notes if needed
9. Save statement

**Record Amount-Based Utility**:
1. Click "Record Utility Statement"
2. Select "Amount-Based"
3. Choose utility type
4. Select billing period
5. Enter total bill amount
6. Add notes
7. Save statement

#### API Integration

- `GET /api/leases/{leaseId}/utility-statements` - Load statements
- `POST /api/leases/{leaseId}/utility-statements` - Create new statement
- `GET /api/utility-rate-plans` - Load available rate plans

#### Code Example

```csharp
// UtilityStatements.razor.cs (Meter-Based)
private async Task CreateMeterBasedStatementAsync()
{
    var request = new CreateUtilityStatementRequest
    {
        LeaseId = LeaseId.Value,
        UtilityType = _statement.UtilityType,
        BillingPeriodStart = _statement.PeriodStart,
        BillingPeriodEnd = _statement.PeriodEnd,
        IsMeterBased = true,
        UtilityRatePlanId = _selectedRatePlan.Id,
        PreviousReading = _statement.PreviousReading,
        CurrentReading = _statement.CurrentReading,
        Notes = _statement.Notes
    };
    
    // Backend calculates UnitsConsumed and CalculatedAmount
    var result = await _apiClient.CreateUtilityStatementAsync(request);
}
```

---

### Invoice Management

The invoice management system consists of two main components:

#### Invoice List

**Route**: `/billing/invoices`  
**Component**: `InvoiceList.razor`

##### Purpose

Browse, search, and filter all invoices in the organization.

##### Features

**Filtering**:
- **Status Filter**: Draft, Issued, PartiallyPaid, Paid, Overdue, Cancelled, WrittenOff
- **Date Range**: Filter by invoice date or due date
- **Search**: Search by invoice number or lease number
- **Organization Filter**: Filter by organization (for multi-org setups)

**Display Columns**:
- **Invoice Number**: Unique identifier (e.g., INV-202601-000001)
- **Date**: Invoice generation date
- **Due Date**: Payment due date
- **Lease**: Lease number and unit information
- **Tenant**: Tenant name
- **Total Amount**: Invoice total (formatted as currency)
- **Paid Amount**: Amount already paid
- **Balance**: Remaining amount due
- **Status**: Color-coded status chip

**Status Colors**:
- Draft: Gray
- Issued: Blue
- PartiallyPaid: Yellow
- Paid: Green
- Overdue: Red
- Cancelled: Dark gray
- WrittenOff: Purple

**Sorting**:
- Sort by any column
- Default: Most recent first

**Pagination**:
- Configurable page size (10, 25, 50, 100)
- Navigate through pages
- Shows total count

**Quick Actions**:
- **View Details**: Navigate to invoice detail page
- **Void**: Cancel issued invoice (with confirmation)
- **Create Credit Note**: Issue credit for invoice

##### Usage

```razor
<!-- Default view: All issued invoices -->
/billing/invoices

<!-- With filters -->
/billing/invoices?status=Overdue&from=2026-01-01&to=2026-01-31
```

##### API Integration

- `GET /api/invoices?status={status}&from={date}&to={date}&page={n}&pageSize={size}`

---

#### Invoice Detail

**Route**: `/billing/invoices/{invoiceId}`  
**Component**: `InvoiceDetail.razor`

##### Purpose

View complete invoice details, line items, and perform invoice lifecycle actions.

##### Sections

**1. Invoice Header**:
- Invoice number, date, due date
- Status with color-coded badge
- Organization and lease information

**2. Lease & Tenant Information**:
- Tenant name and contact
- Unit number and building
- Lease number and dates
- Billing period covered

**3. Line Items Table**:

| Charge Type | Description | Quantity | Unit Price | Amount | Tax | Total |
|-------------|-------------|----------|------------|--------|-----|-------|
| RENT | Monthly Rent | 1 | 15000.00 | 15000.00 | 0.00 | 15000.00 |
| MAINT | Maintenance | 1 | 2000.00 | 2000.00 | 0.00 | 2000.00 |
| ELEC | Electricity | 250 | 14.00 | 3500.00 | 0.00 | 3500.00 |

**4. Totals Section**:
- **Subtotal**: Sum of all line items (before tax)
- **Tax Amount**: Total tax applied
- **Total Amount**: Grand total
- **Paid Amount**: Amount received
- **Balance Amount**: Remaining due

**5. Payment Instructions**:
- Custom instructions from billing settings
- Example: "Please pay via bank transfer to Account #12345"

**6. Notes**:
- Internal notes about the invoice
- Visible only to admins

##### Available Actions

**Issue Invoice**:
- Changes status from Draft → Issued
- Finalizes invoice for sending to tenant
- Sets `IssuedAtUtc` timestamp
- Cannot be modified after issuing

**Void Invoice**:
- Cancels an issued invoice
- Changes status to Cancelled
- Requires confirmation
- Cannot void paid invoices
- Creates audit trail

**Create Credit Note**:
- Issue partial or full credit
- Select line items to credit
- Specify reason (InvoiceError, Discount, Refund, Goodwill, Adjustment)
- Reduces invoice balance
- Generates credit note number

**Record Payment** (Future):
- Mark invoice as paid
- Record payment date and method
- Update paid amount
- Change status to Paid or PartiallyPaid

**Download PDF** (Future):
- Generate PDF invoice
- Professional invoice template
- Include all line items and totals

##### Usage Workflow

**Issuing an Invoice**:
1. Navigate to invoice detail (Draft status)
2. Review all line items and totals
3. Click "Issue Invoice" button
4. Confirm action in dialog
5. Status changes to Issued
6. Invoice can now be sent to tenant

**Voiding an Invoice**:
1. Open invoice detail (Issued status)
2. Click "Void Invoice" button
3. Confirm action with reason
4. Status changes to Cancelled
5. Balance set to 0

**Creating Credit Note**:
1. Open invoice detail
2. Click "Create Credit Note" button
3. Select line items to credit
4. Enter credit amount
5. Choose reason
6. Add notes
7. Submit
8. Credit note created and applied

##### API Integration

- `GET /api/invoices/{invoiceId}` - Load invoice with line items
- `POST /api/invoices/{invoiceId}/issue` - Issue invoice
- `POST /api/invoices/{invoiceId}/void` - Void invoice
- `POST /api/invoices/{invoiceId}/credit-notes` - Create credit note

##### Code Example

```csharp
// InvoiceDetail.razor.cs
private async Task IssueInvoiceAsync()
{
    if (!await ConfirmActionAsync("Issue Invoice", "Are you sure you want to issue this invoice?"))
        return;
    
    var result = await _apiClient.IssueInvoiceAsync(InvoiceId);
    
    if (result.Success)
    {
        await LoadInvoiceAsync(); // Refresh
        ShowSuccessMessage("Invoice issued successfully");
    }
}
```

---

### Invoice Runs

**Route**: `/billing/invoice-runs`  
**Component**: `InvoiceRuns.razor`

#### Purpose

Execute batch invoice generation across multiple active leases and track execution history.

#### Features

**Run History Table**:
- **Run Number**: Unique identifier (e.g., RUN-202601-001)
- **Billing Period**: Start and end dates
- **Status**: Pending, InProgress, Completed, CompletedWithErrors, Failed, Cancelled
- **Started At**: Execution start timestamp
- **Completed At**: Execution completion timestamp
- **Total Leases**: Number of leases processed
- **Success Count**: Successfully generated invoices
- **Failure Count**: Failed invoice generations
- **Actions**: View details, retry failed (future)

**Status Indicators**:
- Pending: Gray
- InProgress: Blue (animated spinner)
- Completed: Green
- CompletedWithErrors: Yellow (warning icon)
- Failed: Red
- Cancelled: Dark gray

**Initiate New Run**:
- Click "Run Invoice Generation" button
- Select billing period (start/end dates)
- Choose proration method
- Add optional notes
- Submit to start batch process

#### Run Execution Process

1. **Preparation**: 
   - System identifies all active leases in organization
   - Creates InvoiceRun record with status "Pending"
   - Generates unique run number

2. **Execution**:
   - Status changes to "InProgress"
   - For each lease:
     - Generate invoice using configured settings
     - Record success or failure
     - Create InvoiceRunItem record

3. **Completion**:
   - Calculate success/failure counts
   - Set completion timestamp
   - Determine final status:
     - All succeeded → Completed
     - Some failed → CompletedWithErrors
     - All failed → Failed

4. **Notification** (Future):
   - Email notification to admins
   - Summary of results

#### Usage Workflow

**Starting a New Run**:
1. Navigate to Invoice Runs page
2. Click "Run Invoice Generation"
3. Select billing period (e.g., 2026-01-01 to 2026-01-31)
4. Choose proration method (ActualDaysInMonth)
5. Add notes (e.g., "January 2026 monthly billing")
6. Click "Start Run"
7. Run begins executing
8. Status updates in real-time
9. Navigate to run detail to see per-lease results

**Viewing Run Details**:
1. Click on run number or "View Details"
2. See summary: status, totals, timestamps
3. View per-lease results table
4. See error messages for failed items
5. Export results (future)

#### API Integration

- `GET /api/invoice-runs` - List all invoice runs
- `POST /api/invoice-runs` - Execute new invoice run
- `GET /api/invoice-runs/{runId}` - Get run summary
- `GET /api/invoice-runs/{runId}/items` - Get per-lease results

#### Code Example

```csharp
// InvoiceRuns.razor.cs
private async Task ExecuteInvoiceRunAsync()
{
    var request = new ExecuteInvoiceRunRequest
    {
        OrgId = _currentOrgId,
        BillingPeriodStart = _runPeriod.StartDate,
        BillingPeriodEnd = _runPeriod.EndDate,
        ProrationMethod = ProrationMethod.ActualDaysInMonth,
        Notes = _runNotes
    };
    
    var result = await _apiClient.ExecuteInvoiceRunAsync(request);
    
    if (result.Success)
    {
        NavigateToRunDetail(result.Data.Id);
    }
}
```

---

#### Invoice Run Detail

**Route**: `/billing/invoice-runs/{runId}`  
**Component**: `InvoiceRunDetail.razor`

##### Purpose

View detailed results of a specific invoice run execution, including per-lease outcomes.

##### Sections

**1. Run Summary**:
- Run number and status
- Billing period (start/end dates)
- Execution timestamps (started, completed)
- Duration (calculated)
- Total counts (leases, success, failure)
- Overall status badge

**2. Per-Lease Results Table**:

| Lease # | Unit | Tenant | Status | Invoice # | Amount | Error Message |
|---------|------|--------|--------|-----------|--------|---------------|
| LSE-001 | A-101 | John Doe | Success | INV-000001 | $15,000 | - |
| LSE-002 | A-102 | Jane Smith | Success | INV-000002 | $17,000 | - |
| LSE-003 | A-103 | Bob Wilson | Failed | - | - | Missing billing settings |

**3. Actions**:
- **Retry Failed**: Re-run failed invoice generations (future)
- **Export Results**: Download results as CSV/Excel (future)
- **View Invoice**: Navigate to generated invoice

##### Features

**Success/Failure Indicators**:
- Success: Green checkmark icon
- Failure: Red error icon
- Color-coded status chips

**Error Messages**:
- Detailed error for each failed item
- Helps identify and fix issues
- Common errors:
  - Missing billing settings
  - No recurring charges defined
  - Invalid lease status
  - Duplicate invoice for period

**Filtering**:
- Show all results
- Show only failures
- Show only successes

##### API Integration

- `GET /api/invoice-runs/{runId}/items` - Load per-lease results

---

### Credit Notes

Credit note functionality is integrated into the Invoice Detail component but follows a specific workflow.

#### Purpose

Issue credits for refunds, corrections, and adjustments to previously issued invoices.

#### Credit Note Reasons

- **InvoiceError**: Incorrect amount or charge on original invoice
- **Discount**: Promotional discount or goodwill gesture
- **Refund**: Return of payment (tenant overpaid)
- **Goodwill**: Customer service gesture
- **Adjustment**: General adjustment
- **Other**: Other reasons with notes

#### Create Credit Note Workflow

**From Invoice Detail Page**:

1. Navigate to invoice detail
2. Click "Create Credit Note" button
3. **Select Line Items**:
   - Choose which invoice lines to credit
   - Specify amount to credit (partial or full)
4. **Choose Reason**: Select from dropdown
5. **Add Notes**: Explain reason for credit
6. **Preview**:
   - Shows credit note number (auto-generated)
   - Total credit amount
   - Updated invoice balance
7. **Submit**: Create credit note
8. **Confirmation**: Credit note created and applied to invoice

#### Credit Note Details

**Generated Information**:
- **Credit Note Number**: Auto-generated (e.g., CN-202601-000001)
- **Credit Note Date**: Current date
- **Reference Invoice**: Original invoice number
- **Total Amount**: Total credit amount
- **Line Items**: Detailed breakdown matching invoice lines

**Effect on Invoice**:
- Reduces invoice balance
- Updates invoice status if fully credited
- Creates audit trail
- Cannot be reversed (must create another adjustment)

#### API Integration

- `POST /api/invoices/{invoiceId}/credit-notes` - Create credit note
- `GET /api/credit-notes/{creditNoteId}` - View credit note
- `POST /api/credit-notes/{creditNoteId}/void` - Void credit note (future)

#### Code Example

```csharp
// InvoiceDetail.razor.cs
private async Task CreateCreditNoteAsync()
{
    var request = new CreateCreditNoteRequest
    {
        InvoiceId = InvoiceId,
        Reason = _creditNote.Reason,
        TotalAmount = _creditNote.TotalAmount,
        Notes = _creditNote.Notes,
        LineItems = _creditNote.LineItems.Select(li => new CreditNoteLineRequest
        {
            InvoiceLineId = li.InvoiceLineId,
            Description = li.Description,
            Amount = li.Amount,
            TaxAmount = li.TaxAmount
        }).ToList()
    };
    
    var result = await _apiClient.CreateCreditNoteAsync(request);
    
    if (result.Success)
    {
        await LoadInvoiceAsync(); // Refresh invoice
        ShowSuccessMessage($"Credit note {result.Data.CreditNoteNumber} created");
    }
}
```

---

## Tenant Portal - My Bills

The Tenant Portal provides tenants with read-only access to view their invoices (bills) for their active lease.

### My Bills List

**Route**: `/tenant/my-bills`  
**Component**: `TenantInvoices.razor`

#### Purpose

Allows tenants to view all their invoices in a mobile-friendly, paginated list with filtering capabilities.

#### Key Features

**Invoice List Display**:
- Shows only issued invoices (not drafts) by default
- Displays invoice number, dates, billing period, status, and amounts
- Color-coded status indicators (Issued, Paid, Overdue, etc.)
- Shows both total amount and outstanding balance

**Filtering**:
- Filter by invoice status (Issued, Partially Paid, Paid, Overdue)
- Apply filters button to refresh results

**Pagination**:
- 10 invoices per page by default
- Page navigation controls
- Shows current page and total pages

**Responsive Layout**:
- **Mobile View**: Card-based layout with all key information
- **Desktop View**: Table layout with sortable columns
- Automatically adapts based on screen size

**Authorization**:
- Protected by `PolicyNames.RequireTenantRole`
- Only shows invoices for the current tenant's lease

#### Usage Example

```razor
@page "/tenant/my-bills"
@attribute [Authorize(Policy = PolicyNames.RequireTenantRole)]

<!-- Component automatically:
  1. Fetches tenant's lease information
  2. Loads all invoices for that lease
  3. Filters to show only issued invoices
  4. Displays in paginated, responsive format
-->
```

#### Code Structure

```
TenantInvoices.razor        # UI markup with responsive layouts
TenantInvoices.razor.cs     # Code-behind with filtering and pagination logic
```

---

### Bill Details

**Route**: `/tenant/my-bills/{invoiceId}`  
**Component**: `TenantInvoiceDetail.razor`

#### Purpose

Provides detailed view of a specific invoice with complete line item breakdown and payment status.

#### Key Features

**Invoice Header**:
- Invoice number and dates (invoice date, due date)
- Billing period
- Status indicator with color coding
- Issue date (when the invoice was finalized)

**Line Items Breakdown**:
- Complete list of all charges
- For each line item:
  - Charge type (e.g., Rent, Utilities, Maintenance)
  - Description
  - Quantity and unit price
  - Amount before tax
  - Tax amount
  - Total amount

**Financial Summary**:
- Subtotal (before tax)
- Total tax amount
- Grand total
- Amount paid (if any)
- Outstanding balance

**Payment Status**:
- Visual indicator for paid/unpaid status
- Shows partial payment amounts
- Placeholder message for payment tracking (Payments & Collection feature)

**Additional Information**:
- Invoice notes (if any)
- Payment instructions (if provided)

**Actions**:
- Download PDF button (placeholder, disabled - coming soon)
- Back to My Bills navigation

**Responsive Layout**:
- **Mobile View**: Card-based layout for line items
- **Desktop View**: Table layout for line items
- Automatically adapts based on screen size

**Authorization & Security**:
- Protected by `PolicyNames.RequireTenantRole`
- Verifies invoice belongs to tenant's lease
- Returns 403 Forbidden if tenant tries to access another tenant's invoice

#### Usage Example

```razor
@page "/tenant/my-bills/{Id:guid}"
@attribute [Authorize(Policy = PolicyNames.RequireTenantRole)]

<!-- Component automatically:
  1. Validates tenant has access to this invoice
  2. Loads complete invoice details with line items
  3. Displays in mobile-friendly format
  4. Shows payment status and financial breakdown
-->
```

#### Code Structure

```
TenantInvoiceDetail.razor       # UI markup with responsive layouts
TenantInvoiceDetail.razor.cs    # Code-behind with invoice loading logic
```

---

## Navigation

### Menu Structure

Billing features are accessible via the main navigation menu:

**For Managers/Administrators:**
```
📊 Billing
├── 📈 Dashboard               (/billing/dashboard)
├── ⚙️ Lease Settings          (via Lease detail page)
├── 🔄 Recurring Charges       (via Lease detail page)
├── ⚡ Utility Statements      (via Lease detail page)
├── 📄 Invoices                (/billing/invoices)
└── 🚀 Invoice Runs            (/billing/invoice-runs)
```

**For Tenants:**
```
👤 Tenant Portal
├── 🏠 Dashboard               (/tenant/dashboard)
├── 📋 Lease Details           (/tenant/lease-summary)
├── 🧾 My Bills                (/tenant/my-bills)
├── 📁 My Documents            (/tenant/documents)
└── ✅ Move-in Handover        (/tenant/move-in)
```

### Routes Reference

**Manager/Administrator Routes:**

| Route | Component | Description |
|-------|-----------|-------------|
| `/billing/dashboard` | BillingDashboard | Main billing hub |
| `/billing/lease-settings/{leaseId}` | LeaseBillingSettings | Configure lease billing |
| `/billing/recurring-charges/{leaseId}` | RecurringCharges | Manage recurring charges |
| `/billing/utility-statements/{leaseId}` | UtilityStatements | Record utilities |
| `/billing/invoices` | InvoiceList | Browse invoices |
| `/billing/invoices/{invoiceId}` | InvoiceDetail | View invoice details |
| `/billing/invoice-runs` | InvoiceRuns | View run history |
| `/billing/invoice-runs/{runId}` | InvoiceRunDetail | View run details |

**Tenant Portal Routes:**

| Route | Component | Description | Authorization |
|-------|-----------|-------------|---------------|
| `/tenant/my-bills` | TenantInvoices | View all bills for tenant's lease | RequireTenantRole |
| `/tenant/my-bills/{invoiceId}` | TenantInvoiceDetail | View detailed bill information | RequireTenantRole |

### Navigation Guards

- **Manager/Admin routes**: Protected with `[Authorize(Policy = PolicyNames.RequireManagerRole)]`
- **Tenant routes**: Protected with `[Authorize(Policy = PolicyNames.RequireTenantRole)]`

Unauthorized users are redirected to login or shown "Access Denied" message.

---

## Authorization

### Role-Based Access Control

**Allowed Roles**:
- **Manager**: Can view and manage all billing operations
- **Administrator**: Full access to billing features
- **SuperAdmin**: Complete system access

**Denied Roles**:
- **User**: No access to billing screens
- **Tenant**: No access to billing screens (tenants see their own invoices via tenant portal)
- **Guest**: No access

### Policy Configuration

Billing screens use the centralized authorization policy:

```csharp
[Authorize(Policy = PolicyNames.RequireManagerRole)]
```

This policy requires the user to have one of: Manager, Administrator, or SuperAdmin roles.

### API Authorization

All billing API endpoints enforce the same role requirements:

```csharp
[Authorize(Policy = PolicyNames.RequireManagerRole)]
public class InvoicesController : ControllerBase
{
    // All endpoints require Manager+ role
}
```

### Navigation Menu Visibility

Billing menu items are conditionally rendered based on user roles:

```razor
<AuthorizeView Policy="@PolicyNames.RequireManagerRole">
    <Authorized>
        <MudNavLink Href="/billing/dashboard" Icon="@Icons.Material.Filled.Dashboard">
            Billing Dashboard
        </MudNavLink>
    </Authorized>
</AuthorizeView>
```

Users without appropriate roles will not see billing menu items.

---

## Common Workflows

### Workflow 1: Monthly Billing Process

**Goal**: Generate invoices for all active leases for the month

**Steps**:
1. **Navigate** to Billing Dashboard (`/billing/dashboard`)
2. **Review** current month statistics
3. **Click** "Run Invoice Generation" button
4. **Configure Run**:
   - Billing Period: 2026-01-01 to 2026-01-31
   - Proration: ActualDaysInMonth
   - Notes: "January 2026 monthly billing"
5. **Submit** to start batch process
6. **Monitor** run status (InProgress → Completed)
7. **Navigate** to Invoice Run Detail
8. **Review** per-lease results
9. **Fix** any failures (e.g., add missing settings)
10. **Retry** failed items (future feature)
11. **Notify** tenants (future: automated email)

**Result**: All active leases have invoices generated for January 2026

---

### Workflow 2: Configure New Lease Billing

**Goal**: Set up billing settings for a newly created lease

**Steps**:
1. **Navigate** to Lease Detail page
2. **Click** "Billing Settings" tab
3. **Configure Settings**:
   - Billing Day: 1 (first of month)
   - Payment Terms: 5 days
   - Auto-Generation: ✅ Enabled
   - Invoice Prefix: "INV"
   - Payment Instructions: "Bank transfer to Account #12345"
4. **Preview** settings
5. **Save** configuration
6. **Add Recurring Charges**:
   - Click "Recurring Charges" tab
   - Add RENT charge: $15,000/month
   - Add MAINT charge: $2,000/month
   - Set start dates
7. **Record Initial Utility Meters** (if applicable):
   - Click "Utility Statements"
   - Record initial meter readings
8. **Verify** everything is configured
9. **Wait** for first auto-generation or manually generate invoice

**Result**: Lease is ready for automated monthly billing

---

### Workflow 3: Handle Billing Dispute

**Goal**: Issue credit note for incorrect charge

**Steps**:
1. **Receive** tenant complaint about incorrect maintenance charge
2. **Navigate** to Invoice List (`/billing/invoices`)
3. **Search** for invoice by number or tenant
4. **Open** Invoice Detail
5. **Review** line items
6. **Identify** incorrect charge (Maintenance: $2,500 instead of $2,000)
7. **Click** "Create Credit Note"
8. **Select** Maintenance line item
9. **Enter** credit amount: $500
10. **Choose** reason: InvoiceError
11. **Add** notes: "Maintenance charge overcharged by $500"
12. **Preview** credit note
13. **Submit** to create
14. **Verify** invoice balance updated
15. **Notify** tenant of credit (email/phone)

**Result**: Invoice balance reduced by $500, tenant satisfied

---

### Workflow 4: Record Utility Consumption

**Goal**: Bill tenant for monthly electricity usage

**Steps**:
1. **Collect** meter reading from unit
   - Previous: 1000 kWh
   - Current: 1250 kWh
2. **Navigate** to Lease Detail → Utility Statements
3. **Click** "Record Utility Statement"
4. **Select** "Meter-Based" mode
5. **Configure**:
   - Utility Type: Electricity
   - Billing Period: 2026-01-01 to 2026-01-31
   - Rate Plan: "Residential Electricity - 2026"
   - Previous Reading: 1000
   - Current Reading: 1250
6. **System Calculates**:
   - Units Consumed: 250 kWh
   - Amount: $35.00 (based on rate plan)
7. **Add** notes: "January 2026 electricity consumption"
8. **Save** statement
9. **Verify** statement appears in list
10. **Generate** invoice (manually or wait for auto-generation)
11. **Confirm** electricity line item appears on invoice

**Result**: Tenant billed accurately for electricity usage

---

### Workflow 5: Issue Invoices to Tenants

**Goal**: Finalize and issue all draft invoices for the month

**Steps**:
1. **Navigate** to Invoice List
2. **Filter** by status: Draft
3. **Review** each draft invoice:
   - Open Invoice Detail
   - Verify line items
   - Check totals
   - Review lease and tenant information
4. **Issue** each invoice:
   - Click "Issue Invoice" button
   - Confirm action
   - Status changes to Issued
5. **Batch Issue** (future): Select multiple and issue all at once
6. **Export** invoice list for records
7. **Send** invoices to tenants:
   - Email with PDF attachment (future)
   - Portal notification (future)
   - Manual email/delivery
8. **Update** tracking spreadsheet (if using)

**Result**: All tenants receive their invoices for the month

---

## Tips & Best Practices

### Configuration

✅ **Always configure billing settings before first invoice**  
✅ **Use consistent invoice prefixes across organization**  
✅ **Enable auto-generation only after testing manually**  
✅ **Set payment terms to match your policies (e.g., 5 days)**  
✅ **Add clear payment instructions to every lease**

### Recurring Charges

✅ **Review and verify all charges before lease activation**  
✅ **Use descriptive names** (e.g., "Rent - Unit A-101" not just "Rent")  
✅ **Set end dates for temporary charges**  
✅ **Deactivate rather than delete** for audit trail  
✅ **Document frequency clearly** in description if non-standard

### Utility Billing

✅ **Record meter readings consistently on same day each month**  
✅ **Double-check readings** before saving to avoid errors  
✅ **Use meter-based when possible** for accuracy and transparency  
✅ **Keep utility bills** for verification (upload when feature available)  
✅ **Create rate plans** that match your provider's actual rates

### Invoice Management

✅ **Review drafts carefully** before issuing  
✅ **Issue invoices promptly** at start of billing period  
✅ **Track overdue invoices** weekly via dashboard  
✅ **Document reasons** when voiding invoices  
✅ **Use credit notes** for corrections, not voids

### Batch Billing

✅ **Run invoice generation** on consistent schedule (e.g., 1st of month)  
✅ **Review run results** immediately after completion  
✅ **Fix failures promptly** to avoid missing billing  
✅ **Monitor run history** for patterns or recurring issues  
✅ **Add notes** to each run for audit trail

### Security & Compliance

✅ **Limit billing access** to Manager+ roles only  
✅ **Review audit logs** regularly  
✅ **Backup invoice data** before major changes  
✅ **Document all credit notes** with clear reasons  
✅ **Keep historical records** for tax and legal compliance

---

## Troubleshooting

### Common Issues

**Issue**: Invoice generation fails for a lease  
**Solution**: Check billing settings exist, verify recurring charges are active, ensure lease is active

**Issue**: Utility calculation incorrect  
**Solution**: Verify rate plan slabs, check meter readings, ensure correct rate plan selected

**Issue**: Cannot issue invoice  
**Solution**: Verify invoice is in Draft status, check all line items are valid, ensure total amount > 0

**Issue**: Billing menu not visible  
**Solution**: Verify user has Manager, Administrator, or SuperAdmin role assigned

**Issue**: Auto-generation not working  
**Solution**: Check billing settings has `GenerateInvoiceAutomatically = true`, verify billing day is set

---

## Related Documentation

- **[Billing Engine Guide](BILLING_ENGINE.md)** - Backend billing architecture and database schema
- **[API Guide](API_GUIDE.md)** - Complete API endpoint reference
- **[Authorization Guide](AUTHORIZATION_GUIDE.md)** - Role-based access control details
- **[Project Structure](PROJECT_STRUCTURE.md)** - Component organization and structure
- **[Tenant & Lease Management](TENANT_LEASE_MANAGEMENT.md)** - Lease lifecycle management

---

## Support

For questions or issues with the billing UI:
- Check this guide and related documentation
- Review component source code in `src/TentMan.Ui/Pages/Billing/`
- Report bugs on [GitHub Issues](https://github.com/chethandvg/TenantManagement/issues)
- Tag issues with `billing` and `ui` labels

---

**Last Updated**: 2026-01-11  
**Version**: 1.0  
**Part of**: TentMan Billing UI Feature (#51)  
**Maintainer**: TentMan Development Team
