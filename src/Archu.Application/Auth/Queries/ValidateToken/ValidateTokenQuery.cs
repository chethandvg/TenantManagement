using Archu.Application.Common;
using MediatR;

namespace Archu.Application.Auth.Queries.ValidateToken;

/// <summary>
/// Query to validate whether a refresh token is valid and not expired.
/// </summary>
public sealed record ValidateTokenQuery : IRequest<Result<bool>>
{
    /// <summary>
    /// The refresh token to validate.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;
}
