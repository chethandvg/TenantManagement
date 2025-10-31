# Archu Documentation Hub

Welcome to the Archu documentation! This hub provides links to all documentation resources.

---

## 📚 Quick Navigation

### Essential Guides (Start Here)

1. **[Getting Started Guide](GETTING_STARTED.md)** ⚡ **START HERE**
 - Complete setup in 10 minutes
   - JWT configuration
   - Database seeding
   - Testing your setup

2. **[Architecture Guide](ARCHITECTURE.md)**
   - Clean Architecture explained
 - Project structure
   - Design patterns

3. **[Application & Infrastructure Quick Reference](APPLICATION_INFRASTRUCTURE_QUICK_REFERENCE.md)**
   - Layer comparison and dependency flow
   - Key abstractions & implementations
   - CQRS flow examples
   - Quick command reference

4. **[API Guide](API_GUIDE.md)**
   - Complete API reference for both Main API and Admin API
   - All endpoints documented
   - Authentication flows
   - Common workflows
   - Error handling

5. **[Authentication Guide](AUTHENTICATION_GUIDE.md)**
   - JWT configuration
   - Token management
   - Security best practices
   - Troubleshooting

6. **[Authorization Guide](AUTHORIZATION_GUIDE.md)**
   - Role-based access control
   - Security restrictions
   - Policy configuration

7. **[Password Security Guide](PASSWORD_SECURITY_GUIDE.md)**
   - Password policies
   - Complexity rules
   - Validation implementation

8. **[Database Guide](DATABASE_GUIDE.md)**
   - Database setup
   - Migrations
   - Seeding
   - Retry strategy

9. **[Development Guide](DEVELOPMENT_GUIDE.md)**
   - Development workflow
   - Code patterns
- Best practices
   - Testing

10. **[Project Structure](PROJECT_STRUCTURE.md)**
    - Directory organization
    - File conventions

11. **[Archive](ARCHIVE.md)**
    - Historical documentation
    - Implementation summaries
    - Migration guides

---

## 🎯 Documentation by Task

### I want to get started...
1. ✅ **[Getting Started Guide](GETTING_STARTED.md)** - Complete setup (10 minutes)
2. ✅ **[Architecture Guide](ARCHITECTURE.md)** - Understand the system (15 minutes)
3. ✅ **[API Guide](API_GUIDE.md)** - Explore the APIs (20 minutes)

### I want to use the API...
1. ✅ **[API Guide](API_GUIDE.md)** - Complete API reference
2. ✅ **HTTP Request Files** - `src/Archu.Api/Archu.Api.http` (40+ examples)
3. ✅ **Scalar UI** - https://localhost:7123/scalar/v1 (interactive docs)

### I want to build a frontend...
1. ✅ **[Archu.Web README](../src/Archu.Web/README.md)** - Blazor WebAssembly application ⭐ NEW
2. ✅ **[Archu.ApiClient README](../src/Archu.ApiClient/README.md)** - HTTP client library
3. ✅ **[Authentication Framework](../src/Archu.ApiClient/Authentication/README.md)** - JWT authentication ⭐ NEW
4. ✅ **[Archu.Ui README](../src/Archu.Ui/README.md)** - Shared component library
5. ✅ **[API Guide](API_GUIDE.md)** - API endpoints and contracts

### I want to develop features...
1. ✅ **[Development Guide](DEVELOPMENT_GUIDE.md)** - Development workflow
2. ✅ **[New Entity Guide](../src/README_NEW_ENTITY.md)** - Step-by-step tutorial
3. ✅ **[Architecture Guide](ARCHITECTURE.md)** - Design patterns
4. ✅ **[Application Layer](../src/Archu.Application/README.md)** - CQRS & use cases
5. ✅ **[Infrastructure Layer](../src/Archu.Infrastructure/README.md)** - Data access & repositories
6. ✅ **[Quick Reference](APPLICATION_INFRASTRUCTURE_QUICK_REFERENCE.md)** - Layer comparison
7. ✅ **[Archu.AppHost](../src/Archu.AppHost/README.md)** - Local development orchestration ⭐ NEW
8. ✅ **[Archu.ServiceDefaults](../src/Archu.ServiceDefaults/README.md)** - Shared configuration ⭐ NEW

### I want to manage security...
1. ✅ **[Authentication Guide](AUTHENTICATION_GUIDE.md)** - JWT setup
2. ✅ **[Authorization Guide](AUTHORIZATION_GUIDE.md)** - Role management
3. ✅ **[Password Security Guide](PASSWORD_SECURITY_GUIDE.md)** - Password policies

### I want to work with the database...
1. ✅ **[Database Guide](DATABASE_GUIDE.md)** - Complete database guide
2. ✅ **[Development Guide](DEVELOPMENT_GUIDE.md)** - Migrations and patterns
3. ✅ **[Infrastructure Layer](../src/Archu.Infrastructure/README.md)** - Repository implementations

### I want to test the application... ⭐ NEW
1. ✅ **[Integration Tests](../tests/Archu.IntegrationTests/README.md)** - API integration testing
2. ✅ **[API Client Tests](../tests/Archu.ApiClient.Tests/README.md)** - HTTP client unit tests
3. ✅ **[UI Tests](../tests/Archu.Ui.Tests/README.md)** - Accessibility & component tests
4. ✅ **[Unit Tests](../tests/Archu.UnitTests/README.md)** - Business logic tests (if exists)

---

## 📖 Documentation by Audience

### For New Developers
**Start here to understand the project:**
1. **[Getting Started Guide](GETTING_STARTED.md)** - Get running in 10 minutes ⚡
2. **[Architecture Guide](ARCHITECTURE.md)** - Understand the structure
3. **[Application & Infrastructure Quick Reference](APPLICATION_INFRASTRUCTURE_QUICK_REFERENCE.md)** - Core layers overview
4. **[API Guide](API_GUIDE.md)** - Learn the APIs
5. **[Development Guide](DEVELOPMENT_GUIDE.md)** - Development patterns

**Total onboarding time**: ~45 minutes

### For Frontend Developers
**API integration and usage:**
1. **[Archu.Web README](../src/Archu.Web/README.md)** - Blazor WebAssembly app ⭐ NEW
2. **[Archu.ApiClient README](../src/Archu.ApiClient/README.md)** - HTTP client with resilience
3. **[Authentication Framework](../src/Archu.ApiClient/Authentication/README.md)** - JWT tokens ⭐ NEW
4. **[Archu.Ui README](../src/Archu.Ui/README.md)** - Component library
5. **[API Guide](API_GUIDE.md)** - Complete API reference
6. **HTTP Examples** - `src/Archu.Api/Archu.Api.http`
7. **Scalar UI** - https://localhost:7123/scalar/v1

### For Backend Developers
**Implementation and architecture:**
1. **[Architecture Guide](ARCHITECTURE.md)** - System design
2. **[Domain Layer](../src/Archu.Domain/README.md)** - Business entities & logic
3. **[Application Layer](../src/Archu.Application/README.md)** - Use cases & CQRS
4. **[Infrastructure Layer](../src/Archu.Infrastructure/README.md)** - Repositories & EF Core
5. **[Contracts Layer](../src/Archu.Contracts/README.md)** - API DTOs & contracts
6. **[Quick Reference](APPLICATION_INFRASTRUCTURE_QUICK_REFERENCE.md)** - Layer cheat sheet
7. **[Development Guide](DEVELOPMENT_GUIDE.md)** - Code patterns
8. **[Database Guide](DATABASE_GUIDE.md)** - Data access
9. **[New Entity Guide](../src/README_NEW_ENTITY.md)** - Feature development
10. **[Archu.AppHost](../src/Archu.AppHost/README.md)** - Orchestration with Aspire ⭐ NEW
11. **[Archu.ServiceDefaults](../src/Archu.ServiceDefaults/README.md)** - Shared configuration ⭐ NEW

### For Test Engineers ⭐ NEW
**Testing strategy and infrastructure:**
1. **[Integration Tests](../tests/Archu.IntegrationTests/README.md)** - API integration testing (17 tests)
2. **[API Client Tests](../tests/Archu.ApiClient.Tests/README.md)** - HTTP client unit tests (11 tests)
3. **[UI Tests](../tests/Archu.Ui.Tests/README.md)** - Accessibility testing (2 tests)
4. **[Development Guide](DEVELOPMENT_GUIDE.md)** - Testing best practices

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
5. **[Infrastructure Layer](../src/Archu.Infrastructure/README.md)** - Authentication implementation

---

## 🏗️ Project Structure

```
Archu/
├── docs/      # 📚 Documentation
│   ├── README.md  # This file - Documentation hub
│   ├── GETTING_STARTED.md   # ⚡ Start here - Complete setup guide
│   ├── ARCHITECTURE.md  # System architecture
│   ├── PROJECT_STRUCTURE.md    # Directory organization
│   ├── APPLICATION_INFRASTRUCTURE_QUICK_REFERENCE.md  # Layer comparison
│   ├── API_GUIDE.md    # Complete API reference
│   ├── AUTHENTICATION_GUIDE.md    # JWT and authentication
│   ├── AUTHORIZATION_GUIDE.md # Role-based access control
│   ├── PASSWORD_SECURITY_GUIDE.md # Password policies
│   ├── DATABASE_GUIDE.md        # Database and migrations
│   ├── DEVELOPMENT_GUIDE.md          # Development workflow
│   └── ARCHIVE.md  # Historical documentation
├── src/
│   ├── Archu.Api/   # 🌐 Main REST API
│   │   ├── Archu.Api.http         # 40+ HTTP examples
│   │   └── README.md       # API project documentation
│   ├── Archu.AdminApi/   # 🛡️ Admin API
│   │   ├── Archu.AdminApi.http   # 31 HTTP examples
│ │   └── README.md        # Admin API documentation
│   ├── Archu.Domain/      # 💼 Business logic
│   │   └── README.md   # Complete Domain layer guide
│   ├── Archu.Application/           # 🎯 Use cases & CQRS
│   │   └── README.md# Complete Application layer guide
│   ├── Archu.Infrastructure/    # 🔌 Data access & repositories
│   │   └── README.md      # Complete Infrastructure layer guide
│   ├── Archu.Contracts/         # 📝 API DTOs
│   │   └── README.md# Complete Contracts layer guide
│   ├── Archu.ApiClient/  # 📡 HTTP client library
│   │   ├── README.md    # Complete client documentation
│   │   ├── RESILIENCE.md   # Resilience & error handling
│   │   └── Authentication/
│   │       └── README.md# ⭐ Authentication framework guide
│   ├── Archu.Ui/   # 🎨 Blazor components
│   │   ├── README.md      # Component library documentation
│   │   ├── CHANGELOG.md   # Version history
│   │   └── INTEGRATION.md # Platform-specific integration guide
│   ├── Archu.Web/   # 🌐 Blazor WebAssembly app
│   │   └── README.md ⭐ WebAssembly application guide
│   ├── Archu.ServiceDefaults/ # ⚙️ Aspire shared configuration
│   │   └── README.md ⭐ NEW - Service defaults documentation
│   ├── Archu.AppHost/    # 🚀 Aspire orchestrator
│   │   └── README.md ⭐ NEW - Orchestration guide
│   └── README_NEW_ENTITY.md # Development tutorial
├── tests/  # 🧪 Test Projects ⭐ NEW
│   ├── Archu.IntegrationTests/  # API integration tests
│   │   └── README.md    # 17 integration tests
│   ├── Archu.ApiClient.Tests/   # API client unit tests
│   │   └── README.md    # 11 unit tests
│   └── Archu.Ui.Tests/     # UI accessibility tests
│       └── README.md       # 2 accessibility tests
└── README.md        # Project overview
```

---

## 🔑 Key Concepts

### Clean Architecture
- **Domain**: Business entities and logic (no dependencies)
- **Application**: Use cases and abstractions (CQRS with MediatR)
- **Infrastructure**: Database, authentication, external services
- **API**: REST endpoints and presentation

**[Learn more →](ARCHITECTURE.md)**  
**[Domain Layer Details →](../src/Archu.Domain/README.md)**  
**[Application Layer Details →](../src/Archu.Application/README.md)**  
**[Infrastructure Layer Details →](../src/Archu.Infrastructure/README.md)**  
**[Contracts Layer Details →](../src/Archu.Contracts/README.md)**

### CQRS Pattern
- **Commands**: Create, Update, Delete operations
- **Queries**: Read operations
- Uses MediatR for request handling

**[Learn more →](DEVELOPMENT_GUIDE.md)**  
**[Application Layer Guide →](../src/Archu.Application/README.md)**

### Repository Pattern
- **Abstractions**: Defined in Application layer
- **Implementations**: Provided in Infrastructure layer
- **Base Repository**: Common CRUD operations with concurrency support

**[Infrastructure Layer Guide →](../src/Archu.Infrastructure/README.md)**

### JWT Authentication
- Access tokens for API requests (short-lived: 15-60 min)
- Refresh tokens for re-authentication (long-lived: 7-30 days)
- Secure secret management with User Secrets or Azure Key Vault

**[Learn more →](AUTHENTICATION_GUIDE.md)**  
**[Infrastructure Implementation →](../src/Archu.Infrastructure/README.md)**  
**[Client-Side Authentication →](../src/Archu.ApiClient/Authentication/README.md)** ⭐ NEW

### Role-Based Authorization
- 5 system roles: Guest, User, Manager, Administrator, SuperAdmin
- Policy-based authorization
- Security restrictions prevent privilege escalation

**[Learn more →](AUTHORIZATION_GUIDE.md)**

### API Client Library
- Strongly-typed HTTP clients
- Automatic retry with exponential backoff
- Circuit breaker pattern
- JWT token management
- Platform-specific registration (WASM vs Server)

**[Learn more →](../src/Archu.ApiClient/README.md)**  
**[Resilience Guide →](../src/Archu.ApiClient/RESILIENCE.md)**  
**[Authentication Guide →](../src/Archu.ApiClient/Authentication/README.md)** ⭐ NEW

### Testing Strategy ⭐ NEW
- **Integration Tests**: API endpoints with real SQL Server containers (17 tests)
- **API Client Tests**: HTTP client behavior and resilience (11 tests)
- **UI Tests**: Accessibility and WCAG 2.1 compliance (2 tests)
- **Unit Tests**: Business logic and command/query handlers

**[Integration Tests →](../tests/Archu.IntegrationTests/README.md)**  
**[API Client Tests →](../tests/Archu.ApiClient.Tests/README.md)**  
**[UI Tests →](../tests/Archu.Ui.Tests/README.md)**

---

## 🛠️ Common Tasks

### Running the Application

```bash
cd src/Archu.AppHost
dotnet run
```

**Access Points:**
- Main API: https://localhost:7123
- Admin API: https://localhost:7290
- Blazor WASM: https://localhost:5001
- Main API Docs: https://localhost:7123/scalar/v1
- Admin API Docs: https://localhost:7290/scalar/v1

### Setting Up Authentication

```bash
cd src/Archu.Api
dotnet user-secrets set "Jwt:Secret" "YourSecure64CharacterSecretKey"
```

**[Full guide →](GETTING_STARTED.md)**

### Testing the API

```bash
# Open HTTP files in Visual Studio
# src/Archu.Api/Archu.Api.http (40+ examples)
# Archu.AdminApi/Archu.AdminApi.http (31 examples)

# Or use Scalar UI
https://localhost:7123/scalar/v1  # Main API
https://localhost:7290/scalar/v1  # Admin API
```

**[Full guide →](API_GUIDE.md)**

### Running Tests ⭐ NEW

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Archu.IntegrationTests
dotnet test tests/Archu.ApiClient.Tests
dotnet test tests/Archu.Ui.Tests

# Run with code coverage
dotnet test /p:CollectCoverage=true
```

**[Test Documentation →](#i-want-to-test-the-application-)**

### Creating a Migration

```bash
cd src/Archu.Infrastructure
dotnet ef migrations add YourMigrationName --startup-project ../Archu.Api
dotnet ef database update --startup-project ../Archu.Api
```

**[Full guide →](DATABASE_GUIDE.md)**

### Adding a New Feature

Follow the step-by-step guide: **[Adding New Entities](../src/README_NEW_ENTITY.md)**

---

## 📊 Documentation Statistics

| Metric | Value |
|--------|-------|
| **Total Documentation Files** | 21 essential docs ⬆️ (+3 new) |
| **Quick Start Time** | 10 minutes |
| **Full Onboarding Time** | 45 minutes |
| **HTTP Request Examples** | 71+ examples |
| **API Endpoints** | 28 total (16 Main + 12 Admin) |
| **Project READMEs** | 13 ⬆️ (+2 new) |
| **Test Project READMEs** | 3 ⭐ NEW |
| **Total Tests** | 30+ tests ⭐ NEW |

### Recent Updates ⭐ NEW

**Date**: 2025-01-24

**New Documentation:**
- ✅ [Archu.IntegrationTests README](../tests/Archu.IntegrationTests/README.md) - Complete integration testing guide ⭐ NEW
- ✅ [Archu.ApiClient.Tests README](../tests/Archu.ApiClient.Tests/README.md) - API client unit tests ⭐ NEW
- ✅ [Archu.Ui.Tests README](../tests/Archu.Ui.Tests/README.md) - Accessibility testing guide ⭐ NEW

**Updated Documentation:**
- ✅ [docs/README.md](README.md) - Added test documentation section
- ✅ [docs/ARCHIVE.md](ARCHIVE.md) - Consolidated historical documentation

**Test Coverage:**
- ✅ Integration Tests: 17 tests (API endpoints)
- ✅ API Client Tests: 11 tests (HTTP client behavior)
- ✅ UI Tests: 2 tests (accessibility & WCAG 2.1)

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
| **Blazor WebAssembly** | [Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-webassembly) |
| **MudBlazor** | [Official Docs](https://mudblazor.com/) |
| **xUnit** | [Official Docs](https://xunit.net/) |
| **bUnit** | [Official Docs](https://bunit.dev/) |
| **Testcontainers** | [Official Docs](https://dotnet.testcontainers.org/) |
| **WCAG 2.1** | [Quick Reference](https://www.w3.org/WAI/WCAG21/quickref/) |

---

## 🤝 Getting Help

- **Getting started**: See [GETTING_STARTED.md](GETTING_STARTED.md)
- **API questions**: See [API_GUIDE.md](API_GUIDE.md)
- **Architecture questions**: See [ARCHITECTURE.md](ARCHITECTURE.md)
- **Authentication questions**: See [AUTHENTICATION_GUIDE.md](AUTHENTICATION_GUIDE.md)
- **Authorization questions**: See [AUTHORIZATION_GUIDE.md](AUTHORIZATION_GUIDE.md)
- **Development questions**: See [DEVELOPMENT_GUIDE.md](DEVELOPMENT_GUIDE.md)
- **Database questions**: See [DATABASE_GUIDE.md](DATABASE_GUIDE.md)
- **Frontend questions**: See [Archu.Web README](../src/Archu.Web/README.md) ⭐ NEW
- **Client library questions**: See [Archu.ApiClient README](../src/Archu.ApiClient/README.md)
- **Testing questions**: See [Integration Tests](../tests/Archu.IntegrationTests/README.md), [API Client Tests](../tests/Archu.ApiClient.Tests/README.md), [UI Tests](../tests/Archu.Ui.Tests/README.md) ⭐ NEW

---

## 📅 Version History

| Version | Date | Changes |
|---------|------|---------|
| 4.3 | 2025-01-24 | **Added test documentation** (3 new test project READMEs) |
| 4.2 | 2025-01-23 | **Added Aspire documentation** (AppHost, ServiceDefaults READMEs) |
| 4.1 | 2025-01-23 | **Added frontend documentation** (Archu.Web, Authentication Framework) |
| 4.0 | 2025-01-22 | **Major consolidation** (51 files → 12 files, 76% reduction) |
| 3.0 | 2025-01-22 | Major API documentation overhaul (7 new docs, 71+ HTTP examples) |
| 2.3 | 2025-01-22 | Added password policy and database seeding guides |
| 2.2 | 2025-01-22 | Added JWT configuration guides |
| 2.1 | 2025-01-22 | Added security fixes documentation |
| 2.0 | 2025-01-22 | Major documentation overhaul |
| 1.0 | 2025-01-17 | Initial documentation |

---

**Last Updated**: 2025-01-24  
**Version**: 4.3 ⚡ **TEST DOCUMENTATION ADDED**  
**Maintainer**: Archu Development Team  
**Questions?** Open an issue on [GitHub](https://github.com/chethandvg/archu/issues)
