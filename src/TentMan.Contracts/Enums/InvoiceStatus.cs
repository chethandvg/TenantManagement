namespace TentMan.Contracts.Enums;

/// <summary>
/// Represents the status of an invoice.
/// </summary>
public enum InvoiceStatus
{
    /// <summary>
    /// Invoice is being drafted
    /// </summary>
    Draft = 1,

    /// <summary>
    /// Invoice is issued and awaiting payment
    /// </summary>
    Issued = 2,

    /// <summary>
    /// Invoice is partially paid
    /// </summary>
    PartiallyPaid = 3,

    /// <summary>
    /// Invoice is fully paid
    /// </summary>
    Paid = 4,

    /// <summary>
    /// Invoice is overdue
    /// </summary>
    Overdue = 5,

    /// <summary>
    /// Invoice is cancelled
    /// </summary>
    Cancelled = 6,

    /// <summary>
    /// Invoice is written off
    /// </summary>
    WrittenOff = 7
}
