using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class BuildingOwnershipShareConfiguration : IEntityTypeConfiguration<BuildingOwnershipShare>
{
    public void Configure(EntityTypeBuilder<BuildingOwnershipShare> b)
    {
        b.ToTable("BuildingOwnershipShares");
        b.HasKey(x => x.Id);

        b.Property(x => x.BuildingId)
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
        b.HasOne(x => x.Building)
            .WithMany(x => x.OwnershipShares)
            .HasForeignKey(x => x.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Owner)
            .WithMany(x => x.BuildingOwnershipShares)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        b.HasIndex(x => x.BuildingId);
        b.HasIndex(x => x.OwnerId);
        b.HasIndex(x => new { x.BuildingId, x.EffectiveTo });

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
