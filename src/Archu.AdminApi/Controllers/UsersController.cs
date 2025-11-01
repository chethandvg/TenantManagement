using Archu.AdminApi.Authorization;
using Archu.Application.Admin.Commands.CreateUser;
using Archu.Application.Admin.Commands.DeleteUser;
using Archu.Application.Admin.Queries.GetUsers;
using Archu.Contracts.Admin;
using Archu.Contracts.Common;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Archu.AdminApi.Controllers;

/// <summary>
/// Administrative endpoints for user management.
/// Provides CRUD operations for system users.
/// </summary>
/// <remarks>
/// **Access Control:**
/// - View: SuperAdmin, Administrator, Manager
/// - Create: SuperAdmin, Administrator, Manager
/// - Update: SuperAdmin, Administrator, Manager
/// - Delete: SuperAdmin, Administrator
/// 
/// **Security Restrictions:**
/// - Cannot delete the last SuperAdmin in the system
/// - Cannot delete yourself (self-deletion protection)
/// </remarks>
[ApiController]
[Route("api/v{version:apiVersion}/admin/[controller]")]
[ApiVersion("1.0")]
[Authorize(Policy = AdminPolicyNames.RequireAdminAccess)]
[Tags("Users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all users in the system with pagination.
    /// </summary>
    /// <remarks>
    /// **Required Permission:** Admin.Users.View
    /// 
    /// **Allowed Roles:**
    /// - SuperAdmin
    /// - Administrator
    /// - Manager
    /// 
    /// **Example Request:**
    /// ```
    /// GET /api/v1/admin/users?pageNumber=1&amp;pageSize=10
    /// ```
    /// 
    /// **Example Response:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "data": [
    ///     {
    ///       "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///       "userName": "john.doe",
    ///       "email": "john.doe@example.com",
    ///       "emailConfirmed": true,
    ///       "phoneNumber": "+1234567890",
    ///       "twoFactorEnabled": false,
    ///       "lockoutEnabled": true,
    ///       "roles": ["User", "Manager"]
    ///     }
    ///   ],
    ///   "message": "Users retrieved successfully",
    ///   "timestamp": "2025-01-22T10:30:00Z"
    /// }
    /// ```
    /// 
    /// **Pagination:**
    /// - Default page size: 10
    /// - Maximum page size: 100
    /// - Page numbers start at 1
    /// </remarks>
    /// <param name="pageNumber">The page number (default: 1).</param>
    /// <param name="pageSize">The number of users per page (default: 10, max: 100).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A paginated list of users.</returns>
    /// <response code="200">Returns the list of users.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user doesn't have required permissions.</response>
    [HttpGet]
    [Authorize(Policy = AdminPolicyNames.Users.View)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving users (Page: {PageNumber}, PageSize: {PageSize})", pageNumber, pageSize);

        var users = await _mediator.Send(new GetUsersQuery(pageNumber, pageSize), cancellationToken);

        return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(users, "Users retrieved successfully"));
    }

    /// <summary>
    /// Creates a new user in the system.
    /// </summary>
    /// <remarks>
    /// **Required Permission:** Admin.Users.Create
    /// 
    /// **Allowed Roles:**
    /// - SuperAdmin
    /// - Administrator
    /// - Manager
    /// 
    /// **Example Request:**
    /// ```json
    /// POST /api/v1/admin/users
    /// Content-Type: application/json
    /// 
    /// {
    ///   "userName": "john.doe",
    ///   "email": "john.doe@example.com",
    ///   "password": "SecurePassword123!",
    ///   "phoneNumber": "+1234567890",
    ///   "emailConfirmed": false,
    ///   "twoFactorEnabled": false
    /// }
    /// ```
    /// 
    /// **Example Response:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "data": {
    ///     "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///     "userName": "john.doe",
    ///     "email": "john.doe@example.com",
    ///     "emailConfirmed": false,
    ///     "phoneNumber": "+1234567890",
    ///     "twoFactorEnabled": false,
    ///     "lockoutEnabled": true,
    ///     "roles": []
    ///   },
    ///   "message": "User created successfully",
    ///   "timestamp": "2025-01-22T10:30:00Z"
    /// }
    /// ```
    /// 
    /// **Password Requirements:**
    /// - Minimum 8 characters
    /// - At least one uppercase letter
    /// - At least one lowercase letter
    /// - At least one digit
    /// - At least one special character
    /// 
    /// **Important Notes:**
    /// - Email must be unique in the system
    /// - Username must be unique in the system
    /// - User is created without roles - use UserRoles endpoint to assign roles
    /// - Password is securely hashed using BCrypt
    /// </remarks>
    /// <param name="request">The user creation request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The created user.</returns>
    /// <response code="201">Returns the newly created user.</response>
    /// <response code="400">If the request is invalid or user already exists.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user doesn't have required permissions.</response>
    [HttpPost]
    [Authorize(Policy = AdminPolicyNames.Users.Create)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating user: {UserName}", request.UserName);

        try
        {
            var command = new CreateUserCommand(
                request.UserName,
                request.Email,
                request.Password,
                request.PhoneNumber,
                request.EmailConfirmed,
                request.TwoFactorEnabled);

            var user = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("User created with ID: {UserId}", user.Id);

            return CreatedAtAction(
                nameof(GetUsers),
                new { version = "1.0" },
                ApiResponse<UserDto>.Ok(user, "User created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create user: {UserName}", request.UserName);
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Deletes a user from the system (soft delete).
    /// </summary>
    /// <remarks>
    /// **Required Permission:** Admin.Users.Delete
    /// 
    /// **Allowed Roles:**
    /// - SuperAdmin
    /// - Administrator
    /// 
    /// **Security Restrictions:**
    /// - ❌ Cannot delete yourself (self-deletion protection)
    /// - ❌ Cannot delete the last SuperAdmin in the system
    /// - ✅ At least one SuperAdmin must exist to maintain system administration
    /// 
    /// **Example Request:**
    /// ```
    /// DELETE /api/v1/admin/users/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// ```
    /// 
    /// **Example Success Response:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "data": {},
    ///   "message": "User deleted successfully",
    ///   "timestamp": "2025-01-22T10:30:00Z"
    /// }
    /// ```
    /// 
    /// **Example Error Response (Last SuperAdmin):**
    /// ```json
    /// {
    ///   "success": false,
    ///   "data": null,
    ///   "message": "Critical security restriction: Cannot delete the last SuperAdmin user from the system...",
    ///   "timestamp": "2025-01-22T10:30:00Z"
    /// }
    /// ```
    /// 
    /// **Notes:**
    /// - This is a soft delete - user is marked as deleted but not physically removed
    /// - User's roles are preserved in case of restoration
    /// - Deletion is logged with the current admin user
    /// - Deleted users cannot login but data remains for audit purposes
    /// </remarks>
    /// <param name="id">The user ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Success message.</returns>
    /// <response code="200">If the user was successfully deleted.</response>
    /// <response code="400">If the deletion failed due to security restrictions.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user doesn't have required permissions.</response>
    /// <response code="404">If the user is not found.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AdminPolicyNames.Users.Delete)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting user {UserId}", id);

        var command = new DeleteUserCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to delete user: {Error}", result.Error);
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        }

        return Ok(ApiResponse<object>.Ok(new { }, "User deleted successfully"));
    }
}
