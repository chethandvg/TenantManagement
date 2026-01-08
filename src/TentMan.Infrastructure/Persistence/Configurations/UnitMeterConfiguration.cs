using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class UnitMeterConfiguration : IEntityTypeConfiguration<UnitMeter>
{
    public void Configure(EntityTypeBuilder<UnitMeter> b)
    {
        b.ToTable("UnitMeters");
        b.HasKey(x => x.Id);

        b.Property(x => x.UnitId)
            .IsRequired();

        b.Property(x => x.UtilityType)
            .IsRequired();

        b.Property(x => x.MeterNumber)
            .HasMaxLength(100)
            .IsRequired();

        b.Property(x => x.Provider)
            .HasMaxLength(200);

        b.Property(x => x.ConsumerAccount)
            .HasMaxLength(100);

        b.Property(x => x.IsActive)
            .IsRequired();

        // Relationships
        b.HasOne(x => x.Unit)
            .WithMany(x => x.Meters)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        b.HasIndex(x => x.UnitId);
        b.HasIndex(x => x.MeterNumber);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
