# Archu

A modern, cloud-native application built with Clean Architecture and .NET Aspire.

## 🚀 Quick Start

```bash
# Clone the repository
git clone https://github.com/chethandvg/archu.git
cd archu

# Run the application with Aspire orchestration
cd src/Archu.AppHost
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
- **[Concurrency Guide](docs/CONCURRENCY_GUIDE.md)** - Data integrity and optimistic concurrency
- **[Adding New Entities](src/README_NEW_ENTITY.md)** - Step-by-step development guide

### Quick Links
| Topic | Document |
|-------|----------|
| 🏗️ Architecture & Design | [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) |
| 🔒 Concurrency & Data | [docs/CONCURRENCY_GUIDE.md](docs/CONCURRENCY_GUIDE.md) |
| ➕ Adding Features | [src/README_NEW_ENTITY.md](src/README_NEW_ENTITY.md) |
| 📖 API Reference | [src/Archu.Api/README.md](src/Archu.Api/README.md) |

## 🏗️ Architecture

Archu follows **Clean Architecture** principles with clear separation of concerns:

```
┌─────────────────────────┐
│   Archu.AppHost         │  .NET Aspire orchestration
└────────┬────────────────┘
         │
    ┌────▼─────┐
    │ Archu.Api│  ASP.NET Core Web API
    └────┬─────┘
         │
         ├─ Archu.Infrastructure  (EF Core, Repositories)
         │     └─ Archu.Application  (CQRS, Use Cases)
         │           └─ Archu.Domain  (Entities, Business Logic)
         │
         ├─ Archu.Contracts  (DTOs)
         └─ Archu.ServiceDefaults  (Aspire defaults)
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

### Data Integrity
- **Optimistic Concurrency**: Prevents lost updates using SQL Server `rowversion`
- **Soft Delete**: Preserves data history instead of physical deletion
- **Audit Tracking**: Automatic tracking of who changed what and when

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
cd src/Archu.Infrastructure
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

### Add a New Entity
Follow the guide: [src/README_NEW_ENTITY.md](src/README_NEW_ENTITY.md)

1. Create entity in `Archu.Domain`
2. Create repository interface in `Archu.Application`
3. Implement repository in `Archu.Infrastructure`
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
Archu/
├── docs/                          # All documentation
│   ├── README.md                  # Documentation hub
│   ├── ARCHITECTURE.md            # Architecture guide
│   └── CONCURRENCY_GUIDE.md       # Data integrity guide
├── src/
│   ├── Archu.Domain/              # Business logic (no dependencies)
│   ├── Archu.Application/         # Use cases, CQRS handlers
│   ├── Archu.Infrastructure/      # EF Core, repositories
│   ├── Archu.Contracts/           # API DTOs
│   ├── Archu.Api/                 # REST API
│   ├── Archu.Ui/                  # Blazor components
│   ├── Archu.ServiceDefaults/     # Aspire defaults
│   ├── Archu.AppHost/             # Aspire orchestrator
│   └── README_NEW_ENTITY.md       # Development guide
└── README.md                      # This file
```

## 🤝 Contributing

1. Follow Clean Architecture principles
2. Include concurrency control for updates
3. Write tests for new features
4. Update documentation
5. Use consistent patterns from existing code

See [docs/README.md](docs/README.md) for detailed contribution guidelines.

## 📄 License

[Your License Here]

## 🙋 Support

- **Documentation**: Start with [docs/README.md](docs/README.md)
- **Issues**: Report on [GitHub Issues](https://github.com/chethandvg/archu/issues)
- **Architecture Questions**: See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- **Concurrency Questions**: See [docs/CONCURRENCY_GUIDE.md](docs/CONCURRENCY_GUIDE.md)

---

**Maintained by**: Archu Development Team  
**Last Updated**: 2025-01-22
