using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Tenants;

/// <summary>
/// Request to create a new tenant.
/// </summary>
public class CreateTenantRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
}
