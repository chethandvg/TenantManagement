using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TentMan.Domain.Entities;

namespace TentMan.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PaymentStatusHistory entity.
/// </summary>
public class PaymentStatusHistoryConfiguration : IEntityTypeConfiguration<PaymentStatusHistory>
{
    public void Configure(EntityTypeBuilder<PaymentStatusHistory> builder)
    {
        builder.ToTable("PaymentStatusHistory");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.PaymentId)
            .IsRequired();

        builder.Property(h => h.FromStatus)
            .IsRequired();

        builder.Property(h => h.ToStatus)
            .IsRequired();

        builder.Property(h => h.ChangedAtUtc)
            .IsRequired();

        builder.Property(h => h.ChangedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(h => h.Reason)
            .HasMaxLength(2000);

        builder.Property(h => h.Metadata)
            .HasColumnType("nvarchar(max)");

        builder.Property(h => h.RowVersion)
            .IsRowVersion();

        builder.Property(h => h.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(h => h.PaymentId)
            .HasDatabaseName("IX_PaymentStatusHistory_PaymentId");

        builder.HasIndex(h => h.ChangedAtUtc)
            .HasDatabaseName("IX_PaymentStatusHistory_ChangedAtUtc");

        builder.HasIndex(h => new { h.PaymentId, h.ChangedAtUtc })
            .HasDatabaseName("IX_PaymentStatusHistory_PaymentId_ChangedAtUtc");

        // Relationships
        builder.HasOne(h => h.Payment)
            .WithMany(p => p.StatusHistory)
            .HasForeignKey(h => h.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
