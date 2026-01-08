using TentMan.Application.Abstractions.Authentication;
using TentMan.Application.Common;
using MediatR;

namespace TentMan.Application.Auth.Commands.RefreshToken;

/// <summary>
/// Command to refresh an expired access token using a valid refresh token.
/// </summary>
/// <param name="RefreshToken">The refresh token.</param>
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthenticationResult>>;
