using FluentValidation;

namespace Archu.Application.Admin.Commands.RemovePermissionFromUser;

/// <summary>
/// Validates requests that revoke user permissions.
/// </summary>
public sealed class RemovePermissionFromUserCommandValidator : AbstractValidator<RemovePermissionFromUserCommand>
{
    /// <summary>
    /// Builds validation rules ensuring a user identifier and permission names are supplied.
    /// </summary>
    public RemovePermissionFromUserCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command.PermissionNames)
            .NotNull()
            .NotEmpty();

        RuleForEach(command => command.PermissionNames)
            .NotEmpty()
            .MaximumLength(256);
    }
}
