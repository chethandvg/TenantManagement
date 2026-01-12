# Payment Infrastructure Updates

## Overview
Added repository implementation and database configuration for the Payment entity to support multiple payment modes.

**NEW (2026-01-12):** Added PaymentConfirmationRequest repository and database configuration to support tenant-initiated payment confirmation workflow.

## Changes Made

### Repositories
- **PaymentRepository**: Implementation of IPaymentRepository
  - Extends BaseRepository<Payment> for optimistic concurrency
  - Methods: GetByIdAsync, GetByInvoiceIdAsync, GetByLeaseIdAsync, GetByOrgIdAsync, AddAsync, UpdateAsync, DeleteAsync, ExistsAsync, GetTotalPaidAmountAsync
- **PaymentConfirmationRequestRepository**: Implementation of IPaymentConfirmationRequestRepository
  - Extends BaseRepository<PaymentConfirmationRequest> for optimistic concurrency
  - Methods: GetByIdAsync, GetByInvoiceIdAsync, GetByLeaseIdAsync, GetPendingByOrgIdAsync, GetByOrgIdAndStatusAsync, AddAsync, UpdateAsync, DeleteAsync, ExistsAsync

### EF Core Configuration
- **PaymentConfiguration**: Entity type configuration for Payment entity
  - Precision: decimal(18, 2) for Amount field
  - Indexes on: OrgId, InvoiceId, LeaseId, PaymentDateUtc, TransactionReference
  - Foreign keys to Organization, Invoice, Lease (DeleteBehavior.Restrict)
  - RowVersion for concurrency control
  - IsDeleted for soft delete support
- **PaymentConfirmationRequestConfiguration**: Entity type configuration for PaymentConfirmationRequest entity
  - Precision: decimal(18, 2) for Amount field
  - Indexes on: OrgId, InvoiceId, LeaseId, Status, PaymentDateUtc
  - Foreign keys to Organization, Invoice, Lease, ProofFile, Payment
  - RowVersion for concurrency control
  - IsDeleted for soft delete support

### Database Migrations
- **20260112005443_AddPaymentTable**: Creates Payments table with all required fields and relationships
- **20260112012710_AddPaymentConfirmationRequest**: Creates PaymentConfirmationRequests table with file upload support

### Dependency Injection
- Registered PaymentRepository in DI container (DependencyInjection.cs)
- Registered PaymentConfirmationRequestRepository in DI container (DependencyInjection.cs)

## Database Schema
```sql
CREATE TABLE Payments (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    OrgId uniqueidentifier NOT NULL,
    InvoiceId uniqueidentifier NOT NULL,
    LeaseId uniqueidentifier NOT NULL,
    PaymentMode int NOT NULL,
    Status int NOT NULL,
    Amount decimal(18,2) NOT NULL,
    PaymentDateUtc datetime2 NOT NULL,
    TransactionReference nvarchar(200),
    ReceivedBy nvarchar(100) NOT NULL,
    PayerName nvarchar(200),
    Notes nvarchar(2000),
    PaymentMetadata nvarchar(max),
    CreatedAtUtc datetime2 NOT NULL,
    CreatedBy nvarchar(max),
    ModifiedAtUtc datetime2,
    ModifiedBy nvarchar(max),
    IsDeleted bit NOT NULL,
    DeletedAtUtc datetime2,
    RowVersion rowversion NOT NULL,
    CONSTRAINT FK_Payments_Organizations FOREIGN KEY (OrgId) REFERENCES Organizations (Id),
    CONSTRAINT FK_Payments_Invoices FOREIGN KEY (InvoiceId) REFERENCES Invoices (Id),
    CONSTRAINT FK_Payments_Leases FOREIGN KEY (LeaseId) REFERENCES Leases (Id)
);

CREATE TABLE PaymentConfirmationRequests (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    OrgId uniqueidentifier NOT NULL,
    InvoiceId uniqueidentifier NOT NULL,
    LeaseId uniqueidentifier NOT NULL,
    Amount decimal(18,2) NOT NULL,
    PaymentDateUtc datetime2 NOT NULL,
    ReceiptNumber nvarchar(200),
    Notes nvarchar(2000),
    ProofFileId uniqueidentifier,
    Status int NOT NULL,
    ReviewedAtUtc datetime2,
    ReviewedBy nvarchar(100),
    ReviewResponse nvarchar(2000),
    PaymentId uniqueidentifier,
    CreatedAtUtc datetime2 NOT NULL,
    CreatedBy nvarchar(max),
    ModifiedAtUtc datetime2,
    ModifiedBy nvarchar(max),
    IsDeleted bit NOT NULL,
    DeletedAtUtc datetime2,
    RowVersion rowversion NOT NULL,
    CONSTRAINT FK_PaymentConfirmationRequests_Organizations FOREIGN KEY (OrgId) REFERENCES Organizations (Id),
    CONSTRAINT FK_PaymentConfirmationRequests_Invoices FOREIGN KEY (InvoiceId) REFERENCES Invoices (Id),
    CONSTRAINT FK_PaymentConfirmationRequests_Leases FOREIGN KEY (LeaseId) REFERENCES Leases (Id),
    CONSTRAINT FK_PaymentConfirmationRequests_FileMetadata FOREIGN KEY (ProofFileId) REFERENCES FileMetadata (Id),
    CONSTRAINT FK_PaymentConfirmationRequests_Payments FOREIGN KEY (PaymentId) REFERENCES Payments (Id)
);
```
