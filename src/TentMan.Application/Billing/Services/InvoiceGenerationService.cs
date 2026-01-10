using TentMan.Application.Abstractions.Billing;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;

namespace TentMan.Application.Billing.Services;

/// <summary>
/// Service for generating invoices for leases with support for rent and recurring charges.
/// Implements idempotency by updating existing draft invoices.
/// </summary>
public class InvoiceGenerationService : IInvoiceGenerationService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILeaseRepository _leaseRepository;
    private readonly ILeaseBillingSettingRepository _billingSettingRepository;
    private readonly IChargeTypeRepository _chargeTypeRepository;
    private readonly IRentCalculationService _rentCalculationService;
    private readonly IRecurringChargeCalculationService _recurringChargeCalculationService;
    private readonly IInvoiceNumberGenerator _invoiceNumberGenerator;

    public InvoiceGenerationService(
        IInvoiceRepository invoiceRepository,
        ILeaseRepository leaseRepository,
        ILeaseBillingSettingRepository billingSettingRepository,
        IChargeTypeRepository chargeTypeRepository,
        IRentCalculationService rentCalculationService,
        IRecurringChargeCalculationService recurringChargeCalculationService,
        IInvoiceNumberGenerator invoiceNumberGenerator)
    {
        _invoiceRepository = invoiceRepository;
        _leaseRepository = leaseRepository;
        _billingSettingRepository = billingSettingRepository;
        _chargeTypeRepository = chargeTypeRepository;
        _rentCalculationService = rentCalculationService;
        _recurringChargeCalculationService = recurringChargeCalculationService;
        _invoiceNumberGenerator = invoiceNumberGenerator;
    }

    /// <inheritdoc/>
    public async Task<InvoiceGenerationResult> GenerateInvoiceAsync(
        Guid leaseId,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        ProrationMethod prorationMethod,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (billingPeriodEnd < billingPeriodStart)
            {
                return new InvoiceGenerationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Billing period end cannot be before start"
                };
            }

            // Get lease with details
            var lease = await _leaseRepository.GetByIdWithDetailsAsync(leaseId, cancellationToken);
            if (lease == null)
            {
                return new InvoiceGenerationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Lease with ID {leaseId} not found"
                };
            }

            // Only generate invoices for active leases
            if (lease.Status != LeaseStatus.Active)
            {
                return new InvoiceGenerationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Lease is not active (Status: {lease.Status})"
                };
            }

            // Check for existing draft invoice (idempotency)
            var existingInvoice = await _invoiceRepository.GetDraftInvoiceForPeriodAsync(
                leaseId, billingPeriodStart, billingPeriodEnd, cancellationToken);

            bool isUpdate = existingInvoice != null;
            var invoice = existingInvoice ?? new Invoice
            {
                Id = Guid.NewGuid(),
                OrgId = lease.OrgId,
                LeaseId = leaseId,
                BillingPeriodStart = billingPeriodStart,
                BillingPeriodEnd = billingPeriodEnd,
                Status = InvoiceStatus.Draft
            };

            // Clear existing lines if updating
            if (isUpdate && invoice.Lines.Any())
            {
                invoice.Lines.Clear();
            }

            // Get billing settings
            var billingSettings = await _billingSettingRepository.GetByLeaseIdAsync(leaseId, cancellationToken);

            // Calculate invoice date and due date
            var invoiceDate = billingPeriodEnd;
            var dueDate = CalculateDueDate(invoiceDate, billingSettings);

            invoice.InvoiceDate = invoiceDate;
            invoice.DueDate = dueDate;

            // Generate invoice number if new
            if (!isUpdate)
            {
                invoice.InvoiceNumber = await _invoiceNumberGenerator.GenerateNextAsync(
                    lease.OrgId,
                    billingSettings?.InvoicePrefix ?? "INV",
                    cancellationToken);
            }

            // Add payment instructions if available
            if (!string.IsNullOrWhiteSpace(billingSettings?.PaymentInstructions))
            {
                invoice.PaymentInstructions = billingSettings.PaymentInstructions;
            }

            // Generate invoice lines
            var lineNumber = 1;

            // 1. Generate rent lines
            lineNumber = await GenerateRentLinesAsync(invoice, lease.OrgId, leaseId, billingPeriodStart, billingPeriodEnd, prorationMethod, lineNumber, cancellationToken);

            // 2. Generate recurring charge lines
            await GenerateRecurringChargeLinesAsync(invoice, lease.OrgId, leaseId, billingPeriodStart, billingPeriodEnd, prorationMethod, lineNumber, cancellationToken);

            // 3. Calculate totals
            CalculateInvoiceTotals(invoice);

            // Save invoice
            if (isUpdate)
            {
                await _invoiceRepository.UpdateAsync(invoice, invoice.RowVersion, cancellationToken);
            }
            else
            {
                await _invoiceRepository.AddAsync(invoice, cancellationToken);
            }

            return new InvoiceGenerationResult
            {
                IsSuccess = true,
                Invoice = invoice,
                WasUpdated = isUpdate
            };
        }
        catch (Exception ex)
        {
            return new InvoiceGenerationResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to generate invoice: {ex.Message}"
            };
        }
    }

    private async Task<int> GenerateRentLinesAsync(
        Invoice invoice,
        Guid orgId,
        Guid leaseId,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        ProrationMethod prorationMethod,
        int lineNumber,
        CancellationToken cancellationToken)
    {
        var rentCalculation = await _rentCalculationService.CalculateRentAsync(
            leaseId, billingPeriodStart, billingPeriodEnd, prorationMethod, cancellationToken);

        if (rentCalculation.LineItems.Any())
        {
            var rentChargeType = await _chargeTypeRepository.GetByCodeAsync(ChargeTypeCode.RENT, orgId, cancellationToken);
            if (rentChargeType == null)
            {
                throw new InvalidOperationException("RENT charge type not found");
            }

            var rentLines = rentCalculation.LineItems.Select(rentLineItem => new InvoiceLine
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                ChargeTypeId = rentChargeType.Id,
                LineNumber = lineNumber++,
                Description = rentLineItem.Description,
                Quantity = 1,
                UnitPrice = rentLineItem.Amount,
                Amount = rentLineItem.Amount,
                TaxRate = 0, // No tax on rent by default
                TaxAmount = 0,
                TotalAmount = rentLineItem.Amount
            });

            foreach (var line in rentLines)
            {
                invoice.Lines.Add(line);
            }
        }

        return lineNumber;
    }

    private async Task<int> GenerateRecurringChargeLinesAsync(
        Invoice invoice,
        Guid orgId,
        Guid leaseId,
        DateOnly billingPeriodStart,
        DateOnly billingPeriodEnd,
        ProrationMethod prorationMethod,
        int lineNumber,
        CancellationToken cancellationToken)
    {
        var chargeCalculation = await _recurringChargeCalculationService.CalculateChargesAsync(
            leaseId, billingPeriodStart, billingPeriodEnd, prorationMethod, cancellationToken);

        if (!chargeCalculation.LineItems.Any())
        {
            return lineNumber;
        }

        // Get unique charge type IDs to avoid N+1 queries
        var uniqueChargeTypeIds = chargeCalculation.LineItems
            .Select(item => item.ChargeTypeId)
            .Distinct()
            .ToList();

        // Fetch all needed charge types in one go
        var chargeTypes = new Dictionary<Guid, ChargeType>();
        foreach (var chargeTypeId in uniqueChargeTypeIds)
        {
            var chargeType = await _chargeTypeRepository.GetByIdAsync(chargeTypeId, cancellationToken);
            if (chargeType != null)
            {
                chargeTypes[chargeTypeId] = chargeType;
            }
        }

        foreach (var chargeLineItem in chargeCalculation.LineItems)
        {
            // Get charge type from dictionary
            if (!chargeTypes.TryGetValue(chargeLineItem.ChargeTypeId, out var chargeType))
            {
                // Log warning and skip if charge type not found
                // In production, this should use proper logging infrastructure
                Console.WriteLine(
                    $"[Warning] Charge type '{chargeLineItem.ChargeTypeId}' not found for org '{orgId}' and lease '{leaseId}' " +
                    $"while generating recurring charges. Charge '{chargeLineItem.ChargeDescription}' with amount '{chargeLineItem.Amount}' was skipped.");
                continue;
            }

            var invoiceLine = new InvoiceLine
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                ChargeTypeId = chargeType.Id,
                LineNumber = lineNumber++,
                Description = chargeLineItem.ChargeDescription,
                Quantity = 1,
                UnitPrice = chargeLineItem.Amount,
                Amount = chargeLineItem.Amount,
                TaxRate = 0, // No tax by default
                TaxAmount = 0,
                TotalAmount = chargeLineItem.Amount
            };

            invoice.Lines.Add(invoiceLine);
        }

        return lineNumber;
    }

    private void CalculateInvoiceTotals(Invoice invoice)
    {
        invoice.SubTotal = invoice.Lines.Sum(l => l.Amount);
        invoice.TaxAmount = invoice.Lines.Sum(l => l.TaxAmount);
        invoice.TotalAmount = invoice.Lines.Sum(l => l.TotalAmount);
        invoice.PaidAmount = 0;
        invoice.BalanceAmount = invoice.TotalAmount;
    }

    private DateOnly CalculateDueDate(DateOnly invoiceDate, LeaseBillingSetting? billingSettings)
    {
        var paymentTermDays = billingSettings?.PaymentTermDays ?? 0;
        return invoiceDate.AddDays(paymentTermDays);
    }
}
