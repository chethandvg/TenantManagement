using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Auth.Commands.Logout;

/// <summary>
/// Command to log out a user by revoking their refresh token.
/// </summary>
/// <param name="UserId">The user's unique identifier (typically from ICurrentUser).</param>
public sealed record LogoutCommand(string? UserId) : IRequest<Result>;
