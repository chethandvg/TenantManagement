using TentMan.Application.Billing.Commands.RecordPayment;
using TentMan.Application.Billing.Queries.GetPayments;
using TentMan.Contracts.Common;
using TentMan.Contracts.Payments;
using TentMan.Shared.Constants.Authorization;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers.Billing;

/// <summary>
/// API endpoints for managing invoice payments.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IMediator mediator,
        ILogger<PaymentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Records a cash payment for an invoice.
    /// Cash payments are immediately marked as completed.
    /// Only owners, administrators, and managers can record cash payments.
    /// </summary>
    [HttpPost("api/invoices/{invoiceId:guid}/payments/cash")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<Guid>>> RecordCashPayment(
        Guid invoiceId,
        RecordCashPaymentRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Recording cash payment for invoice {InvoiceId}", invoiceId);

        var command = new RecordCashPaymentCommand
        {
            InvoiceId = invoiceId,
            Amount = request.Amount,
            PaymentDate = request.PaymentDate,
            PayerName = request.PayerName,
            ReceiptNumber = request.ReceiptNumber,
            Notes = request.Notes
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Failed to record cash payment"));
        }

        return Ok(ApiResponse<Guid>.Ok(
            result.PaymentId!.Value, 
            $"Cash payment recorded successfully. Invoice balance: {result.InvoiceBalanceAmount:C}"));
    }

    /// <summary>
    /// Records an online payment for an invoice.
    /// This is a stub for future payment gateway integration.
    /// Only owners, administrators, and managers can record online payments.
    /// </summary>
    [HttpPost("api/invoices/{invoiceId:guid}/payments/online")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<Guid>>> RecordOnlinePayment(
        Guid invoiceId,
        RecordOnlinePaymentRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Recording online payment for invoice {InvoiceId}", invoiceId);

        var command = new RecordOnlinePaymentCommand
        {
            InvoiceId = invoiceId,
            PaymentMode = request.PaymentMode,
            Amount = request.Amount,
            PaymentDate = request.PaymentDate,
            TransactionReference = request.TransactionReference,
            PayerName = request.PayerName,
            Notes = request.Notes,
            PaymentMetadata = request.PaymentMetadata
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Failed to record online payment"));
        }

        return Ok(ApiResponse<Guid>.Ok(
            result.PaymentId!.Value, 
            $"Online payment recorded successfully. Invoice balance: {result.InvoiceBalanceAmount:C}"));
    }

    /// <summary>
    /// Gets all payments for a specific invoice.
    /// </summary>
    [HttpGet("api/invoices/{invoiceId:guid}/payments")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PaymentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PaymentDto>>>> GetInvoicePayments(
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting payments for invoice {InvoiceId}", invoiceId);

        var query = new GetInvoicePaymentsQuery { InvoiceId = invoiceId };
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(ApiResponse<object>.Fail(result.ErrorMessage ?? "Invoice not found"));
        }

        return Ok(ApiResponse<IEnumerable<PaymentDto>>.Ok(result.Payments, "Payments retrieved successfully"));
    }
}
