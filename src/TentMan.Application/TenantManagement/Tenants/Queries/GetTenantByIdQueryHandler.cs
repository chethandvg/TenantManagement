using TentMan.Application.Abstractions;
using TentMan.Contracts.Tenants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TentMan.Application.TenantManagement.Tenants.Queries;

public class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, TenantDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetTenantByIdQueryHandler> _logger;

    public GetTenantByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetTenantByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TenantDetailDto?> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting tenant: {TenantId}", request.TenantId);

        var tenant = await _unitOfWork.Tenants.GetByIdWithDetailsAsync(request.TenantId, cancellationToken);

        if (tenant == null)
            return null;

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
