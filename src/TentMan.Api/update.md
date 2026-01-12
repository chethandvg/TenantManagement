# Payment API Updates

## Overview
Added REST API endpoints for recording and retrieving invoice payments with support for multiple payment modes.

## Changes Made

### New Controller
- **PaymentsController**: Handles payment-related API requests
  - Authorization: RequireManagerRole (Owner, Administrator, Manager)
  - Base path: `/api/invoices/{invoiceId}/payments`

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

### Validation
- Payment amount must be > 0
- Payment amount cannot exceed invoice remaining balance
- Transaction reference required for non-cash payments
- Invoice must be in Issued, PartiallyPaid, or Overdue status
- Cannot record payments for Draft, Voided, or Cancelled invoices

### Status Codes
- 200 OK: Successful operation
- 400 Bad Request: Validation errors
- 401 Unauthorized: Missing authentication
- 403 Forbidden: Insufficient permissions
- 404 Not Found: Invoice not found
