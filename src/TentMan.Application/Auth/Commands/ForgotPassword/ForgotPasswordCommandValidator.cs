using FluentValidation;

namespace TentMan.Application.Auth.Commands.ForgotPassword;

/// <summary>
/// Validator for ForgotPasswordCommand.
/// </summary>
public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}
