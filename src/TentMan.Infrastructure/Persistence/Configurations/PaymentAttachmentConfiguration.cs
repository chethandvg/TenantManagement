using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TentMan.Domain.Entities;

namespace TentMan.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PaymentAttachment entity.
/// </summary>
public class PaymentAttachmentConfiguration : IEntityTypeConfiguration<PaymentAttachment>
{
    public void Configure(EntityTypeBuilder<PaymentAttachment> builder)
    {
        builder.ToTable("PaymentAttachments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.PaymentId)
            .IsRequired();

        builder.Property(a => a.FileId)
            .IsRequired();

        builder.Property(a => a.AttachmentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Description)
            .HasMaxLength(500);

        builder.Property(a => a.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.RowVersion)
            .IsRowVersion();

        builder.Property(a => a.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(a => a.PaymentId)
            .HasDatabaseName("IX_PaymentAttachments_PaymentId");

        builder.HasIndex(a => a.FileId)
            .HasDatabaseName("IX_PaymentAttachments_FileId");

        builder.HasIndex(a => new { a.PaymentId, a.DisplayOrder })
            .HasDatabaseName("IX_PaymentAttachments_PaymentId_DisplayOrder");

        // Relationships
        builder.HasOne(a => a.Payment)
            .WithMany(p => p.Attachments)
            .HasForeignKey(a => a.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.File)
            .WithMany(f => f.PaymentAttachments)
            .HasForeignKey(a => a.FileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
