namespace TentMan.Contracts.Enums;

/// <summary>
/// Represents the status of an invoice run (batch billing process).
/// </summary>
public enum InvoiceRunStatus
{
    /// <summary>
    /// Invoice run is pending execution
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Invoice run is in progress
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Invoice run completed successfully
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Invoice run completed with errors
    /// </summary>
    CompletedWithErrors = 4,

    /// <summary>
    /// Invoice run failed
    /// </summary>
    Failed = 5,

    /// <summary>
    /// Invoice run was cancelled
    /// </summary>
    Cancelled = 6
}
