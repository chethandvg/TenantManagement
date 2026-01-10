using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class ChargeTypeConfiguration : IEntityTypeConfiguration<ChargeType>
{
    public void Configure(EntityTypeBuilder<ChargeType> b)
    {
        b.ToTable("ChargeTypes");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code)
            .IsRequired();

        b.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        b.Property(x => x.Description)
            .HasMaxLength(500);

        b.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        b.Property(x => x.IsSystemDefined)
            .IsRequired()
            .HasDefaultValue(false);

        b.Property(x => x.IsTaxable)
            .IsRequired()
            .HasDefaultValue(false);

        b.Property(x => x.DefaultAmount)
            .HasPrecision(18, 2);

        // Relationships
        b.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        b.HasIndex(x => x.Code);
        b.HasIndex(x => new { x.OrgId, x.Code });
        b.HasIndex(x => x.IsActive);
        b.HasIndex(x => x.IsSystemDefined);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
