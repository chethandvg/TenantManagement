using System.ComponentModel.DataAnnotations;
using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Admin;

/// <summary>
/// Request to initialize the system with a super admin user.
/// This should only be used once during initial system setup.
/// </summary>
public sealed class InitializeSystemRequest
{
    [Required]
    [MaxLength(256)]
    public string UserName { get; init; } = "superadmin";

    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Optional organization details. If provided, an organization will be created during initialization.
    /// </summary>
    public OrganizationInfo? Organization { get; init; }

    /// <summary>
    /// Optional owner details. If provided, an owner will be created and linked to the superadmin user.
    /// Requires Organization to also be provided.
    /// </summary>
    public OwnerInfo? Owner { get; init; }
}

/// <summary>
/// Organization information for initialization.
/// </summary>
public sealed class OrganizationInfo
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? TimeZone { get; init; }
}

/// <summary>
/// Owner information for initialization.
/// </summary>
public sealed class OwnerInfo
{
    public OwnerType OwnerType { get; init; } = OwnerType.Individual;

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; init; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Phone { get; init; } = string.Empty;

    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [MaxLength(10)]
    public string? Pan { get; init; }

    [MaxLength(15)]
    public string? Gstin { get; init; }
}
