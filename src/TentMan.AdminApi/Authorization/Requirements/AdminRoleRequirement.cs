using Microsoft.AspNetCore.Authorization;

namespace TentMan.AdminApi.Authorization.Requirements;

/// <summary>
/// Authorization requirement for admin role-based permissions.
/// Validates that the current user has one of the required roles.
/// </summary>
public class AdminRoleRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the required roles for authorization.
    /// User must have at least one of these roles.
    /// </summary>
    public string[] RequiredRoles { get; }

    /// <summary>
    /// Gets the operation being performed (for logging purposes).
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// Initializes a new instance of the AdminRoleRequirement class.
    /// </summary>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="requiredRoles">The roles required to perform the operation.</param>
    public AdminRoleRequirement(string operation, params string[] requiredRoles)
    {
        if (requiredRoles == null || requiredRoles.Length == 0)
        {
            throw new ArgumentException("At least one role must be specified", nameof(requiredRoles));
        }

        Operation = operation ?? throw new ArgumentNullException(nameof(operation));
        RequiredRoles = requiredRoles;
    }
}
