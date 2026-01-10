using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.ToTable("Invoices");
        b.HasKey(x => x.Id);

        b.Property(x => x.OrgId)
            .IsRequired();

        b.Property(x => x.LeaseId)
            .IsRequired();

        b.Property(x => x.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(50);

        b.Property(x => x.InvoiceDate)
            .IsRequired();

        b.Property(x => x.DueDate)
            .IsRequired();

        b.Property(x => x.Status)
            .IsRequired();

        b.Property(x => x.BillingPeriodStart)
            .IsRequired();

        b.Property(x => x.BillingPeriodEnd)
            .IsRequired();

        b.Property(x => x.SubTotal)
            .IsRequired()
            .HasPrecision(18, 2);

        b.Property(x => x.TaxAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        b.Property(x => x.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        b.Property(x => x.PaidAmount)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        b.Property(x => x.BalanceAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        b.Property(x => x.Notes)
            .HasMaxLength(1000);

        b.Property(x => x.PaymentInstructions)
            .HasMaxLength(1000);

        // Relationships
        b.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Lease)
            .WithMany()
            .HasForeignKey(x => x.LeaseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        b.HasIndex(x => x.InvoiceNumber)
            .IsUnique();
        b.HasIndex(x => x.OrgId);
        b.HasIndex(x => x.LeaseId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.InvoiceDate);
        b.HasIndex(x => x.DueDate);
        b.HasIndex(x => new { x.OrgId, x.InvoiceDate });

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
