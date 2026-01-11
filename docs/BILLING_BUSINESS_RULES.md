# Billing Business Rules - TentMan

Comprehensive documentation of business rules, timing conventions, and calculation methods for the TentMan billing engine.

---

## Table of Contents

- [Rent Timing Rules](#rent-timing-rules)
- [Utility Timing Rules](#utility-timing-rules)
- [Proration Rules](#proration-rules)
- [Immutability Rules](#immutability-rules)
- [Credit Note Workflow](#credit-note-workflow)
- [Invoice Lifecycle](#invoice-lifecycle)
- [Billing Day Rules](#billing-day-rules)
- [Tax Rules](#tax-rules)
- [Edge Cases and Exceptions](#edge-cases-and-exceptions)

---

## Rent Timing Rules

### Advance vs Arrears

**TentMan supports both advance and arrears billing**, but the default configuration is **arrears** (billing for the current/past period).

#### Arrears Billing (Default)

Rent is billed **after** the tenant has occupied the property for the billing period.

**Example - Arrears**:
```
Billing Period: January 1-31, 2026
Invoice Generated: February 1, 2026 (or configured billing day)
Payment Due: February 5, 2026 (invoice date + payment terms)
Covering: Rent for January (already occupied)
```

**Use Cases**:
- Standard residential leases in most regions
- Utilities always billed in arrears
- Easier for tenants to understand (pay for what you've used)

#### Advance Billing

Rent is billed **before** the tenant occupies the property for the billing period.

**Example - Advance**:
```
Billing Period: February 1-28, 2026
Invoice Generated: January 26, 2026 (5 days before period starts)
Payment Due: January 31, 2026 (before move-in)
Covering: Rent for February (not yet occupied)
```

**Configuration**:
To enable advance billing:
1. Configure `LeaseBillingSettings.BillingDay` to be before the start of the billing period
2. Adjust background job schedule to run earlier in the month
3. Set `PaymentTermDays` to ensure payment is due before the period starts

**Use Cases**:
- Commercial leases in some regions
- High-value properties requiring upfront payment
- Deposits and security deposits (always advance)

#### India-Specific Convention

In India, the most common practice is **advance billing** for rent:

**Standard Indian Lease**:
```
Month: January 2026
Invoice Date: December 25-31, 2025
Payment Due: January 1, 2026
Covering: Rent for January 2026 (paid in advance)
```

**Implementation**:
- Set `BillingDay = 26` (or earlier, e.g., 25)
- Background job runs on 26th of each month
- Generates invoices for the **next** month's billing period
- Example: Job runs on Jan 26 → generates invoice for Feb 1-28

**Code Configuration**:
```csharp
// In LeaseBillingSettings
BillingDay = 26,
PaymentTermDays = 5,
// Invoice generated on 26th, due on 31st/1st of next month
```

### Rent Calculation

#### Full Month Rent

When a lease covers a complete calendar month:

```csharp
// No proration needed
InvoiceLine {
    Description = "Monthly Rent - January 2026",
    Amount = 15000.00m,
    Quantity = 1,
    Source = "Rent",
    SourceRefId = leaseTermId
}
```

#### Partial Month Rent (Mid-Month Start)

When a lease starts mid-month, rent is **prorated** based on actual days occupied:

**Example - Lease starts Jan 15**:
```
Billing Period: Jan 1-31, 2026 (31 days)
Lease Start: Jan 15, 2026
Days Occupied: 17 days (Jan 15-31, inclusive)
Monthly Rent: ₹15,000

Proration (ActualDaysInMonth):
= (17 days / 31 days) × ₹15,000
= 0.5484 × ₹15,000
= ₹8,225.81
```

#### Partial Month Rent (Mid-Month End)

When a lease ends mid-month, final rent is **prorated**:

**Example - Lease ends Jan 15**:
```
Billing Period: Jan 1-31, 2026 (31 days)
Lease End: Jan 15, 2026
Days Occupied: 15 days (Jan 1-15, inclusive)
Monthly Rent: ₹15,000

Proration (ActualDaysInMonth):
= (15 days / 31 days) × ₹15,000
= 0.4839 × ₹15,000
= ₹7,258.06
```

#### Rent Changes Mid-Month

When rent amount changes during a billing period, the invoice will have **multiple line items**:

**Example - Rent increases from ₹10,000 to ₹12,000 on Jan 16**:
```
Line 1: Rent for Jan 1-15 (Term 1)
  = (15 days / 31 days) × ₹10,000
  = ₹4,838.71

Line 2: Rent for Jan 16-31 (Term 2)
  = (16 days / 31 days) × ₹12,000
  = ₹6,193.55

Total Rent for January: ₹11,032.26
```

---

## Utility Timing Rules

### Always Arrears

**Utilities are ALWAYS billed in arrears** (after consumption), regardless of rent billing method.

**Rationale**:
- Consumption cannot be known until the period ends
- Meter readings are taken at the end of the period
- Provider bills arrive after the consumption period

### Utility Billing Workflow

#### Timeline

```
Month: January 2026
Billing Period: Jan 1-31, 2026
Meter Reading Date: Jan 31, 2026 (or first few days of Feb)
Utility Statement Created: Feb 1-5, 2026
Invoice Generated: Feb 1 or next monthly invoice run
Payment Due: Per lease payment terms
```

#### Example Workflow

**Step 1 - Consumption Period**:
```
Jan 1: Previous Reading = 1000 kWh
Jan 31: Current Reading = 1250 kWh
Consumption: 250 kWh
```

**Step 2 - Record Utility Statement**:
```csharp
CreateUtilityStatementRequest {
    LeaseId = leaseGuid,
    UtilityType = UtilityType.Electricity,
    BillingPeriodStart = new DateOnly(2026, 1, 1),
    BillingPeriodEnd = new DateOnly(2026, 1, 31),
    IsMeterBased = true,
    PreviousReading = 1000,
    CurrentReading = 1250,
    UtilityRatePlanId = ratePlanGuid
}
```

**Step 3 - Calculate Amount**:
```
Using Rate Plan "Residential Electricity 2026":
  Slab 1: 0-100 units @ ₹3/unit = ₹300
  Slab 2: 101-200 units @ ₹4/unit = ₹400
  Slab 3: 201-250 units @ ₹5/unit = ₹250
Total: ₹950
```

**Step 4 - Include in Invoice**:
```
Invoice for January 2026 (generated Feb 1):
  Line 1: Monthly Rent = ₹15,000
  Line 2: Maintenance = ₹2,000
  Line 3: Electricity (Jan consumption) = ₹950
Total: ₹17,950
```

### Meter-Based vs Amount-Based

#### Meter-Based (Recommended)

Use when you have access to meter readings and want transparent, calculated billing.

**Advantages**:
- Transparent to tenants (can verify calculation)
- Automatic calculation using rate plans
- Audit trail with readings
- Handles tiered pricing correctly

**Example**:
```csharp
// System calculates based on consumption and rate plan
UtilityStatement {
    IsMeterBased = true,
    PreviousReading = 1000,
    CurrentReading = 1250,
    UnitsConsumed = 250,  // Auto-calculated
    CalculatedAmount = 950,  // Auto-calculated from rate plan
    TotalAmount = 950
}
```

#### Amount-Based (Direct Billing)

Use when you receive a provider bill and want to pass through the exact amount.

**Advantages**:
- Simple and fast
- Exact match to provider invoice
- No rate plan configuration needed

**Example**:
```csharp
// Direct amount from provider bill
UtilityStatement {
    IsMeterBased = false,
    DirectBillAmount = 1200,
    TotalAmount = 1200
}
```

### Late Utility Billing

Utilities can be added to **past billing periods** if meter readings are delayed:

**Scenario**:
- February invoice already generated and issued
- January electricity reading arrives late
- Need to bill for January electricity

**Solution**:
```csharp
// Create utility statement for past period
CreateUtilityStatementRequest {
    BillingPeriodStart = new DateOnly(2026, 1, 1),
    BillingPeriodEnd = new DateOnly(2026, 1, 31),
    // ... other fields
}

// Will be included in NEXT invoice (March)
// OR generate a supplemental invoice for January
```

---

## Proration Rules

### Proration Methods

TentMan supports two proration calculation methods, configurable per lease:

#### 1. Actual Days in Month (Default)

Uses the **actual number of calendar days** in each month (28-31 days).

**Formula**:
```
Prorated Amount = (Days Used / Total Days in Period) × Full Amount
```

**Example - January (31 days)**:
```
Full Rent: ₹15,000
Lease Start: Jan 15
Days Used: 17 days (Jan 15-31)

Proration:
= (17 / 31) × ₹15,000
= 0.548387 × ₹15,000
= ₹8,225.81
```

**Example - February (28 days)**:
```
Full Rent: ₹15,000
Lease Start: Feb 15
Days Used: 14 days (Feb 15-28)

Proration:
= (14 / 28) × ₹15,000
= 0.5 × ₹15,000
= ₹7,500.00
```

**Advantages**:
- Most accurate reflection of actual usage
- Fair to both landlord and tenant
- Handles leap years correctly

**Disadvantages**:
- Amounts vary by month (Jan vs Feb different for same dates)
- Slightly more complex to explain

#### 2. Thirty-Day Month

Uses a **fixed 30-day month** regardless of actual calendar days.

**Formula**:
```
Prorated Amount = (Days Used / 30) × Full Amount
```

**Example - January (31 actual days)**:
```
Full Rent: ₹15,000
Lease Start: Jan 15
Days Used: 17 days (Jan 15-31)

Proration:
= (17 / 30) × ₹15,000
= 0.566667 × ₹15,000
= ₹8,500.01
```

**Example - February (28 actual days)**:
```
Full Rent: ₹15,000
Lease Start: Feb 15
Days Used: 14 days (Feb 15-28)

Proration:
= (14 / 30) × ₹15,000
= 0.466667 × ₹15,000
= ₹7,000.01
```

**Advantages**:
- Consistent calculation across all months
- Simpler to explain
- Traditional banking/finance method

**Disadvantages**:
- Not perfectly accurate for usage
- May slightly favor landlord in longer months, tenant in shorter months

### When Proration Applies

Proration is automatically applied in these scenarios:

1. **Lease Start Mid-Month**: First invoice prorated
2. **Lease End Mid-Month**: Final invoice prorated
3. **Recurring Charge Start Mid-Month**: First charge prorated
4. **Recurring Charge End Mid-Month**: Final charge prorated
5. **Rent Change Mid-Month**: Both old and new rent prorated

### India-Specific Proration

In India, the **30-Day Month** method is more commonly used:

**Configuration**:
```csharp
LeaseBillingSettings {
    ProrationMethod = ProrationMethod.ThirtyDayMonth
}
```

**Reason**: Simplifies calculations and is traditional in Indian real estate.

---

## Immutability Rules

### Invoice States and Immutability

TentMan enforces strict immutability rules to ensure data integrity and audit trail compliance.

#### Draft Invoices

**Status**: `Draft`

**Rules**:
- ✅ **Can be modified** (regenerated with updated amounts)
- ✅ **Can be deleted**
- ✅ **Can be issued**
- ❌ **Not visible to tenants**

**Use Case**: Allow corrections before finalizing

#### Issued Invoices

**Status**: `Issued`

**Rules**:
- ❌ **Cannot be modified** (immutable)
- ❌ **Cannot be deleted** (soft delete only)
- ❌ **Cannot be regenerated**
- ✅ **Can be voided** (if unpaid)
- ✅ **Can have credit notes issued**
- ✅ **Visible to tenants**

**Timestamp**: `IssuedAtUtc` set when issued

**Use Case**: Finalized invoices maintain integrity for accounting and legal compliance

#### Paid/Partially Paid Invoices

**Status**: `Paid` or `PartiallyPaid`

**Rules**:
- ❌ **Cannot be modified**
- ❌ **Cannot be deleted**
- ❌ **Cannot be voided** (payment already received)
- ✅ **Can have credit notes issued** (for refunds)

**Timestamp**: `PaidAtUtc` set when fully paid

**Use Case**: Preserve payment history

#### Voided Invoices

**Status**: `Cancelled` (after voiding)

**Rules**:
- ❌ **Terminal state** - no further changes allowed
- ❌ **Cannot be un-voided**
- ❌ **Cannot have payments applied**
- ❌ **Cannot have credit notes issued**

**Timestamp**: `VoidedAtUtc` set when voided

**Field**: `VoidReason` - required explanation

**Use Case**: Cancel invoice issued in error

### State Transition Diagram

```
    Draft
      │
      ├─ (regenerate) ─→ Draft (updated)
      ├─ (delete) ────→ [Deleted]
      └─ (issue) ─────→ Issued
                          │
                          ├─ (payment) ──→ PartiallyPaid
                          │                     │
                          │                     └─ (payment) ──→ Paid
                          │
                          ├─ (due date passed) → Overdue
                          │
                          ├─ (void, if unpaid) → Cancelled [Terminal]
                          │
                          └─ (credit note) ───→ Issued (with reduced balance)
```

### Why Immutability?

**Legal Compliance**:
- Invoices are legal documents in many jurisdictions
- Cannot be altered after issuance for tax/audit purposes

**Accounting Integrity**:
- Prevents manipulation of financial records
- Maintains accurate audit trail

**Tenant Trust**:
- Tenants can rely on issued invoices
- No "surprise" changes after issuance

---

## Credit Note Workflow

### Purpose

Credit notes are used to adjust or refund issued invoices **without** modifying the original invoice (maintaining immutability).

### When to Use Credit Notes

1. **Invoice Error**: Incorrect amount charged
2. **Discount**: Promotional discount or goodwill
3. **Refund**: Tenant overpaid or paid in advance
4. **Adjustment**: General adjustments to balances
5. **Partial Credit**: Partial refund or discount

### Credit Note Process

#### Step 1: Identify Invoice to Credit

```csharp
// Original invoice
Invoice {
    InvoiceNumber = "INV-202601-000042",
    Status = InvoiceStatus.Issued,
    TotalAmount = 17000,
    PaidAmount = 0,
    BalanceAmount = 17000
}
```

#### Step 2: Create Credit Note

```csharp
CreateCreditNoteRequest {
    InvoiceId = invoiceGuid,
    Reason = CreditNoteReason.InvoiceError,
    Notes = "Maintenance charge was incorrect - should be ₹1500 not ₹2000",
    LineItems = [
        new CreditNoteLineRequest {
            InvoiceLineId = maintenanceLineGuid,
            Description = "Credit for maintenance overcharge",
            Amount = 500,  // Negative applied to invoice
            TaxAmount = 0,
            TotalAmount = 500
        }
    ]
}
```

#### Step 3: Issue Credit Note

```csharp
// Credit note created
CreditNote {
    CreditNoteNumber = "CN-202601-000008",
    InvoiceId = invoiceGuid,
    Reason = CreditNoteReason.InvoiceError,
    TotalAmount = 500,
    AppliedAtUtc = null  // Not yet applied
}

// Issue the credit note
await creditNoteService.IssueCreditNoteAsync(creditNoteGuid);

// After issuing
CreditNote {
    AppliedAtUtc = "2026-01-15T10:30:00Z"
}
```

#### Step 4: Invoice Balance Updated

```csharp
// Updated invoice
Invoice {
    InvoiceNumber = "INV-202601-000042",
    Status = InvoiceStatus.Issued,  // Status unchanged
    TotalAmount = 17000,  // Original total unchanged
    PaidAmount = 0,
    BalanceAmount = 16500,  // Reduced by credit note
    // Original invoice remains immutable
}
```

### Credit Note Rules

**Restrictions**:
- ❌ Cannot create credit note for **Draft** invoices (just regenerate)
- ❌ Cannot create credit note for **Voided** invoices
- ✅ Can create credit note for **Issued**, **Paid**, or **PartiallyPaid** invoices
- ❌ Cannot credit more than the invoice balance (validation enforced)

**Effect on Invoice**:
- Original invoice amounts **unchanged**
- `BalanceAmount` reduced by credit note total
- Invoice status may change to `Paid` if credit note covers full balance

### Credit Note vs Void Invoice

| Scenario | Use Credit Note | Use Void Invoice |
|----------|----------------|------------------|
| Invoice already paid | ✅ Yes | ❌ No |
| Partial correction | ✅ Yes | ❌ No |
| Small error | ✅ Yes | Either |
| Completely wrong invoice | Either | ✅ Yes (if unpaid) |
| Discount/goodwill | ✅ Yes | ❌ No |

---

## Invoice Lifecycle

### Complete Lifecycle

```
1. DRAFT
   ├─ Created: Invoice generated by system or manually
   ├─ Can be regenerated/updated
   ├─ Not visible to tenants
   └─ Default state for new invoices

2. ISSUED
   ├─ Draft invoice finalized and sent to tenant
   ├─ IssuedAtUtc timestamp set
   ├─ Immutable (cannot be modified)
   └─ Visible to tenants

3. PARTIALLY PAID
   ├─ Partial payment received
   ├─ PaidAmount < TotalAmount
   └─ BalanceAmount > 0

4. PAID
   ├─ Full payment received
   ├─ PaidAmount = TotalAmount
   ├─ BalanceAmount = 0
   └─ PaidAtUtc timestamp set

5. OVERDUE
   ├─ Due date passed without full payment
   ├─ DueDate < CurrentDate
   ├─ BalanceAmount > 0
   └─ May trigger late fees (future feature)

6. CANCELLED (Voided)
   ├─ Invoice voided before payment
   ├─ VoidedAtUtc timestamp set
   ├─ VoidReason recorded
   └─ Terminal state (no further changes)

7. WRITTEN OFF
   ├─ Uncollectible debt written off
   ├─ Balance removed from accounts
   └─ Future feature
```

### Automatic Status Updates

**System automatically updates status based on**:

```csharp
// Status calculation logic
if (PaidAmount == 0 && IssuedAtUtc != null)
    Status = InvoiceStatus.Issued;

if (PaidAmount > 0 && PaidAmount < TotalAmount)
    Status = InvoiceStatus.PartiallyPaid;

if (PaidAmount >= TotalAmount)
{
    Status = InvoiceStatus.Paid;
    PaidAtUtc = DateTime.UtcNow;
}

if (DueDate < DateOnly.FromDateTime(DateTime.UtcNow) 
    && BalanceAmount > 0 
    && Status != InvoiceStatus.Cancelled)
    Status = InvoiceStatus.Overdue;
```

---

## Billing Day Rules

### Valid Billing Days

**Range**: 1-28 only

**Rationale**: Ensures billing day exists in **every month**, including February.

### February Handling

**Problem**: February has only 28 days (29 in leap years)

**Solution**: Restrict billing day to 1-28

**Examples**:

```
❌ Invalid: BillingDay = 29
Reason: February only has 28/29 days

❌ Invalid: BillingDay = 30
Reason: February only has 28/29 days

❌ Invalid: BillingDay = 31
Reason: Multiple months don't have 31 days

✅ Valid: BillingDay = 1
Works in: All months

✅ Valid: BillingDay = 28
Works in: All months (exists even in February)
```

### Recommended Billing Days

**For Monthly Rent**:
- **1st of month**: Most common, easy to remember
- **25th-28th**: Allows time for review before month end
- **15th**: Mid-month billing for special cases

**For India (Advance Billing)**:
- **26th**: Generate invoice for next month, due on 1st
- **27th**: Generate invoice for next month, due on 1st

### Background Job Timing

**Monthly Rent Job**:
```csharp
// Runs on 26th of each month at 2:00 AM UTC
RecurringJob.AddOrUpdate<MonthlyRentGenerationJob>(
    "monthly-rent-generation",
    job => job.ExecuteForNextMonthAsync(...),
    "0 2 26 * *");
```

**Lead Time**: 5 days before billing period starts (for advance billing)

---

## Tax Rules

### Tax Application

**TaxRate**: Configured per invoice line based on charge type

**Formula**:
```csharp
TaxAmount = Amount × (TaxRate / 100)
TotalAmount = Amount + TaxAmount
```

**Example**:
```
Charge Type: Maintenance
IsTaxable: true
TaxRate: 18% (GST in India)

InvoiceLine {
    Amount = 2000,
    TaxRate = 18,
    TaxAmount = 2000 × 0.18 = 360,
    TotalAmount = 2360
}
```

### Taxable Charge Types

**System-Defined Taxable Types**:
- ✅ Maintenance (MAINT) - typically taxable
- ❌ Rent (RENT) - typically not taxable (varies by jurisdiction)
- ✅ Utilities (ELEC, WATER, GAS) - may be taxable depending on region

**Configuration**:
```csharp
ChargeType {
    Code = ChargeTypeCode.MAINT,
    IsTaxable = true,  // Tax will be calculated
    TaxRate = 18  // Percentage (can be overridden per line)
}
```

### India-Specific Tax Rules

**GST (Goods and Services Tax)**:
- **Residential Rent**: Generally **exempt** from GST
- **Commercial Rent**: May be subject to GST (18%)
- **Maintenance Charges**: Typically subject to GST (18%)
- **Utilities**: Pass-through from provider (includes GST in provider bill)

---

## Edge Cases and Exceptions

### Handled Edge Cases

Comprehensive list of edge cases the billing engine handles correctly:

#### 1. Lease Starts Mid-Month

**Scenario**: Lease begins on Jan 15

**Handling**: Automatic proration for first invoice

```
Invoice Line:
  Description: "Rent for Jan 15-31, 2026 (17 days)"
  Amount: ₹8,225.81 (prorated)
```

#### 2. Lease Ends Mid-Month

**Scenario**: Lease terminates on Jan 15

**Handling**: Automatic proration for final invoice

```
Invoice Line:
  Description: "Rent for Jan 1-15, 2026 (15 days)"
  Amount: ₹7,258.06 (prorated)
```

#### 3. Rent Changes Mid-Month

**Scenario**: Rent increases from ₹10,000 to ₹12,000 on Jan 16

**Handling**: Multiple line items, each prorated

```
Line 1: "Rent for Jan 1-15 @ ₹10,000/mo"
  Amount: ₹4,838.71

Line 2: "Rent for Jan 16-31 @ ₹12,000/mo"
  Amount: ₹6,193.55
```

#### 4. Duplicate Invoice Prevention

**Scenario**: Invoice generation triggered twice for same period

**Handling**: Idempotent - updates existing draft invoice

```csharp
// First call: Creates invoice
var invoice1 = await GenerateInvoiceAsync(lease, period);

// Second call: Updates same invoice (not duplicate)
var invoice2 = await GenerateInvoiceAsync(lease, period);

Assert.Equal(invoice1.Id, invoice2.Id);  // Same invoice
```

#### 5. February Leap Year

**Scenario**: Billing in February of a leap year

**Handling**: Correct day count (29 days vs 28 days)

```
Non-Leap Year (2026):
  Feb 1-28 = 28 days
  Proration: (X days / 28) × Rent

Leap Year (2024):
  Feb 1-29 = 29 days
  Proration: (X days / 29) × Rent
```

#### 6. Issued Invoice Immutability

**Scenario**: Attempt to regenerate an issued invoice

**Handling**: Rejected with error message

```csharp
var result = await GenerateInvoiceAsync(lease, period);

// Result.IsSuccess = false
// Result.ErrorMessage = "Cannot regenerate issued invoice"
```

#### 7. Utility Statement Versioning

**Scenario**: Need to correct a utility statement

**Handling**: Create new version, mark as final

```csharp
// Original statement (draft)
UtilityStatement {
    Version = 1,
    IsFinal = false
}

// Corrected statement
UtilityStatement {
    Version = 2,
    IsFinal = true  // Prevents further corrections
}
```

#### 8. Late Utility Billing

**Scenario**: January utility reading arrives in March

**Handling**: Create statement for past period, include in next invoice

```csharp
CreateUtilityStatementRequest {
    BillingPeriodStart = new DateOnly(2026, 1, 1),
    BillingPeriodEnd = new DateOnly(2026, 1, 31)
    // Will be included in March invoice or supplemental invoice
}
```

#### 9. Multiple Utility Types Same Period

**Scenario**: Electricity, Water, and Gas for same month

**Handling**: Separate statements, all included in invoice

```
Invoice for January 2026:
  Line 1: Rent = ₹15,000
  Line 2: Maintenance = ₹2,000
  Line 3: Electricity (Jan) = ₹950
  Line 4: Water (Jan) = ₹200
  Line 5: Gas (Jan) = ₹350
Total: ₹18,500
```

#### 10. Voiding Paid Invoice

**Scenario**: Attempt to void an invoice that's been paid

**Handling**: Rejected (use credit note instead)

```csharp
var result = await VoidInvoiceAsync(paidInvoiceId);

// Result.IsSuccess = false
// Result.ErrorMessage = "Cannot void paid invoice. Use credit note instead."
```

### Future Enhancements

Edge cases planned for future implementation:

- **Lease Renewal Mid-Month**: Automatic creation of new term
- **Currency Changes**: Multi-currency support
- **Discount Codes**: Promotional discounts applied to invoices
- **Payment Plans**: Installment payments for large invoices
- **Auto Late Fees**: Automatic late fee charges for overdue invoices

---

## Summary

This document provides comprehensive business rules for the TentMan billing engine. Key takeaways:

1. **Rent**: Configurable for advance or arrears (default: arrears, India: advance)
2. **Utilities**: Always arrears, after consumption period
3. **Proration**: Two methods (ActualDays or ThirtyDay), handles mid-month changes
4. **Immutability**: Strict rules maintain invoice integrity and audit compliance
5. **Credit Notes**: Proper way to adjust issued invoices
6. **Billing Day**: Restricted to 1-28 for universal compatibility
7. **Edge Cases**: Comprehensively handled for production reliability

---

**Last Updated**: 2026-01-11  
**Version**: 1.0  
**Part of**: TentMan Billing Engine Documentation  
**Related**: [BILLING_ENGINE.md](BILLING_ENGINE.md), [BILLING_UI_GUIDE.md](BILLING_UI_GUIDE.md)
