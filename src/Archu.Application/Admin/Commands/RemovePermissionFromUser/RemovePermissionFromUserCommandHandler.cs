using System.Collections.Generic;
using System.Linq;
using Archu.Application.Abstractions;
using Archu.Contracts.Admin;
using Archu.Domain.Entities.Identity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Admin.Commands.RemovePermissionFromUser;

/// <summary>
/// Handles revoking permissions from a user.
/// </summary>
public sealed class RemovePermissionFromUserCommandHandler : IRequestHandler<RemovePermissionFromUserCommand, UserPermissionsDto>
{
    private static readonly StringComparer PermissionComparer = StringComparer.Ordinal;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemovePermissionFromUserCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance that revokes direct permissions from users.
    /// </summary>
    public RemovePermissionFromUserCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<RemovePermissionFromUserCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Revokes the requested permissions from the user after validating existence and current assignments.
    /// </summary>
    public async Task<UserPermissionsDto> Handle(RemovePermissionFromUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Removing permissions {@PermissionNames} from user {UserId}",
            request.PermissionNames,
            request.UserId);

        var normalizedPermissionNames = NormalizePermissionNames(request.PermissionNames);

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("User {UserId} not found when removing permissions", request.UserId);
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
                "Cannot remove permissions {MissingPermissions} from user {UserId} because they do not exist",
                missingPermissions,
                request.UserId);

            throw new InvalidOperationException(
                $"The following permissions do not exist: {string.Join(", ", missingPermissions)}");
        }

        var existingPermissionNames = await _unitOfWork.UserPermissions
            .GetPermissionNamesByUserIdAsync(request.UserId, cancellationToken);
        var existingPermissionSet = new HashSet<string>(existingPermissionNames, PermissionComparer);

        var permissionsToRemove = normalizedPermissionNames
            .Where(name => existingPermissionSet.Contains(name))
            .ToList();

        if (permissionsToRemove.Count == 0)
        {
            _logger.LogWarning(
                "None of the requested permissions are assigned to user {UserId}",
                request.UserId);

            throw new InvalidOperationException(
                "None of the requested permissions are currently assigned to the user.");
        }

        var permissionIdsToRemove = permissionsToRemove
            .Select(name => permissionsByName[name].Id)
            .ToList();

        await _unitOfWork.UserPermissions.UnlinkPermissionsAsync(user.Id, permissionIdsToRemove, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        existingPermissionSet.ExceptWith(permissionsToRemove);

        var remainingPermissions = await _unitOfWork.Permissions
            .GetByNormalizedNamesAsync(existingPermissionSet, cancellationToken);
        var permissionDtos = MapToPermissionDtos(remainingPermissions);

        _logger.LogInformation(
            "Removed {PermissionCount} permissions from user {UserId}",
            permissionsToRemove.Count,
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
    /// Normalizes the provided permission names so comparisons remain consistent.
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
    /// Maps permission entities to DTOs for clients.
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
