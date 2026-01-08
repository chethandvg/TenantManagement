using TentMan.Application.Abstractions.Authentication;
using TentMan.Application.Common;
using MediatR;

namespace TentMan.Application.Auth.Commands.Register;

/// <summary>
/// Command to register a new user in the system.
/// </summary>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The user's password.</param>
/// <param name="ConfirmPassword">Confirmation of the password.</param>
/// <param name="UserName">The user's username.</param>
/// <param name="PhoneNumber">Optional phone number.</param>
public sealed record RegisterCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    string UserName,
    string? PhoneNumber = null) : IRequest<Result<AuthenticationResult>>;
