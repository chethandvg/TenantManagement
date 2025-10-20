# Archu Documentation

Welcome to the Archu project documentation!

## 📚 Core Documentation

| Document | Description |
|----------|-------------|
| **[Architecture Guide](./ARCHITECTURE.md)** | Clean Architecture, patterns, and design principles |
| **[Project Structure](./PROJECT_STRUCTURE.md)** | All 9 projects and their responsibilities |
| **[Authentication & Authorization](./AUTHENTICATION_AND_AUTHORIZATION.md)** | Complete JWT auth and role-based access guide |

## 🚀 Quick Start Guides

| Guide | Purpose |
|-------|---------|
| **[Adding New Entity](./getting-started/ADDING_NEW_ENTITY.md)** | Step-by-step CRUD implementation |
| **[Concurrency Guide](./database/CONCURRENCY_GUIDE.md)** | Optimistic concurrency & soft delete |
| **[Current User Service](./authentication/CURRENT_USER_SERVICE.md)** | Access authenticated user info |
| **[Infrastructure Auth Setup](./authentication/INFRASTRUCTURE_AUTH_SETUP.md)** | Database setup for authentication |
| **[JWT Token Implementation](./authentication/JWT_TOKEN_IMPLEMENTATION.md)** | Detailed JWT implementation |

---

## 🔐 Authentication & Authorization Quick Reference

### Authentication Endpoints
```bash
# Register
POST /api/v1/authentication/register

# Login
POST /api/v1/authentication/login

# Refresh Token
POST /api/v1/authentication/refresh-token
```

### Role Hierarchy
- **Admin**: Full access (CRUD)
- **Manager**: Read, Create, Update
- **User**: Read only

### Using Authorization
```csharp
// Require role
[Authorize(Roles = Roles.Admin)]

// Require policy
[Authorize(Policy = AuthorizationPolicies.CanCreateProducts)]
```

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────┐
│          Archu.Api                  │  Controllers, Middleware
├─────────────────────────────────────┤
│      Archu.Application              │  Commands, Queries, Handlers
├─────────────────────────────────────┤
│      Archu.Infrastructure           │  Repositories, DbContext
├─────────────────────────────────────┤
│        Archu.Domain                 │  Entities, Business Logic
└─────────────────────────────────────┘
```

See [Architecture Guide](./ARCHITECTURE.md) for details.

---

## 🗄️ Database Features

- **Optimistic Concurrency** - RowVersion-based conflict detection
- **Soft Delete** - Records marked as deleted, not removed
- **Audit Tracking** - Automatic created/modified tracking

See [Concurrency Guide](./database/CONCURRENCY_GUIDE.md) for usage.

---

## 🔄 Development Workflow

### Setup
```bash
# Clone repository
git clone https://github.com/chethandvg/archu.git

# Update connection string in appsettings.Development.json
# Apply migrations
cd src/Archu.Infrastructure
dotnet ef database update --startup-project ../Archu.Api

# Run with Aspire
cd ../Archu.AppHost
dotnet run
```

### Add New Feature
1. Create entity in `Archu.Domain`
2. Create repository interface in `Archu.Application`
3. Implement repository in `Archu.Infrastructure`
4. Create commands/queries in `Archu.Application`
5. Add API endpoints in `Archu.Api`

See [Adding New Entity](./getting-started/ADDING_NEW_ENTITY.md) for detailed steps.

---

## 📦 Project Structure

**9 Projects:**
- **Domain** - Entities (zero dependencies)
- **Application** - Use cases, CQRS
- **Infrastructure** - Data access, services
- **Contracts** - API DTOs
- **Api** - REST endpoints
- **ApiClient** - Typed HTTP client
- **Ui** - Blazor components
- **AppHost** - Aspire orchestrator
- **ServiceDefaults** - Shared config

See [Project Structure](./PROJECT_STRUCTURE.md) for complete details.

---

## 🛠️ Common Tasks

### Run Application
```bash
cd src/Archu.AppHost
dotnet run
# Open: https://localhost:7001 (API)
# Dashboard: https://localhost:15001 (Aspire)
```

### Apply Migrations
```bash
cd src/Archu.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../Archu.Api
dotnet ef database update --startup-project ../Archu.Api
```

### Test API
```bash
# Via Scalar UI
https://localhost:7001/scalar/v1

# Via curl
curl -X GET https://localhost:7001/api/v1/products
```

---

## 🧪 Testing

### Unit Tests
Focus on Domain and Application layers (no dependencies)

### Integration Tests
Test Infrastructure and API layers (with database)

### Authentication Testing
```bash
# Get token
TOKEN=$(curl -X POST http://localhost:5000/api/v1/authentication/login \
  -d '{"email":"admin@example.com","password":"Admin123!"}' | jq -r '.data.accessToken')

# Use token
curl -X GET http://localhost:5000/api/v1/products \
  -H "Authorization: Bearer $TOKEN"
```

---

## 📞 Support

- **Issues**: Create GitHub issue
- **Questions**: GitHub Discussions
- **Documentation Updates**: Submit PR

---

## 📖 External Resources

- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs)

---

**Last Updated**: 2025-01-22  
**Version**: 4.0 (Consolidated)
