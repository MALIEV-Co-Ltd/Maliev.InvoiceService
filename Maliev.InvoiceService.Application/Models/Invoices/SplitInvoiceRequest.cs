using System.ComponentModel.DataAnnotations;

namespace Maliev.InvoiceService.Application.Models.Invoices;

/// <summary>
/// Request model for splitting an invoice into multiple child invoices.
/// </summary>
public class SplitInvoiceRequest
{
    /// <summary>
    /// Gets or sets the list of split rules defining how to divide the invoice.
    /// </summary>
    [Required]
    [MinLength(2)]
    public List<InvoiceSplitRule> SplitRules { get; set; } = new();

    /// <summary>
    /// Gets or sets the reason for splitting the invoice.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Defines a rule for splitting an invoice, specifying the percentage allocation for a child invoice.
/// </summary>
public class InvoiceSplitRule
{
    /// <summary>
    /// Gets or sets the percentage of the original invoice to allocate to this child invoice.
    /// </summary>
    [Range(0.01, 100.0)]
    public decimal Percentage { get; set; }

    /// <summary>
    /// Gets or sets optional notes for this split rule.
    /// </summary>
    [StringLength(2000)]
    public string? Notes { get; set; }
}
