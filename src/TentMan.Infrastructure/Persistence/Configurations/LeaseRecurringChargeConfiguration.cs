using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class LeaseRecurringChargeConfiguration : IEntityTypeConfiguration<LeaseRecurringCharge>
{
    public void Configure(EntityTypeBuilder<LeaseRecurringCharge> b)
    {
        b.ToTable("LeaseRecurringCharges");
        b.HasKey(x => x.Id);

        b.Property(x => x.LeaseId)
            .IsRequired();

        b.Property(x => x.ChargeTypeId)
            .IsRequired();

        b.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(x => x.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        b.Property(x => x.Frequency)
            .IsRequired();

        b.Property(x => x.StartDate)
            .IsRequired();

        b.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        b.Property(x => x.Notes)
            .HasMaxLength(500);

        // Relationships
        b.HasOne(x => x.Lease)
            .WithMany()
            .HasForeignKey(x => x.LeaseId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.ChargeType)
            .WithMany(x => x.LeaseRecurringCharges)
            .HasForeignKey(x => x.ChargeTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        b.HasIndex(x => x.LeaseId);
        b.HasIndex(x => x.ChargeTypeId);
        b.HasIndex(x => x.IsActive);
        b.HasIndex(x => new { x.LeaseId, x.ChargeTypeId });

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
