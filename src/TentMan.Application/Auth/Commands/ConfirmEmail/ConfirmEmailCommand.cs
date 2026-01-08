using TentMan.Application.Common;
using MediatR;

namespace TentMan.Application.Auth.Commands.ConfirmEmail;

/// <summary>
/// Command to confirm a user's email address using a confirmation token.
/// </summary>
/// <param name="UserId">The user's unique identifier.</param>
/// <param name="Token">The email confirmation token.</param>
public sealed record ConfirmEmailCommand(
    string UserId,
    string Token) : IRequest<Result>;
