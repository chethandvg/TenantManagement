using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class UnitOccupancyConfiguration : IEntityTypeConfiguration<UnitOccupancy>
{
    public void Configure(EntityTypeBuilder<UnitOccupancy> b)
    {
        b.ToTable("UnitOccupancies");
        b.HasKey(x => x.Id);

        b.Property(x => x.UnitId)
            .IsRequired();

        b.Property(x => x.LeaseId)
            .IsRequired();

        b.Property(x => x.FromDate)
            .IsRequired();

        b.Property(x => x.Status)
            .IsRequired();

        // Relationships
        b.HasOne(x => x.Unit)
            .WithMany(x => x.Occupancies)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Lease)
            .WithMany(x => x.Occupancies)
            .HasForeignKey(x => x.LeaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        b.HasIndex(x => x.UnitId);
        b.HasIndex(x => x.LeaseId);
        b.HasIndex(x => new { x.UnitId, x.FromDate });
        b.HasIndex(x => x.Status);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
