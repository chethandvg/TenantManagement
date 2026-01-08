using TentMan.Application.PropertyManagement.Buildings.Commands.CreateBuilding;
using TentMan.Application.PropertyManagement.Buildings.Queries.GetBuildings;
using TentMan.Contracts.Buildings;
using TentMan.Contracts.Common;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers.PropertyManagement;

/// <summary>
/// API endpoints for managing buildings in the property management system.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/organizations/{orgId}/[controller]")]
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
    /// Creates a new building for an organization.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BuildingDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<BuildingDetailDto>>> CreateBuilding(
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
                nameof(GetBuildings),
                new { orgId, version = "1.0" },
                ApiResponse<BuildingDetailDto>.Ok(building, "Building created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Gets all buildings for an organization.
    /// </summary>
    [HttpGet]
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
}
