using TentMan.Contracts.Admin;
using MediatR;

namespace TentMan.Application.Admin.Commands.CreateUser;

/// <summary>
/// Command to create a new user in the system.
/// </summary>
public record CreateUserCommand(
    string UserName,
    string Email,
    string Password,
    string? PhoneNumber,
    bool EmailConfirmed,
    bool TwoFactorEnabled
) : IRequest<UserDto>;
