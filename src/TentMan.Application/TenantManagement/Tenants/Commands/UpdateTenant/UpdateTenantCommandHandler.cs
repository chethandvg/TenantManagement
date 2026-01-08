using TentMan.Application.Abstractions;
using TentMan.Application.Common;
using TentMan.Contracts.Tenants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.TenantManagement.Tenants.Commands.UpdateTenant;

public class UpdateTenantCommandHandler : BaseCommandHandler, IRequestHandler<UpdateTenantCommand, TenantDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTenantCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<UpdateTenantCommandHandler> logger)
        : base(currentUser, logger)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TenantDetailDto> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Updating tenant: {TenantId}", request.TenantId);

        var tenant = await _unitOfWork.Tenants.GetByIdWithDetailsAsync(request.TenantId, cancellationToken)
            ?? throw new InvalidOperationException($"Tenant {request.TenantId} not found");

        // Check for duplicate phone number in the same organization
        if (await _unitOfWork.Tenants.PhoneExistsAsync(tenant.OrgId, request.Phone, request.TenantId, cancellationToken))
        {
            throw new InvalidOperationException($"A tenant with phone '{request.Phone}' already exists in this organization");
        }

        // Check for duplicate email in the same organization if email is provided
        if (!string.IsNullOrWhiteSpace(request.Email) && 
            await _unitOfWork.Tenants.EmailExistsAsync(tenant.OrgId, request.Email, request.TenantId, cancellationToken))
        {
            throw new InvalidOperationException($"A tenant with email '{request.Email}' already exists in this organization");
        }

        tenant.FullName = request.FullName;
        tenant.Phone = request.Phone;
        tenant.Email = request.Email;
        tenant.DateOfBirth = request.DateOfBirth;
        tenant.Gender = request.Gender;
        tenant.IsActive = request.IsActive;

        await _unitOfWork.Tenants.UpdateAsync(tenant, request.RowVersion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("Tenant updated: {TenantId}", tenant.Id);

        return MapToDetailDto(tenant);
    }

    private static TenantDetailDto MapToDetailDto(Domain.Entities.Tenant tenant)
    {
        return new TenantDetailDto
        {
            Id = tenant.Id,
            OrgId = tenant.OrgId,
            FullName = tenant.FullName,
            Phone = tenant.Phone,
            Email = tenant.Email,
            DateOfBirth = tenant.DateOfBirth,
            Gender = tenant.Gender,
            IsActive = tenant.IsActive,
            CreatedAtUtc = tenant.CreatedAtUtc,
            ModifiedAtUtc = tenant.ModifiedAtUtc,
            RowVersion = tenant.RowVersion,
            Addresses = tenant.Addresses.Select(a => new TenantAddressDto
            {
                Id = a.Id,
                Type = a.Type,
                Line1 = a.Line1,
                Line2 = a.Line2,
                City = a.City,
                District = a.District,
                State = a.State,
                Pincode = a.Pincode,
                Country = a.Country,
                FromDate = a.FromDate,
                ToDate = a.ToDate,
                IsPrimary = a.IsPrimary
            }).ToList(),
            EmergencyContacts = tenant.EmergencyContacts.Select(c => new TenantEmergencyContactDto
            {
                Id = c.Id,
                Name = c.Name,
                Relationship = c.Relationship,
                Phone = c.Phone,
                Email = c.Email
            }).ToList(),
            Documents = tenant.Documents.Select(d => new TenantDocumentDto
            {
                Id = d.Id,
                DocType = d.DocType,
                DocNumberMasked = d.DocNumberMasked,
                IssueDate = d.IssueDate,
                ExpiryDate = d.ExpiryDate,
                FileId = d.FileId,
                FileName = d.File?.FileName,
                Notes = d.Notes
            }).ToList()
        };
    }
}
