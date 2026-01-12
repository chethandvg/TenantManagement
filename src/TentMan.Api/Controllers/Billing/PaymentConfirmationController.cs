using TentMan.Application.Billing.Commands.PaymentConfirmation;
using TentMan.Application.Billing.Queries.GetPaymentConfirmationRequests;
using TentMan.Contracts.Common;
using TentMan.Contracts.Payments;
using TentMan.Shared.Constants.Authorization;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers.Billing;

/// <summary>
/// API endpoints for managing payment confirmation requests.
/// Allows tenants to request payment confirmation and owners to review/approve them.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class PaymentConfirmationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentConfirmationController> _logger;

    public PaymentConfirmationController(
        IMediator mediator,
        ILogger<PaymentConfirmationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a payment confirmation request for an invoice.
    /// Tenants can submit proof of cash payment for owner verification.
    /// </summary>
    [HttpPost("api/invoices/{invoiceId:guid}/payment-confirmation-requests")]
    [Authorize(Policy = PolicyNames.RequireTenantRole)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreatePaymentConfirmationRequest(
        Guid invoiceId,
        [FromForm] CreatePaymentConfirmationRequest request,
        [FromForm] IFormFile? proofFile,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating payment confirmation request for invoice {InvoiceId}", invoiceId);

        var command = new CreatePaymentConfirmationRequestCommand
        {
            InvoiceId = invoiceId,
            Amount = request.Amount,
            PaymentDate = request.PaymentDate,
            ReceiptNumber = request.ReceiptNumber,
            Notes = request.Notes,
            ProofFileStream = proofFile?.OpenReadStream(),
            ProofFileName = proofFile?.FileName,
            ProofFileContentType = proofFile?.ContentType,
            ProofFileSize = proofFile?.Length ?? 0
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Failed to create payment confirmation request"));
        }

        return Ok(ApiResponse<Guid>.Ok(
            result.RequestId!.Value,
            "Payment confirmation request created successfully. Awaiting owner review."));
    }

    /// <summary>
    /// Gets all pending payment confirmation requests for the organization.
    /// Only owners, administrators, and managers can view pending requests.
    /// </summary>
    [HttpGet("api/organizations/{orgId:guid}/payment-confirmation-requests/pending")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PaymentConfirmationRequestDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PaymentConfirmationRequestDto>>>> GetPendingRequests(
        Guid orgId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting pending payment confirmation requests for organization {OrgId}", orgId);

        var query = new GetPendingPaymentConfirmationRequestsQuery { OrgId = orgId };
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Failed to retrieve pending requests"));
        }

        return Ok(ApiResponse<IEnumerable<PaymentConfirmationRequestDto>>.Ok(
            result.Requests,
            "Pending payment confirmation requests retrieved successfully"));
    }

    /// <summary>
    /// Gets all payment confirmation requests for a specific invoice.
    /// </summary>
    [HttpGet("api/invoices/{invoiceId:guid}/payment-confirmation-requests")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PaymentConfirmationRequestDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PaymentConfirmationRequestDto>>>> GetInvoiceRequests(
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting payment confirmation requests for invoice {InvoiceId}", invoiceId);

        var query = new GetInvoicePaymentConfirmationRequestsQuery { InvoiceId = invoiceId };
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Failed to retrieve requests"));
        }

        return Ok(ApiResponse<IEnumerable<PaymentConfirmationRequestDto>>.Ok(
            result.Requests,
            "Payment confirmation requests retrieved successfully"));
    }

    /// <summary>
    /// Confirms a payment confirmation request and creates a payment record.
    /// Only owners, administrators, and managers can confirm requests.
    /// </summary>
    [HttpPost("api/payment-confirmation-requests/{requestId:guid}/confirm")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<Guid>>> ConfirmRequest(
        Guid requestId,
        [FromBody] ConfirmPaymentRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Confirming payment confirmation request {RequestId}", requestId);

        var command = new ConfirmPaymentRequestCommand
        {
            RequestId = requestId,
            ReviewResponse = request.ReviewResponse
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Failed to confirm payment request"));
        }

        return Ok(ApiResponse<Guid>.Ok(
            result.PaymentId!.Value,
            "Payment confirmed successfully. Invoice updated."));
    }

    /// <summary>
    /// Rejects a payment confirmation request.
    /// Only owners, administrators, and managers can reject requests.
    /// </summary>
    [HttpPost("api/payment-confirmation-requests/{requestId:guid}/reject")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> RejectRequest(
        Guid requestId,
        [FromBody] RejectPaymentRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rejecting payment confirmation request {RequestId}", requestId);

        var command = new RejectPaymentRequestCommand
        {
            RequestId = requestId,
            ReviewResponse = request.ReviewResponse
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Failed to reject payment request"));
        }

        return Ok(ApiResponse<object>.Ok(
            null,
            "Payment request rejected successfully."));
    }
}
