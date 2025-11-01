namespace Archu.Domain.Abstractions.Identity;

/// <summary>
/// Marker interface for entities that support role-based access control.
/// Implement this interface on entities that need to track ownership or access control.
/// </summary>
public interface IHasOwner
{
    /// <summary>
    /// Gets or sets the user ID of the entity owner.
    /// </summary>
    Guid OwnerId { get; set; }

    /// <summary>
    /// Checks if the specified user is the owner of this entity.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <returns>True if the user is the owner, otherwise false.</returns>
    bool IsOwnedBy(Guid userId);
}
