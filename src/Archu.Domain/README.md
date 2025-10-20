# Archu.Domain

## Overview
The Domain project contains the core business logic and domain models for the Archu application. This layer is at the heart of the Clean Architecture and has no dependencies on other projects.

## Target Framework
- .NET 9.0

## Responsibilities
- **Entities**: Core business objects with unique identities
- **Abstractions**: Domain-level interfaces and contracts
- **Common**: Shared domain primitives and base classes

## Key Components

### Entities

#### Business Entities
- **Product**: Represents a product in the catalog with pricing and inventory information

#### Identity Entities (Authentication & Authorization)
- **ApplicationUser**: Represents an authenticated user with credentials, tokens, and security features
- **ApplicationRole**: Represents a security role (e.g., Admin, User, Manager)
- **UserRole**: Junction entity for many-to-many user-role relationships

See [Identity/README.md](./Entities/Identity/README.md) for detailed documentation on authentication entities.

### Abstractions
- **IAuditable**: Interface for entities that track creation and modification metadata
- **ISoftDeletable**: Interface for entities that support soft deletion (logical deletion)

### Common
- **BaseEntity**: Base class for all domain entities, providing common properties and behavior
  - Unique identifier (GUID)
  - Auditing properties (CreatedAtUtc, ModifiedAtUtc, etc.)
  - Soft delete properties (IsDeleted, DeletedAtUtc, etc.)
  - Concurrency control (RowVersion)

## Design Principles
- **Domain-Driven Design (DDD)**: Entities encapsulate business rules and invariants
- **Framework Independence**: No external dependencies except core .NET libraries
- **Rich Domain Model**: Business logic lives in the domain, not in services
- **Clean Architecture**: Domain has no dependencies on outer layers

## Usage
This project should be referenced by:
- `Archu.Application` - for defining application services and use cases
- `Archu.Infrastructure` - for implementing persistence and data access
- `Archu.Api` - for exposing domain through API endpoints

## Best Practices
- Keep domain entities focused on business logic
- Avoid infrastructure concerns (database, external services)
- Use value objects for complex domain concepts without identity
- Define domain events for cross-aggregate communication
- All entities inherit from `BaseEntity` for consistency
- Use meaningful domain concepts and ubiquitous language

## Entity Features

### Automatic Features (via BaseEntity)
All entities inheriting from `BaseEntity` automatically get:

✅ **Auditing**: Tracks who created/modified and when
✅ **Soft Delete**: Logical deletion (data retained for compliance)
✅ **Concurrency Control**: Optimistic locking via RowVersion
✅ **Unique Identifiers**: GUID-based IDs

### Security Features (Identity Entities)
Authentication entities provide:

✅ **Password Hashing**: Secure password storage
✅ **JWT Refresh Tokens**: Secure token renewal
✅ **Account Lockout**: Brute force protection
✅ **Email Verification**: Account confirmation
✅ **Role-Based Access**: Permission management
✅ **Security Stamps**: Token invalidation on credential changes
