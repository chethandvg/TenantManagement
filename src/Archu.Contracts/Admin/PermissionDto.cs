namespace Archu.Contracts.Admin;

/// <summary>
/// Represents a discrete permission definition exposed to administrative clients.
/// </summary>
public sealed class PermissionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string NormalizedName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
