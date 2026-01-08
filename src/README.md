# TentMan Source Code

This directory contains all source code projects for the TentMan Tenant Management System.

---

## 📁 Project Structure

```
src/
├── TentMan.Domain/           # Core business entities and logic
├── TentMan.Application/      # Use cases, CQRS handlers, business rules
├── TentMan.Infrastructure/   # Data access, external services, implementations
├── TentMan.Contracts/        # DTOs, request/response models
├── TentMan.Api/              # Main REST API
├── TentMan.AdminApi/         # Administrative API
├── TentMan.ApiClient/        # HTTP client library
├── TentMan.Ui/               # Blazor UI component library
├── TentMan.Web/              # Blazor Web host application
├── TentMan.ServiceDefaults/  # .NET Aspire service defaults
└── TentMan.AppHost/          # .NET Aspire orchestrator
```

---

## 🏛️ Clean Architecture Layers

The projects follow Clean Architecture with dependency inversion:

```
                    ┌─────────────────────┐
                    │    TentMan.AppHost    │  Orchestration
                    └──────────┬──────────┘
                               │
          ┌────────────────────┼────────────────────┐
          │                    │                    │
    ┌─────▼─────┐        ┌─────▼─────┐        ┌────▼────┐
    │ TentMan.Api │        │TentMan.AdminApi│        │TentMan.Web│
    └─────┬─────┘        └─────┬─────┘        └────┬────┘
          │                    │                    │
          └────────────────────┼────────────────────┘
                               │
                    ┌──────────▼──────────┐
                    │ TentMan.Infrastructure │  External Concerns
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │  TentMan.Application   │  Business Rules
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │    TentMan.Domain     │  Core Entities
                    └─────────────────────┘
```

### Dependency Rules

- **Domain**: No dependencies on other projects
- **Application**: Depends only on Domain
- **Infrastructure**: Depends on Application and Domain
- **API/UI**: Depends on all layers

---

## 📋 Project Summaries

| Project | Purpose | Key Technologies |
|---------|---------|-----------------|
| **Domain** | Entities, value objects, enums | Pure C# |
| **Application** | CQRS handlers, validators, interfaces | MediatR, FluentValidation |
| **Infrastructure** | EF Core, repositories, auth services | Entity Framework, Identity |
| **Contracts** | DTOs, API models | Records, validation attributes |
| **Api** | REST endpoints, middleware | ASP.NET Core, Scalar |
| **AdminApi** | Admin endpoints, system init | ASP.NET Core |
| **ApiClient** | HTTP client, resilience | HttpClientFactory, Polly |
| **Ui** | Blazor components, layouts | Blazor, MudBlazor |
| **Web** | Blazor host application | Blazor Server/WASM |
| **ServiceDefaults** | Aspire defaults | .NET Aspire |
| **AppHost** | Service orchestration | .NET Aspire |

---

## 🔧 Development Guidelines

### Code Organization Rules

All C# files should follow these limits:

| Rule | Limit | Action When Exceeded |
|------|-------|---------------------|
| Lines per file | **300** (+30 max) | Use partial classes |
| Methods per class | 10-15 | Extract service classes |
| Parameters per method | 5-7 | Use parameter objects |
| Dependencies per class | 5-7 | Use facade pattern |

### Partial Class Usage

When a class exceeds 300 lines:

```
MyService.cs                    # Core definition and main methods
MyService.Validation.cs         # Validation logic
MyService.Mapping.cs            # Mapping logic
MyService.Events.cs             # Event handling
```

### Adding New Features

Follow the guide: **[README_NEW_ENTITY.md](README_NEW_ENTITY.md)**

---

## 🚀 Running the Application

```bash
# Start with Aspire orchestration
cd src/TentMan.AppHost
dotnet run
```

### Access Points

- **Main API**: https://localhost:7123
- **Admin API**: https://localhost:7290
- **Aspire Dashboard**: https://localhost:15XXX (check console)
- **API Docs**: https://localhost:7123/scalar/v1

---

## 📚 Documentation

Each project folder contains a `README.md` with:
- Purpose and responsibilities
- Folder structure
- Coding guidelines specific to that project
- Usage examples

See [CONTRIBUTING.md](../CONTRIBUTING.md) for complete coding guidelines.

---

**Last Updated**: 2026-01-08  
**Maintainer**: TentMan Development Team
