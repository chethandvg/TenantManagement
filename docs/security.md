# Archu Security Architecture

This document captures how the Archu platform reuses a single identity store, JWT configuration, and permission model across every API surface. It is the canonical reference for service owners that need to integrate with the shared authentication and authorization infrastructure.

## Shared ApplicationDbContext & Connection String

Both **Archu.Api** and **Archu.AdminApi** call `services.AddInfrastructure(configuration, environment)` during startup, which registers `ApplicationDbContext` with the same configuration for both APIs. The Infrastructure layer resolves the connection string from the `Sql` (fallback `archudb`) connection string entry and wires up Entity Framework Core with retry and migration settings. This ensures every microservice using the Infrastructure package interacts with the same SQL Server database and identity schema.

- `src/Archu.Api/Program.cs` → `builder.Services.AddInfrastructure(...)`
- `src/Archu.AdminApi/Program.cs` → `builder.Services.AddInfrastructure(...)`
- `src/Archu.Infrastructure/DependencyInjection.cs` → `AddDatabase` configures `ApplicationDbContext` using the shared connection string

Because both APIs resolve the context from the same DI registration, every write to roles, permissions, or tokens is persisted to the same database and becomes visible to the other API after the transaction is committed. Note that visibility of changes may depend on transaction isolation levels and caching, so changes may not be immediately reflected until the relevant transaction is completed and caches are refreshed. Future services should reuse the Infrastructure package instead of registering their own DbContext to stay aligned with the platform defaults.

## Canonical Identity Store & Permission Entities

`ApplicationDbContext` exposes all identity tables, including the new permission entities that power fine-grained authorization:

| Entity | Purpose | DbSet |
| ------ | ------- | ----- |
| `ApplicationPermission` | Catalog of discrete permissions (`Name`, `NormalizedName`, description) | `ApplicationDbContext.Permissions` |
| `RolePermission` | Junction table linking roles to permissions | `ApplicationDbContext.RolePermissions` |
| `UserPermission` | Junction table assigning permissions directly to users | `ApplicationDbContext.UserPermissions` |

These DbSets live alongside existing identity entities (`ApplicationUser`, `ApplicationRole`, `UserRole`) in the same context. Repositories such as `PermissionRepository`, `RolePermissionRepository`, and `UserPermissionRepository` are registered by the Infrastructure layer so every API can query or mutate the canonical permission data without custom plumbing.

## JWT Issuer/Audience/Signing Key Reuse

`AddInfrastructure` also wires up the shared JWT authentication stack. `AddAuthenticationServices` binds the `Jwt` configuration section into `JwtOptions`, validates the issuer/audience/secret, and registers `JwtTokenService` plus the ASP.NET Core JWT bearer handler. Because both APIs call the same extension, they all rely on identical issuer, audience, and signing key settings, guaranteeing tokens created by one API are trusted by the others.

If you introduce a new service, reference the same configuration section (and ideally the same configuration source via Aspire/AppHost) to avoid drift. Changing these values requires rotating secrets across every consumer.

## Permission Propagation Into Tokens

`AuthenticationService.GenerateAuthenticationResultAsync` is responsible for minting access tokens. During login/registration it loads the caller’s roles, resolves role-assigned permissions via `RolePermissions`, gathers direct grants from `UserPermissions`, and then hydrates the token with `CustomClaimTypes.Permission` claims. Any role lacking explicit database assignments falls back to the legacy static permissions defined in `RolePermissionClaims` so partially migrated tenants continue to function. The generated JWT includes:

- Standard identity claims (subject, email, username)
- Role claims for every role assignment
- Permission claims representing the union of role and direct permissions
- Feature flags such as `EmailVerified` and `TwoFactorEnabled`

Because the Admin API issues tokens using the same infrastructure services, tokens from either API carry the same claim shape. Downstream services can therefore rely on JWT permissions as the source of truth without re-querying the database.

## Integrating New Services

When building a new API or background worker that needs authentication/authorization:

1. Reference `Archu.Infrastructure` and call `services.AddInfrastructure(configuration, environment)`.
2. Consume `ApplicationDbContext` or the repository abstractions from the Application layer to work with identity, roles, and permissions.
3. Configure your host to read the shared `Jwt` configuration so token validation matches the rest of the platform.
4. Use the JWT permission claims to enforce policies instead of duplicating permission logic.

Following these steps keeps every service aligned with the canonical identity store while preserving centralized token issuance and validation.
