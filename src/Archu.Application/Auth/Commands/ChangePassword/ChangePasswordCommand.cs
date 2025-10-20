using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Auth.Commands.ChangePassword;

/// <summary>
/// Command to change a user's password (requires current password verification).
/// </summary>
public sealed record ChangePasswordCommand : IRequest<Result>
{
    /// <summary>
    /// The user's unique identifier (typically from ICurrentUser).
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// The current password.
    /// </summary>
    public string CurrentPassword { get; init; } = string.Empty;

    /// <summary>
    /// The new password.
    /// </summary>
    public string NewPassword { get; init; } = string.Empty;

    /// <summary>
    /// Confirmation of the new password.
    /// </summary>
    public string ConfirmPassword { get; init; } = string.Empty;
}
