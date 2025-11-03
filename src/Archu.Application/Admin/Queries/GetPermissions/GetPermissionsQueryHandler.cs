using System.Collections.Generic;
using System.Linq;
using Archu.Application.Abstractions;
using Archu.Contracts.Admin;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Admin.Queries.GetPermissions;

/// <summary>
/// Handles retrieving the catalogue of permissions.
/// </summary>
public sealed class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, IEnumerable<PermissionDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetPermissionsQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance for retrieving permission definitions.
    /// </summary>
    public GetPermissionsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetPermissionsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Returns all permissions defined within the system.
    /// </summary>
    public async Task<IEnumerable<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving all permissions");

        var permissions = await _unitOfWork.Permissions.GetAllAsync(cancellationToken);

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
