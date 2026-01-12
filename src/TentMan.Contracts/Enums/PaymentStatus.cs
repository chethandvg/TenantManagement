namespace TentMan.Contracts.Enums;

/// <summary>
/// Represents the status of a payment.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment is pending processing
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Payment has been completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Payment processing failed
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Payment was cancelled
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Payment is being processed
    /// </summary>
    Processing = 5,

    /// <summary>
    /// Payment is refunded
    /// </summary>
    Refunded = 6,

    /// <summary>
    /// Cash payment is awaiting owner confirmation
    /// </summary>
    PendingConfirmation = 7,

    /// <summary>
    /// Cash payment confirmation was rejected by owner
    /// </summary>
    Rejected = 8
}
