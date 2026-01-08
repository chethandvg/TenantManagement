using TentMan.Application.Abstractions.Authentication;
using TentMan.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.Auth.Commands.ForgotPassword;

/// <summary>
/// Handler for forgot password command.
/// </summary>
public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IAuthenticationService authenticationService,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password reset requested for email: {Email}", request.Email);

        // Call authentication service (internally handles non-existent users securely)
        var result = await _authenticationService.ForgotPasswordAsync(request.Email, cancellationToken);

        if (!result.IsSuccess)
        {
            // Log the actual error for operational visibility
            _logger.LogError(
                "Password reset failed for email: {Email}. Error: {Error}",
                request.Email,
                result.Error);

            // Return generic error to prevent email enumeration
            // but surface operational failures (email service down, database errors, etc.)
            return Result.Failure("Unable to process password reset request at this time. Please try again later.");
        }

        _logger.LogInformation("Password reset email processed successfully for: {Email}", request.Email);

        // Return success - could be actual success or non-existent user (handled by service)
        return Result.Success();
    }
}
