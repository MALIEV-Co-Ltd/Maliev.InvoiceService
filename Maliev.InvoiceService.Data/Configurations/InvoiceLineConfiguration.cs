using Maliev.InvoiceService.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.InvoiceService.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for InvoiceLine entity.
/// </summary>
public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("invoice_lines");

        // Primary Key
        builder.HasKey(il => il.Id);
        builder.Property(il => il.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .IsRequired();

        // Foreign Key
        builder.Property(il => il.InvoiceId)
            .HasColumnName("invoice_id")
            .IsRequired();
        builder.HasIndex(il => il.InvoiceId)
            .HasDatabaseName("idx_invoice_lines_invoice_id");

        // Line Number (unique within invoice)
        builder.Property(il => il.LineNumber)
            .HasColumnName("line_number")
            .IsRequired();
        builder.HasIndex(il => new { il.InvoiceId, il.LineNumber })
            .IsUnique()
            .HasDatabaseName("idx_invoice_lines_invoice_line_unique");

        // Item Information
        builder.Property(il => il.ItemCode)
            .HasColumnName("item_code")
            .HasMaxLength(100);
        builder.HasIndex(il => il.ItemCode)
            .HasDatabaseName("idx_invoice_lines_item_code")
            .HasFilter("item_code IS NOT NULL");

        builder.Property(il => il.Description)
            .HasColumnName("description")
            .HasMaxLength(1000)
            .IsRequired();

        // Quantities and Pricing
        builder.Property(il => il.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(il => il.UnitPrice)
            .HasColumnName("unit_price")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(il => il.DiscountPercentage)
            .HasColumnName("discount_percentage")
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(0)
            .IsRequired();

        // Tax Information
        builder.Property(il => il.TaxCategory)
            .HasColumnName("tax_category")
            .HasMaxLength(50)
            .HasDefaultValue("VAT")
            .IsRequired();

        builder.Property(il => il.TaxRate)
            .HasColumnName("tax_rate")
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(7.00m)
            .IsRequired();

        // Calculated Amounts
        builder.Property(il => il.LineSubtotal)
            .HasColumnName("line_subtotal")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(il => il.TaxAmount)
            .HasColumnName("tax_amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(il => il.LineTotal)
            .HasColumnName("line_total")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        // Timestamps
        builder.Property(il => il.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(il => il.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()")
            .IsRequired();
    }
}
