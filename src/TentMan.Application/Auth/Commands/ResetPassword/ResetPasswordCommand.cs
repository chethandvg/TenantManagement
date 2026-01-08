using TentMan.Application.Common;
using MediatR;

namespace TentMan.Application.Auth.Commands.ResetPassword;

/// <summary>
/// Command to reset a user's password using a valid reset token.
/// </summary>
/// <param name="Email">The user's email address.</param>
/// <param name="Token">The password reset token.</param>
/// <param name="NewPassword">The new password.</param>
/// <param name="ConfirmPassword">Confirmation of the new password.</param>
public sealed record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmPassword) : IRequest<Result>;
