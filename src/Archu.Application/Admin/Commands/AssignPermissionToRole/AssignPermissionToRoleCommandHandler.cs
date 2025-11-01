using System.Collections.Generic;
using System.Linq;
using Archu.Application.Abstractions;
using Archu.Contracts.Admin;
using Archu.Domain.Entities.Identity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Admin.Commands.AssignPermissionToRole;

/// <summary>
/// Handles assigning permissions to a role while enforcing data integrity rules.
/// </summary>
public sealed class AssignPermissionToRoleCommandHandler : IRequestHandler<AssignPermissionToRoleCommand, RolePermissionsDto>
{
    private static readonly StringComparer PermissionComparer = StringComparer.Ordinal;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssignPermissionToRoleCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the handler responsible for role permission assignments.
    /// </summary>
    public AssignPermissionToRoleCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<AssignPermissionToRoleCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Assigns the requested permissions to the specified role after validating existence and duplicates.
    /// </summary>
    public async Task<RolePermissionsDto> Handle(AssignPermissionToRoleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Assigning permissions {@PermissionNames} to role {RoleId}",
            request.PermissionNames,
            request.RoleId);

        var normalizedPermissionNames = NormalizePermissionNames(request.PermissionNames);

        var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            _logger.LogWarning("Role {RoleId} not found when assigning permissions", request.RoleId);
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
                "Cannot assign permissions {MissingPermissions} to role {RoleId} because they do not exist",
                missingPermissions,
                request.RoleId);

            throw new InvalidOperationException(
                $"The following permissions do not exist: {string.Join(", ", missingPermissions)}");
        }

        var existingPermissionNames = await _unitOfWork.RolePermissions
            .GetPermissionNamesByRoleIdsAsync(new[] { request.RoleId }, cancellationToken);
        var existingPermissionSet = new HashSet<string>(existingPermissionNames, PermissionComparer);

        var newPermissionNames = normalizedPermissionNames
            .Where(name => !existingPermissionSet.Contains(name))
            .ToList();

        if (newPermissionNames.Count == 0)
        {
            _logger.LogWarning(
                "All requested permissions are already linked to role {RoleId}",
                request.RoleId);

            throw new InvalidOperationException(
                "All requested permissions are already assigned to the role.");
        }

        var permissionIdsToAssign = newPermissionNames
            .Select(name => permissionsByName[name].Id)
            .ToList();

        await _unitOfWork.RolePermissions.LinkPermissionsAsync(role.Id, permissionIdsToAssign, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        existingPermissionSet.UnionWith(newPermissionNames);

        var updatedPermissions = await _unitOfWork.Permissions
            .GetByNormalizedNamesAsync(existingPermissionSet, cancellationToken);
        var permissionDtos = MapToPermissionDtos(updatedPermissions);

        _logger.LogInformation(
            "Assigned {PermissionCount} permissions to role {RoleId}",
            newPermissionNames.Count,
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
    /// Normalizes the supplied permission names to ensure consistent comparisons and storage.
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
    /// Maps permission entities to data transfer objects exposed to clients.
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
