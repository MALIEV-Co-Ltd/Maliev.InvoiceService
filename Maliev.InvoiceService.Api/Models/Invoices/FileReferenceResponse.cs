namespace Maliev.InvoiceService.Api.Models.Invoices;

/// <summary>
/// Response containing file reference details
/// </summary>
public class FileReferenceResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for the file reference.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the invoice identifier this file is associated with.
    /// </summary>
    public Guid InvoiceId { get; set; }

    /// <summary>
    /// Gets or sets the type of file (e.g., "PDF", "XML").
    /// </summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL where the file is stored.
    /// </summary>
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the service or user that generated the file.
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file checksum for integrity verification.
    /// </summary>
    public string? Checksum { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the file reference was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
