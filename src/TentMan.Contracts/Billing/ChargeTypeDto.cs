using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Billing;

/// <summary>
/// DTO for charge types that can be applied to invoices (RENT, MAINT, ELEC, etc.).
/// Supports both system-defined types and organization-specific custom types.
/// </summary>
public sealed class ChargeTypeDto
{
    /// <summary>
    /// Gets the unique identifier for the charge type.
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Gets the organization ID for custom charge types.
    /// Null for system-defined charge types that are available to all organizations.
    /// </summary>
    public Guid? OrgId { get; init; }
    
    /// <summary>
    /// Gets the charge type code enum value (RENT, MAINT, ELEC, WATER, GAS, LATE_FEE, ADJUSTMENT).
    /// </summary>
    public ChargeTypeCode Code { get; init; }
    
    /// <summary>
    /// Gets the display name of the charge type (e.g., "Monthly Rent", "Electricity").
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets the description explaining what this charge type is used for.
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether this charge type is currently active and can be used.
    /// Inactive charge types cannot be added to new recurring charges.
    /// </summary>
    public bool IsActive { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether this is a system-defined charge type.
    /// System-defined types (RENT, MAINT, etc.) cannot be modified or deleted.
    /// </summary>
    public bool IsSystemDefined { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether this charge type is subject to tax.
    /// When true, tax will be calculated on invoice lines using this charge type.
    /// </summary>
    public bool IsTaxable { get; init; }
    
    /// <summary>
    /// Gets the default amount for this charge type.
    /// Can be overridden when creating recurring charges or invoice lines.
    /// </summary>
    public decimal? DefaultAmount { get; init; }
}
