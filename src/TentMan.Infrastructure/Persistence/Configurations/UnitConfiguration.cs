using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> b)
    {
        b.ToTable("Units");
        b.HasKey(x => x.Id);

        b.Property(x => x.BuildingId)
            .IsRequired();

        b.Property(x => x.UnitNumber)
            .HasMaxLength(50)
            .IsRequired();

        b.Property(x => x.Floor)
            .IsRequired();

        b.Property(x => x.UnitType)
            .IsRequired();

        b.Property(x => x.AreaSqFt)
            .HasPrecision(10, 2)
            .IsRequired();

        b.Property(x => x.Bedrooms)
            .IsRequired();

        b.Property(x => x.Bathrooms)
            .IsRequired();

        b.Property(x => x.Furnishing)
            .IsRequired();

        b.Property(x => x.ParkingSlots)
            .IsRequired();

        b.Property(x => x.OccupancyStatus)
            .IsRequired();

        b.Property(x => x.HasUnitOwnershipOverride)
            .IsRequired();

        // Relationships
        b.HasOne(x => x.Building)
            .WithMany(x => x.Units)
            .HasForeignKey(x => x.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: UnitNumber must be unique per building
        b.HasIndex(x => new { x.BuildingId, x.UnitNumber })
            .IsUnique();

        // Indexes
        b.HasIndex(x => x.BuildingId);
        b.HasIndex(x => x.OccupancyStatus);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
