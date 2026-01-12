# Payment API Updates

## Overview
Added REST API endpoints for recording and retrieving invoice payments with support for multiple payment modes.

**NEW (2026-01-12):** Added cash payment confirmation workflow API endpoints allowing tenants to submit payment proofs for owner review.

## Changes Made

### New Controllers
- **PaymentsController**: Handles payment-related API requests
  - Authorization: RequireManagerRole (Owner, Administrator, Manager)
  - Base path: `/api/invoices/{invoiceId}/payments`
- **PaymentConfirmationController**: Handles payment confirmation workflow
  - Authorization: RequireTenantRole for creation, RequireManagerRole for review
  - Base paths: Multiple paths for different operations

### API Endpoints

#### Record Cash Payment
- **Endpoint**: `POST /api/invoices/{invoiceId}/payments/cash`
- **Authorization**: RequireManagerRole
- **Request**: RecordCashPaymentRequest
  - Amount (decimal, required)
  - PaymentDate (DateTime, required)
  - PayerName (string, optional)
  - ReceiptNumber (string, optional)
  - Notes (string, optional)
- **Response**: ApiResponse<Guid> (PaymentId)
- **Behavior**: Cash payments are immediately marked as Completed

#### Record Online Payment
- **Endpoint**: `POST /api/invoices/{invoiceId}/payments/online`
- **Authorization**: RequireManagerRole
- **Request**: RecordOnlinePaymentRequest
  - PaymentMode (enum: Online, BankTransfer, UPI, etc.)
  - Amount (decimal, required)
  - PaymentDate (DateTime, required)
  - TransactionReference (string, required)
  - PayerName (string, optional)
  - Notes (string, optional)
  - PaymentMetadata (string/JSON, optional)
- **Response**: ApiResponse<Guid> (PaymentId)
- **Note**: This is a stub for future payment gateway integration

#### Get Invoice Payments
- **Endpoint**: `GET /api/invoices/{invoiceId}/payments`
- **Authorization**: RequireManagerRole
- **Response**: ApiResponse<IEnumerable<PaymentDto>>
- **Returns**: List of all payments for the invoice, ordered by payment date (descending)

### Payment Confirmation Workflow Endpoints

#### Create Payment Confirmation Request (Tenant)
- **Endpoint**: `POST /api/invoices/{invoiceId}/payment-confirmation-requests`
- **Authorization**: RequireTenantRole
- **Content-Type**: multipart/form-data
- **Request**: CreatePaymentConfirmationRequest + optional file upload
  - Amount (decimal, required)
  - PaymentDate (DateTime, required)
  - ReceiptNumber (string, optional)
  - Notes (string, optional)
  - ProofFile (IFormFile, optional) - receipt photo, screenshot, etc.
- **Response**: ApiResponse<Guid> (RequestId)
- **Behavior**: Creates pending request awaiting owner review

#### Get Pending Confirmation Requests (Owner)
- **Endpoint**: `GET /api/organizations/{orgId}/payment-confirmation-requests/pending`
- **Authorization**: RequireManagerRole
- **Response**: ApiResponse<IEnumerable<PaymentConfirmationRequestDto>>
- **Returns**: List of pending payment confirmation requests for the organization

#### Get Invoice Confirmation Requests
- **Endpoint**: `GET /api/invoices/{invoiceId}/payment-confirmation-requests`
- **Authorization**: RequireManagerRole
- **Response**: ApiResponse<IEnumerable<PaymentConfirmationRequestDto>>
- **Returns**: All payment confirmation requests for the invoice (all statuses)

#### Confirm Payment Request (Owner)
- **Endpoint**: `POST /api/payment-confirmation-requests/{requestId}/confirm`
- **Authorization**: RequireManagerRole
- **Request**: ConfirmPaymentRequest
  - ReviewResponse (string, optional) - approval notes
- **Response**: ApiResponse<Guid> (PaymentId)
- **Behavior**: Creates payment record, updates invoice, marks request as confirmed

#### Reject Payment Request (Owner)
- **Endpoint**: `POST /api/payment-confirmation-requests/{requestId}/reject`
- **Authorization**: RequireManagerRole
- **Request**: RejectPaymentRequest
  - ReviewResponse (string, required) - rejection reason
- **Response**: ApiResponse<object>
- **Behavior**: Marks request as rejected with response note

### Validation
- Payment amount must be > 0
- Payment amount cannot exceed invoice remaining balance
- Transaction reference required for non-cash payments
- Invoice must be in Issued, PartiallyPaid, or Overdue status
- Cannot record payments for Draft, Voided, or Cancelled invoices
- Confirmation requests can only be created by tenants
- Confirmation/rejection can only be done by owners/managers
- Requests can only be confirmed/rejected when in Pending status

### Status Codes
- 200 OK: Successful operation
- 400 Bad Request: Validation errors
- 401 Unauthorized: Missing authentication
- 403 Forbidden: Insufficient permissions
- 404 Not Found: Invoice or request not found

### File Upload Support
- Proof files stored using IFileStorageService (Azure Blob Storage)
- Supported file types: images, PDFs, documents
- File metadata tracked in FileMetadata table
- Signed URLs generated for secure file access (60-minute expiry)
