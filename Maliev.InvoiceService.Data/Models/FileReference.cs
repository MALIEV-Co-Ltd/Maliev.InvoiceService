namespace Maliev.InvoiceService.Data.Models;

/// <summary>
/// Represents a file reference (PDF, XML, etc.) associated with an invoice
/// </summary>
public class FileReference
{
    /// <summary>
    /// Primary key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to invoice
    /// </summary>
    public Guid InvoiceId { get; set; }

    /// <summary>
    /// Type of file (PDF, XML, etc.)
    /// </summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// URL where the file is stored
    /// </summary>
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Service or user that generated the file
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;

    /// <summary>
    /// File checksum for integrity verification
    /// </summary>
    public string? Checksum { get; set; }

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Invoice Invoice { get; set; } = null!;
}
