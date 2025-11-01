using System.Collections.Generic;
using System.Linq;
using Archu.Application.Abstractions;
using Archu.Contracts.Admin;
using Archu.Domain.Entities.Identity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Admin.Queries.GetEffectiveUserPermissions;

/// <summary>
/// Handles computing the effective permissions for a user by combining direct and role-derived grants.
/// </summary>
public sealed class GetEffectiveUserPermissionsQueryHandler : IRequestHandler<GetEffectiveUserPermissionsQuery, EffectiveUserPermissionsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetEffectiveUserPermissionsQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance that calculates effective permissions for users.
    /// </summary>
    public GetEffectiveUserPermissionsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetEffectiveUserPermissionsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Aggregates direct and role-derived permissions for the requested user.
    /// </summary>
    public async Task<EffectiveUserPermissionsDto> Handle(GetEffectiveUserPermissionsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Computing effective permissions for user {UserId}", request.UserId);

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("User {UserId} not found when computing effective permissions", request.UserId);
            throw new InvalidOperationException($"User with ID {request.UserId} was not found.");
        }

        var directPermissionNames = await _unitOfWork.UserPermissions
            .GetPermissionNamesByUserIdAsync(request.UserId, cancellationToken);

        var roles = (await _unitOfWork.Roles.GetUserRolesAsync(request.UserId, cancellationToken))
            .ToList();

        var rolePermissionNames = new Dictionary<Guid, IReadOnlyCollection<string>>();
        var effectivePermissionNames = new HashSet<string>(directPermissionNames, StringComparer.Ordinal);

        foreach (var role in roles)
        {
            var permissionsForRole = await _unitOfWork.RolePermissions
                .GetPermissionNamesByRoleIdsAsync(new[] { role.Id }, cancellationToken);

            rolePermissionNames[role.Id] = permissionsForRole;
            effectivePermissionNames.UnionWith(permissionsForRole);
        }

        IReadOnlyCollection<PermissionDto> effectivePermissions;
        IReadOnlyCollection<PermissionDto> directPermissions;
        IReadOnlyCollection<RolePermissionsDto> rolePermissions;

        if (effectivePermissionNames.Count == 0)
        {
            effectivePermissions = Array.Empty<PermissionDto>();
            directPermissions = Array.Empty<PermissionDto>();
            rolePermissions = roles
                .Select(role => new RolePermissionsDto
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    NormalizedRoleName = role.NormalizedName,
                    Description = role.Description,
                    Permissions = Array.Empty<PermissionDto>()
                })
                .ToArray();
        }
        else
        {
            var permissionEntities = await _unitOfWork.Permissions
                .GetByNormalizedNamesAsync(effectivePermissionNames, cancellationToken);

            var permissionLookup = permissionEntities
                .ToDictionary(permission => permission.NormalizedName, StringComparer.Ordinal);

            effectivePermissions = permissionLookup.Values
                .Select(MapPermission)
                .OrderBy(dto => dto.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            directPermissions = directPermissionNames
                .Select(name => TryGetPermissionDto(name, permissionLookup))
                .Where(dto => dto is not null)
                .Select(dto => dto!)
                .OrderBy(dto => dto.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            rolePermissions = roles
                .Select(role => new RolePermissionsDto
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    NormalizedRoleName = role.NormalizedName,
                    Description = role.Description,
                    Permissions = rolePermissionNames.TryGetValue(role.Id, out var names)
                        ? names
                            .Select(name => TryGetPermissionDto(name, permissionLookup))
                            .Where(dto => dto is not null)
                            .Select(dto => dto!)
                            .OrderBy(dto => dto.Name, StringComparer.OrdinalIgnoreCase)
                            .ToArray()
                        : Array.Empty<PermissionDto>()
                })
                .ToArray();
        }

        _logger.LogInformation(
            "Computed {EffectiveCount} effective permissions for user {UserId}",
            effectivePermissions.Count,
            request.UserId);

        return new EffectiveUserPermissionsDto
        {
            UserId = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            DirectPermissions = directPermissions,
            RolePermissions = rolePermissions,
            EffectivePermissions = effectivePermissions
        };
    }

    /// <summary>
    /// Maps an <see cref="ApplicationPermission"/> to a DTO.
    /// </summary>
    private static PermissionDto MapPermission(ApplicationPermission permission)
    {
        return new PermissionDto
        {
            Id = permission.Id,
            Name = permission.Name,
            NormalizedName = permission.NormalizedName,
            Description = permission.Description,
            CreatedAtUtc = permission.CreatedAtUtc,
            RowVersion = permission.RowVersion
        };
    }

    /// <summary>
    /// Attempts to resolve a permission DTO from the lookup dictionary.
    /// </summary>
    private static PermissionDto? TryGetPermissionDto(
        string normalizedName,
        IReadOnlyDictionary<string, ApplicationPermission> lookup)
    {
        return lookup.TryGetValue(normalizedName, out var permission)
            ? MapPermission(permission)
            : null;
    }
}
