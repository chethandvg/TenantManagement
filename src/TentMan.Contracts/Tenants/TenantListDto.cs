using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Tenants;

/// <summary>
/// DTO for tenant list display.
/// </summary>
public class TenantListDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
