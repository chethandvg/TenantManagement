using Archu.Application.Abstractions.Authentication;
using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Auth.Commands.Login;

/// <summary>
/// Command to authenticate a user and obtain access tokens.
/// </summary>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The user's password.</param>
/// <param name="RememberMe">Optional flag to remember the user (extend refresh token lifetime).</param>
public sealed record LoginCommand(
    string Email,
    string Password,
    bool RememberMe = false) : IRequest<Result<AuthenticationResult>>;
