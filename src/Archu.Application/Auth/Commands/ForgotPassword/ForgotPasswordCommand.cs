using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Auth.Commands.ForgotPassword;

/// <summary>
/// Command to initiate password reset by sending a reset token to the user's email.
/// </summary>
/// <param name="Email">The user's email address.</param>
public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;
