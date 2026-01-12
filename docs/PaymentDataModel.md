# Unified Payment Data Model - Schema Documentation

## Overview
This document describes the comprehensive payment infrastructure that supports rent (lease), utility (BBPS), invoice, deposit, and other payment types with multiple payment modes (Cash, UPI, Gateway, etc.).

## Database Schema

### Core Payment Table

**Table: `Payments`**

The central payment table that handles all payment types.

| Column | Type | Description | Constraints |
|--------|------|-------------|-------------|
| Id | GUID | Primary key | PK, Required |
| OrgId | GUID | Organization reference | FK to Organizations, Required, Indexed |
| InvoiceId | GUID | Invoice reference | FK to Invoices, Required, Indexed |
| LeaseId | GUID | Lease reference | FK to Leases, Required, Indexed |
| **PaymentType** | INT (enum) | Type of payment (Rent, Utility, Invoice, Deposit, Maintenance, LateFee, Other) | Required, Indexed, Default: Rent (1) |
| PaymentMode | INT (enum) | Payment method (Cash, Online, BankTransfer, Cheque, UPI, Other) | Required |
| Status | INT (enum) | Payment status (Pending, Completed, Failed, Cancelled, Processing, Refunded, PendingConfirmation, Rejected) | Required |
| Amount | DECIMAL(18,2) | Payment amount | Required |
| PaymentDateUtc | DATETIME2 | Payment date/time | Required, Indexed |
| TransactionReference | NVARCHAR(200) | Bank/UPI/Check reference | Optional, Indexed |
| **GatewayTransactionId** | NVARCHAR(200) | Gateway transaction ID | Optional, Indexed |
| **GatewayName** | NVARCHAR(100) | Payment gateway name (Razorpay, Stripe, PayPal) | Optional |
| **GatewayResponse** | NVARCHAR(MAX) | Full gateway response (JSON) | Optional |
| ReceivedBy | NVARCHAR(100) | User ID who recorded payment | Required |
| PayerName | NVARCHAR(200) | Name of payer | Optional |
| Notes | NVARCHAR(2000) | Payment notes | Optional |
| PaymentMetadata | NVARCHAR(MAX) | Additional metadata (JSON) | Optional |
| **UtilityStatementId** | GUID | Reference to utility bill (for utility payments) | FK to UtilityStatements, Optional |
| **DepositTransactionId** | GUID | Reference to deposit transaction | FK to DepositTransactions, Optional |
| **CountryCode** | NVARCHAR(2) | ISO 3166-1 alpha-2 country code (e.g., "IN", "US") | Optional |
| **BillerId** | NVARCHAR(100) | BBPS/utility biller ID | Optional |
| **ConsumerId** | NVARCHAR(100) | BBPS/utility consumer ID | Optional |
| CreatedAtUtc | DATETIME2 | Created timestamp | Required |
| CreatedBy | NVARCHAR(MAX) | Created by user | Optional |
| ModifiedAtUtc | DATETIME2 | Last modified timestamp | Optional |
| ModifiedBy | NVARCHAR(MAX) | Last modified by user | Optional |
| IsDeleted | BIT | Soft delete flag | Required, Default: false |
| DeletedAtUtc | DATETIME2 | Deleted timestamp | Optional |
| DeletedBy | NVARCHAR(MAX) | Deleted by user | Optional |
| RowVersion | ROWVERSION | Concurrency token | Required |

**Indexes:**
- IX_Payments_OrgId (OrgId)
- IX_Payments_InvoiceId (InvoiceId)
- IX_Payments_LeaseId (LeaseId)
- IX_Payments_PaymentDateUtc (PaymentDateUtc)
- IX_Payments_TransactionReference (TransactionReference)
- IX_Payments_GatewayTransactionId (GatewayTransactionId)
- IX_Payments_PaymentType (PaymentType)
- IX_Payments_OrgId_PaymentType_PaymentDateUtc (OrgId, PaymentType, PaymentDateUtc) - Composite for efficient filtering

**Foreign Keys:**
- OrgId → Organizations(Id) ON DELETE RESTRICT
- InvoiceId → Invoices(Id) ON DELETE RESTRICT
- LeaseId → Leases(Id) ON DELETE RESTRICT
- UtilityStatementId → UtilityStatements(Id) ON DELETE SET NULL
- DepositTransactionId → DepositTransactions(Id) ON DELETE SET NULL

---

### Payment Status History (Audit Trail)

**Table: `PaymentStatusHistory`**

Tracks all status changes for payments, providing a complete audit trail.

| Column | Type | Description | Constraints |
|--------|------|-------------|-------------|
| Id | GUID | Primary key | PK, Required |
| PaymentId | GUID | Reference to payment | FK to Payments, Required, Indexed |
| FromStatus | INT (enum) | Previous status | Required |
| ToStatus | INT (enum) | New status | Required |
| ChangedAtUtc | DATETIME2 | When status changed | Required, Indexed |
| ChangedBy | NVARCHAR(100) | User who changed status | Required |
| Reason | NVARCHAR(2000) | Reason for change | Optional |
| Metadata | NVARCHAR(MAX) | Additional metadata (JSON) | Optional |
| CreatedAtUtc | DATETIME2 | Created timestamp | Required |
| CreatedBy | NVARCHAR(MAX) | Created by user | Optional |
| ModifiedAtUtc | DATETIME2 | Last modified timestamp | Optional |
| ModifiedBy | NVARCHAR(MAX) | Last modified by user | Optional |
| IsDeleted | BIT | Soft delete flag | Required, Default: false |
| DeletedAtUtc | DATETIME2 | Deleted timestamp | Optional |
| DeletedBy | NVARCHAR(MAX) | Deleted by user | Optional |
| RowVersion | ROWVERSION | Concurrency token | Required |

**Indexes:**
- IX_PaymentStatusHistory_PaymentId (PaymentId)
- IX_PaymentStatusHistory_ChangedAtUtc (ChangedAtUtc)
- IX_PaymentStatusHistory_PaymentId_ChangedAtUtc (PaymentId, ChangedAtUtc) - Composite

**Foreign Keys:**
- PaymentId → Payments(Id) ON DELETE CASCADE

---

### Payment Attachments

**Table: `PaymentAttachments`**

Links payment proof files (receipts, screenshots, documents) to payments.

| Column | Type | Description | Constraints |
|--------|------|-------------|-------------|
| Id | GUID | Primary key | PK, Required |
| PaymentId | GUID | Reference to payment | FK to Payments, Required, Indexed |
| FileId | GUID | Reference to file metadata | FK to Files, Required, Indexed |
| AttachmentType | NVARCHAR(100) | Type/category (Receipt, Screenshot, Statement) | Required |
| Description | NVARCHAR(500) | Description of attachment | Optional |
| DisplayOrder | INT | Display order for UI | Required, Default: 0 |
| CreatedAtUtc | DATETIME2 | Created timestamp | Required |
| CreatedBy | NVARCHAR(MAX) | Created by user | Optional |
| ModifiedAtUtc | DATETIME2 | Last modified timestamp | Optional |
| ModifiedBy | NVARCHAR(MAX) | Last modified by user | Optional |
| IsDeleted | BIT | Soft delete flag | Required, Default: false |
| DeletedAtUtc | DATETIME2 | Deleted timestamp | Optional |
| DeletedBy | NVARCHAR(MAX) | Deleted by user | Optional |
| RowVersion | ROWVERSION | Concurrency token | Required |

**Indexes:**
- IX_PaymentAttachments_PaymentId (PaymentId)
- IX_PaymentAttachments_FileId (FileId)
- IX_PaymentAttachments_PaymentId_DisplayOrder (PaymentId, DisplayOrder) - Composite

**Foreign Keys:**
- PaymentId → Payments(Id) ON DELETE CASCADE
- FileId → Files(Id) ON DELETE RESTRICT

---

### Supporting Tables (Already Existing)

**Table: `Invoices`**

Already has comprehensive support for:
- InvoiceNumber, InvoiceDate, DueDate
- SubTotal, TaxAmount, TotalAmount
- **PaidAmount** - Updated from payments
- **BalanceAmount** - Calculated (TotalAmount - PaidAmount)
- Status (Draft, Issued, PartiallyPaid, Paid, Overdue, Voided, Cancelled)

**Table: `UtilityStatements`**

Already exists for tracking utility bills:
- LeaseId, UtilityType (Electricity, Water, Gas, Internet, etc.)
- BillingPeriodStart, BillingPeriodEnd
- TotalAmount, Notes
- Now has **Payments** navigation property

**Table: `DepositTransactions`**

Already exists for tracking security deposits:
- LeaseId, TxnType (Deposit, Refund, Deduction, Adjustment)
- Amount, TxnDate, Reference, Notes
- Now has **Payments** navigation property

**Table: `Files` (FileMetadata)**

Already exists for file storage:
- OrgId, StorageProvider (Azure, AWS, Local)
- StorageKey, FileName, ContentType, SizeBytes
- Now has **PaymentAttachments** navigation property

---

## Entity Relationships (ER Diagram)

```
┌─────────────────┐
│  Organizations  │
└────────┬────────┘
         │
         │ 1:N
         │
┌────────▼────────────────────────────────────────────────────────┐
│                          Payments                               │
│  - PaymentType (Rent, Utility, Invoice, Deposit, etc.)         │
│  - PaymentMode (Cash, UPI, Gateway, etc.)                      │
│  - GatewayTransactionId, GatewayName, GatewayResponse          │
│  - UtilityStatementId (optional)                               │
│  - DepositTransactionId (optional)                             │
│  - CountryCode, BillerId, ConsumerId (for BBPS)               │
└──┬───────┬───────┬────────────────┬────────────────┬────────────┘
   │       │       │                │                │
   │ N:1   │ N:1   │ N:1            │ 1:N            │ 1:N
   │       │       │                │                │
┌──▼──┐ ┌──▼──┐ ┌──▼─────────┐  ┌──▼────────────┐ ┌─▼──────────────┐
│Lease│ │Inv. │ │Utility     │  │PaymentStatus  │ │Payment         │
│     │ │     │ │Statement   │  │History        │ │Attachments     │
└─────┘ └──┬──┘ │(optional)  │  │               │ └────┬───────────┘
           │    └────────────┘  │- FromStatus   │      │
           │                    │- ToStatus     │      │ N:1
           │                    │- ChangedAtUtc │      │
           │ 1:N                │- ChangedBy    │  ┌───▼────────┐
           │                    │- Reason       │  │FileMetadata│
      ┌────▼─────────┐         └───────────────┘  │(Files)     │
      │InvoiceLines  │                             └────────────┘
      └──────────────┘
      
┌────────────────────┐
│DepositTransaction  │
│(optional linkage)  │
└────────────────────┘
```

---

## Enums

### PaymentType
```csharp
public enum PaymentType
{
    Rent = 1,          // Rent payment for lease
    Utility = 2,       // Utility bills (BBPS/international)
    Invoice = 3,       // General invoices
    Deposit = 4,       // Security deposit payments/refunds
    Maintenance = 5,   // Maintenance charges
    LateFee = 6,       // Late fees/penalties
    Other = 99         // Other payment types
}
```

### PaymentMode
```csharp
public enum PaymentMode
{
    Cash = 1,          // Cash payment
    Online = 2,        // Online gateway payment
    BankTransfer = 3,  // Bank transfer/NEFT/RTGS
    Cheque = 4,        // Cheque payment
    UPI = 5,           // UPI payment
    Other = 99         // Other payment methods
}
```

### PaymentStatus
```csharp
public enum PaymentStatus
{
    Pending = 1,              // Payment pending
    Completed = 2,            // Payment completed
    Failed = 3,               // Payment failed
    Cancelled = 4,            // Payment cancelled
    Processing = 5,           // Payment being processed
    Refunded = 6,             // Payment refunded
    PendingConfirmation = 7,  // Cash payment awaiting confirmation
    Rejected = 8              // Cash payment rejected
}
```

---

## Use Cases

### 1. Rent Payment (Cash)
- PaymentType = Rent
- PaymentMode = Cash
- InvoiceId, LeaseId populated
- No gateway fields
- Can attach receipt via PaymentAttachments

### 2. Utility Payment (BBPS - India)
- PaymentType = Utility
- PaymentMode = Online/UPI
- UtilityStatementId populated
- CountryCode = "IN"
- BillerId, ConsumerId populated (BBPS IDs)
- GatewayTransactionId, GatewayName populated

### 3. Deposit Refund
- PaymentType = Deposit
- DepositTransactionId populated
- PaymentMode = BankTransfer
- TransactionReference = bank reference

### 4. Invoice Payment via Gateway
- PaymentType = Invoice
- PaymentMode = Online
- GatewayTransactionId, GatewayName, GatewayResponse populated
- Status history tracks: Pending → Processing → Completed

### 5. International Utility Payment
- PaymentType = Utility
- CountryCode = "US" (or other)
- BillerId, ConsumerId for local billing system
- Gateway fields for online payment

---

## Backward Compatibility

All changes are **backward compatible**:
- PaymentType defaults to `Rent` for existing payments
- New fields are nullable or have defaults
- Existing queries continue to work
- Migration is non-breaking

---

## Scalability & Future Considerations

### International BBPS Support
- CountryCode field allows for country-specific billing systems
- BillerId/ConsumerId are flexible string fields
- Can store country-specific metadata in PaymentMetadata JSON field

### Gateway Integration
- GatewayName supports multiple providers (Razorpay, Stripe, PayPal, etc.)
- GatewayResponse stores complete response for reconciliation
- GatewayTransactionId enables transaction lookup

### Audit Trail
- PaymentStatusHistory provides complete audit trail
- Tracks all status changes with who, when, why
- Metadata field stores additional context (error details, gateway responses)

### Attachments
- PaymentAttachments separates concerns from PaymentConfirmationRequest
- Supports multiple files per payment
- DisplayOrder enables UI ordering
- AttachmentType enables categorization

---

## Migration

**Migration Name:** `20260112020003_AddUnifiedPaymentModel`

**Changes:**
1. Add new columns to Payments table
2. Create PaymentStatusHistory table
3. Create PaymentAttachments table
4. Add indexes for performance
5. Add foreign keys with appropriate delete behaviors

**Rollback:** Migration includes complete Down() method for safe rollback.

---

## Files Created/Modified

### New Files:
1. `TentMan.Contracts/Enums/PaymentType.cs` - Payment type enum
2. `TentMan.Domain/Entities/PaymentStatusHistory.cs` - Status history entity
3. `TentMan.Domain/Entities/PaymentAttachment.cs` - Attachment entity
4. `TentMan.Infrastructure/Persistence/Configurations/PaymentStatusHistoryConfiguration.cs` - EF config
5. `TentMan.Infrastructure/Persistence/Configurations/PaymentAttachmentConfiguration.cs` - EF config
6. `TentMan.Infrastructure/Persistence/Migrations/20260112020003_AddUnifiedPaymentModel.cs` - Migration

### Modified Files:
1. `TentMan.Domain/Entities/Payment.cs` - Added new fields and navigation properties
2. `TentMan.Domain/Entities/FileMetadata.cs` - Added PaymentAttachments navigation
3. `TentMan.Domain/Entities/UtilityStatement.cs` - Added Payments navigation
4. `TentMan.Domain/Entities/DepositTransaction.cs` - Added Payments navigation
5. `TentMan.Infrastructure/Persistence/Configurations/PaymentConfiguration.cs` - Updated config
6. `TentMan.Infrastructure/Persistence/ApplicationDbContext.cs` - Added new DbSets

---

## Summary

This unified payment data model provides:
- ✅ Support for multiple payment types (Rent, Utility, Invoice, Deposit, etc.)
- ✅ Multiple payment modes (Cash, UPI, Gateway, etc.)
- ✅ Gateway integration fields (transaction ID, name, response)
- ✅ BBPS support with international scalability (country code, biller ID, consumer ID)
- ✅ Complete audit trail via PaymentStatusHistory
- ✅ Flexible attachment system via PaymentAttachments
- ✅ Linkage to utility statements and deposit transactions
- ✅ Backward compatible with existing payment infrastructure
- ✅ Comprehensive indexing for performance
- ✅ Soft delete support across all entities
- ✅ Concurrency control via RowVersion
