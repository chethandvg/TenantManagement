using Archu.Application.Abstractions.Authentication;
using Archu.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Auth.Queries.ValidateToken;

/// <summary>
/// Handler for token validation query.
/// </summary>
public sealed class ValidateTokenQueryHandler : IRequestHandler<ValidateTokenQuery, Result<bool>>
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<ValidateTokenQueryHandler> _logger;

    public ValidateTokenQueryHandler(
        IAuthenticationService authenticationService,
        ILogger<ValidateTokenQueryHandler> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ValidateTokenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating refresh token");

        var result = await _authenticationService.ValidateRefreshTokenAsync(
            request.RefreshToken,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Token validation failed: {Error}", result.Error);
            return Result<bool>.Failure(result.Error!);
        }

        _logger.LogInformation("Token validation result: {IsValid}", result.Value);

        return result;
    }
}
