using TentMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TentMan.Infrastructure.Persistence.Configurations;

public class CreditNoteLineConfiguration : IEntityTypeConfiguration<CreditNoteLine>
{
    public void Configure(EntityTypeBuilder<CreditNoteLine> b)
    {
        b.ToTable("CreditNoteLines");
        b.HasKey(x => x.Id);

        b.Property(x => x.CreditNoteId)
            .IsRequired();

        b.Property(x => x.InvoiceLineId)
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
        b.HasOne(x => x.CreditNote)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.CreditNoteId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.InvoiceLine)
            .WithMany(x => x.CreditNoteLines)
            .HasForeignKey(x => x.InvoiceLineId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        b.HasIndex(x => x.CreditNoteId);
        b.HasIndex(x => x.InvoiceLineId);
        b.HasIndex(x => new { x.CreditNoteId, x.LineNumber })
            .IsUnique();

        // Concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
