using Archu.Application.Abstractions.Authentication;
using Archu.Contracts.Authentication;
using FluentValidation;

namespace Archu.Application.Auth.Validators;

/// <summary>
/// Validator for password reset requests with password policy enforcement.
/// </summary>
public sealed class ResetPasswordRequestPasswordValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestPasswordValidator(IPasswordValidator passwordValidator)
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email address format");

        RuleFor(x => x.ResetToken)
            .NotEmpty().WithMessage("Reset token is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .Custom((newPassword, context) =>
            {
                var email = context.InstanceToValidate.Email;

                var validationResult = passwordValidator.ValidatePassword(newPassword, email: email);

                if (!validationResult.IsValid)
                {
                    foreach (var error in validationResult.Errors)
                    {
                        context.AddFailure(nameof(ResetPasswordRequest.NewPassword), error);
                    }
                }
            });
    }
}
