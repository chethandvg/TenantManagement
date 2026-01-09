using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Tenants;

/// <summary>
/// DTO for tenant details with related data.
/// </summary>
public class TenantDetailDto
{
    public Guid Id { get; set; }
    public Guid OrgId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public List<TenantAddressDto> Addresses { get; set; } = new();
    public List<TenantEmergencyContactDto> EmergencyContacts { get; set; } = new();
    public List<TenantDocumentDto> Documents { get; set; } = new();
}

public class TenantAddressDto
{
    public Guid Id { get; set; }
    public AddressType Type { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string Country { get; set; } = "IN";
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public bool IsPrimary { get; set; }
}

public class TenantEmergencyContactDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class TenantDocumentDto
{
    public Guid Id { get; set; }
    public DocumentType DocType { get; set; }
    public string? DocNumberMasked { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public Guid FileId { get; set; }
    public string? FileName { get; set; }
    public string? Notes { get; set; }
    public DocumentStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
