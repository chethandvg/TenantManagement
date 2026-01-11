using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Billing;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;

namespace TentMan.Application.Billing.Services;

/// <summary>
/// Service for managing credit note operations.
/// Implements credit note creation and issuance workflows.
/// </summary>
public class CreditNoteService : ICreditNoteService
{
    private readonly ICreditNoteRepository _creditNoteRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ICreditNoteNumberGenerator _creditNoteNumberGenerator;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreditNoteService(
        ICreditNoteRepository creditNoteRepository,
        IInvoiceRepository invoiceRepository,
        ICreditNoteNumberGenerator creditNoteNumberGenerator,
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _creditNoteRepository = creditNoteRepository;
        _invoiceRepository = invoiceRepository;
        _creditNoteNumberGenerator = creditNoteNumberGenerator;
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc/>
    public async Task<CreditNoteCreationResult> CreateCreditNoteAsync(
        Guid invoiceId,
        CreditNoteReason reason,
        IEnumerable<CreditNoteLineRequest> lineItems,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate line items
            var lineItemsList = lineItems.ToList();
            if (!lineItemsList.Any())
            {
                return new CreditNoteCreationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "At least one line item is required for credit note"
                };
            }

            // Get invoice with lines
            var invoice = await _invoiceRepository.GetByIdWithLinesAsync(invoiceId, cancellationToken);
            if (invoice == null)
            {
                return new CreditNoteCreationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Invoice with ID {invoiceId} not found"
                };
            }

            // Validate invoice is issued or paid
            if (invoice.Status != InvoiceStatus.Issued && 
                invoice.Status != InvoiceStatus.PartiallyPaid && 
                invoice.Status != InvoiceStatus.Paid &&
                invoice.Status != InvoiceStatus.Overdue)
            {
                return new CreditNoteCreationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Credit notes can only be created for issued, paid, partially paid, or overdue invoices. Current status: {invoice.Status}"
                };
            }

            // Validate all line items exist on the invoice
            var invoiceLineIds = invoice.Lines.Select(l => l.Id).ToHashSet();
            foreach (var lineItem in lineItemsList)
            {
                if (!invoiceLineIds.Contains(lineItem.InvoiceLineId))
                {
                    return new CreditNoteCreationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Invoice line {lineItem.InvoiceLineId} not found on invoice {invoiceId}"
                    };
                }

                if (lineItem.Amount <= 0)
                {
                    return new CreditNoteCreationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Credit note line amount must be positive"
                    };
                }
            }

            // Generate credit note number
            var creditNoteNumber = await _creditNoteNumberGenerator.GenerateNextAsync(
                invoice.OrgId,
                "CN",
                cancellationToken);

            // Create credit note
            var creditNote = new CreditNote
            {
                Id = Guid.NewGuid(),
                OrgId = invoice.OrgId,
                InvoiceId = invoiceId,
                CreditNoteNumber = creditNoteNumber,
                CreditNoteDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Reason = reason,
                Notes = notes,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId ?? "System"
            };

            // Create credit note lines with negative amounts
            var lineNumber = 1;
            foreach (var lineItemRequest in lineItemsList)
            {
                var invoiceLine = invoice.Lines.First(l => l.Id == lineItemRequest.InvoiceLineId);
                
                // Validate credit amount doesn't exceed invoice line amount
                if (lineItemRequest.Amount > invoiceLine.TotalAmount)
                {
                    return new CreditNoteCreationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Credit amount {lineItemRequest.Amount:C} exceeds invoice line amount {invoiceLine.TotalAmount:C}"
                    };
                }

                // Calculate tax amount proportionally based on the invoice line's tax/total relationship
                // This handles both tax-inclusive and tax-exclusive scenarios
                var creditTaxAmount = invoiceLine.TotalAmount > 0
                    ? lineItemRequest.Amount * (invoiceLine.TaxAmount / invoiceLine.TotalAmount)
                    : 0;
                var creditAmount = lineItemRequest.Amount - creditTaxAmount; // Base (pre-tax) portion

                var creditNoteLine = new CreditNoteLine
                {
                    Id = Guid.NewGuid(),
                    CreditNoteId = creditNote.Id,
                    InvoiceLineId = lineItemRequest.InvoiceLineId,
                    LineNumber = lineNumber++,
                    Description = $"Credit for: {invoiceLine.Description}",
                    Quantity = 1,
                    UnitPrice = -creditAmount, // Negative for credit
                    Amount = -creditAmount, // Negative for credit
                    TaxAmount = -creditTaxAmount, // Negative for credit
                    TotalAmount = -lineItemRequest.Amount, // Negative for credit
                    Notes = lineItemRequest.Notes,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedBy = _currentUser.UserId ?? "System"
                };

                creditNote.Lines.Add(creditNoteLine);
            }

            // Calculate credit note total (sum of negative line amounts)
            creditNote.TotalAmount = creditNote.Lines.Sum(l => l.TotalAmount);

            // Save credit note
            await _creditNoteRepository.AddAsync(creditNote, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new CreditNoteCreationResult
            {
                IsSuccess = true,
                CreditNote = creditNote
            };
        }
        catch (Exception ex)
        {
            return new CreditNoteCreationResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to create credit note: {ex.Message}"
            };
        }
    }

    /// <inheritdoc/>
    public async Task<CreditNoteIssueResult> IssueCreditNoteAsync(Guid creditNoteId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get credit note with lines
            var creditNote = await _creditNoteRepository.GetByIdWithLinesAsync(creditNoteId, cancellationToken);
            if (creditNote == null)
            {
                return new CreditNoteIssueResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Credit note with ID {creditNoteId} not found"
                };
            }

            // Check if already issued
            if (creditNote.AppliedAtUtc.HasValue)
            {
                return new CreditNoteIssueResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Credit note has already been issued"
                };
            }

            // Validate credit note has lines
            if (!creditNote.Lines.Any())
            {
                return new CreditNoteIssueResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Credit note cannot be issued without line items"
                };
            }

            // Issue the credit note
            creditNote.AppliedAtUtc = DateTime.UtcNow;
            creditNote.ModifiedAtUtc = DateTime.UtcNow;
            creditNote.ModifiedBy = _currentUser.UserId ?? "System";

            await _creditNoteRepository.UpdateAsync(creditNote, creditNote.RowVersion, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new CreditNoteIssueResult
            {
                IsSuccess = true,
                CreditNote = creditNote
            };
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return new CreditNoteIssueResult
            {
                IsSuccess = false,
                ErrorMessage = "Credit note was modified by another process. Please retry."
            };
        }
        catch (Exception ex)
        {
            return new CreditNoteIssueResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to issue credit note: {ex.Message}"
            };
        }
    }
}
