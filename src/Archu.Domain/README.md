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
- **Product**: Represents a product in the catalog with pricing and inventory information

### Abstractions
- **IAuditable**: Interface for entities that track creation and modification metadata
- **ISoftDeletable**: Interface for entities that support soft deletion (logical deletion)

### Common
- **BaseEntity**: Base class for all domain entities, providing common properties and behavior

## Design Principles
- **Domain-Driven Design (DDD)**: Entities encapsulate business rules and invariants
- **Framework Independence**: No external dependencies except core .NET libraries
- **Rich Domain Model**: Business logic lives in the domain, not in services

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
