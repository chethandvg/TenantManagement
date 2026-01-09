using TentMan.Application.TenantManagement.TenantPortal.Commands.UploadTenantDocument;
using TentMan.Application.TenantManagement.TenantPortal.Commands.SubmitHandover;
using TentMan.Application.TenantManagement.TenantPortal.Queries;
using TentMan.Contracts.Common;
using TentMan.Contracts.Tenants;
using TentMan.Contracts.TenantPortal;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TentMan.Application.Abstractions;

namespace TentMan.Api.Controllers.TenantPortal;

/// <summary>
/// API endpoints for the tenant portal.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = "Tenant")]
public class TenantPortalController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TenantPortalController> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public TenantPortalController(IMediator mediator, ILogger<TenantPortalController> logger, IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _logger = logger;
        _unitOfWork = unitOfWork;
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
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
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
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
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
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
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
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
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
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
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
}
