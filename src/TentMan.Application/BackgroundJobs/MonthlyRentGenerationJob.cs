using Microsoft.Extensions.Logging;
using TentMan.Application.Abstractions.Billing;
using TentMan.Contracts.Enums;

namespace TentMan.Application.BackgroundJobs;

/// <summary>
/// Background job for automated monthly rent generation.
/// Runs on a recurring schedule to generate invoices for all active leases.
/// </summary>
public class MonthlyRentGenerationJob
{
    private readonly IInvoiceRunService _invoiceRunService;
    private readonly ILogger<MonthlyRentGenerationJob> _logger;

    public MonthlyRentGenerationJob(
        IInvoiceRunService invoiceRunService,
        ILogger<MonthlyRentGenerationJob> logger)
    {
        _invoiceRunService = invoiceRunService;
        _logger = logger;
    }

    /// <summary>
    /// Executes the monthly rent generation job for a specific organization.
    /// </summary>
    /// <param name="orgId">Organization ID</param>
    /// <param name="billingPeriodStart">Start date of the billing period</param>
    /// <param name="billingPeriodEnd">End date of the billing period</param>
    /// <param name="prorationMethod">Proration method to use</param>
    public async Task ExecuteAsync(
        Guid orgId,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        ProrationMethod prorationMethod = ProrationMethod.ActualDaysInMonth)
    {
        _logger.LogInformation(
            "Starting monthly rent generation job for organization {OrgId}, period {PeriodStart} to {PeriodEnd}",
            orgId, billingPeriodStart, billingPeriodEnd);

        try
        {
            var result = await _invoiceRunService.ExecuteMonthlyRentRunAsync(
                orgId,
                billingPeriodStart,
                billingPeriodEnd,
                prorationMethod,
                CancellationToken.None);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Monthly rent generation completed successfully. Total: {TotalLeases}, Success: {SuccessCount}, Failures: {FailureCount}",
                    result.TotalLeases, result.SuccessCount, result.FailureCount);

                if (result.FailureCount > 0)
                {
                    _logger.LogWarning(
                        "Some invoices failed to generate. Errors: {Errors}",
                        string.Join("; ", result.ErrorMessages));
                }
            }
            else
            {
                _logger.LogError(
                    "Monthly rent generation failed. Errors: {Errors}",
                    string.Join("; ", result.ErrorMessages));
                
                throw new Exception($"Monthly rent generation failed: {string.Join("; ", result.ErrorMessages)}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception occurred during monthly rent generation for organization {OrgId}",
                orgId);
            throw;
        }
    }

    /// <summary>
    /// Executes the monthly rent generation job for the upcoming billing period.
    /// Calculates the next month's billing period automatically.
    /// </summary>
    /// <param name="orgId">Organization ID</param>
    /// <param name="daysBeforePeriodStart">Number of days before period start to generate invoices (default: 5)</param>
    public async Task ExecuteForNextMonthAsync(Guid orgId, int daysBeforePeriodStart = 5)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nextMonth = today.AddMonths(1);
        var billingPeriodStart = new DateOnly(nextMonth.Year, nextMonth.Month, 1);
        var billingPeriodEnd = billingPeriodStart.AddMonths(1).AddDays(-1);

        _logger.LogInformation(
            "Executing monthly rent generation for next month. Period: {PeriodStart} to {PeriodEnd}, Days before start: {DaysBeforePeriodStart}",
            billingPeriodStart, billingPeriodEnd, daysBeforePeriodStart);

        await ExecuteAsync(orgId, billingPeriodStart, billingPeriodEnd);
    }
}
