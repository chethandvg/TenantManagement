using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Contracts.Owners;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.PropertyManagement.Owners.Commands.CreateOwner;

public class CreateOwnerCommandHandler : BaseCommandHandler, IRequestHandler<CreateOwnerCommand, OwnerDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateOwnerCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<CreateOwnerCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OwnerDto> Handle(CreateOwnerCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Creating owner: {Name} for organization: {OrgId}", request.DisplayName, request.OrgId);

        // Validate organization exists
        if (!await _unitOfWork.Organizations.ExistsAsync(request.OrgId, cancellationToken))
        {
            throw new InvalidOperationException($"Organization {request.OrgId} not found");
        }

        var owner = new Owner
        {
            OrgId = request.OrgId,
            OwnerType = request.OwnerType,
            DisplayName = request.DisplayName,
            Phone = request.Phone,
            Email = request.Email,
            Pan = request.Pan,
            Gstin = request.Gstin,
            LinkedUserId = request.LinkedUserId
        };

        await _unitOfWork.Owners.AddAsync(owner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Owner created with ID: {OwnerId}", owner.Id);

        return new OwnerDto
        {
            Id = owner.Id,
            OrgId = owner.OrgId,
            OwnerType = owner.OwnerType,
            DisplayName = owner.DisplayName,
            Phone = owner.Phone,
            Email = owner.Email,
            Pan = owner.Pan,
            Gstin = owner.Gstin,
            LinkedUserId = owner.LinkedUserId,
            RowVersion = owner.RowVersion
        };
    }
}
