using TentMan.Application.Abstractions.Billing;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Common;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Invoices;
using TentMan.Shared.Constants.Authorization;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers.Billing;

/// <summary>
/// API endpoints for managing invoices.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILeaseRepository _leaseRepository;
    private readonly IInvoiceGenerationService _invoiceGenerationService;
    private readonly IInvoiceManagementService _invoiceManagementService;
    private readonly IChargeTypeRepository _chargeTypeRepository;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(
        IInvoiceRepository invoiceRepository,
        ILeaseRepository leaseRepository,
        IInvoiceGenerationService invoiceGenerationService,
        IInvoiceManagementService invoiceManagementService,
        IChargeTypeRepository chargeTypeRepository,
        ILogger<InvoicesController> logger)
    {
        _invoiceRepository = invoiceRepository;
        _leaseRepository = leaseRepository;
        _invoiceGenerationService = invoiceGenerationService;
        _invoiceManagementService = invoiceManagementService;
        _chargeTypeRepository = chargeTypeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Generates a draft invoice for a lease for the current billing period.
    /// Uses the default proration method.
    /// Only admins and managers can generate invoices.
    /// </summary>
    [HttpPost("api/leases/{leaseId:guid}/invoices/generate")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> GenerateInvoice(
        Guid leaseId,
        [FromQuery] DateOnly? periodStart,
        [FromQuery] DateOnly? periodEnd,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating invoice for lease {LeaseId}", leaseId);

        // Verify lease exists
        if (!await _leaseRepository.ExistsAsync(leaseId, cancellationToken))
        {
            return NotFound(ApiResponse<object>.Fail($"Lease {leaseId} not found"));
        }

        // Default to current month if not specified
        var billingStart = periodStart ?? new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var billingEnd = periodEnd ?? billingStart.AddMonths(1).AddDays(-1);

        var result = await _invoiceGenerationService.GenerateInvoiceAsync(
            leaseId,
            billingStart,
            billingEnd,
            ProrationMethod.ActualDaysInMonth,
            cancellationToken);

        if (!result.IsSuccess || result.Invoice == null)
        {
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Failed to generate invoice"));
        }

        var dto = await MapToDto(result.Invoice, cancellationToken);

        return Ok(ApiResponse<InvoiceDto>.Ok(dto, result.WasUpdated ? "Invoice updated successfully" : "Invoice generated successfully"));
    }

    /// <summary>
    /// Gets all invoices with optional filters.
    /// </summary>
    [HttpGet("api/invoices")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<InvoiceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IEnumerable<InvoiceDto>>>> GetInvoices(
        [FromQuery] Guid? orgId,
        [FromQuery] InvoiceStatus? status,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting invoices with filters - OrgId: {OrgId}, Status: {Status}", orgId, status);

        if (!orgId.HasValue)
        {
            return BadRequest(ApiResponse<object>.Fail("Organization ID is required"));
        }

        var invoices = await _invoiceRepository.GetByOrgIdAsync(orgId.Value, status, cancellationToken);

        var dtos = new List<InvoiceDto>();
        foreach (var invoice in invoices)
        {
            dtos.Add(await MapToDto(invoice, cancellationToken));
        }

        return Ok(ApiResponse<IEnumerable<InvoiceDto>>.Ok(dtos, "Invoices retrieved successfully"));
    }

    /// <summary>
    /// Gets a specific invoice by ID with all line items.
    /// </summary>
    [HttpGet("api/invoices/{id:guid}")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> GetInvoice(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting invoice {InvoiceId}", id);

        var invoice = await _invoiceRepository.GetByIdWithLinesAsync(id, cancellationToken);
        if (invoice == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Invoice {id} not found"));
        }

        var dto = await MapToDto(invoice, cancellationToken);

        return Ok(ApiResponse<InvoiceDto>.Ok(dto, "Invoice retrieved successfully"));
    }

    /// <summary>
    /// Issues an invoice (transitions from Draft to Issued).
    /// Once issued, the invoice cannot be modified.
    /// Only admins and managers can issue invoices.
    /// </summary>
    [HttpPost("api/invoices/{id:guid}/issue")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> IssueInvoice(
        Guid id,
        IssueInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Issuing invoice {InvoiceId}", id);

        var result = await _invoiceManagementService.IssueInvoiceAsync(id, cancellationToken);

        if (!result.IsSuccess || result.Invoice == null)
        {
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Failed to issue invoice"));
        }

        var dto = await MapToDto(result.Invoice, cancellationToken);

        return Ok(ApiResponse<InvoiceDto>.Ok(dto, "Invoice issued successfully"));
    }

    /// <summary>
    /// Voids an invoice with a reason.
    /// Voided invoices cannot be edited or un-voided.
    /// Only admins and managers can void invoices.
    /// </summary>
    [HttpPost("api/invoices/{id:guid}/void")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> VoidInvoice(
        Guid id,
        VoidInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Voiding invoice {InvoiceId}", id);

        var result = await _invoiceManagementService.VoidInvoiceAsync(id, request.VoidReason, cancellationToken);

        if (!result.IsSuccess || result.Invoice == null)
        {
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Failed to void invoice"));
        }

        var dto = await MapToDto(result.Invoice, cancellationToken);

        return Ok(ApiResponse<InvoiceDto>.Ok(dto, "Invoice voided successfully"));
    }

    private async Task<InvoiceDto> MapToDto(Domain.Entities.Invoice invoice, CancellationToken cancellationToken)
    {
        var lineDtos = new List<InvoiceLineDto>();
        foreach (var line in invoice.Lines)
        {
            var chargeType = await _chargeTypeRepository.GetByIdAsync(line.ChargeTypeId, cancellationToken);
            lineDtos.Add(new InvoiceLineDto
            {
                Id = line.Id,
                InvoiceId = line.InvoiceId,
                ChargeTypeId = line.ChargeTypeId,
                ChargeTypeName = chargeType?.Name ?? "Unknown",
                LineNumber = line.LineNumber,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                Amount = line.Amount,
                TaxRate = line.TaxRate,
                TaxAmount = line.TaxAmount,
                TotalAmount = line.TotalAmount,
                Notes = line.Notes
            });
        }

        return new InvoiceDto
        {
            Id = invoice.Id,
            OrgId = invoice.OrgId,
            LeaseId = invoice.LeaseId,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            Status = invoice.Status,
            BillingPeriodStart = invoice.BillingPeriodStart,
            BillingPeriodEnd = invoice.BillingPeriodEnd,
            SubTotal = invoice.SubTotal,
            TaxAmount = invoice.TaxAmount,
            TotalAmount = invoice.TotalAmount,
            PaidAmount = invoice.PaidAmount,
            BalanceAmount = invoice.BalanceAmount,
            IssuedAtUtc = invoice.IssuedAtUtc,
            PaidAtUtc = invoice.PaidAtUtc,
            VoidedAtUtc = invoice.VoidedAtUtc,
            Notes = invoice.Notes,
            PaymentInstructions = invoice.PaymentInstructions,
            VoidReason = invoice.VoidReason,
            Lines = lineDtos,
            RowVersion = invoice.RowVersion
        };
    }
}
