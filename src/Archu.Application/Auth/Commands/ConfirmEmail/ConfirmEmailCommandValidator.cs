using FluentValidation;

namespace Archu.Application.Auth.Commands.ConfirmEmail;

/// <summary>
/// Validator for ConfirmEmailCommand.
/// </summary>
public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Confirmation token is required");
    }
}
