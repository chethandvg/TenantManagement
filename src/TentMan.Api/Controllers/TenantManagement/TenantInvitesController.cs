using TentMan.Application.TenantManagement.TenantInvites.Commands.GenerateInvite;
using TentMan.Application.TenantManagement.TenantInvites.Commands.AcceptInvite;
using TentMan.Application.TenantManagement.TenantInvites.Commands.CancelInvite;
using TentMan.Application.TenantManagement.TenantInvites.Queries.ValidateInvite;
using TentMan.Application.TenantManagement.TenantInvites.Queries.GetInvitesByTenant;
using TentMan.Contracts.Common;
using TentMan.Contracts.TenantInvites;
using TentMan.Contracts.Authentication;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers.TenantManagement;

/// <summary>
/// API endpoints for managing tenant invites.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
public class TenantInvitesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TenantInvitesController> _logger;

    public TenantInvitesController(IMediator mediator, ILogger<TenantInvitesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Generates an invite for a tenant.
    /// </summary>
    [HttpPost("api/v{version:apiVersion}/organizations/{orgId}/tenants/{tenantId}/invites")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TenantInviteDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TenantInviteDto>>> GenerateInvite(
        Guid orgId,
        Guid tenantId,
        GenerateInviteRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating invite for tenant {TenantId} in organization {OrgId}", tenantId, orgId);

        var command = new GenerateInviteCommand(
            orgId,
            tenantId,
            request.ExpiryDays);

        try
        {
            var invite = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(
                nameof(ValidateInvite),
                new { token = invite.InviteToken, version = "1.0" },
                ApiResponse<TenantInviteDto>.Ok(invite, "Invite generated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Validates an invite token.
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/invites/validate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ValidateInviteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ValidateInviteResponse>>> ValidateInvite(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating invite token");

        var query = new ValidateInviteQuery(token);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<ValidateInviteResponse>.Ok(result, "Invite validated"));
    }

    /// <summary>
    /// Accepts an invite and creates a user account.
    /// </summary>
    [HttpPost("api/v{version:apiVersion}/invites/accept")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthenticationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthenticationResponse>>> AcceptInvite(
        AcceptInviteRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Accepting invite with token");

        var command = new AcceptInviteCommand(
            request.InviteToken,
            request.UserName,
            request.Email,
            request.Password);

        try
        {
            var authResponse = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<AuthenticationResponse>.Ok(authResponse, "Invite accepted and user created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Gets all invites for a tenant.
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/organizations/{orgId}/tenants/{tenantId}/invites")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TenantInviteDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<TenantInviteDto>>>> GetInvitesByTenant(
        Guid orgId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching invites for tenant {TenantId} in organization {OrgId}", tenantId, orgId);

        var query = new GetInvitesByTenantQuery(orgId, tenantId);

        try
        {
            var invites = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<IEnumerable<TenantInviteDto>>.Ok(invites, "Invites retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Cancels an invite.
    /// </summary>
    [HttpDelete("api/v{version:apiVersion}/organizations/{orgId}/invites/{inviteId}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> CancelInvite(
        Guid orgId,
        Guid inviteId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Canceling invite {InviteId} in organization {OrgId}", inviteId, orgId);

        var command = new CancelInviteCommand(orgId, inviteId);

        try
        {
            await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<object>.Ok(null, "Invite canceled successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
