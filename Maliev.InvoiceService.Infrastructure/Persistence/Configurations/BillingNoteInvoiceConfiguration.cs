using Maliev.InvoiceService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.InvoiceService.Infrastructure.Persistence.Configurations;

public class BillingNoteInvoiceConfiguration : IEntityTypeConfiguration<BillingNoteInvoice>
{
    public void Configure(EntityTypeBuilder<BillingNoteInvoice> builder)
    {
        builder.HasKey(bi => new { bi.BillingNoteId, bi.InvoiceId });

        builder.Property(bi => bi.IncludedAmount)
            .HasPrecision(18, 2);

        builder.HasOne(bi => bi.BillingNote)
            .WithMany(b => b.BillingNoteInvoices)
            .HasForeignKey(bi => bi.BillingNoteId)
            .OnDelete(DeleteBehavior.Cascade);

        // Assuming Invoice does NOT have a collection back to BillingNoteInvoices yet, or we don't map it explicitly
        // If Invoice entity doesn't have the collection, we can just configure the relationship here.
        // But BillingNoteInvoice has `public Invoice Invoice { get; set; }`.
        builder.HasOne(bi => bi.Invoice)
            .WithMany() // One-to-Many from Invoice to BillingNoteInvoices if Invoice has no collection
            .HasForeignKey(bi => bi.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
