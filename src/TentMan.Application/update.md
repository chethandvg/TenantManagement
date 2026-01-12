# Payment Module Updates

## Overview
Added support for multiple payment modes (Cash, Online, BankTransfer, UPI, Cheque, etc.) to the billing engine. This enables tracking of rent and invoice payments through various payment methods.

**NEW (2026-01-12):** Added cash payment confirmation workflow allowing tenants to submit payment proofs for owner verification before payment is recorded.

## Changes Made

### New Entities
- **Payment**: Core entity for tracking payments against invoices
  - Fields: PaymentMode, PaymentStatus, Amount, PaymentDate, TransactionReference, ReceivedBy, PayerName, Notes, PaymentMetadata
- **PaymentConfirmationRequest**: Entity for tenant-initiated payment confirmation requests
  - Fields: Amount, PaymentDateUtc, ReceiptNumber, Notes, ProofFileId, Status, ReviewedAtUtc, ReviewedBy, ReviewResponse, PaymentId

### New Enums
- **PaymentMode**: Cash, Online, BankTransfer, Cheque, UPI, Other
- **PaymentStatus**: Pending, Completed, Failed, Cancelled, Processing, Refunded, PendingConfirmation, Rejected
- **PaymentConfirmationStatus**: Pending, Confirmed, Rejected, Cancelled

### Command Handlers
- **RecordCashPaymentCommandHandler**: Records cash payments (immediately marked as completed)
- **RecordOnlinePaymentCommandHandler**: Records online payments (stub for future gateway integration)
- **CreatePaymentConfirmationRequestCommandHandler**: Creates payment confirmation request with optional file proof
- **ConfirmPaymentRequestCommandHandler**: Confirms payment request, creates payment, updates invoice
- **RejectPaymentRequestCommandHandler**: Rejects payment request with response note

### Query Handlers
- **GetInvoicePaymentsQueryHandler**: Retrieves all payments for an invoice
- **GetPendingPaymentConfirmationRequestsQueryHandler**: Gets pending requests for organization
- **GetInvoicePaymentConfirmationRequestsQueryHandler**: Gets all requests for an invoice

### Business Logic
- Payment validation (amount, invoice status, remaining balance)
- Automatic invoice status updates (Issued → PartiallyPaid → Paid)
- Automatic PaidAmount and BalanceAmount calculations
- Concurrency control using RowVersion
- File upload support for payment proofs (receipts, photos)
- Payment confirmation workflow with owner review

## Database Changes
- New table: `Payments`
- New table: `PaymentConfirmationRequests`
- Updated table: `Invoices` (added Payments navigation property)
- Updated table: `FileMetadata` (added PaymentConfirmationRequests navigation property)
- Migration: `20260112005443_AddPaymentTable`
- Migration: `20260112012710_AddPaymentConfirmationRequest`

## API Endpoints
- `POST /api/invoices/{id}/payments/cash` - Record cash payment (owner/manager)
- `POST /api/invoices/{id}/payments/online` - Record online payment (stub)
- `GET /api/invoices/{id}/payments` - Get invoice payments
- `POST /api/invoices/{id}/payment-confirmation-requests` - Create payment confirmation request (tenant)
- `GET /api/organizations/{id}/payment-confirmation-requests/pending` - Get pending requests (owner/manager)
- `GET /api/invoices/{id}/payment-confirmation-requests` - Get invoice requests (owner/manager)
- `POST /api/payment-confirmation-requests/{id}/confirm` - Confirm payment request (owner/manager)
- `POST /api/payment-confirmation-requests/{id}/reject` - Reject payment request (owner/manager)

## Testing
- Unit tests for RecordCashPaymentCommandHandler (5 tests)
- Unit tests for CreatePaymentConfirmationRequestCommandHandler (5 tests)
- Unit tests for ConfirmPaymentRequestCommandHandler (6 tests)
- Unit tests for RejectPaymentRequestCommandHandler (4 tests)
- All tests passing successfully

## Payment Recording and Workflow APIs - January 2026

### New Commands

#### RecordPaymentCommandHandler (Unified)
**File:** `Billing/Commands/RecordPayment/RecordPaymentCommandHandler.cs`

Unified handler for recording payments via `/api/payments` endpoint.
- Supports all payment modes: Cash, Online, UPI, BankTransfer, Cheque, etc.
- Determines payment status based on mode:
  - Cash: Immediately Completed
  - Online with GatewayTransactionId: Pending (awaiting gateway confirmation)
  - Other modes with TransactionReference: Completed
- Validates invoice status, amount, and transaction reference requirements
- Auto-updates invoice when payment is Completed
- Creates payment record with full metadata (gateway info, payer, notes)

#### UploadPaymentAttachmentCommandHandler
**File:** `Billing/Commands/UploadPaymentAttachment/UploadPaymentAttachmentCommandHandler.cs`

Handles receipt/proof file uploads for existing payments.
- Uploads file to Azure Blob Storage ("payment-receipts" container)
- Creates FileMetadata record with storage provider info
- Creates PaymentAttachment link between Payment and File
- Validates file size (max 10 MB) and payment existence
- Returns signed URL for immediate file access (60-minute expiry)

#### ConfirmPaymentCommandHandler
**File:** `Billing/Commands/ConfirmRejectPayment/ConfirmPaymentDirectCommand.cs`

Owner approves pending payment.
- Updates payment status from Pending/PendingConfirmation → Completed
- Creates PaymentStatusHistory audit record
- Auto-updates invoice amounts and status
- Only works on Pending or PendingConfirmation payments

#### RejectPaymentDirectCommandHandler
**File:** `Billing/Commands/ConfirmRejectPayment/RejectPaymentDirectCommand.cs`

Owner rejects pending payment.
- Updates payment status to Rejected
- Requires rejection reason (audit trail)
- Creates PaymentStatusHistory audit record
- Does not update invoice (payment rejected)

### New Queries

#### GetPaymentsWithFiltersQueryHandler
**File:** `Billing/Queries/GetPaymentsWithFilters/GetPaymentsWithFiltersQuery.cs`

Advanced payment filtering and pagination.
- Filters: orgId, leaseId, invoiceId, status, paymentMode, paymentType, date range, payer name, received by
- Pagination: page number, page size (max 100 per page)
- Returns: payments + total count + pagination metadata
- Uses repository GetWithFiltersAsync for efficient querying

#### GetLeasePaymentsQueryHandler
**File:** `Billing/Queries/GetPayments/GetLeasePaymentsQuery.cs`

Retrieves all payments for a specific lease.
- Ordered by payment date (most recent first)
- Maps to PaymentDto for API response
- Uses existing IPaymentRepository.GetByLeaseIdAsync

#### GetPaymentHistoryQueryHandler
**File:** `Billing/Queries/GetPaymentHistory/GetPaymentHistoryQuery.cs`

Retrieves status change history for a payment.
- Returns all PaymentStatusHistory records for a payment
- Ordered by change date (most recent first)
- Maps to PaymentStatusHistoryDto
- Includes who changed status, when, and why

### Background Jobs

#### OverduePaymentDetectionJob
**File:** `BackgroundJobs/OverduePaymentDetectionJob.cs`

Daily job for detecting overdue invoices and updating status.
- Finds Issued or PartiallyPaid invoices past their due date
- Verifies actual payment status against invoice
- Updates invoice status to Overdue if balance remains
- Logs all status changes for audit
- Should be scheduled via Hangfire/Quartz.NET

**Execution:**
- Per organization: `ExecuteAsync(Guid orgId)`
- All organizations: `ExecuteForAllOrganizationsAsync()` (requires scheduler integration)

### Repository Enhancements

Added methods to IPaymentRepository and PaymentRepository:

#### GetWithFiltersAsync
Advanced filtering with pagination support.
- Filters: orgId, leaseId, invoiceId, status, paymentMode, paymentType, dates, names
- Returns tuple: (IEnumerable<Payment>, int totalCount)
- Efficient query building with optional parameters

#### GetStatusHistoryAsync
Retrieves PaymentStatusHistory records for a payment.
- Ordered by ChangedAtUtc descending
- Filters out soft-deleted records

#### AddStatusHistoryAsync
Saves new PaymentStatusHistory record.
- Called when payment status changes (confirm, reject, gateway updates)

IFileMetadataRepository additions:

#### SavePaymentAttachmentAsync
Saves PaymentAttachment record linking Payment to File.
- Used after file upload to Azure Blob
- Maintains display order for multiple attachments

### Business Logic Changes

**Payment Status Determination:**
- Cash payments: Always Completed immediately
- Online with GatewayTransactionId: Starts as Pending (webhook will confirm)
- Other modes: Completed if TransactionReference provided

**Invoice Auto-Update:**
- Only updates invoice when payment status is Completed
- Pending payments don't affect invoice balance (allows for rollback if failed)
- Full payment (balance = 0) marks invoice as Paid
- Partial payment marks invoice as PartiallyPaid

**Validation Rules:**
- Transaction reference required for non-cash payments
- Payment amount must be > 0 and ≤ remaining balance
- Cannot pay Draft, Voided, or Cancelled invoices
- File size limit: 10 MB for attachments

### Testing
Created RecordUnifiedPaymentCommandHandlerTests with tests for:
- Cash payment (Completed status)
- Non-cash without transaction reference (error)
- Invoice not found (error)

Note: Online payment test needs adjustment for correct mock setup.

### Files Created
- RecordPaymentCommandHandler.cs
- UploadPaymentAttachmentCommand.cs + Handler
- ConfirmPaymentDirectCommand.cs + Handler
- RejectPaymentDirectCommand.cs + Handler
- GetPaymentsWithFiltersQuery.cs + Handler
- GetLeasePaymentsQuery.cs + Handler
- GetPaymentHistoryQuery.cs + Handler
- OverduePaymentDetectionJob.cs
- RecordUnifiedPaymentCommandHandlerTests.cs

### Files Modified
- IPaymentRepository.cs (added GetWithFiltersAsync, GetStatusHistoryAsync, AddStatusHistoryAsync)
- IFileMetadataRepository.cs (added SavePaymentAttachmentAsync)
- PaymentRepository.cs (implemented new methods)
- FileMetadataRepository.cs (implemented SavePaymentAttachmentAsync)
