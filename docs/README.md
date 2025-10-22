# Archu Documentation Hub

Welcome to the Archu documentation! This hub provides links to all documentation resources.

---

## 📚 Documentation Index

### Getting Started
- **[Quick Start](../README.md#quick-start)** - Get the application running
- **[JWT Quick Start](JWT_QUICK_START.md)** - Set up authentication in 5 minutes ⚡ 
- **[Database Seeding Guide](DATABASE_SEEDING_GUIDE.md)** - Initialize roles and admin user ⚡
- **[Password Policy Guide](PASSWORD_POLICY_GUIDE.md)** - Configure password requirements ⚡ **NEW**

### Architecture & Design
- **[Architecture Guide](ARCHITECTURE.md)** - Clean Architecture overview
- **[Project Structure](PROJECT_STRUCTURE.md)** - Directory organization

### Authentication & Security
- **[JWT Quick Start](JWT_QUICK_START.md)** - 5-minute authentication setup ⚡
- **[JWT Configuration Guide](JWT_CONFIGURATION_GUIDE.md)** - Complete JWT configuration reference
- **[Database Seeding Guide](DATABASE_SEEDING_GUIDE.md)** - Initialize roles and admin user
- **[Password Policy Guide](PASSWORD_POLICY_GUIDE.md)** - Configure password requirements **NEW**
- **[Authentication Infrastructure](../src/Archu.Infrastructure/Authentication/README.md)** - Authentication components
- **[Authentication & Authorization](AUTHENTICATION_AND_AUTHORIZATION.md)** - Overview
- **[Authentication Implementation Details](AUTHENTICATION_IMPLEMENTATION_DETAIL.md)** - Technical details

### Development Guides
- **[Adding New Entities](../src/README_NEW_ENTITY.md)** - Step-by-step entity creation
- **[Contributing Guide](contributing/)** - Contribution guidelines

### Security
- **[Security Fixes Summary](SECURITY_FIXES_SUMMARY.md)** - Security improvements
- **[Password Reset Implementation](FIX_1_PASSWORD_RESET_IMPLEMENTATION.md)** - Password reset guide
- **[Email Confirmation Implementation](FIX_4_EMAIL_CONFIRMATION_IMPLEMENTATION.md)** - Email confirmation guide

---

## 🎯 Quick Links by Task

### I want to...

#### Set up authentication
1. **[JWT Quick Start](JWT_QUICK_START.md)** - 5-minute setup ⚡
2. **[JWT Configuration Guide](JWT_CONFIGURATION_GUIDE.md)** - Detailed reference
3. **[Database Seeding Guide](DATABASE_SEEDING_GUIDE.md)** - Initialize roles and admin user ⚡ **NEW**
4. **[Password Policy Guide](PASSWORD_POLICY_GUIDE.md)** - Configure password requirements ⚡ **NEW**

#### Add a new feature
1. [Adding New Entities](../src/README_NEW_ENTITY.md)
2. [Architecture Guide](ARCHITECTURE.md)

#### Understand the architecture
1. [Architecture Guide](ARCHITECTURE.md)
2. [Project Structure](PROJECT_STRUCTURE.md)

#### Deploy to production
1. [JWT Configuration Guide](JWT_CONFIGURATION_GUIDE.md#production-deployment)
2. [Architecture Guide](ARCHITECTURE.md#deployment)

---

## 📖 Documentation by Audience

### For New Developers
Start here to understand the project:
1. [Architecture Guide](ARCHITECTURE.md)
2. [Quick Start](../README.md#quick-start)
3. **[JWT Quick Start](JWT_QUICK_START.md)** ⚡
4. [Adding New Entities](../src/README_NEW_ENTITY.md)

### For DevOps Engineers
Deployment and configuration:
1. **[JWT Configuration Guide](JWT_CONFIGURATION_GUIDE.md)**
2. [Architecture Guide - Deployment](ARCHITECTURE.md#deployment)

### For Security Auditors
Security features and best practices:
1. **[JWT Configuration Guide - Security](JWT_CONFIGURATION_GUIDE.md#security-best-practices)**
2. **[Authentication Infrastructure](../src/Archu.Infrastructure/Authentication/README.md)**
3. [Security Fixes Summary](SECURITY_FIXES_SUMMARY.md)

---

## 🏗️ Project Structure

```
Archu/
├── docs/                                   # 📚 Documentation
│   ├── README.md                           # This file
│   ├── ARCHITECTURE.md                     # Architecture guide
│   ├── JWT_QUICK_START.md                  # 5-minute JWT setup ⚡ NEW
│   ├── JWT_CONFIGURATION_GUIDE.md          # Complete JWT reference NEW
│   ├── PROJECT_STRUCTURE.md                # Directory organization
│   ├── authentication/                     # Authentication docs
│   ├── authorization/                      # Authorization docs
│   ├── database/                           # Database docs
│   └── getting-started/                    # Getting started guides
├── scripts/                                # 🔧 Utility scripts
│   ├── setup-jwt-secrets.ps1               # JWT setup (Windows) NEW
│   └── setup-jwt-secrets.sh                # JWT setup (Linux/macOS) NEW
├── src/
│   ├── Archu.Domain/                       # 💼 Business logic
│   ├── Archu.Application/                  # 🎯 Use cases
│   ├── Archu.Infrastructure/               # 🔌 External concerns
│   │   ├── Authentication/                 # 🔐 Authentication
│   │   │   └── README.md                   # Auth docs NEW
│   │   └── DependencyInjection.cs          # Service registration NEW
│   ├── Archu.Contracts/                    # 📝 API DTOs
│   ├── Archu.Api/                          # 🌐 REST API
│   │   ├── appsettings.Staging.json        # Staging config NEW
│   │   └── appsettings.Production.json     # Production config NEW
│   ├── Archu.Ui/                           # 🎨 Blazor components
│   ├── Archu.ServiceDefaults/              # ⚙️ Aspire defaults
│   ├── Archu.AppHost/                      # 🚀 Aspire orchestrator
│   └── README_NEW_ENTITY.md                # Development guide
└── README.md                               # Project overview
```

---

## 🔑 Key Concepts

### Clean Architecture
The project follows Clean Architecture principles with clear separation of concerns:
- **Domain**: Business entities and logic (no dependencies)
- **Application**: Use cases and abstractions
- **Infrastructure**: Database, authentication, external services
- **API**: REST endpoints and presentation

[Learn more →](ARCHITECTURE.md)

### CQRS Pattern
Commands (writes) and Queries (reads) are separated:
- **Commands**: Create, Update, Delete operations
- **Queries**: Read operations
- Uses MediatR for request handling

[Learn more →](ARCHITECTURE.md)

### JWT Authentication
Secure token-based authentication:
- Access tokens for API requests (short-lived)
- Refresh tokens for seamless re-authentication (long-lived)
- Secure secret management with User Secrets or Azure Key Vault

**[Learn more →](JWT_CONFIGURATION_GUIDE.md)**

---

## 🛠️ Common Tasks

### Running the Application

```bash
cd src/Archu.AppHost
dotnet run
```

### Setting Up Authentication

```bash
cd src/Archu.Api
../../scripts/setup-jwt-secrets.ps1  # Windows
../../scripts/setup-jwt-secrets.sh   # Linux/macOS
```

**[Full guide →](JWT_QUICK_START.md)**

### Creating a Migration

```bash
cd src/Archu.Infrastructure
dotnet ef migrations add YourMigrationName --startup-project ../Archu.Api
dotnet ef database update --startup-project ../Archu.Api
```

### Adding a New Entity

Follow the step-by-step guide: [Adding New Entities](../src/README_NEW_ENTITY.md)

---

## 📝 What's New (2025-01-22)

### Password Policy (NEW)

✨ **New Feature:**
- **[Password Policy Guide](PASSWORD_POLICY_GUIDE.md)** - Complete password policy guide
- **[Password Policy Implementation Summary](PASSWORD_POLICY_IMPLEMENTATION_SUMMARY.md)** - What was built
- Configurable password complexity requirements
- Real-time password validation
- Password strength scoring (0-100)
- Common password detection (top 100)
- Username/email prevention in passwords
- FluentValidation integration

✨ **New Components:**
- `PasswordPolicyOptions.cs` - Configuration value object
- `IPasswordValidator.cs` - Password validator interface
- `PasswordValidator.cs` - Implementation with strength scoring
- `PasswordValidationResult.cs` - Validation result object
- FluentValidation validators for registration, password change, and reset

✨ **Security Features:**
- Prevents top 100 most common passwords
- Prevents passwords containing username or email
- Configurable character requirements (uppercase, lowercase, digits, special)
- Minimum unique characters requirement
- Environment-specific policies (stricter in production)

### Database Seeding

✨ **New Feature:**
- **[Database Seeding Guide](DATABASE_SEEDING_GUIDE.md)** - Complete seeding guide
- **[Seeding Implementation Summary](DATABASE_SEEDING_IMPLEMENTATION_SUMMARY.md)** - What was built
- Automatic initialization of system roles and admin user
- Setup scripts for Windows and Linux/macOS
- Environment-specific configuration
- Idempotent seeding (safe to run multiple times)

✨ **New Tools:**
- `setup-database-seeding.ps1` - Automated seeding setup for Windows
- `setup-database-seeding.sh` - Automated seeding setup for Linux/macOS

✨ **New Infrastructure:**
- `DatabaseSeeder.cs` - Main seeding class
- `DatabaseSeederOptions.cs` - Configuration class
- `DatabaseSeedingExtensions.cs` - Extension methods
- Seeds 5 system roles (Guest, User, Manager, Administrator, SuperAdmin)
- Seeds admin user with configurable credentials

### JWT Configuration Enhancements

✨ **New Documentation:**
- **[JWT Quick Start](JWT_QUICK_START.md)** - Get authentication running in 5 minutes
- **[JWT Configuration Guide](JWT_CONFIGURATION_GUIDE.md)** - Complete reference with Azure Key Vault
- **[Authentication Infrastructure README](../src/Archu.Infrastructure/Authentication/README.md)** - Component documentation

✨ **New Tools:**
- `setup-jwt-secrets.ps1` - Automated JWT setup for Windows
- `setup-jwt-secrets.sh` - Automated JWT setup for Linux/macOS

✨ **New Infrastructure:**
- `DependencyInjection.cs` - Clean service registration for Infrastructure layer
- `appsettings.Staging.json` - Staging environment configuration
- `appsettings.Production.json` - Production environment configuration

✨ **Improved Program.cs:**
- Simplified service registration using `AddInfrastructure()` extension method
- Cleaner, more maintainable code

---

## 📚 Related External Resources

| Topic | Resource |
|-------|----------|
| **.NET 9** | [Official Docs](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9) |
| **Clean Architecture** | [Martin Fowler](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) |
| **.NET Aspire** | [Official Docs](https://learn.microsoft.com/en-us/dotnet/aspire/) |
| **JWT Best Practices** | [RFC 8725](https://datatracker.ietf.org/doc/html/rfc8725) |
| **User Secrets** | [Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) |
| **Azure Key Vault** | [Microsoft Docs](https://learn.microsoft.com/en-us/azure/key-vault/general/overview) |

---

## 🤝 Getting Help

- **Architecture questions**: See [Architecture Guide](ARCHITECTURE.md)
- **Authentication questions**: See [JWT Configuration Guide](JWT_CONFIGURATION_GUIDE.md) or [JWT Quick Start](JWT_QUICK_START.md)
- **Security questions**: See [Security Fixes Summary](SECURITY_FIXES_SUMMARY.md)
- **Development questions**: See [Adding New Entities](../src/README_NEW_ENTITY.md)

---

## 📅 Version History

| Version | Date | Changes |
|---------|------|---------|
| 2.2 | 2025-01-22 | Added JWT configuration guides, scripts, and DependencyInjection |
| 2.1 | 2025-01-22 | Added security fixes documentation |
| 2.0 | 2025-01-22 | Major documentation overhaul |
| 1.0 | 2025-01-17 | Initial documentation |

---

**Last Updated**: 2025-01-22  
**Version**: 2.2  
**Maintainer**: Archu Development Team  
**Questions?** Open an issue on [GitHub](https://github.com/chethandvg/archu/issues)
