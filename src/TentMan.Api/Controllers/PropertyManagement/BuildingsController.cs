using TentMan.Application.PropertyManagement.Buildings.Commands.CreateBuilding;
using TentMan.Application.PropertyManagement.Buildings.Commands.UpdateBuilding;
using TentMan.Application.PropertyManagement.Buildings.Commands.SetBuildingAddress;
using TentMan.Application.PropertyManagement.Buildings.Commands.SetBuildingOwnership;
using TentMan.Application.PropertyManagement.Buildings.Commands.AddBuildingFile;
using TentMan.Application.PropertyManagement.Buildings.Queries.GetBuildings;
using TentMan.Application.PropertyManagement.Buildings.Queries.GetBuilding;
using TentMan.Application.PropertyManagement.Units.Commands.CreateUnit;
using TentMan.Application.PropertyManagement.Units.Commands.BulkCreateUnits;
using TentMan.Contracts.Buildings;
using TentMan.Contracts.Units;
using TentMan.Contracts.Common;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers.PropertyManagement;

/// <summary>
/// API endpoints for managing buildings in the property management system.
/// Routes:
/// - New API routes: /api/buildings/* (simple, non-versioned)
/// - Legacy routes: /api/v{version}/organizations/{orgId}/buildings (versioned, organization-scoped)
/// Both routing patterns are maintained for backward compatibility.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class BuildingsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BuildingsController> _logger;

    public BuildingsController(IMediator mediator, ILogger<BuildingsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all buildings for an organization.
    /// </summary>
    [HttpGet("api/buildings")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<BuildingListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<BuildingListDto>>>> GetAllBuildings(
        [FromQuery] Guid orgId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting buildings for organization {OrgId}", orgId);

        var query = new GetBuildingsQuery(orgId);
        var buildings = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<IEnumerable<BuildingListDto>>.Ok(buildings, "Buildings retrieved successfully"));
    }

    /// <summary>
    /// Creates a new building.
    /// </summary>
    [HttpPost("api/buildings")]
    [ProducesResponseType(typeof(ApiResponse<BuildingDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<BuildingDetailDto>>> CreateBuilding(
        CreateBuildingRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating building {BuildingCode} for organization {OrgId}", request.BuildingCode, request.OrgId);

        var command = new CreateBuildingCommand(
            request.OrgId,
            request.BuildingCode,
            request.Name,
            request.PropertyType,
            request.TotalFloors,
            request.HasLift,
            request.Notes);

        try
        {
            var building = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(
                nameof(GetBuilding),
                new { id = building.Id },
                ApiResponse<BuildingDetailDto>.Ok(building, "Building created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Gets a specific building by ID.
    /// </summary>
    [HttpGet("api/buildings/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BuildingDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<BuildingDetailDto>>> GetBuilding(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting building {BuildingId}", id);

        var query = new GetBuildingQuery(id);
        var building = await _mediator.Send(query, cancellationToken);

        if (building == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Building {id} not found"));
        }

        return Ok(ApiResponse<BuildingDetailDto>.Ok(building, "Building retrieved successfully"));
    }

    /// <summary>
    /// Updates a building.
    /// </summary>
    [HttpPut("api/buildings/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BuildingDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<BuildingDetailDto>>> UpdateBuilding(
        Guid id,
        UpdateBuildingRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating building {BuildingId}", id);

        var command = new UpdateBuildingCommand(
            id,
            request.Name,
            request.PropertyType,
            request.TotalFloors,
            request.HasLift,
            request.Notes,
            request.RowVersion);

        try
        {
            var building = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<BuildingDetailDto>.Ok(building, "Building updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Sets or updates the address for a building.
    /// </summary>
    [HttpPut("api/buildings/{id:guid}/address")]
    [ProducesResponseType(typeof(ApiResponse<BuildingDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<BuildingDetailDto>>> SetBuildingAddress(
        Guid id,
        SetBuildingAddressRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting address for building {BuildingId}", id);

        var command = new SetBuildingAddressCommand(
            id,
            request.Line1,
            request.Line2,
            request.Locality,
            request.City,
            request.District,
            request.State,
            request.PostalCode,
            request.Landmark,
            request.Latitude,
            request.Longitude);

        try
        {
            var building = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<BuildingDetailDto>.Ok(building, "Building address updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Creates a new unit in a building.
    /// </summary>
    [HttpPost("api/buildings/{id:guid}/units")]
    [ProducesResponseType(typeof(ApiResponse<UnitListDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UnitListDto>>> CreateUnit(
        Guid id,
        CreateUnitRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating unit {UnitNumber} in building {BuildingId}", request.UnitNumber, id);

        var command = new CreateUnitCommand(
            id,
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
            // Return Created with direct URL - no GET endpoint for single unit exists
            return Created($"/api/buildings/{id}/units/{unit.Id}", ApiResponse<UnitListDto>.Ok(unit, "Unit created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Creates multiple units in a building.
    /// </summary>
    [HttpPost("api/buildings/{id:guid}/units/bulk")]
    [ProducesResponseType(typeof(ApiResponse<List<UnitListDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<UnitListDto>>>> BulkCreateUnits(
        Guid id,
        BulkCreateUnitsRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Bulk creating {Count} units in building {BuildingId}", request.Units.Count, id);

        var unitData = request.Units.Select(u => new CreateUnitData(
            u.UnitNumber,
            u.Floor,
            u.UnitType,
            u.AreaSqFt,
            u.Bedrooms,
            u.Bathrooms,
            u.Furnishing,
            u.ParkingSlots)).ToList();

        var command = new BulkCreateUnitsCommand(id, unitData);

        try
        {
            var units = await _mediator.Send(command, cancellationToken);
            // Return Created with direct URL - no GET endpoint for single unit exists
            return Created($"/api/buildings/{id}/units", ApiResponse<List<UnitListDto>>.Ok(units, $"{units.Count} units created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Sets ownership shares for a building.
    /// </summary>
    [HttpPut("api/buildings/{id:guid}/ownership-shares")]
    [ProducesResponseType(typeof(ApiResponse<BuildingDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<BuildingDetailDto>>> SetBuildingOwnership(
        Guid id,
        SetBuildingOwnershipRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting ownership shares for building {BuildingId}", id);

        var command = new SetBuildingOwnershipCommand(id, request.Shares, request.EffectiveFrom);

        try
        {
            var building = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<BuildingDetailDto>.Ok(building, "Building ownership shares updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Adds a file to a building.
    /// </summary>
    [HttpPost("api/buildings/{id:guid}/files")]
    [ProducesResponseType(typeof(ApiResponse<BuildingDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<BuildingDetailDto>>> AddBuildingFile(
        Guid id,
        AddFileRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding file {FileId} to building {BuildingId}", request.FileId, id);

        var command = new AddBuildingFileCommand(id, request.FileId, request.FileTag, request.SortOrder);

        try
        {
            var building = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(
                nameof(GetBuilding),
                new { id },
                ApiResponse<BuildingDetailDto>.Ok(building, "File added to building successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    #region Legacy Organization-scoped routes

    /// <summary>
    /// Creates a new building for an organization (legacy route).
    /// </summary>
    [HttpPost("api/v{version:apiVersion}/organizations/{orgId}/buildings")]
    [ProducesResponseType(typeof(ApiResponse<BuildingDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<BuildingDetailDto>>> CreateBuildingLegacy(
        Guid orgId,
        CreateBuildingRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating building {BuildingCode} for organization {OrgId}", request.BuildingCode, orgId);

        var command = new CreateBuildingCommand(
            orgId,
            request.BuildingCode,
            request.Name,
            request.PropertyType,
            request.TotalFloors,
            request.HasLift,
            request.Notes);

        try
        {
            var building = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(
                nameof(GetBuilding),
                new { id = building.Id },
                ApiResponse<BuildingDetailDto>.Ok(building, "Building created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Gets all buildings for an organization (legacy route).
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/organizations/{orgId}/buildings")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<BuildingListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<BuildingListDto>>>> GetBuildings(
        Guid orgId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting buildings for organization {OrgId}", orgId);

        var query = new GetBuildingsQuery(orgId);
        var buildings = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<IEnumerable<BuildingListDto>>.Ok(buildings, "Buildings retrieved successfully"));
    }

    #endregion
}
