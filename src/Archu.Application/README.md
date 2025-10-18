# Archu.Application

## Overview
The Application layer contains the business use cases and orchestrates the flow of data between the Domain and Infrastructure layers. This layer defines interfaces that are implemented by the outer layers.

## Target Framework
- .NET 9.0

## Responsibilities
- **Abstractions**: Application-level interfaces for cross-cutting concerns
- **Use Cases**: Business workflows and application services (to be implemented)
- **DTOs**: Data transfer objects for application boundaries (if needed)

## Key Components

### Abstractions
- **ICurrentUser**: Interface for accessing the currently authenticated user's information
- **ITimeProvider**: Interface for abstracting system time operations (testability, time zone handling)

## Dependencies
- `Archu.Domain` - references domain entities and abstractions

## Design Principles
- **Dependency Inversion**: Defines interfaces that infrastructure implements
- **Use Case Driven**: Each use case represents a single business operation
- **Framework Agnostic**: No direct dependencies on web frameworks or databases

## Usage
This project is referenced by:
- `Archu.Infrastructure` - to implement abstractions
- `Archu.Api` - to invoke use cases and access abstractions

## Future Enhancements
Consider adding:
- **CQRS** (Command Query Responsibility Segregation) with MediatR
- **FluentValidation** for request validation
- **AutoMapper** for object mapping
- **Application Services** for complex business workflows
- **Specification Pattern** for reusable query logic
