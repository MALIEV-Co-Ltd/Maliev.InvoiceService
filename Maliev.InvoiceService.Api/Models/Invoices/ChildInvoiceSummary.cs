namespace Maliev.InvoiceService.Api.Models.Invoices;

/// <summary>
/// Lightweight summary of a child invoice created from a split operation.
/// </summary>
public class ChildInvoiceSummary
{
    /// <summary>
    /// Gets or sets the unique invoice identifier (UUID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the invoice number (e.g., "INV-2025-00001").
    /// </summary>
    public string? InvoiceNumber { get; set; }

    /// <summary>
    /// Gets or sets the grand total amount.
    /// </summary>
    public decimal GrandTotal { get; set; }

    /// <summary>
    /// Gets or sets the invoice status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the payment due date.
    /// </summary>
    public DateTime DueDate { get; set; }
}
