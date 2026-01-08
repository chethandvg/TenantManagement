using TentMan.Application.Abstractions.Authentication;
using TentMan.Contracts.Authentication;
using FluentValidation;

namespace TentMan.Application.Auth.Validators;

/// <summary>
/// Validator for user registration requests with password policy enforcement.
/// </summary>
public sealed class RegisterRequestPasswordValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestPasswordValidator(IPasswordValidator passwordValidator)
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email address format")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters");

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Username is required")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters")
            .MaximumLength(50).WithMessage("Username cannot exceed 50 characters")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Username can only contain letters, numbers, hyphens, and underscores");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .Custom((password, context) =>
            {
                var username = context.InstanceToValidate.UserName;
                var email = context.InstanceToValidate.Email;

                var validationResult = passwordValidator.ValidatePassword(password, username, email);

                if (!validationResult.IsValid)
                {
                    foreach (var error in validationResult.Errors)
                    {
                        context.AddFailure(nameof(RegisterRequest.Password), error);
                    }
                }
            });
    }
}
