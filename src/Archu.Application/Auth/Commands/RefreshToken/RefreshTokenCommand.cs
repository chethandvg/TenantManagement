using Archu.Application.Abstractions.Authentication;
using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Auth.Commands.RefreshToken;

/// <summary>
/// Command to refresh an expired access token using a valid refresh token.
/// </summary>
public sealed record RefreshTokenCommand : IRequest<Result<AuthenticationResult>>
{
    /// <summary>
    /// The refresh token.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;
}
