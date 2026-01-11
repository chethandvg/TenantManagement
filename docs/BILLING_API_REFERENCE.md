# Billing API Reference - TentMan

Complete API endpoint reference for the TentMan billing engine. All endpoints are versioned (v1.0) and require authentication.

---

## Table of Contents

- [Authentication](#authentication)
- [Common Response Format](#common-response-format)
- [Billing Settings](#billing-settings)
- [Recurring Charges](#recurring-charges)
- [Charge Types](#charge-types)
- [Utility Statements](#utility-statements)
- [Invoices](#invoices)
- [Invoice Runs](#invoice-runs)
- [Credit Notes](#credit-notes)
- [Error Handling](#error-handling)

---

## Authentication

All billing API endpoints require authentication via JWT Bearer token.

**Authorization Header**:
```http
Authorization: Bearer <your-jwt-token>
```

**Required Roles**:
- Most billing endpoints: `Manager`, `Administrator`, or `SuperAdmin`
- Tenant invoice viewing: `Tenant` role

**Unauthorized Response** (401):
```json
{
  "statusCode": 401,
  "message": "Unauthorized"
}
```

**Forbidden Response** (403):
```json
{
  "statusCode": 403,
  "message": "User does not have the required role"
}
```

---

## Common Response Format

All API responses follow a consistent format using `ApiResponse<T>`.

### Success Response

```json
{
  "success": true,
  "data": { /* response data */ },
  "message": "Success message",
  "errors": null
}
```

### Error Response

```json
{
  "success": false,
  "data": null,
  "message": "Error message",
  "errors": [
    "Detailed error 1",
    "Detailed error 2"
  ]
}
```

### Validation Error Response (400)

```json
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errors": [
    "Billing day must be between 1 and 28",
    "Payment term days must be between 0 and 365"
  ]
}
```

---

## Billing Settings

Manage billing configuration for individual leases.

### Get Billing Settings

Retrieves billing settings for a specific lease.

**Endpoint**: `GET /api/leases/{leaseId}/billing-settings`

**Authorization**: Manager, Administrator, or SuperAdmin

**Path Parameters**:
- `leaseId` (guid, required) - The lease ID

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "leaseId": "660e8400-e29b-41d4-a716-446655440001",
    "billingDay": 1,
    "paymentTermDays": 5,
    "generateInvoiceAutomatically": true,
    "prorationMethod": 1,
    "invoicePrefix": "INV",
    "paymentInstructions": "Please pay via bank transfer to Account #12345",
    "notes": "Standard billing configuration",
    "rowVersion": "AAAAAAAAB9E="
  }
}
```

**Errors**:
- `404 Not Found` - Lease not found or has no billing settings

---

### Update Billing Settings

Updates billing settings for a lease.

**Endpoint**: `PUT /api/leases/{leaseId}/billing-settings`

**Authorization**: Manager, Administrator, or SuperAdmin

**Path Parameters**:
- `leaseId` (guid, required) - The lease ID

**Request Body**:
```json
{
  "billingDay": 1,
  "paymentTermDays": 5,
  "generateInvoiceAutomatically": true,
  "prorationMethod": 1,
  "invoicePrefix": "INV",
  "paymentInstructions": "Please pay via bank transfer to Account #12345",
  "notes": "Updated billing configuration",
  "rowVersion": "AAAAAAAAB9E="
}
```

**Validation Rules**:
- `billingDay`: 1-28 (required)
- `paymentTermDays`: 0-365 (required)
- `prorationMethod`: 1 (ActualDaysInMonth) or 2 (ThirtyDayMonth) (required)
- `invoicePrefix`: Max 50 characters
- `paymentInstructions`: Max 1000 characters
- `notes`: Max 2000 characters
- `rowVersion`: Required for concurrency control

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "billingDay": 1,
    "paymentTermDays": 5,
    // ... other fields with updated values
  },
  "message": "Billing settings updated successfully"
}
```

**Errors**:
- `400 Bad Request` - Validation failed
- `404 Not Found` - Lease not found
- `409 Conflict` - Concurrency conflict (rowVersion mismatch)

---

## Recurring Charges

Manage recurring charges for leases.

### List Recurring Charges

Retrieves all recurring charges for a lease.

**Endpoint**: `GET /api/leases/{leaseId}/recurring-charges`

**Authorization**: Manager, Administrator, or SuperAdmin

**Path Parameters**:
- `leaseId` (guid, required) - The lease ID

**Query Parameters**:
- `includeInactive` (bool, optional) - Include inactive charges (default: false)

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "id": "770e8400-e29b-41d4-a716-446655440002",
      "leaseId": "660e8400-e29b-41d4-a716-446655440001",
      "chargeTypeId": "880e8400-e29b-41d4-a716-446655440003",
      "chargeTypeName": "Monthly Rent",
      "description": "Rent for Unit A-101",
      "amount": 15000.00,
      "frequency": 2,
      "startDate": "2026-01-01",
      "endDate": null,
      "isActive": true,
      "notes": "Standard monthly rent",
      "rowVersion": "AAAAAAAAB9F="
    }
  ]
}
```

---

### Create Recurring Charge

Creates a new recurring charge for a lease.

**Endpoint**: `POST /api/leases/{leaseId}/recurring-charges`

**Authorization**: Manager, Administrator, or SuperAdmin

**Path Parameters**:
- `leaseId` (guid, required) - The lease ID

**Request Body**:
```json
{
  "chargeTypeId": "880e8400-e29b-41d4-a716-446655440003",
  "description": "Maintenance Fee - Unit A-101",
  "amount": 2000.00,
  "frequency": 2,
  "startDate": "2026-01-01",
  "endDate": null,
  "isActive": true,
  "notes": "Monthly maintenance charge"
}
```

**Validation Rules**:
- `chargeTypeId`: Required, must exist
- `description`: Required, max 500 characters
- `amount`: Required, must be > 0
- `frequency`: Required, 1-4 (1=OneTime, 2=Monthly, 3=Quarterly, 4=Yearly)
- `startDate`: Required
- `endDate`: Optional, must be after startDate
- `notes`: Optional, max 2000 characters

**Response** (201 Created):
```json
{
  "success": true,
  "data": {
    "id": "770e8400-e29b-41d4-a716-446655440004",
    "leaseId": "660e8400-e29b-41d4-a716-446655440001",
    // ... other fields
  },
  "message": "Recurring charge created successfully"
}
```

**Errors**:
- `400 Bad Request` - Validation failed
- `404 Not Found` - Lease or charge type not found

---

### Update Recurring Charge

Updates an existing recurring charge.

**Endpoint**: `PUT /api/recurring-charges/{id}`

**Authorization**: Manager, Administrator, or SuperAdmin

**Path Parameters**:
- `id` (guid, required) - The recurring charge ID

**Request Body**:
```json
{
  "description": "Updated Maintenance Fee - Unit A-101",
  "amount": 2500.00,
  "frequency": 2,
  "startDate": "2026-01-01",
  "endDate": "2026-12-31",
  "isActive": true,
  "notes": "Increased maintenance charge",
  "rowVersion": "AAAAAAAAB9F="
}
```

**Note**: `chargeTypeId` cannot be changed. Create a new charge instead.

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "770e8400-e29b-41d4-a716-446655440004",
    // ... updated fields
  },
  "message": "Recurring charge updated successfully"
}
```

**Errors**:
- `400 Bad Request` - Validation failed
- `404 Not Found` - Recurring charge not found
- `409 Conflict` - Concurrency conflict

---

### Delete Recurring Charge

Deletes (soft delete) a recurring charge.

**Endpoint**: `DELETE /api/recurring-charges/{id}`

**Authorization**: Manager, Administrator, or SuperAdmin

**Path Parameters**:
- `id` (guid, required) - The recurring charge ID

**Response** (200 OK):
```json
{
  "success": true,
  "message": "Recurring charge deleted successfully"
}
```

**Errors**:
- `404 Not Found` - Recurring charge not found
- `400 Bad Request` - Cannot delete charge referenced in issued invoices

---

## Charge Types

Retrieve available charge types.

### List Charge Types

Retrieves all active charge types for an organization.

**Endpoint**: `GET /api/charge-types`

**Authorization**: Manager, Administrator, or SuperAdmin

**Query Parameters**:
- `orgId` (guid, optional) - Filter by organization (includes system-defined + org-specific)

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "id": "880e8400-e29b-41d4-a716-446655440003",
      "orgId": null,
      "code": 1,
      "name": "Monthly Rent",
      "description": "Monthly rent charge",
      "isActive": true,
      "isSystemDefined": true,
      "isTaxable": false,
      "defaultAmount": null
    },
    {
      "id": "880e8400-e29b-41d4-a716-446655440004",
      "orgId": null,
      "code": 2,
      "name": "Maintenance",
      "description": "Maintenance/association charge",
      "isActive": true,
      "isSystemDefined": true,
      "isTaxable": true,
      "defaultAmount": null
    }
  ]
}
```

**Charge Type Codes**:
- `1` - RENT
- `2` - MAINT (Maintenance)
- `3` - ELEC (Electricity)
- `4` - WATER
- `5` - GAS
- `6` - LATE_FEE
- `7` - ADJUSTMENT

---

## Utility Statements

Record and manage utility consumption and billing.

### List Utility Statements

Retrieves utility statements for a unit or lease.

**Endpoint**: `GET /api/units/{unitId}/utilities`

**Authorization**: Manager, Administrator, or SuperAdmin

**Path Parameters**:
- `unitId` (guid, required) - The unit ID

**Query Parameters**:
- `utilityType` (int, optional) - Filter by utility type (1=Electricity, 2=Water, 3=Gas)
- `fromDate` (date, optional) - Filter statements from this date
- `toDate` (date, optional) - Filter statements to this date

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "id": "990e8400-e29b-41d4-a716-446655440005",
      "leaseId": "660e8400-e29b-41d4-a716-446655440001",
      "utilityType": 1,
      "billingPeriodStart": "2026-01-01",
      "billingPeriodEnd": "2026-01-31",
      "isMeterBased": true,
      "utilityRatePlanId": "aa0e8400-e29b-41d4-a716-446655440006",
      "previousReading": 1000.00,
      "currentReading": 1250.00,
      "unitsConsumed": 250.00,
      "calculatedAmount": 950.00,
      "directBillAmount": null,
      "totalAmount": 950.00,
      "notes": "January electricity consumption",
      "invoiceLineId": null,
      "version": 1,
      "isFinal": true,
      "rowVersion": "AAAAAAAAB9G="
    }
  ]
}
```

---

### Create Utility Statement

Creates a new utility statement.

**Endpoint**: `POST /api/units/{unitId}/utilities`

**Authorization**: Manager, Administrator, or SuperAdmin

**Request Body (Meter-Based)**:
```json
{
  "leaseId": "660e8400-e29b-41d4-a716-446655440001",
  "utilityType": 1,
  "billingPeriodStart": "2026-01-01",
  "billingPeriodEnd": "2026-01-31",
  "isMeterBased": true,
  "utilityRatePlanId": "aa0e8400-e29b-41d4-a716-446655440006",
  "previousReading": 1000.00,
  "currentReading": 1250.00,
  "notes": "January electricity consumption"
}
```

**Request Body (Amount-Based)**:
```json
{
  "leaseId": "660e8400-e29b-41d4-a716-446655440001",
  "utilityType": 1,
  "billingPeriodStart": "2026-01-01",
  "billingPeriodEnd": "2026-01-31",
  "isMeterBased": false,
  "directBillAmount": 1200.00,
  "notes": "January electricity bill from provider"
}
```

**Validation Rules**:
- `leaseId`: Required
- `utilityType`: Required (1-3)
- `billingPeriodStart`: Required
- `billingPeriodEnd`: Required, must be after start
- For meter-based:
  - `utilityRatePlanId`: Required
  - `previousReading`: Required, >= 0
  - `currentReading`: Required, >= previousReading
- For amount-based:
  - `directBillAmount`: Required, > 0

**Response** (201 Created):
```json
{
  "success": true,
  "data": {
    "id": "990e8400-e29b-41d4-a716-446655440007",
    // ... calculated fields
  },
  "message": "Utility statement created successfully"
}
```

---

### Finalize Utility Statement

Marks a utility statement as final (prevents further edits).

**Endpoint**: `POST /api/utilities/{id}/finalize`

**Authorization**: Manager, Administrator, or SuperAdmin

**Path Parameters**:
- `id` (guid, required) - The utility statement ID

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "990e8400-e29b-41d4-a716-446655440007",
    "isFinal": true,
    // ... other fields
  },
  "message": "Utility statement finalized"
}
```

**Errors**:
- `400 Bad Request` - Statement already finalized or already billed

---

## Invoices

Generate, manage, and retrieve invoices.

### Generate Invoice

Generates a draft invoice for a lease.

**Endpoint**: `POST /api/leases/{leaseId}/invoices/generate`

**Authorization**: Manager, Administrator, or SuperAdmin

**Path Parameters**:
- `leaseId` (guid, required) - The lease ID

**Query Parameters**:
- `periodStart` (date, optional) - Billing period start (defaults to current month)
- `periodEnd` (date, optional) - Billing period end (defaults to current month)

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "bb0e8400-e29b-41d4-a716-446655440008",
    "orgId": "cc0e8400-e29b-41d4-a716-446655440009",
    "leaseId": "660e8400-e29b-41d4-a716-446655440001",
    "invoiceNumber": "INV-202601-000042",
    "invoiceDate": "2026-01-01",
    "dueDate": "2026-01-05",
    "status": 1,
    "billingPeriodStart": "2026-01-01",
    "billingPeriodEnd": "2026-01-31",
    "subTotal": 17000.00,
    "taxAmount": 0.00,
    "totalAmount": 17000.00,
    "paidAmount": 0.00,
    "balanceAmount": 17000.00,
    "issuedAtUtc": null,
    "paidAtUtc": null,
    "notes": null,
    "paymentInstructions": "Please pay via bank transfer",
    "lines": [
      {
        "id": "dd0e8400-e29b-41d4-a716-446655440010",
        "invoiceId": "bb0e8400-e29b-41d4-a716-446655440008",
        "chargeTypeId": "880e8400-e29b-41d4-a716-446655440003",
        "lineNumber": 1,
        "description": "Monthly Rent - January 2026",
        "quantity": 1.00,
        "unitPrice": 15000.00,
        "amount": 15000.00,
        "taxRate": 0.00,
        "taxAmount": 0.00,
        "totalAmount": 15000.00,
        "notes": null,
        "source": "Rent",
        "sourceRefId": "ee0e8400-e29b-41d4-a716-446655440011"
      }
    ]
  },
  "message": "Invoice generated successfully"
}
```

**Idempotency**: If a draft invoice already exists for the same lease and period, it will be updated instead of creating a duplicate.

**Errors**:
- `400 Bad Request` - Lease inactive or missing billing settings
- `404 Not Found` - Lease not found

---

### List Invoices

Retrieves invoices with optional filtering.

**Endpoint**: `GET /api/invoices`

**Authorization**: Manager, Administrator, or SuperAdmin

**Query Parameters**:
- `orgId` (guid, required) - Organization ID
- `status` (int, optional) - Filter by status (1=Draft, 2=Issued, 3=PartiallyPaid, 4=Paid, 5=Overdue, 6=Cancelled)
- `fromDate` (date, optional) - Filter from invoice date
- `toDate` (date, optional) - Filter to invoice date

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "id": "bb0e8400-e29b-41d4-a716-446655440008",
      "invoiceNumber": "INV-202601-000042",
      "invoiceDate": "2026-01-01",
      "dueDate": "2026-01-05",
      "status": 2,
      "totalAmount": 17000.00,
      "paidAmount": 0.00,
      "balanceAmount": 17000.00,
      // ... other fields
    }
  ]
}
```

---

### Get Invoice

Retrieves a specific invoice with line items.

**Endpoint**: `GET /api/invoices/{id}`

**Authorization**: Manager, Administrator, or SuperAdmin

**Path Parameters**:
- `id` (guid, required) - The invoice ID

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "bb0e8400-e29b-41d4-a716-446655440008",
    "invoiceNumber": "INV-202601-000042",
    // ... full invoice with lines (see Generate Invoice response)
  }
}
```

**Errors**:
- `404 Not Found` - Invoice not found

---

### Issue Invoice

Finalizes a draft invoice (changes status to Issued).

**Endpoint**: `POST /api/invoices/{id}/issue`

**Authorization**: Manager, Administrator, or SuperAdmin

**Path Parameters**:
- `id` (guid, required) - The invoice ID

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "bb0e8400-e29b-41d4-a716-446655440008",
    "status": 2,
    "issuedAtUtc": "2026-01-01T10:30:00Z",
    // ... other fields
  },
  "message": "Invoice issued successfully"
}
```

**Errors**:
- `400 Bad Request` - Invoice not in Draft status, or has no lines, or total is zero
- `404 Not Found` - Invoice not found

---

### Void Invoice

Cancels an issued invoice.

**Endpoint**: `POST /api/invoices/{id}/void`

**Authorization**: Manager, Administrator, or SuperAdmin

**Path Parameters**:
- `id` (guid, required) - The invoice ID

**Request Body**:
```json
{
  "reason": "Invoice issued in error - duplicate"
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "bb0e8400-e29b-41d4-a716-446655440008",
    "status": 6,
    "voidedAtUtc": "2026-01-01T11:00:00Z",
    "voidReason": "Invoice issued in error - duplicate",
    // ... other fields
  },
  "message": "Invoice voided successfully"
}
```

**Errors**:
- `400 Bad Request` - Invoice not issued, or already paid/partially paid
- `404 Not Found` - Invoice not found

---

## Invoice Runs

Execute batch invoice generation.

### Execute Monthly Rent Run

Generates invoices for all active leases in an organization.

**Endpoint**: `POST /api/invoice-runs/monthly`

**Authorization**: Manager, Administrator, or SuperAdmin

**Request Body**:
```json
{
  "orgId": "cc0e8400-e29b-41d4-a716-446655440009",
  "billingPeriodStart": "2026-02-01",
  "billingPeriodEnd": "2026-02-28",
  "prorationMethod": 1,
  "notes": "February 2026 monthly billing"
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "ff0e8400-e29b-41d4-a716-446655440012",
    "orgId": "cc0e8400-e29b-41d4-a716-446655440009",
    "runNumber": "RUN-202601-001",
    "billingPeriodStart": "2026-02-01",
    "billingPeriodEnd": "2026-02-28",
    "status": 3,
    "startedAtUtc": "2026-01-26T02:00:00Z",
    "completedAtUtc": "2026-01-26T02:05:00Z",
    "totalLeases": 50,
    "successCount": 48,
    "failureCount": 2,
    "errorMessage": null,
    "notes": "February 2026 monthly billing"
  },
  "message": "Invoice run completed successfully"
}
```

**Run Statuses**:
- `1` - Pending
- `2` - InProgress
- `3` - Completed
- `4` - CompletedWithErrors
- `5` - Failed
- `6` - Cancelled

---

### List Invoice Runs

Retrieves invoice run history.

**Endpoint**: `GET /api/invoice-runs`

**Authorization**: Manager, Administrator, or SuperAdmin

**Query Parameters**:
- `orgId` (guid, required) - Organization ID
- `status` (int, optional) - Filter by status
- `fromDate` (date, optional) - Filter from start date
- `toDate` (date, optional) - Filter to start date

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "id": "ff0e8400-e29b-41d4-a716-446655440012",
      "runNumber": "RUN-202601-001",
      "status": 3,
      "totalLeases": 50,
      "successCount": 48,
      "failureCount": 2,
      // ... other fields
    }
  ]
}
```

---

### Get Invoice Run Details

Retrieves details of a specific invoice run including per-lease results.

**Endpoint**: `GET /api/invoice-runs/{id}`

**Authorization**: Manager, Administrator, or SuperAdmin

**Path Parameters**:
- `id` (guid, required) - The invoice run ID

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "ff0e8400-e29b-41d4-a716-446655440012",
    "runNumber": "RUN-202601-001",
    "status": 3,
    "totalLeases": 50,
    "successCount": 48,
    "failureCount": 2,
    "items": [
      {
        "id": "110e8400-e29b-41d4-a716-446655440013",
        "invoiceRunId": "ff0e8400-e29b-41d4-a716-446655440012",
        "leaseId": "660e8400-e29b-41d4-a716-446655440001",
        "invoiceId": "bb0e8400-e29b-41d4-a716-446655440008",
        "isSuccess": true,
        "errorMessage": null,
        "processedAtUtc": "2026-01-26T02:01:00Z"
      },
      {
        "id": "120e8400-e29b-41d4-a716-446655440014",
        "invoiceRunId": "ff0e8400-e29b-41d4-a716-446655440012",
        "leaseId": "660e8400-e29b-41d4-a716-446655440002",
        "invoiceId": null,
        "isSuccess": false,
        "errorMessage": "Missing billing settings",
        "processedAtUtc": "2026-01-26T02:01:30Z"
      }
    ]
  }
}
```

---

## Credit Notes

Issue credits for refunds and adjustments.

### Create Credit Note

Creates a credit note for an invoice.

**Endpoint**: `POST /api/credit-notes`

**Authorization**: Manager, Administrator, or SuperAdmin

**Request Body**:
```json
{
  "invoiceId": "bb0e8400-e29b-41d4-a716-446655440008",
  "reason": 1,
  "notes": "Maintenance charge was incorrect",
  "lineItems": [
    {
      "invoiceLineId": "dd0e8400-e29b-41d4-a716-446655440010",
      "description": "Credit for maintenance overcharge",
      "amount": 500.00,
      "taxAmount": 0.00,
      "totalAmount": 500.00,
      "notes": null
    }
  ]
}
```

**Credit Note Reasons**:
- `1` - InvoiceError
- `2` - Discount
- `3` - Refund
- `4` - Goodwill
- `5` - Adjustment
- `99` - Other

**Response** (201 Created):
```json
{
  "success": true,
  "data": {
    "id": "130e8400-e29b-41d4-a716-446655440015",
    "orgId": "cc0e8400-e29b-41d4-a716-446655440009",
    "invoiceId": "bb0e8400-e29b-41d4-a716-446655440008",
    "creditNoteNumber": "CN-202601-000008",
    "creditNoteDate": "2026-01-01",
    "reason": 1,
    "totalAmount": 500.00,
    "notes": "Maintenance charge was incorrect",
    "appliedAtUtc": null,
    "lines": [
      {
        "id": "140e8400-e29b-41d4-a716-446655440016",
        "creditNoteId": "130e8400-e29b-41d4-a716-446655440015",
        "invoiceLineId": "dd0e8400-e29b-41d4-a716-446655440010",
        "lineNumber": 1,
        "description": "Credit for maintenance overcharge",
        "amount": 500.00,
        "taxAmount": 0.00,
        "totalAmount": 500.00
      }
    ]
  },
  "message": "Credit note created successfully"
}
```

**Errors**:
- `400 Bad Request` - Invoice is draft or voided, or credit amount exceeds balance
- `404 Not Found` - Invoice not found

---

### Issue Credit Note

Finalizes a credit note (applies it to the invoice).

**Endpoint**: `POST /api/credit-notes/{id}/issue`

**Authorization**: Manager, Administrator, or SuperAdmin

**Path Parameters**:
- `id` (guid, required) - The credit note ID

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "130e8400-e29b-41d4-a716-446655440015",
    "appliedAtUtc": "2026-01-01T14:00:00Z",
    // ... other fields
  },
  "message": "Credit note issued successfully"
}
```

**Effect**: Reduces the invoice `balanceAmount` by the credit note total.

---

### Get Credit Note

Retrieves a specific credit note.

**Endpoint**: `GET /api/credit-notes/{id}`

**Authorization**: Manager, Administrator, or SuperAdmin

**Path Parameters**:
- `id` (guid, required) - The credit note ID

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "130e8400-e29b-41d4-a716-446655440015",
    "creditNoteNumber": "CN-202601-000008",
    // ... full credit note with lines
  }
}
```

---

### Void Credit Note

Cancels an issued credit note (future feature).

**Endpoint**: `POST /api/credit-notes/{id}/void`

**Authorization**: Manager, Administrator, or SuperAdmin

**Status**: Not yet implemented

---

## Error Handling

### HTTP Status Codes

- `200 OK` - Request succeeded
- `201 Created` - Resource created successfully
- `400 Bad Request` - Validation failed or invalid request
- `401 Unauthorized` - Missing or invalid authentication token
- `403 Forbidden` - User lacks required permissions
- `404 Not Found` - Resource not found
- `409 Conflict` - Concurrency conflict (rowVersion mismatch)
- `500 Internal Server Error` - Unexpected server error

### Common Error Scenarios

#### Validation Errors

```json
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    "Billing day must be between 1 and 28",
    "Amount must be greater than 0"
  ]
}
```

#### Concurrency Conflict

```json
{
  "success": false,
  "message": "Concurrency conflict",
  "errors": [
    "The record has been modified by another user. Please refresh and try again."
  ]
}
```

#### Business Rule Violation

```json
{
  "success": false,
  "message": "Cannot void paid invoice",
  "errors": [
    "Invoice has been paid. Use credit note for refunds instead."
  ]
}
```

---

## Pagination

Currently, most list endpoints return all matching results. Pagination will be added in a future version.

**Planned Pagination Format**:
```json
{
  "success": true,
  "data": {
    "items": [...],
    "pageNumber": 1,
    "pageSize": 25,
    "totalCount": 100,
    "totalPages": 4
  }
}
```

---

## Rate Limiting

Currently, there are no rate limits enforced on API endpoints. This may be added in future versions for production deployments.

---

## Versioning

All billing API endpoints are versioned using API versioning.

**Current Version**: v1.0

**Version Header** (optional):
```http
Api-Version: 1.0
```

**URL Versioning** (future):
```
/api/v1/invoices
/api/v2/invoices
```

---

## Related Documentation

- **[Billing Engine Guide](BILLING_ENGINE.md)** - Database schema and architecture
- **[Billing UI Guide](BILLING_UI_GUIDE.md)** - Frontend component documentation
- **[Billing Business Rules](BILLING_BUSINESS_RULES.md)** - Business rules and timing conventions
- **[Background Jobs](BACKGROUND_JOBS.md)** - Automated billing jobs

---

**Last Updated**: 2026-01-11  
**Version**: 1.0  
**API Version**: v1.0  
**Part of**: TentMan Billing Engine Documentation
