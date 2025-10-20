# Archu.Application - Documentation Index

## Overview
Welcome to the Archu Application layer documentation. This directory contains all documentation related to the application layer implementation.

---

## 📚 Documentation Structure

### Core Documentation
- **[Application Layer Overview](./01-APPLICATION-OVERVIEW.md)** - Main application layer documentation, architecture, and patterns
- **[Quick Start Guide](./02-QUICK-START.md)** - Get started quickly with common patterns and examples

### Authentication Documentation
- **[Authentication Guide](./Authentication/README.md)** - Complete authentication implementation guide
  - ICurrentUser interface documentation
  - Authentication services and interfaces
  - Commands and queries for auth operations
  - Usage examples and best practices

---

## 🗂️ What's in Each Document

### 01-APPLICATION-OVERVIEW.md
- Project structure and responsibilities
- Key components (Abstractions, Commands, Queries, Validators)
- Architecture patterns (CQRS, Repository, Unit of Work)
- Design principles and best practices
- Testing strategies

### 02-QUICK-START.md
- Quick reference for common operations
- Authentication quick examples
- Role-based authorization snippets
- Command/Query usage patterns

### Authentication/README.md (Consolidated)
- ICurrentUser interface and usage
- IAuthenticationService and ITokenService interfaces
- Authentication commands and queries
- Security best practices
- Complete usage examples
- Testing guidance

---

## 🚀 Quick Links

### Common Tasks
- [Register a new user](./Authentication/README.md#register-command)
- [Login a user](./Authentication/README.md#login-command)
- [Refresh tokens](./Authentication/README.md#refresh-token-command)
- [Check user roles](./Authentication/README.md#icurrentuser-interface)
- [Validate permissions](./Authentication/README.md#role-based-authorization)

### Implementation Guides
- [Add authentication to command handlers](./Authentication/README.md#usage-in-command-handlers)
- [Create new commands/queries](./01-APPLICATION-OVERVIEW.md#cqrs-implementation)
- [Add FluentValidation rules](./01-APPLICATION-OVERVIEW.md#validation)
- [Implement role-based features](./Authentication/README.md#application-roles)

---

## 📖 Related Documentation

### Other Layers
- [Domain Layer](../../Archu.Domain/README.md) - Domain entities and business logic
- [Infrastructure Layer](../../Archu.Infrastructure/README.md) - Data access and external services
- [API Layer](../../Archu.Api/README.md) - HTTP endpoints and controllers

### Architecture
- [Clean Architecture Guide](../../../docs/ARCHITECTURE.md) - Overall solution architecture
- [Domain Identity Entities](../../Archu.Domain/Entities/Identity/README.md) - User and role entities

---

## 🔧 For Developers

### Adding New Features
1. Read the [Application Overview](./01-APPLICATION-OVERVIEW.md) to understand the patterns
2. Check [Authentication Guide](./Authentication/README.md) for auth-related features
3. Follow CQRS pattern for new operations
4. Add FluentValidation validators
5. Update documentation

### Contributing
- Keep documentation up to date
- Follow established patterns
- Add examples for complex features
- Update this index when adding new docs

---

**Last Updated**: 2025-01-22  
**Version**: 2.0  
**Maintainer**: Archu Development Team
