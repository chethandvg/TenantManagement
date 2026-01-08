using TentMan.Application.Common;
using MediatR;

namespace TentMan.Application.Auth.Queries.ValidateToken;

/// <summary>
/// Query to validate whether a refresh token is valid and not expired.
/// </summary>
/// <param name="RefreshToken">The refresh token to validate.</param>
public sealed record ValidateTokenQuery(string RefreshToken) : IRequest<Result<bool>>;
