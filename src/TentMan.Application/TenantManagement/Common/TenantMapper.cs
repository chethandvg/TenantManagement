using TentMan.Contracts.Tenants;
using TentMan.Domain.Entities;

namespace TentMan.Application.TenantManagement.Common;

/// <summary>
/// Shared mapper for Tenant entity to TenantDetailDto.
/// </summary>
public static class TenantMapper
{
    public static TenantDetailDto ToDetailDto(Tenant tenant)
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
