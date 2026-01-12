# Payment Module Updates

## Overview
Added support for multiple payment modes (Cash, Online, BankTransfer, UPI, Cheque, etc.) to the billing engine. This enables tracking of rent and invoice payments through various payment methods.

## Changes Made

### New Entities
- **Payment**: Core entity for tracking payments against invoices
  - Fields: PaymentMode, PaymentStatus, Amount, PaymentDate, TransactionReference, ReceivedBy, PayerName, Notes, PaymentMetadata

### New Enums
- **PaymentMode**: Cash, Online, BankTransfer, Cheque, UPI, Other
- **PaymentStatus**: Pending, Completed, Failed, Cancelled, Processing, Refunded

### Command Handlers
- **RecordCashPaymentCommandHandler**: Records cash payments (immediately marked as completed)
- **RecordOnlinePaymentCommandHandler**: Records online payments (stub for future gateway integration)

### Query Handlers
- **GetInvoicePaymentsQueryHandler**: Retrieves all payments for an invoice

### Business Logic
- Payment validation (amount, invoice status, remaining balance)
- Automatic invoice status updates (Issued → PartiallyPaid → Paid)
- Automatic PaidAmount and BalanceAmount calculations
- Concurrency control using RowVersion

## Database Changes
- New table: `Payments`
- Updated table: `Invoices` (added Payments navigation property)
- Migration: `20260112005443_AddPaymentTable`

## API Endpoints
- `POST /api/invoices/{id}/payments/cash` - Record cash payment
- `POST /api/invoices/{id}/payments/online` - Record online payment (stub)
- `GET /api/invoices/{id}/payments` - Get invoice payments

## Testing
- Unit tests for RecordCashPaymentCommandHandler
- Validated payment scenarios: valid payment, invoice not found, draft invoice, amount exceeds balance, full payment
