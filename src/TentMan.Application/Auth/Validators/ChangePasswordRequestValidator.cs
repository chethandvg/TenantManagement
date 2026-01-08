using TentMan.Application.Abstractions.Authentication;
using TentMan.Contracts.Authentication;
using FluentValidation;

namespace TentMan.Application.Auth.Validators;

/// <summary>
/// Validator for password change requests with password policy enforcement.
/// </summary>
public sealed class ChangePasswordRequestPasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestPasswordValidator(IPasswordValidator passwordValidator)
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password")
            .Custom((newPassword, context) =>
            {
                var validationResult = passwordValidator.ValidatePassword(newPassword);

                if (!validationResult.IsValid)
                {
                    foreach (var error in validationResult.Errors)
                    {
                        context.AddFailure(nameof(ChangePasswordRequest.NewPassword), error);
                    }
                }
            });
    }
}
