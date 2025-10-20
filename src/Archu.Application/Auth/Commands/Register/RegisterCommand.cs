using Archu.Application.Abstractions.Authentication;
using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Auth.Commands.Register;

/// <summary>
/// Command to register a new user in the system.
/// </summary>
public sealed record RegisterCommand : IRequest<Result<AuthenticationResult>>
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
    /// Confirmation of the password.
    /// </summary>
    public string ConfirmPassword { get; init; } = string.Empty;

    /// <summary>
    /// The user's username.
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// Optional phone number.
    /// </summary>
    public string? PhoneNumber { get; init; }
}
