using Maliev.InvoiceService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.InvoiceService.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for Invoice entity.
/// Follows PostgreSQL snake_case naming convention and includes all indexes from data model.
/// </summary>
public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        // Primary Key
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .IsRequired();

        // Invoice Number
        builder.Property(i => i.InvoiceNumber)
            .HasColumnName("invoice_number")
            .HasMaxLength(50);
        builder.HasIndex(i => i.InvoiceNumber)
            .IsUnique()
            .HasDatabaseName("idx_invoices_invoice_number_unique");

        // Parent Invoice Relationship (Self-referencing)
        builder.Property(i => i.ParentInvoiceId)
            .HasColumnName("parent_invoice_id");
        builder.HasOne(i => i.ParentInvoice)
            .WithMany(i => i.ChildInvoices)
            .HasForeignKey(i => i.ParentInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(i => i.ParentInvoiceId)
            .HasDatabaseName("idx_invoices_parent_id")
            .HasFilter("parent_invoice_id IS NOT NULL");

        // Customer Information
        builder.Property(i => i.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();
        builder.HasIndex(i => i.CustomerId)
            .HasDatabaseName("idx_invoices_customer_id");

        builder.Property(i => i.CustomerName)
            .HasColumnName("customer_name")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(i => i.CustomerTaxId)
            .HasColumnName("customer_tax_id")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(i => i.BillingAddress)
            .HasColumnName("billing_address")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(i => i.ShippingAddress)
            .HasColumnName("shipping_address")
            .HasColumnType("text");

        // References
        builder.Property(i => i.QuotationReference)
            .HasColumnName("quotation_reference")
            .HasMaxLength(100);
        builder.HasIndex(i => i.QuotationReference)
            .HasDatabaseName("idx_invoices_quotation_reference")
            .HasFilter("quotation_reference IS NOT NULL");

        builder.Property(i => i.PoNumber)
            .HasColumnName("po_number")
            .HasMaxLength(100);

        // Status
        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasDefaultValue("Draft")
            .IsRequired();
        builder.HasIndex(i => i.Status)
            .HasDatabaseName("idx_invoices_status");

        // Currency
        builder.Property(i => i.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .HasDefaultValue("THB")
            .IsRequired();

        builder.Property(i => i.ExchangeRate)
            .HasColumnName("exchange_rate")
            .HasColumnType("decimal(18,6)");

        builder.Property(i => i.ExchangeRateSource)
            .HasColumnName("exchange_rate_source")
            .HasMaxLength(100);

        // Financial Amounts
        builder.Property(i => i.Subtotal)
            .HasColumnName("subtotal")
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(i => i.TaxAmount)
            .HasColumnName("tax_amount")
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(i => i.WithholdingTaxAmount)
            .HasColumnName("withholding_tax_amount")
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(i => i.GrandTotal)
            .HasColumnName("grand_total")
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0)
            .IsRequired();

        // Dates
        builder.Property(i => i.IssueDate)
            .HasColumnName("issue_date")
            .HasColumnType("date")
            .IsRequired();
        builder.HasIndex(i => i.IssueDate)
            .HasDatabaseName("idx_invoices_issue_date")
            .IsDescending();

        builder.Property(i => i.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("date")
            .IsRequired();
        builder.HasIndex(i => i.DueDate)
            .HasDatabaseName("idx_invoices_due_date");

        builder.Property(i => i.PaymentTermsDays)
            .HasColumnName("payment_terms_days")
            .HasDefaultValue(30)
            .IsRequired();

        builder.Property(i => i.CreditTermCode)
            .HasColumnName("credit_term_code")
            .HasMaxLength(20);
        builder.HasOne(i => i.CreditTerm)
            .WithMany()
            .HasForeignKey(i => i.CreditTermCode)
            .HasPrincipalKey(ct => ct.Code)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.Property(i => i.LateFeePercentage)
            .HasColumnName("late_fee_percentage")
            .HasColumnType("decimal(5,2)");

        // Finalization
        builder.Property(i => i.FinalizedAt)
            .HasColumnName("finalized_at")
            .HasColumnType("timestamptz");

        builder.Property(i => i.FinalizedBy)
            .HasColumnName("finalized_by")
            .HasMaxLength(100);

        // Cancellation
        builder.Property(i => i.CancelledAt)
            .HasColumnName("cancelled_at")
            .HasColumnType("timestamptz");

        builder.Property(i => i.CancelledBy)
            .HasColumnName("cancelled_by")
            .HasMaxLength(100);

        builder.Property(i => i.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasColumnType("text");

        // PDF File Reference
        builder.Property(i => i.PdfFileReference)
            .HasColumnName("pdf_file_reference")
            .HasMaxLength(1000);

        // Soft Delete
        builder.Property(i => i.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        // Optimistic Concurrency - CRITICAL for PostgreSQL
        builder.Property(i => i.RowVersion)
            .HasColumnName("row_version")
            .IsConcurrencyToken()  // Use ConcurrencyToken instead of IsRowVersion for PostgreSQL bytea
            .ValueGeneratedNever()  // CRITICAL: Must manually increment in SaveChangesAsync
            .IsRequired();

        // Timestamps
        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(i => i.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Composite Indexes
        builder.HasIndex(i => new { i.CustomerId, i.Status, i.IssueDate })
            .HasDatabaseName("idx_invoices_customer_status_dates")
            .HasFilter("is_deleted = FALSE")
            .IsDescending(false, false, true);

        builder.HasIndex(i => new { i.DueDate, i.GrandTotal })
            .HasDatabaseName("idx_invoices_pending_payment")
            .HasFilter("status IN ('Finalized', 'PartiallyPaid') AND is_deleted = FALSE");

        // Relationships
        builder.HasMany(i => i.Lines)
            .WithOne(l => l.Invoice)
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.InvoicePaymentAllocations)
            .WithOne(ipa => ipa.Invoice)
            .HasForeignKey(ipa => ipa.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.AuditLogs)
            .WithOne(a => a.Invoice)
            .HasForeignKey(a => a.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
