using FluentValidation;

namespace Archu.Application.Auth.Queries.ValidateToken;

/// <summary>
/// Validator for ValidateTokenQuery.
/// </summary>
public sealed class ValidateTokenQueryValidator : AbstractValidator<ValidateTokenQuery>
{
    public ValidateTokenQueryValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");
    }
}
