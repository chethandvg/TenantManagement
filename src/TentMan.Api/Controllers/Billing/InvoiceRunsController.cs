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
/// API endpoints for managing invoice runs (batch invoice generation).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class InvoiceRunsController : ControllerBase
{
    private readonly IInvoiceRunRepository _invoiceRunRepository;
    private readonly IInvoiceRunService _invoiceRunService;
    private readonly ILogger<InvoiceRunsController> _logger;

    public InvoiceRunsController(
        IInvoiceRunRepository invoiceRunRepository,
        IInvoiceRunService invoiceRunService,
        ILogger<InvoiceRunsController> logger)
    {
        _invoiceRunRepository = invoiceRunRepository;
        _invoiceRunService = invoiceRunService;
        _logger = logger;
    }

    /// <summary>
    /// Creates and executes a monthly rent invoice run for all active leases.
    /// Only admins and managers can create invoice runs.
    /// </summary>
    [HttpPost("api/invoice-runs/monthly")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceRunDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<InvoiceRunDto>>> CreateMonthlyInvoiceRun(
        [FromQuery] Guid orgId,
        [FromQuery] DateOnly periodStart,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating monthly invoice run for organization {OrgId}, period {PeriodStart}", orgId, periodStart);

        // Calculate period end (last day of the month)
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);

        var result = await _invoiceRunService.ExecuteMonthlyRentRunAsync(
            orgId,
            periodStart,
            periodEnd,
            ProrationMethod.ActualDaysInMonth,
            cancellationToken);

        if (!result.IsSuccess || result.InvoiceRun == null)
        {
            return BadRequest(ApiResponse<object>.Fail($"Failed to create invoice run: {string.Join(", ", result.ErrorMessages)}"));
        }

        var dto = MapToDto(result.InvoiceRun);

        return CreatedAtAction(
            nameof(GetInvoiceRun),
            new { id = result.InvoiceRun.Id },
            ApiResponse<InvoiceRunDto>.Ok(dto, "Monthly invoice run completed successfully"));
    }

    /// <summary>
    /// Creates and executes a utility billing run for leases with pending utility statements.
    /// Only admins and managers can create invoice runs.
    /// </summary>
    [HttpPost("api/invoice-runs/utilities")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceRunDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<InvoiceRunDto>>> CreateUtilityInvoiceRun(
        [FromQuery] Guid orgId,
        [FromQuery] DateOnly periodStart,
        [FromQuery] DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating utility invoice run for organization {OrgId}, period {PeriodStart} to {PeriodEnd}", 
            orgId, periodStart, periodEnd);

        if (periodEnd < periodStart)
        {
            return BadRequest(ApiResponse<object>.Fail("Period end must be after or equal to period start"));
        }

        var result = await _invoiceRunService.ExecuteUtilityRunAsync(
            orgId,
            periodStart,
            periodEnd,
            cancellationToken);

        if (!result.IsSuccess || result.InvoiceRun == null)
        {
            return BadRequest(ApiResponse<object>.Fail($"Failed to create invoice run: {string.Join(", ", result.ErrorMessages)}"));
        }

        var dto = MapToDto(result.InvoiceRun);

        return CreatedAtAction(
            nameof(GetInvoiceRun),
            new { id = result.InvoiceRun.Id },
            ApiResponse<InvoiceRunDto>.Ok(dto, "Utility invoice run completed successfully"));
    }

    /// <summary>
    /// Gets all invoice runs for an organization with optional status filter.
    /// </summary>
    [HttpGet("api/invoice-runs")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<InvoiceRunDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IEnumerable<InvoiceRunDto>>>> GetInvoiceRuns(
        [FromQuery] Guid orgId,
        [FromQuery] InvoiceRunStatus? status,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting invoice runs for organization {OrgId}, status {Status}", orgId, status);

        var invoiceRuns = await _invoiceRunRepository.GetByOrgIdAsync(orgId, status, cancellationToken);

        var dtos = invoiceRuns.Select(MapToDto).ToList();

        return Ok(ApiResponse<IEnumerable<InvoiceRunDto>>.Ok(dtos, "Invoice runs retrieved successfully"));
    }

    /// <summary>
    /// Gets a specific invoice run by ID with all items.
    /// </summary>
    [HttpGet("api/invoice-runs/{id:guid}")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceRunDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<InvoiceRunDto>>> GetInvoiceRun(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting invoice run {InvoiceRunId}", id);

        var invoiceRun = await _invoiceRunRepository.GetByIdWithItemsAsync(id, cancellationToken);
        if (invoiceRun == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Invoice run {id} not found"));
        }

        var dto = MapToDto(invoiceRun);

        return Ok(ApiResponse<InvoiceRunDto>.Ok(dto, "Invoice run retrieved successfully"));
    }

    private static InvoiceRunDto MapToDto(Domain.Entities.InvoiceRun invoiceRun)
    {
        var itemDtos = invoiceRun.Items.Select(item => new InvoiceRunItemDto
        {
            Id = item.Id,
            InvoiceRunId = item.InvoiceRunId,
            LeaseId = item.LeaseId,
            LeaseNumber = item.Lease?.LeaseNumber ?? "Unknown",
            InvoiceId = item.InvoiceId,
            InvoiceNumber = item.Invoice?.InvoiceNumber,
            IsSuccess = item.IsSuccess,
            ErrorMessage = item.ErrorMessage
        }).ToList();

        return new InvoiceRunDto
        {
            Id = invoiceRun.Id,
            OrgId = invoiceRun.OrgId,
            RunNumber = invoiceRun.RunNumber,
            BillingPeriodStart = invoiceRun.BillingPeriodStart,
            BillingPeriodEnd = invoiceRun.BillingPeriodEnd,
            Status = invoiceRun.Status,
            StartedAtUtc = invoiceRun.StartedAtUtc,
            CompletedAtUtc = invoiceRun.CompletedAtUtc,
            TotalLeases = invoiceRun.TotalLeases,
            SuccessCount = invoiceRun.SuccessCount,
            FailureCount = invoiceRun.FailureCount,
            ErrorMessage = invoiceRun.ErrorMessage,
            Notes = invoiceRun.Notes,
            Items = itemDtos,
            RowVersion = invoiceRun.RowVersion
        };
    }
}
