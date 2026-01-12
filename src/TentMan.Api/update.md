# API Layer Updates - Payment Recording and Workflow APIs

## Summary
Added comprehensive payment management API endpoints to support:
- Unified payment recording for all payment modes (cash, online, UPI, bank transfer, etc.)
- Receipt/attachment uploads with Azure Blob storage
- Owner approval workflow (confirm/reject payments)
- Advanced payment filtering and querying
- Lease-specific payment retrieval
- Payment status history auditing
- Webhook stubs for payment gateway integrations (Razorpay, Stripe, PayPal)

## New Controllers

### 1. UnifiedPaymentsController (`/api/payments`)
Unified endpoint for all payment operations.

**Endpoints:**
- `POST /api/payments` - Record payment (manual entry, cash, or online)
  - Authorization: RequireManagerRole (Owner, Administrator, Manager)
  - Supports all payment modes and types
  - Request body: RecordUnifiedPaymentRequest

- `POST /api/payments/{id}/attachments` - Upload receipt/proof
  - Authorization: RequireManagerRole
  - Multipart form-data (file upload)
  - Uploads to Azure Blob Storage in "payment-receipts" container
  - Max file size: 10 MB

- `PUT /api/payments/{id}/confirm` - Confirm pending payment
  - Authorization: RequireManagerRole
  - Updates payment status to Completed
  - Auto-updates invoice amounts and status
  - Creates status history record

- `PUT /api/payments/{id}/reject` - Reject pending payment
  - Authorization: RequireManagerRole
  - Requires rejection reason
  - Updates payment status to Rejected
  - Creates status history record

- `GET /api/payments` - List payments with advanced filtering
  - Authorization: RequireManagerRole
  - Query parameters: orgId, leaseId, invoiceId, status, paymentMode, paymentType, fromDate, toDate, payerName, receivedBy, pageNumber, pageSize
  - Returns paginated results with total count

- `GET /api/payments/{id}/history` - Get payment status change history
  - Authorization: RequireManagerRole
  - Returns all status transitions with audit trail

### 2. LeasePaymentsController (`/api/leases`)
Lease-specific payment operations.

**Endpoints:**
- `GET /api/leases/{leaseId}/payments` - Get all payments for a lease
  - Authorization: RequireManagerRole
  - Returns payments ordered by date (most recent first)

### 3. PaymentWebhookController (`/api/webhooks/payments`)
Webhook endpoints for payment gateway integrations (stub implementation).

**Endpoints:**
- `POST /api/webhooks/payments/razorpay` - Razorpay webhook
  - AllowAnonymous (webhooks use signature verification)
  - Receives payment.authorized, payment.captured, payment.failed, refund.created events
  - TODO: Implement signature verification and event processing

- `POST /api/webhooks/payments/stripe` - Stripe webhook
  - AllowAnonymous
  - Receives payment_intent.succeeded, payment_intent.payment_failed events
  - TODO: Implement signature verification using Stripe webhook secret

- `POST /api/webhooks/payments/paypal` - PayPal IPN webhook
  - AllowAnonymous
  - Receives payment completions, refunds, disputes
  - TODO: Implement IPN validation by posting back to PayPal

- `POST /api/webhooks/payments/generic` - Generic webhook for testing
  - AllowAnonymous
  - Logs all webhook calls for debugging

## New Request/Response DTOs

### Request DTOs
- **RecordUnifiedPaymentRequest**: Record payment via unified endpoint
  - InvoiceId, PaymentType, PaymentMode, Amount, PaymentDate
  - TransactionReference, GatewayTransactionId, GatewayName
  - PayerName, Notes, PaymentMetadata

- **ConfirmPaymentDirectRequest**: Confirm pending payment
  - Notes (optional)

- **RejectPaymentDirectRequest**: Reject pending payment
  - Reason (required)

### Response DTOs
- **PaymentDto**: Payment details (already exists)
- **PaymentStatusHistoryDto**: Status change history
  - FromStatus, ToStatus, ChangedAtUtc, ChangedBy, Reason, Metadata

## Authorization
All payment endpoints require authentication. Most require `RequireManagerRole` policy (Owner, Administrator, Manager). Webhooks are AllowAnonymous but should implement signature verification.

## Validation
- Payment amounts must be > 0 and ≤ remaining invoice balance
- Transaction reference required for non-cash payments
- File size limit: 10 MB for attachments
- Only Pending/PendingConfirmation payments can be confirmed/rejected
- Only Issued, PartiallyPaid, or Overdue invoices can receive payments

## Integration Notes
- All endpoints use MediatR CQRS pattern for business logic separation
- File uploads integrate with existing Azure Blob Storage via IFileStorageService
- Payment status changes create audit history records via PaymentStatusHistory
- Invoice amounts automatically updated when payments are completed
- Webhook endpoints are stubs ready for gateway-specific implementation

## Security Considerations
- Webhook endpoints need signature verification before processing
- File uploads validate file size and content type
- Payment amounts validated against invoice balances
- Optimistic concurrency control via RowVersion on updates
- Audit trail tracks all status changes with user attribution

## Testing
- Unit tests: RecordUnifiedPaymentCommandHandlerTests (4 tests, 3 passing)
- Integration tests: TODO (requires test database setup)
- Manual testing: Use Swagger UI or Postman to test endpoints

## Files Modified
- Created: UnifiedPaymentsController.cs
- Created: LeasePaymentsController.cs
- Created: PaymentWebhookController.cs

## Dependencies
- Microsoft.AspNetCore.Mvc
- MediatR
- TentMan.Application (commands, queries)
- TentMan.Contracts (DTOs, enums)
- TentMan.Shared.Constants (authorization policies)
