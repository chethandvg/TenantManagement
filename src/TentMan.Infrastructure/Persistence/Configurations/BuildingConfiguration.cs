using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> b)
    {
        b.ToTable("Buildings");
        b.HasKey(x => x.Id);

        b.Property(x => x.OrgId)
            .IsRequired();

        b.Property(x => x.BuildingCode)
            .HasMaxLength(50)
            .IsRequired();

        b.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.PropertyType)
            .IsRequired();

        b.Property(x => x.TotalFloors)
            .IsRequired();

        b.Property(x => x.HasLift)
            .IsRequired();

        b.Property(x => x.Notes)
            .HasMaxLength(1000);

        // Relationships
        b.HasOne(x => x.Organization)
            .WithMany(x => x.Buildings)
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: BuildingCode must be unique per organization
        b.HasIndex(x => new { x.OrgId, x.BuildingCode })
            .IsUnique();

        // Indexes
        b.HasIndex(x => x.OrgId);
        b.HasIndex(x => x.Name);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
