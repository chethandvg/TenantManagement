using TentMan.Application.TenantManagement.Leases.Commands.CreateLease;
using TentMan.Application.TenantManagement.Leases.Commands.AddLeaseParty;
using TentMan.Application.TenantManagement.Leases.Commands.AddLeaseTerm;
using TentMan.Application.TenantManagement.Leases.Commands.ActivateLease;
using TentMan.Application.TenantManagement.Leases.Queries;
using TentMan.Contracts.Common;
using TentMan.Contracts.Leases;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers.TenantManagement;

/// <summary>
/// API endpoints for managing leases.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class LeasesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<LeasesController> _logger;

    public LeasesController(IMediator mediator, ILogger<LeasesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new lease (draft).
    /// </summary>
    [HttpPost("api/v{version:apiVersion}/organizations/{orgId}/leases")]
    [ProducesResponseType(typeof(ApiResponse<LeaseListDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LeaseListDto>>> CreateLease(
        Guid orgId,
        CreateLeaseRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating lease for unit {UnitId} in organization {OrgId}", request.UnitId, orgId);

        var command = new CreateLeaseCommand(
            orgId,
            request.UnitId,
            request.LeaseNumber,
            request.StartDate,
            request.EndDate,
            request.RentDueDay,
            request.GraceDays,
            request.NoticePeriodDays,
            request.LateFeeType,
            request.LateFeeValue,
            request.PaymentMethodNote,
            request.TermsText,
            request.IsAutoRenew);

        try
        {
            var lease = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(
                nameof(GetLease),
                new { leaseId = lease.Id },
                ApiResponse<LeaseListDto>.Ok(lease, "Lease created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Gets a lease by ID.
    /// </summary>
    [HttpGet("api/leases/{leaseId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LeaseDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LeaseDetailDto>>> GetLease(
        Guid leaseId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting lease {LeaseId}", leaseId);

        var query = new GetLeaseByIdQuery(leaseId);
        var lease = await _mediator.Send(query, cancellationToken);

        if (lease == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Lease {leaseId} not found"));
        }

        return Ok(ApiResponse<LeaseDetailDto>.Ok(lease, "Lease retrieved successfully"));
    }

    /// <summary>
    /// Gets lease history for a unit.
    /// </summary>
    [HttpGet("api/units/{unitId:guid}/leases")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<LeaseListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<LeaseListDto>>>> GetLeasesByUnit(
        Guid unitId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting leases for unit {UnitId}", unitId);

        var query = new GetLeasesByUnitQuery(unitId);
        var leases = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<IEnumerable<LeaseListDto>>.Ok(leases, "Leases retrieved successfully"));
    }

    /// <summary>
    /// Adds a party (tenant/occupant) to a lease.
    /// </summary>
    [HttpPost("api/leases/{leaseId:guid}/parties")]
    [ProducesResponseType(typeof(ApiResponse<LeaseDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LeaseDetailDto>>> AddLeaseParty(
        Guid leaseId,
        AddLeasePartyRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding party {TenantId} to lease {LeaseId}", request.TenantId, leaseId);

        var command = new AddLeasePartyCommand(
            leaseId,
            request.TenantId,
            request.Role,
            request.IsResponsibleForPayment,
            request.MoveInDate);

        try
        {
            var lease = await _mediator.Send(command, cancellationToken);
            return Created($"/api/leases/{leaseId}", ApiResponse<LeaseDetailDto>.Ok(lease, "Party added to lease successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Adds a financial term to a lease.
    /// </summary>
    [HttpPost("api/leases/{leaseId:guid}/terms")]
    [ProducesResponseType(typeof(ApiResponse<LeaseDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LeaseDetailDto>>> AddLeaseTerm(
        Guid leaseId,
        AddLeaseTermRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding term to lease {LeaseId} effective from {EffectiveFrom}", leaseId, request.EffectiveFrom);

        var command = new AddLeaseTermCommand(
            leaseId,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.MonthlyRent,
            request.SecurityDeposit,
            request.MaintenanceCharge,
            request.OtherFixedCharge,
            request.EscalationType,
            request.EscalationValue,
            request.EscalationEveryMonths,
            request.Notes);

        try
        {
            var lease = await _mediator.Send(command, cancellationToken);
            return Created($"/api/leases/{leaseId}", ApiResponse<LeaseDetailDto>.Ok(lease, "Term added to lease successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Activates a draft lease.
    /// </summary>
    [HttpPost("api/leases/{leaseId:guid}/activate")]
    [ProducesResponseType(typeof(ApiResponse<LeaseDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LeaseDetailDto>>> ActivateLease(
        Guid leaseId,
        [FromBody] ActivateLeaseRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activating lease {LeaseId}", leaseId);

        var command = new ActivateLeaseCommand(leaseId, request.RowVersion);

        try
        {
            var lease = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<LeaseDetailDto>.Ok(lease, "Lease activated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
