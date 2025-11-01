using System.Collections.Generic;
using System.Linq;
using Archu.Application.Abstractions;
using Archu.Contracts.Admin;
using Archu.Domain.Entities.Identity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Admin.Queries.GetUserDirectPermissions;

/// <summary>
/// Handles retrieving the permissions assigned directly to a user.
/// </summary>
public sealed class GetUserDirectPermissionsQueryHandler : IRequestHandler<GetUserDirectPermissionsQuery, UserPermissionsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetUserDirectPermissionsQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance for retrieving user-specific permission assignments.
    /// </summary>
    public GetUserDirectPermissionsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetUserDirectPermissionsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Returns the permissions that are linked directly to the requested user.
    /// </summary>
    public async Task<UserPermissionsDto> Handle(GetUserDirectPermissionsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving direct permissions for user {UserId}", request.UserId);

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("User {UserId} not found when fetching direct permissions", request.UserId);
            throw new InvalidOperationException($"User with ID {request.UserId} was not found.");
        }

        var normalizedPermissionNames = await _unitOfWork.UserPermissions
            .GetPermissionNamesByUserIdAsync(request.UserId, cancellationToken);

        if (normalizedPermissionNames.Count == 0)
        {
            return MapToDto(user, Array.Empty<ApplicationPermission>());
        }

        var permissions = await _unitOfWork.Permissions
            .GetByNormalizedNamesAsync(normalizedPermissionNames, cancellationToken);

        return MapToDto(user, permissions);
    }

    /// <summary>
    /// Converts a user and permission collection into a <see cref="UserPermissionsDto"/>.
    /// </summary>
    private static UserPermissionsDto MapToDto(ApplicationUser user, IEnumerable<ApplicationPermission> permissions)
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

        return new UserPermissionsDto
        {
            UserId = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Permissions = permissionDtos
        };
    }
}
