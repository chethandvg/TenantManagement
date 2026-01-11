using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Billing;
using TentMan.Contracts.Common;
using TentMan.Contracts.Enums;
using TentMan.Domain.Entities;
using TentMan.Shared.Constants.Authorization;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers.Billing;

/// <summary>
/// API endpoints for managing utility statements.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class UtilityStatementsController : ControllerBase
{
    private readonly IUtilityStatementRepository _utilityStatementRepository;
    private readonly ILeaseRepository _leaseRepository;
    private readonly IUnitRepository _unitRepository;
    private readonly ILogger<UtilityStatementsController> _logger;

    public UtilityStatementsController(
        IUtilityStatementRepository utilityStatementRepository,
        ILeaseRepository leaseRepository,
        IUnitRepository unitRepository,
        ILogger<UtilityStatementsController> logger)
    {
        _utilityStatementRepository = utilityStatementRepository;
        _leaseRepository = leaseRepository;
        _unitRepository = unitRepository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new utility statement.
    /// Only admins and managers can create utility statements.
    /// </summary>
    [HttpPost("api/units/{unitId:guid}/utilities")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<UtilityStatementDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<UtilityStatementDto>>> CreateUtilityStatement(
        Guid unitId,
        CreateUtilityStatementRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating utility statement for unit {UnitId}", unitId);

        // Verify unit exists
        if (!await _unitRepository.ExistsAsync(unitId, cancellationToken))
        {
            return NotFound(ApiResponse<object>.Fail($"Unit {unitId} not found"));
        }

        // Verify lease exists
        if (!await _leaseRepository.ExistsAsync(request.LeaseId, cancellationToken))
        {
            return NotFound(ApiResponse<object>.Fail($"Lease {request.LeaseId} not found"));
        }

        // Validate billing period
        if (request.BillingPeriodEnd < request.BillingPeriodStart)
        {
            return BadRequest(ApiResponse<object>.Fail("Billing period end must be after or equal to start"));
        }

        // Validate meter-based fields
        if (request.IsMeterBased)
        {
            if (!request.UtilityRatePlanId.HasValue)
            {
                return BadRequest(ApiResponse<object>.Fail("Utility rate plan is required for meter-based statements"));
            }
            if (!request.CurrentReading.HasValue || !request.PreviousReading.HasValue)
            {
                return BadRequest(ApiResponse<object>.Fail("Current and previous readings are required for meter-based statements"));
            }
            if (request.CurrentReading.Value < request.PreviousReading.Value)
            {
                return BadRequest(ApiResponse<object>.Fail("Current reading must be greater than or equal to previous reading"));
            }
        }
        else if (!request.DirectBillAmount.HasValue)
        {
            return BadRequest(ApiResponse<object>.Fail("Direct bill amount is required for non-meter-based statements"));
        }

        var statement = new UtilityStatement
        {
            LeaseId = request.LeaseId,
            UtilityType = request.UtilityType,
            BillingPeriodStart = request.BillingPeriodStart,
            BillingPeriodEnd = request.BillingPeriodEnd,
            IsMeterBased = request.IsMeterBased,
            UtilityRatePlanId = request.UtilityRatePlanId,
            PreviousReading = request.PreviousReading,
            CurrentReading = request.CurrentReading,
            DirectBillAmount = request.DirectBillAmount,
            Notes = request.Notes
        };

        // Calculate amounts using helper method
        CalculateUtilityAmounts(statement);

        await _utilityStatementRepository.AddAsync(statement, cancellationToken);

        var dto = MapToDto(statement);

        return CreatedAtAction(
            nameof(GetUtilityStatement),
            new { id = statement.Id },
            ApiResponse<UtilityStatementDto>.Ok(dto, "Utility statement created successfully"));
    }

    /// <summary>
    /// Gets all utility statements for a unit with optional filters.
    /// </summary>
    [HttpGet("api/units/{unitId:guid}/utilities")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UtilityStatementDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UtilityStatementDto>>>> GetUtilityStatements(
        Guid unitId,
        [FromQuery] UtilityType? utilityType,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting utility statements for unit {UnitId}", unitId);

        // Verify unit exists
        if (!await _unitRepository.ExistsAsync(unitId, cancellationToken))
        {
            return NotFound(ApiResponse<object>.Fail($"Unit {unitId} not found"));
        }

        var statements = await _utilityStatementRepository.GetByUnitIdAsync(unitId, utilityType, cancellationToken);

        var dtos = statements.Select(MapToDto).ToList();

        return Ok(ApiResponse<IEnumerable<UtilityStatementDto>>.Ok(dtos, "Utility statements retrieved successfully"));
    }

    /// <summary>
    /// Gets a specific utility statement by ID.
    /// </summary>
    [HttpGet("api/utilities/{id:guid}")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<UtilityStatementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<UtilityStatementDto>>> GetUtilityStatement(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting utility statement {StatementId}", id);

        var statement = await _utilityStatementRepository.GetByIdAsync(id, cancellationToken);

        if (statement == null || statement.IsDeleted)
        {
            return NotFound(ApiResponse<object>.Fail($"Utility statement {id} not found"));
        }

        var dto = MapToDto(statement);

        return Ok(ApiResponse<UtilityStatementDto>.Ok(dto, "Utility statement retrieved successfully"));
    }

    /// <summary>
    /// Updates a utility statement.
    /// Only admins and managers can update utility statements.
    /// </summary>
    [HttpPut("api/utilities/{id:guid}")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<UtilityStatementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<UtilityStatementDto>>> UpdateUtilityStatement(
        Guid id,
        UpdateUtilityStatementRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating utility statement {StatementId}", id);

        var statement = await _utilityStatementRepository.GetByIdAsync(id, cancellationToken);

        if (statement == null || statement.IsDeleted)
        {
            return NotFound(ApiResponse<object>.Fail($"Utility statement {id} not found"));
        }

        // Check if already invoiced
        if (statement.InvoiceLineId.HasValue)
        {
            return BadRequest(ApiResponse<object>.Fail("Cannot update a utility statement that has been invoiced"));
        }

        // Update fields
        if (request.UtilityRatePlanId.HasValue)
            statement.UtilityRatePlanId = request.UtilityRatePlanId;
        if (request.PreviousReading.HasValue)
            statement.PreviousReading = request.PreviousReading;
        if (request.CurrentReading.HasValue)
            statement.CurrentReading = request.CurrentReading;
        if (request.DirectBillAmount.HasValue)
            statement.DirectBillAmount = request.DirectBillAmount;
        if (request.Notes != null)
            statement.Notes = request.Notes;

        statement.ModifiedAtUtc = DateTime.UtcNow;

        // Recalculate amounts
        if (statement.IsMeterBased && statement.CurrentReading.HasValue && statement.PreviousReading.HasValue)
        {
            statement.UnitsConsumed = statement.CurrentReading.Value - statement.PreviousReading.Value;
            statement.CalculatedAmount = statement.UnitsConsumed.Value * 10; // Placeholder
            statement.TotalAmount = statement.CalculatedAmount.Value;
        }
        else
        {
            statement.TotalAmount = statement.DirectBillAmount ?? 0;
        }

        try
        {
            await _utilityStatementRepository.UpdateAsync(statement, statement.RowVersion, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }

        var dto = MapToDto(statement);

        return Ok(ApiResponse<UtilityStatementDto>.Ok(dto, "Utility statement updated successfully"));
    }

    /// <summary>
    /// Finalizes a utility statement (marks it ready for invoicing).
    /// Only admins and managers can finalize utility statements.
    /// </summary>
    [HttpPut("api/utilities/{id:guid}/finalize")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<UtilityStatementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<UtilityStatementDto>>> FinalizeUtilityStatement(
        Guid id,
        FinalizeUtilityStatementRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Finalizing utility statement {StatementId}", id);

        var statement = await _utilityStatementRepository.GetByIdAsync(id, cancellationToken);

        if (statement == null || statement.IsDeleted)
        {
            return NotFound(ApiResponse<object>.Fail($"Utility statement {id} not found"));
        }

        // Check if already invoiced
        if (statement.InvoiceLineId.HasValue)
        {
            return BadRequest(ApiResponse<object>.Fail("Utility statement has already been invoiced"));
        }

        // Validate that required fields are set
        if (statement.TotalAmount <= 0)
        {
            return BadRequest(ApiResponse<object>.Fail("Cannot finalize utility statement with zero or negative amount"));
        }

        statement.ModifiedAtUtc = DateTime.UtcNow;

        try
        {
            await _utilityStatementRepository.UpdateAsync(statement, request.RowVersion, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }

        var dto = MapToDto(statement);

        return Ok(ApiResponse<UtilityStatementDto>.Ok(dto, "Utility statement finalized successfully"));
    }

    /// <summary>
    /// Helper method to calculate utility amounts based on statement type.
    /// TODO: Integrate with proper rate plan calculation service when available.
    /// </summary>
    private static void CalculateUtilityAmounts(UtilityStatement statement)
    {
        if (statement.IsMeterBased && statement.CurrentReading.HasValue && statement.PreviousReading.HasValue)
        {
            statement.UnitsConsumed = statement.CurrentReading.Value - statement.PreviousReading.Value;
            // TODO: Replace with actual rate plan calculation service
            // This is a placeholder - should use utility rate plan to calculate tiered pricing
            const decimal placeholderRatePerUnit = 10m;
            statement.CalculatedAmount = statement.UnitsConsumed.Value * placeholderRatePerUnit;
            statement.TotalAmount = statement.CalculatedAmount.Value;
        }
        else
        {
            statement.TotalAmount = statement.DirectBillAmount ?? 0;
        }
    }

    private static UtilityStatementDto MapToDto(UtilityStatement statement)
    {
        return new UtilityStatementDto
        {
            Id = statement.Id,
            LeaseId = statement.LeaseId,
            UtilityType = statement.UtilityType,
            BillingPeriodStart = statement.BillingPeriodStart,
            BillingPeriodEnd = statement.BillingPeriodEnd,
            IsMeterBased = statement.IsMeterBased,
            UtilityRatePlanId = statement.UtilityRatePlanId,
            PreviousReading = statement.PreviousReading,
            CurrentReading = statement.CurrentReading,
            UnitsConsumed = statement.UnitsConsumed,
            CalculatedAmount = statement.CalculatedAmount,
            DirectBillAmount = statement.DirectBillAmount,
            TotalAmount = statement.TotalAmount,
            Notes = statement.Notes,
            InvoiceLineId = statement.InvoiceLineId,
            RowVersion = statement.RowVersion
        };
    }
}
