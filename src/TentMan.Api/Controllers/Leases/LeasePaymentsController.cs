using TentMan.Application.Billing.Queries.GetPayments;
using TentMan.Contracts.Common;
using TentMan.Contracts.Payments;
using TentMan.Shared.Constants.Authorization;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers.Leases;

/// <summary>
/// API endpoints for lease-specific payment operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/leases")]
[Authorize]
public class LeasePaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<LeasePaymentsController> _logger;

    public LeasePaymentsController(
        IMediator mediator,
        ILogger<LeasePaymentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all payments for a specific lease.
    /// Returns payments ordered by payment date (most recent first).
    /// </summary>
    [HttpGet("{leaseId:guid}/payments")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PaymentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PaymentDto>>>> GetLeasePayments(
        Guid leaseId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting payments for lease {LeaseId}", leaseId);

        var query = new GetLeasePaymentsQuery { LeaseId = leaseId };
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(ApiResponse<object>.Fail(result.ErrorMessage ?? "Lease not found or has no payments"));
        }

        return Ok(ApiResponse<IEnumerable<PaymentDto>>.Ok(result.Payments, "Payments retrieved successfully"));
    }
}
