using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Auth.Commands.ForgotPassword;

/// <summary>
/// Command to initiate password reset by sending a reset token to the user's email.
/// </summary>
public sealed record ForgotPasswordCommand : IRequest<Result>
{
    /// <summary>
    /// The user's email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;
}
