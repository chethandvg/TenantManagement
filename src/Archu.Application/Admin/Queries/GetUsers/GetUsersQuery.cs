using Archu.Contracts.Admin;
using MediatR;

namespace Archu.Application.Admin.Queries.GetUsers;

/// <summary>
/// Query to retrieve all users in the system.
/// </summary>
public record GetUsersQuery(
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<IEnumerable<UserDto>>;
