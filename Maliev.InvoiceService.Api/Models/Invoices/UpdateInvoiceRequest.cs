using System.ComponentModel.DataAnnotations;

namespace Maliev.InvoiceService.Api.Models.Invoices;

/// <summary>
/// Request model for updating an existing draft invoice.
/// Includes RowVersion for optimistic concurrency control.
/// </summary>
public class UpdateInvoiceRequest
{
    /// <summary>
    /// Gets or sets the customer name as it appears on the invoice.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer tax identification number (e.g., VAT number, TIN).
    /// </summary>
    [Required]
    [StringLength(50)]
    public string CustomerTaxId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the billing address for the invoice.
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string BillingAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional shipping address if different from billing address.
    /// </summary>
    public string? ShippingAddress { get; set; }

    /// <summary>
    /// Gets or sets the optional customer purchase order number for reference.
    /// </summary>
    public string? PoNumber { get; set; }

    /// <summary>
    /// Gets or sets the invoice currency code (ISO 4217, e.g., "THB", "USD").
    /// Defaults to "THB" (Thai Baht).
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3)]
    [RegularExpression("^[A-Z]{3}$")]
    public string Currency { get; set; } = "THB";

    /// <summary>
    /// Gets or sets the payment due date for the invoice.
    /// </summary>
    [Required]
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Gets or sets the payment terms in days (e.g., 30 for Net 30).
    /// Defaults to 30 days.
    /// </summary>
    [Range(1, 365)]
    public int PaymentTermsDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the optional late fee percentage applied to overdue invoices.
    /// </summary>
    [Range(0.0, 100.0)]
    public decimal? LateFeePercentage { get; set; }

    /// <summary>
    /// Gets or sets the withholding tax (WHT) percentage (0%, 1%, 2%, 3%, or 5%).
    /// Defaults to 0% (no withholding tax).
    /// </summary>
    [Range(0.0, 100.0)]
    public decimal WithholdingTaxPercentage { get; set; } = 0m;

    /// <summary>
    /// Gets or sets the optional manual exchange rate override.
    /// If null, exchange rate will be fetched from Currency Service.
    /// </summary>
    public decimal? ManualExchangeRate { get; set; }

    /// <summary>
    /// Gets or sets the list of invoice line items.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public List<InvoiceLineItemRequest> Lines { get; set; } = new();

    /// <summary>
    /// Gets or sets the row version for optimistic concurrency control.
    /// Must match the current version in the database to prevent conflicting updates.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required byte[] RowVersion { get; set; }
}
