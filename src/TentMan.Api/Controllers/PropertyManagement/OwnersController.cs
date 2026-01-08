using TentMan.Application.PropertyManagement.Owners.Commands.CreateOwner;
using TentMan.Application.PropertyManagement.Owners.Queries.GetOwners;
using TentMan.Contracts.Common;
using TentMan.Contracts.Owners;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers.PropertyManagement;

/// <summary>
/// API endpoints for managing property owners.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/organizations/{orgId}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
public class OwnersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OwnersController> _logger;

    public OwnersController(IMediator mediator, ILogger<OwnersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new owner for an organization.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OwnerDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<OwnerDto>>> CreateOwner(
        Guid orgId,
        CreateOwnerRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating owner {DisplayName} for organization {OrgId}", request.DisplayName, orgId);

        var command = new CreateOwnerCommand(
            orgId,
            request.OwnerType,
            request.DisplayName,
            request.Phone,
            request.Email,
            request.Pan,
            request.Gstin,
            request.LinkedUserId);

        try
        {
            var owner = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(
                nameof(GetOwners),
                new { orgId, version = "1.0" },
                ApiResponse<OwnerDto>.Ok(owner, "Owner created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Gets all owners for an organization.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<OwnerDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<OwnerDto>>>> GetOwners(
        Guid orgId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting owners for organization {OrgId}", orgId);

        var query = new GetOwnersQuery(orgId);
        var owners = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<IEnumerable<OwnerDto>>.Ok(owners, "Owners retrieved successfully"));
    }
}
