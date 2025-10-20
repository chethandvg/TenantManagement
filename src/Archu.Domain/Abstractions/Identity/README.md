# Identity Abstractions and Role-Based Access Control

This directory contains domain-level abstractions, enums, and constants for implementing role-based access control (RBAC) and permission management.

## 📁 Structure

```
Archu.Domain/
├── Abstractions/Identity/
│   ├── IRoleService.cs          # Role management operations
│   ├── IPermissionService.cs    # Permission checking and management
│   ├── IHasOwner.cs             # Ownership tracking interface
│   └── IHasSharedAccess.cs      # Shared access interface
├── Enums/
│   ├── SystemRole.cs            # System role enumeration
│   └── Permission.cs            # Permission flags enumeration
├── Constants/
│   ├── RoleNames.cs             # Role name constants
│   └── RolePermissions.cs       # Default role-to-permission mappings
└── Extensions/
    └── PermissionExtensions.cs  # Permission utility methods
```

## 🎯 Core Concepts

### 1. System Roles (Enum)

Defines the hierarchy of roles in the application:

- **Guest** (0) - Minimal read-only access
- **User** (1) - Basic application access
- **Manager** (2) - Team management capabilities
- **Administrator** (3) - Full system access
- **SuperAdmin** (4) - Unrestricted access

**Usage:**
```csharp
var role = SystemRole.Administrator;
var roleValue = (int)role; // 3
```

### 2. Permissions (Flags Enum)

Fine-grained permissions using bitwise flags:

- `Read`, `Create`, `Update`, `Delete` - CRUD operations
- `ManageUsers`, `ManageRoles` - User/role management
- `SystemConfiguration` - System settings
- `ViewAuditLogs` - Audit trail access
- `ExportData`, `ImportData` - Data operations
- `All` - Combined permissions

**Usage:**
```csharp
// Combine permissions
var userPerms = Permission.Read | Permission.Create | Permission.Update;

// Check permission
if (userPerms.HasPermission(Permission.Create))
{
    // User can create
}

// Check multiple permissions
if (userPerms.HasAllPermissions(Permission.Read, Permission.Create))
{
    // User has both permissions
}
```

### 3. Role Service Interface

Defines operations for role management:

```csharp
public interface IRoleService
{
    Task<bool> IsInRoleAsync(Guid userId, string roleName, ...);
    Task<IEnumerable<string>> GetUserRolesAsync(Guid userId, ...);
    Task<bool> AssignRoleAsync(Guid userId, string roleName, ...);
    Task<bool> RemoveRoleAsync(Guid userId, string roleName, ...);
    // ... more methods
}
```

**Implementation Note:** This interface should be implemented in the **Application** or **Infrastructure** layer, not in the Domain layer.

### 4. Permission Service Interface

Defines operations for permission checking:

```csharp
public interface IPermissionService
{
    Task<bool> HasPermissionAsync(Guid userId, Permission permission, ...);
    Task<Permission> GetUserPermissionsAsync(Guid userId, ...);
    Task<bool> HasAllPermissionsAsync(Guid userId, IEnumerable<Permission> permissions, ...);
    // ... more methods
}
```

### 5. Ownership and Access Control

#### IHasOwner
For entities that belong to a specific user:

```csharp
public class Document : BaseEntity, IHasOwner
{
    public Guid OwnerId { get; set; }
    
    public bool IsOwnedBy(Guid userId) => OwnerId == userId;
}
```

#### IHasSharedAccess
For entities shared among multiple users:

```csharp
public class Project : BaseEntity, IHasSharedAccess
{
    public ICollection<Guid> SharedWithUserIds { get; } = new List<Guid>();
    
    public bool HasAccess(Guid userId) => SharedWithUserIds.Contains(userId);
    
    public void GrantAccessTo(Guid userId)
    {
        if (!SharedWithUserIds.Contains(userId))
            SharedWithUserIds.Add(userId);
    }
    
    public void RevokeAccessFrom(Guid userId)
    {
        SharedWithUserIds.Remove(userId);
    }
}
```

## 🔐 Role Permissions Mapping

Default permissions for each role:

| Role            | Permissions                                                                 |
|----------------|-----------------------------------------------------------------------------|
| **Guest**       | Read                                                                        |
| **User**        | Read, Create, Update                                                        |
| **Manager**     | Read, Create, Update, Delete, ExportData, ViewAuditLogs                     |
| **Administrator** | Read, Create, Update, Delete, ManageUsers, ExportData, ImportData, ViewAuditLogs |
| **SuperAdmin**  | All                                                                         |

**Usage:**
```csharp
using Archu.Domain.Constants;

// Get default permissions for a role
var adminPerms = RolePermissions.AdministratorPermissions;

// Check if role has permission
bool canManageUsers = RolePermissions.RoleHasPermission(
    RoleNames.Administrator, 
    Permission.ManageUsers); // true
```

## 🛠️ Extension Methods

### PermissionExtensions

Utility methods for working with permissions:

```csharp
using Archu.Domain.Extensions;

var permissions = Permission.Read | Permission.Create;

// Check single permission
if (permissions.HasPermission(Permission.Read)) { }

// Check any permission
if (permissions.HasAnyPermission(Permission.Read, Permission.Delete)) { }

// Check all permissions
if (permissions.HasAllPermissions(Permission.Read, Permission.Create)) { }

// Add permission
permissions = permissions.AddPermission(Permission.Update);

// Remove permission
permissions = permissions.RemovePermission(Permission.Create);

// Get individual permissions
var individual = permissions.GetIndividualPermissions(); // [Read, Create]

// Readable string
var readable = permissions.ToReadableString(); // "Read, Create"
```

## 📋 Usage Examples

### Example 1: Checking User Roles

```csharp
public class ProductService
{
    private readonly IRoleService _roleService;
    private readonly ICurrentUser _currentUser;
    
    public async Task<bool> CanDeleteProduct(Guid productId)
    {
        var userId = Guid.Parse(_currentUser.UserId!);
        
        // Check if user is admin or manager
        return await _roleService.IsInAnyRoleAsync(
            userId, 
            RoleNames.ManagerAndAbove);
    }
}
```

### Example 2: Permission-Based Authorization

```csharp
public class ReportService
{
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUser _currentUser;
    
    public async Task<Report> ExportReport()
    {
        var userId = Guid.Parse(_currentUser.UserId!);
        
        // Check specific permission
        if (!await _permissionService.HasPermissionAsync(userId, Permission.ExportData))
        {
            throw new UnauthorizedAccessException("You don't have permission to export data.");
        }
        
        // Generate report
        return new Report();
    }
}
```

### Example 3: Resource Ownership Check

```csharp
public class DocumentService
{
    private readonly IDocumentRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly IRoleService _roleService;
    
    public async Task DeleteDocument(Guid documentId)
    {
        var document = await _repository.GetByIdAsync(documentId);
        var userId = Guid.Parse(_currentUser.UserId!);
        
        // Check ownership or admin role
        bool canDelete = document.IsOwnedBy(userId) ||
                        await _roleService.IsInAnyRoleAsync(userId, RoleNames.AdminRoles);
        
        if (!canDelete)
        {
            throw new UnauthorizedAccessException("You cannot delete this document.");
        }
        
        await _repository.DeleteAsync(document);
    }
}
```

### Example 4: Shared Access Control

```csharp
public class ProjectService
{
    public async Task ShareProject(Guid projectId, Guid targetUserId)
    {
        var project = await _repository.GetByIdAsync(projectId);
        var currentUserId = Guid.Parse(_currentUser.UserId!);
        
        // Only owner can share
        if (!project.IsOwnedBy(currentUserId))
        {
            throw new UnauthorizedAccessException("Only the owner can share this project.");
        }
        
        project.GrantAccessTo(targetUserId);
        await _repository.UpdateAsync(project);
    }
}
```

## 🏗️ Architecture Guidelines

### Domain Layer (Current)
✅ **What belongs here:**
- Enums (SystemRole, Permission)
- Interfaces (IRoleService, IPermissionService)
- Constants (RoleNames, RolePermissions)
- Extension methods (PermissionExtensions)
- Marker interfaces (IHasOwner, IHasSharedAccess)

❌ **What doesn't belong here:**
- Interface implementations (belongs in Application/Infrastructure)
- Database access logic
- HTTP/API concerns
- External service integrations

### Application Layer (Next)
- Implement IRoleService and IPermissionService
- Create authorization handlers
- Define authorization policies
- Create role management commands/queries

### Infrastructure Layer
- Implement repository patterns for roles
- Database configurations for identity entities
- Caching strategies for permissions
- Integration with external auth providers (future)

### API Layer
- Authorization attributes and policies
- Role-based endpoint protection
- Permission checks in controllers

## 🔄 Integration with Existing Code

### Update ICurrentUser Interface

The `ICurrentUser` interface in the Application layer should be enhanced:

```csharp
namespace Archu.Application.Abstractions;

public interface ICurrentUser
{
    string? UserId { get; }
    bool IsAuthenticated { get; }
    Task<bool> IsInRoleAsync(string role);
    Task<IEnumerable<string>> GetRolesAsync();
    Task<bool> HasPermissionAsync(Permission permission);
}
```

### Authorization in Controllers

```csharp
[Authorize(Roles = RoleNames.Administrator)]
public class AdminController : ControllerBase
{
    // Only administrators can access
}

[Authorize]
public class ProductsController : ControllerBase
{
    [HttpDelete("{id}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.Manager}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        // Only admins and managers can delete
    }
}
```

## 🧪 Testing Examples

### Unit Test for Permission Extensions

```csharp
[Fact]
public void HasPermission_WithValidPermission_ReturnsTrue()
{
    // Arrange
    var permissions = Permission.Read | Permission.Create;
    
    // Act
    var hasRead = permissions.HasPermission(Permission.Read);
    
    // Assert
    hasRead.Should().BeTrue();
}
```

### Integration Test for Role Service

```csharp
[Fact]
public async Task AssignRole_ValidUserAndRole_ReturnsTrue()
{
    // Arrange
    var userId = Guid.NewGuid();
    
    // Act
    var result = await _roleService.AssignRoleAsync(
        userId, 
        RoleNames.User, 
        "system");
    
    // Assert
    result.Should().BeTrue();
    var roles = await _roleService.GetUserRolesAsync(userId);
    roles.Should().Contain(RoleNames.User);
}
```

## 🚀 Next Steps

1. ✅ **Domain Layer** - Complete (current implementation)
2. ⏭️ **Application Layer** - Implement interfaces and services
3. ⏭️ **Infrastructure Layer** - Database configurations and repositories
4. ⏭️ **API Layer** - Authorization policies and middleware

## 📚 References

- [ASP.NET Core Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/introduction)
- [Role-Based Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles)
- [Claims-Based Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/claims)
- [Policy-Based Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies)

---

**Version**: 1.0  
**Last Updated**: 2025-01-22  
**Author**: Archu Development Team
