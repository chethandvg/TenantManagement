using SharedAuthConstants = TentMan.Shared.Constants.Authorization;
using TentMan.Api.Authorization.Requirements;
using TentMan.Application.Abstractions;
using TentMan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace TentMan.Api.Authorization.Handlers;

/// <summary>
/// Handler for LeaseAccessRequirement.
/// Checks if the user has permission to access a lease.
/// - Admins/SuperAdmins/Managers: Full access to all leases
/// - Tenants: Access only to leases where they are a party
/// </summary>
public class LeaseAccessRequirementHandler : AuthorizationHandler<LeaseAccessRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LeaseAccessRequirementHandler> _logger;

    public LeaseAccessRequirementHandler(
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider,
        ILogger<LeaseAccessRequirementHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        LeaseAccessRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid");
            return;
        }

        // Admins, SuperAdmins, and Managers have full access
        if (context.User.IsInRole(SharedAuthConstants.RoleNames.Administrator) || 
            context.User.IsInRole(SharedAuthConstants.RoleNames.SuperAdmin) ||
            context.User.IsInRole(SharedAuthConstants.RoleNames.Manager))
        {
            _logger.LogDebug("User {UserId} has admin/manager role, granting lease access", userId);
            context.Succeed(requirement);
            return;
        }

        // For Tenant role, check if they are a party to the lease
        if (context.User.IsInRole(SharedAuthConstants.RoleNames.Tenant) && requirement.LeaseId.HasValue)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Check if tenant has a linked tenant record and is a party to this lease
            var hasAccess = await dbContext.Tenants
                .Where(t => t.LinkedUserId == userId)
                .SelectMany(t => t.LeaseParties)
                .AnyAsync(lp => lp.LeaseId == requirement.LeaseId.Value);

            if (hasAccess)
            {
                _logger.LogDebug("User {UserId} is a party to lease {LeaseId}, granting access", userId, requirement.LeaseId);
                context.Succeed(requirement);
                return;
            }

            _logger.LogWarning("User {UserId} is not authorized to access lease {LeaseId}", userId, requirement.LeaseId);
        }
        else if (!requirement.LeaseId.HasValue)
        {
            // If no specific lease ID is provided, let the query layer handle tenant filtering
            _logger.LogDebug("No lease ID specified, deferring access check to query layer");
            context.Succeed(requirement);
        }
    }
}
