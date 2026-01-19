using Maliev.InvoiceService.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.InvoiceService.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for AuditLog entity.
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        // Primary Key
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .IsRequired();

        // Foreign Key
        builder.Property(a => a.InvoiceId)
            .HasColumnName("invoice_id")
            .IsRequired();
        builder.HasIndex(a => a.InvoiceId)
            .HasDatabaseName("idx_audit_logs_invoice_id");

        // Event Information
        builder.Property(a => a.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.Timestamp)
            .HasColumnName("timestamp")
            .HasColumnType("timestamptz")
            .IsRequired();
        builder.HasIndex(a => a.Timestamp)
            .HasDatabaseName("idx_audit_logs_timestamp")
            .IsDescending();

        builder.Property(a => a.ActorId)
            .HasColumnName("actor_id")
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(a => a.ActorId)
            .HasDatabaseName("idx_audit_logs_actor_id");

        // Changed Fields (JSONB)
        builder.Property(a => a.ChangedFields)
            .HasColumnName("changed_fields")
            .HasColumnType("jsonb");

        builder.Property(a => a.Reason)
            .HasColumnName("reason")
            .HasColumnType("text");

        // Archival
        builder.Property(a => a.IsArchived)
            .HasColumnName("is_archived")
            .HasDefaultValue(false)
            .IsRequired();
        builder.HasIndex(a => a.IsArchived)
            .HasDatabaseName("idx_audit_logs_archived")
            .HasFilter("is_archived = FALSE");

        // Timestamp
        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()")
            .IsRequired();
    }
}
