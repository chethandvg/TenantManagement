# Archu.Application

## Overview
The Application layer contains business use cases and orchestrates data flow between Domain and Infrastructure layers. This layer defines interfaces implemented by outer layers.

## Target Framework
- .NET 9.0

## Quick Links
📖 **[Complete Documentation](./docs/README.md)** - Full documentation index  
🚀 **[Quick Start Guide](./docs/02-QUICK-START.md)** - Get started quickly  
🔐 **[Authentication Guide](./docs/Authentication/README.md)** - Authentication implementation  

## Project Structure

```
Archu.Application/
├── Abstractions/           # Interfaces for cross-cutting concerns
│   ├── Authentication/     # Auth service interfaces
│   ├── ICurrentUser.cs    # Current user context
│   ├── IUnitOfWork.cs     # Transaction management
│   └── Repositories/      # Data access abstractions
├── Auth/                  # Authentication operations (CQRS)
│   ├── Commands/          # Auth commands (Register, Login, etc.)
│   └── Queries/           # Auth queries (ValidateToken, etc.)
├── Products/              # Product feature (example)
│   ├── Commands/          # Product commands
│   └── Queries/           # Product queries
├── Common/                # Shared application logic
│   ├── Behaviors/         # MediatR pipeline behaviors
│   ├── ApplicationRoles.cs # Role constants
│   └── Result.cs          # Result pattern
└── docs/                  # 📚 Documentation
    ├── README.md          # Documentation index
    ├── 01-APPLICATION-OVERVIEW.md
    ├── 02-QUICK-START.md
    └── Authentication/    # Auth documentation
        └── README.md
```

## Key Features

✅ **CQRS Pattern** - Clear separation of commands and queries  
✅ **FluentValidation** - Automatic request validation  
✅ **MediatR Pipeline** - Validation and performance behaviors  
✅ **Authentication** - Complete auth with JWT support  
✅ **Role-Based Authorization** - Fine-grained access control  
✅ **Result Pattern** - Consistent error handling  

## Quick Examples

### Command Handler with Authorization
```csharp
public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsInRole(ApplicationRoles.Admin))
            return Result.Failure("Admin access required");
        
        // Proceed with deletion
    }
}
```

### Authentication Flow
```csharp
// Register
var registerCommand = new RegisterCommand { Email = "...", Password = "..." };
var result = await _mediator.Send(registerCommand);

// Login
var loginCommand = new LoginCommand { Email = "...", Password = "..." };
var authResult = await _mediator.Send(loginCommand);

// Use access token for API calls
```

## Dependencies
- `Archu.Domain` - Domain entities and abstractions
- `MediatR` - CQRS pattern implementation
- `FluentValidation` - Request validation

## Getting Started

1. **Read the documentation**: Start with [docs/README.md](./docs/README.md)
2. **Explore examples**: Check [Quick Start Guide](./docs/02-QUICK-START.md)
3. **Implement features**: Follow CQRS pattern with commands/queries
4. **Add authentication**: Use [Authentication Guide](./docs/Authentication/README.md)

## Documentation

📖 **[Full Documentation Index](./docs/README.md)**

**Core Guides:**
- [Application Overview](./docs/01-APPLICATION-OVERVIEW.md) - Architecture and patterns
- [Quick Start](./docs/02-QUICK-START.md) - Common patterns and examples
- [Authentication](./docs/Authentication/README.md) - Complete auth guide

## Best Practices

✅ Use CQRS pattern for all operations  
✅ Validate all commands with FluentValidation  
✅ Use `ApplicationRoles` constants for role names  
✅ Return `Result<T>` for consistent error handling  
✅ Log authorization failures for security auditing  
✅ Check authentication/authorization in handlers  

## Version History

### v2.0.0 (2025-01-22)
- 📚 Reorganized documentation into `docs/` folder
- ✨ Complete authentication implementation
- ✨ Enhanced ICurrentUser with role-based authorization
- ✨ Added ApplicationRoles constants
- ✨ Added authentication commands and queries

### v1.0.0
- ✅ CQRS pattern with MediatR
- ✅ FluentValidation
- ✅ Repository and Unit of Work patterns
- ✅ Pipeline behaviors

---

**For detailed documentation, see [docs/README.md](./docs/README.md)**

**Maintainer**: Archu Development Team  
**Last Updated**: 2025-01-22
