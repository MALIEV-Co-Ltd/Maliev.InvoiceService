using Maliev.InvoiceService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.InvoiceService.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for InvoicePaymentAllocation entity.
/// Uses composite primary key (invoice_id, payment_id) for idempotency.
/// Payment ID references Payment Service WITHOUT FK constraint (loose coupling).
/// </summary>
public class InvoicePaymentAllocationConfiguration : IEntityTypeConfiguration<InvoicePaymentAllocation>
{
    public void Configure(EntityTypeBuilder<InvoicePaymentAllocation> builder)
    {
        builder.ToTable("invoice_payment_allocations");

        // Composite Primary Key (idempotency)
        builder.HasKey(ipa => new { ipa.InvoiceId, ipa.PaymentId });

        // Foreign Key to Invoice
        builder.Property(ipa => ipa.InvoiceId)
            .HasColumnName("invoice_id")
            .IsRequired();

        // Payment ID (NO FK constraint - references Payment Service)
        builder.Property(ipa => ipa.PaymentId)
            .HasColumnName("payment_id")
            .IsRequired();

        // Allocated Amount
        builder.Property(ipa => ipa.AllocatedAmount)
            .HasColumnName("allocated_amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        // Allocation Date
        builder.Property(ipa => ipa.AllocationDate)
            .HasColumnName("allocation_date")
            .HasColumnType("timestamptz")
            .IsRequired();

        // Allocation Status
        builder.Property(ipa => ipa.AllocationStatus)
            .HasColumnName("allocation_status")
            .HasMaxLength(50)
            .HasDefaultValue("Confirmed")
            .IsRequired();

        // Allocated By
        builder.Property(ipa => ipa.AllocatedBy)
            .HasColumnName("allocated_by")
            .HasMaxLength(100)
            .HasDefaultValue("system")
            .IsRequired();

        // Created At
        builder.Property(ipa => ipa.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Relationships
        builder.HasOne(ipa => ipa.Invoice)
            .WithMany(i => i.InvoicePaymentAllocations)
            .HasForeignKey(ipa => ipa.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ipa => ipa.PaymentId)
            .HasDatabaseName("idx_invoice_payment_allocations_payment_id");

        builder.HasIndex(ipa => new { ipa.InvoiceId, ipa.AllocationStatus })
            .HasDatabaseName("idx_invoice_payment_allocations_invoice_status");
    }
}
