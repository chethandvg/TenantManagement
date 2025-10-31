# Archu - Modern .NET Platform

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

### 🎯 Quick Navigation

| For... | Start Here | Then Read |
|--------|------------|-----------|
| **New Developers** | [QUICKSTART.md](QUICKSTART.md) | [Getting Started](docs/GETTING_STARTED.md) → [Architecture](docs/ARCHITECTURE.md) |
| **Understanding the System** | [Architecture Guide](docs/ARCHITECTURE.md) | [Development Guide](docs/DEVELOPMENT_GUIDE.md) |
| **Adding Features** | [Adding New Entity](docs/getting-started/ADDING_NEW_ENTITY.md) | [Development Guide](docs/DEVELOPMENT_GUIDE.md) |
| **API Development** | [API Guide](docs/API_GUIDE.md) | [Database Guide](docs/DATABASE_GUIDE.md) |
| **Authentication/Security** | [Authentication Guide](docs/AUTHENTICATION_GUIDE.md) | [Authorization Guide](docs/AUTHORIZATION_GUIDE.md) |
| **Testing** | [Testing Guide](tests/TESTING_GUIDE.md) | [Integration Tests](tests/INTEGRATION_TESTING_GUIDE.md) |
| **All Documentation** | [📖 Documentation Hub](docs/README.md) | Browse all guides |

### 📁 Essential Documents

- **[QUICKSTART.md](QUICKSTART.md)** - Get running in 5 minutes
- **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)** - System design & patterns
- **[docs/DEVELOPMENT_GUIDE.md](docs/DEVELOPMENT_GUIDE.md)** - Development workflow
- **[docs/getting-started/ADDING_NEW_ENTITY.md](docs/getting-started/ADDING_NEW_ENTITY.md)** - Step-by-step feature development
- **[docs/database/CONCURRENCY_GUIDE.md](docs/database/CONCURRENCY_GUIDE.md)** - Data integrity & concurrency
- **[tests/TESTING_GUIDE.md](tests/TESTING_GUIDE.md)** - Testing strategy

### Quick Links
| Topic | Document |
|-------|----------|
| 🏗️ Architecture & Design | [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) |
| 🔒 Concurrency & Data | [docs/database/CONCURRENCY_GUIDE.md](docs/database/CONCURRENCY_GUIDE.md) |
| ➕ Adding Features | [docs/getting-started/ADDING_NEW_ENTITY.md](docs/getting-started/ADDING_NEW_ENTITY.md) |
| 📖 API Reference | [src/Archu.Api/README.md](src/Archu.Api/README.md) |

## 🏗️ Architecture

Archu follows **Clean Architecture** principles with clear separation of concerns:

```
┌─────────────────────────┐
│   Archu.AppHost    │  .NET Aspire orchestration
└────────┬────────────────┘
  │
    ┌────▼─────┐
    │ Archu.Api│  ASP.NET Core Web API
    └────┬─────┘
   │
         ├─ Archu.Infrastructure  (EF Core, Repositories)
         │     └─ Archu.Application  (CQRS, Use Cases)
     │      └─ Archu.Domain  (Entities, Business Logic)
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

**Layer Documentation:**
- **[Domain Layer](src/Archu.Domain/README.md)** - Business entities and logic (zero dependencies)
- **[Application Layer](src/Archu.Application/README.md)** - Use cases and CQRS handlers
- **[Infrastructure Layer](src/Archu.Infrastructure/README.md)** - Data access and repositories
- **[Contracts Layer](src/Archu.Contracts/README.md)** - API DTOs and request/response models
- **[AppHost](src/Archu.AppHost/README.md)** - .NET Aspire orchestration and service management ⭐ NEW
- **[ServiceDefaults](src/Archu.ServiceDefaults/README.md)** - Shared observability and resilience configuration ⭐ NEW

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

# Run specific test projects
dotnet test tests/Archu.IntegrationTests  # API integration tests (17 tests)
dotnet test tests/Archu.ApiClient.Tests   # HTTP client tests (11 tests)
dotnet test tests/Archu.Ui.Tests       # Accessibility tests (2 tests)
dotnet test tests/Archu.UnitTests      # Business logic tests (37 test classes)

# Run with coverage
dotnet test /p:CollectCoverage=true
```

**Test Documentation**:
- 📖 **[Testing Guide](tests/TESTING_GUIDE.md)** - Comprehensive testing strategy
- 📖 **[Integration Testing](tests/INTEGRATION_TESTING_GUIDE.md)** - API integration tests
- 📖 **[Integration Tests](tests/Archu.IntegrationTests/README.md)** - API endpoint testing
- 📖 **[API Client Tests](tests/Archu.ApiClient.Tests/README.md)** - HTTP client testing
- 📖 **[UI Tests](tests/Archu.Ui.Tests/README.md)** - Accessibility testing
- 📖 **[Unit Tests](tests/Archu.UnitTests/README.md)** - Domain & Application logic testing

## 🔧 Common Tasks

### Create a Migration
```bash
cd src/Archu.Infrastructure
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

### Add a New Entity
Follow the guide: **[docs/getting-started/ADDING_NEW_ENTITY.md](docs/getting-started/ADDING_NEW_ENTITY.md)**

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
├── docs/         # 📚 All documentation
│   ├── README.md    # Documentation hub & navigation
│   ├── ARCHITECTURE.md  # System architecture & design
│   ├── DEVELOPMENT_GUIDE.md    # Development workflow
│   ├── GETTING_STARTED.md# Detailed setup guide
│   ├── API_GUIDE.md            # API documentation
│   ├── DATABASE_GUIDE.md       # Database guide
│   ├── AUTHENTICATION_GUIDE.md # Authentication & security
│   ├── AUTHORIZATION_GUIDE.md  # Authorization & permissions
│   ├── authentication/     # Detailed auth documentation
│   ├── database/               # Database-specific guides
│   │   └── CONCURRENCY_GUIDE.md # Data integrity & concurrency
│   ├── getting-started/
│   │   └── ADDING_NEW_ENTITY.md # Entity creation guide
│   └── archu-ui/         # UI documentation
├── src/
│   ├── Archu.Domain/       # Business logic (no dependencies)
│ │   └── README.md      # ⭐ Domain layer documentation
│   ├── Archu.Application/      # Use cases, CQRS handlers
│   │   └── README.md    # Application layer documentation
│   ├── Archu.Infrastructure/   # EF Core, repositories
│   │   └── README.md         # Infrastructure layer documentation
│   ├── Archu.Contracts/        # API DTOs
│   │   └── README.md      # ⭐ Contracts layer documentation
│   ├── Archu.Api/     # REST API
│   │   └── README.md           # API project documentation
│   ├── Archu.AdminApi/         # Admin API
│   │   └── README.md           # Admin API documentation
│   ├── Archu.ApiClient/        # HTTP client library
│   │   └── README.md        # API client documentation
│├── Archu.Ui/ # Blazor components
│   │   └── README.md         # UI project documentation
│   ├── Archu.Web/       # Web project
│ │   └── README.md    # Web project documentation
│   ├── Archu.ServiceDefaults/  # Aspire defaults
│   │   └── README.md     # Service defaults documentation
│   └── Archu.AppHost/          # Aspire orchestrator
│└── README.md # App host documentation
├── tests/          # 🧪 Test projects
│   ├── README.md          # Testing overview
│   ├── TESTING_GUIDE.md        # Comprehensive testing guide
│   ├── INTEGRATION_TESTING_GUIDE.md # Integration testing guide
│   ├── Archu.IntegrationTests/ # API integration tests (17 tests)
│   ├── Archu.ApiClient.Tests/  # HTTP client tests (11 tests)
│   ├── Archu.Ui.Tests/         # Accessibility tests (2 tests)
│   └── Archu.UnitTests/# Business logic tests (37 test classes)
├── README.md       # This file - project overview
└── QUICKSTART.md               # Fast setup guide
```

## 🤝 Contributing

1. Follow Clean Architecture principles
2. Include concurrency control for updates
3. Write tests for new features
4. Update documentation
5. Use consistent patterns from existing code

See **[docs/README.md](docs/README.md)** for detailed contribution guidelines and **[docs/DEVELOPMENT_GUIDE.md](docs/DEVELOPMENT_GUIDE.md)** for development workflow.

## 📄 License

[Your License Here]

## 🙋 Support

- **📚 Documentation**: Start with **[docs/README.md](docs/README.md)** - your navigation hub
- **🐛 Issues**: Report on [GitHub Issues](https://github.com/chethandvg/archu/issues)
- **🏗️ Architecture**: See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- **🔐 Authentication**: See [docs/AUTHENTICATION_GUIDE.md](docs/AUTHENTICATION_GUIDE.md)
- **🗄️ Database & Concurrency**: See [docs/database/CONCURRENCY_GUIDE.md](docs/database/CONCURRENCY_GUIDE.md)
- **🧪 Testing**: See [tests/TESTING_GUIDE.md](tests/TESTING_GUIDE.md)

### 📖 Documentation Status

This project maintains clean, organized documentation. See **[DOCUMENTATION_CLEANUP_SUMMARY.md](DOCUMENTATION_CLEANUP_SUMMARY.md)** for the complete documentation structure and recent improvements.

---

**Maintained by**: Archu Development Team  
**Last Updated**: 2025-01-23
