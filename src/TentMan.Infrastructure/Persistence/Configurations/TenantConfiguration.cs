using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("Tenants");
        b.HasKey(x => x.Id);

        b.Property(x => x.OrgId)
            .IsRequired();

        b.Property(x => x.FullName)
            .HasMaxLength(150)
            .IsRequired();

        b.Property(x => x.Phone)
            .HasMaxLength(15)
            .IsRequired();

        b.Property(x => x.Email)
            .HasMaxLength(254);

        b.Property(x => x.DateOfBirth);

        b.Property(x => x.Gender);

        b.Property(x => x.IsActive)
            .IsRequired();

        // Relationships
        b.HasOne(x => x.Organization)
            .WithMany(x => x.Tenants)
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraints
        b.HasIndex(x => new { x.OrgId, x.Phone }).IsUnique();
        b.HasIndex(x => new { x.OrgId, x.Email })
            .IsUnique()
            .HasFilter("[Email] IS NOT NULL");

        // Indexes
        b.HasIndex(x => x.OrgId);
        b.HasIndex(x => x.IsActive);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
