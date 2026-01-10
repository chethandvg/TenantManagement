using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class UtilityRateSlabConfiguration : IEntityTypeConfiguration<UtilityRateSlab>
{
    public void Configure(EntityTypeBuilder<UtilityRateSlab> b)
    {
        b.ToTable("UtilityRateSlabs");
        b.HasKey(x => x.Id);

        b.Property(x => x.UtilityRatePlanId)
            .IsRequired();

        b.Property(x => x.SlabOrder)
            .IsRequired();

        b.Property(x => x.FromUnits)
            .IsRequired()
            .HasPrecision(18, 2);

        b.Property(x => x.ToUnits)
            .HasPrecision(18, 2);

        b.Property(x => x.RatePerUnit)
            .IsRequired()
            .HasPrecision(18, 4);

        b.Property(x => x.FixedCharge)
            .HasPrecision(18, 2);

        // Relationships
        b.HasOne(x => x.UtilityRatePlan)
            .WithMany(x => x.RateSlabs)
            .HasForeignKey(x => x.UtilityRatePlanId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        b.HasIndex(x => x.UtilityRatePlanId);
        b.HasIndex(x => new { x.UtilityRatePlanId, x.SlabOrder })
            .IsUnique();

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
