# TentMan - Tenant Management System

A modern, cloud-native **Tenant Management System** built with Clean Architecture and .NET Aspire. TentMan provides comprehensive multi-tenancy support for managing tenants, their data, and ensuring secure data isolation in SaaS applications.

## 🎯 What is TentMan?

TentMan is a production-ready **Tenant Management System** designed for multi-tenant SaaS applications. It provides:

- **Tenant Onboarding**: Streamlined tenant registration and setup
- **Data Isolation**: Secure separation of tenant data
- **User Management**: Role-based access control per tenant
- **Multi-tenancy Support**: Efficient handling of multiple tenants
- **Tenant Administration**: Complete tenant lifecycle management

## 🚀 Quick Start

```bash
# Clone the repository
git clone https://github.com/chethandvg/TenantManagement.git
cd TenantManagement

# Run the application with Aspire orchestration
cd src/TentMan.AppHost
dotnet run
```

The Aspire Dashboard will open automatically, showing all running services.

- **API**: http://localhost:5000
- **Scalar API Docs**: http://localhost:5000/scalar/v1
- **Aspire Dashboard**: http://localhost:15XXX (check console output)

## 📚 Documentation

### Essential Reading
- **[Documentation Hub](docs/README.md)** - Start here for all documentation
- **[Architecture Guide](docs/ARCHITECTURE.md)** - Understanding the solution structure
- **[Security & Privacy](docs/SECURITY.md)** - Security controls and best practices
- **[Concurrency Guide](docs/CONCURRENCY_GUIDE.md)** - Data integrity and optimistic concurrency
- **[Adding New Entities](src/README_NEW_ENTITY.md)** - Step-by-step development guide

### Quick Links
| Topic | Document |
|-------|----------|
| 🏗️ Architecture & Design | [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) |
| 🔒 Authorization & Access Control | [docs/AUTHORIZATION_GUIDE.md](docs/AUTHORIZATION_GUIDE.md) |
| 🔒 Security & Privacy | [docs/SECURITY.md](docs/SECURITY.md) |
| 🔒 Concurrency & Data | [docs/CONCURRENCY_GUIDE.md](docs/CONCURRENCY_GUIDE.md) |
| ➕ Adding Features | [src/README_NEW_ENTITY.md](src/README_NEW_ENTITY.md) |
| 📖 API Reference | [src/TentMan.Api/README.md](src/TentMan.Api/README.md) |

## 🏗️ Architecture

TentMan is a **Tenant Management System** built following **Clean Architecture** principles with clear separation of concerns:

```
┌─────────────────────────┐
│   TentMan.AppHost         │  .NET Aspire orchestration
└────────┬────────────────┘
         │
    ┌────▼─────┐
    │ TentMan.Api│  ASP.NET Core Web API
    └────┬─────┘
         │
         ├─ TentMan.Infrastructure  (EF Core, Repositories)
         │     └─ TentMan.Application  (CQRS, Use Cases)
         │           └─ TentMan.Domain  (Entities, Business Logic)
         │
         ├─ TentMan.Contracts  (DTOs)
         └─ TentMan.ServiceDefaults  (Aspire defaults)
```

**Key Principles:**
- ✅ Clean Architecture with dependency inversion
- ✅ CQRS with MediatR
- ✅ Optimistic concurrency control
- ✅ Soft delete for data preservation
- ✅ Automatic audit tracking
- ✅ .NET Aspire for cloud-native development

## 🛠️ Tech Stack

| Category | Technologies |
|----------|-------------|
| **Framework** | .NET 9, ASP.NET Core |
| **Database** | Entity Framework Core 9, SQL Server |
| **Architecture** | Clean Architecture, CQRS |
| **Cloud-Native** | .NET Aspire, OpenTelemetry |
| **API Docs** | Scalar (OpenAPI) |
| **UI** | Blazor with MudBlazor |

## 📋 Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for Aspire AppHost)
- SQL Server or Docker
- Visual Studio 2022 / Rider / VS Code

## 🎯 Key Features

### Property Management 🏢 UPDATED!
- **Multi-Organization Support**: Complete organization isolation for multi-tenancy
- **Building Management**: Track properties with addresses, types, and metadata
- **Unit Management**: Manage individual units (flats, shops, offices) within buildings
- **Ownership Tracking**: Support for full and co-ownership with percentage shares
- **Ownership Validation**: Automated validation ensuring shares sum to 100%
- **Utility Meters**: Track electricity, water, and gas meters per unit
- **Indian Tax IDs**: Support for PAN and GSTIN identifiers
- **File Attachments**: Metadata storage for documents and photos
- **Blazor WASM Frontend** ✨: Complete UI screens for buildings and owners management
  - Buildings List with search, filters, and card/grid views
  - 5-step Create Building Wizard (Info → Address → Units → Ownership → Documents)
  - Building Details with tabbed interface (Units, Ownership, Documents)
  - Owners List with add/edit functionality
  - Reusable MudBlazor components (OwnershipEditor, FileUploader, SearchFilterPanel)

📚 **[Read the Property Management Guide →](docs/PROPERTY_MANAGEMENT.md)**

### Tenant Management 👥 NEW!
- **Tenant Registry**: Complete tenant (renter) management with profile, addresses, documents
- **Tenant Search**: Search by phone number or name with 600ms debounce
- **Document Management** ✨: Full document upload and management for tenants
  - Upload documents via tenant portal (ID proofs, address proofs, photos, etc.)
  - File validation (type, size up to 10MB)
  - Support for PDF, JPEG, PNG, DOC, DOCX formats
  - Document metadata with masked numbers, dates, and notes
  - Secure storage in **Azure Blob Storage** with SHA256 hash verification
  - List and view uploaded documents with status tracking
- **Lease History**: View tenant's lease history with status tracking
- **Tenant Invite System** ✨: Secure invite-based tenant onboarding and management
  - Generate cryptographically secure invite tokens (32-char hex)
  - Email validation and uniqueness checks
  - Configurable expiry (1-90 days, default 7 days)
  - Automatic role assignment ("Tenant" role)
  - User-tenant account linking
  - **Invite Management** ✨ NEW!
    - List all invites for a tenant with status tracking (Pending, Used, Expired)
    - Cancel pending invites
    - Copy invite links to clipboard
    - View invite history (created date, expiry, used date)
  - Optimistic concurrency control
- **Tenant Portal Pages** ✨: Self-service portal for tenants
  - **Accept Invite** ✨: Complete invite acceptance flow
    - Token validation with real-time feedback
    - User account creation (username, email, password)
    - Automatic "Tenant" role assignment
    - Automatic linking to tenant record via LinkedUserId
    - JWT token generation and auto-login
    - Redirect to tenant dashboard after successful signup
  - **Tenant Dashboard** ✨: Personalized overview for tenants
    - Active lease summary display
    - Quick access to lease details, documents, and handover
    - **Role-aware navigation** - Only shown to users with "Tenant" role
  - **Lease Summary**: Read-only view of active lease details
  - **Document Upload**: Drag-and-drop interface for document management
  - **Move-in Handover Checklist** ✨:
    - Digital signature pad using HTML5 Canvas
    - Checklist items with condition tracking (Good, Ok, Bad, Missing)
    - Meter readings display (Electricity, Water, Gas)
    - Photo/document attachments for checklist items
    - Signature image capture and submission
    - Read-only view after completion
    - Secure signature storage in Azure Blob Storage
- **Blazor WASM Frontend** ✨: Complete UI screens for tenant management
  - Tenants List with search by phone/name
  - Tenant Details with tabbed interface (Profile, Addresses, Documents, Leases, **Invites** ✨)
  - Add/Edit tenant dialogs
  - **Generate Invite Dialog** ✨ NEW!
    - Configurable expiry days (1-90 days)
    - Instant invite link generation
    - One-click copy to clipboard
    - Display of invite details (phone, email, expiry)
  - **Invites Tab** ✨ NEW!
    - View all invites with status badges (Pending, Used, Expired)
    - Copy invite URLs to share with tenants
    - Cancel pending invites with confirmation
    - Track invite usage history

📚 **[Read the Tenant & Lease Management Guide →](docs/TENANT_LEASE_MANAGEMENT.md)**  
📚 **[Read the Tenant Invite System Guide →](docs/TENANT_INVITE_SYSTEM.md)**

### Billing Engine 💰 NEW!
- **Flexible Charge Types**: System-defined (RENT, MAINT, ELEC, WATER, GAS, LATE_FEE, ADJUSTMENT) and custom charge types
- **Recurring Charges**: Monthly, quarterly, yearly, and one-time charges per lease
- **Utility Billing**: 
  - **Meter-based**: Calculate charges using rate plans with tiered slabs
  - **Amount-based**: Direct billing from provider invoices
  - Support for Electricity, Water, and Gas utilities
- **Invoice Management**: Automated and manual invoice generation with line items
- **Credit Notes**: Issue credits for refunds, adjustments, and corrections
- **Batch Billing**: Invoice runs for generating invoices across multiple leases
- **Payment Tracking**: Track paid, partially paid, and overdue invoices
- **Database Schema**: 12 new tables with foreign keys and indexes for optimal performance

📚 **[Read the Billing Engine Guide →](docs/BILLING_ENGINE.md)**

### Lease Management 📝 NEW!
- **7-Step Lease Creation Wizard**: Guided workflow for creating leases
  - Step 1: Select Unit (dropdown with unit summary)
  - Step 2: Dates & Rules (start/end, due day, grace period, late fee, notice)
  - Step 3: Add Parties (search existing or create inline, role assignment)
  - Step 4: Financial Terms (rent, deposit, maintenance, escalation)
  - Step 5: Documents (upload lease agreement and proofs)
  - Step 6: Move-in Handover (checklist, meter readings, photos)
  - Step 7: Review & Activate (validation summary and confirmation)
- **Party Management**: Add tenants with roles (Primary, Co-Tenant, Occupant, Guarantor)
- **Financial Terms**: Monthly rent, security deposit, maintenance charges, escalation rules
- **Validation Summary**: Comprehensive validation before lease activation

### Multi-Tenant Architecture
- **Multi-Tenant Architecture**: Support for unlimited tenants with data isolation
- **Tenant Provisioning**: Automated tenant setup and configuration
- **Tenant-Specific Settings**: Customizable configurations per tenant
- **Tenant Lifecycle**: Complete onboarding, management, and offboarding workflows
- **Cross-Tenant Administration**: Centralized management of all tenants

### Security & Access Control ✨ ENHANCED!
- **Policy-Based Authorization**: Consistent authorization using centralized policy constants
  - All authorization constants in `TentMan.Shared.Constants.Authorization`
  - Role hierarchy support (SuperAdmin > Administrator > Manager > User)
  - Permission-based policies for fine-grained access control
- **Role-Based Access Control (RBAC)**: Granular permissions per tenant
  - **SuperAdmin/Administrator**: Full system access and user management
  - **Owner/Manager**: Full CRUD operations on tenants, leases, and resources
  - **Tenant**: Read-only access to their own leases, can upload documents
  - **User**: Standard authenticated access
- **JWT Authentication**: Secure token-based authentication with refresh tokens
- **Authorization Policies**: Resource-based authorization for leases and files
- **Data Masking**: Automatic masking of sensitive document numbers (****1234)
- **Secure File Access**: Short-lived signed URLs (Azure Blob SAS) for temporary file access
- **File Authorization**: All file downloads require authentication and authorization checks
- **Data Isolation**: Complete separation of tenant data
- **Admin APIs**: Secure administrative endpoints for system management

📚 **[Read the Authorization Guide →](docs/AUTHORIZATION_GUIDE.md)** | **[Security & Privacy Guide →](docs/SECURITY.md)**

### Audit Logging & Compliance ✨ NEW!
- **Automatic Audit Logging**: All changes to Lease, LeaseTerm, and DepositTransaction are logged
- **Complete Audit Trail**: Records who, when, what changed, and before/after state
- **File Access Logging**: All file access attempts (successful and denied) are logged
- **Compliance Ready**: Audit logs support GDPR and regulatory requirements

### Data Integrity
- **Optimistic Concurrency**: Prevents lost updates using SQL Server `rowversion`
- **Soft Delete**: Preserves data history instead of physical deletion
- **Audit Tracking**: Automatic tracking of who changed what and when
- **Tenant-Aware Queries**: Automatic tenant filtering on all queries

### Developer Experience
- **Aspire Dashboard**: Real-time monitoring of all services
- **Hot Reload**: Fast development iteration
- **Scalar API Docs**: Interactive API documentation
- **Structured Logging**: Built-in OpenTelemetry integration

### Code Quality
- **Clean Architecture**: Testable, maintainable, framework-independent
- **CQRS Pattern**: Clear separation of reads and writes
- **Repository Pattern**: Abstracted data access
- **Result Pattern**: Explicit success/failure handling

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## 🔧 Common Tasks

### Create a Migration
```bash
cd src/TentMan.Infrastructure
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

### Add a New Entity
Follow the guide: [src/README_NEW_ENTITY.md](src/README_NEW_ENTITY.md)

1. Create entity in `TentMan.Domain`
2. Create repository interface in `TentMan.Application`
3. Implement repository in `TentMan.Infrastructure`
4. Create DTOs and commands/queries
5. Add controller endpoints
6. Create migration

## 🚀 Deployment

### Local Development
Already covered in Quick Start above.

### Azure (via Aspire)
```bash
azd init
azd up
```

### Docker
```bash
dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer
```

## 🗂️ Project Structure

```
TentMan/
├── docs/                          # All documentation
│   ├── README.md                  # Documentation hub
│   ├── ARCHITECTURE.md            # Architecture guide
│   └── CONCURRENCY_GUIDE.md       # Data integrity guide
├── src/
│   ├── TentMan.Domain/              # Business logic (no dependencies)
│   ├── TentMan.Application/         # Use cases, CQRS handlers
│   ├── TentMan.Infrastructure/      # EF Core, repositories
│   ├── TentMan.Contracts/           # API DTOs
│   ├── TentMan.Api/                 # REST API
│   ├── TentMan.Ui/                  # Blazor components
│   ├── TentMan.ServiceDefaults/     # Aspire defaults
│   ├── TentMan.AppHost/             # Aspire orchestrator
│   └── README_NEW_ENTITY.md       # Development guide
└── README.md                      # This file
```

## 🤝 Contributing

See **[CONTRIBUTING.md](CONTRIBUTING.md)** for complete coding guidelines including:

- **Code organization rules** (300 LOC limit per .cs file, partial class usage)
- **Frontend component guidelines** (modular Blazor components, code-behind pattern)
- **Naming conventions** and file structure
- **Testing requirements**

### Quick Guidelines

1. Follow Clean Architecture principles
2. Keep .cs files under 300 lines (use partial classes if needed)
3. Use code-behind pattern for Blazor components
4. Include concurrency control for updates
5. Write tests for new features
6. Update documentation
7. Use consistent patterns from existing code

See [docs/README.md](docs/README.md) for documentation hub.

## 📄 License

[Your License Here]

## 🙋 Support

- **Documentation**: Start with [docs/README.md](docs/README.md)
- **Issues**: Report on [GitHub Issues](https://github.com/chethandvg/TenantManagement/issues)
- **Architecture Questions**: See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- **Concurrency Questions**: See [docs/CONCURRENCY_GUIDE.md](docs/CONCURRENCY_GUIDE.md)

---

**Project Type**: Tenant Management System for Multi-Tenant SaaS Applications  
**Maintained by**: TentMan Development Team  
**Repository**: https://github.com/chethandvg/TenantManagement  
**Last Updated**: 2026-01-08
