using TentMan.Application.Abstractions.Billing;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Common;
using TentMan.Contracts.CreditNotes;
using TentMan.Shared.Constants.Authorization;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers.Billing;

/// <summary>
/// API endpoints for managing credit notes.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class CreditNotesController : ControllerBase
{
    private readonly ICreditNoteRepository _creditNoteRepository;
    private readonly ICreditNoteService _creditNoteService;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILogger<CreditNotesController> _logger;

    public CreditNotesController(
        ICreditNoteRepository creditNoteRepository,
        ICreditNoteService creditNoteService,
        IInvoiceRepository invoiceRepository,
        ILogger<CreditNotesController> logger)
    {
        _creditNoteRepository = creditNoteRepository;
        _creditNoteService = creditNoteService;
        _invoiceRepository = invoiceRepository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new credit note for an invoice.
    /// Only admins and managers can create credit notes.
    /// </summary>
    [HttpPost("api/credit-notes")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<CreditNoteDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<CreditNoteDto>>> CreateCreditNote(
        CreateCreditNoteRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating credit note for invoice {InvoiceId}", request.InvoiceId);

        // Verify invoice exists
        var invoice = await _invoiceRepository.GetByIdWithLinesAsync(request.InvoiceId, cancellationToken);
        if (invoice == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Invoice {request.InvoiceId} not found"));
        }

        // Map request to service model
        var lineRequests = request.Lines.Select(l => new CreditNoteLineRequest
        {
            InvoiceLineId = l.InvoiceLineId,
            Amount = l.Amount,
            Notes = l.Notes
        }).ToList();

        var result = await _creditNoteService.CreateCreditNoteAsync(
            request.InvoiceId,
            request.Reason,
            lineRequests,
            request.Notes,
            cancellationToken);

        if (!result.IsSuccess || result.CreditNote == null)
        {
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Failed to create credit note"));
        }

        var dto = MapToDto(result.CreditNote);

        return CreatedAtAction(
            nameof(GetCreditNote),
            new { id = result.CreditNote.Id },
            ApiResponse<CreditNoteDto>.Ok(dto, "Credit note created successfully"));
    }

    /// <summary>
    /// Gets all credit notes with optional organization filter.
    /// </summary>
    [HttpGet("api/credit-notes")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CreditNoteDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IEnumerable<CreditNoteDto>>>> GetCreditNotes(
        [FromQuery] Guid? orgId,
        [FromQuery] Guid? invoiceId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting credit notes - OrgId: {OrgId}, InvoiceId: {InvoiceId}", orgId, invoiceId);

        IEnumerable<Domain.Entities.CreditNote> creditNotes;

        if (invoiceId.HasValue)
        {
            creditNotes = await _creditNoteRepository.GetByInvoiceIdAsync(invoiceId.Value, cancellationToken);
        }
        else if (orgId.HasValue)
        {
            creditNotes = await _creditNoteRepository.GetByOrgIdAsync(orgId.Value, cancellationToken);
        }
        else
        {
            return BadRequest(ApiResponse<object>.Fail("Either Organization ID or Invoice ID is required"));
        }

        var dtos = creditNotes.Select(MapToDto).ToList();

        return Ok(ApiResponse<IEnumerable<CreditNoteDto>>.Ok(dtos, "Credit notes retrieved successfully"));
    }

    /// <summary>
    /// Gets a specific credit note by ID with all line items.
    /// </summary>
    [HttpGet("api/credit-notes/{id:guid}")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<CreditNoteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<CreditNoteDto>>> GetCreditNote(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting credit note {CreditNoteId}", id);

        var creditNote = await _creditNoteRepository.GetByIdWithLinesAsync(id, cancellationToken);
        if (creditNote == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Credit note {id} not found"));
        }

        var dto = MapToDto(creditNote);

        return Ok(ApiResponse<CreditNoteDto>.Ok(dto, "Credit note retrieved successfully"));
    }

    /// <summary>
    /// Issues a credit note (makes it final and immutable).
    /// Once issued, the credit note cannot be modified.
    /// Only admins and managers can issue credit notes.
    /// </summary>
    [HttpPost("api/credit-notes/{id:guid}/issue")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<CreditNoteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<CreditNoteDto>>> IssueCreditNote(
        Guid id,
        IssueCreditNoteRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Issuing credit note {CreditNoteId}", id);

        var result = await _creditNoteService.IssueCreditNoteAsync(id, cancellationToken);

        if (!result.IsSuccess || result.CreditNote == null)
        {
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Failed to issue credit note"));
        }

        var dto = MapToDto(result.CreditNote);

        return Ok(ApiResponse<CreditNoteDto>.Ok(dto, "Credit note issued successfully"));
    }

    /// <summary>
    /// Voids a credit note (for future implementation - currently returns not implemented).
    /// </summary>
    [HttpPost("api/credit-notes/{id:guid}/void")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<CreditNoteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<ActionResult<ApiResponse<CreditNoteDto>>> VoidCreditNote(
        Guid id,
        VoidCreditNoteRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Void credit note requested for {CreditNoteId} - Not implemented", id);

        // TODO: Implement void credit note functionality in the service layer
        return Task.FromResult<ActionResult<ApiResponse<CreditNoteDto>>>(
            BadRequest(ApiResponse<object>.Fail("Voiding credit notes is not yet implemented")));
    }

    private static CreditNoteDto MapToDto(Domain.Entities.CreditNote creditNote)
    {
        var lineDtos = creditNote.Lines.Select(line => new CreditNoteLineDto
        {
            Id = line.Id,
            CreditNoteId = line.CreditNoteId,
            InvoiceLineId = line.InvoiceLineId,
            LineNumber = line.LineNumber,
            Description = line.Description,
            Amount = line.Amount,
            Notes = line.Notes
        }).ToList();

        return new CreditNoteDto
        {
            Id = creditNote.Id,
            OrgId = creditNote.OrgId,
            InvoiceId = creditNote.InvoiceId,
            CreditNoteNumber = creditNote.CreditNoteNumber,
            CreditNoteDate = creditNote.CreditNoteDate,
            Reason = creditNote.Reason,
            TotalAmount = creditNote.TotalAmount,
            Notes = creditNote.Notes,
            AppliedAtUtc = creditNote.AppliedAtUtc,
            Lines = lineDtos,
            RowVersion = creditNote.RowVersion
        };
    }
}
