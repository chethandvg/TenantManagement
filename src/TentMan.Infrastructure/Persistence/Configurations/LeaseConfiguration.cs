using TentMan.Domain.Entities;
using TentMan.Contracts.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class LeaseConfiguration : IEntityTypeConfiguration<Lease>
{
    public void Configure(EntityTypeBuilder<Lease> b)
    {
        b.ToTable("Leases");
        b.HasKey(x => x.Id);

        b.Property(x => x.OrgId)
            .IsRequired();

        b.Property(x => x.UnitId)
            .IsRequired();

        b.Property(x => x.LeaseNumber)
            .HasMaxLength(50);

        b.Property(x => x.Status)
            .IsRequired();

        b.Property(x => x.StartDate)
            .IsRequired();

        b.Property(x => x.RentDueDay)
            .IsRequired();

        b.Property(x => x.GraceDays)
            .IsRequired()
            .HasDefaultValue((byte)0);

        b.Property(x => x.LateFeeType)
            .IsRequired();

        b.Property(x => x.LateFeeValue)
            .HasPrecision(18, 2);

        b.Property(x => x.PaymentMethodNote)
            .HasMaxLength(200);

        b.Property(x => x.TermsText);

        b.Property(x => x.IsAutoRenew)
            .IsRequired()
            .HasDefaultValue(false);

        // Relationships
        b.HasOne(x => x.Organization)
            .WithMany(x => x.Leases)
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Unit)
            .WithMany(x => x.Leases)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // Critical constraint: Only one active lease per unit
        // Filtered unique index on (OrgId, UnitId) where Status in (Active, NoticeGiven)
        b.HasIndex(x => new { x.OrgId, x.UnitId })
            .IsUnique()
            .HasFilter($"[Status] IN ({(int)LeaseStatus.Active}, {(int)LeaseStatus.NoticeGiven}) AND [IsDeleted] = 0");

        // Indexes
        b.HasIndex(x => x.OrgId);
        b.HasIndex(x => x.UnitId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.StartDate);
        b.HasIndex(x => x.EndDate);

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
