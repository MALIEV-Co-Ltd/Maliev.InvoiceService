namespace Maliev.InvoiceService.Api.Models.Invoices;

/// <summary>
/// Request model for finalizing an invoice.
/// </summary>
public class FinalizeInvoiceRequest
{
    /// <summary>
    /// Gets or sets the identifier of the user finalizing the invoice.
    /// </summary>
    public string FinalizedBy { get; set; } = string.Empty;
}
