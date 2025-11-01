namespace Archu.Contracts.Admin;

/// <summary>
/// Represents a role in the system for admin operations.
/// </summary>
public sealed class RoleDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string NormalizedName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
