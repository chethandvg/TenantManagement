using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Auth.Commands.ConfirmEmail;

/// <summary>
/// Command to confirm a user's email address using a confirmation token.
/// </summary>
public sealed record ConfirmEmailCommand : IRequest<Result>
{
    /// <summary>
    /// The user's unique identifier.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// The email confirmation token.
    /// </summary>
    public string Token { get; init; } = string.Empty;
}
