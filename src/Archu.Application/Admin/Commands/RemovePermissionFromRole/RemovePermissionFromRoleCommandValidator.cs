using FluentValidation;

namespace Archu.Application.Admin.Commands.RemovePermissionFromRole;

/// <summary>
/// Validates requests that remove permissions from a role.
/// </summary>
public sealed class RemovePermissionFromRoleCommandValidator : AbstractValidator<RemovePermissionFromRoleCommand>
{
    /// <summary>
    /// Creates validator rules ensuring at least one permission name is supplied.
    /// </summary>
    public RemovePermissionFromRoleCommandValidator()
    {
        RuleFor(command => command.RoleId)
            .NotEmpty();

        RuleFor(command => command.PermissionNames)
            .NotNull()
            .NotEmpty();

        RuleForEach(command => command.PermissionNames)
            .NotEmpty()
            .MaximumLength(256);
    }
}
