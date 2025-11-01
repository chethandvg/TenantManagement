namespace Archu.Domain.Constants;

/// <summary>
/// Defines the custom claim types issued by the Archu identity platform.
/// Keeping the constants in the domain layer ensures consistent usage
/// across API, infrastructure, and client projects without creating
/// circular dependencies.
/// </summary>
public static class CustomClaimTypes
{
    /// <summary>
    /// Claim that represents a granular permission such as "products:create".
    /// Multiple instances of this claim may be issued for a single identity.
    /// </summary>
    public const string Permission = "permission";

    /// <summary>
    /// Claim indicating whether the user has confirmed their email address.
    /// Stored as a boolean string ("True"/"False").
    /// </summary>
    public const string EmailVerified = "email_verified";

    /// <summary>
    /// Claim indicating whether the user has enabled multi-factor authentication.
    /// Stored as a boolean string ("True"/"False").
    /// </summary>
    public const string TwoFactorEnabled = "two_factor_enabled";

    /// <summary>
    /// Claim used for associating a user with a department when required.
    /// </summary>
    public const string Department = "department";

    /// <summary>
    /// Claim used for associating a user with an internal employee identifier.
    /// </summary>
    public const string EmployeeId = "employee_id";
}
