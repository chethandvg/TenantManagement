using Archu.Application.Abstractions.Authentication;
using Archu.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Auth.Commands.ResetPassword;

/// <summary>
/// Handler for password reset command.
/// </summary>
public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IAuthenticationService authenticationService,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password reset attempt for email: {Email}", request.Email);

        var result = await _authenticationService.ResetPasswordAsync(
            request.Email,
            request.Token,
            request.NewPassword,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "Password reset failed for email: {Email}. Error: {Error}",
                request.Email,
                result.Error);
            
            return result;
        }

        _logger.LogInformation("Password reset successfully for email: {Email}", request.Email);

        return result;
    }
}
