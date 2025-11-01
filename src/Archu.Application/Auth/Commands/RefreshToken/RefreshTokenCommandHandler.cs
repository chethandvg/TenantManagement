using Archu.Application.Abstractions.Authentication;
using Archu.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Auth.Commands.RefreshToken;

/// <summary>
/// Handler for refresh token command.
/// </summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthenticationResult>>
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IAuthenticationService authenticationService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    public async Task<Result<AuthenticationResult>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to refresh access token");

        var result = await _authenticationService.RefreshTokenAsync(
            request.RefreshToken,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Token refresh failed: {Error}", result.Error);
            return Result<AuthenticationResult>.Failure("Invalid or expired refresh token");
        }

        _logger.LogInformation("Access token refreshed successfully for user: {UserId}", 
            result.Value!.User.Id);

        return result;
    }
}
