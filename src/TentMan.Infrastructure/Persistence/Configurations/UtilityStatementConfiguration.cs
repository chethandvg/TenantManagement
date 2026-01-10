using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class UtilityStatementConfiguration : IEntityTypeConfiguration<UtilityStatement>
{
    public void Configure(EntityTypeBuilder<UtilityStatement> b)
    {
        b.ToTable("UtilityStatements");
        b.HasKey(x => x.Id);

        b.Property(x => x.LeaseId)
            .IsRequired();

        b.Property(x => x.UtilityType)
            .IsRequired();

        b.Property(x => x.BillingPeriodStart)
            .IsRequired();

        b.Property(x => x.BillingPeriodEnd)
            .IsRequired();

        b.Property(x => x.IsMeterBased)
            .IsRequired()
            .HasDefaultValue(false);

        b.Property(x => x.PreviousReading)
            .HasPrecision(18, 2);

        b.Property(x => x.CurrentReading)
            .HasPrecision(18, 2);

        b.Property(x => x.UnitsConsumed)
            .HasPrecision(18, 2);

        b.Property(x => x.CalculatedAmount)
            .HasPrecision(18, 2);

        b.Property(x => x.DirectBillAmount)
            .HasPrecision(18, 2);

        b.Property(x => x.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        b.Property(x => x.Notes)
            .HasMaxLength(1000);

        // Relationships
        b.HasOne(x => x.Lease)
            .WithMany()
            .HasForeignKey(x => x.LeaseId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.UtilityRatePlan)
            .WithMany(x => x.UtilityStatements)
            .HasForeignKey(x => x.UtilityRatePlanId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.InvoiceLine)
            .WithMany()
            .HasForeignKey(x => x.InvoiceLineId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        b.HasIndex(x => x.LeaseId);
        b.HasIndex(x => x.UtilityType);
        b.HasIndex(x => new { x.LeaseId, x.BillingPeriodStart, x.BillingPeriodEnd });

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
