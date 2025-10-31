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
4. ✅ **[Unit Tests](../tests/Archu.UnitTests/README.md)** - Business logic tests ⭐ NEW

---

### For Test Engineers ⭐ NEW
**Testing strategy and infrastructure:**
1. **[Integration Tests](../tests/Archu.IntegrationTests/README.md)** - API integration testing (17 tests)
2. **[API Client Tests](../tests/Archu.ApiClient.Tests/README.md)** - HTTP client unit tests (11 tests)
3. **[UI Tests](../tests/Archu.Ui.Tests/README.md)** - Accessibility testing (2 tests)
4. **[Unit Tests](../tests/Archu.UnitTests/README.md)** - Business logic unit tests (37 test classes) ⭐ NEW
5. **[Development Guide](DEVELOPMENT_GUIDE.md)** - Testing best practices

---
# Project Structure

## Overview
The directory structure for the Archu solution is organized to separate different concerns and facilitate easy navigation and maintenance. Here's an overview of the top-level directories:

```
├── src/                     # Source files for the application
│   ├── Archu.Api/          # Main API project
│   ├── Archu.Web/          # Blazor WebAssembly front-end
│   ├── Archu.ApiClient/    # HTTP client for API access
│   ├── Archu.Ui/           # Shared UI components
│   ├── Archu.AppHost/      # Application host for local development
│   └── Archu.ServiceDefaults/# Shared configuration values
├── tests/                  # 🧪 Test Projects ⭐ NEW
│   ├── Archu.IntegrationTests/  # API integration tests
│   │   └── README.md    # 17 integration tests
│   ├── Archu.ApiClient.Tests/   # API client unit tests
│   │   └── README.md    # 11 unit tests
│   ├── Archu.Ui.Tests/     # UI accessibility tests
│   │   └── README.md       # 2 accessibility tests
│   └── Archu.UnitTests/# Business logic unit tests ⭐ NEW
│       └── README.md   # 37 test classes
├── docs/                   # Documentation files
└── README.md               # Solution overview and quick links
```

## Details

### src/

- **Archu.Api/**: The main API project, containing the core application logic and API endpoints.
- **Archu.Web/**: The Blazor WebAssembly front-end application.
- **Archu.ApiClient/**: A dedicated HTTP client project for accessing the API, with authentication and error handling.
- **Archu.Ui/**: A library of shared UI components used by both the Blazor front-end and the API.
- **Archu.AppHost/**: The entry point for the application in development, configuring services and middleware.
- **Archu.ServiceDefaults/**: Contains default settings and configurations used across the application.

### tests/

- **Archu.IntegrationTests/**: Contains integration tests for verifying API functionality and interactions between components.
- **Archu.ApiClient.Tests/**: Unit tests for the Archu.ApiClient project, ensuring HTTP communication and data handling works as expected.
- **Archu.Ui.Tests/**: Tests focused on the accessibility and rendering of UI components.
- **Archu.UnitTests/**: Unit tests for the core business logic, covering use cases and domain services.

---

## Running Tests ⭐ NEW

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Archu.IntegrationTests
dotnet test tests/Archu.ApiClient.Tests
dotnet test tests/Archu.Ui.Tests
dotnet test tests/Archu.UnitTests

# Run with code coverage
dotnet test /p:CollectCoverage=true
```

**[Test Documentation →](#i-want-to-test-the-application-)**

---

## 📊 Documentation Statistics

| Metric | Value |
|--------|-------|
| **Total Documentation Files** | 23 essential docs ⬆️ (+5 new) |
| **Quick Start Time** | 10 minutes |
| **Full Onboarding Time** | 45 minutes |
| **HTTP Request Examples** | 71+ examples |
| **API Endpoints** | 28 total (16 Main + 12 Admin) |
| **Project READMEs** | 13 |
| **Test Project READMEs** | 4 ⭐ NEW |
| **Total Tests** | 67+ tests ⭐ NEW |

---

### Recent Updates ⭐ NEW

**Date**: 2025-01-24

**New Documentation:**
- ✅ [Archu.IntegrationTests README](../tests/Archu.IntegrationTests/README.md) - Complete integration testing guide ⭐ NEW
- ✅ [Archu.ApiClient.Tests README](../tests/Archu.ApiClient.Tests/README.md) - API client unit tests ⭐ NEW
- ✅ [Archu.Ui.Tests README](../tests/Archu.Ui.Tests/README.md) - Accessibility testing guide ⭐ NEW
- ✅ [Archu.UnitTests README](../tests/Archu.UnitTests/README.md) - Business logic unit tests ⭐ NEW

**Updated Documentation:**
- ✅ [docs/README.md](README.md) - Added complete test documentation section
- ✅ [docs/ARCHIVE.md](ARCHIVE.md) - Consolidated historical documentation

**Test Coverage:**
- ✅ Integration Tests: 17 tests (API endpoints)
- ✅ API Client Tests: 11 tests (HTTP client behavior)
- ✅ UI Tests: 2 tests (accessibility & WCAG 2.1)
- ✅ Unit Tests: 37 test classes (Domain + Application layers) ⭐ NEW

---

## 📅 Version History

| Version | Date | Changes |
|---------|------|---------|
| 4.4 | 2025-01-24 | **Added unit tests documentation** (Archu.UnitTests README) ⭐ NEW |
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
**Version**: 4.4 ⚡ **UNIT TESTS DOCUMENTATION ADDED**  
**Maintainer**: Archu Development Team  
**Questions?** Open an issue on [GitHub](https://github.com/chethandvg/archu/issues)
