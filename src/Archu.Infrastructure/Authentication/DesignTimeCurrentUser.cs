using Archu.Application.Abstractions;

namespace Archu.Infrastructure.Authentication;

/// <summary>
/// Design-time implementation of ICurrentUser for EF Core tools and testing scenarios.
/// This implementation returns predefined values and is not intended for production use.
/// </summary>
public sealed class DesignTimeCurrentUser : ICurrentUser
{
    private readonly string _userId;
    private readonly bool _isAuthenticated;
    private readonly IEnumerable<string> _roles;

    /// <summary>
    /// Initializes a new instance of the <see cref="DesignTimeCurrentUser"/> class with default values.
    /// </summary>
    public DesignTimeCurrentUser()
        : this("design-time-user", isAuthenticated: true, roles: Array.Empty<string>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DesignTimeCurrentUser"/> class with custom values.
    /// </summary>
    /// <param name="userId">The user ID to return.</param>
    /// <param name="isAuthenticated">Whether the user is authenticated.</param>
    /// <param name="roles">The roles assigned to the user.</param>
    public DesignTimeCurrentUser(string userId, bool isAuthenticated = true, IEnumerable<string>? roles = null)
    {
        _userId = userId ?? throw new ArgumentNullException(nameof(userId));
        _isAuthenticated = isAuthenticated;
        _roles = roles ?? Array.Empty<string>();
    }

    /// <inheritdoc />
    public string? UserId => _isAuthenticated ? _userId : null;

    /// <inheritdoc />
    public bool IsAuthenticated => _isAuthenticated;

    /// <inheritdoc />
    public bool IsInRole(string role)
    {
        if (!_isAuthenticated || string.IsNullOrWhiteSpace(role))
            return false;

        return _roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public bool HasAnyRole(params string[] roles)
    {
        if (!_isAuthenticated || roles is null || roles.Length == 0)
            return false;

        return roles.Any(role => !string.IsNullOrWhiteSpace(role) && 
                                 _roles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public IEnumerable<string> GetRoles()
    {
        return _isAuthenticated ? _roles : Enumerable.Empty<string>();
    }
}
