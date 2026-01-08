using TentMan.Application.Abstractions.Authentication;
using TentMan.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.Auth.Commands.ConfirmEmail;

/// <summary>
/// Handler for email confirmation command.
/// </summary>
public sealed class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, Result>
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<ConfirmEmailCommandHandler> _logger;

    public ConfirmEmailCommandHandler(
        IAuthenticationService authenticationService,
        ILogger<ConfirmEmailCommandHandler> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    public async Task<Result> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email confirmation attempt for user: {UserId}", request.UserId);

        var result = await _authenticationService.ConfirmEmailAsync(
            request.UserId,
            request.Token,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "Email confirmation failed for user: {UserId}. Error: {Error}",
                request.UserId,
                result.Error);
            
            return result;
        }

        _logger.LogInformation("Email confirmed successfully for user: {UserId}", request.UserId);

        return result;
    }
}
