using Archu.Application.Abstractions.Authentication;
using Archu.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Auth.Commands.ForgotPassword;

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
        await _authenticationService.ForgotPasswordAsync(request.Email, cancellationToken);

        _logger.LogInformation("Password reset email processed for: {Email}", request.Email);

        // Always return success to prevent email enumeration attacks
        return Result.Success();
    }
}
