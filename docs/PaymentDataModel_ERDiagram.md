# Payment Data Model - ER Diagram

## Entity Relationship Diagram

This diagram shows the comprehensive payment infrastructure and its relationships with other entities in the system.

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                              ORGANIZATIONS                                    │
│  - Id (PK)                                                                   │
│  - Name, Settings, etc.                                                      │
└────────────────┬─────────────────────────────────────────────────────────────┘
                 │
                 │ 1:N
                 │
    ┌────────────┼─────────────┬──────────────┬──────────────┬──────────────┐
    │            │             │              │              │              │
    │            │             │              │              │              │
┌───▼────┐  ┌───▼────┐   ┌────▼─────┐  ┌────▼─────┐   ┌───▼──────┐  ┌────▼─────┐
│ Leases │  │Invoices│   │ Utility  │  │ Deposit  │   │  Files   │  │ Tenants  │
│        │  │        │   │Statement │  │Transaction│  │(Metadata)│  │          │
└───┬────┘  └───┬────┘   └────┬─────┘  └────┬─────┘   └────┬─────┘  └──────────┘
    │           │             │              │               │
    │ 1:N       │ 1:N         │ 1:N          │ 1:N           │ 1:N
    │           │             │              │               │
    │           │             │              │               │
┌───▼───────────▼─────────────▼──────────────▼───────────────▼──────────────────┐
│                             PAYMENTS (Core Entity)                            │
│  ─────────────────────────────────────────────────────────────────────────   │
│  Identity & References:                                                       │
│  - Id (PK)                                                                    │
│  - OrgId (FK) → Organizations                                                │
│  - InvoiceId (FK) → Invoices                                                 │
│  - LeaseId (FK) → Leases                                                     │
│  - UtilityStatementId (FK, nullable) → UtilityStatements                     │
│  - DepositTransactionId (FK, nullable) → DepositTransactions                 │
│                                                                               │
│  Payment Classification:                                                      │
│  - PaymentType (enum): Rent, Utility, Invoice, Deposit, Maintenance, etc.   │
│  - PaymentMode (enum): Cash, Online, BankTransfer, Cheque, UPI, Other       │
│  - Status (enum): Pending, Completed, Failed, Cancelled, etc.               │
│                                                                               │
│  Payment Details:                                                             │
│  - Amount (decimal 18,2)                                                      │
│  - PaymentDateUtc                                                             │
│  - TransactionReference (bank/UPI/check ref)                                 │
│  - PayerName                                                                  │
│  - ReceivedBy                                                                 │
│  - Notes                                                                      │
│                                                                               │
│  Gateway Integration (for online payments):                                   │
│  - GatewayTransactionId (e.g., "txn_abc123")                                 │
│  - GatewayName (e.g., "Razorpay", "Stripe", "PayPal")                       │
│  - GatewayResponse (full JSON response)                                      │
│  - PaymentMetadata (additional JSON metadata)                                │
│                                                                               │
│  BBPS / International Billing:                                                │
│  - CountryCode (ISO 3166-1 alpha-2: "IN", "US", "UK", etc.)                 │
│  - BillerId (BBPS biller ID or equivalent)                                   │
│  - ConsumerId (customer/consumer ID)                                         │
│                                                                               │
│  Audit Fields:                                                                │
│  - CreatedAtUtc, CreatedBy                                                   │
│  - ModifiedAtUtc, ModifiedBy                                                 │
│  - IsDeleted, DeletedAtUtc, DeletedBy                                        │
│  - RowVersion (concurrency)                                                  │
└───┬─────────────────────────────────────────┬─────────────────────────────┬──┘
    │                                         │                             │
    │ 1:N                                     │ 1:N                         │ 1:1
    │                                         │                             │
┌───▼───────────────────┐         ┌───────────▼─────────────┐    ┌─────────▼────────────┐
│ PaymentStatusHistory  │         │  PaymentAttachments     │    │ PaymentConfirmation  │
│  (Audit Trail)        │         │  (Proof/Receipts)       │    │ Request (optional)   │
│ ───────────────────── │         │ ─────────────────────── │    │ ──────────────────── │
│ - Id (PK)            │         │ - Id (PK)               │    │ - Id (PK)            │
│ - PaymentId (FK)     │         │ - PaymentId (FK)        │    │ - PaymentId (FK)     │
│ - FromStatus         │         │ - FileId (FK) ──────────┼────┼─→ Files             │
│ - ToStatus           │         │ - AttachmentType        │    │ - Amount             │
│ - ChangedAtUtc       │         │ - Description           │    │ - Status             │
│ - ChangedBy          │         │ - DisplayOrder          │    │ - ProofFileId        │
│ - Reason             │         │ - Audit fields          │    │ - etc.               │
│ - Metadata (JSON)    │         └─────────────────────────┘    └──────────────────────┘
│ - Audit fields       │
└──────────────────────┘


┌──────────────────────────────────────────────────────────────────────────────┐
│                            RELATED ENTITIES                                   │
└──────────────────────────────────────────────────────────────────────────────┘

┌─────────────────┐
│    Invoices     │  Already exists with comprehensive support
│ ─────────────── │
│ - InvoiceNumber │
│ - InvoiceDate   │
│ - DueDate       │
│ - SubTotal      │
│ - TaxAmount     │
│ - TotalAmount   │
│ - PaidAmount    │  ◄── Updated from Payments
│ - BalanceAmount │  ◄── Calculated (Total - Paid)
│ - Status        │  ◄── Auto-updated (Issued → PartiallyPaid → Paid)
│ - Lines (1:N)   │
└─────────────────┘

┌────────────────────┐
│ UtilityStatements  │  For utility bill tracking
│ ──────────────────│
│ - LeaseId          │
│ - UtilityType      │  (Electricity, Water, Gas, Internet, etc.)
│ - BillingPeriod    │
│ - TotalAmount      │
│ - IsMeterBased     │
│ - Payments (1:N)   │  ◄── NEW: Links to payments
└────────────────────┘

┌───────────────────┐
│DepositTransactions│  For security deposit tracking
│ ──────────────────│
│ - LeaseId         │
│ - TxnType         │  (Deposit, Refund, Deduction, Adjustment)
│ - Amount          │
│ - TxnDate         │
│ - Payments (1:N)  │  ◄── NEW: Links to payments
└───────────────────┘

┌─────────────────┐
│ Files (Metadata)│  For file storage (Azure Blob, AWS S3, etc.)
│ ─────────────── │
│ - StorageKey    │
│ - FileName      │
│ - ContentType   │
│ - SizeBytes     │
│ - PaymentAttachments (1:N) │  ◄── NEW: For payment proof
└─────────────────┘
```

## Key Relationships

### 1:N Relationships
- **Organization → Payments**: One organization has many payments
- **Invoice → Payments**: One invoice can have multiple payments (partial payments)
- **Lease → Payments**: One lease can have many payments over time
- **UtilityStatement → Payments**: One utility bill can have multiple payments
- **DepositTransaction → Payments**: One deposit transaction can link to multiple payments
- **Payment → PaymentStatusHistory**: One payment has many status change records
- **Payment → PaymentAttachments**: One payment can have multiple proof attachments
- **FileMetadata → PaymentAttachments**: One file can be used in multiple payment attachments

### 1:1 Relationships
- **Payment → PaymentConfirmationRequest**: Optional 1:1 for tenant-initiated confirmations

### Optional (Nullable) Foreign Keys
- **Payment.UtilityStatementId**: Only set for utility payments
- **Payment.DepositTransactionId**: Only set for deposit payments
- **Payment.ProofFileId** (via PaymentConfirmationRequest): Only for confirmed cash payments

## Delete Behaviors

### CASCADE
- Payment → PaymentStatusHistory: Delete payment history when payment is deleted
- Payment → PaymentAttachments: Delete attachments when payment is deleted

### RESTRICT
- Organization → Payments: Cannot delete organization with payments
- Invoice → Payments: Cannot delete invoice with payments
- Lease → Payments: Cannot delete lease with payments
- FileMetadata → PaymentAttachments: Cannot delete file used in attachments

### SET NULL
- UtilityStatement → Payments: Deleting utility statement nullifies reference
- DepositTransaction → Payments: Deleting deposit transaction nullifies reference

## Indexes for Performance

### Single Column Indexes
- OrgId (for organization-wide queries)
- InvoiceId (for invoice payment lookups)
- LeaseId (for lease payment history)
- PaymentDateUtc (for date-range queries)
- TransactionReference (for transaction lookups)
- GatewayTransactionId (for gateway reconciliation)
- PaymentType (for payment type filtering)

### Composite Indexes
- (OrgId, PaymentType, PaymentDateUtc): Efficient filtering by org, type, and date
- (PaymentId, ChangedAtUtc): Payment status history timeline
- (PaymentId, DisplayOrder): Payment attachments ordering

## Payment Type Use Cases

### Rent Payment (PaymentType.Rent)
- Links to Invoice (rent invoice)
- Links to Lease (lease agreement)
- Can be Cash, UPI, Gateway, etc.

### Utility Payment (PaymentType.Utility)
- Links to UtilityStatement (electricity, water, gas bill)
- Uses BBPS fields (CountryCode, BillerId, ConsumerId)
- Typically Online/UPI via gateway

### Invoice Payment (PaymentType.Invoice)
- Links to Invoice (maintenance, repairs, services)
- Can be any payment mode
- Gateway fields for online payments

### Deposit Payment (PaymentType.Deposit)
- Links to DepositTransaction (security deposit)
- Typically BankTransfer or Cheque
- Refunds also use this type with negative flow

### Maintenance Payment (PaymentType.Maintenance)
- For recurring maintenance charges
- Links to Invoice if invoiced

### Late Fee Payment (PaymentType.LateFee)
- For penalty charges
- Links to Invoice with late fee line items

## Audit Trail Flow

```
Payment Created
    ↓
PaymentStatusHistory (FromStatus: NULL → ToStatus: Pending)
    ↓
Gateway Processing
    ↓
PaymentStatusHistory (FromStatus: Pending → ToStatus: Processing)
    ↓
Gateway Response
    ↓
PaymentStatusHistory (FromStatus: Processing → ToStatus: Completed/Failed)
    ↓
Payment Updated with GatewayResponse
```

## Attachment Flow

```
Payment Created
    ↓
User Uploads Receipt (File → FileMetadata)
    ↓
PaymentAttachment Created (links Payment + FileMetadata)
    ↓
AttachmentType: "Receipt"
Description: "Cash payment receipt for Jan 2024"
DisplayOrder: 1
    ↓
Additional Attachments can be added
```

## Schema Evolution

### Phase 1 (Completed) ✅
- Core Payment entity
- Invoice integration
- Payment modes (Cash, Online, etc.)
- Payment confirmation workflow

### Phase 2 (Current Implementation) ✅
- PaymentType enum
- Gateway fields
- BBPS/International support
- PaymentStatusHistory
- PaymentAttachment
- Utility and Deposit linkage

### Phase 3 (Future - Not in Scope)
- Gateway service implementations
- Automated status updates from webhooks
- Payment reconciliation tools
- Analytics and reporting
- Refund processing
- Recurring payment schedules
