# Cash Payment Confirmation Workflow

## Overview
The cash payment confirmation workflow allows tenants to submit proof of cash payments for owner review and verification. This provides an audit trail and confirmation mechanism for cash transactions that don't have automatic verification like online payments.

## User Flow

### Tenant Journey
1. Tenant makes a cash payment to the owner/manager
2. Tenant logs into the system and navigates to their invoice
3. Tenant creates a payment confirmation request with:
   - Payment amount
   - Payment date
   - Receipt number (optional)
   - Notes about the payment
   - Proof file upload (photo of receipt, screenshot, etc.) - optional
4. System creates a pending confirmation request
5. Tenant receives confirmation that request was submitted
6. Tenant can view status of their request (Pending, Confirmed, or Rejected)

### Owner/Manager Journey
1. Owner/manager receives notification of pending payment confirmation requests
2. Owner logs into system and views all pending requests for their organization
3. For each request, owner can:
   - View payment details (amount, date, receipt number, notes)
   - Download/view proof file if uploaded
   - See which invoice the payment is for
   - See which tenant submitted the request
4. Owner reviews the request and decides to:
   - **Confirm**: Creates payment record, updates invoice, marks request as confirmed
   - **Reject**: Marks request as rejected with a reason/note
5. System processes the decision:
   - If confirmed: Payment is recorded, invoice balance updated automatically
   - If rejected: Request is marked rejected, tenant can view rejection reason

## Technical Implementation

### Database Schema

#### PaymentConfirmationRequest Table
- `Id`: Unique identifier
- `OrgId`: Organization owning the invoice
- `InvoiceId`: Invoice being paid
- `LeaseId`: Lease associated with the invoice
- `Amount`: Amount claimed to be paid
- `PaymentDateUtc`: Date when payment was allegedly made
- `ReceiptNumber`: Optional receipt/reference number
- `Notes`: Optional notes from tenant
- `ProofFileId`: Foreign key to uploaded proof file
- `Status`: Current status (Pending, Confirmed, Rejected, Cancelled)
- `ReviewedAtUtc`: When request was reviewed
- `ReviewedBy`: Who reviewed the request (user ID)
- `ReviewResponse`: Response/notes from reviewer
- `PaymentId`: Foreign key to created Payment (when confirmed)
- Standard audit fields: CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy, IsDeleted, RowVersion

### API Endpoints

#### Tenant Endpoints
```
POST /api/invoices/{invoiceId}/payment-confirmation-requests
- Create new payment confirmation request
- Supports multipart/form-data for file upload
- Authorization: RequireTenantRole
```

#### Owner/Manager Endpoints
```
GET /api/organizations/{orgId}/payment-confirmation-requests/pending
- Get all pending requests for organization
- Authorization: RequireManagerRole

GET /api/invoices/{invoiceId}/payment-confirmation-requests
- Get all requests for specific invoice
- Authorization: RequireManagerRole

POST /api/payment-confirmation-requests/{requestId}/confirm
- Confirm a pending request
- Creates payment and updates invoice
- Authorization: RequireManagerRole

POST /api/payment-confirmation-requests/{requestId}/reject
- Reject a pending request with reason
- Authorization: RequireManagerRole
```

### Business Rules

1. **Creation Validation**:
   - Invoice must exist and be in a payable status (not Draft, Voided, or Cancelled)
   - Payment amount must be > 0
   - Payment amount cannot exceed invoice balance
   - Tenant can only create requests for their own invoices

2. **Confirmation Process**:
   - Request must be in Pending status
   - Invoice must still be in a payable status
   - Payment amount must not exceed current invoice balance (in case other payments were made)
   - Creates Payment record with:
     - PaymentMode = Cash
     - Status = Completed
     - References the confirmation request
   - Updates invoice PaidAmount, BalanceAmount, and Status
   - Marks request as Confirmed with timestamp and reviewer

3. **Rejection Process**:
   - Request must be in Pending status
   - Rejection reason is required
   - Marks request as Rejected with timestamp and reviewer
   - No payment is created

4. **File Upload**:
   - Optional proof file (receipt, screenshot, etc.)
   - Stored in Azure Blob Storage under "payment-proofs" container
   - File metadata tracked in FileMetadata table
   - Signed URLs generated for secure access (60-minute expiry)

### State Diagram

```
Pending (Initial State)
  |
  ├── Confirm → Confirmed (Final State)
  |             └── Creates Payment record
  |
  └── Reject → Rejected (Final State)
              └── No payment created
```

### Security Considerations

1. **Authorization**:
   - Only tenants can create confirmation requests
   - Only owners/managers can view, confirm, or reject requests
   - Users can only access requests within their organization

2. **File Security**:
   - Files stored in secure blob storage
   - Access via time-limited signed URLs
   - File metadata tracked with creator information

3. **Concurrency Control**:
   - RowVersion field prevents concurrent modifications
   - Optimistic concurrency pattern used throughout

4. **Audit Trail**:
   - All actions tracked with timestamps
   - Creator and modifier information stored
   - Soft delete support for data preservation

## Testing

### Unit Tests (14 total)
- CreatePaymentConfirmationRequestCommandHandler: 5 tests
  - Valid request succeeds
  - Invoice not found returns error
  - Draft invoice returns error
  - Amount exceeds balance returns error
  - Zero amount returns error

- ConfirmPaymentRequestCommandHandler: 6 tests
  - Valid confirmation creates payment and updates invoice
  - Full payment marks invoice as paid
  - Request not found returns error
  - Request not pending returns error
  - Amount exceeds remaining balance returns error

- RejectPaymentRequestCommandHandler: 4 tests
  - Valid rejection succeeds
  - Request not found returns error
  - Request not pending returns error
  - Empty review response returns error

## Future Enhancements

1. **Notifications**:
   - Email/SMS notifications when tenant creates request
   - Email/SMS notifications when owner confirms/rejects

2. **Bulk Operations**:
   - Bulk confirm multiple requests at once
   - Bulk reject with same reason

3. **Analytics**:
   - Average time to confirm
   - Rejection rate
   - Most common rejection reasons

4. **Tenant Self-Service**:
   - Allow tenant to cancel pending requests
   - Allow tenant to update pending requests

5. **Auto-Approval**:
   - Configure auto-approval for trusted tenants
   - Configure auto-approval below certain amount threshold

## Related Documents
- [Payment Module Updates](../TentMan.Application/update.md)
- [Payment Infrastructure Updates](../TentMan.Infrastructure/update.md)
- [Payment API Updates](../TentMan.Api/update.md)
