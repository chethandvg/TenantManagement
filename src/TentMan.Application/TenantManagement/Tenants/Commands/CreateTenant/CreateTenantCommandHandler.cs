using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Contracts.Tenants;
using TentMan.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.TenantManagement.Tenants.Commands.CreateTenant;

public class CreateTenantCommandHandler : BaseCommandHandler, IRequestHandler<CreateTenantCommand, TenantListDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateTenantCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<CreateTenantCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TenantListDto> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Creating tenant: {FullName} in org: {OrgId}", request.FullName, request.OrgId);

        // Validate organization exists
        if (!await _unitOfWork.Organizations.ExistsAsync(request.OrgId, cancellationToken))
        {
            throw new InvalidOperationException($"Organization {request.OrgId} not found");
        }

        // Check for duplicate phone number in the same organization
        if (await _unitOfWork.Tenants.PhoneExistsAsync(request.OrgId, request.Phone, null, cancellationToken))
        {
            throw new InvalidOperationException($"A tenant with phone '{request.Phone}' already exists in this organization");
        }

        // Check for duplicate email in the same organization if email is provided
        if (!string.IsNullOrWhiteSpace(request.Email) && 
            await _unitOfWork.Tenants.EmailExistsAsync(request.OrgId, request.Email, null, cancellationToken))
        {
            throw new InvalidOperationException($"A tenant with email '{request.Email}' already exists in this organization");
        }

        var tenant = new Tenant
        {
            OrgId = request.OrgId,
            FullName = request.FullName,
            Phone = request.Phone,
            Email = request.Email,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            IsActive = true
        };

        await _unitOfWork.Tenants.AddAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Tenant created with ID: {TenantId}", tenant.Id);

        return new TenantListDto
        {
            Id = tenant.Id,
            FullName = tenant.FullName,
            Phone = tenant.Phone,
            Email = tenant.Email,
            IsActive = tenant.IsActive,
            RowVersion = tenant.RowVersion
        };
    }
}
