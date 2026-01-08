using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class UnitOwnershipShareConfiguration : IEntityTypeConfiguration<UnitOwnershipShare>
{
    public void Configure(EntityTypeBuilder<UnitOwnershipShare> b)
    {
        b.ToTable("UnitOwnershipShares");
        b.HasKey(x => x.Id);

        b.Property(x => x.UnitId)
            .IsRequired();

        b.Property(x => x.OwnerId)
            .IsRequired();

        b.Property(x => x.SharePercent)
            .HasPrecision(5, 2)
            .IsRequired();

        b.Property(x => x.EffectiveFrom)
            .IsRequired();

        b.Property(x => x.EffectiveTo);

        // Relationships
        b.HasOne(x => x.Unit)
            .WithMany(x => x.OwnershipShares)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Owner)
            .WithMany(x => x.UnitOwnershipShares)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        b.HasIndex(x => x.UnitId);
        b.HasIndex(x => x.OwnerId);
        b.HasIndex(x => new { x.UnitId, x.EffectiveTo });

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
