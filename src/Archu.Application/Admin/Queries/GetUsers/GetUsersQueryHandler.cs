using Archu.Application.Abstractions;
using Archu.Contracts.Admin;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Admin.Queries.GetUsers;

/// <summary>
/// Handles the retrieval of all users with pagination.
/// </summary>
public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IEnumerable<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetUsersQueryHandler> _logger;

    public GetUsersQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetUsersQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Retrieving users (Page: {PageNumber}, PageSize: {PageSize})",
            request.PageNumber,
            request.PageSize);

        var users = await _unitOfWork.Users.GetAllAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _unitOfWork.Roles.GetUserRolesAsync(user.Id, cancellationToken);

            userDtos.Add(new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                TwoFactorEnabled = user.TwoFactorEnabled,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                CreatedAtUtc = user.CreatedAtUtc,
                Roles = roles.Select(r => r.Name).ToList(),
                RowVersion = user.RowVersion
            });
        }

        return userDtos;
    }
}
