# TentMan.Infrastructure

The Infrastructure layer implements external concerns including database access, authentication, and third-party integrations.

---

## 📁 Folder Structure

```
TentMan.Infrastructure/
├── Authentication/            # Authentication services
│   ├── AuthenticationService.cs
│   ├── JwtTokenGenerator.cs
│   └── README.md
├── Persistence/               # Database access
│   ├── ApplicationDbContext.cs
│   ├── Configurations/       # EF Core entity configurations
│   │   ├── ProductConfiguration.cs
│   │   ├── BuildingConfiguration.cs
│   │   ├── TenantConfiguration.cs      # Tenant management
│   │   ├── LeaseConfiguration.cs       # Lease management
│   │   ├── LeasePartyConfiguration.cs
│   │   ├── LeaseTermConfiguration.cs
│   │   ├── DepositTransactionConfiguration.cs
│   │   ├── ChargeTypeConfiguration.cs              # Billing engine
│   │   ├── LeaseBillingSettingConfiguration.cs
│   │   ├── LeaseRecurringChargeConfiguration.cs
│   │   ├── UtilityRatePlanConfiguration.cs
│   │   ├── UtilityRateSlabConfiguration.cs
│   │   ├── UtilityStatementConfiguration.cs
│   │   ├── InvoiceConfiguration.cs
│   │   ├── InvoiceLineConfiguration.cs
│   │   ├── CreditNoteConfiguration.cs
│   │   ├── CreditNoteLineConfiguration.cs
│   │   ├── InvoiceRunConfiguration.cs
│   │   ├── InvoiceRunItemConfiguration.cs
│   │   └── ...
│   ├── Migrations/           # EF Core migrations
│   └── Interceptors/         # EF Core interceptors
├── Repositories/              # Repository implementations
│   ├── ProductRepository.cs
│   ├── BuildingRepository.cs
│   ├── TenantRepository.cs    # Tenant operations
│   ├── TenantInviteRepository.cs  # Tenant invite operations
│   ├── LeaseRepository.cs     # Lease operations
│   ├── FileMetadataRepository.cs
│   └── ...
├── Time/                      # Time abstractions
│   └── DateTimeProvider.cs
└── DependencyInjection.cs     # Service registration
```

---

## 🎯 Purpose

The Infrastructure layer:
- Implements repository interfaces from Application
- Configures Entity Framework Core
- Provides authentication services
- Handles external service integrations
- Manages database migrations

---

## 📋 Coding Guidelines

### Repository Implementation

```csharp
namespace TentMan.Infrastructure.Repositories;

/// <summary>
/// Repository for product operations.
/// </summary>
public sealed class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;
    
    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<Product?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
    
    public async Task AddAsync(
        Product product, 
        CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

### Entity Configuration

```csharp
namespace TentMan.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Product entity.
/// </summary>
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(p => p.RowVersion)
            .IsRowVersion();
            
        // Soft delete filter
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
```

### File Size Limits

| File Type | Limit | Action |
|-----------|-------|--------|
| Repository | 300 lines | Use partial classes |
| DbContext | 300 lines | Use partial classes |
| Service | 300 lines | Use partial classes |
| Configuration | 100 lines | One per entity |

### Partial Class Example

When `AuthenticationService.cs` exceeds 300 lines:

```
Authentication/
├── AuthenticationService.cs                # Core methods, DI
├── AuthenticationService.Login.cs          # Login methods
├── AuthenticationService.Registration.cs   # Registration methods
└── AuthenticationService.TokenRefresh.cs   # Token refresh logic
```

### DependencyInjection Pattern

```csharp
namespace TentMan.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        
        // Repositories (partial list - see DependencyInjection.cs for full registrations)
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IBuildingRepository, BuildingRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantInviteRepository, TenantInviteRepository>();
        services.AddScoped<ILeaseRepository, LeaseRepository>();
        // ... additional repositories
        
        // Services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        
        return services;
    }
}
```

---

## 🔗 Dependencies

- **TentMan.Domain**: Entity references
- **TentMan.Application**: Interface implementations
- **Microsoft.EntityFrameworkCore**: ORM
- **Microsoft.AspNetCore.Identity**: User management
- **System.IdentityModel.Tokens.Jwt**: JWT handling

---

## 📚 Key Components

### ApplicationDbContext

Central database context with:
- DbSet for each entity
- Global query filters for soft delete
- Audit tracking interceptors
- Concurrency handling

### Repository Pattern

Each aggregate root has a dedicated repository:
- Implements interface from Application layer
- Uses DbContext for data access
- Handles soft delete logic

### Authentication Service

Provides:
- User registration
- Login/logout
- JWT token generation
- Password management
- Token refresh

---

## 🗄️ Database Commands

```bash
# Create migration
dotnet ef migrations add MigrationName \
    --project src/TentMan.Infrastructure \
    --startup-project src/TentMan.Api

# Update database
dotnet ef database update \
    --project src/TentMan.Infrastructure \
    --startup-project src/TentMan.Api
```

---

## ✅ Checklist for New Infrastructure

- [ ] Implement interface from Application layer
- [ ] Register in DependencyInjection.cs
- [ ] Add EF Core configuration if entity
- [ ] Handle soft delete in queries
- [ ] Add proper logging
- [ ] File size under 300 lines
- [ ] Use partial classes if needed

---

**Last Updated**: 2026-01-10  
**Maintainer**: TentMan Development Team
