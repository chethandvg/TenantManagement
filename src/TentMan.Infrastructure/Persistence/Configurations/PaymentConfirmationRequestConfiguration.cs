using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TentMan.Domain.Entities;

namespace TentMan.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PaymentConfirmationRequest entity.
/// </summary>
public class PaymentConfirmationRequestConfiguration : IEntityTypeConfiguration<PaymentConfirmationRequest>
{
    public void Configure(EntityTypeBuilder<PaymentConfirmationRequest> builder)
    {
        builder.ToTable("PaymentConfirmationRequests");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.OrgId)
            .IsRequired();

        builder.Property(p => p.InvoiceId)
            .IsRequired();

        builder.Property(p => p.LeaseId)
            .IsRequired();

        builder.Property(p => p.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.PaymentDateUtc)
            .IsRequired();

        builder.Property(p => p.ReceiptNumber)
            .HasMaxLength(200);

        builder.Property(p => p.Notes)
            .HasMaxLength(2000);

        builder.Property(p => p.Status)
            .IsRequired();

        builder.Property(p => p.ReviewedBy)
            .HasMaxLength(100);

        builder.Property(p => p.ReviewResponse)
            .HasMaxLength(2000);

        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        builder.Property(p => p.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(p => p.OrgId)
            .HasDatabaseName("IX_PaymentConfirmationRequests_OrgId");

        builder.HasIndex(p => p.InvoiceId)
            .HasDatabaseName("IX_PaymentConfirmationRequests_InvoiceId");

        builder.HasIndex(p => p.LeaseId)
            .HasDatabaseName("IX_PaymentConfirmationRequests_LeaseId");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("IX_PaymentConfirmationRequests_Status");

        builder.HasIndex(p => p.PaymentDateUtc)
            .HasDatabaseName("IX_PaymentConfirmationRequests_PaymentDateUtc");

        // Relationships
        builder.HasOne(p => p.Organization)
            .WithMany()
            .HasForeignKey(p => p.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Invoice)
            .WithMany()
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Lease)
            .WithMany()
            .HasForeignKey(p => p.LeaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ProofFile)
            .WithMany(f => f.PaymentConfirmationRequests)
            .HasForeignKey(p => p.ProofFileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.Payment)
            .WithOne(pay => pay.PaymentConfirmationRequest)
            .HasForeignKey<PaymentConfirmationRequest>(p => p.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
