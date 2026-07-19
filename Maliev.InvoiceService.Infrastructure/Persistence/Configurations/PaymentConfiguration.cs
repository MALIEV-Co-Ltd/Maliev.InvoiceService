using Maliev.InvoiceService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.InvoiceService.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for Payment entity.
/// </summary>
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        // Primary Key
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .IsRequired();

        // Payment Information
        builder.Property(p => p.PaymentAmount)
            .HasColumnName("payment_amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.PaymentDate)
            .HasColumnName("payment_date")
            .HasColumnType("date")
            .IsRequired();
        builder.HasIndex(p => p.PaymentDate)
            .HasDatabaseName("idx_payments_payment_date")
            .IsDescending();

        builder.Property(p => p.PaymentMethod)
            .HasColumnName("payment_method")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.ReferenceNumber)
            .HasColumnName("reference_number")
            .HasMaxLength(200);
        builder.HasIndex(p => p.ReferenceNumber)
            .HasDatabaseName("idx_payments_reference_number")
            .HasFilter("reference_number IS NOT NULL");

        builder.Property(p => p.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        builder.Property(p => p.RecordedBy)
            .HasColumnName("recorded_by")
            .HasMaxLength(100)
            .IsRequired();

        // Timestamp
        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // No relationship to InvoicePaymentAllocation - designed for loose coupling
        // InvoicePaymentAllocation only stores PaymentId as a reference without FK constraint
    }
}
