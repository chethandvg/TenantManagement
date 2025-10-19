# Archu Documentation Hub

Welcome to the Archu project documentation. This guide helps you navigate all documentation resources.

## 📚 Quick Navigation

### 🏗️ Architecture & Design
- **[Architecture Guide](ARCHITECTURE.md)** - Complete architecture overview, Clean Architecture, .NET Aspire integration
- **[Project Structure](#project-structure)** - Detailed breakdown of each project

### 🔒 Concurrency & Data Integrity
- **[Concurrency Guide](CONCURRENCY_GUIDE.md)** - Complete guide to optimistic concurrency, soft delete, and audit tracking
  - Includes: How-to, code examples, testing, and troubleshooting

### 🔧 Development Guides
- **[Adding New Entities](../src/README_NEW_ENTITY.md)** - Step-by-step guide for adding entities with all features

### 📖 API Documentation
- **[REST API Reference](../src/Archu.Api/README.md)** - API endpoints, authentication, versioning
- **Scalar UI** - Interactive API documentation (available when running the app)

---

## 🗂️ Project Structure

The solution follows Clean Architecture principles:

```
src/
├── Archu.Domain/              → Core business logic (no dependencies)
├── Archu.Application/         → Use cases, interfaces, CQRS handlers
├── Archu.Infrastructure/      → EF Core, repositories, external services
├── Archu.Contracts/           → API DTOs and request/response models
├── Archu.Api/                 → ASP.NET Core Web API
├── Archu.Ui/                  → Blazor component library (MudBlazor)
├── Archu.ServiceDefaults/     → .NET Aspire shared configuration
└── Archu.AppHost/             → .NET Aspire orchestrator
```

### Individual Project Documentation

| Project | Purpose | README |
|---------|---------|--------|
| **Archu.Domain** | Business entities & rules | [View](../src/Archu.Domain/README.md) |
| **Archu.Application** | Application logic & CQRS | [View](../src/Archu.Application/README.md) |
| **Archu.Infrastructure** | Data access & external services | [View](../src/Archu.Infrastructure/README.md) |
| **Archu.Contracts** | API contracts (DTOs) | [View](../src/Archu.Contracts/README.md) |
| **Archu.Api** | REST API endpoints | [View](../src/Archu.Api/README.md) |
| **Archu.Ui** | Blazor components | [View](../src/Archu.Ui/README.md) |
| **Archu.ServiceDefaults** | Aspire defaults | [View](../src/Archu.ServiceDefaults/README.md) |
| **Archu.AppHost** | Aspire orchestration | [View](../src/Archu.AppHost/README.md) |

---

## 🚀 Getting Started

### Prerequisites
- .NET 9 SDK
- .NET 8 SDK (for Aspire AppHost)
- SQL Server (or use Docker)
- Visual Studio 2022 or Rider

### Running the Application

```bash
# Start all services with Aspire orchestration
cd src/Archu.AppHost
dotnet run

# Access the Aspire Dashboard (URL shown in console)
# Access the API: http://localhost:5000
# Access Scalar API Docs: http://localhost:5000/scalar/v1
```

---

## 🎯 Key Concepts

### Clean Architecture
- **Dependency Rule**: Inner layers never depend on outer layers
- **Domain at the core**: Business logic has zero external dependencies
- **Testability**: Core logic can be tested without database or HTTP

### Optimistic Concurrency
- Uses SQL Server `rowversion` for conflict detection
- Client sends `RowVersion` with every update
- Returns **409 Conflict** if data was modified by another user
- See: [Concurrency Guide](CONCURRENCY_GUIDE.md)

### Soft Delete
- Records are never physically deleted
- Marked with `IsDeleted = true`
- Automatically excluded from all queries
- Preserves audit history

### CQRS with MediatR
- **Commands**: CreateProduct, UpdateProduct, DeleteProduct
- **Queries**: GetProducts, GetProductById
- Clear separation of read/write operations

---

## 📋 Common Tasks

### Adding a New Entity
See: [Adding New Entities Guide](../src/README_NEW_ENTITY.md)

Quick checklist:
1. Create entity in `Archu.Domain/Entities/`
2. Create repository interface in `Archu.Application/Abstractions/`
3. Implement repository in `Archu.Infrastructure/Repositories/`
4. Add to `UnitOfWork`
5. Create DTOs in `Archu.Contracts/`
6. Create commands/queries in `Archu.Application/`
7. Create controller in `Archu.Api/Controllers/`
8. Add EF Core configuration
9. Create migration

### Working with Concurrency
See: [Concurrency Guide](CONCURRENCY_GUIDE.md) for complete details.

**Quick pattern:**
```csharp
// 1. Get entity with RowVersion
var product = await _repository.GetByIdAsync(id);

// 2. Update properties
product.Name = newName;

// 3. Pass client's RowVersion
await _repository.UpdateAsync(product, clientRowVersion);

// 4. Save and handle conflicts
try {
    await _unitOfWork.SaveChangesAsync();
} catch (DbUpdateConcurrencyException) {
    return Result.Failure("Modified by another user");
}

// 5. Return new RowVersion to client
return new ProductDto { RowVersion = product.RowVersion };
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific project tests
dotnet test tests/Archu.Application.Tests
```

### Creating Migrations
```bash
cd src/Archu.Infrastructure
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

---

## 🧪 Testing Strategy

| Test Type | Target | Tools |
|-----------|--------|-------|
| **Unit Tests** | Domain, Application logic | xUnit, FluentAssertions |
| **Integration Tests** | API endpoints, Database | xUnit, WebApplicationFactory, Testcontainers |
| **E2E Tests** | Full workflows | Playwright |

---

## 📦 Technologies

### Core Framework
- **.NET 9** (Domain, Application, Infrastructure, API, Contracts, ServiceDefaults, UI)
- **.NET 8** (AppHost - Aspire requirement)

### Data & Persistence
- **Entity Framework Core 9** with SQL Server
- **Optimistic Concurrency** via `rowversion`
- **Migrations** for schema management

### API & Communication
- **ASP.NET Core 9** Web API
- **MediatR** for CQRS
- **FluentValidation** for validation
- **Scalar** for API documentation

### Cloud-Native & Orchestration
- **.NET Aspire** for service orchestration
- **OpenTelemetry** for observability
- **Health Checks** for monitoring

### UI Framework
- **Blazor** (Server, WASM, Hybrid support)
- **MudBlazor** for components

---

## 🤝 Contributing

When making changes:
1. **Follow Clean Architecture** - respect dependency rules
2. **Include concurrency control** for all update operations
3. **Write tests** for new features
4. **Update documentation** when adding features
5. **Use consistent patterns** from existing code

---

## 📞 Support

For questions or issues:
- Review relevant documentation sections above
- Check individual project READMEs
- Consult the [Concurrency Guide](CONCURRENCY_GUIDE.md) for data integrity questions
- See [Architecture Guide](ARCHITECTURE.md) for design questions

---

## 📝 Documentation Updates

| Document | Last Updated | Version |
|----------|--------------|---------|
| Architecture Guide | 2025-01-22 | 1.0 |
| Concurrency Guide | 2025-01-22 | 1.0 |
| This Hub | 2025-01-22 | 1.0 |

---

**Maintained by**: Archu Development Team  
**License**: [Your License]
