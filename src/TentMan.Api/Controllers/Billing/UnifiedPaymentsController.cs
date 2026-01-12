using TentMan.Application.Billing.Commands.ConfirmRejectPayment;
using TentMan.Application.Billing.Commands.RecordPayment;
using TentMan.Application.Billing.Commands.UploadPaymentAttachment;
using TentMan.Application.Billing.Queries.GetPaymentHistory;
using TentMan.Application.Billing.Queries.GetPayments;
using TentMan.Application.Billing.Queries.GetPaymentsWithFilters;
using TentMan.Contracts.Common;
using TentMan.Contracts.Enums;
using TentMan.Contracts.Payments;
using TentMan.Shared.Constants.Authorization;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers.Billing;

/// <summary>
/// Unified API endpoints for payment management across the system.
/// Supports recording, confirming, rejecting, and querying payments.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/payments")]
[Authorize]
public class UnifiedPaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UnifiedPaymentsController> _logger;

    public UnifiedPaymentsController(
        IMediator mediator,
        ILogger<UnifiedPaymentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Records a payment (manual entry, online, or cash).
    /// Supports all payment modes and types.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<Guid>>> RecordPayment(
        RecordUnifiedPaymentRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Recording payment for invoice {InvoiceId} via unified endpoint", request.InvoiceId);

        var command = new RecordUnifiedPaymentCommand
        {
            InvoiceId = request.InvoiceId,
            PaymentType = request.PaymentType,
            PaymentMode = request.PaymentMode,
            Amount = request.Amount,
            PaymentDate = request.PaymentDate,
            TransactionReference = request.TransactionReference,
            GatewayTransactionId = request.GatewayTransactionId,
            GatewayName = request.GatewayName,
            PayerName = request.PayerName,
            Notes = request.Notes,
            PaymentMetadata = request.PaymentMetadata
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Failed to record payment"));
        }

        return Ok(ApiResponse<Guid>.Ok(
            result.PaymentId!.Value,
            $"Payment recorded successfully. Invoice balance: {result.InvoiceBalanceAmount:C}"));
    }

    /// <summary>
    /// Uploads a receipt/attachment to an existing payment.
    /// Supports images, PDFs, and other document types (max 10 MB).
    /// </summary>
    [HttpPost("{id:guid}/attachments")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<Guid>>> UploadAttachment(
        Guid id,
        [FromForm] IFormFile file,
        [FromForm] string? attachmentType,
        [FromForm] string? description,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Uploading attachment for payment {PaymentId}", id);

        using var fileStream = file.OpenReadStream();
        
        var command = new UploadPaymentAttachmentCommand
        {
            PaymentId = id,
            FileStream = fileStream,
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            AttachmentType = attachmentType,
            Description = description
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Failed to upload attachment"));
        }

        return Ok(ApiResponse<Guid>.Ok(
            result.AttachmentId!.Value,
            "Attachment uploaded successfully"));
    }

    /// <summary>
    /// Confirms a pending payment (owner approval).
    /// Updates payment status to Completed and updates invoice.
    /// </summary>
    [HttpPut("{id:guid}/confirm")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> ConfirmPayment(
        Guid id,
        [FromBody] ConfirmPaymentDirectRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Confirming payment {PaymentId}", id);

        var command = new ConfirmPaymentCommand
        {
            PaymentId = id,
            Notes = request.Notes
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Failed to confirm payment"));
        }

        return Ok(ApiResponse<object>.Ok(
            new { PaymentId = result.PaymentId, Status = result.NewStatus },
            "Payment confirmed successfully"));
    }

    /// <summary>
    /// Rejects a pending payment (owner rejection).
    /// Requires rejection reason.
    /// </summary>
    [HttpPut("{id:guid}/reject")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> RejectPayment(
        Guid id,
        [FromBody] RejectPaymentDirectRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rejecting payment {PaymentId}", id);

        var command = new RejectPaymentDirectCommand
        {
            PaymentId = id,
            Reason = request.Reason
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Failed to reject payment"));
        }

        return Ok(ApiResponse<object>.Ok(
            new { PaymentId = result.PaymentId, Status = result.NewStatus },
            "Payment rejected successfully"));
    }

    /// <summary>
    /// Gets payments with advanced filtering and pagination.
    /// Supports filtering by lease, invoice, status, payment mode, dates, etc.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<GetPaymentsWithFiltersResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<GetPaymentsWithFiltersResult>>> GetPayments(
        [FromQuery] Guid orgId,
        [FromQuery] Guid? leaseId,
        [FromQuery] Guid? invoiceId,
        [FromQuery] PaymentStatus? status,
        [FromQuery] PaymentMode? paymentMode,
        [FromQuery] PaymentType? paymentType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? payerName,
        [FromQuery] string? receivedBy,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting payments with filters for organization {OrgId}", orgId);

        var query = new GetPaymentsWithFiltersQuery
        {
            OrgId = orgId,
            LeaseId = leaseId,
            InvoiceId = invoiceId,
            Status = status,
            PaymentMode = paymentMode,
            PaymentType = paymentType,
            FromDate = fromDate,
            ToDate = toDate,
            PayerName = payerName,
            ReceivedBy = receivedBy,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<GetPaymentsWithFiltersResult>.Fail(result.ErrorMessage ?? "Failed to retrieve payments"));
        }

        return Ok(ApiResponse<GetPaymentsWithFiltersResult>.Ok(result, "Payments retrieved successfully"));
    }

    /// <summary>
    /// Gets payment status history for a specific payment.
    /// Shows all status changes with timestamps, who made the change, and reasons.
    /// </summary>
    [HttpGet("{id:guid}/history")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PaymentStatusHistoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PaymentStatusHistoryDto>>>> GetPaymentHistory(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting payment history for payment {PaymentId}", id);

        var query = new GetPaymentHistoryQuery { PaymentId = id };
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(ApiResponse<object>.Fail(result.ErrorMessage ?? "Payment not found"));
        }

        return Ok(ApiResponse<IEnumerable<PaymentStatusHistoryDto>>.Ok(
            result.History,
            "Payment history retrieved successfully"));
    }
}
