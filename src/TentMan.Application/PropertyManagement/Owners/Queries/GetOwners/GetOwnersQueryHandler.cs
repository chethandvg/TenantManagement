using TentMan.Application.Abstractions;
using TentMan.Contracts.Owners;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Owners.Queries.GetOwners;

public class GetOwnersQueryHandler : IRequestHandler<GetOwnersQuery, IEnumerable<OwnerDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetOwnersQueryHandler> _logger;

    public GetOwnersQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetOwnersQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<OwnerDto>> Handle(GetOwnersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting owners for organization: {OrgId}", request.OrgId);

        var owners = await _unitOfWork.Owners.GetByOrganizationIdAsync(request.OrgId, cancellationToken);

        return owners.Select(o => new OwnerDto
        {
            Id = o.Id,
            OrgId = o.OrgId,
            OwnerType = o.OwnerType,
            DisplayName = o.DisplayName,
            Phone = o.Phone,
            Email = o.Email,
            Pan = o.Pan,
            Gstin = o.Gstin,
            LinkedUserId = o.LinkedUserId,
            RowVersion = o.RowVersion
        });
    }
}
