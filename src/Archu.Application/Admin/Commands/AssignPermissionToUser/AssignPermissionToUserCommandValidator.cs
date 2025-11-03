using FluentValidation;

namespace Archu.Application.Admin.Commands.AssignPermissionToUser;

/// <summary>
/// Validates requests that assign permissions directly to a user.
/// </summary>
public sealed class AssignPermissionToUserCommandValidator : AbstractValidator<AssignPermissionToUserCommand>
{
    /// <summary>
    /// Builds validation rules ensuring a target user and permission names are provided.
    /// </summary>
    public AssignPermissionToUserCommandValidator()
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
