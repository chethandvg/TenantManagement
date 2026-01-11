using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Billing;
using TentMan.Contracts.Common;
using TentMan.Domain.Entities;
using TentMan.Shared.Constants.Authorization;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Api.Controllers.Billing;

/// <summary>
/// API endpoints for managing lease recurring charges.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class RecurringChargesController : ControllerBase
{
    private readonly ILeaseRecurringChargeRepository _recurringChargeRepository;
    private readonly ILeaseRepository _leaseRepository;
    private readonly IChargeTypeRepository _chargeTypeRepository;
    private readonly ILogger<RecurringChargesController> _logger;

    public RecurringChargesController(
        ILeaseRecurringChargeRepository recurringChargeRepository,
        ILeaseRepository leaseRepository,
        IChargeTypeRepository chargeTypeRepository,
        ILogger<RecurringChargesController> logger)
    {
        _recurringChargeRepository = recurringChargeRepository;
        _leaseRepository = leaseRepository;
        _chargeTypeRepository = chargeTypeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new recurring charge for a lease.
    /// Only admins and managers can create recurring charges.
    /// </summary>
    [HttpPost("api/leases/{leaseId:guid}/recurring-charges")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<LeaseRecurringChargeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<LeaseRecurringChargeDto>>> CreateRecurringCharge(
        Guid leaseId,
        CreateRecurringChargeRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating recurring charge for lease {LeaseId}", leaseId);

        // Verify lease exists
        if (!await _leaseRepository.ExistsAsync(leaseId, cancellationToken))
        {
            return NotFound(ApiResponse<object>.Fail($"Lease {leaseId} not found"));
        }

        // Verify charge type exists
        var chargeType = await _chargeTypeRepository.GetByIdAsync(request.ChargeTypeId, cancellationToken);
        if (chargeType == null)
        {
            return BadRequest(ApiResponse<object>.Fail($"Charge type {request.ChargeTypeId} not found"));
        }

        // Validate end date if provided
        if (request.EndDate.HasValue && request.EndDate.Value < request.StartDate)
        {
            return BadRequest(ApiResponse<object>.Fail("End date must be after or equal to start date"));
        }

        var charge = new LeaseRecurringCharge
        {
            LeaseId = leaseId,
            ChargeTypeId = request.ChargeTypeId,
            Description = request.Description,
            Amount = request.Amount,
            Frequency = request.Frequency,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = request.IsActive,
            Notes = request.Notes
        };

        await _recurringChargeRepository.AddAsync(charge, cancellationToken);

        var dto = new LeaseRecurringChargeDto
        {
            Id = charge.Id,
            LeaseId = charge.LeaseId,
            ChargeTypeId = charge.ChargeTypeId,
            ChargeTypeName = chargeType.Name,
            Description = charge.Description,
            Amount = charge.Amount,
            Frequency = charge.Frequency,
            StartDate = charge.StartDate,
            EndDate = charge.EndDate,
            IsActive = charge.IsActive,
            Notes = charge.Notes,
            RowVersion = charge.RowVersion
        };

        return CreatedAtAction(
            nameof(GetRecurringCharges),
            new { leaseId },
            ApiResponse<LeaseRecurringChargeDto>.Ok(dto, "Recurring charge created successfully"));
    }

    /// <summary>
    /// Gets all recurring charges for a lease.
    /// </summary>
    [HttpGet("api/leases/{leaseId:guid}/recurring-charges")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<LeaseRecurringChargeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IEnumerable<LeaseRecurringChargeDto>>>> GetRecurringCharges(
        Guid leaseId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting recurring charges for lease {LeaseId}", leaseId);

        // Verify lease exists
        if (!await _leaseRepository.ExistsAsync(leaseId, cancellationToken))
        {
            return NotFound(ApiResponse<object>.Fail($"Lease {leaseId} not found"));
        }

        var charges = await _recurringChargeRepository.GetByLeaseIdAsync(leaseId, cancellationToken);

        // Get all unique charge type IDs to avoid N+1 queries
        var chargeTypeIds = charges.Select(c => c.ChargeTypeId).Distinct().ToList();
        var chargeTypesDict = new Dictionary<Guid, string>();
        
        foreach (var chargeTypeId in chargeTypeIds)
        {
            var chargeType = await _chargeTypeRepository.GetByIdAsync(chargeTypeId, cancellationToken);
            if (chargeType != null)
            {
                chargeTypesDict[chargeTypeId] = chargeType.Name;
            }
        }

        var dtos = new List<LeaseRecurringChargeDto>();
        foreach (var charge in charges)
        {
            dtos.Add(new LeaseRecurringChargeDto
            {
                Id = charge.Id,
                LeaseId = charge.LeaseId,
                ChargeTypeId = charge.ChargeTypeId,
                ChargeTypeName = chargeTypesDict.GetValueOrDefault(charge.ChargeTypeId, "Unknown"),
                Description = charge.Description,
                Amount = charge.Amount,
                Frequency = charge.Frequency,
                StartDate = charge.StartDate,
                EndDate = charge.EndDate,
                IsActive = charge.IsActive,
                Notes = charge.Notes,
                RowVersion = charge.RowVersion
            });
        }

        return Ok(ApiResponse<IEnumerable<LeaseRecurringChargeDto>>.Ok(dtos, "Recurring charges retrieved successfully"));
    }

    /// <summary>
    /// Updates a recurring charge.
    /// Only admins and managers can update recurring charges.
    /// </summary>
    [HttpPut("api/recurring-charges/{id:guid}")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<LeaseRecurringChargeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<LeaseRecurringChargeDto>>> UpdateRecurringCharge(
        Guid id,
        UpdateRecurringChargeRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating recurring charge {ChargeId}", id);

        var charge = await _recurringChargeRepository.GetByIdAsync(id, cancellationToken);
        if (charge == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Recurring charge {id} not found"));
        }

        // Validate end date if provided
        if (request.EndDate.HasValue && request.EndDate.Value < request.StartDate)
        {
            return BadRequest(ApiResponse<object>.Fail("End date must be after or equal to start date"));
        }

        charge.Description = request.Description;
        charge.Amount = request.Amount;
        charge.Frequency = request.Frequency;
        charge.StartDate = request.StartDate;
        charge.EndDate = request.EndDate;
        charge.IsActive = request.IsActive;
        charge.Notes = request.Notes;
        charge.ModifiedAtUtc = DateTime.UtcNow;

        try
        {
            await _recurringChargeRepository.UpdateAsync(charge, request.RowVersion, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return BadRequest(ApiResponse<object>.Fail("The recurring charge was modified by another process. Please retry."));
        }

        var chargeType = await _chargeTypeRepository.GetByIdAsync(charge.ChargeTypeId, cancellationToken);

        var dto = new LeaseRecurringChargeDto
        {
            Id = charge.Id,
            LeaseId = charge.LeaseId,
            ChargeTypeId = charge.ChargeTypeId,
            ChargeTypeName = chargeType?.Name ?? "Unknown",
            Description = charge.Description,
            Amount = charge.Amount,
            Frequency = charge.Frequency,
            StartDate = charge.StartDate,
            EndDate = charge.EndDate,
            IsActive = charge.IsActive,
            Notes = charge.Notes,
            RowVersion = charge.RowVersion
        };

        return Ok(ApiResponse<LeaseRecurringChargeDto>.Ok(dto, "Recurring charge updated successfully"));
    }

    /// <summary>
    /// Deletes (soft deletes) a recurring charge.
    /// Only admins and managers can delete recurring charges.
    /// </summary>
    [HttpDelete("api/recurring-charges/{id:guid}")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteRecurringCharge(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting recurring charge {ChargeId}", id);

        var charge = await _recurringChargeRepository.GetByIdAsync(id, cancellationToken);
        if (charge == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Recurring charge {id} not found"));
        }

        // Soft delete by marking as inactive
        charge.IsActive = false;
        charge.IsDeleted = true;
        charge.ModifiedAtUtc = DateTime.UtcNow;

        try
        {
            await _recurringChargeRepository.UpdateAsync(charge, charge.RowVersion, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return BadRequest(ApiResponse<object>.Fail("The recurring charge was modified by another process. Please retry."));
        }

        return Ok(ApiResponse<string>.Ok("Deleted", "Recurring charge deleted successfully"));
    }
}
