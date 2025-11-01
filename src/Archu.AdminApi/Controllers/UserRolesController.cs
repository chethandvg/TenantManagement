using Archu.AdminApi.Authorization;
using Archu.Application.Abstractions;
using Archu.Application.Admin.Commands.AssignRole;
using Archu.Application.Admin.Commands.RemoveRole;
using Archu.Contracts.Admin;
using Archu.Contracts.Common;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Archu.AdminApi.Controllers;

/// <summary>
/// Administrative endpoints for user-role assignment management.
/// Provides operations for assigning and removing roles from users.
/// </summary>
/// <remarks>
/// **Access Control:**
/// - View User Roles: SuperAdmin, Administrator, Manager
/// - Assign Roles: SuperAdmin, Administrator
/// - Remove Roles: SuperAdmin, Administrator
/// 
/// **Security Restrictions:**
/// - Only SuperAdmin can assign/remove SuperAdmin and Administrator roles
/// - Administrator can assign/remove User, Manager, and Guest roles only
/// - Users cannot remove their own privileged roles (SuperAdmin/Administrator)
/// - Cannot remove SuperAdmin role if user is the last SuperAdmin
/// 
/// **Role Assignment Matrix:**
/// 
/// | Admin Role | Can Assign → | SuperAdmin | Administrator | Manager | User | Guest |
/// |-----------|--------------|-----------|--------------|---------|------|-------|
/// | **SuperAdmin** | | ✅ | ✅ | ✅ | ✅ | ✅ |
/// | **Administrator** | | ❌ | ❌ | ✅ | ✅ | ✅ |
/// | **Manager** | | ❌ | ❌ | ❌ | ❌ | ❌ |
/// 
/// **Role Removal Matrix:**
/// 
/// | Admin Role | Can Remove → | SuperAdmin | Administrator | Manager | User | Guest |
/// |-----------|--------------|-----------|--------------|---------|------|-------|
/// | **SuperAdmin** | | ✅* | ✅ | ✅ | ✅ | ✅ |
/// | **Administrator** | | ❌ | ❌** | ✅ | ✅ | ✅ |
/// 
/// *Except own SuperAdmin or last SuperAdmin  
/// **Except own Administrator
/// </remarks>
[ApiController]
[Route("api/v{version:apiVersion}/admin/[controller]")]
[ApiVersion("1.0")]
[Authorize(Policy = AdminPolicyNames.RequireAdminAccess)]
[Tags("UserRoles")]
public class UserRolesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserRolesController> _logger;

    public UserRolesController(
        IMediator mediator,
        IUnitOfWork unitOfWork,
        ILogger<UserRolesController> logger)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all roles assigned to a specific user.
    /// </summary>
    /// <remarks>
    /// **Required Permission:** Admin.UserRoles.View
    /// 
    /// **Allowed Roles:**
    /// - SuperAdmin
    /// - Administrator
    /// - Manager
    /// 
    /// **Example Request:**
    /// ```
    /// GET /api/v1/admin/user-roles/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// Authorization: Bearer &lt;your-jwt-token&gt;
    /// ```
    /// 
    /// **Example Response:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "data": [
    ///     {
    ///       "id": "7ba85f64-5717-4562-b3fc-2c963f66afa9",
    ///       "name": "Manager",
    ///       "normalizedName": "MANAGER",
    ///       "description": "Manager role with elevated permissions"
    ///     },
    ///     {
    ///       "id": "1da85f64-5717-4562-b3fc-2c963f66afac",
    ///       "name": "User",
    ///       "normalizedName": "USER",
    ///       "description": "Standard user role with basic application access"
    ///     }
    ///   ],
    ///   "message": "User roles retrieved successfully",
    ///   "timestamp": "2025-01-22T10:30:00Z"
    /// }
    /// ```
    /// 
    /// **Use Cases:**
    /// - Check what roles a user currently has before assigning new ones
    /// - Verify user permissions for troubleshooting
    /// - Audit user access levels
    /// </remarks>
    /// <param name="userId">The user ID to retrieve roles for.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of roles assigned to the user.</returns>
    /// <response code="200">Returns the list of roles.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user doesn't have required permissions.</response>
    /// <response code="404">If the user is not found.</response>
    [HttpGet("{userId:guid}")]
    [Authorize(Policy = AdminPolicyNames.UserRoles.View)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<RoleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IEnumerable<RoleDto>>>> GetUserRoles(
        Guid userId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving roles for user {UserId}", userId);

        try
        {
            // Check if user exists
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return NotFound(ApiResponse<IEnumerable<RoleDto>>.Fail($"User with ID {userId} not found"));
            }

            // Get all roles and check which ones the user has
            var allRoles = await _unitOfWork.Roles.GetAllAsync(cancellationToken);
            var userRoles = new List<RoleDto>();

            foreach (var role in allRoles)
            {
                if (await _unitOfWork.UserRoles.UserHasRoleAsync(userId, role.Id, cancellationToken))
                {
                    userRoles.Add(new RoleDto
                    {
                        Id = role.Id,
                        Name = role.Name,
                        NormalizedName = role.NormalizedName,
                        Description = role.Description
                    });
                }
            }

            return Ok(ApiResponse<IEnumerable<RoleDto>>.Ok(userRoles, "User roles retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles for user {UserId}", userId);
            return BadRequest(ApiResponse<IEnumerable<RoleDto>>.Fail("Failed to retrieve user roles"));
        }
    }

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    /// <remarks>
    /// **Required Permission:** Admin.UserRoles.Assign
    /// 
    /// **Allowed Roles:**
    /// - SuperAdmin
    /// - Administrator
    /// 
    /// **Security Restrictions:**
    /// - ✅ SuperAdmin can assign **any role** (including SuperAdmin and Administrator)
    /// - ✅ Administrator can assign **User, Manager, and Guest** roles only
    /// - ❌ Administrator **CANNOT** assign SuperAdmin role
    /// - ❌ Administrator **CANNOT** assign Administrator role
    /// 
    /// **Example Request:**
    /// ```json
    /// POST /api/v1/admin/user-roles/assign
    /// Content-Type: application/json
    /// Authorization: Bearer &lt;your-jwt-token&gt;
    /// 
    /// {
    ///   "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "roleId": "7ba85f64-5717-4562-b3fc-2c963f66afa9"
    /// }
    /// ```
    /// 
    /// **Example Success Response:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "data": {},
    ///   "message": "Role assigned successfully",
    ///   "timestamp": "2025-01-22T10:30:00Z"
    /// }
    /// ```
    /// 
    /// **Example Error Response (Administrator tries to assign SuperAdmin):**
    /// ```json
    /// {
    ///   "success": false,
    ///   "data": null,
    ///   "message": "Permission denied: Only SuperAdmin can assign the 'SuperAdmin' role. Administrators cannot elevate users to SuperAdmin status.",
    ///   "timestamp": "2025-01-22T10:30:00Z"
    /// }
    /// ```
    /// 
    /// **Example Error Response (User already has role):**
    /// ```json
    /// {
    ///   "success": false,
    ///   "data": null,
    ///   "message": "User 'john.doe' already has the role 'Manager'",
    ///   "timestamp": "2025-01-22T10:30:00Z"
    /// }
    /// ```
    /// 
    /// **Important Notes:**
    /// - Both user and role must exist in the system
    /// - If the user already has the role, an error is returned
    /// - Assignment is logged with the current admin user as the assignor
    /// - Role assignments are tracked with timestamp and assignor for audit purposes
    /// </remarks>
    /// <param name="request">The role assignment request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Success message.</returns>
    /// <response code="200">If the role was successfully assigned.</response>
    /// <response code="400">If the request is invalid or assignment failed.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user doesn't have required permissions.</response>
    [HttpPost("assign")]
    [Authorize(Policy = AdminPolicyNames.UserRoles.Assign)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> AssignRole(
        AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Assigning role {RoleId} to user {UserId}",
            request.RoleId,
            request.UserId);

        var command = new AssignRoleCommand(request.UserId, request.RoleId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to assign role: {Error}", result.Error);
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<object>.Ok(new { }, "Role assigned successfully"));
    }

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    /// <remarks>
    /// **Required Permission:** Admin.UserRoles.Remove
    /// 
    /// **Allowed Roles:**
    /// - SuperAdmin
    /// - Administrator
    /// 
    /// **Security Restrictions:**
    /// - ✅ SuperAdmin can remove **any role** (with exceptions below)
    /// - ✅ Administrator can remove **User, Manager, and Guest** roles only
    /// - ❌ Administrator **CANNOT** remove SuperAdmin role
    /// - ❌ Administrator **CANNOT** remove Administrator role
    /// - ❌ **Cannot remove your own SuperAdmin role**
    /// - ❌ **Cannot remove your own Administrator role**
    /// - ❌ **Cannot remove SuperAdmin role if user is the last SuperAdmin**
    /// 
    /// **Example Request:**
    /// ```
    /// DELETE /api/v1/admin/user-roles/3fa85f64-5717-4562-b3fc-2c963f66afa6/roles/7ba85f64-5717-4562-b3fc-2c963f66afa9
    /// Authorization: Bearer &lt;your-jwt-token&gt;
    /// ```
    /// 
    /// **Example Success Response:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "data": {},
    ///   "message": "Role removed successfully",
    ///   "timestamp": "2025-01-22T10:30:00Z"
    /// }
    /// ```
    /// 
    /// **Example Error Response (Self-removal of SuperAdmin):**
    /// ```json
    /// {
    ///   "success": false,
    ///   "data": null,
    ///   "message": "Security restriction: You cannot remove your own SuperAdmin role. This prevents accidental loss of system administration privileges. Another SuperAdmin must remove this role.",
    ///   "timestamp": "2025-01-22T10:30:00Z"
    /// }
    /// ```
    /// 
    /// **Example Error Response (Last SuperAdmin):**
    /// ```json
    /// {
    ///   "success": false,
    ///   "data": null,
    ///   "message": "Critical security restriction: Cannot remove the last SuperAdmin role from the system. At least one SuperAdmin must exist to maintain system administration capabilities...",
    ///   "timestamp": "2025-01-22T10:30:00Z"
    /// }
    /// ```
    /// 
    /// **Example Error Response (Administrator tries to remove SuperAdmin):**
    /// ```json
    /// {
    ///   "success": false,
    ///   "data": null,
    ///   "message": "Permission denied: Only SuperAdmin can remove the 'SuperAdmin' role. Administrators cannot demote SuperAdmin users.",
    ///   "timestamp": "2025-01-22T10:30:00Z"
    /// }
    /// ```
    /// 
    /// **Important Notes:**
    /// - Both user and role must exist in the system
    /// - If the user doesn't have the role, a 400 error is returned
    /// - Removal is logged with the current admin user
    /// - These restrictions prevent accidental privilege loss and maintain system integrity
    /// </remarks>
    /// <param name="userId">The user ID.</param>
    /// <param name="roleId">The role ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Success message.</returns>
    /// <response code="200">If the role was successfully removed.</response>
    /// <response code="400">If the removal failed.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user doesn't have required permissions.</response>
    /// <response code="404">If the user or role is not found.</response>
    [HttpDelete("{userId:guid}/roles/{roleId:guid}")]
    [Authorize(Policy = AdminPolicyNames.UserRoles.Remove)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> RemoveRole(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Removing role {RoleId} from user {UserId}", roleId, userId);

        var command = new RemoveRoleCommand(userId, roleId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to remove role: {Error}", result.Error);
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<object>.Ok(new { }, "Role removed successfully"));
    }
}
