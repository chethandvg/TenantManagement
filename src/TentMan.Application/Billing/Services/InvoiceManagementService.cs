using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Billing;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;

namespace TentMan.Application.Billing.Services;

/// <summary>
/// Service for managing invoice lifecycle operations (issue, void).
/// Implements business rules for invoice state transitions.
/// </summary>
public class InvoiceManagementService : IInvoiceManagementService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IApplicationDbContext _dbContext;

    public InvoiceManagementService(
        IInvoiceRepository invoiceRepository,
        IApplicationDbContext dbContext)
    {
        _invoiceRepository = invoiceRepository;
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<InvoiceIssueResult> IssueInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get invoice with lines
            var invoice = await _invoiceRepository.GetByIdWithLinesAsync(invoiceId, cancellationToken);
            if (invoice == null)
            {
                return new InvoiceIssueResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Invoice with ID {invoiceId} not found"
                };
            }

            // Validate invoice can be issued
            if (invoice.Status != InvoiceStatus.Draft)
            {
                return new InvoiceIssueResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Invoice cannot be issued. Current status: {invoice.Status}. Only Draft invoices can be issued."
                };
            }

            // Validate invoice has lines
            if (!invoice.Lines.Any())
            {
                return new InvoiceIssueResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invoice cannot be issued without line items"
                };
            }

            // Validate invoice has a valid total
            if (invoice.TotalAmount <= 0)
            {
                return new InvoiceIssueResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invoice cannot be issued with zero or negative total amount"
                };
            }

            // Issue the invoice
            invoice.Status = InvoiceStatus.Issued;
            invoice.IssuedAtUtc = DateTime.UtcNow;
            invoice.ModifiedAtUtc = DateTime.UtcNow;
            invoice.ModifiedBy = "System"; // In production, this should come from current user context

            await _invoiceRepository.UpdateAsync(invoice, invoice.RowVersion, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new InvoiceIssueResult
            {
                IsSuccess = true,
                Invoice = invoice
            };
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return new InvoiceIssueResult
            {
                IsSuccess = false,
                ErrorMessage = "Invoice was modified by another process. Please retry."
            };
        }
        catch (Exception ex)
        {
            return new InvoiceIssueResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to issue invoice: {ex.Message}"
            };
        }
    }

    /// <inheritdoc/>
    public async Task<InvoiceVoidResult> VoidInvoiceAsync(Guid invoiceId, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate reason
            if (string.IsNullOrWhiteSpace(reason))
            {
                return new InvoiceVoidResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Void reason is required"
                };
            }

            // Get invoice
            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
            if (invoice == null)
            {
                return new InvoiceVoidResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Invoice with ID {invoiceId} not found"
                };
            }

            // Validate invoice can be voided
            if (invoice.Status == InvoiceStatus.Voided)
            {
                return new InvoiceVoidResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invoice is already voided"
                };
            }

            if (invoice.Status == InvoiceStatus.Draft)
            {
                return new InvoiceVoidResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Draft invoices should be deleted, not voided"
                };
            }

            // Check if invoice has payments (in a real system)
            // For now, we'll allow voiding of any non-draft, non-voided invoice
            if (invoice.PaidAmount > 0)
            {
                return new InvoiceVoidResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Cannot void an invoice with payments. Issue a credit note instead."
                };
            }

            // Void the invoice
            invoice.Status = InvoiceStatus.Voided;
            invoice.VoidedAtUtc = DateTime.UtcNow;
            invoice.VoidReason = reason;
            invoice.ModifiedAtUtc = DateTime.UtcNow;
            invoice.ModifiedBy = "System"; // In production, this should come from current user context

            await _invoiceRepository.UpdateAsync(invoice, invoice.RowVersion, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new InvoiceVoidResult
            {
                IsSuccess = true,
                Invoice = invoice
            };
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return new InvoiceVoidResult
            {
                IsSuccess = false,
                ErrorMessage = "Invoice was modified by another process. Please retry."
            };
        }
        catch (Exception ex)
        {
            return new InvoiceVoidResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to void invoice: {ex.Message}"
            };
        }
    }
}
