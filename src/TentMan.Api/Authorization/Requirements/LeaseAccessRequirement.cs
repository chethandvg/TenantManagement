using Microsoft.AspNetCore.Authorization;

namespace TentMan.Api.Authorization.Requirements;

/// <summary>
/// Requirement for accessing lease-related resources.
/// Admins/Managers have full access, Tenants can only access their own leases.
/// </summary>
public class LeaseAccessRequirement : IAuthorizationRequirement
{
    public Guid? LeaseId { get; }

    public LeaseAccessRequirement(Guid? leaseId = null)
    {
        LeaseId = leaseId;
    }
}
