using TentMan.Application.Abstractions;
using TentMan.Contracts.Common;
using TentMan.Domain.Entities;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TentMan.Api.Controllers;

/// <summary>
/// API endpoints for querying audit logs.
/// Only accessible to Admin and Manager roles.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = "Administrator,SuperAdmin,Manager")]
[Route("api/v{version:apiVersion}/audit-logs")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(
        IAuditLogRepository auditLogRepository,
        ILogger<AuditLogsController> logger)
    {
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets audit logs for a specific entity.
    /// </summary>
    /// <param name="entityType">The entity type (e.g., "Lease", "LeaseTerm", "DepositTransaction").</param>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 100, max: 1000).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("entity/{entityType}/{entityId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AuditLog>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IEnumerable<AuditLog>>>> GetAuditLogsByEntity(
        string entityType,
        Guid entityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting audit logs for {EntityType} {EntityId}", entityType, entityId);

        var logs = await _auditLogRepository.GetByEntityAsync(entityType, entityId, page, pageSize, cancellationToken);

        return Ok(ApiResponse<IEnumerable<AuditLog>>.Ok(logs, $"Audit logs for {entityType} retrieved successfully"));
    }

    /// <summary>
    /// Gets audit logs for an organization within an optional date range.
    /// </summary>
    /// <param name="orgId">The organization ID.</param>
    /// <param name="startDate">Optional start date filter (ISO 8601 format).</param>
    /// <param name="endDate">Optional end date filter (ISO 8601 format).</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 100, max: 1000).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("organization/{orgId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AuditLog>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IEnumerable<AuditLog>>>> GetAuditLogsByOrganization(
        Guid orgId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting audit logs for organization {OrgId}", orgId);

        var logs = await _auditLogRepository.GetByOrganizationAsync(orgId, startDate, endDate, page, pageSize, cancellationToken);

        return Ok(ApiResponse<IEnumerable<AuditLog>>.Ok(logs, "Audit logs for organization retrieved successfully"));
    }

    /// <summary>
    /// Gets audit logs for a specific user within an optional date range.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="startDate">Optional start date filter (ISO 8601 format).</param>
    /// <param name="endDate">Optional end date filter (ISO 8601 format).</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 100, max: 1000).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AuditLog>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IEnumerable<AuditLog>>>> GetAuditLogsByUser(
        Guid userId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting audit logs for user {UserId}", userId);

        var logs = await _auditLogRepository.GetByUserAsync(userId, startDate, endDate, page, pageSize, cancellationToken);

        return Ok(ApiResponse<IEnumerable<AuditLog>>.Ok(logs, "Audit logs for user retrieved successfully"));
    }
}
