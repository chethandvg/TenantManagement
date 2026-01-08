using TentMan.Application.PropertyManagement.Units.Commands.CreateUnit;
using TentMan.Application.PropertyManagement.Units.Queries.GetUnits;
using TentMan.Contracts.Common;
using TentMan.Contracts.Units;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers.PropertyManagement;

/// <summary>
/// API endpoints for managing units within buildings.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/buildings/{buildingId}/[controller]")]
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
    /// Creates a new unit in a building.
    /// </summary>
    [HttpPost]
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
    /// Gets all units for a building.
    /// </summary>
    [HttpGet]
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
}
