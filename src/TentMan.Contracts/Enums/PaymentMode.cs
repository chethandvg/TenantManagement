namespace TentMan.Contracts.Enums;

/// <summary>
/// Represents the mode/method of payment.
/// </summary>
public enum PaymentMode
{
    /// <summary>
    /// Payment made in cash - recorded directly by owner
    /// </summary>
    Cash = 1,

    /// <summary>
    /// Payment made via online payment gateway
    /// </summary>
    Online = 2,

    /// <summary>
    /// Payment made via bank transfer/NEFT/RTGS
    /// </summary>
    BankTransfer = 3,

    /// <summary>
    /// Payment made via cheque
    /// </summary>
    Cheque = 4,

    /// <summary>
    /// Payment made via UPI (Unified Payments Interface)
    /// </summary>
    UPI = 5,

    /// <summary>
    /// Other payment methods
    /// </summary>
    Other = 99
}
