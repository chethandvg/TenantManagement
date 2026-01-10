using TentMan.Application.Abstractions.Billing;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;

namespace TentMan.Application.Billing.Services;

/// <summary>
/// Service for orchestrating batch invoice generation runs.
/// Handles processing multiple leases with partial failure support.
/// </summary>
public class InvoiceRunService : IInvoiceRunService
{
    private readonly IInvoiceRunRepository _invoiceRunRepository;
    private readonly ILeaseRepository _leaseRepository;
    private readonly IInvoiceGenerationService _invoiceGenerationService;

    public InvoiceRunService(
        IInvoiceRunRepository invoiceRunRepository,
        ILeaseRepository leaseRepository,
        IInvoiceGenerationService invoiceGenerationService)
    {
        _invoiceRunRepository = invoiceRunRepository;
        _leaseRepository = leaseRepository;
        _invoiceGenerationService = invoiceGenerationService;
    }

    /// <inheritdoc/>
    public async Task<InvoiceRunResult> ExecuteMonthlyRentRunAsync(
        Guid orgId,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        ProrationMethod prorationMethod,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create invoice run
            var invoiceRun = new InvoiceRun
            {
                Id = Guid.NewGuid(),
                OrgId = orgId,
                RunNumber = GenerateRunNumber(billingPeriodStart),
                BillingPeriodStart = billingPeriodStart,
                BillingPeriodEnd = billingPeriodEnd,
                Status = InvoiceRunStatus.InProgress,
                StartedAtUtc = DateTime.UtcNow
            };

            // Get all active leases for the organization
            var activeLeases = await _leaseRepository.GetByOrgIdAsync(orgId, LeaseStatus.Active, cancellationToken);
            var leasesList = activeLeases.ToList();

            invoiceRun.TotalLeases = leasesList.Count;

            if (!leasesList.Any())
            {
                invoiceRun.Status = InvoiceRunStatus.Completed;
                invoiceRun.CompletedAtUtc = DateTime.UtcNow;
                invoiceRun.Notes = "No active leases found";
                await _invoiceRunRepository.AddAsync(invoiceRun, cancellationToken);

                return new InvoiceRunResult
                {
                    IsSuccess = true,
                    InvoiceRun = invoiceRun,
                    TotalLeases = 0,
                    SuccessCount = 0,
                    FailureCount = 0
                };
            }

            // Process each lease
            var errorMessages = new List<string>();
            int successCount = 0;
            int failureCount = 0;

            foreach (var lease in leasesList)
            {
                var runItem = new InvoiceRunItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceRunId = invoiceRun.Id,
                    LeaseId = lease.Id,
                    ProcessedAtUtc = DateTime.UtcNow
                };

                try
                {
                    // Generate invoice for this lease
                    var result = await _invoiceGenerationService.GenerateInvoiceAsync(
                        lease.Id,
                        billingPeriodStart,
                        billingPeriodEnd,
                        prorationMethod,
                        cancellationToken);

                    if (result.IsSuccess && result.Invoice != null)
                    {
                        runItem.IsSuccess = true;
                        runItem.InvoiceId = result.Invoice.Id;
                        successCount++;
                    }
                    else
                    {
                        runItem.IsSuccess = false;
                        runItem.ErrorMessage = result.ErrorMessage ?? "Unknown error";
                        errorMessages.Add($"Lease {lease.LeaseNumber ?? lease.Id.ToString()}: {runItem.ErrorMessage}");
                        failureCount++;
                    }
                }
                catch (Exception ex)
                {
                    runItem.IsSuccess = false;
                    runItem.ErrorMessage = ex.Message;
                    errorMessages.Add($"Lease {lease.LeaseNumber ?? lease.Id.ToString()}: {ex.Message}");
                    failureCount++;
                }

                invoiceRun.Items.Add(runItem);
            }

            // Update run status
            invoiceRun.SuccessCount = successCount;
            invoiceRun.FailureCount = failureCount;
            invoiceRun.CompletedAtUtc = DateTime.UtcNow;

            if (failureCount == 0)
            {
                invoiceRun.Status = InvoiceRunStatus.Completed;
            }
            else if (successCount > 0)
            {
                invoiceRun.Status = InvoiceRunStatus.CompletedWithErrors;
                invoiceRun.ErrorMessage = string.Join("; ", errorMessages.Take(10)); // Limit to first 10 errors
            }
            else
            {
                invoiceRun.Status = InvoiceRunStatus.Failed;
                invoiceRun.ErrorMessage = "All invoices failed to generate";
            }

            // Save invoice run
            await _invoiceRunRepository.AddAsync(invoiceRun, cancellationToken);

            return new InvoiceRunResult
            {
                IsSuccess = true,
                InvoiceRun = invoiceRun,
                TotalLeases = invoiceRun.TotalLeases,
                SuccessCount = successCount,
                FailureCount = failureCount,
                ErrorMessages = errorMessages
            };
        }
        catch (Exception ex)
        {
            return new InvoiceRunResult
            {
                IsSuccess = false,
                ErrorMessages = new List<string> { $"Invoice run failed: {ex.Message}" }
            };
        }
    }

    /// <inheritdoc/>
    public async Task<InvoiceRunResult> ExecuteUtilityRunAsync(
        Guid orgId,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        CancellationToken cancellationToken = default)
    {
        // Placeholder implementation for utility billing run
        // This would need a UtilityStatementRepository to find pending statements
        // For now, return a not implemented result
        return await Task.FromResult(new InvoiceRunResult
        {
            IsSuccess = false,
            ErrorMessages = new List<string> { "Utility billing run not yet implemented - requires UtilityStatementRepository" }
        });
    }

    private string GenerateRunNumber(DateOnly billingPeriodStart)
    {
        return $"RUN-{billingPeriodStart:yyyyMM}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }
}
