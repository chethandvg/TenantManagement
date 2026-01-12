using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TentMan.Domain.Entities;

namespace TentMan.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Payment entity.
/// </summary>
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.OrgId)
            .IsRequired();

        builder.Property(p => p.InvoiceId)
            .IsRequired();

        builder.Property(p => p.LeaseId)
            .IsRequired();

        builder.Property(p => p.PaymentMode)
            .IsRequired();

        builder.Property(p => p.Status)
            .IsRequired();

        builder.Property(p => p.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.PaymentDateUtc)
            .IsRequired();

        builder.Property(p => p.TransactionReference)
            .HasMaxLength(200);

        builder.Property(p => p.ReceivedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.PayerName)
            .HasMaxLength(200);

        builder.Property(p => p.Notes)
            .HasMaxLength(2000);

        builder.Property(p => p.PaymentMetadata)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        builder.Property(p => p.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(p => p.OrgId)
            .HasDatabaseName("IX_Payments_OrgId");

        builder.HasIndex(p => p.InvoiceId)
            .HasDatabaseName("IX_Payments_InvoiceId");

        builder.HasIndex(p => p.LeaseId)
            .HasDatabaseName("IX_Payments_LeaseId");

        builder.HasIndex(p => p.PaymentDateUtc)
            .HasDatabaseName("IX_Payments_PaymentDateUtc");

        builder.HasIndex(p => p.TransactionReference)
            .HasDatabaseName("IX_Payments_TransactionReference");

        // Relationships
        builder.HasOne(p => p.Organization)
            .WithMany()
            .HasForeignKey(p => p.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Lease)
            .WithMany()
            .HasForeignKey(p => p.LeaseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
