using System.ComponentModel.DataAnnotations;

namespace Maliev.InvoiceService.Application.Models.Invoices;

/// <summary>
/// Request model for registering PDF file reference from Upload Service callback.
/// </summary>
public class RegisterPdfFileReferenceRequest
{
    /// <summary>
    /// File reference (URL or ID) to the PDF document in Upload Service.
    /// </summary>
    [Required(ErrorMessage = "PDF file reference is required")]
    [MaxLength(1000, ErrorMessage = "PDF file reference must not exceed 1000 characters")]
    public required string PdfFileReference { get; set; }
}
