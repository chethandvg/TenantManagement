using TentMan.Application.PropertyManagement.Buildings.Commands.AddBuildingFile;
using TentMan.Application.PropertyManagement.Buildings.Commands.CreateBuilding;
using TentMan.Application.PropertyManagement.Buildings.Commands.SetBuildingAddress;
using TentMan.Application.PropertyManagement.Buildings.Commands.SetBuildingOwnership;
using TentMan.Application.PropertyManagement.Buildings.Commands.UpdateBuilding;
using TentMan.Application.PropertyManagement.Buildings.Queries.GetBuildingById;
using TentMan.Application.PropertyManagement.Buildings.Queries.GetBuildings;
using TentMan.Application.PropertyManagement.Units.Commands.BulkCreateUnits;
using TentMan.Application.PropertyManagement.Units.Commands.CreateUnit;
using TentMan.Contracts.Buildings;
using TentMan.Contracts.Common;
using TentMan.Contracts.Files;
using TentMan.Contracts.Units;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers.REST;

/// <summary>
/// REST API endpoints for managing buildings.
/// </summary>
[ApiController]
[Route("api/[controller]")]
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
    /// Gets all buildings (requires orgId in the request body or from authenticated context).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<BuildingListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<BuildingListDto>>>> GetBuildings(
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
    [HttpPost]
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
                nameof(GetBuildingById),
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
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BuildingDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<BuildingDetailDto>>> GetBuildingById(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting building {BuildingId}", id);

        var query = new GetBuildingByIdQuery(id);
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
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BuildingDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<BuildingDetailDto>>> UpdateBuilding(
        Guid id,
        UpdateBuildingRequest request,
        CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            return BadRequest(ApiResponse<object>.Fail("ID in URL does not match ID in request body"));
        }

        _logger.LogInformation("Updating building {BuildingId}", id);

        var command = new UpdateBuildingCommand(
            request.Id,
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
    [HttpPut("{id}/address")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> SetBuildingAddress(
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
            await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<object>.Ok(null, "Building address set successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Sets ownership shares for a building.
    /// </summary>
    [HttpPut("{id}/ownership-shares")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> SetBuildingOwnership(
        Guid id,
        SetBuildingOwnershipRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting ownership for building {BuildingId}", id);

        var command = new SetBuildingOwnershipCommand(
            id,
            request.Shares,
            request.EffectiveFrom);

        try
        {
            await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<object>.Ok(null, "Building ownership set successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Creates a new unit in a building.
    /// </summary>
    [HttpPost("{id}/units")]
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
            return CreatedAtAction(
                nameof(GetBuildingById),
                new { id },
                ApiResponse<UnitListDto>.Ok(unit, "Unit created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Creates multiple units in a building at once.
    /// </summary>
    [HttpPost("{id}/units/bulk")]
    [ProducesResponseType(typeof(ApiResponse<List<UnitListDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<UnitListDto>>>> BulkCreateUnits(
        Guid id,
        BulkCreateUnitsRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Bulk creating {Count} units in building {BuildingId}", request.Units.Count, id);

        var command = new BulkCreateUnitsCommand(id, request.Units);

        try
        {
            var units = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(
                nameof(GetBuildingById),
                new { id },
                ApiResponse<List<UnitListDto>>.Ok(units, $"{units.Count} units created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Adds a file to a building.
    /// </summary>
    [HttpPost("{id}/files")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> AddBuildingFile(
        Guid id,
        AddBuildingFileRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding file to building {BuildingId}", id);

        var command = new AddBuildingFileCommand(
            id,
            request.FileId,
            request.FileTag,
            request.SortOrder);

        try
        {
            await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<object>.Ok(null, "File added to building successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
