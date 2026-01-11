using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class LeaseBillingSettingConfiguration : IEntityTypeConfiguration<LeaseBillingSetting>
{
    public void Configure(EntityTypeBuilder<LeaseBillingSetting> b)
    {
        b.ToTable("LeaseBillingSettings");
        b.HasKey(x => x.Id);

        b.Property(x => x.LeaseId)
            .IsRequired();

        b.Property(x => x.BillingDay)
            .IsRequired()
            .HasDefaultValue((byte)1);

        b.Property(x => x.PaymentTermDays)
            .IsRequired()
            .HasDefaultValue((short)0);

        b.Property(x => x.GenerateInvoiceAutomatically)
            .IsRequired()
            .HasDefaultValue(true);

        b.Property(x => x.ProrationMethod)
            .IsRequired()
            .HasDefaultValue(TentMan.Contracts.Enums.ProrationMethod.ActualDaysInMonth);

        b.Property(x => x.InvoicePrefix)
            .HasMaxLength(20);

        b.Property(x => x.PaymentInstructions)
            .HasMaxLength(1000);

        b.Property(x => x.Notes)
            .HasMaxLength(500);

        // Relationships
        b.HasOne(x => x.Lease)
            .WithMany()
            .HasForeignKey(x => x.LeaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // One billing setting per lease
        b.HasIndex(x => x.LeaseId)
            .IsUnique();

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
