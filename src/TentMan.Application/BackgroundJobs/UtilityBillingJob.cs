using Microsoft.Extensions.Logging;
using TentMan.Application.Abstractions.Billing;

namespace TentMan.Application.BackgroundJobs;

/// <summary>
/// Background job for automated utility billing generation.
/// Runs on a recurring schedule to generate invoices for leases with pending utility statements.
/// </summary>
public class UtilityBillingJob
{
    private readonly IInvoiceRunService _invoiceRunService;
    private readonly ILogger<UtilityBillingJob> _logger;

    public UtilityBillingJob(
        IInvoiceRunService invoiceRunService,
        ILogger<UtilityBillingJob> logger)
    {
        _invoiceRunService = invoiceRunService;
        _logger = logger;
    }

    /// <summary>
    /// Executes the utility billing job for a specific organization.
    /// </summary>
    /// <param name="orgId">Organization ID</param>
    /// <param name="billingPeriodStart">Start date of the billing period</param>
    /// <param name="billingPeriodEnd">End date of the billing period</param>
    public async Task ExecuteAsync(
        Guid orgId,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd)
    {
        _logger.LogInformation(
            "Starting utility billing job for organization {OrgId}, period {PeriodStart} to {PeriodEnd}",
            orgId, billingPeriodStart, billingPeriodEnd);

        try
        {
            var result = await _invoiceRunService.ExecuteUtilityRunAsync(
                orgId,
                billingPeriodStart,
                billingPeriodEnd,
                CancellationToken.None);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Utility billing completed successfully. Total: {TotalLeases}, Success: {SuccessCount}, Failures: {FailureCount}",
                    result.TotalLeases, result.SuccessCount, result.FailureCount);

                if (result.FailureCount > 0)
                {
                    _logger.LogWarning(
                        "Some utility bills failed to generate. Errors: {Errors}",
                        string.Join("; ", result.ErrorMessages));
                }
            }
            else
            {
                _logger.LogError(
                    "Utility billing failed. Errors: {Errors}",
                    string.Join("; ", result.ErrorMessages));
                
                // Note: Not throwing here since utility billing is not yet fully implemented
                // This prevents job from failing repeatedly
                _logger.LogWarning("Utility billing job completed with errors (feature not fully implemented)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception occurred during utility billing for organization {OrgId}",
                orgId);
            throw;
        }
    }

    /// <summary>
    /// Executes the utility billing job for the current billing period.
    /// </summary>
    /// <param name="orgId">Organization ID</param>
    public async Task ExecuteForCurrentPeriodAsync(Guid orgId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var billingPeriodStart = new DateOnly(today.Year, today.Month, 1);
        var billingPeriodEnd = billingPeriodStart.AddMonths(1).AddDays(-1);

        _logger.LogInformation(
            "Executing utility billing for current period. Period: {PeriodStart} to {PeriodEnd}",
            billingPeriodStart, billingPeriodEnd);

        await ExecuteAsync(orgId, billingPeriodStart, billingPeriodEnd);
    }
}
