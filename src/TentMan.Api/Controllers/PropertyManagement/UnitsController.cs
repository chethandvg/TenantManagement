using TentMan.Application.PropertyManagement.Units.Commands.CreateUnit;
using TentMan.Application.PropertyManagement.Units.Commands.UpdateUnit;
using TentMan.Application.PropertyManagement.Units.Commands.SetUnitOwnership;
using TentMan.Application.PropertyManagement.Units.Commands.AddUnitFile;
using TentMan.Application.PropertyManagement.Units.Queries.GetUnits;
using TentMan.Contracts.Buildings;
using TentMan.Contracts.Common;
using TentMan.Contracts.Units;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers.PropertyManagement;

/// <summary>
/// API endpoints for managing units within buildings.
/// Routes:
/// - New API routes: /api/units/* (simple, non-versioned)
/// - Legacy routes: /api/v{version}/buildings/{buildingId}/units (versioned, building-scoped)
/// Both routing patterns are maintained for backward compatibility.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class UnitsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UnitsController> _logger;

    public UnitsController(IMediator mediator, ILogger<UnitsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Updates a unit.
    /// </summary>
    [HttpPut("api/units/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UnitDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UnitDetailDto>>> UpdateUnit(
        Guid id,
        UpdateUnitRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating unit {UnitId}", id);

        var command = new UpdateUnitCommand(
            id,
            request.Floor,
            request.UnitType,
            request.AreaSqFt,
            request.Bedrooms,
            request.Bathrooms,
            request.Furnishing,
            request.ParkingSlots,
            request.OccupancyStatus,
            request.RowVersion);

        try
        {
            var unit = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<UnitDetailDto>.Ok(unit, "Unit updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Sets ownership shares for a unit.
    /// </summary>
    [HttpPut("api/units/{id:guid}/ownership-shares")]
    [ProducesResponseType(typeof(ApiResponse<UnitDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UnitDetailDto>>> SetUnitOwnership(
        Guid id,
        SetUnitOwnershipRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting ownership shares for unit {UnitId}", id);

        var command = new SetUnitOwnershipCommand(id, request.Shares, request.EffectiveFrom);

        try
        {
            var unit = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<UnitDetailDto>.Ok(unit, "Unit ownership shares updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Adds a file to a unit.
    /// </summary>
    [HttpPost("api/units/{id:guid}/files")]
    [ProducesResponseType(typeof(ApiResponse<UnitDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UnitDetailDto>>> AddUnitFile(
        Guid id,
        AddFileRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding file {FileId} to unit {UnitId}", request.FileId, id);

        var command = new AddUnitFileCommand(id, request.FileId, request.FileTag, request.SortOrder);

        try
        {
            var unit = await _mediator.Send(command, cancellationToken);
            // Return Created status - there's no dedicated GET endpoint for a single unit, so we return the unit details directly
            return Created($"/api/units/{id}/files", ApiResponse<UnitDetailDto>.Ok(unit, "File added to unit successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    #region Legacy Building-scoped routes

    /// <summary>
    /// Creates a new unit in a building (legacy route).
    /// </summary>
    [HttpPost("api/v{version:apiVersion}/buildings/{buildingId}/units")]
    [ProducesResponseType(typeof(ApiResponse<UnitListDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UnitListDto>>> CreateUnit(
        Guid buildingId,
        CreateUnitRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating unit {UnitNumber} in building {BuildingId}", request.UnitNumber, buildingId);

        var command = new CreateUnitCommand(
            buildingId,
            request.UnitNumber,
            request.Floor,
            request.UnitType,
            request.AreaSqFt,
            request.Bedrooms,
            request.Bathrooms,
            request.Furnishing,
            request.ParkingSlots);

        try
        {
            var unit = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(
                nameof(GetUnits),
                new { buildingId, version = "1.0" },
                ApiResponse<UnitListDto>.Ok(unit, "Unit created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Gets all units for a building (legacy route).
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/buildings/{buildingId}/units")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UnitListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UnitListDto>>>> GetUnits(
        Guid buildingId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting units for building {BuildingId}", buildingId);

        var query = new GetUnitsQuery(buildingId);
        var units = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<IEnumerable<UnitListDto>>.Ok(units, "Units retrieved successfully"));
    }

    #endregion
}
