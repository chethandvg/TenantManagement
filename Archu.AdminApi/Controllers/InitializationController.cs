using Archu.Application.Admin.Commands.InitializeSystem;
using Archu.Contracts.Admin;
using Archu.Contracts.Common;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Archu.AdminApi.Controllers;

/// <summary>
/// Endpoints for system initialization and setup.
/// These endpoints bootstrap the application with default roles and a super admin user.
/// </summary>
/// <remarks>
/// **Important:**
/// - These endpoints should only be used during initial setup
/// - The initialization endpoint can only be called once (when no users exist)
/// - Subsequent calls will fail to prevent accidental data corruption
/// 
/// **Typical Workflow:**
/// 1. Deploy application
/// 2. Call `/api/v1/admin/initialization/initialize` to create roles and super admin
/// 3. Use super admin credentials to manage other users and roles
/// </remarks>
[ApiController]
[Route("api/v{version:apiVersion}/admin/[controller]")]
[ApiVersion("1.0")]
[Tags("Initialization")]
public class InitializationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<InitializationController> _logger;

    public InitializationController(IMediator mediator, ILogger<InitializationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the system with all required roles and a super admin user.
    /// This endpoint should only be called once during initial system setup.
    /// After initialization, this endpoint will be protected and require authentication.
    /// </summary>
    /// <param name="request">The initialization request containing super admin credentials.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The initialization result.</returns>
    /// <response code="200">System initialized successfully.</response>
    /// <response code="400">If the system is already initialized or the request is invalid.</response>
    /// <remarks>
    /// This endpoint creates:
    /// - 5 system roles: Guest, User, Manager, Administrator, SuperAdmin
    /// - 1 super admin user with the provided credentials
    /// - Assigns the SuperAdmin role to the created user
    /// 
    /// **IMPORTANT**: 
    /// - This endpoint can only be called when no users exist in the system
    /// - After successful initialization, subsequent calls will be rejected
    /// - Store the super admin credentials securely
    /// - After initialization, use the super admin account to create other users and assign roles
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "userName": "superadmin",
    ///   "email": "admin@example.com",
    ///   "password": "YourSecurePassword123!"
    /// }
    /// ```
    /// </remarks>
    [HttpPost("initialize")]
    [AllowAnonymous] // Allow anonymous access for initial setup only
    [ProducesResponseType(typeof(ApiResponse<InitializationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<InitializationResult>>> InitializeSystem(
        InitializeSystemRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("System initialization requested for user: {UserName}", request.UserName);

        try
        {
            var command = new InitializeSystemCommand(
                request.UserName,
                request.Email,
                request.Password);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("System initialization failed: {Error}", result.Error);
                return BadRequest(ApiResponse<object>.Fail(result.Error!));
            }

            _logger.LogInformation("System initialized successfully");

            return Ok(ApiResponse<InitializationResult>.Ok(
                result.Value!,
                "System initialized successfully. You can now login with the super admin credentials."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during system initialization");
            return BadRequest(ApiResponse<object>.Fail("An unexpected error occurred during system initialization"));
        }
    }
}
