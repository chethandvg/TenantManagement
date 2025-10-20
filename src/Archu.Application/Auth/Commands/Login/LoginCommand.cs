using Archu.Application.Abstractions.Authentication;
using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Auth.Commands.Login;

/// <summary>
/// Command to authenticate a user and obtain access tokens.
/// </summary>
public sealed record LoginCommand : IRequest<Result<AuthenticationResult>>
{
    /// <summary>
    /// The user's email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// The user's password.
    /// </summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Optional flag to remember the user (extend refresh token lifetime).
    /// </summary>
    public bool RememberMe { get; init; }
}
