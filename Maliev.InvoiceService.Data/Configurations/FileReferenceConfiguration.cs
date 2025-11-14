using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.InvoiceService.Data.Models;

namespace Maliev.InvoiceService.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for FileReference entity.
/// </summary>
public class FileReferenceConfiguration : IEntityTypeConfiguration<FileReference>
{
    public void Configure(EntityTypeBuilder<FileReference> builder)
    {
        builder.ToTable("file_references");

        // Primary Key
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .IsRequired();

        // Foreign Key
        builder.Property(f => f.InvoiceId)
            .HasColumnName("invoice_id")
            .IsRequired();
        builder.HasIndex(f => f.InvoiceId)
            .HasDatabaseName("idx_file_references_invoice_id");

        // File Information
        builder.Property(f => f.FileType)
            .HasColumnName("file_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(f => f.FileUrl)
            .HasColumnName("file_url")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(f => f.FileSizeBytes)
            .HasColumnName("file_size_bytes")
            .IsRequired();

        builder.Property(f => f.GeneratedBy)
            .HasColumnName("generated_by")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(f => f.Checksum)
            .HasColumnName("checksum")
            .HasMaxLength(200);

        // Timestamp
        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Relationships
        builder.HasOne(f => f.Invoice)
            .WithMany(i => i.FileReferences)
            .HasForeignKey(f => f.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
