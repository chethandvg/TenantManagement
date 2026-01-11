using Microsoft.Extensions.Logging;
using TentMan.Application.Abstractions.Billing;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;

namespace TentMan.Application.BackgroundJobs;

/// <summary>
/// Background job for automated monthly rent generation.
/// Runs on a recurring schedule to generate invoices for all active leases across all organizations.
/// </summary>
public class MonthlyRentGenerationJob
{
    private readonly IInvoiceRunService _invoiceRunService;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ILogger<MonthlyRentGenerationJob> _logger;

    public MonthlyRentGenerationJob(
        IInvoiceRunService invoiceRunService,
        IOrganizationRepository organizationRepository,
        ILogger<MonthlyRentGenerationJob> logger)
    {
        _invoiceRunService = invoiceRunService;
        _organizationRepository = organizationRepository;
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
    /// Processes all organizations and generates invoices for the next month.
    /// </summary>
    /// <param name="daysBeforePeriodStart">Number of days before period start to generate invoices (for validation)</param>
    public async Task ExecuteForNextMonthAsync(int daysBeforePeriodStart = 5)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nextMonth = today.AddMonths(1);
        var billingPeriodStart = new DateOnly(nextMonth.Year, nextMonth.Month, 1);
        var billingPeriodEnd = billingPeriodStart.AddMonths(1).AddDays(-1);

        // Validate that we're running at the appropriate time
        var daysUntilPeriodStart = billingPeriodStart.DayNumber - today.DayNumber;
        if (daysUntilPeriodStart > daysBeforePeriodStart + 1 || daysUntilPeriodStart < daysBeforePeriodStart - 1)
        {
            _logger.LogWarning(
                "Job is running outside the expected window. Days until period start: {DaysUntil}, Expected: {Expected}",
                daysUntilPeriodStart, daysBeforePeriodStart);
        }

        _logger.LogInformation(
            "Starting monthly rent generation for all organizations. Period: {PeriodStart} to {PeriodEnd}, Days until start: {DaysUntil}",
            billingPeriodStart, billingPeriodEnd, daysUntilPeriodStart);

        try
        {
            var organizations = await _organizationRepository.GetAllAsync(CancellationToken.None);
            var orgList = organizations.ToList();

            if (!orgList.Any())
            {
                _logger.LogWarning("No organizations found to process");
                return;
            }

            _logger.LogInformation("Processing {Count} organizations", orgList.Count);

            var totalSuccess = 0;
            var totalFailures = 0;
            var orgErrors = new List<string>();

            foreach (var org in orgList)
            {
                try
                {
                    _logger.LogInformation(
                        "Processing organization {OrgId} - {OrgName}",
                        org.Id, org.Name);

                    await ExecuteAsync(org.Id, billingPeriodStart, billingPeriodEnd);
                    totalSuccess++;
                }
                catch (Exception ex)
                {
                    totalFailures++;
                    var errorMsg = $"Organization {org.Name} ({org.Id}): {ex.Message}";
                    orgErrors.Add(errorMsg);
                    _logger.LogError(ex, "Failed to process organization {OrgId} - {OrgName}", org.Id, org.Name);
                }
            }

            _logger.LogInformation(
                "Monthly rent generation completed. Organizations processed: {Total}, Success: {Success}, Failures: {Failures}",
                orgList.Count, totalSuccess, totalFailures);

            if (totalFailures > 0)
            {
                _logger.LogWarning(
                    "Some organizations failed. Errors: {Errors}",
                    string.Join("; ", orgErrors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during monthly rent generation job");
            throw;
        }
    }
}
