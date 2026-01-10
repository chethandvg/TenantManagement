using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class UtilityRatePlanConfiguration : IEntityTypeConfiguration<UtilityRatePlan>
{
    public void Configure(EntityTypeBuilder<UtilityRatePlan> b)
    {
        b.ToTable("UtilityRatePlans");
        b.HasKey(x => x.Id);

        b.Property(x => x.OrgId)
            .IsRequired();

        b.Property(x => x.UtilityType)
            .IsRequired();

        b.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        b.Property(x => x.Description)
            .HasMaxLength(500);

        b.Property(x => x.EffectiveFrom)
            .IsRequired();

        b.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Relationships
        b.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        b.HasIndex(x => x.OrgId);
        b.HasIndex(x => x.UtilityType);
        b.HasIndex(x => x.IsActive);
        b.HasIndex(x => new { x.OrgId, x.UtilityType, x.EffectiveFrom });

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
