# Archu.Infrastructure

## Overview
The Infrastructure layer contains implementations of interfaces defined in the Application layer. This includes data access, external services, file system access, and other infrastructure concerns.

## Target Framework
- .NET 9.0

## Responsibilities
- **Persistence**: Database context, configurations, and migrations
- **External Services**: Integration with third-party APIs
- **Cross-Cutting Concerns**: Logging, caching, email, etc.

## Key Components

### Persistence
- **ApplicationDbContext**: Main Entity Framework Core DbContext
- **Configurations**: Fluent API configurations for entities (e.g., ProductConfiguration)
- **Migrations**: EF Core database migrations
- **DesignTimeDbContextFactory**: Factory for EF Core tooling support

### Time
- **SystemTimeProvider**: Implementation of `ITimeProvider` using `TimeProvider.System`

## Technologies
- **Entity Framework Core**: ORM for data access
- **SQL Server**: Database provider

## Database Configuration
- Uses SQL Server with retry on failure
- Command timeout: 30 seconds
- Connection string: `archudb` or fallback to `Sql`

## Migrations
To add a new migration:
```bash
dotnet ef migrations add <MigrationName> --project src/Archu.Infrastructure
```

To update the database:
```bash
dotnet ef database update --project src/Archu.Infrastructure
```

## Dependencies
- `Archu.Domain` - for entity definitions
- `Archu.Application` - for interface implementations
- `Microsoft.EntityFrameworkCore.SqlServer` - for SQL Server support
- `Microsoft.EntityFrameworkCore.Design` - for design-time tooling

## Design Patterns
- **Repository Pattern**: Encapsulated in DbContext
- **Unit of Work**: Provided by DbContext
- **Configuration Pattern**: Separate configuration classes for each entity

## Best Practices
- Keep infrastructure code isolated from business logic
- Use configurations to define database schema
- Implement interfaces defined in Application layer
- Handle infrastructure exceptions appropriately
