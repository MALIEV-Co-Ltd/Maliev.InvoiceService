using Maliev.InvoiceService.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.InvoiceService.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for IdempotencyKey entity.
/// Ensures idempotency keys are unique and automatically expired.
/// </summary>
public class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("idempotency_keys");

        // Composite Primary Key (Key + Operation)
        builder.HasKey(k => new { k.Key, k.Operation });

        builder.Property(k => k.Key)
            .HasColumnName("key")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(k => k.Operation)
            .HasColumnName("operation")
            .HasMaxLength(100)
            .IsRequired();

        // Resource Information
        builder.Property(k => k.ResourceId)
            .HasColumnName("resource_id")
            .IsRequired();
        builder.HasIndex(k => k.ResourceId)
            .HasDatabaseName("idx_idempotency_keys_resource_id");

        // Response Storage
        builder.Property(k => k.Response)
            .HasColumnName("response")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(k => k.StatusCode)
            .HasColumnName("status_code")
            .IsRequired();

        // Timestamps
        builder.Property(k => k.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(k => k.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamptz")
            .IsRequired();
        builder.HasIndex(k => k.ExpiresAt)
            .HasDatabaseName("idx_idempotency_keys_expires_at");
    }
}
