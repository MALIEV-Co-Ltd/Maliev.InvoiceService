namespace Maliev.InvoiceService.Application.Models.Invoices;

/// <summary>
/// Request to cancel a finalized invoice
/// </summary>
public class CancelInvoiceRequest
{
    /// <summary>
    /// User who cancelled the invoice
    /// </summary>
    public required string CancelledBy { get; set; }

    /// <summary>
    /// Reason for cancellation
    /// </summary>
    public required string CancellationReason { get; set; }
}
