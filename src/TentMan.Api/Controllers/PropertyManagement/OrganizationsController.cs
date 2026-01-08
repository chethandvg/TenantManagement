using TentMan.Application.PropertyManagement.Organizations.Commands.CreateOrganization;
using TentMan.Contracts.Common;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers.PropertyManagement;

/// <summary>
/// API endpoints for managing organizations.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrganizationsController> _logger;

    public OrganizationsController(IMediator mediator, ILogger<OrganizationsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new organization.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateOrganization(
        CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating organization {Name}", request.Name);

        var command = new CreateOrganizationCommand(request.Name, request.TimeZone);
        var organizationId = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(CreateOrganization),
            new { id = organizationId },
            ApiResponse<Guid>.Ok(organizationId, "Organization created successfully"));
    }
}

public record CreateOrganizationRequest(string Name, string TimeZone = "Asia/Kolkata");
