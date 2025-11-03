using System;
using Archu.AdminApi.Authorization;
using Archu.Application.Admin.Commands.AssignPermissionToRole;
using Archu.Application.Admin.Commands.RemovePermissionFromRole;
using Archu.Application.Admin.Queries.GetRolePermissions;
using Archu.Contracts.Admin;
using Archu.Contracts.Common;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Archu.AdminApi.Controllers;

/// <summary>
/// Administrative endpoints for managing the permissions that belong to a role.
/// Supports viewing, assigning, and removing permissions with strict authorization controls.
/// </summary>
/// <remarks>
/// **Access Control:**
/// - View Role Permissions: SuperAdmin, Administrator, Manager
/// - Assign Permissions to Role: SuperAdmin only
/// - Remove Permissions from Role: SuperAdmin only
///
/// **Typical Workflow:**
/// 1. Retrieve the role's current permission set with <c>GET /roles/{roleId}/permissions</c>
/// 2. Assign missing permissions using <c>POST /roles/{roleId}/permissions</c>
/// 3. Remove obsolete permissions using <c>DELETE /roles/{roleId}/permissions</c>
/// </remarks>
[ApiController]
[Route("api/v{version:apiVersion}/admin/roles/{roleId:guid}/permissions")]
[ApiVersion("1.0")]
[Authorize(Policy = AdminPolicyNames.RequireAdminAccess)]
[Tags("RolePermissions")]
public sealed class RolePermissionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RolePermissionsController> _logger;

    /// <summary>
    /// Initializes a new <see cref="RolePermissionsController"/> instance.
    /// </summary>
    public RolePermissionsController(IMediator mediator, ILogger<RolePermissionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the permissions currently associated with the specified role.
    /// </summary>
    /// <param name="roleId">Unique identifier of the role to inspect.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The role metadata together with its permission assignments.</returns>
    /// <response code="200">Returns the role along with the assigned permissions.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller lacks the required admin policy.</response>
    /// <response code="404">The requested role could not be found.</response>
    [HttpGet]
    [Authorize(Policy = AdminPolicyNames.RolePermissions.View)]
    [ProducesResponseType(typeof(ApiResponse<RolePermissionsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RolePermissionsDto>>> GetRolePermissions(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving permissions for role {RoleId}", roleId);

        try
        {
            var query = new GetRolePermissionsQuery(roleId);
            var result = await _mediator.Send(query, cancellationToken);

            return Ok(ApiResponse<RolePermissionsDto>.Ok(result, "Role permissions retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Role {RoleId} not found when retrieving permissions", roleId);
            return NotFound(ApiResponse<RolePermissionsDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Assigns one or more permissions to the specified role.
    /// </summary>
    /// <param name="roleId">Unique identifier of the role receiving new permissions.</param>
    /// <param name="request">List of permission names that should be granted to the role.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The updated role permission projection.</returns>
    /// <response code="200">Permissions were assigned successfully.</response>
    /// <response code="400">The request was invalid or all permissions were already assigned.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller lacks SuperAdmin privileges.</response>
    /// <response code="404">The target role was not found.</response>
    [HttpPost]
    [Authorize(Policy = AdminPolicyNames.RolePermissions.Assign)]
    [ProducesResponseType(typeof(ApiResponse<RolePermissionsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RolePermissionsDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RolePermissionsDto>>> AssignPermissionsToRole(
        Guid roleId,
        [FromBody] ModifyRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Assigning permissions {@PermissionNames} to role {RoleId}",
            request.PermissionNames,
            roleId);

        try
        {
            var command = new AssignPermissionToRoleCommand(roleId, request.PermissionNames);
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(ApiResponse<RolePermissionsDto>.Ok(result, "Permissions assigned to role successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to assign permissions to role {RoleId}", roleId);

            return ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(ApiResponse<RolePermissionsDto>.Fail(ex.Message))
                : BadRequest(ApiResponse<RolePermissionsDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Removes one or more permissions from the specified role.
    /// </summary>
    /// <param name="roleId">Unique identifier of the role losing permissions.</param>
    /// <param name="request">List of permission names that should be revoked from the role.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The updated role permission projection.</returns>
    /// <response code="200">Permissions were removed successfully.</response>
    /// <response code="400">The request was invalid or none of the permissions were assigned.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller lacks SuperAdmin privileges.</response>
    /// <response code="404">The role could not be located.</response>
    [HttpDelete]
    [Authorize(Policy = AdminPolicyNames.RolePermissions.Remove)]
    [ProducesResponseType(typeof(ApiResponse<RolePermissionsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RolePermissionsDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RolePermissionsDto>>> RemovePermissionsFromRole(
        Guid roleId,
        [FromBody] ModifyRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Removing permissions {@PermissionNames} from role {RoleId}",
            request.PermissionNames,
            roleId);

        try
        {
            var command = new RemovePermissionFromRoleCommand(roleId, request.PermissionNames);
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(ApiResponse<RolePermissionsDto>.Ok(result, "Permissions removed from role successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to remove permissions from role {RoleId}", roleId);

            return ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(ApiResponse<RolePermissionsDto>.Fail(ex.Message))
                : BadRequest(ApiResponse<RolePermissionsDto>.Fail(ex.Message));
        }
    }
}
