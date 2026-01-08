using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class BuildingAddressConfiguration : IEntityTypeConfiguration<BuildingAddress>
{
    public void Configure(EntityTypeBuilder<BuildingAddress> b)
    {
        b.ToTable("BuildingAddresses");
        b.HasKey(x => x.Id);

        b.Property(x => x.BuildingId)
            .IsRequired();

        b.Property(x => x.Line1)
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.Line2)
            .HasMaxLength(200);

        b.Property(x => x.Locality)
            .HasMaxLength(100)
            .IsRequired();

        b.Property(x => x.City)
            .HasMaxLength(100)
            .IsRequired();

        b.Property(x => x.District)
            .HasMaxLength(100)
            .IsRequired();

        b.Property(x => x.State)
            .HasMaxLength(100)
            .IsRequired();

        b.Property(x => x.PostalCode)
            .HasMaxLength(10)
            .IsRequired();

        b.Property(x => x.Landmark)
            .HasMaxLength(200);

        b.Property(x => x.Latitude)
            .HasPrecision(10, 7);

        b.Property(x => x.Longitude)
            .HasPrecision(10, 7);

        // 1:1 relationship with Building
        b.HasOne(x => x.Building)
            .WithOne(x => x.Address)
            .HasForeignKey<BuildingAddress>(x => x.BuildingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        b.HasIndex(x => x.BuildingId)
            .IsUnique();
        b.HasIndex(x => x.PostalCode);
        b.HasIndex(x => x.City);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
