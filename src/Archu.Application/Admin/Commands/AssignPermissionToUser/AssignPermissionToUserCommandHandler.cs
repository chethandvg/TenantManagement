using System.Collections.Generic;
using System.Linq;
using Archu.Application.Abstractions;
using Archu.Contracts.Admin;
using Archu.Domain.Entities.Identity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Admin.Commands.AssignPermissionToUser;

/// <summary>
/// Handles assigning permissions directly to a user.
/// </summary>
public sealed class AssignPermissionToUserCommandHandler : IRequestHandler<AssignPermissionToUserCommand, UserPermissionsDto>
{
    private static readonly StringComparer PermissionComparer = StringComparer.Ordinal;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssignPermissionToUserCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance that manages direct user permission assignments.
    /// </summary>
    public AssignPermissionToUserCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<AssignPermissionToUserCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Assigns the requested permissions to the user after validating existence and duplicates.
    /// </summary>
    public async Task<UserPermissionsDto> Handle(AssignPermissionToUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Assigning permissions {@PermissionNames} to user {UserId}",
            request.PermissionNames,
            request.UserId);

        var normalizedPermissionNames = NormalizePermissionNames(request.PermissionNames);

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("User {UserId} not found when assigning permissions", request.UserId);
            throw new InvalidOperationException($"User with ID {request.UserId} was not found.");
        }

        var permissions = await _unitOfWork.Permissions.GetByNormalizedNamesAsync(normalizedPermissionNames, cancellationToken);
        var permissionsByName = permissions.ToDictionary(permission => permission.NormalizedName, PermissionComparer);

        var missingPermissions = normalizedPermissionNames
            .Where(name => !permissionsByName.ContainsKey(name))
            .ToList();

        if (missingPermissions.Count > 0)
        {
            _logger.LogWarning(
                "Cannot assign permissions {MissingPermissions} to user {UserId} because they do not exist",
                missingPermissions,
                request.UserId);

            throw new InvalidOperationException(
                $"The following permissions do not exist: {string.Join(", ", missingPermissions)}");
        }

        var existingPermissionNames = await _unitOfWork.UserPermissions
            .GetPermissionNamesByUserIdAsync(request.UserId, cancellationToken);
        var existingPermissionSet = new HashSet<string>(existingPermissionNames, PermissionComparer);

        var newPermissionNames = normalizedPermissionNames
            .Where(name => !existingPermissionSet.Contains(name))
            .ToList();

        if (newPermissionNames.Count == 0)
        {
            _logger.LogWarning(
                "All requested permissions are already linked to user {UserId}",
                request.UserId);

            throw new InvalidOperationException(
                "All requested permissions are already assigned to the user.");
        }

        var permissionIdsToAssign = newPermissionNames
            .Select(name => permissionsByName[name].Id)
            .ToList();

        await _unitOfWork.UserPermissions.LinkPermissionsAsync(user.Id, permissionIdsToAssign, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        existingPermissionSet.UnionWith(newPermissionNames);

        var updatedPermissions = await _unitOfWork.Permissions
            .GetByNormalizedNamesAsync(existingPermissionSet, cancellationToken);
        var permissionDtos = MapToPermissionDtos(updatedPermissions);

        _logger.LogInformation(
            "Assigned {PermissionCount} permissions to user {UserId}",
            newPermissionNames.Count,
            request.UserId);

        return new UserPermissionsDto
        {
            UserId = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Permissions = permissionDtos
        };
    }

    /// <summary>
    /// Normalizes the provided permission names for consistent comparison.
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
    /// Maps permission entities to DTOs.
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
