using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Admin.Commands.DeleteUser;

/// <summary>
/// Command to delete a user from the system (soft delete).
/// </summary>
/// <param name="UserId">The user's unique identifier.</param>
public record DeleteUserCommand(
    Guid UserId
) : IRequest<Result>;
