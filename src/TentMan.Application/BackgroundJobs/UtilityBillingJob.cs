using Microsoft.Extensions.Logging;
using TentMan.Application.Abstractions.Billing;
using TentMan.Application.Abstractions.Repositories;

namespace TentMan.Application.BackgroundJobs;

/// <summary>
/// Background job for automated utility billing generation.
/// Runs on a recurring schedule to generate invoices for leases with pending utility statements across all organizations.
/// </summary>
public class UtilityBillingJob
{
    private readonly IInvoiceRunService _invoiceRunService;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ILogger<UtilityBillingJob> _logger;

    public UtilityBillingJob(
        IInvoiceRunService invoiceRunService,
        IOrganizationRepository organizationRepository,
        ILogger<UtilityBillingJob> logger)
    {
        _invoiceRunService = invoiceRunService;
        _organizationRepository = organizationRepository;
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
    /// Processes all organizations.
    /// </summary>
    public async Task ExecuteForCurrentPeriodAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var billingPeriodStart = new DateOnly(today.Year, today.Month, 1);
        var billingPeriodEnd = billingPeriodStart.AddMonths(1).AddDays(-1);

        _logger.LogInformation(
            "Starting utility billing for all organizations. Period: {PeriodStart} to {PeriodEnd}",
            billingPeriodStart, billingPeriodEnd);

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
                "Utility billing completed. Organizations processed: {Total}, Success: {Success}, Failures: {Failures}",
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
            _logger.LogError(ex, "Fatal error during utility billing job");
            throw;
        }
    }
}
