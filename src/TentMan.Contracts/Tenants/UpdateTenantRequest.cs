using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Tenants;

/// <summary>
/// Request to update an existing tenant.
/// </summary>
public class UpdateTenantRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public bool IsActive { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
