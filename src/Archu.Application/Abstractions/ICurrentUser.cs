namespace Archu.Application.Abstractions;

/// <summary>
/// Represents the currently authenticated user in the application context.
/// Provides access to user identity, authentication status, and role information.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Gets the unique identifier of the current user.
    /// Returns null if no user is authenticated.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets a value indicating whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Determines whether the current user is in the specified role.
    /// </summary>
    /// <param name="role">The role name to check.</param>
    /// <returns>True if the user is in the specified role; otherwise, false.</returns>
    bool IsInRole(string role);

    /// <summary>
    /// Determines whether the current user is in any of the specified roles.
    /// </summary>
    /// <param name="roles">The role names to check.</param>
    /// <returns>True if the user is in at least one of the specified roles; otherwise, false.</returns>
    bool HasAnyRole(params string[] roles);

    /// <summary>
    /// Gets all roles assigned to the current user.
    /// Returns an empty collection if the user is not authenticated or has no roles.
    /// </summary>
    /// <returns>A collection of role names.</returns>
    IEnumerable<string> GetRoles();
}
