namespace Maliev.InvoiceService.Api.Models.Invoices;

/// <summary>
/// Request model for splitting an invoice into multiple child invoices.
/// </summary>
public class SplitInvoiceRequest
{
    /// <summary>
    /// Gets or sets the list of split rules defining how to divide the invoice.
    /// </summary>
    public List<InvoiceSplitRule> SplitRules { get; set; } = new();
}

/// <summary>
/// Defines a rule for splitting an invoice, specifying the percentage allocation for a child invoice.
/// </summary>
public class InvoiceSplitRule
{
    /// <summary>
    /// Gets or sets the percentage of the original invoice to allocate to this child invoice.
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// Gets or sets optional notes for this split rule.
    /// </summary>
    public string? Notes { get; set; }
}
