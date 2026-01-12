namespace TentMan.Contracts.Enums;

/// <summary>
/// Represents the status of a payment confirmation request.
/// </summary>
public enum PaymentConfirmationStatus
{
    /// <summary>
    /// Request is pending owner review
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Request has been confirmed by owner and payment recorded
    /// </summary>
    Confirmed = 2,

    /// <summary>
    /// Request was rejected by owner
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Request was cancelled by tenant
    /// </summary>
    Cancelled = 4
}
