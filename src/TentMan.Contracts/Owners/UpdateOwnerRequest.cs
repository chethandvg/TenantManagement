using TentMan.Contracts.Enums;

namespace TentMan.Contracts.Owners;

/// <summary>
/// Request to update an existing owner.
/// </summary>
public sealed class UpdateOwnerRequest
{
    public OwnerType OwnerType { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Pan { get; init; }
    public string? Gstin { get; init; }
    public Guid? LinkedUserId { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
