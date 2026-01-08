using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class HandoverChecklistItemConfiguration : IEntityTypeConfiguration<HandoverChecklistItem>
{
    public void Configure(EntityTypeBuilder<HandoverChecklistItem> b)
    {
        b.ToTable("HandoverChecklistItems");
        b.HasKey(x => x.Id);

        b.Property(x => x.HandoverId)
            .IsRequired();

        b.Property(x => x.Category)
            .HasMaxLength(50)
            .IsRequired();

        b.Property(x => x.ItemName)
            .HasMaxLength(150)
            .IsRequired();

        b.Property(x => x.Condition)
            .IsRequired();

        b.Property(x => x.Remarks)
            .HasMaxLength(200);

        // Relationships
        b.HasOne(x => x.Handover)
            .WithMany(x => x.ChecklistItems)
            .HasForeignKey(x => x.HandoverId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.PhotoFile)
            .WithMany()
            .HasForeignKey(x => x.PhotoFileId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        b.HasIndex(x => x.HandoverId);
        b.HasIndex(x => x.Category);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
