using Archu.AdminApi.Authorization.Requirements;
using Archu.Application.Abstractions;
using Archu.SharedKernel.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Archu.AdminApi.Authorization.Handlers;

/// <summary>
/// Handles authorization for admin role-based permissions.
/// Validates that the current user has at least one of the required roles.
/// </summary>
public class AdminRoleRequirementHandler : AuthorizationHandler<AdminRoleRequirement>
{
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AdminRoleRequirementHandler> _logger;

    public AdminRoleRequirementHandler(
        ICurrentUser currentUser,
        ILogger<AdminRoleRequirementHandler> logger)
    {
        _currentUser = currentUser;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminRoleRequirement requirement)
    {
        if (!_currentUser.IsAuthenticated)
        {
            _logger.LogWarning(
                "User is not authenticated for operation: {Operation}",
                requirement.Operation);
            return Task.CompletedTask;
        }

        var userId = _currentUser.UserId;
        var userRoles = _currentUser.GetRoles().ToList();

        _logger.LogDebug(
            "Checking admin permission for operation '{Operation}' - User: {UserId}, Roles: {Roles}",
            requirement.Operation,
            userId,
            string.Join(", ", userRoles));

        // SuperAdmin has access to everything
        if (_currentUser.IsInRole(RoleNames.SuperAdmin))
        {
            _logger.LogDebug(
                "User {UserId} is SuperAdmin - granting access to operation: {Operation}",
                userId,
                requirement.Operation);
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check if user has any of the required roles
        var hasRequiredRole = requirement.RequiredRoles.Any(role => _currentUser.IsInRole(role));

        if (hasRequiredRole)
        {
            _logger.LogDebug(
                "User {UserId} has required role for operation: {Operation}",
                userId,
                requirement.Operation);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "User {UserId} denied access to operation: {Operation}. User roles: {UserRoles}, Required roles: {RequiredRoles}",
                userId,
                requirement.Operation,
                string.Join(", ", userRoles),
                string.Join(", ", requirement.RequiredRoles));
        }

        return Task.CompletedTask;
    }
}
