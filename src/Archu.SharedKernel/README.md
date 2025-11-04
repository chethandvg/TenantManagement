# Archu.SharedKernel

This project contains shared constants and cross-cutting concerns that are used across multiple layers of the application.

## Purpose

The SharedKernel follows Clean Architecture principles by providing a central location for:
- Constants used by both Domain and Contracts layers
- Cross-cutting concerns that don't belong to any specific layer
- Shared abstractions with zero external dependencies

## Contents

### Constants

#### CustomClaimTypes
Defines custom JWT claim types used throughout the application:
- `Permission` - Granular permission claims (e.g., "products:create")
- `EmailVerified` - Email verification status
- `TwoFactorEnabled` - MFA status
- `Department` - User department
- `EmployeeId` - Internal employee identifier

#### PermissionNames
Defines all permission strings used for authorization:
- **Products**: Read, Create, Update, Delete, Manage
- **Users**: Read, Create, Update, Delete, Manage
- **Roles**: Read, Create, Update, Delete, Manage

#### RoleNames
Defines system role names:
- Guest
- User
- Manager
- Administrator
- SuperAdmin

## Architecture

The SharedKernel sits at the foundation of the Clean Architecture layers:

```
External Apps (UI, Mobile, 3rd Party)
         ↓
    [Contracts]  ← References SharedKernel
         ↓
   [Application]
         ↓
    [Infrastructure]
         ↓
    [Domain]  ← References SharedKernel
         ↓
    [SharedKernel]  ← NO dependencies
```

## Key Principles

1. **Zero Dependencies**: SharedKernel must not reference any other projects
2. **Immutable Constants**: All constants should be defined as `const` strings
3. **No Business Logic**: SharedKernel contains only data and simple structures
4. **Widely Shared**: Only add items that are truly needed across multiple layers

## Usage

```csharp
using Archu.SharedKernel.Constants;

// Use permission constants
var permission = PermissionNames.Products.Create;

// Use custom claim types
var claimType = CustomClaimTypes.Permission;

// Use role names
var role = RoleNames.Administrator;
```

## Why SharedKernel?

Without SharedKernel, we had two options:
1. Put constants in Domain → External layers can't access without referencing Domain ❌
2. Put constants in Contracts → Domain must reference Contracts (outward dependency) ❌

With SharedKernel:
- Domain references SharedKernel (acceptable minimal dependency) ✅
- Contracts references SharedKernel (acceptable) ✅
- External layers access through Contracts (proper layering) ✅
- Domain remains pure with minimal dependencies ✅
