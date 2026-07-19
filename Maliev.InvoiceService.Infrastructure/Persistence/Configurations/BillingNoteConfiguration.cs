using Maliev.InvoiceService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.InvoiceService.Infrastructure.Persistence.Configurations;

public class BillingNoteConfiguration : IEntityTypeConfiguration<BillingNote>
{
    public void Configure(EntityTypeBuilder<BillingNote> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BillingNoteNumber)
            .HasMaxLength(50);

        builder.HasIndex(b => b.BillingNoteNumber)
            .IsUnique();

        builder.Property(b => b.CustomerName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(b => b.CustomerTaxId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.BillingAddress)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(b => b.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(b => b.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.Notes)
            .HasMaxLength(2000);

        builder.Property(b => b.TotalAmount)
            .HasPrecision(18, 2);
    }
}
