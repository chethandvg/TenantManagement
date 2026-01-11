using TentMan.Application.Abstractions.Repositories;
using TentMan.Contracts.Billing;
using TentMan.Contracts.Common;
using TentMan.Shared.Constants.Authorization;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers.Billing;

/// <summary>
/// API endpoints for managing charge types.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class ChargeTypesController : ControllerBase
{
    private readonly IChargeTypeRepository _chargeTypeRepository;
    private readonly ILogger<ChargeTypesController> _logger;

    public ChargeTypesController(
        IChargeTypeRepository chargeTypeRepository,
        ILogger<ChargeTypesController> logger)
    {
        _chargeTypeRepository = chargeTypeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets all charge types for an organization (including system-defined ones).
    /// </summary>
    [HttpGet("api/charge-types")]
    [Authorize(Policy = PolicyNames.RequireManagerRole)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ChargeTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ChargeTypeDto>>>> GetChargeTypes(
        [FromQuery] Guid? orgId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting charge types for organization {OrgId}", orgId);

        var chargeTypes = await _chargeTypeRepository.GetByOrgIdAsync(orgId, isActive: true, cancellationToken);

        var dtos = chargeTypes.Select(ct => new ChargeTypeDto
        {
            Id = ct.Id,
            OrgId = ct.OrgId,
            Code = ct.Code,
            Name = ct.Name,
            Description = ct.Description,
            IsActive = ct.IsActive,
            IsSystemDefined = ct.IsSystemDefined,
            IsTaxable = ct.IsTaxable,
            DefaultAmount = ct.DefaultAmount
        }).ToList();

        return Ok(ApiResponse<IEnumerable<ChargeTypeDto>>.Ok(dtos, "Charge types retrieved successfully"));
    }
}
