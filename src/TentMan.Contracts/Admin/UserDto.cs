namespace TentMan.Contracts.Admin;

/// <summary>
/// Represents a user in the system for admin operations.
/// </summary>
public sealed class UserDto
{
    public Guid Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool EmailConfirmed { get; init; }
    public bool LockoutEnabled { get; init; }
    public DateTime? LockoutEnd { get; init; }
    public bool TwoFactorEnabled { get; init; }
    public string? PhoneNumber { get; init; }
    public bool PhoneNumberConfirmed { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public ICollection<string> Roles { get; init; } = new List<string>();
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
