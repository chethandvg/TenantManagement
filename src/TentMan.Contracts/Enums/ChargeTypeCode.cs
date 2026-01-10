namespace TentMan.Contracts.Enums;

/// <summary>
/// Represents the type of charge for billing.
/// System-defined codes for common charges.
/// </summary>
public enum ChargeTypeCode
{
    /// <summary>
    /// Monthly rent charge
    /// </summary>
    RENT = 1,

    /// <summary>
    /// Maintenance charge
    /// </summary>
    MAINT = 2,

    /// <summary>
    /// Electricity utility charge
    /// </summary>
    ELEC = 3,

    /// <summary>
    /// Water utility charge
    /// </summary>
    WATER = 4,

    /// <summary>
    /// Gas utility charge
    /// </summary>
    GAS = 5,

    /// <summary>
    /// Late payment fee
    /// </summary>
    LATE_FEE = 6,

    /// <summary>
    /// Manual adjustment (positive or negative)
    /// </summary>
    ADJUSTMENT = 7
}
