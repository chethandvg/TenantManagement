using Archu.AdminApi.Authorization;
using Archu.Application.Admin.Commands.CreateRole;
using Archu.Application.Admin.Queries.GetRoles;
using Archu.Contracts.Admin;
using Archu.Contracts.Common;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Archu.AdminApi.Controllers;

/// <summary>
/// Administrative endpoints for role management.
/// Provides CRUD operations for system roles.
/// </summary>
/// <remarks>
/// **Access Control:**
/// - View: SuperAdmin, Administrator, Manager
/// - Create: SuperAdmin, Administrator
/// - Update: SuperAdmin, Administrator
/// - Delete: SuperAdmin only
/// 
/// **System Roles:**
/// - **SuperAdmin** - System administrator with unrestricted access
/// - **Administrator** - Full system access except SuperAdmin management
/// - **Manager** - Elevated permissions for team management
/// - **User** - Standard user with basic application access
/// - **Guest** - Minimal read-only access
/// </remarks>
[ApiController]
[Route("api/v{version:apiVersion}/admin/[controller]")]
[ApiVersion("1.0")]
[Authorize(Policy = AdminPolicyNames.RequireAdminAccess)]
[Tags("Roles")]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IMediator mediator, ILogger<RolesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all roles in the system.
    /// </summary>
    /// <remarks>
    /// **Required Permission:** Admin.Roles.View
    /// 
    /// **Allowed Roles:**
    /// - SuperAdmin
    /// - Administrator
    /// - Manager
    /// 
    /// **Example Request:**
    /// ```
    /// GET /api/v1/admin/roles
    /// Authorization: Bearer &lt;your-jwt-token&gt;
    /// ```
    /// 
    /// **Example Response:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "data": [
    ///     {
    ///       "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///       "name": "SuperAdmin",
    ///       "normalizedName": "SUPERADMIN",
    ///       "description": "System administrator with unrestricted access"
    ///     },
    ///     {
    ///       "id": "7ba85f64-5717-4562-b3fc-2c963f66afa9",
    ///       "name": "Administrator",
    ///       "normalizedName": "ADMINISTRATOR",
    ///       "description": "Administrator role with full system access"
    ///     },
    ///     {
    ///       "id": "9ca85f64-5717-4562-b3fc-2c963f66afab",
    ///       "name": "Manager",
    ///       "normalizedName": "MANAGER",
    ///       "description": "Manager role with elevated permissions for team management"
    ///     },
    ///     {
    ///       "id": "1da85f64-5717-4562-b3fc-2c963f66afac",
    ///       "name": "User",
    ///       "normalizedName": "USER",
    ///       "description": "Standard user role with basic application access"
    ///     },
    ///     {
    ///       "id": "2ea85f64-5717-4562-b3fc-2c963f66afad",
    ///       "name": "Guest",
    ///       "normalizedName": "GUEST",
    ///       "description": "Guest role with minimal read-only access"
    ///     }
    ///   ],
    ///   "message": "Roles retrieved successfully",
    ///   "timestamp": "2025-01-22T10:30:00Z"
    /// }
    /// ```
    /// 
    /// **Notes:**
    /// - Returns all active roles in the system
    /// - System roles (SuperAdmin, Administrator, Manager, User, Guest) are created during initialization
    /// - Custom roles can be created by SuperAdmin and Administrator
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of all roles.</returns>
    /// <response code="200">Returns the list of roles.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user doesn't have required permissions.</response>
    [HttpGet]
    [Authorize(Policy = AdminPolicyNames.Roles.View)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<RoleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IEnumerable<RoleDto>>>> GetRoles(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving all roles");

        var roles = await _mediator.Send(new GetRolesQuery(), cancellationToken);

        return Ok(ApiResponse<IEnumerable<RoleDto>>.Ok(roles, "Roles retrieved successfully"));
    }

    /// <summary>
    /// Creates a new custom role in the system.
    /// </summary>
    /// <remarks>
    /// **Required Permission:** Admin.Roles.Create
    /// 
    /// **Allowed Roles:**
    /// - SuperAdmin
    /// - Administrator
    /// 
    /// **Example Request:**
    /// ```json
    /// POST /api/v1/admin/roles
    /// Content-Type: application/json
    /// Authorization: Bearer &lt;your-jwt-token&gt;
    /// 
    /// {
    ///   "name": "ContentEditor",
    ///   "description": "Can edit content but not manage users"
    /// }
    /// ```
    /// 
    /// **Example Response:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "data": {
    ///     "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///     "name": "ContentEditor",
    ///     "normalizedName": "CONTENTEDITOR",
    ///     "description": "Can edit content but not manage users"
    ///   },
    ///   "message": "Role created successfully",
    ///   "timestamp": "2025-01-22T10:30:00Z"
    /// }
    /// ```
    /// 
    /// **Validation Rules:**
    /// - Role name is required
    /// - Role name must be unique (case-insensitive)
    /// - Role name length: 3-50 characters
    /// - Role name can only contain letters, numbers, and spaces
    /// - Description is optional but recommended (max 500 characters)
    /// 
    /// **Important Notes:**
    /// - Role names are automatically normalized to uppercase for comparison
    /// - Cannot create roles with same name as existing roles
    /// - Custom roles can be assigned to users via UserRoles endpoints
    /// - Role deletion is restricted to SuperAdmin only
    /// </remarks>
    /// <param name="request">The role creation request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The created role.</returns>
    /// <response code="201">Returns the newly created role.</response>
    /// <response code="400">If the request is invalid or role already exists.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user doesn't have required permissions.</response>
    [HttpPost]
    [Authorize(Policy = AdminPolicyNames.Roles.Create)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole(
        CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating role: {RoleName}", request.Name);

        try
        {
            var command = new CreateRoleCommand(request.Name, request.Description);
            var role = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("Role created with ID: {RoleId}", role.Id);

            return CreatedAtAction(
                nameof(GetRoles),
                new { version = "1.0" },
                ApiResponse<RoleDto>.Ok(role, "Role created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create role: {RoleName}", request.Name);
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
