using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TentMan.Contracts.Authorization;
using TentMan.Contracts.Common;

namespace TentMan.Api.Controllers;

/// <summary>
/// Controller for authorization-related operations.
/// Provides endpoints to check user permissions and policies.
/// </summary>
[ApiController]
[Route("api/v1/authorization")]
[Authorize]
public class AuthorizationController : ControllerBase
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<AuthorizationController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationController"/> class.
    /// </summary>
    /// <param name="authorizationService">The authorization service.</param>
    /// <param name="logger">The logger.</param>
    public AuthorizationController(
        IAuthorizationService authorizationService,
        ILogger<AuthorizationController> logger)
    {
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks if the current user has the specified permission.
    /// </summary>
    /// <param name="request">The permission check request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authorization check result.</returns>
    [HttpPost("check-permission")]
    [ProducesResponseType(typeof(ApiResponse<AuthorizationCheckResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse<AuthorizationCheckResponse>> CheckPermission(
        [FromBody] CheckPermissionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Permission))
        {
            return BadRequest(ApiResponse<object>.Fail("Permission cannot be empty."));
        }

        var hasPermission = User.HasClaim(c =>
            c.Type == "permission" &&
            c.Value.Equals(request.Permission, StringComparison.Ordinal));

        var response = new AuthorizationCheckResponse
        {
            IsAuthorized = hasPermission,
            Message = hasPermission
                ? $"User has permission: {request.Permission}"
                : $"User does not have permission: {request.Permission}"
        };

        _logger.LogDebug(
            "Permission check for {Permission}: {IsAuthorized}",
            request.Permission,
            hasPermission);

        return Ok(ApiResponse<AuthorizationCheckResponse>.Ok(response));
    }

    /// <summary>
    /// Checks if the current user satisfies the specified policy.
    /// </summary>
    /// <param name="request">The policy check request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authorization check result.</returns>
    [HttpPost("check-policy")]
    [ProducesResponseType(typeof(ApiResponse<AuthorizationCheckResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthorizationCheckResponse>>> CheckPolicy(
        [FromBody] CheckPolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PolicyName))
        {
            return BadRequest(ApiResponse<object>.Fail("Policy name cannot be empty."));
        }

        var authResult = await _authorizationService.AuthorizeAsync(User, request.PolicyName);

        var response = new AuthorizationCheckResponse
        {
            IsAuthorized = authResult.Succeeded,
            Message = authResult.Succeeded
                ? $"User satisfies policy: {request.PolicyName}"
                : $"User does not satisfy policy: {request.PolicyName}"
        };

        _logger.LogDebug(
            "Policy check for {PolicyName}: {IsAuthorized}",
            request.PolicyName,
            authResult.Succeeded);

        return Ok(ApiResponse<AuthorizationCheckResponse>.Ok(response));
    }
}
