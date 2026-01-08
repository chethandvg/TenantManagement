using TentMan.Contracts.Owners;
using MediatR;

namespace TentMan.Application.PropertyManagement.Owners.Queries.GetOwners;

public record GetOwnersQuery(Guid OrgId) : IRequest<IEnumerable<OwnerDto>>;
