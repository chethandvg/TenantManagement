using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class TenantAddressConfiguration : IEntityTypeConfiguration<TenantAddress>
{
    public void Configure(EntityTypeBuilder<TenantAddress> b)
    {
        b.ToTable("TenantAddresses");
        b.HasKey(x => x.Id);

        b.Property(x => x.TenantId)
            .IsRequired();

        b.Property(x => x.Type)
            .IsRequired();

        b.Property(x => x.Line1)
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.Line2)
            .HasMaxLength(200);

        b.Property(x => x.City)
            .HasMaxLength(100)
            .IsRequired();

        b.Property(x => x.District)
            .HasMaxLength(100);

        b.Property(x => x.State)
            .HasMaxLength(100)
            .IsRequired();

        b.Property(x => x.Pincode)
            .HasMaxLength(10)
            .IsRequired();

        b.Property(x => x.Country)
            .HasMaxLength(50)
            .HasDefaultValue("IN")
            .IsRequired();

        b.Property(x => x.IsPrimary)
            .IsRequired();

        // Relationships
        b.HasOne(x => x.Tenant)
            .WithMany(x => x.Addresses)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        b.HasIndex(x => x.TenantId);
        b.HasIndex(x => new { x.TenantId, x.IsPrimary });

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
