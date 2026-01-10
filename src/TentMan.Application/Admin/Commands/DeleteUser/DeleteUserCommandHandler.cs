using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Shared.Constants.Authorization;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.Admin.Commands.DeleteUser;

/// <summary>
/// Handles the deletion (soft delete) of a user with security restrictions.
/// </summary>
/// <remarks>
/// **Security Rules:**
/// - Cannot delete the last SuperAdmin in the system
/// - Cannot delete yourself (self-deletion protection)
/// - Only SuperAdmin and Administrator can delete users
/// </remarks>
public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DeleteUserCommandHandler> _logger;

    public DeleteUserCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<DeleteUserCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var adminUserId = _currentUser.UserId;
        _logger.LogInformation(
            "Admin {AdminUserId} attempting to delete user {UserId}",
            adminUserId,
            request.UserId);

        // Verify user exists
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", request.UserId);
            return Result.Failure($"User with ID {request.UserId} not found");
        }

        // ✅ SECURITY: Prevent self-deletion
        if (adminUserId == request.UserId.ToString())
        {
            _logger.LogWarning(
                "Self-deletion denied: Admin {AdminUserId} attempted to delete their own account",
                adminUserId);
            return Result.Failure(
                "Security restriction: You cannot delete your own account. " +
                "This prevents accidental loss of system access. " +
                "Another administrator must delete your account.");
        }

        // ✅ SECURITY: Check if user is a SuperAdmin and if they're the last one
        var userRoles = await _unitOfWork.Roles.GetUserRolesAsync(request.UserId, cancellationToken);
        var isSuperAdmin = userRoles.Any(r =>
            r.Name.Equals(RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase));

        if (isSuperAdmin)
        {
            var lastSuperAdminValidation = await ValidateNotLastSuperAdminAsync(
                request.UserId,
                cancellationToken);

            if (!lastSuperAdminValidation.IsSuccess)
            {
                _logger.LogWarning(
                    "Cannot delete SuperAdmin user: {Reason}",
                    lastSuperAdminValidation.Error);
                return lastSuperAdminValidation;
            }
        }

        // Soft delete the user
        await _unitOfWork.Users.DeleteAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User '{UserName}' (ID: {UserId}) successfully deleted by admin {AdminUserId}",
            user.UserName,
            request.UserId,
            adminUserId);

        return Result.Success();
    }

    /// <summary>
    /// Validates that deleting this user won't leave the system without a SuperAdmin.
    /// </summary>
    private async Task<Result> ValidateNotLastSuperAdminAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking if user {UserId} is the last SuperAdmin", userId);

        // Get SuperAdmin role
        var superAdminRole = await _unitOfWork.Roles.GetByNameAsync(
            RoleNames.SuperAdmin,
            cancellationToken);

        if (superAdminRole == null)
        {
            // This should never happen, but if it does, allow deletion
            _logger.LogWarning("SuperAdmin role not found in database");
            return Result.Success();
        }

        // Count how many users have SuperAdmin role using a single database query
        var superAdminCount = await _unitOfWork.UserRoles.CountUsersWithRoleAsync(
            superAdminRole.Id,
            cancellationToken);

        _logger.LogDebug("Found {Count} SuperAdmin users in the system", superAdminCount);

        if (superAdminCount <= 1)
        {
            return Result.Failure(
                "Critical security restriction: Cannot delete the last SuperAdmin user from the system. " +
                "At least one SuperAdmin must exist to maintain system administration capabilities. " +
                "Please create or promote another SuperAdmin before deleting this user.");
        }

        return Result.Success();
    }
}
