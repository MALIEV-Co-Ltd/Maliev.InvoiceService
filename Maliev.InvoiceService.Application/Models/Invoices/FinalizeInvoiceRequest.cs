using System.ComponentModel.DataAnnotations;

namespace Maliev.InvoiceService.Application.Models.Invoices;

/// <summary>
/// Request model for finalizing an invoice.
/// </summary>
public class FinalizeInvoiceRequest
{
    /// <summary>
    /// Gets or sets the identifier of the user finalizing the invoice.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string FinalizedBy { get; set; } = string.Empty;
}
