using Microsoft.AspNetCore.Authorization;

namespace Archu.Api.Authorization.Requirements;

/// <summary>
/// Requirement that checks if the user is the owner of a resource.
/// Used for protecting user-specific data.
/// </summary>
public sealed class ResourceOwnerRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the resource ID that needs ownership verification.
    /// </summary>
    public Guid? ResourceId { get; }

    public ResourceOwnerRequirement(Guid? resourceId = null)
    {
        ResourceId = resourceId;
    }
}
