using Archu.Application.Abstractions;
using Archu.Application.Common;
using Archu.Domain.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Admin.Commands.RemoveRole;

/// <summary>
/// Handles the removal of a role from a user with security restrictions.
/// </summary>
/// <remarks>
/// **Role Removal Rules:**
/// - SuperAdmin: Can remove any role from any user
/// - Administrator: Can remove User, Manager, and Guest roles ONLY
/// - Manager: Cannot remove roles (blocked by authorization policy)
/// - Users cannot remove their own privileged roles (SuperAdmin/Administrator)
/// - Cannot remove SuperAdmin role if user is the last SuperAdmin in the system
/// </remarks>
public class RemoveRoleCommandHandler : IRequestHandler<RemoveRoleCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RemoveRoleCommandHandler> _logger;

    public RemoveRoleCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<RemoveRoleCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result> Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
    {
        var adminUserId = _currentUser.UserId;
        _logger.LogInformation(
            "Admin {AdminUserId} attempting to remove role {RoleId} from user {UserId}",
            adminUserId,
            request.RoleId,
            request.UserId);

        // Verify user exists
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", request.UserId);
            return Result.Failure($"User with ID {request.UserId} not found");
        }

        // Verify role exists
        var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId, cancellationToken);
        if (role == null)
        {
            _logger.LogWarning("Role {RoleId} not found", request.RoleId);
            return Result.Failure($"Role with ID {request.RoleId} not found");
        }

        // Check if user has the role
        if (!await _unitOfWork.UserRoles.UserHasRoleAsync(request.UserId, request.RoleId, cancellationToken))
        {
            _logger.LogWarning(
                "User {UserId} does not have role {RoleId}",
                request.UserId,
                request.RoleId);
            return Result.Failure($"User '{user.UserName}' does not have the role '{role.Name}'");
        }

        // ✅ SECURITY: Prevent self-removal of privileged roles
        var selfRemovalValidation = ValidateSelfRemovalRestriction(role.Name, request.UserId);
        if (!selfRemovalValidation.IsSuccess)
        {
            _logger.LogWarning(
                "Self-removal denied: Admin {AdminUserId} cannot remove their own '{RoleName}' role. Reason: {Reason}",
                adminUserId,
                role.Name,
                selfRemovalValidation.Error);
            return selfRemovalValidation;
        }

        // ✅ SECURITY: Validate role removal permissions
        var permissionValidation = ValidateRoleRemovalPermissions(role.Name);
        if (!permissionValidation.IsSuccess)
        {
            _logger.LogWarning(
                "Role removal denied: Admin {AdminUserId} ({AdminRoles}) cannot remove role {RoleName}. Reason: {Reason}",
                adminUserId,
                string.Join(", ", _currentUser.GetRoles()),
                role.Name,
                permissionValidation.Error);
            return permissionValidation;
        }

        // ✅ SECURITY: Prevent removal of last SuperAdmin
        if (role.Name.Equals(RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase))
        {
            var lastSuperAdminValidation = await ValidateNotLastSuperAdminAsync(
                request.UserId,
                role.Id,
                cancellationToken);

            if (!lastSuperAdminValidation.IsSuccess)
            {
                _logger.LogWarning(
                    "Cannot remove SuperAdmin role: {Reason}",
                    lastSuperAdminValidation.Error);
                return lastSuperAdminValidation;
            }
        }

        // Remove the role
        await _unitOfWork.UserRoles.RemoveAsync(request.UserId, request.RoleId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Role '{RoleName}' successfully removed from user '{UserName}' by admin {AdminUserId}",
            role.Name,
            user.UserName,
            adminUserId);

        return Result.Success();
    }

    /// <summary>
    /// Validates that users cannot remove their own privileged roles.
    /// </summary>
    private Result ValidateSelfRemovalRestriction(string roleName, Guid targetUserId)
    {
        var currentUserId = _currentUser.UserId;

        // Check if admin is removing their own role
        if (currentUserId == targetUserId.ToString())
        {
            var normalizedRoleName = roleName.ToUpperInvariant();

            // Prevent self-removal of SuperAdmin
            if (normalizedRoleName == RoleNames.SuperAdmin.ToUpperInvariant())
            {
                return Result.Failure(
                    "Security restriction: You cannot remove your own SuperAdmin role. " +
                    "This prevents accidental loss of system administration privileges. " +
                    "Another SuperAdmin must remove this role.");
            }

            // Prevent self-removal of Administrator
            if (normalizedRoleName == RoleNames.Administrator.ToUpperInvariant())
            {
                return Result.Failure(
                    "Security restriction: You cannot remove your own Administrator role. " +
                    "This prevents accidental loss of administrative privileges. " +
                    "Another Administrator or SuperAdmin must remove this role.");
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Validates that the current admin user has permission to remove the specified role.
    /// </summary>
    private Result ValidateRoleRemovalPermissions(string roleName)
    {
        var normalizedRoleName = roleName.ToUpperInvariant();

        // SuperAdmin can remove any role
        if (_currentUser.IsInRole(RoleNames.SuperAdmin))
        {
            _logger.LogDebug("SuperAdmin has permission to remove any role including '{RoleName}'", roleName);
            return Result.Success();
        }

        // Administrator restrictions
        if (_currentUser.IsInRole(RoleNames.Administrator))
        {
            // Administrator CANNOT remove SuperAdmin
            if (normalizedRoleName == RoleNames.SuperAdmin.ToUpperInvariant())
            {
                return Result.Failure(
                    $"Permission denied: Only SuperAdmin can remove the '{RoleNames.SuperAdmin}' role. " +
                    "Administrators cannot demote SuperAdmin users.");
            }

            // Administrator CANNOT remove Administrator
            if (normalizedRoleName == RoleNames.Administrator.ToUpperInvariant())
            {
                return Result.Failure(
                    $"Permission denied: Only SuperAdmin can remove the '{RoleNames.Administrator}' role. " +
                    "Administrators cannot demote other administrators.");
            }

            // Administrator CAN remove other roles (User, Manager, Guest)
            _logger.LogDebug(
                "Administrator has permission to remove role '{RoleName}'",
                roleName);
            return Result.Success();
        }

        // If not SuperAdmin or Administrator
        _logger.LogError(
            "Unauthorized role removal attempt by user {UserId} with roles: {Roles}",
            _currentUser.UserId,
            string.Join(", ", _currentUser.GetRoles()));

        return Result.Failure(
            "Permission denied: You do not have sufficient privileges to remove roles. " +
            "Only SuperAdmin and Administrator can remove roles.");
    }

    /// <summary>
    /// Validates that removing this role won't leave the system without a SuperAdmin.
    /// </summary>
    private async Task<Result> ValidateNotLastSuperAdminAsync(
        Guid userId,
        Guid superAdminRoleId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking if user {UserId} is the last SuperAdmin", userId);

        // Get all users who have the SuperAdmin role
        var superAdminUsers = await _unitOfWork.UserRoles.GetUserRolesAsync(userId, cancellationToken);

        // Count how many users have SuperAdmin role
        var superAdminCount = 0;
        var allUsers = await _unitOfWork.Users.GetAllAsync(1, int.MaxValue, cancellationToken);

        foreach (var user in allUsers)
        {
            if (await _unitOfWork.UserRoles.UserHasRoleAsync(user.Id, superAdminRoleId, cancellationToken))
            {
                superAdminCount++;
            }
        }

        _logger.LogDebug("Found {Count} SuperAdmin users in the system", superAdminCount);

        if (superAdminCount <= 1)
        {
            return Result.Failure(
                "Critical security restriction: Cannot remove the last SuperAdmin role from the system. " +
                "At least one SuperAdmin must exist to maintain system administration capabilities. " +
                "Please assign SuperAdmin role to another user before removing it from this user.");
        }

        return Result.Success();
    }
}
