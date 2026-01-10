# TentMan Documentation Hub

Welcome to the TentMan documentation! This hub provides links to all documentation resources.

---

## 📚 Quick Navigation

### Essential Guides (Start Here)

1. **[Getting Started Guide](GETTING_STARTED.md)** ⚡ **START HERE**
   - Complete setup in 10 minutes
   - JWT configuration
   - Database seeding
   - Testing your setup

2. **[Contributing Guide](../CONTRIBUTING.md)** 🤖 **FOR CODING AGENTS**
   - Code organization rules (300 LOC limit)
   - Backend guidelines (partial classes, naming conventions)
   - Frontend guidelines (modular components, code-behind pattern)
   - Testing requirements

3. **[Architecture Guide](ARCHITECTURE.md)**
   - Clean Architecture explained
   - Project structure
   - Design patterns

4. **[API Guide](API_GUIDE.md)**
   - Complete API reference for both Main API and Admin API
   - All endpoints documented
   - Authentication flows
   - Common workflows
   - Error handling

5. **[Property Management Guide](PROPERTY_MANAGEMENT.md)** 🏢 **UPDATED!**
   - Multi-organization property management
   - Buildings, units, and ownership tracking
   - Complete API reference
   - **Blazor WASM Frontend screens** ✨
   - Business rules and validation
   - Usage examples

6. **[Tenant and Lease Management Guide](TENANT_LEASE_MANAGEMENT.md)** 📋 **UPDATED!**
   - Tenant profiles and document management
   - Lease creation and activation workflow
   - One active lease per unit constraint
   - Multi-tenant leases (Primary, CoTenant, Occupant, Guarantor)
   - Versioned financial terms (rent, deposit, escalation)
   - Deposit ledger tracking
   - Lease activation validation rules
   - **Blazor WASM Frontend screens** ✨ NEW!

7. **[Billing Engine Guide](BILLING_ENGINE.md)** 💰 **NEW!**
   - Flexible charge types and recurring charges
   - Utility billing (meter-based and amount-based)
   - Invoice generation and payment tracking
   - Credit notes and adjustments
   - Batch billing with invoice runs
   - Complete database schema reference

8. **[Tenant Invite System Guide](TENANT_INVITE_SYSTEM.md)** 🔐 **NEW!**
   - Secure invite-based tenant onboarding
   - Token generation and validation
   - Email validation and uniqueness checks
   - Role assignment and user-tenant linking
   - Tenant portal pages (Dashboard, Documents, Handover)
   - Complete API reference
   - Security features and best practices

8. **[Authentication Guide](AUTHENTICATION_GUIDE.md)**
   - JWT configuration
   - Token management
   - Security best practices
   - Troubleshooting

9. **[Authorization Guide](AUTHORIZATION_GUIDE.md)**
   - Role-based access control
   - Security restrictions
   - Policy configuration

10. **[Password Security Guide](PASSWORD_SECURITY_GUIDE.md)**
    - Password policies
    - Complexity rules
    - Validation implementation

11. **[Database Guide](DATABASE_GUIDE.md)**
    - Database setup
    - Migrations
    - Seeding
    - Retry strategy

12. **[Development Guide](DEVELOPMENT_GUIDE.md)**
    - Development workflow
    - Code patterns
    - Best practices
    - Testing

13. **[Project Structure](PROJECT_STRUCTURE.md)**
    - Directory organization
    - File conventions

14. **[Archive](ARCHIVE.md)**
    - Historical documentation
    - Implementation summaries
    - Migration guides

---

## 🎯 Documentation by Task

### I want to get started...
1. ✅ **[Getting Started Guide](GETTING_STARTED.md)** - Complete setup (10 minutes)
2. ✅ **[Contributing Guide](../CONTRIBUTING.md)** - Coding guidelines
3. ✅ **[Architecture Guide](ARCHITECTURE.md)** - Understand the system (15 minutes)
4. ✅ **[API Guide](API_GUIDE.md)** - Explore the APIs (20 minutes)

### I want to use the API...
1. ✅ **[API Guide](API_GUIDE.md)** - Complete API reference
2. ✅ **HTTP Request Files** - `src/TentMan.Api/TentMan.Api.http` (40+ examples)
3. ✅ **Scalar UI** - https://localhost:7123/scalar/v1 (interactive docs)

### I want to develop features...
1. ✅ **[Development Guide](DEVELOPMENT_GUIDE.md)** - Development workflow
2. ✅ **[New Entity Guide](../src/README_NEW_ENTITY.md)** - Step-by-step tutorial
3. ✅ **[Architecture Guide](ARCHITECTURE.md)** - Design patterns

### I want to manage security...
1. ✅ **[Authentication Guide](AUTHENTICATION_GUIDE.md)** - JWT setup
2. ✅ **[Authorization Guide](AUTHORIZATION_GUIDE.md)** - Role management
3. ✅ **[Password Security Guide](PASSWORD_SECURITY_GUIDE.md)** - Password policies

### I want to work with the database...
1. ✅ **[Database Guide](DATABASE_GUIDE.md)** - Complete database guide
2. ✅ **[Development Guide](DEVELOPMENT_GUIDE.md)** - Migrations and patterns

---

## 📖 Documentation by Audience

### For Coding Agents 🤖
**Essential reading for AI-assisted development:**
1. **[Contributing Guide](../CONTRIBUTING.md)** - **REQUIRED** - Code organization rules ⚡
2. **[Architecture Guide](ARCHITECTURE.md)** - Understand the structure
3. **Project READMEs** - Each `src/` folder has a README.md with specific guidelines
4. **[New Entity Guide](../src/README_NEW_ENTITY.md)** - Step-by-step feature development

**Key Rules:**
- Keep `.cs` files under **300 lines** (+30 max variance)
- Keep `.razor` files under **200 lines** (+20 max variance)
- Use **partial classes** when files exceed limits
- Use **code-behind pattern** for Blazor components
- Follow existing patterns in the codebase

### For New Developers
**Start here to understand the project:**
1. **[Getting Started Guide](GETTING_STARTED.md)** - Get running in 10 minutes ⚡
2. **[Contributing Guide](../CONTRIBUTING.md)** - Coding guidelines
3. **[Architecture Guide](ARCHITECTURE.md)** - Understand the structure
4. **[API Guide](API_GUIDE.md)** - Learn the APIs
5. **[Development Guide](DEVELOPMENT_GUIDE.md)** - Development patterns

**Total onboarding time**: ~45 minutes

### For Frontend Developers
**API integration and usage:**
1. **[Contributing Guide](../CONTRIBUTING.md)** - Frontend component guidelines
2. **[API Guide](API_GUIDE.md)** - Complete API reference
3. **[Authentication Guide](AUTHENTICATION_GUIDE.md)** - JWT tokens
4. **HTTP Examples** - `src/TentMan.Api/TentMan.Api.http`
5. **Scalar UI** - https://localhost:7123/scalar/v1

### For Backend Developers
**Implementation and architecture:**
1. **[Contributing Guide](../CONTRIBUTING.md)** - Backend coding rules
2. **[Architecture Guide](ARCHITECTURE.md)** - System design
3. **[Development Guide](DEVELOPMENT_GUIDE.md)** - Code patterns
4. **[Database Guide](DATABASE_GUIDE.md)** - Data access
5. **[New Entity Guide](../src/README_NEW_ENTITY.md)** - Feature development

### For Administrators
**System management:**
1. **[Getting Started Guide](GETTING_STARTED.md)** - Initial setup
2. **[API Guide](API_GUIDE.md)** - Admin API reference
3. **[Authorization Guide](AUTHORIZATION_GUIDE.md)** - User/role management
4. **[Database Guide](DATABASE_GUIDE.md)** - Database management

### For Security Auditors
**Security features and practices:**
1. **[Authentication Guide](AUTHENTICATION_GUIDE.md)** - JWT security
2. **[Authorization Guide](AUTHORIZATION_GUIDE.md)** - Access control
3. **[Password Security Guide](PASSWORD_SECURITY_GUIDE.md)** - Password policies
4. **[API Guide](API_GUIDE.md)** - Security restrictions

---

## 🏗️ Project Structure

```
TentMan/
├── docs/                                   # 📚 Documentation (12 files)
│   ├── README.md                           # This file - Documentation hub
│   ├── GETTING_STARTED.md                  # ⚡ Start here - Complete setup guide
│   ├── ARCHITECTURE.md                     # System architecture
│   ├── PROJECT_STRUCTURE.md                # Directory organization
│   ├── API_GUIDE.md                        # Complete API reference
│   ├── AUTHENTICATION_GUIDE.md             # JWT and authentication
│   ├── AUTHORIZATION_GUIDE.md              # Role-based access control
│   ├── PASSWORD_SECURITY_GUIDE.md          # Password policies
│   ├── DATABASE_GUIDE.md                   # Database and migrations
│   ├── DEVELOPMENT_GUIDE.md                # Development workflow
│   └── ARCHIVE.md                          # Historical documentation
├── src/
│   ├── TentMan.Api/                          # 🌐 Main REST API
│   │   ├── TentMan.Api.http                  # 40+ HTTP examples
│   │   └── README.md                       # API project documentation
│   ├── TentMan.AdminApi/                     # 🛡️ Admin API
│   │   ├── TentMan.AdminApi.http             # 31 HTTP examples
│   │   └── README.md                       # Admin API documentation
│   ├── TentMan.Domain/                       # 💼 Business logic
│   ├── TentMan.Application/                  # 🎯 Use cases
│   ├── TentMan.Infrastructure/               # 🔌 Data access
│   ├── TentMan.Contracts/                    # 📝 API DTOs
│   ├── TentMan.Ui/                           # 🎨 Blazor components
│   ├── TentMan.ServiceDefaults/              # ⚙️ Aspire defaults
│   ├── TentMan.AppHost/                      # 🚀 Aspire orchestrator
│   └── README_NEW_ENTITY.md                # Development tutorial
└── README.md                               # Project overview
```

---

## 🔑 Key Concepts

### Clean Architecture
- **Domain**: Business entities and logic (no dependencies)
- **Application**: Use cases and abstractions
- **Infrastructure**: Database, authentication, external services
- **API**: REST endpoints and presentation

**[Learn more →](ARCHITECTURE.md)**

### CQRS Pattern
- **Commands**: Create, Update, Delete operations
- **Queries**: Read operations
- Uses MediatR for request handling

**[Learn more →](DEVELOPMENT_GUIDE.md)**

### JWT Authentication
- Access tokens for API requests (short-lived: 15-60 min)
- Refresh tokens for re-authentication (long-lived: 7-30 days)
- Secure secret management with User Secrets or Azure Key Vault

**[Learn more →](AUTHENTICATION_GUIDE.md)**

### Role-Based Authorization
- 5 system roles: Guest, User, Manager, Administrator, SuperAdmin
- Policy-based authorization
- Security restrictions prevent privilege escalation

**[Learn more →](AUTHORIZATION_GUIDE.md)**

---

## 🛠️ Common Tasks

### Running the Application

```bash
cd src/TentMan.AppHost
dotnet run
```

**Access Points:**
- Main API: https://localhost:7123
- Admin API: https://localhost:7290
- Main API Docs: https://localhost:7123/scalar/v1
- Admin API Docs: https://localhost:7290/scalar/v1

### Setting Up Authentication

```bash
cd src/TentMan.Api
dotnet user-secrets set "Jwt:Secret" "YourSecure64CharacterSecretKey"
```

**[Full guide →](GETTING_STARTED.md)**

### Testing the API

```bash
# Open HTTP files in Visual Studio
# src/TentMan.Api/TentMan.Api.http (40+ examples)
# TentMan.AdminApi/TentMan.AdminApi.http (31 examples)

# Or use Scalar UI
https://localhost:7123/scalar/v1  # Main API
https://localhost:7290/scalar/v1  # Admin API
```

**[Full guide →](API_GUIDE.md)**

### Creating a Migration

```bash
cd src/TentMan.Infrastructure
dotnet ef migrations add YourMigrationName --startup-project ../TentMan.Api
dotnet ef database update --startup-project ../TentMan.Api
```

**[Full guide →](DATABASE_GUIDE.md)**

### Adding a New Feature

Follow the step-by-step guide: **[Adding New Entities](../src/README_NEW_ENTITY.md)**

Remember to follow the coding guidelines in **[CONTRIBUTING.md](../CONTRIBUTING.md)**.

---

## 📊 Documentation Statistics

| Metric | Value |
|--------|-------|
| **Total Documentation Files** | 14 essential docs + project READMEs |
| **Project README Files** | 11 project-specific guides |
| **Quick Start Time** | 10 minutes |
| **Full Onboarding Time** | 45 minutes |
| **HTTP Request Examples** | 80+ examples |
| **API Endpoints** | 38 total (26 Main + 12 Admin) |

---

## 📚 External Resources

| Topic | Resource |
|-------|----------|
| **.NET 9** | [Official Docs](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9) |
| **Clean Architecture** | [Martin Fowler](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) |
| **.NET Aspire** | [Official Docs](https://learn.microsoft.com/en-us/dotnet/aspire/) |
| **OpenAPI/Swagger** | [Official Docs](https://swagger.io/specification/) |
| **Scalar UI** | [GitHub](https://github.com/scalar/scalar) |
| **JWT Best Practices** | [RFC 8725](https://datatracker.ietf.org/doc/html/rfc8725) |
| **Entity Framework Core** | [Microsoft Docs](https://learn.microsoft.com/en-us/ef/core/) |

---

## 🤝 Getting Help

- **Coding guidelines**: See [CONTRIBUTING.md](../CONTRIBUTING.md)
- **Getting started**: See [GETTING_STARTED.md](GETTING_STARTED.md)
- **API questions**: See [API_GUIDE.md](API_GUIDE.md)
- **Architecture questions**: See [ARCHITECTURE.md](ARCHITECTURE.md)
- **Authentication questions**: See [AUTHENTICATION_GUIDE.md](AUTHENTICATION_GUIDE.md)
- **Authorization questions**: See [AUTHORIZATION_GUIDE.md](AUTHORIZATION_GUIDE.md)
- **Development questions**: See [DEVELOPMENT_GUIDE.md](DEVELOPMENT_GUIDE.md)
- **Database questions**: See [DATABASE_GUIDE.md](DATABASE_GUIDE.md)

---

## 📅 Version History

| Version | Date | Changes |
|---------|------|---------|
| 6.1 | 2026-01-09 | **Added Blazor UI for Tenant & Lease Management** - Tenants list, tenant details, 7-step lease creation wizard |
| 6.0 | 2026-01-09 | **Added Tenant and Lease Management** - Complete guide with API endpoints, business rules, and database schema |
| 5.0 | 2026-01-08 | **Added CONTRIBUTING.md** with coding agent guidelines, project READMEs |
| 4.0 | 2025-01-22 | **Major consolidation** (51 files → 12 files, 76% reduction) |
| 3.0 | 2025-01-22 | Major API documentation overhaul (7 new docs, 71+ HTTP examples) |
| 2.3 | 2025-01-22 | Added password policy and database seeding guides |
| 2.2 | 2025-01-22 | Added JWT configuration guides |
| 2.1 | 2025-01-22 | Added security fixes documentation |
| 2.0 | 2025-01-22 | Major documentation overhaul |
| 1.0 | 2025-01-17 | Initial documentation |

---

**Last Updated**: 2026-01-09  
**Version**: 6.1 ⚡ **WITH TENANT & LEASE MANAGEMENT UI**  
**Maintainer**: TentMan Development Team  
**Questions?** Open an issue on [GitHub](https://github.com/chethandvg/tentman/issues)
