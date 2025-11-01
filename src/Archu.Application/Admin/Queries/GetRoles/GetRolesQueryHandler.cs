using Archu.Application.Abstractions;
using Archu.Contracts.Admin;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Admin.Queries.GetRoles;

/// <summary>
/// Handles the retrieval of all roles.
/// </summary>
public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IEnumerable<RoleDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetRolesQueryHandler> _logger;

    public GetRolesQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetRolesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving all roles");

        var roles = await _unitOfWork.Roles.GetAllAsync(cancellationToken);

        return roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            NormalizedName = r.NormalizedName,
            Description = r.Description,
            CreatedAtUtc = r.CreatedAtUtc,
            RowVersion = r.RowVersion
        });
    }
}
