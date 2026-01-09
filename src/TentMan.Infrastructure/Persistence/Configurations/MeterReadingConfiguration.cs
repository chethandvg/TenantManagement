using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class MeterReadingConfiguration : IEntityTypeConfiguration<MeterReading>
{
    public void Configure(EntityTypeBuilder<MeterReading> b)
    {
        b.ToTable("MeterReadings");
        b.HasKey(x => x.Id);

        b.Property(x => x.UnitId)
            .IsRequired();

        b.Property(x => x.MeterType)
            .IsRequired();

        b.Property(x => x.ReadingDate)
            .IsRequired();

        b.Property(x => x.ReadingValue)
            .HasPrecision(18, 3)
            .IsRequired();

        // Relationships
        b.HasOne(x => x.Unit)
            .WithMany(x => x.MeterReadings)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Lease)
            .WithMany(x => x.MeterReadings)
            .HasForeignKey(x => x.LeaseId)
            .OnDelete(DeleteBehavior.NoAction);

        b.HasOne(x => x.PhotoFile)
            .WithMany()
            .HasForeignKey(x => x.PhotoFileId)
            .OnDelete(DeleteBehavior.NoAction);

        // Index for efficient querying by unit, meter type, and date
        b.HasIndex(x => new { x.UnitId, x.MeterType, x.ReadingDate })
            .IsDescending(false, false, true);

        // Indexes
        b.HasIndex(x => x.UnitId);
        b.HasIndex(x => x.LeaseId);
        b.HasIndex(x => x.ReadingDate);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
