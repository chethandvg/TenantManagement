using TentMan.Application.TenantManagement.Tenants.Commands.CreateTenant;
using TentMan.Application.TenantManagement.Tenants.Commands.UpdateTenant;
using TentMan.Application.TenantManagement.Tenants.Queries;
using TentMan.Contracts.Common;
using TentMan.Contracts.Tenants;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers.TenantManagement;

/// <summary>
/// API endpoints for managing tenants.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(IMediator mediator, ILogger<TenantsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    [HttpPost("api/v{version:apiVersion}/organizations/{orgId}/tenants")]
    [ProducesResponseType(typeof(ApiResponse<TenantListDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TenantListDto>>> CreateTenant(
        Guid orgId,
        CreateTenantRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating tenant {FullName} for organization {OrgId}", request.FullName, orgId);

        var command = new CreateTenantCommand(
            orgId,
            request.FullName,
            request.Phone,
            request.Email,
            request.DateOfBirth,
            request.Gender);

        try
        {
            var tenant = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(
                nameof(GetTenants),
                new { orgId, version = "1.0" },
                ApiResponse<TenantListDto>.Ok(tenant, "Tenant created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Gets all tenants for an organization.
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/organizations/{orgId}/tenants")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TenantListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<TenantListDto>>>> GetTenants(
        Guid orgId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting tenants for organization {OrgId}", orgId);

        var query = new GetTenantsQuery(orgId, search);
        var tenants = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<IEnumerable<TenantListDto>>.Ok(tenants, "Tenants retrieved successfully"));
    }

    /// <summary>
    /// Gets a tenant by ID.
    /// </summary>
    [HttpGet("api/tenants/{tenantId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TenantDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TenantDetailDto>>> GetTenant(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting tenant {TenantId}", tenantId);

        var query = new GetTenantByIdQuery(tenantId);
        var tenant = await _mediator.Send(query, cancellationToken);

        if (tenant == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Tenant {tenantId} not found"));
        }

        return Ok(ApiResponse<TenantDetailDto>.Ok(tenant, "Tenant retrieved successfully"));
    }

    /// <summary>
    /// Updates a tenant.
    /// </summary>
    [HttpPut("api/tenants/{tenantId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TenantDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TenantDetailDto>>> UpdateTenant(
        Guid tenantId,
        UpdateTenantRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating tenant {TenantId}", tenantId);

        var command = new UpdateTenantCommand(
            tenantId,
            request.FullName,
            request.Phone,
            request.Email,
            request.DateOfBirth,
            request.Gender,
            request.IsActive,
            request.RowVersion);

        try
        {
            var tenant = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<TenantDetailDto>.Ok(tenant, "Tenant updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
