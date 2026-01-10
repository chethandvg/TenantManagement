using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;

namespace TentMan.Application.Abstractions.Billing;

/// <summary>
/// Service for orchestrating batch invoice generation runs.
/// Handles processing multiple leases with partial failure support.
/// </summary>
public interface IInvoiceRunService
{
    /// <summary>
    /// Executes a batch invoice run for monthly rent across all active leases.
    /// </summary>
    /// <param name="orgId">The organization ID</param>
    /// <param name="billingPeriodStart">Start of the billing period</param>
    /// <param name="billingPeriodEnd">End of the billing period (inclusive)</param>
    /// <param name="prorationMethod">Method to use for proration calculations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Invoice run result with summary</returns>
    Task<InvoiceRunResult> ExecuteMonthlyRentRunAsync(
        Guid orgId,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        ProrationMethod prorationMethod,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a utility billing run for leases with pending utility statements.
    /// </summary>
    /// <param name="orgId">The organization ID</param>
    /// <param name="billingPeriodStart">Start of the billing period</param>
    /// <param name="billingPeriodEnd">End of the billing period (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Invoice run result with summary</returns>
    Task<InvoiceRunResult> ExecuteUtilityRunAsync(
        Guid orgId,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an invoice run execution.
/// </summary>
public class InvoiceRunResult
{
    public bool IsSuccess { get; init; }
    public InvoiceRun? InvoiceRun { get; init; }
    public int TotalLeases { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public List<string> ErrorMessages { get; init; } = new();
}
