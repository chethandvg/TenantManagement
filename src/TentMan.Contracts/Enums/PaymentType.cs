namespace TentMan.Contracts.Enums;

/// <summary>
/// Represents the type/category of payment.
/// Distinguishes between different payment purposes.
/// </summary>
public enum PaymentType
{
    /// <summary>
    /// Payment for rent (lease agreement)
    /// </summary>
    Rent = 1,

    /// <summary>
    /// Payment for utility bills (electricity, water, gas, internet, etc.)
    /// Supports BBPS (Bharat Bill Payment System) in India and similar systems internationally
    /// </summary>
    Utility = 2,

    /// <summary>
    /// Payment for general invoices (maintenance, repairs, services, etc.)
    /// </summary>
    Invoice = 3,

    /// <summary>
    /// Security deposit payment or refund
    /// </summary>
    Deposit = 4,

    /// <summary>
    /// Maintenance or service charges
    /// </summary>
    Maintenance = 5,

    /// <summary>
    /// Late fees or penalty charges
    /// </summary>
    LateFee = 6,

    /// <summary>
    /// Other payment types not covered above
    /// </summary>
    Other = 99
}
