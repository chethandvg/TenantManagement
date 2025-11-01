using System.Collections.Generic;
using Archu.AdminApi.Authorization;
using Archu.Application.Admin.Queries.GetPermissions;
using Archu.Contracts.Admin;
using Archu.Contracts.Common;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Archu.AdminApi.Controllers;

/// <summary>
/// Provides read-only access to the permission catalog so admin UIs can render selection lists.
/// </summary>
/// <remarks>
/// **Access Control:**
/// - View Permission Catalog: SuperAdmin, Administrator, Manager
///
/// The endpoint is intentionally read-only and safe to expose to privileged operators for building assignment experiences.
/// </remarks>
[ApiController]
[Route("api/v{version:apiVersion}/admin/[controller]")]
[ApiVersion("1.0")]
[Authorize(Policy = AdminPolicyNames.RequireAdminAccess)]
[Tags("Permissions")]
public sealed class PermissionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PermissionsController> _logger;

    /// <summary>
    /// Initializes a new <see cref="PermissionsController"/> instance.
    /// </summary>
    public PermissionsController(IMediator mediator, ILogger<PermissionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Lists all permissions available in the system.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>Collection of permissions including metadata for display.</returns>
    /// <response code="200">The permissions were retrieved successfully.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller lacks the required admin policy.</response>
    [HttpGet]
    [Authorize(Policy = AdminPolicyNames.Permissions.View)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PermissionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PermissionDto>>>> GetPermissions(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving permission catalog");

        var query = new GetPermissionsQuery();
        var permissions = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<IEnumerable<PermissionDto>>.Ok(permissions, "Permissions retrieved successfully"));
    }
}
