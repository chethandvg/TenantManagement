using TentMan.Application.TenantManagement.TenantPortal.Commands.UploadTenantDocument;
using TentMan.Application.TenantManagement.TenantPortal.Commands.SubmitHandover;
using TentMan.Application.TenantManagement.TenantPortal.Queries;
using TentMan.Contracts.Common;
using TentMan.Contracts.Tenants;
using TentMan.Contracts.TenantPortal;
using TentMan.Contracts.Invoices;
using TentMan.Contracts.Enums;
using TentMan.Shared.Constants.Authorization;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemClaimTypes = System.Security.Claims.ClaimTypes;
using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;

namespace TentMan.Api.Controllers.TenantPortal;

/// <summary>
/// API endpoints for the tenant portal.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = PolicyNames.RequireTenantRole)]
public class TenantPortalController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TenantPortalController> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IChargeTypeRepository _chargeTypeRepository;

    public TenantPortalController(
        IMediator mediator, 
        ILogger<TenantPortalController> logger, 
        IUnitOfWork unitOfWork,
        IInvoiceRepository invoiceRepository,
        IChargeTypeRepository chargeTypeRepository)
    {
        _mediator = mediator;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _invoiceRepository = invoiceRepository;
        _chargeTypeRepository = chargeTypeRepository;
    }

    /// <summary>
    /// Gets the current tenant's active lease summary.
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/tenant-portal/lease-summary")]
    [ProducesResponseType(typeof(ApiResponse<TenantLeaseSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TenantLeaseSummaryResponse>>> GetLeaseSummary(
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(SystemClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Invalid user ID claim for tenant lease summary");
            return Unauthorized(ApiResponse<object>.Fail("Invalid user authentication"));
        }

        _logger.LogInformation("Getting lease summary for user {UserId}", userId);

        var query = new GetTenantLeaseByUserIdQuery(userId);
        var leaseSummary = await _mediator.Send(query, cancellationToken);

        if (leaseSummary == null)
        {
            return NotFound(ApiResponse<object>.Fail("No active lease found for the current tenant"));
        }

        return Ok(ApiResponse<TenantLeaseSummaryResponse>.Ok(leaseSummary, "Lease summary retrieved successfully"));
    }

    /// <summary>
    /// Uploads a document for the current tenant.
    /// </summary>
    [HttpPost("api/v{version:apiVersion}/tenant-portal/documents")]
    [ProducesResponseType(typeof(ApiResponse<TenantDocumentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TenantDocumentDto>>> UploadDocument(
        [FromForm] TenantDocumentUploadRequest request,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(SystemClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Invalid user ID claim for document upload");
            return Unauthorized(ApiResponse<object>.Fail("Invalid user authentication"));
        }

        _logger.LogInformation("Uploading document for user {UserId}", userId);

        // Get tenant from linked user
        var tenant = await _unitOfWork.Tenants.GetByLinkedUserIdAsync(userId, cancellationToken);
        if (tenant == null)
        {
            return NotFound(ApiResponse<object>.Fail("No tenant found for the current user"));
        }

        try
        {
            using var stream = file.OpenReadStream();
            var command = new UploadTenantDocumentCommand(
                tenant.Id,
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                request);

            var document = await _mediator.Send(command, cancellationToken);
            
            return CreatedAtAction(
                nameof(GetDocuments),
                new { version = "1.0" },
                ApiResponse<TenantDocumentDto>.Ok(document, "Document uploaded successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Gets all documents for the current tenant.
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/tenant-portal/documents")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TenantDocumentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<TenantDocumentDto>>>> GetDocuments(
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(SystemClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Invalid user ID claim for get documents");
            return Unauthorized(ApiResponse<object>.Fail("Invalid user authentication"));
        }

        _logger.LogInformation("Getting documents for user {UserId}", userId);

        // Get tenant from linked user
        var tenant = await _unitOfWork.Tenants.GetByLinkedUserIdAsync(userId, cancellationToken);
        if (tenant == null)
        {
            return NotFound(ApiResponse<object>.Fail("No tenant found for the current user"));
        }

        try
        {
            var query = new GetTenantDocumentsQuery(tenant.Id);
            var documents = await _mediator.Send(query, cancellationToken);

            return Ok(ApiResponse<IEnumerable<TenantDocumentDto>>.Ok(documents, "Documents retrieved successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Gets the move-in handover checklist for the current tenant.
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/tenant-portal/move-in-handover")]
    [ProducesResponseType(typeof(ApiResponse<MoveInHandoverResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<MoveInHandoverResponse>>> GetMoveInHandover(
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(SystemClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Invalid user ID claim for move-in handover");
            return Unauthorized(ApiResponse<object>.Fail("Invalid user authentication"));
        }

        _logger.LogInformation("Getting move-in handover for user {UserId}", userId);

        var query = new GetMoveInHandoverQuery(userId);
        var handover = await _mediator.Send(query, cancellationToken);

        if (handover == null)
        {
            return NotFound(ApiResponse<object>.Fail("No move-in handover found for the current tenant"));
        }

        return Ok(ApiResponse<MoveInHandoverResponse>.Ok(handover, "Move-in handover retrieved successfully"));
    }

    /// <summary>
    /// Submits the move-in handover checklist with tenant signature.
    /// </summary>
    [HttpPost("api/v{version:apiVersion}/tenant-portal/move-in-handover/submit")]
    [ProducesResponseType(typeof(ApiResponse<MoveInHandoverResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<MoveInHandoverResponse>>> SubmitMoveInHandover(
        [FromForm] SubmitHandoverRequest request,
        IFormFile signatureImage,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(SystemClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Invalid user ID claim for submit handover");
            return Unauthorized(ApiResponse<object>.Fail("Invalid user authentication"));
        }

        _logger.LogInformation("Submitting move-in handover for user {UserId}", userId);

        if (signatureImage == null)
        {
            return BadRequest(ApiResponse<object>.Fail("Signature image is required"));
        }

        try
        {
            byte[] signatureBytes;
            using (var stream = signatureImage.OpenReadStream())
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream, cancellationToken);
                signatureBytes = memoryStream.ToArray();
            }

            var command = new SubmitHandoverCommand(
                userId,
                request,
                signatureBytes,
                signatureImage.FileName,
                signatureImage.ContentType);

            var result = await _mediator.Send(command, cancellationToken);
            
            return Ok(ApiResponse<MoveInHandoverResponse>.Ok(result, "Move-in handover submitted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Gets all invoices for the current tenant's lease(s).
    /// Only returns issued invoices (not drafts).
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/tenant-portal/invoices")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<InvoiceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<InvoiceDto>>>> GetInvoices(
        [FromQuery] InvoiceStatus? status,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(SystemClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Invalid user ID claim for get tenant invoices");
            return Unauthorized(ApiResponse<object>.Fail("Invalid user authentication"));
        }

        _logger.LogInformation("Getting invoices for user {UserId}", userId);

        // Get tenant's lease
        var query = new GetTenantLeaseByUserIdQuery(userId);
        var leaseSummary = await _mediator.Send(query, cancellationToken);

        if (leaseSummary == null)
        {
            return NotFound(ApiResponse<object>.Fail("No active lease found for the current tenant"));
        }

        // Get invoices for the lease
        var allInvoices = await _invoiceRepository.GetByLeaseIdAsync(leaseSummary.LeaseId, cancellationToken);
        
        // Always exclude draft invoices; optionally filter by requested status (except Draft)
        var invoiceQuery = allInvoices.Where(i => i.Status != InvoiceStatus.Draft);
        if (status.HasValue && status.Value != InvoiceStatus.Draft)
        {
            invoiceQuery = invoiceQuery.Where(i => i.Status == status.Value);
        }
        var filteredInvoices = invoiceQuery.ToList();

        // Pre-load all unique charge types needed across all filtered invoices
        var allChargeTypeIds = filteredInvoices
            .SelectMany(inv => inv.Lines.Select(l => l.ChargeTypeId))
            .Distinct()
            .ToList();
        
        var chargeTypes = await _chargeTypeRepository.GetByIdsAsync(allChargeTypeIds, cancellationToken);
        var chargeTypesDict = chargeTypes.ToDictionary(ct => ct.Id, ct => ct.Name);

        var dtos = new List<InvoiceDto>();
        foreach (var invoice in filteredInvoices)
        {
            dtos.Add(MapToDto(invoice, chargeTypesDict));
        }

        return Ok(ApiResponse<IEnumerable<InvoiceDto>>.Ok(dtos, "Invoices retrieved successfully"));
    }

    /// <summary>
    /// Gets a specific invoice by ID for the current tenant.
    /// Only returns invoices that belong to the tenant's lease.
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/tenant-portal/invoices/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> GetInvoice(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(SystemClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Invalid user ID claim for get tenant invoice");
            return Unauthorized(ApiResponse<object>.Fail("Invalid user authentication"));
        }

        _logger.LogInformation("Getting invoice {InvoiceId} for user {UserId}", id, userId);

        // Get tenant's lease to verify ownership
        var query = new GetTenantLeaseByUserIdQuery(userId);
        var leaseSummary = await _mediator.Send(query, cancellationToken);

        if (leaseSummary == null)
        {
            return NotFound(ApiResponse<object>.Fail("No active lease found for the current tenant"));
        }

        // Get the invoice
        var invoice = await _invoiceRepository.GetByIdWithLinesAsync(id, cancellationToken);
        if (invoice == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Invoice {id} not found"));
        }

        // Verify the invoice belongs to the tenant's lease
        if (invoice.LeaseId != leaseSummary.LeaseId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, 
                ApiResponse<object>.Fail("You do not have access to this invoice"));
        }

        // Do not expose draft invoices to tenants
        if (invoice.Status == InvoiceStatus.Draft)
        {
            return NotFound(ApiResponse<object>.Fail("Invoice not found"));
        }

        // Pre-load charge types for this invoice
        var chargeTypeIds = invoice.Lines.Select(l => l.ChargeTypeId).Distinct().ToList();
        var chargeTypes = await _chargeTypeRepository.GetByIdsAsync(chargeTypeIds, cancellationToken);
        var chargeTypesDict = chargeTypes.ToDictionary(ct => ct.Id, ct => ct.Name);

        var dto = MapToDto(invoice, chargeTypesDict);

        return Ok(ApiResponse<InvoiceDto>.Ok(dto, "Invoice retrieved successfully"));
    }

    private InvoiceDto MapToDto(Domain.Entities.Invoice invoice, Dictionary<Guid, string> chargeTypesDict)
    {
        var lineDtos = new List<InvoiceLineDto>();
        foreach (var line in invoice.Lines)
        {
            lineDtos.Add(new InvoiceLineDto
            {
                Id = line.Id,
                InvoiceId = line.InvoiceId,
                ChargeTypeId = line.ChargeTypeId,
                ChargeTypeName = chargeTypesDict.GetValueOrDefault(line.ChargeTypeId, "Unknown"),
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
