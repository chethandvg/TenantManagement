using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> b)
    {
        b.ToTable("InvoiceLines");
        b.HasKey(x => x.Id);

        b.Property(x => x.InvoiceId)
            .IsRequired();

        b.Property(x => x.ChargeTypeId)
            .IsRequired();

        b.Property(x => x.LineNumber)
            .IsRequired();

        b.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(x => x.Quantity)
            .IsRequired()
            .HasPrecision(18, 2);

        b.Property(x => x.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        b.Property(x => x.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        b.Property(x => x.TaxRate)
            .IsRequired()
            .HasPrecision(5, 2)
            .HasDefaultValue(0);

        b.Property(x => x.TaxAmount)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        b.Property(x => x.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        b.Property(x => x.Notes)
            .HasMaxLength(500);

        // Relationships
        b.HasOne(x => x.Invoice)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.ChargeType)
            .WithMany(x => x.InvoiceLines)
            .HasForeignKey(x => x.ChargeTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        b.HasIndex(x => x.InvoiceId);
        b.HasIndex(x => x.ChargeTypeId);
        b.HasIndex(x => new { x.InvoiceId, x.LineNumber })
            .IsUnique();

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
