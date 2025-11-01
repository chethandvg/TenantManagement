using Archu.Application.Abstractions;
using Archu.Application.Common;
using Archu.Domain.Constants;
using Archu.Domain.Entities.Identity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Admin.Commands.AssignRole;

/// <summary>
/// Handles the assignment of a role to a user with security restrictions.
/// </summary>
/// <remarks>
/// **Role Assignment Rules:**
/// - SuperAdmin: Can assign any role (including SuperAdmin and Administrator)
/// - Administrator: Can assign User, Manager, and Guest roles ONLY
/// - Manager: Cannot assign roles (blocked by authorization policy)
/// </remarks>
public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<AssignRoleCommandHandler> _logger;

    public AssignRoleCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ITimeProvider timeProvider,
        ILogger<AssignRoleCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var adminUserId = _currentUser.UserId;
        _logger.LogInformation(
            "Admin {AdminUserId} attempting to assign role {RoleId} to user {UserId}",
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

        // ✅ SECURITY: Validate role assignment permissions
        var validationResult = ValidateRoleAssignmentPermissions(role.Name);
        if (!validationResult.IsSuccess)
        {
            _logger.LogWarning(
                "Role assignment denied: Admin {AdminUserId} ({AdminRoles}) cannot assign role {RoleName}. Reason: {Reason}",
                adminUserId,
                string.Join(", ", _currentUser.GetRoles()),
                role.Name,
                validationResult.Error);
            return validationResult;
        }

        // Check if user already has this role
        if (await _unitOfWork.UserRoles.UserHasRoleAsync(request.UserId, request.RoleId, cancellationToken))
        {
            _logger.LogWarning("User {UserId} already has role {RoleId}", request.UserId, request.RoleId);
            return Result.Failure($"User '{user.UserName}' already has the role '{role.Name}'");
        }

        var userRole = new UserRole
        {
            UserId = request.UserId,
            RoleId = request.RoleId,
            AssignedAtUtc = _timeProvider.UtcNow,
            AssignedBy = adminUserId
        };

        await _unitOfWork.UserRoles.AddAsync(userRole, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Role '{RoleName}' successfully assigned to user '{UserName}' by admin {AdminUserId}",
            role.Name,
            user.UserName,
            adminUserId);

        return Result.Success();
    }

    /// <summary>
    /// Validates that the current admin user has permission to assign the specified role.
    /// </summary>
    /// <remarks>
    /// **Security Rules:**
    /// 1. Only SuperAdmin can assign SuperAdmin role
    /// 2. Only SuperAdmin can assign Administrator role
    /// 3. Administrator can assign User, Manager, and Guest roles only
    /// </remarks>
    private Result ValidateRoleAssignmentPermissions(string roleName)
    {
        var normalizedRoleName = roleName.ToUpperInvariant();

        // SuperAdmin can assign any role
        if (_currentUser.IsInRole(RoleNames.SuperAdmin))
        {
            _logger.LogDebug("SuperAdmin has permission to assign any role including '{RoleName}'", roleName);
            return Result.Success();
        }

        // Administrator restrictions
        if (_currentUser.IsInRole(RoleNames.Administrator))
        {
            // Administrator CANNOT assign SuperAdmin
            if (normalizedRoleName == RoleNames.SuperAdmin.ToUpperInvariant())
            {
                return Result.Failure(
                    $"Permission denied: Only SuperAdmin can assign the '{RoleNames.SuperAdmin}' role. " +
                    "Administrators cannot elevate users to SuperAdmin status.");
            }

            // Administrator CANNOT assign Administrator
            if (normalizedRoleName == RoleNames.Administrator.ToUpperInvariant())
            {
                return Result.Failure(
                    $"Permission denied: Only SuperAdmin can assign the '{RoleNames.Administrator}' role. " +
                    "Administrators cannot create other administrators.");
            }

            // Administrator CAN assign other roles (User, Manager, Guest)
            _logger.LogDebug(
                "Administrator has permission to assign role '{RoleName}'",
                roleName);
            return Result.Success();
        }

        // If not SuperAdmin or Administrator (should not reach here due to authorization policy)
        _logger.LogError(
            "Unauthorized role assignment attempt by user {UserId} with roles: {Roles}",
            _currentUser.UserId,
            string.Join(", ", _currentUser.GetRoles()));
        
        return Result.Failure(
            "Permission denied: You do not have sufficient privileges to assign roles. " +
            "Only SuperAdmin and Administrator can assign roles.");
    }
}
