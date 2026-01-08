using TentMan.Domain.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace TentMan.Domain.Common;

public abstract class BaseEntity : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Auditing
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    // Concurrency (SQL Server rowversion)
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}