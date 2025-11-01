using System.Collections.Generic;
using System.Linq;
using Archu.Application.Abstractions;
using Archu.Contracts.Admin;
using Archu.Domain.Entities.Identity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Admin.Commands.RemovePermissionFromRole;

/// <summary>
/// Handles removing permissions from a role while ensuring data consistency.
/// </summary>
public sealed class RemovePermissionFromRoleCommandHandler : IRequestHandler<RemovePermissionFromRoleCommand, RolePermissionsDto>
{
    private static readonly StringComparer PermissionComparer = StringComparer.Ordinal;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemovePermissionFromRoleCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance responsible for removing permissions from roles.
    /// </summary>
    public RemovePermissionFromRoleCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<RemovePermissionFromRoleCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Removes the specified permissions from the role after validating existence and current assignments.
    /// </summary>
    public async Task<RolePermissionsDto> Handle(RemovePermissionFromRoleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Removing permissions {@PermissionNames} from role {RoleId}",
            request.PermissionNames,
            request.RoleId);

        var normalizedPermissionNames = NormalizePermissionNames(request.PermissionNames);

        var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            _logger.LogWarning("Role {RoleId} not found when removing permissions", request.RoleId);
            throw new InvalidOperationException($"Role with ID {request.RoleId} was not found.");
        }

        var permissions = await _unitOfWork.Permissions.GetByNormalizedNamesAsync(normalizedPermissionNames, cancellationToken);
        var permissionsByName = permissions.ToDictionary(permission => permission.NormalizedName, PermissionComparer);

        var missingPermissions = normalizedPermissionNames
            .Where(name => !permissionsByName.ContainsKey(name))
            .ToList();

        if (missingPermissions.Count > 0)
        {
            _logger.LogWarning(
                "Cannot remove permissions {MissingPermissions} from role {RoleId} because they do not exist",
                missingPermissions,
                request.RoleId);

            throw new InvalidOperationException(
                $"The following permissions do not exist: {string.Join(", ", missingPermissions)}");
        }

        var existingPermissionNames = await _unitOfWork.RolePermissions
            .GetPermissionNamesByRoleIdsAsync(new[] { request.RoleId }, cancellationToken);
        var existingPermissionSet = new HashSet<string>(existingPermissionNames, PermissionComparer);

        var permissionsToRemove = normalizedPermissionNames
            .Where(name => existingPermissionSet.Contains(name))
            .ToList();

        if (permissionsToRemove.Count == 0)
        {
            _logger.LogWarning(
                "None of the requested permissions are assigned to role {RoleId}",
                request.RoleId);

            throw new InvalidOperationException(
                "None of the requested permissions are currently assigned to the role.");
        }

        var permissionIdsToRemove = permissionsToRemove
            .Select(name => permissionsByName[name].Id)
            .ToList();

        await _unitOfWork.RolePermissions.UnlinkPermissionsAsync(role.Id, permissionIdsToRemove, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        existingPermissionSet.ExceptWith(permissionsToRemove);

        var remainingPermissions = await _unitOfWork.Permissions
            .GetByNormalizedNamesAsync(existingPermissionSet, cancellationToken);
        var permissionDtos = MapToPermissionDtos(remainingPermissions);

        _logger.LogInformation(
            "Removed {PermissionCount} permissions from role {RoleId}",
            permissionsToRemove.Count,
            request.RoleId);

        return new RolePermissionsDto
        {
            RoleId = role.Id,
            RoleName = role.Name,
            NormalizedRoleName = role.NormalizedName,
            Description = role.Description,
            Permissions = permissionDtos
        };
    }

    /// <summary>
    /// Normalizes the supplied permission names so comparisons are performed consistently.
    /// </summary>
    private static IReadOnlyCollection<string> NormalizePermissionNames(IEnumerable<string> permissionNames)
    {
        return permissionNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim().ToUpperInvariant())
            .Distinct(PermissionComparer)
            .ToArray();
    }

    /// <summary>
    /// Maps permission entities to DTOs for client consumption.
    /// </summary>
    private static IReadOnlyCollection<PermissionDto> MapToPermissionDtos(IEnumerable<ApplicationPermission> permissions)
    {
        return permissions
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
    }
}
