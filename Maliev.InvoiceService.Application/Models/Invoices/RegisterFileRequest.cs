namespace Maliev.InvoiceService.Application.Models.Invoices;

/// <summary>
/// Request to register a file reference for an invoice (e.g., PDF, XML)
/// </summary>
public class RegisterFileRequest
{
    /// <summary>
    /// Type of file (e.g., "PDF", "XML")
    /// </summary>
    public required string FileType { get; set; }

    /// <summary>
    /// URL where the file is stored
    /// </summary>
    public required string FileUrl { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Service or user that generated the file
    /// </summary>
    public required string GeneratedBy { get; set; }

    /// <summary>
    /// File checksum for integrity verification (optional)
    /// </summary>
    public string? Checksum { get; set; }
}
