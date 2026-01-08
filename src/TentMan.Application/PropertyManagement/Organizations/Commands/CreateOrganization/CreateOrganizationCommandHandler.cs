using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Organizations.Commands.CreateOrganization;

public class CreateOrganizationCommandHandler : BaseCommandHandler, IRequestHandler<CreateOrganizationCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrganizationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<CreateOrganizationCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Creating organization: {Name}", request.Name);

        var organization = new Organization
        {
            Name = request.Name,
            TimeZone = request.TimeZone,
            IsActive = true
        };

        await _unitOfWork.Organizations.AddAsync(organization, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Organization created with ID: {OrganizationId}", organization.Id);

        return organization.Id;
    }
}
