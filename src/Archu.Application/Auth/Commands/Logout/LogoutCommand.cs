using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Auth.Commands.Logout;

/// <summary>
/// Command to log out a user by revoking their refresh token.
/// </summary>
public sealed record LogoutCommand : IRequest<Result>
{
    /// <summary>
    /// The user's unique identifier (typically from ICurrentUser).
    /// </summary>
    public string UserId { get; init; } = string.Empty;
}
