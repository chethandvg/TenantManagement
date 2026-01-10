using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class InvoiceRunConfiguration : IEntityTypeConfiguration<InvoiceRun>
{
    public void Configure(EntityTypeBuilder<InvoiceRun> b)
    {
        b.ToTable("InvoiceRuns");
        b.HasKey(x => x.Id);

        b.Property(x => x.OrgId)
            .IsRequired();

        b.Property(x => x.RunNumber)
            .IsRequired()
            .HasMaxLength(50);

        b.Property(x => x.BillingPeriodStart)
            .IsRequired();

        b.Property(x => x.BillingPeriodEnd)
            .IsRequired();

        b.Property(x => x.Status)
            .IsRequired();

        b.Property(x => x.TotalLeases)
            .IsRequired()
            .HasDefaultValue(0);

        b.Property(x => x.SuccessCount)
            .IsRequired()
            .HasDefaultValue(0);

        b.Property(x => x.FailureCount)
            .IsRequired()
            .HasDefaultValue(0);

        b.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        b.Property(x => x.Notes)
            .HasMaxLength(1000);

        // Relationships
        b.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        b.HasIndex(x => x.RunNumber)
            .IsUnique();
        b.HasIndex(x => x.OrgId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => new { x.OrgId, x.BillingPeriodStart, x.BillingPeriodEnd });

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
