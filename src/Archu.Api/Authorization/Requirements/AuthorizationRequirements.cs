using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Archu.Api.Authorization.Requirements;

/// <summary>
/// Requirement that checks if the user has a specific permission claim.
/// Permissions follow the pattern: {resource}:{action} (e.g., "products:create").
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }
}

/// <summary>
/// Handler for PermissionRequirement.
/// Checks if the user has the required permission claim.
/// </summary>
public class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ILogger<PermissionRequirementHandler> _logger;

    public PermissionRequirementHandler(ILogger<PermissionRequirementHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // ✅ FIX #6: Use case-sensitive comparison (Ordinal) for security
        // Permissions must match exactly to prevent authorization bypass
        var hasPermission = context.User.HasClaim(c =>
            c.Type == CustomClaimTypes.Permission &&
            c.Value.Equals(requirement.Permission, StringComparison.Ordinal));

        if (hasPermission)
        {
            _logger.LogDebug(
                "User {UserId} has permission {Permission}",
                userId,
                requirement.Permission);

            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "User {UserId} does not have permission {Permission}",
                userId,
                requirement.Permission);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Requirement that checks if the user's email is verified.
/// </summary>
public class EmailVerifiedRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Handler for EmailVerifiedRequirement.
/// Checks the email_verified claim.
/// </summary>
public class EmailVerifiedRequirementHandler : AuthorizationHandler<EmailVerifiedRequirement>
{
    private readonly ILogger<EmailVerifiedRequirementHandler> _logger;

    public EmailVerifiedRequirementHandler(ILogger<EmailVerifiedRequirementHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EmailVerifiedRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var emailVerifiedClaim = context.User.FindFirst(CustomClaimTypes.EmailVerified)?.Value;

        if (bool.TryParse(emailVerifiedClaim, out var isVerified) && isVerified)
        {
            _logger.LogDebug("User {UserId} has verified email", userId);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("User {UserId} email is not verified", userId);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Requirement that checks if the user has Two-Factor Authentication enabled.
/// </summary>
public class TwoFactorEnabledRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Handler for TwoFactorEnabledRequirement.
/// </summary>
public class TwoFactorEnabledRequirementHandler : AuthorizationHandler<TwoFactorEnabledRequirement>
{
    private readonly ILogger<TwoFactorEnabledRequirementHandler> _logger;

    public TwoFactorEnabledRequirementHandler(ILogger<TwoFactorEnabledRequirementHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TwoFactorEnabledRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var twoFactorClaim = context.User.FindFirst(CustomClaimTypes.TwoFactorEnabled)?.Value;

        if (bool.TryParse(twoFactorClaim, out var isEnabled) && isEnabled)
        {
            _logger.LogDebug("User {UserId} has 2FA enabled", userId);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("User {UserId} does not have 2FA enabled", userId);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Requirement that checks if the user has any of the specified roles.
/// </summary>
public class MinimumRoleRequirement : IAuthorizationRequirement
{
    public string[] Roles { get; }

    public MinimumRoleRequirement(params string[] roles)
    {
        Roles = roles ?? throw new ArgumentNullException(nameof(roles));
    }
}

/// <summary>
/// Handler for MinimumRoleRequirement.
/// </summary>
public class MinimumRoleRequirementHandler : AuthorizationHandler<MinimumRoleRequirement>
{
    private readonly ILogger<MinimumRoleRequirementHandler> _logger;

    public MinimumRoleRequirementHandler(ILogger<MinimumRoleRequirementHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumRoleRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var hasRole = requirement.Roles.Any(role =>
            context.User.IsInRole(role));

        if (hasRole)
        {
            var userRole = requirement.Roles.First(role => context.User.IsInRole(role));
            _logger.LogDebug("User {UserId} has required role {Role}", userId, userRole);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "User {UserId} does not have any of the required roles: {Roles}",
                userId,
                string.Join(", ", requirement.Roles));
        }

        return Task.CompletedTask;
    }
}
