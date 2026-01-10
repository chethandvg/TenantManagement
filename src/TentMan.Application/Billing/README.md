# Billing Calculation Services

This directory contains the core calculation services for the billing engine, implementing proration logic, rent calculations, recurring charge calculations, utility billing, invoice generation, batch invoice runs, and number generation.

---

## 📁 Services Overview

### Invoice Generation and Orchestration

#### InvoiceGenerationService
Generates invoices for leases with support for rent, recurring charges, and future utility billing.

**Features**:
- Generates invoices for a specific lease and billing period
- Implements idempotency by updating existing draft invoices
- Calculates due dates based on billing settings
- Automatically generates rent and recurring charge lines
- Calculates totals and tax amounts

**Example**:
```csharp
var service = new InvoiceGenerationService(/* dependencies */);
var result = await service.GenerateInvoiceAsync(
    leaseId: leaseGuid,
    billingPeriodStart: new DateOnly(2024, 1, 1),
    billingPeriodEnd: new DateOnly(2024, 1, 31),
    prorationMethod: ProrationMethod.ActualDaysInMonth
);

if (result.IsSuccess)
{
    var invoice = result.Invoice;
    Console.WriteLine($"Invoice {invoice.InvoiceNumber} generated");
    Console.WriteLine($"Total: {invoice.TotalAmount:C}");
}
```

#### InvoiceRunService
Orchestrates batch invoice generation for multiple leases.

**Features**:
- Executes monthly rent billing runs across all active leases
- Handles partial failures with per-lease error tracking
- Logs run results with InvoiceRunItem records
- Supports both monthly rent and utility billing runs

**Example**:
```csharp
var service = new InvoiceRunService(/* dependencies */);
var result = await service.ExecuteMonthlyRentRunAsync(
    orgId: organizationGuid,
    billingPeriodStart: new DateOnly(2024, 1, 1),
    billingPeriodEnd: new DateOnly(2024, 1, 31),
    prorationMethod: ProrationMethod.ActualDaysInMonth
);

Console.WriteLine($"Run completed: {result.SuccessCount} succeeded, {result.FailureCount} failed");
```

---

### Proration Calculators

#### ActualDaysInMonthCalculator
Calculates prorated amounts using the actual number of days in a calendar month (28-31 days).

**Formula**: `(DaysUsed / TotalDaysInPeriod) × FullAmount`

**Example**:
```csharp
var calculator = new ActualDaysInMonthCalculator();
var prorated = calculator.CalculateProration(
    fullAmount: 10000m,
    startDate: new DateOnly(2024, 1, 15),
    endDate: new DateOnly(2024, 1, 31),
    billingPeriodStart: new DateOnly(2024, 1, 1),
    billingPeriodEnd: new DateOnly(2024, 1, 31)
);
// Result: 5483.87 (17 days out of 31)
```

#### ThirtyDayMonthCalculator
Calculates prorated amounts using a fixed 30-day month regardless of actual calendar days.

**Formula**: `(DaysUsed / 30) × FullAmount`

**Example**:
```csharp
var calculator = new ThirtyDayMonthCalculator();
var prorated = calculator.CalculateProration(
    fullAmount: 10000m,
    startDate: new DateOnly(2024, 1, 15),
    endDate: new DateOnly(2024, 1, 31),
    billingPeriodStart: new DateOnly(2024, 1, 1),
    billingPeriodEnd: new DateOnly(2024, 1, 31)
);
// Result: 5666.67 (17 actual days / 30)
```

---

### Rent Calculation Service

Calculates rent for a lease period, handling:
- Multiple lease terms (versioned terms)
- Mid-period rent changes with automatic proration
- Different billing frequencies

**Example**:
```csharp
var service = new RentCalculationService(leaseRepository);
var result = await service.CalculateRentAsync(
    leaseId: leaseGuid,
    billingPeriodStart: new DateOnly(2024, 1, 1),
    billingPeriodEnd: new DateOnly(2024, 1, 31),
    prorationMethod: ProrationMethod.ActualDaysInMonth
);

// Result includes line items for each term period
// e.g., Jan 1-15 at $10,000/mo = $4,838.71
//       Jan 16-31 at $12,000/mo = $6,193.55
```

---

### Recurring Charge Calculation Service

Calculates recurring charges (maintenance fees, parking, etc.) with proration support.

**Features**:
- Handles charges that start or end mid-period
- Supports different billing frequencies (monthly, quarterly, yearly)
- Automatically prorates based on effective dates

**Example**:
```csharp
var service = new RecurringChargeCalculationService(recurringChargeRepository);
var result = await service.CalculateChargesAsync(
    leaseId: leaseGuid,
    billingPeriodStart: new DateOnly(2024, 1, 1),
    billingPeriodEnd: new DateOnly(2024, 1, 31),
    prorationMethod: ProrationMethod.ActualDaysInMonth
);

// Result includes all active recurring charges prorated if needed
```

---

### Utility Calculation Service

Calculates utility charges with three modes:

#### 1. Amount-Based (Direct Billing)
```csharp
var service = new UtilityCalculationService(ratePlanRepository);
var result = service.CalculateAmountBased(
    amount: 1500m,
    utilityType: UtilityType.Electricity
);
// Direct pass-through of utility bill amount
```

#### 2. Meter-Based with Flat Rate
```csharp
var result = service.CalculateMeterBasedFlatRate(
    unitsConsumed: 250m,
    ratePerUnit: 5.50m,
    fixedCharge: 50m,
    utilityType: UtilityType.Electricity
);
// Result: (250 × 5.50) + 50 = 1425.00
```

#### 3. Meter-Based with Slabs
```csharp
var result = await service.CalculateMeterBasedSlabsAsync(
    unitsConsumed: 350m,
    ratePlanId: ratePlanGuid,
    utilityType: UtilityType.Electricity
);
// Calculates using tiered rates:
// 0-100 units @ $3/unit = $300
// 101-200 units @ $4/unit = $400
// 201-350 units @ $5/unit = $750
// Total: $1450
```

---

### Number Generation Services

Thread-safe services for generating unique invoice and credit note numbers.

#### Invoice Number Generator
```csharp
var generator = new InvoiceNumberGenerator(numberSequenceRepository);
var invoiceNumber = await generator.GenerateNextAsync(
    orgId: organizationGuid,
    prefix: "INV" // Optional, defaults to "INV"
);
// Result: "INV-202601-000042"
```

#### Credit Note Number Generator
```csharp
var generator = new CreditNoteNumberGenerator(numberSequenceRepository);
var creditNoteNumber = await generator.GenerateNextAsync(
    orgId: organizationGuid,
    prefix: "CN" // Optional, defaults to "CN"
);
// Result: "CN-202601-000015"
```

**Number Format**: `{PREFIX}-{YYYYMM}-{NNNNNN}`
- PREFIX: Customizable prefix (default: INV or CN)
- YYYYMM: Year and month
- NNNNNN: 6-digit sequence number (padded with zeros)

---

## 🔧 Dependencies

### Repository Interfaces Required
- `ILeaseRepository` - For fetching lease and term data
- `ILeaseRecurringChargeRepository` - For recurring charges
- `IUtilityRatePlanRepository` - For utility rate plans and slabs
- `INumberSequenceRepository` - For thread-safe number generation

---

## 🧪 Testing

All services have comprehensive unit test coverage:
- `ActualDaysInMonthCalculatorTests` - 13 tests
- `ThirtyDayMonthCalculatorTests` - 15 tests  
- `UtilityCalculationServiceTests` - 16 tests
- `InvoiceNumberGeneratorTests` - 10 tests
- `CreditNoteNumberGeneratorTests` - 11 tests

Run billing tests:
```bash
dotnet test --filter "Feature=Billing"
```

---

## 📝 Design Decisions

### Proration Methods
Two methods are provided to accommodate different business requirements:
- **Actual Days**: More accurate for short-term rentals or varying month lengths
- **30-Day Month**: Simplified calculation, consistent across all months

### Rounding
All monetary calculations round to 2 decimal places using `MidpointRounding.AwayFromZero` for consistency.

### Thread Safety
Number generators rely on repository-level locking to ensure uniqueness across concurrent requests.

### Immutability
Result objects are designed as records or with init-only properties to ensure calculation results cannot be modified after creation.

---

## 🚀 Usage in Billing Engine

These services are building blocks for the billing engine and should be used by:
1. **Invoice Generation** - Combine rent, recurring charges, and utilities
2. **Credit Notes** - Calculate refunds with proration
3. **Billing Previews** - Show tenants upcoming charges
4. **Reporting** - Generate financial reports

---

**Last Updated**: 2026-01-10  
**Maintainer**: TentMan Development Team
