using Archu.Application.Abstractions;
using Archu.Contracts.Admin;
using Archu.Domain.Entities.Identity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Archu.Application.Admin.Commands.CreateRole;

/// <summary>
/// Handles the creation of a new role.
/// </summary>
public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, RoleDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CreateRoleCommandHandler> _logger;

    public CreateRoleCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<CreateRoleCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<RoleDto> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        _logger.LogInformation("User {UserId} creating role: {RoleName}", userId, request.Name);

        // Check if role already exists
        var existingRole = await _unitOfWork.Roles.GetByNameAsync(request.Name, cancellationToken);
        if (existingRole != null)
        {
            _logger.LogWarning("Role {RoleName} already exists", request.Name);
            throw new InvalidOperationException($"Role '{request.Name}' already exists");
        }

        var role = new ApplicationRole
        {
            Name = request.Name,
            NormalizedName = request.Name.ToUpperInvariant(),
            Description = request.Description
        };

        var createdRole = await _unitOfWork.Roles.AddAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Role created with ID: {RoleId}", createdRole.Id);

        return new RoleDto
        {
            Id = createdRole.Id,
            Name = createdRole.Name,
            NormalizedName = createdRole.NormalizedName,
            Description = createdRole.Description,
            CreatedAtUtc = createdRole.CreatedAtUtc,
            RowVersion = createdRole.RowVersion
        };
    }
}
