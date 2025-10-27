using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Auth.Commands.ChangePassword;

/// <summary>
/// Command to change a user's password (requires current password verification).
/// </summary>
/// <param name="UserId">The user's unique identifier (typically from ICurrentUser).</param>
/// <param name="CurrentPassword">The current password.</param>
/// <param name="NewPassword">The new password.</param>
/// <param name="ConfirmPassword">Confirmation of the new password.</param>
public sealed record ChangePasswordCommand(
    string? UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword) : IRequest<Result>;
