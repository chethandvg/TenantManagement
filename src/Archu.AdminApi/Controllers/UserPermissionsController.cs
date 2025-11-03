using System;
using Archu.AdminApi.Authorization;
using Archu.Application.Admin.Commands.AssignPermissionToUser;
using Archu.Application.Admin.Commands.RemovePermissionFromUser;
using Archu.Application.Admin.Queries.GetEffectiveUserPermissions;
using Archu.Application.Admin.Queries.GetUserDirectPermissions;
using Archu.Contracts.Admin;
using Archu.Contracts.Common;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Archu.AdminApi.Controllers;

/// <summary>
/// Administrative endpoints for managing permissions assigned directly to users.
/// Provides views into direct and effective permissions plus assign/remove operations.
/// </summary>
/// <remarks>
/// **Access Control:**
/// - View Direct Permissions: SuperAdmin, Administrator, Manager
/// - View Effective Permissions: SuperAdmin, Administrator, Manager
/// - Assign Direct Permissions: SuperAdmin only
/// - Remove Direct Permissions: SuperAdmin only
///
/// **Usage Tips:**
/// - Use the direct view to audit explicit grants that bypass role inheritance.
/// - Use the effective view to troubleshoot aggregate access including role-derived permissions.
/// </remarks>
[ApiController]
[Route("api/v{version:apiVersion}/admin/users/{userId:guid}/permissions")]
[ApiVersion("1.0")]
[Authorize(Policy = AdminPolicyNames.RequireAdminAccess)]
[Tags("UserPermissions")]
public sealed class UserPermissionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UserPermissionsController> _logger;

    /// <summary>
    /// Initializes a new <see cref="UserPermissionsController"/> instance.
    /// </summary>
    public UserPermissionsController(IMediator mediator, ILogger<UserPermissionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the permissions explicitly assigned to the specified user.
    /// </summary>
    /// <param name="userId">Identifier of the user whose direct permissions are requested.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The user profile combined with directly assigned permissions.</returns>
    /// <response code="200">Direct permissions were retrieved successfully.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller lacks the required policy.</response>
    /// <response code="404">The target user was not found.</response>
    [HttpGet("direct")]
    [Authorize(Policy = AdminPolicyNames.UserPermissions.ViewDirect)]
    [ProducesResponseType(typeof(ApiResponse<UserPermissionsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserPermissionsDto>>> GetDirectPermissions(
        Guid userId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving direct permissions for user {UserId}", userId);

        try
        {
            var query = new GetUserDirectPermissionsQuery(userId);
            var result = await _mediator.Send(query, cancellationToken);

            return Ok(ApiResponse<UserPermissionsDto>.Ok(result, "Direct user permissions retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User {UserId} not found when retrieving direct permissions", userId);
            return NotFound(ApiResponse<UserPermissionsDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Retrieves the effective permissions for the specified user, including role inheritance.
    /// </summary>
    /// <param name="userId">Identifier of the user to analyze.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>An aggregate view of direct and role-based permissions.</returns>
    /// <response code="200">Effective permissions were retrieved successfully.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller lacks the required policy.</response>
    /// <response code="404">The target user was not found.</response>
    [HttpGet("effective")]
    [Authorize(Policy = AdminPolicyNames.UserPermissions.ViewEffective)]
    [ProducesResponseType(typeof(ApiResponse<EffectiveUserPermissionsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EffectiveUserPermissionsDto>>> GetEffectivePermissions(
        Guid userId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving effective permissions for user {UserId}", userId);

        try
        {
            var query = new GetEffectiveUserPermissionsQuery(userId);
            var result = await _mediator.Send(query, cancellationToken);

            return Ok(ApiResponse<EffectiveUserPermissionsDto>.Ok(result, "Effective user permissions retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User {UserId} not found when retrieving effective permissions", userId);
            return NotFound(ApiResponse<EffectiveUserPermissionsDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Assigns one or more permissions directly to the specified user.
    /// </summary>
    /// <param name="userId">Identifier of the user that should receive the permissions.</param>
    /// <param name="request">List of permission names to grant directly to the user.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The updated direct permission projection.</returns>
    /// <response code="200">Permissions were assigned successfully.</response>
    /// <response code="400">The request was invalid or permissions already existed.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller lacks SuperAdmin privileges.</response>
    /// <response code="404">The target user was not found.</response>
    [HttpPost]
    [Authorize(Policy = AdminPolicyNames.UserPermissions.Assign)]
    [ProducesResponseType(typeof(ApiResponse<UserPermissionsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserPermissionsDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserPermissionsDto>>> AssignPermissionsToUser(
        Guid userId,
        [FromBody] ModifyUserPermissionsRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Assigning direct permissions {@PermissionNames} to user {UserId}",
            request.PermissionNames,
            userId);

        try
        {
            var command = new AssignPermissionToUserCommand(userId, request.PermissionNames);
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(ApiResponse<UserPermissionsDto>.Ok(result, "Permissions assigned to user successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to assign permissions to user {UserId}", userId);

            return ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(ApiResponse<UserPermissionsDto>.Fail(ex.Message))
                : BadRequest(ApiResponse<UserPermissionsDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Removes one or more direct permissions from the specified user.
    /// </summary>
    /// <param name="userId">Identifier of the user that should lose the permissions.</param>
    /// <param name="request">List of permission names to revoke directly from the user.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The updated direct permission projection.</returns>
    /// <response code="200">Permissions were removed successfully.</response>
    /// <response code="400">The request was invalid or none of the permissions were assigned.</response>
    /// <response code="401">The caller is not authenticated.</response>
    /// <response code="403">The caller lacks SuperAdmin privileges.</response>
    /// <response code="404">The target user was not found.</response>
    [HttpDelete]
    [Authorize(Policy = AdminPolicyNames.UserPermissions.Remove)]
    [ProducesResponseType(typeof(ApiResponse<UserPermissionsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserPermissionsDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserPermissionsDto>>> RemovePermissionsFromUser(
        Guid userId,
        [FromBody] ModifyUserPermissionsRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Removing direct permissions {@PermissionNames} from user {UserId}",
            request.PermissionNames,
            userId);

        try
        {
            var command = new RemovePermissionFromUserCommand(userId, request.PermissionNames);
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(ApiResponse<UserPermissionsDto>.Ok(result, "Permissions removed from user successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to remove permissions from user {UserId}", userId);

            return ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(ApiResponse<UserPermissionsDto>.Fail(ex.Message))
                : BadRequest(ApiResponse<UserPermissionsDto>.Fail(ex.Message));
        }
    }
}
