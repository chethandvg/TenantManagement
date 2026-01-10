namespace TentMan.Contracts.Enums;

/// <summary>
/// Represents the reason for issuing a credit note.
/// </summary>
public enum CreditNoteReason
{
    /// <summary>
    /// Invoice error or overpayment
    /// </summary>
    InvoiceError = 1,

    /// <summary>
    /// Discount applied
    /// </summary>
    Discount = 2,

    /// <summary>
    /// Refund for returned goods/services
    /// </summary>
    Refund = 3,

    /// <summary>
    /// Goodwill gesture
    /// </summary>
    Goodwill = 4,

    /// <summary>
    /// Adjustment for errors
    /// </summary>
    Adjustment = 5,

    /// <summary>
    /// Other reason
    /// </summary>
    Other = 99
}
