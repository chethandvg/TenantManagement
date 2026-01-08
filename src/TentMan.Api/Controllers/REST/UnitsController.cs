using TentMan.Application.PropertyManagement.Units.Commands.AddUnitFile;
using TentMan.Application.PropertyManagement.Units.Commands.SetUnitOwnership;
using TentMan.Application.PropertyManagement.Units.Commands.UpdateUnit;
using TentMan.Contracts.Common;
using TentMan.Contracts.Files;
using TentMan.Contracts.Units;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers.REST;

/// <summary>
/// REST API endpoints for managing units.
/// </summary>
[ApiController]
[Route("api/[controller]")]
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
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UnitDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UnitDetailDto>>> UpdateUnit(
        Guid id,
        UpdateUnitRequest request,
        CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            return BadRequest(ApiResponse<object>.Fail("ID in URL does not match ID in request body"));
        }

        _logger.LogInformation("Updating unit {UnitId}", id);

        var command = new UpdateUnitCommand(
            request.Id,
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
    [HttpPut("{id}/ownership-shares")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> SetUnitOwnership(
        Guid id,
        SetUnitOwnershipRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting ownership for unit {UnitId}", id);

        var command = new SetUnitOwnershipCommand(
            id,
            request.Shares,
            request.EffectiveFrom);

        try
        {
            await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<object>.Ok(null, "Unit ownership set successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Adds a file to a unit.
    /// </summary>
    [HttpPost("{id}/files")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> AddUnitFile(
        Guid id,
        AddUnitFileRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding file to unit {UnitId}", id);

        var command = new AddUnitFileCommand(
            id,
            request.FileId,
            request.FileTag,
            request.SortOrder);

        try
        {
            await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<object>.Ok(null, "File added to unit successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
