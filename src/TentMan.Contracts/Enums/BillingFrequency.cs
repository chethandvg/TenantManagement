namespace TentMan.Contracts.Enums;

/// <summary>
/// Represents how frequently a charge is billed.
/// </summary>
public enum BillingFrequency
{
    /// <summary>
    /// One-time charge
    /// </summary>
    OneTime = 1,

    /// <summary>
    /// Monthly recurring charge
    /// </summary>
    Monthly = 2,

    /// <summary>
    /// Quarterly recurring charge
    /// </summary>
    Quarterly = 3,

    /// <summary>
    /// Yearly recurring charge
    /// </summary>
    Yearly = 4
}
