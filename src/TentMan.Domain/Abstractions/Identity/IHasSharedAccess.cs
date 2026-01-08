namespace TentMan.Domain.Abstractions.Identity;

/// <summary>
/// Marker interface for entities that support team-based or shared access.
/// Implement this interface on entities that can be accessed by multiple users or teams.
/// </summary>
public interface IHasSharedAccess
{
    /// <summary>
    /// Gets the collection of user IDs that have access to this entity.
    /// </summary>
    ICollection<Guid> SharedWithUserIds { get; }

    /// <summary>
    /// Checks if the specified user has access to this entity.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <returns>True if the user has access, otherwise false.</returns>
    bool HasAccess(Guid userId);

    /// <summary>
    /// Grants access to the specified user.
    /// </summary>
    /// <param name="userId">The user ID to grant access to.</param>
    void GrantAccessTo(Guid userId);

    /// <summary>
    /// Revokes access from the specified user.
    /// </summary>
    /// <param name="userId">The user ID to revoke access from.</param>
    void RevokeAccessFrom(Guid userId);
}
