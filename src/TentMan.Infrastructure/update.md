# Payment Infrastructure Updates

## Overview
Added repository implementation and database configuration for the Payment entity to support multiple payment modes.

## Changes Made

### Repositories
- **PaymentRepository**: Implementation of IPaymentRepository
  - Extends BaseRepository<Payment> for optimistic concurrency
  - Methods: GetByIdAsync, GetByInvoiceIdAsync, GetByLeaseIdAsync, GetByOrgIdAsync, AddAsync, UpdateAsync, DeleteAsync, ExistsAsync, GetTotalPaidAmountAsync

### EF Core Configuration
- **PaymentConfiguration**: Entity type configuration for Payment entity
  - Precision: decimal(18, 2) for Amount field
  - Indexes on: OrgId, InvoiceId, LeaseId, PaymentDateUtc, TransactionReference
  - Foreign keys to Organization, Invoice, Lease (DeleteBehavior.Restrict)
  - RowVersion for concurrency control
  - IsDeleted for soft delete support

### Database Migration
- **20260112005443_AddPaymentTable**: Creates Payments table with all required fields and relationships

### Dependency Injection
- Registered PaymentRepository in DI container (DependencyInjection.cs)

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
```
