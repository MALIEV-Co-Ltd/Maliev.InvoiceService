namespace Maliev.InvoiceService.Data.Models;

/// <summary>
/// Represents individual line items on an invoice.
/// </summary>
public class InvoiceLine
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
    /// Line item sequence number (1, 2, 3...)
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Product/service code
    /// </summary>
    public string? ItemCode { get; set; }

    /// <summary>
    /// Line item description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Quantity (supports fractional quantities)
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Price per unit
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Discount percentage (0-100)
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Tax category (VAT, Exempt, ZeroRated)
    /// </summary>
    public string TaxCategory { get; set; } = "VAT";

    /// <summary>
    /// Tax rate percentage (e.g., 7% VAT in Thailand)
    /// </summary>
    public decimal TaxRate { get; set; } = 7.00m;

    /// <summary>
    /// Calculated: (quantity * unit_price) * (1 - discount/100)
    /// </summary>
    public decimal LineSubtotal { get; set; }

    /// <summary>
    /// Calculated: line_subtotal * (tax_rate / 100)
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Calculated: line_subtotal + tax_amount
    /// </summary>
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Record last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Invoice Invoice { get; set; } = null!;
}
