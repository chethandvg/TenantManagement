# Archu Documentation

Welcome to the Archu project documentation!

## 📚 Quick Links

| Topic | Description |
|-------|-------------|
| **[Project Structure](./PROJECT_STRUCTURE.md)** | All projects overview and responsibilities |
| **[Architecture](./ARCHITECTURE.md)** | Clean Architecture design and patterns |
| **[Adding New Entity](./getting-started/ADDING_NEW_ENTITY.md)** | Step-by-step CRUD guide |
| **[Authentication](./authentication/)** | JWT, passwords, user management |
| **[Database](./database/)** | Concurrency control, EF Core, migrations |

---

## 🚀 Getting Started

### For New Developers
1. Read [Project Structure](./PROJECT_STRUCTURE.md)
2. Understand [Architecture](./ARCHITECTURE.md)
3. Try [Adding a New Entity](./getting-started/ADDING_NEW_ENTITY.md)

### For Feature Development
1. **Authentication** - [JWT Implementation](./authentication/JWT_TOKEN_IMPLEMENTATION.md)
2. **Database** - [Concurrency Guide](./database/CONCURRENCY_GUIDE.md)
3. **Current User** - [CurrentUser Service](./authentication/CURRENT_USER_SERVICE.md)

---

## 📖 Documentation Map

```
docs/
├── README.md                                    # This file
├── PROJECT_STRUCTURE.md                         # All projects explained
├── ARCHITECTURE.md                              # Architecture guide
├── getting-started/
│   └── ADDING_NEW_ENTITY.md                     # Complete CRUD guide
├── authentication/
│   ├── JWT_TOKEN_IMPLEMENTATION.md              # JWT tokens (complete guide)
│   ├── INFRASTRUCTURE_AUTH_SETUP.md             # Auth database setup
│   └── CURRENT_USER_SERVICE.md                  # Current user access
└── database/
    └── CONCURRENCY_GUIDE.md                     # Concurrency & soft delete
```

---

## 🎯 Common Tasks

### Setup Development Environment
```bash
# Clone repository
git clone https://github.com/chethandvg/archu.git

# Update connection string in appsettings.Development.json
"ConnectionStrings": {
  "Sql": "Server=localhost;Database=archuDatabase;Trusted_Connection=True;TrustServerCertificate=True;"
}

# Apply migrations
cd src/Archu.Infrastructure
dotnet ef database update --startup-project ../Archu.Api

# Run with Aspire
cd ../Archu.AppHost
dotnet run
```

### Add New Feature
1. Create entity in `Archu.Domain`
2. Create repository in `Archu.Application` (interface) and `Archu.Infrastructure` (implementation)
3. Create commands/queries in `Archu.Application`
4. Add API endpoints in `Archu.Api`

Full guide: [Adding New Entity](./getting-started/ADDING_NEW_ENTITY.md)

---

## 🔐 Authentication & Security

### JWT Authentication
- [Complete Implementation Guide](./authentication/JWT_TOKEN_IMPLEMENTATION.md)
- [Database Setup](./authentication/INFRASTRUCTURE_AUTH_SETUP.md)
- [CurrentUser Service](./authentication/CURRENT_USER_SERVICE.md)

### Quick Setup
```csharp
// appsettings.json
{
  "Jwt": {
    "Secret": "YourSecretKey32CharactersMinimum!",
    "Issuer": "https://api.archu.com",
    "Audience": "https://api.archu.com",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}

// Program.cs
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
```

---

## 🗄️ Database & Persistence

### Key Features
- **Optimistic Concurrency**: RowVersion-based conflict detection
- **Soft Delete**: Records marked as deleted, not removed
- **Audit Tracking**: Automatic created/modified tracking

### Guide
[Complete Concurrency Guide](./database/CONCURRENCY_GUIDE.md)

### Quick Reference
```csharp
// Entity with concurrency, auditing, soft delete
public class Product : BaseEntity
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// Update with concurrency control
await _repository.UpdateAsync(product, request.RowVersion);
await _unitOfWork.SaveChangesAsync();
```

---

## 🏗️ Clean Architecture Layers

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

See: [Architecture Guide](./ARCHITECTURE.md)

---

## 📦 Project Breakdown

See [Project Structure](./PROJECT_STRUCTURE.md) for complete details on all 9 projects.

**Core Projects**:
- **Domain** - Business entities (zero dependencies)
- **Application** - Use cases, CQRS, abstractions
- **Infrastructure** - Data access, external services
- **Contracts** - API DTOs
- **Api** - REST endpoints

**Supporting**:
- **ApiClient** - Typed HTTP client
- **Ui** - Blazor components
- **AppHost** - Aspire orchestrator
- **ServiceDefaults** - Shared config

---

## 🔄 Development Workflow

1. **Create feature branch**
   ```bash
   git checkout -b feature/your-feature
   ```

2. **Make changes** (follow architecture)

3. **Test locally**
   ```bash
   dotnet build
   dotnet test
   ```

4. **Commit and push**
   ```bash
   git commit -m "Add: feature description"
   git push origin feature/your-feature
   ```

5. **Create pull request**

---

## 🛠️ Troubleshooting

### Database Connection Issues
- Check SQL Server is running
- Verify connection string
- Ensure firewall allows connections

### Migration Failures
- Ensure correct directory: `src/Archu.Infrastructure`
- Specify startup project: `--startup-project ../Archu.Api`

### Build Errors
```bash
dotnet restore
dotnet clean
dotnet build
```

---

## 📞 Support

- **Documentation Issues**: Create an issue
- **Questions**: GitHub Discussions
- **Bugs**: GitHub Issues

---

**Last Updated**: 2025-01-22  
**Version**: 3.0 (Streamlined)  
**Total Documentation**: 10 core files (56% reduction)
