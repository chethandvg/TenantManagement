using Microsoft.Extensions.Logging;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;

namespace TentMan.Application.BackgroundJobs;

/// <summary>
/// Background job for detecting overdue payments and updating invoice statuses.
/// Should run daily to check for invoices that have passed their due date without full payment.
/// </summary>
public class OverduePaymentDetectionJob
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<OverduePaymentDetectionJob> _logger;

    public OverduePaymentDetectionJob(
        IInvoiceRepository invoiceRepository,
        IPaymentRepository paymentRepository,
        ILogger<OverduePaymentDetectionJob> logger)
    {
        _invoiceRepository = invoiceRepository;
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    /// <summary>
    /// Executes the overdue detection job for a specific organization.
    /// Updates invoice status to Overdue for unpaid or partially paid invoices past their due date.
    /// </summary>
    /// <param name="orgId">Organization ID to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ExecuteAsync(Guid orgId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting overdue payment detection job for organization {OrgId}", orgId);

        try
        {
            // Get all issued or partially paid invoices for the organization
            var invoices = await _invoiceRepository.GetByOrgIdAsync(orgId, null, cancellationToken);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var overdueInvoices = invoices.Where(i => 
                (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid) &&
                i.DueDate < today).ToList();

            _logger.LogInformation("Found {Count} potentially overdue invoices for organization {OrgId}", 
                overdueInvoices.Count, orgId);

            int updatedCount = 0;

            foreach (var invoice in overdueInvoices)
            {
                try
                {
                    // Verify payment status
                    var totalPaid = await _paymentRepository.GetTotalPaidAmountAsync(invoice.Id, cancellationToken);
                    var balanceAmount = invoice.TotalAmount - totalPaid;

                    // Update status if there's an outstanding balance
                    if (balanceAmount > 0 && invoice.Status != InvoiceStatus.Overdue)
                    {
                        invoice.Status = InvoiceStatus.Overdue;
                        invoice.PaidAmount = totalPaid;
                        invoice.BalanceAmount = balanceAmount;
                        invoice.ModifiedAtUtc = DateTime.UtcNow;
                        invoice.ModifiedBy = "OverdueDetectionJob";

                        await _invoiceRepository.UpdateAsync(invoice, invoice.RowVersion, cancellationToken);
                        updatedCount++;

                        _logger.LogInformation(
                            "Marked invoice {InvoiceId} as Overdue. Balance: {Balance:C}, Due Date: {DueDate}",
                            invoice.Id, balanceAmount, invoice.DueDate);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Failed to process invoice {InvoiceId} during overdue detection", 
                        invoice.Id);
                    // Continue processing other invoices
                }
            }

            _logger.LogInformation(
                "Completed overdue payment detection job for organization {OrgId}. Updated {UpdatedCount} invoices.",
                orgId, updatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to execute overdue payment detection job for organization {OrgId}", 
                orgId);
            throw;
        }
    }

    /// <summary>
    /// Executes the overdue detection job for all organizations.
    /// Should be called by a scheduled job runner (e.g., Hangfire, Quartz.NET).
    /// </summary>
    public async Task ExecuteForAllOrganizationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting overdue payment detection job for all organizations");

        try
        {
            // Note: In a real implementation, you would retrieve organization IDs
            // This is a placeholder that would be implemented when integrated with a job scheduler
            _logger.LogWarning(
                "ExecuteForAllOrganizationsAsync requires organization enumeration. " +
                "Use ExecuteAsync with specific orgId when calling from scheduler.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute overdue payment detection for all organizations");
            throw;
        }
    }
}
