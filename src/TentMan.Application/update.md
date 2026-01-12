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
