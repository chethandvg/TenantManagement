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

3. **[API Guide](API_GUIDE.md)**
   - Complete API reference for both Main API and Admin API
   - All endpoints documented
   - Authentication flows
   - Common workflows
   - Error handling

4. **[Authentication Guide](AUTHENTICATION_GUIDE.md)**
   - JWT configuration
   - Token management
   - Security best practices
   - Troubleshooting

5. **[Authorization Guide](AUTHORIZATION_GUIDE.md)**
   - Role-based access control
   - Security restrictions
   - Policy configuration

6. **[Password Security Guide](PASSWORD_SECURITY_GUIDE.md)**
   - Password policies
   - Complexity rules
   - Validation implementation

7. **[Database Guide](DATABASE_GUIDE.md)**
   - Database setup
   - Migrations
   - Seeding
   - Retry strategy

8. **[Development Guide](DEVELOPMENT_GUIDE.md)**
   - Development workflow
   - Code patterns
   - Best practices
   - Testing

9. **[Project Structure](PROJECT_STRUCTURE.md)**
   - Directory organization
   - File conventions

10. **[Archive](ARCHIVE.md)**
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

### For New Developers
**Start here to understand the project:**
1. **[Getting Started Guide](GETTING_STARTED.md)** - Get running in 10 minutes ⚡
2. **[Architecture Guide](ARCHITECTURE.md)** - Understand the structure
3. **[API Guide](API_GUIDE.md)** - Learn the APIs
4. **[Development Guide](DEVELOPMENT_GUIDE.md)** - Development patterns

**Total onboarding time**: ~45 minutes

### For Frontend Developers
**API integration and usage:**
1. **[API Guide](API_GUIDE.md)** - Complete API reference
2. **[Authentication Guide](AUTHENTICATION_GUIDE.md)** - JWT tokens
3. **HTTP Examples** - `src/Archu.Api/Archu.Api.http`
4. **Scalar UI** - https://localhost:7123/scalar/v1

### For Backend Developers
**Implementation and architecture:**
1. **[Architecture Guide](ARCHITECTURE.md)** - System design
2. **[Development Guide](DEVELOPMENT_GUIDE.md)** - Code patterns
3. **[Database Guide](DATABASE_GUIDE.md)** - Data access
4. **[New Entity Guide](../src/README_NEW_ENTITY.md)** - Feature development

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
Archu/
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
│   ├── Archu.Api/                          # 🌐 Main REST API
│   │   ├── Archu.Api.http                  # 40+ HTTP examples
│   │   └── README.md                       # API project documentation
│   ├── Archu.AdminApi/                     # 🛡️ Admin API
│   │   ├── Archu.AdminApi.http             # 31 HTTP examples
│   │   └── README.md                       # Admin API documentation
│   ├── Archu.Domain/                       # 💼 Business logic
│   ├── Archu.Application/                  # 🎯 Use cases
│   ├── Archu.Infrastructure/               # 🔌 Data access
│   ├── Archu.Contracts/                    # 📝 API DTOs
│   ├── Archu.Ui/                           # 🎨 Blazor components
│   ├── Archu.ServiceDefaults/              # ⚙️ Aspire defaults
│   ├── Archu.AppHost/                      # 🚀 Aspire orchestrator
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
cd src/Archu.AppHost
dotnet run
```

**Access Points:**
- Main API: https://localhost:7123
- Admin API: https://localhost:7290
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
| **Total Documentation Files** | 12 essential docs |
| **Quick Start Time** | 10 minutes |
| **Full Onboarding Time** | 45 minutes |
| **HTTP Request Examples** | 71+ examples |
| **API Endpoints** | 28 total (16 Main + 12 Admin) |

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
| 4.0 | 2025-01-22 | **Major consolidation** (51 files → 12 files, 76% reduction) |
| 3.0 | 2025-01-22 | Major API documentation overhaul (7 new docs, 71+ HTTP examples) |
| 2.3 | 2025-01-22 | Added password policy and database seeding guides |
| 2.2 | 2025-01-22 | Added JWT configuration guides |
| 2.1 | 2025-01-22 | Added security fixes documentation |
| 2.0 | 2025-01-22 | Major documentation overhaul |
| 1.0 | 2025-01-17 | Initial documentation |

---

**Last Updated**: 2025-01-22  
**Version**: 4.0 ⚡ **CONSOLIDATED**  
**Maintainer**: Archu Development Team  
**Questions?** Open an issue on [GitHub](https://github.com/chethandvg/archu/issues)
