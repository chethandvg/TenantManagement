using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class LeaseTermConfiguration : IEntityTypeConfiguration<LeaseTerm>
{
    public void Configure(EntityTypeBuilder<LeaseTerm> b)
    {
        b.ToTable("LeaseTerms");
        b.HasKey(x => x.Id);

        b.Property(x => x.LeaseId)
            .IsRequired();

        b.Property(x => x.EffectiveFrom)
            .IsRequired();

        b.Property(x => x.MonthlyRent)
            .HasPrecision(18, 2)
            .IsRequired();

        b.Property(x => x.SecurityDeposit)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        b.Property(x => x.MaintenanceCharge)
            .HasPrecision(18, 2);

        b.Property(x => x.OtherFixedCharge)
            .HasPrecision(18, 2);

        b.Property(x => x.EscalationType)
            .IsRequired()
            .HasDefaultValue(EscalationType.None);

        b.Property(x => x.EscalationValue)
            .HasPrecision(18, 2);

        b.Property(x => x.Notes)
            .HasMaxLength(300);

        // Relationships
        b.HasOne(x => x.Lease)
            .WithMany(x => x.Terms)
            .HasForeignKey(x => x.LeaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for enforcing uniqueness of effective date per lease
        b.HasIndex(x => new { x.LeaseId, x.EffectiveFrom });

        // Indexes
        b.HasIndex(x => x.LeaseId);
        b.HasIndex(x => x.EffectiveFrom);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
