using System.Collections.Generic;
using System.Linq;
using Archu.Application.Abstractions;
using Archu.Contracts.Admin;
using Archu.Domain.Entities.Identity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Admin.Queries.GetRolePermissions;

/// <summary>
/// Handles retrieving the permissions assigned to a specific role.
/// </summary>
public sealed class GetRolePermissionsQueryHandler : IRequestHandler<GetRolePermissionsQuery, RolePermissionsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetRolePermissionsQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance that resolves permissions for roles.
    /// </summary>
    public GetRolePermissionsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetRolePermissionsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Returns the permissions currently linked to the requested role.
    /// </summary>
    public async Task<RolePermissionsDto> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving permissions for role {RoleId}", request.RoleId);

        var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            _logger.LogWarning("Role {RoleId} not found when fetching permissions", request.RoleId);
            throw new InvalidOperationException($"Role with ID {request.RoleId} was not found.");
        }

        var normalizedPermissionNames = await _unitOfWork.RolePermissions
            .GetPermissionNamesByRoleIdsAsync(new[] { request.RoleId }, cancellationToken);

        if (normalizedPermissionNames.Count == 0)
        {
            return MapToDto(role, Array.Empty<ApplicationPermission>());
        }

        var permissions = await _unitOfWork.Permissions
            .GetByNormalizedNamesAsync(normalizedPermissionNames, cancellationToken);

        return MapToDto(role, permissions);
    }

    /// <summary>
    /// Creates a <see cref="RolePermissionsDto"/> using the supplied role and permissions.
    /// </summary>
    private static RolePermissionsDto MapToDto(ApplicationRole role, IEnumerable<ApplicationPermission> permissions)
    {
        var permissionDtos = permissions
            .Select(permission => new PermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                NormalizedName = permission.NormalizedName,
                Description = permission.Description,
                CreatedAtUtc = permission.CreatedAtUtc,
                RowVersion = permission.RowVersion
            })
            .OrderBy(dto => dto.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new RolePermissionsDto
        {
            RoleId = role.Id,
            RoleName = role.Name,
            NormalizedRoleName = role.NormalizedName,
            Description = role.Description,
            Permissions = permissionDtos
        };
    }
}
