using TentMan.Domain.Enums;

namespace TentMan.Contracts.Owners;

public sealed class CreateOwnerRequest
{
    public Guid OrgId { get; init; }
    public OwnerType OwnerType { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Pan { get; init; }
    public string? Gstin { get; init; }
    public Guid? LinkedUserId { get; init; }
}
