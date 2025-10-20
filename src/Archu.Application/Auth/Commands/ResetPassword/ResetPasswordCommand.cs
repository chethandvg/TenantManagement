using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Auth.Commands.ResetPassword;

/// <summary>
/// Command to reset a user's password using a valid reset token.
/// </summary>
public sealed record ResetPasswordCommand : IRequest<Result>
{
    /// <summary>
    /// The user's email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// The password reset token.
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// The new password.
    /// </summary>
    public string NewPassword { get; init; } = string.Empty;

    /// <summary>
    /// Confirmation of the new password.
    /// </summary>
    public string ConfirmPassword { get; init; } = string.Empty;
}
