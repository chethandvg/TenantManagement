using Archu.Application.Abstractions.Authentication;
using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Auth.Commands.RefreshToken;

/// <summary>
/// Command to refresh an expired access token using a valid refresh token.
/// </summary>
/// <param name="RefreshToken">The refresh token.</param>
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthenticationResult>>;
