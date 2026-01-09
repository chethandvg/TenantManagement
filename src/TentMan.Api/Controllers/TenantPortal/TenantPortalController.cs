using TentMan.Application.TenantManagement.TenantPortal.Queries;
using TentMan.Contracts.Common;
using TentMan.Contracts.TenantPortal;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TentMan.Api.Controllers.TenantPortal;

/// <summary>
/// API endpoints for the tenant portal.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = "Tenant")]
public class TenantPortalController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TenantPortalController> _logger;

    public TenantPortalController(IMediator mediator, ILogger<TenantPortalController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current tenant's active lease summary.
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/tenant-portal/lease-summary")]
    [ProducesResponseType(typeof(ApiResponse<TenantLeaseSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TenantLeaseSummaryResponse>>> GetLeaseSummary(
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Invalid user ID claim for tenant lease summary");
            return Unauthorized(ApiResponse<object>.Fail("Invalid user authentication"));
        }

        _logger.LogInformation("Getting lease summary for user {UserId}", userId);

        var query = new GetTenantLeaseByUserIdQuery(userId);
        var leaseSummary = await _mediator.Send(query, cancellationToken);

        if (leaseSummary == null)
        {
            return NotFound(ApiResponse<object>.Fail("No active lease found for the current tenant"));
        }

        return Ok(ApiResponse<TenantLeaseSummaryResponse>.Ok(leaseSummary, "Lease summary retrieved successfully"));
    }
}
