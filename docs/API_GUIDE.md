# TentMan API Guide

Complete reference for TentMan's two complementary APIs: **Main API** (TentMan.Api) and **Admin API** (TentMan.AdminApi).

---

## 📚 Table of Contents

- [Overview](#overview)
- [Main API (TentMan.Api)](#main-api-tentmanapi)
- [Admin API (TentMan.AdminApi)](#admin-api-tentmanadminapi)
- [Tenant Portal API](#tenant-portal-api)
- [Authentication](#authentication)
- [Common Workflows](#common-workflows)
- [HTTP Examples](#http-examples)
- [OpenAPI Documentation](#openapi-documentation)
- [Error Handling](#error-handling)

---

## 🎯 Overview

### Two APIs, One System

TentMan provides **two complementary APIs** that work together:

| Aspect | Main API (TentMan.Api) | Admin API (TentMan.AdminApi) |
|--------|---------------------|---------------------------|
| **Purpose** | Public-facing API | Administrative operations |
| **Port** | 7123 (HTTPS) | 7290 (HTTPS) |
| **Authentication** | Self-registration + Login | Requires pre-existing account |
| **Primary Users** | End users, frontend apps | Administrators, system managers |
| **Endpoints** | 16 endpoints | 12 endpoints |
| **Features** | Auth, Products, Account | Users, Roles, Assignments |

### Shared Infrastructure

Both APIs share:
- ✅ **Same JWT Secret** - Tokens work across both APIs
- ✅ **Same Database** - Unified user/role management
- ✅ **Same Identity System** - ASP.NET Core Identity
- ✅ **Consistent Error Responses** - Standardized error format

---

## 🌐 Main API (TentMan.Api)

**Base URL**: `https://localhost:7123`  
**API Docs**: `https://localhost:7123/scalar/v1`

### Purpose

Public-facing API for:
- User authentication (register, login, logout)
- Product catalog operations (CRUD)
- Password management (change, reset)
- Email confirmation
- Account management

### Key Features

- ✅ **Self-registration** - Users can create accounts
- ✅ **JWT authentication** - Secure token-based auth
- ✅ **Role-based access** - Different permissions per role
- ✅ **Refresh tokens** - Seamless re-authentication
- ✅ **OpenAPI docs** - Interactive Scalar UI

### Endpoint Categories

#### 1. Authentication (`/api/v1/authentication`)

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/register` | POST | ❌ No | Create new account |
| `/login` | POST | ❌ No | Login and get JWT tokens |
| `/refresh` | POST | ❌ No | Refresh access token |
| `/logout` | POST | ✅ Yes | Logout (invalidate tokens) |
| `/confirm-email` | POST | ❌ No | Confirm email address |

**Example - Register**:
```json
POST /api/v1/authentication/register
{
  "userName": "johndoe",
  "email": "john@example.com",
  "password": "SecurePass123!"
}

Response 200 OK:
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "abc123...",
  "expiresIn": 3600
}
```

**Example - Login**:
```json
POST /api/v1/authentication/login
{
  "email": "john@example.com",
  "password": "SecurePass123!"
}

Response 200 OK:
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "abc123...",
  "expiresIn": 3600
}
```

#### 2. Products (`/api/v1/products`)

| Endpoint | Method | Auth | Roles | Description |
|----------|--------|------|-------|-------------|
| `/` | GET | ✅ Yes | User+ | List all products |
| `/{id}` | GET | ✅ Yes | User+ | Get product by ID |
| `/` | POST | ✅ Yes | Manager+ | Create product |
| `/{id}` | PUT | ✅ Yes | Manager+ | Update product |
| `/{id}` | DELETE | ✅ Yes | Admin+ | Delete product |

**Example - Create Product**:
```json
POST /api/v1/products
Authorization: Bearer <token>

{
  "name": "Laptop",
  "price": 1299.99
}

Response 201 Created:
{
  "id": "guid",
  "name": "Laptop",
  "price": 1299.99,
  "rowVersion": "AAAAAAAAB9E="
}
```

#### 3. Account Management (`/api/v1/account`)

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/change-password` | POST | ✅ Yes | Change your password |
| `/reset-password-request` | POST | ❌ No | Request password reset |
| `/reset-password` | POST | ❌ No | Reset password with token |

**Example - Change Password**:
```json
POST /api/v1/account/change-password
Authorization: Bearer <token>

{
  "currentPassword": "OldPass123!",
  "newPassword": "NewPass456!",
  "confirmNewPassword": "NewPass456!"
}

Response 200 OK:
{
  "success": true,
  "message": "Password changed successfully"
}
```

#### 4. Health Checks

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Overall health |
| `/health/ready` | GET | Readiness check |
| `/health/live` | GET | Liveness check |

#### 5. Tenants (`/api/v1/organizations/{orgId}/tenants`)

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/` | POST | ✅ Yes | Create a new tenant |
| `/` | GET | ✅ Yes | List/search tenants |
| `/{tenantId}` | GET | ✅ Yes | Get tenant details |
| `/{tenantId}` | PUT | ✅ Yes | Update tenant |
| `/{tenantId}/invites` | POST | ✅ Yes | Generate tenant invite |

**Example - Create Tenant**:
```json
POST /api/v1/organizations/{orgId}/tenants
Authorization: Bearer <token>

{
  "fullName": "Rajesh Kumar",
  "phone": "+919876543210",
  "email": "rajesh@example.com"
}

Response 201 Created:
{
  "success": true,
  "data": {
    "id": "guid",
    "fullName": "Rajesh Kumar",
    "phone": "+919876543210",
    "email": "rajesh@example.com",
    "isActive": true
  }
}
```

**Example - Generate Invite**:
```json
POST /api/v1/organizations/{orgId}/tenants/{tenantId}/invites
Authorization: Bearer <token>

{
  "tenantId": "guid",
  "expiryDays": 7
}

Response 201 Created:
{
  "success": true,
  "data": {
    "id": "guid",
    "inviteToken": "a1b2c3d4e5f67890abcdef1234567890",
    "tenantFullName": "Rajesh Kumar",
    "email": "rajesh@example.com",
    "expiresAtUtc": "2026-01-16T10:00:00Z",
    "isUsed": false
  },
  "message": "Invite generated successfully"
}
```

#### 5b. Tenant Invites (`/api/v1/invites`)

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/validate?token={token}` | GET | ❌ No | Validate invite token |
| `/accept` | POST | ❌ No | Accept invite & create account |

**Example - Validate Invite**:
```json
GET /api/v1/invites/validate?token=a1b2c3d4...

Response 200 OK:
{
  "success": true,
  "data": {
    "isValid": true,
    "tenantFullName": "Rajesh Kumar",
    "email": "rajesh@example.com"
  }
}
```

**Example - Accept Invite**:
```json
POST /api/v1/invites/accept

{
  "inviteToken": "a1b2c3d4e5f67890abcdef1234567890",
  "userName": "rajeshk",
  "email": "rajesh@example.com",
  "password": "SecurePass123!@#"
}

Response 200 OK:
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGc...",
    "refreshToken": "abc123...",
    "expiresIn": 3600,
    "user": {
      "id": "guid",
      "userName": "rajeshk",
      "email": "rajesh@example.com",
      "roles": ["Tenant"]
    }
  }
}
```

**See**: [Tenant Invite System Guide](TENANT_INVITE_SYSTEM.md) for complete documentation.

#### 6. Leases (`/api/v1/organizations/{orgId}/leases`)

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/` | POST | ✅ Yes | Create draft lease |
| `/{leaseId}` | GET | ✅ Yes | Get lease details |
| `/{leaseId}/parties` | POST | ✅ Yes | Add tenant to lease |
| `/{leaseId}/terms` | POST | ✅ Yes | Add financial terms |
| `/{leaseId}/activate` | POST | ✅ Yes | Activate lease |
| `/units/{unitId}/leases` | GET | ✅ Yes | Get lease history for unit |

**Example - Create Draft Lease**:
```json
POST /api/v1/organizations/{orgId}/leases
Authorization: Bearer <token>

{
  "unitId": "unit-guid",
  "leaseNumber": "LSE-2026-001",
  "startDate": "2026-02-01",
  "endDate": "2027-01-31",
  "rentDueDay": 5,
  "graceDays": 3
}

Response 201 Created:
{
  "success": true,
  "data": {
    "id": "guid",
    "status": 0,
    "leaseNumber": "LSE-2026-001"
  }
}
```

**See**: [Tenant and Lease Management Guide](TENANT_LEASE_MANAGEMENT.md) for complete API documentation.

#### 7. Billing (`/api/v1/billing/*`)

Comprehensive billing and invoicing APIs for managing charges, invoices, and payments.

##### Invoice Management (`/api/invoices`)

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/invoices` | GET | ✅ Manager+ | List all invoices with filters |
| `/api/invoices/{invoiceId}` | GET | ✅ Manager+ | Get invoice details with line items |
| `/api/leases/{leaseId}/invoices/generate` | POST | ✅ Manager+ | Generate draft invoice for lease |
| `/api/invoices/{invoiceId}/issue` | POST | ✅ Manager+ | Issue (finalize) a draft invoice |
| `/api/invoices/{invoiceId}/void` | POST | ✅ Manager+ | Void an issued invoice |

**Example - List Invoices**:
```json
GET /api/invoices?status=Issued&from=2026-01-01&to=2026-01-31
Authorization: Bearer <token>

Response 200 OK:
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "invoiceNumber": "INV-202601-000001",
      "invoiceDate": "2026-01-01",
      "dueDate": "2026-01-05",
      "status": "Issued",
      "totalAmount": 15000.00,
      "paidAmount": 0.00,
      "balanceAmount": 15000.00,
      "leaseNumber": "LSE-2026-001"
    }
  ]
}
```

**Example - Generate Invoice**:
```json
POST /api/leases/{leaseId}/invoices/generate
Authorization: Bearer <token>

{
  "billingPeriodStart": "2026-01-01",
  "billingPeriodEnd": "2026-01-31",
  "prorationMethod": "ActualDaysInMonth"
}

Response 201 Created:
{
  "success": true,
  "data": {
    "id": "guid",
    "invoiceNumber": "INV-202601-000001",
    "status": "Draft",
    "totalAmount": 15000.00,
    "lineItems": [
      {
        "chargeType": "RENT",
        "description": "Monthly Rent",
        "amount": 15000.00
      }
    ]
  }
}
```

##### Billing Settings (`/api/leases/{leaseId}/billing-settings`)

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/{leaseId}/billing-settings` | GET | ✅ Manager+ | Get lease billing configuration |
| `/{leaseId}/billing-settings` | PUT | ✅ Manager+ | Update billing settings |

**Example - Update Billing Settings**:
```json
PUT /api/leases/{leaseId}/billing-settings
Authorization: Bearer <token>

{
  "billingDay": 1,
  "paymentTermDays": 5,
  "generateInvoiceAutomatically": true,
  "invoicePrefix": "INV",
  "paymentInstructions": "Please pay via bank transfer"
}

Response 200 OK:
{
  "success": true,
  "message": "Billing settings updated successfully"
}
```

##### Recurring Charges (`/api/leases/{leaseId}/recurring-charges`)

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/{leaseId}/recurring-charges` | GET | ✅ Manager+ | List all recurring charges for lease |
| `/{leaseId}/recurring-charges` | POST | ✅ Manager+ | Add new recurring charge |
| `/recurring-charges/{chargeId}` | PUT | ✅ Manager+ | Update recurring charge |
| `/recurring-charges/{chargeId}` | DELETE | ✅ Manager+ | Delete recurring charge |

**Example - Create Recurring Charge**:
```json
POST /api/leases/{leaseId}/recurring-charges
Authorization: Bearer <token>

{
  "chargeTypeId": "guid-for-MAINT",
  "description": "Monthly Maintenance",
  "amount": 2000.00,
  "frequency": "Monthly",
  "startDate": "2026-01-01",
  "isActive": true
}

Response 201 Created:
{
  "success": true,
  "data": {
    "id": "guid",
    "chargeTypeName": "Maintenance",
    "amount": 2000.00,
    "frequency": "Monthly"
  }
}
```

##### Utility Statements (`/api/leases/{leaseId}/utility-statements`)

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/{leaseId}/utility-statements` | GET | ✅ Manager+ | List utility statements |
| `/{leaseId}/utility-statements` | POST | ✅ Manager+ | Record new utility statement |

**Example - Record Meter-Based Utility**:
```json
POST /api/leases/{leaseId}/utility-statements
Authorization: Bearer <token>

{
  "utilityType": "Electricity",
  "billingPeriodStart": "2026-01-01",
  "billingPeriodEnd": "2026-01-31",
  "isMeterBased": true,
  "utilityRatePlanId": "guid",
  "previousReading": 1000.0,
  "currentReading": 1250.0,
  "notes": "January electricity consumption"
}

Response 201 Created:
{
  "success": true,
  "data": {
    "id": "guid",
    "unitsConsumed": 250.0,
    "calculatedAmount": 3500.00,
    "totalAmount": 3500.00
  }
}
```

##### Invoice Runs (`/api/invoice-runs`)

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/invoice-runs` | GET | ✅ Manager+ | List all invoice runs |
| `/api/invoice-runs` | POST | ✅ Manager+ | Execute batch invoice generation |
| `/api/invoice-runs/{runId}` | GET | ✅ Manager+ | Get invoice run details |
| `/api/invoice-runs/{runId}/items` | GET | ✅ Manager+ | Get per-lease results |

**Example - Execute Invoice Run**:
```json
POST /api/invoice-runs
Authorization: Bearer <token>

{
  "orgId": "org-guid",
  "billingPeriodStart": "2026-01-01",
  "billingPeriodEnd": "2026-01-31",
  "notes": "January 2026 monthly billing run"
}

Response 201 Created:
{
  "success": true,
  "data": {
    "id": "guid",
    "runNumber": "RUN-202601-001",
    "status": "InProgress",
    "totalLeases": 25,
    "successCount": 0,
    "failureCount": 0
  }
}
```

##### Credit Notes (`/api/invoices/{invoiceId}/credit-notes`)

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/invoices/{invoiceId}/credit-notes` | POST | ✅ Manager+ | Create credit note for invoice |
| `/credit-notes/{creditNoteId}` | GET | ✅ Manager+ | Get credit note details |
| `/credit-notes/{creditNoteId}/void` | POST | ✅ Manager+ | Void a credit note |

**Example - Create Credit Note**:
```json
POST /api/invoices/{invoiceId}/credit-notes
Authorization: Bearer <token>

{
  "reason": "InvoiceError",
  "amount": 500.00,
  "notes": "Incorrect maintenance charge",
  "lineItems": [
    {
      "invoiceLineId": "guid",
      "description": "Maintenance charge correction",
      "amount": 500.00
    }
  ]
}

Response 201 Created:
{
  "success": true,
  "data": {
    "id": "guid",
    "creditNoteNumber": "CN-202601-000001",
    "totalAmount": 500.00,
    "reason": "InvoiceError"
  }
}
```

**See**: [Billing Engine Guide](BILLING_ENGINE.md) and [Billing UI Guide](BILLING_UI_GUIDE.md) for complete documentation.

### Role Requirements (Main API)

| Operation | Public | User | Manager | Administrator |
|-----------|--------|------|---------|---------------|
| Register/Login | ✅ | ✅ | ✅ | ✅ |
| Password Mgmt | ✅ | ✅ | ✅ | ✅ |
| Read Products | ❌ | ✅ | ✅ | ✅ |
| Create Products | ❌ | ❌ | ✅ | ✅ |
| Update Products | ❌ | ❌ | ✅ | ✅ |
| Delete Products | ❌ | ❌ | ❌ | ✅ |
| Manage Tenants | ❌ | ❌ | ✅ | ✅ |
| Manage Leases | ❌ | ❌ | ✅ | ✅ |
| Manage Billing | ❌ | ❌ | ✅ | ✅ |

---

## 🛡️ Admin API (TentMan.AdminApi)

**Base URL**: `https://localhost:7290`  
**API Docs**: `https://localhost:7290/scalar/v1`

### Purpose

Administrative API for:
- User management (create, delete, list)
- Role management (create, assign, remove)
- System initialization
- Security restrictions enforcement

### Key Features

- ✅ **Centralized user management** - Admins control users
- ✅ **Role-based administration** - Fine-grained permissions
- ✅ **Security restrictions** - Prevent dangerous operations
- ✅ **System initialization** - One-time setup
- ✅ **Pagination support** - Handle large datasets

### Endpoint Categories

#### 1. System Initialization (`/api/v1/admin/initialization`)

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/initialize` | POST | ❌ No (once) | Initialize system with SuperAdmin |

**⚠️ ONE-TIME OPERATION**: Can only be called when no users exist.

**Example**:
```json
POST /api/v1/admin/initialization/initialize

{
  "userName": "superadmin",
  "email": "admin@company.com",
  "password": "SuperSecure123!"
}

Response 200 OK:
{
  "userId": "guid",
  "userName": "superadmin",
  "email": "admin@company.com",
  "roles": ["SuperAdmin", "Administrator", "User"]
}
```

**What it creates**:
- 5 system roles: Guest, User, Manager, Administrator, SuperAdmin
- 1 SuperAdmin user with credentials you specify

#### 2. User Management (`/api/v1/admin/users`)

| Endpoint | Method | Auth | Roles | Description |
|----------|--------|------|-------|-------------|
| `/` | GET | ✅ Yes | Manager+ | List all users (paginated) |
| `/` | POST | ✅ Yes | Manager+ | Create new user |
| `/{id}` | DELETE | ✅ Yes | SuperAdmin+ | Delete user |

**Example - List Users**:
```json
GET /api/v1/admin/users?pageNumber=1&pageSize=10
Authorization: Bearer <token>

Response 200 OK:
{
  "items": [
    {
      "id": "guid",
      "userName": "johndoe",
      "email": "john@example.com",
      "emailConfirmed": true,
      "roles": ["User"]
    }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 25,
  "totalPages": 3
}
```

**Example - Create User**:
```json
POST /api/v1/admin/users
Authorization: Bearer <token>

{
  "userName": "janedoe",
  "email": "jane@example.com",
  "password": "TempPass123!",
  "emailConfirmed": false
}

Response 201 Created:
{
  "id": "guid",
  "userName": "janedoe",
  "email": "jane@example.com",
  "emailConfirmed": false
}
```

**Example - Delete User**:
```json
DELETE /api/v1/admin/users/{userId}
Authorization: Bearer <token>

Response 200 OK
```

**Security Restrictions**:
- ❌ Cannot delete yourself
- ❌ Cannot delete the last SuperAdmin

#### 3. Role Management (`/api/v1/admin/roles`)

| Endpoint | Method | Auth | Roles | Description |
|----------|--------|------|-------|-------------|
| `/` | GET | ✅ Yes | Manager+ | List all roles |
| `/` | POST | ✅ Yes | SuperAdmin+ | Create custom role |

**Example - List Roles**:
```json
GET /api/v1/admin/roles
Authorization: Bearer <token>

Response 200 OK:
[
  {
    "id": "guid",
    "name": "SuperAdmin",
    "description": "Super administrator with full access"
  },
  {
    "id": "guid",
    "name": "User",
    "description": "Standard user role"
  }
]
```

**Example - Create Role**:
```json
POST /api/v1/admin/roles
Authorization: Bearer <token>

{
  "name": "ContentEditor",
  "description": "Can edit content but not manage users"
}

Response 201 Created:
{
  "id": "guid",
  "name": "ContentEditor",
  "description": "Can edit content but not manage users"
}
```

#### 4. User Role Management (`/api/v1/admin/user-roles`)

| Endpoint | Method | Auth | Roles | Description |
|----------|--------|------|-------|-------------|
| `/{userId}` | GET | ✅ Yes | Manager+ | Get user's roles |
| `/assign` | POST | ✅ Yes | SuperAdmin+ | Assign role to user |
| `/{userId}/roles/{roleId}` | DELETE | ✅ Yes | SuperAdmin+ | Remove role from user |

**Example - Get User Roles**:
```json
GET /api/v1/admin/user-roles/{userId}
Authorization: Bearer <token>

Response 200 OK:
[
  {
    "id": "guid",
    "name": "User"
  },
  {
    "id": "guid",
    "name": "Manager"
  }
]
```

**Example - Assign Role**:
```json
POST /api/v1/admin/user-roles/assign
Authorization: Bearer <token>

{
  "userId": "user-guid",
  "roleId": "role-guid"
}

Response 200 OK:
{
  "success": true,
  "message": "Role assigned successfully"
}
```

**Example - Remove Role**:
```json
DELETE /api/v1/admin/user-roles/{userId}/roles/{roleId}
Authorization: Bearer <token>

Response 200 OK
```

**Security Restrictions**:
- ❌ Only SuperAdmin can assign SuperAdmin/Administrator roles
- ❌ Administrators cannot assign SuperAdmin role
- ❌ Cannot remove your own privileged roles (SuperAdmin, Administrator)
- ❌ Cannot remove roles from the last SuperAdmin

#### 5. Health Checks

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Overall health |
| `/health/ready` | GET | Readiness check |
| `/health/live` | GET | Liveness check |

### Role Requirements (Admin API)

| Operation | Manager | Administrator | SuperAdmin |
|-----------|---------|---------------|------------|
| View Users | ✅ | ✅ | ✅ |
| Create Users | ✅ | ✅ | ✅ |
| Delete Users | ❌ | ❌ | ✅ |
| View Roles | ✅ | ✅ | ✅ |
| Create Roles | ❌ | ❌ | ✅ |
| Assign User/Guest roles | ❌ | ✅ | ✅ |
| Assign Manager role | ❌ | ✅ | ✅ |
| Assign Administrator role | ❌ | ❌ | ✅ |
| Assign SuperAdmin role | ❌ | ❌ | ✅ |

---

## 🏠 Tenant Portal API

**Base URL**: Same as Main API (`https://localhost:7123`)  
**API Prefix**: `/api/v1/tenant-portal`

### Purpose

Dedicated API for tenants to:
- Access their active lease information
- Upload and view documents
- Complete move-in handover checklist
- View their tenant dashboard

### Key Features

- ✅ **Role-based access** - Only accessible to users with "Tenant" role
- ✅ **User context** - Automatically identifies tenant from JWT claims
- ✅ **Secure document storage** - Azure Blob Storage integration
- ✅ **Digital signatures** - Move-in handover with signature capture

### Endpoint Categories

#### 1. Lease Summary (`/api/v1/tenant-portal/lease-summary`)

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/lease-summary` | GET | ✅ Yes (Tenant) | Get current tenant's active lease details |

**Example**:
```json
GET /api/v1/tenant-portal/lease-summary
Authorization: Bearer <tenant_token>

Response 200 OK:
{
  "success": true,
  "data": {
    "leaseId": "guid",
    "leaseNumber": "LSE-2024-001",
    "startDate": "2024-01-01",
    "endDate": "2024-12-31",
    "monthlyRent": 15000.00,
    "securityDeposit": 30000.00,
    "unitNumber": "A-101",
    "buildingName": "Sunrise Apartments"
  }
}
```

#### 2. Document Management (`/api/v1/tenant-portal/documents`)

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/documents` | GET | ✅ Yes (Tenant) | List all documents uploaded by tenant |
| `/documents` | POST | ✅ Yes (Tenant) | Upload a new document |

**Example - Upload Document**:
```http
POST /api/v1/tenant-portal/documents
Authorization: Bearer <tenant_token>
Content-Type: multipart/form-data

file=@"id_proof.pdf"
documentType=IdentityProof
maskedNumber=XXXX1234
issueDate=2020-01-15
expiryDate=2030-01-15
notes=Passport copy
```

**Response**:
```json
{
  "success": true,
  "data": {
    "id": "guid",
    "documentType": "IdentityProof",
    "fileName": "id_proof.pdf",
    "storageKey": "tenants/guid/documents/...",
    "uploadedAtUtc": "2024-01-10T10:00:00Z",
    "status": "Uploaded"
  },
  "message": "Document uploaded successfully"
}
```

#### 3. Move-in Handover (`/api/v1/tenant-portal/move-in-handover`)

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/move-in-handover` | GET | ✅ Yes (Tenant) | Get move-in handover checklist |
| `/move-in-handover/submit` | POST | ✅ Yes (Tenant) | Submit completed handover with signature |

**Example - Submit Handover**:
```http
POST /api/v1/tenant-portal/move-in-handover/submit
Authorization: Bearer <tenant_token>
Content-Type: multipart/form-data

signatureImage=@"signature.png"
signedAtUtc=2024-01-10T10:00:00Z
```

### Authorization

All Tenant Portal endpoints require:
- ✅ Valid JWT access token
- ✅ User must have "Tenant" role
- ✅ User must be linked to a tenant record via `LinkedUserId`

**Example Authorization**:
```http
GET /api/v1/tenant-portal/lease-summary
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Error Responses

#### 401 Unauthorized
```json
{
  "success": false,
  "message": "Invalid user authentication"
}
```

**Cause**: Missing or invalid JWT token

#### 403 Forbidden
```json
{
  "success": false,
  "message": "You do not have permission to perform this action"
}
```

**Cause**: User does not have "Tenant" role

#### 404 Not Found
```json
{
  "success": false,
  "message": "No tenant found for the current user"
}
```

**Cause**: User account not linked to a tenant record

---

## 🔐 Authentication

### Obtaining a JWT Token

**Option 1: Register (Main API)**
```json
POST https://localhost:7123/api/v1/authentication/register
{
  "userName": "johndoe",
  "email": "john@example.com",
  "password": "SecurePass123!"
}
```

**Option 2: Login (Main API)**
```json
POST https://localhost:7123/api/v1/authentication/login
{
  "email": "john@example.com",
  "password": "SecurePass123!"
}
```

**Response**:
```json
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "abc123...",
  "expiresIn": 3600
}
```

### Using the Token

**All authenticated requests require the `Authorization` header**:

```http
Authorization: Bearer eyJhbGc...
```

### Token Expiration

- **Access Token**: 60 minutes (configurable)
- **Refresh Token**: 7 days (configurable)

### Refreshing Tokens

When access token expires:

```json
POST https://localhost:7123/api/v1/authentication/refresh
{
  "refreshToken": "abc123..."
}

Response 200 OK:
{
  "accessToken": "newToken...",
  "refreshToken": "newRefreshToken...",
  "expiresIn": 3600
}
```

### Token Interoperability

✅ **Tokens from Main API work on Admin API**  
✅ **Both APIs share the same JWT secret**  
✅ **No need to login separately for each API**

---

## 🔄 Common Workflows

### Workflow 1: New User Registration

```
1. User: POST /api/v1/authentication/register (Main API)
   → Get JWT tokens immediately

2. User: POST /api/v1/authentication/confirm-email (Main API)
   → Verify email (optional)

3. User: Use tokens to access protected endpoints
```

### Workflow 2: Admin Creates User

```
1. Admin: Login to Main API
   → Get JWT token

2. Admin: POST /api/v1/admin/users (Admin API)
   → Create user account

3. Admin: POST /api/v1/admin/user-roles/assign (Admin API)
   → Assign appropriate roles

4. User: Login through Main API
   → Start using the system
```

### Workflow 3: Complete System Setup

```
1. POST /api/v1/admin/initialization/initialize (Admin API)
   → Create SuperAdmin and system roles

2. SuperAdmin: Login (Main API)
   → Get JWT token

3. SuperAdmin: Create Manager account (Admin API)
   → POST /api/v1/admin/users

4. SuperAdmin: Assign Manager role (Admin API)
   → POST /api/v1/admin/user-roles/assign

5. Manager: Create products (Main API)
   → POST /api/v1/products

6. Public: Register as users (Main API)
   → POST /api/v1/authentication/register

7. Users: View/use products (Main API)
   → GET /api/v1/products
```

---

## 📝 HTTP Examples

### Main API Examples

**File**: `src/TentMan.Api/TentMan.Api.http`  
**Total**: 40+ request examples

**Categories**:
- Health checks (3 examples)
- Authentication (5 examples)
  - Register
  - Login
  - Refresh token
  - Logout
  - Confirm email
- Products (10+ examples)
  - List products
  - Get product by ID
  - Create product
  - Update product
  - Delete product
- Account management (5+ examples)
  - Change password
  - Reset password request
  - Reset password

**Usage in Visual Studio**:
1. Open `TentMan.Api.http`
2. Update `jwt_token` variable
3. Click "Send Request" next to any example

### Admin API Examples

**File**: `TentMan.AdminApi/TentMan.AdminApi.http`  
**Total**: 31 request examples

**Categories**:
- Health checks (3 examples)
- System initialization (1 example)
- User management (10+ examples)
  - List users (with pagination)
  - Create user
  - Delete user
  - Security restrictions
- Role management (5+ examples)
  - List roles
  - Create custom role
- User role management (10+ examples)
  - Get user roles
  - Assign role
  - Remove role
  - Security restrictions

**Usage in Visual Studio**:
1. Open `TentMan.AdminApi.http`
2. Update `jwt_token` variable
3. Click "Send Request" next to any example

---

## 📖 OpenAPI Documentation

### Interactive Documentation (Scalar UI)

**Main API**:
- URL: https://localhost:7123/scalar/v1
- Theme: DeepSpace
- Features:
  - Try-it-out functionality
  - JWT authorization
  - Request/response examples
  - Schema browsing
  - Code generation

**Admin API**:
- URL: https://localhost:7290/scalar/v1
- Theme: DeepSpace
- Features: Same as Main API

### OpenAPI Specifications

**Main API JSON**: https://localhost:7123/openapi/v1.json  
**Admin API JSON**: https://localhost:7290/openapi/v1.json

### Features

✅ **Comprehensive endpoint documentation**  
✅ **JWT Bearer authentication configured**  
✅ **Request/response schemas**  
✅ **Example values**  
✅ **Security requirements**  
✅ **Server URLs**  
✅ **API versioning** (v1)

---

## ⚠️ Error Handling

### Standard Error Response

All APIs return errors in a consistent format:

```json
{
  "success": false,
  "message": "Error description",
  "errors": [
    {
      "code": "ErrorCode",
      "message": "Detailed error message"
    }
  ]
}
```

### HTTP Status Codes

| Code | Meaning | Common Causes |
|------|---------|---------------|
| **200** | OK | Successful GET, PUT, DELETE |
| **201** | Created | Successful POST (resource created) |
| **400** | Bad Request | Validation error, business rule violation |
| **401** | Unauthorized | Missing/expired/invalid token |
| **403** | Forbidden | Insufficient permissions |
| **404** | Not Found | Resource doesn't exist |
| **409** | Conflict | Concurrency conflict (stale data) |
| **500** | Internal Server Error | Unexpected server error |

### Common Error Scenarios

#### 400 Bad Request
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    {
      "field": "Password",
      "message": "Password must be at least 8 characters"
    }
  ]
}
```

#### 401 Unauthorized
```json
{
  "success": false,
  "message": "Unauthorized"
}
```

**Causes**:
- Missing `Authorization` header
- Expired access token
- Invalid token

**Fix**: Obtain new token via login or refresh

#### 403 Forbidden
```json
{
  "success": false,
  "message": "You do not have permission to perform this action"
}
```

**Causes**:
- User lacks required role
- Security restriction violated

**Fix**: Ensure user has appropriate role

#### 409 Conflict
```json
{
  "success": false,
  "message": "The record has been modified by another user. Please refresh and try again."
}
```

**Cause**: Optimistic concurrency conflict

**Fix**: Refresh data and retry operation

---

## 🎯 Best Practices

### API Usage

✅ **Always include Authorization header** for protected endpoints  
✅ **Handle token expiration** gracefully (use refresh token)  
✅ **Check role requirements** before calling endpoints  
✅ **Use pagination** for large datasets  
✅ **Handle errors** appropriately  
✅ **Validate input** before sending requests  
✅ **Use HTTPS** in production  

### Security

✅ **Store tokens securely** (never in localStorage)  
✅ **Use short-lived access tokens** (15-60 minutes)  
✅ **Rotate refresh tokens** regularly  
✅ **Validate JWT signatures**  
✅ **Enforce HTTPS** in production  
✅ **Implement rate limiting** for authentication endpoints  
✅ **Monitor admin actions** through logs  

### Performance

✅ **Use pagination** for list endpoints  
✅ **Cache frequently accessed data**  
✅ **Minimize payload size**  
✅ **Use compression** (gzip)  
✅ **Implement retries** with exponential backoff  

---

---

## 💻 API Client Libraries

TentMan provides **two HTTP client libraries** for programmatic API access with built-in resilience, authentication, and type safety.

### TentMan.ApiClient (Main API Client)

**Purpose**: HTTP client for the Main API (TentMan.Api)

**Features**:
- ✅ HttpClientFactory pattern with Polly resilience
- ✅ Automatic retry with exponential backoff
- ✅ Circuit breaker pattern
- ✅ JWT authentication with auto-refresh
- ✅ Blazor Server and WebAssembly support
- ✅ Comprehensive exception handling
- ✅ Structured logging

**Installation**:
```xml
<ProjectReference Include="..\TentMan.ApiClient\TentMan.ApiClient.csproj" />
```

**Quick Start**:
```csharp
// Register in DI
builder.Services.AddApiClient(builder.Configuration);

// Use in your code
@inject IAuthenticationApiClient AuthClient
@inject IProductsApiClient ProductsClient

// Login
var loginResult = await AuthClient.LoginAsync(new LoginRequest
{
    Email = "user@example.com",
    Password = "SecurePassword123!"
});

// Get products
var products = await ProductsClient.GetProductsAsync();
```

**Available Clients**:
- `IAuthenticationApiClient` - Login, register, refresh tokens, logout
- `IProductsApiClient` - Product CRUD operations
- `IBuildingsApiClient` - Building management
- `IOwnersApiClient` - Owner management
- `ITenantsApiClient` - Tenant management
- `ILeasesApiClient` - Lease management

**Documentation**: [TentMan.ApiClient README](../src/TentMan.ApiClient/README.md)

---

### TentMan.AdminApiClient (Admin API Client)

**Purpose**: HTTP client for the Admin API (TentMan.AdminApi)

**Features**:
- ✅ HttpClientFactory pattern with Polly resilience
- ✅ Automatic retry with exponential backoff
- ✅ Circuit breaker pattern
- ✅ Comprehensive exception handling (reuses ApiClient exceptions)
- ✅ Structured logging
- ✅ Full XML documentation

**Installation**:
```xml
<ProjectReference Include="..\TentMan.AdminApiClient\TentMan.AdminApiClient.csproj" />
```

**Quick Start**:
```csharp
// Register in DI
builder.Services.AddAdminApiClient(builder.Configuration);

// Use in your code
@inject IInitializationApiClient InitializationClient
@inject IRolesApiClient RolesClient
@inject IUsersApiClient UsersClient
@inject IUserRolesApiClient UserRolesClient

// Initialize system (first-time setup)
var initResult = await InitializationClient.InitializeSystemAsync(new InitializeSystemRequest
{
    UserName = "superadmin",
    Email = "admin@example.com",
    Password = "SecurePassword123!"
});

// Get all roles
var roles = await RolesClient.GetRolesAsync();

// Create a new user
var user = await UsersClient.CreateUserAsync(new CreateUserRequest
{
    UserName = "john.doe",
    Email = "john.doe@example.com",
    Password = "SecurePassword123!",
    EmailConfirmed = true
});

// Assign role to user
await UserRolesClient.AssignRoleAsync(new AssignRoleRequest
{
    UserId = user.Data!.Id,
    RoleId = managerRoleId
});
```

**Available Clients**:
- `IInitializationApiClient` - System initialization with roles and super admin
- `IRolesApiClient` - Role management (get, create)
- `IUsersApiClient` - User management (get, create, delete)
- `IUserRolesApiClient` - User-role assignments (get, assign, remove)

**Documentation**: [TentMan.AdminApiClient README](../src/TentMan.AdminApiClient/README.md)

---

### Client Library Configuration

Both client libraries support extensive configuration via `appsettings.json`:

**ApiClient Configuration**:
```json
{
  "ApiClient": {
    "BaseUrl": "https://localhost:7123",
    "TimeoutSeconds": 30,
    "RetryCount": 3,
    "EnableCircuitBreaker": true,
    "CircuitBreakerFailureThreshold": 5,
    "CircuitBreakerDurationSeconds": 30
  },
  "Authentication": {
    "AutoAttachToken": true,
    "AutoRefreshToken": true,
    "UseBrowserStorage": true
  }
}
```

**AdminApiClient Configuration**:
```json
{
  "AdminApiClient": {
    "BaseUrl": "https://localhost:7290",
    "TimeoutSeconds": 30,
    "RetryCount": 3,
    "EnableCircuitBreaker": true,
    "CircuitBreakerFailureThreshold": 5,
    "CircuitBreakerDurationSeconds": 30
  }
}
```

### Exception Handling

Both libraries share the same exception types:

```csharp
try
{
    var user = await UsersClient.CreateUserAsync(request);
}
catch (ValidationException ex)
{
    // 400/422 - validation errors
    foreach (var error in ex.Errors)
    {
        Console.WriteLine(error);
    }
}
catch (AuthorizationException ex)
{
    // 401/403 - auth errors
    // Re-authenticate or show access denied
}
catch (ResourceNotFoundException ex)
{
    // 404 - not found
}
catch (ServerException ex)
{
    // 5xx - server errors
}
catch (ApiClientException ex)
{
    // Other API errors
}
```

---

## 📚 Related Documentation

- **[GETTING_STARTED.md](GETTING_STARTED.md)** - Initial setup guide
- **[AUTHENTICATION_GUIDE.md](AUTHENTICATION_GUIDE.md)** - JWT and authentication details
- **[AUTHORIZATION_GUIDE.md](AUTHORIZATION_GUIDE.md)** - Role-based authorization
- **[TENANT_LEASE_MANAGEMENT.md](TENANT_LEASE_MANAGEMENT.md)** - Tenant and Lease API details
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - System architecture
- **[Main API README](../src/TentMan.Api/README.md)** - Main API project details
- **[Admin API README](../TentMan.AdminApi/README.md)** - Admin API project details

---

**Last Updated**: 2026-01-09  
**Version**: 1.1  
**Total Endpoints**: 38 (26 Main + 12 Admin)  
**Maintainer**: TentMan Development Team
