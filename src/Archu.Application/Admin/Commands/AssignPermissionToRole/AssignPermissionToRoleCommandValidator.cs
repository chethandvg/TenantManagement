using FluentValidation;

namespace Archu.Application.Admin.Commands.AssignPermissionToRole;

/// <summary>
/// Validates that the assign permission to role command contains the required data.
/// </summary>
public sealed class AssignPermissionToRoleCommandValidator : AbstractValidator<AssignPermissionToRoleCommand>
{
    /// <summary>
    /// Creates the validator rules ensuring a target role and permission names are provided.
    /// </summary>
    public AssignPermissionToRoleCommandValidator()
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
