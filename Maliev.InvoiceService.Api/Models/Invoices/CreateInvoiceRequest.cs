using System.ComponentModel.DataAnnotations;
using Maliev.InvoiceService.Api.Models.Common;
using Maliev.InvoiceService.Data.Models;

namespace Maliev.InvoiceService.Api.Models.Invoices;

/// <summary>
/// Request model for creating a new draft invoice.
/// </summary>
public class CreateInvoiceRequest
{
    /// <summary>
    /// Gets or sets the document type (TaxInvoice, Invoice, CreditNote, DebitNote).
    /// </summary>
    public DocumentType DocumentType { get; set; } = DocumentType.TaxInvoice;

    /// <summary>
    /// Gets or sets the quotation reference number if this invoice is generated from a quotation.
    /// </summary>
    public string? QuotationReference { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the customer.
    /// </summary>
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the billing identity type (Personal or Corporate).
    /// Determines whether to use customer's Thai National ID or company's tax ID.
    /// </summary>
    [Required]
    public BillingIdentityType BillingIdentityType { get; set; } = BillingIdentityType.Corporate;

    /// <summary>
    /// Gets or sets the customer's name.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer's tax identification number.
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
    /// Gets or sets the shipping address (optional, may differ from billing address).
    /// </summary>
    public string? ShippingAddress { get; set; }

    /// <summary>
    /// Gets or sets the customer's purchase order number.
    /// </summary>
    public string? PoNumber { get; set; }

    /// <summary>
    /// Gets or sets the currency code (ISO 4217 format, e.g., "THB", "USD").
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3)]
    [RegularExpression("^[A-Z]{3}$")]
    public string Currency { get; set; } = "THB";

    /// <summary>
    /// Gets or sets the invoice issue date.
    /// </summary>
    [Required]
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the payment due date.
    /// </summary>
    [Required]
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Gets or sets the number of days for payment terms (e.g., Net 30).
    /// </summary>
    [Range(1, 365)]
    public int PaymentTermsDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the credit term code (e.g., "NET30").
    /// </summary>
    public string? CreditTermCode { get; set; }

    /// <summary>
    /// Gets or sets the late fee percentage applied to overdue invoices.
    /// </summary>
    [Range(0.0, 100.0)]
    public decimal? LateFeePercentage { get; set; }

    /// <summary>
    /// Gets or sets the withholding tax percentage (0, 1, 2, 3, or 5 percent).
    /// </summary>
    [Range(0.0, 100.0)]
    public decimal WithholdingTaxPercentage { get; set; } = 0m;

    /// <summary>
    /// Gets or sets a manual exchange rate override (if not using automatic currency conversion).
    /// </summary>
    public decimal? ManualExchangeRate { get; set; }

    /// <summary>
    /// Gets or sets the list of line items for the invoice.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public List<InvoiceLineItemRequest> Lines { get; set; } = new();
}

/// <summary>
/// Represents a line item in an invoice, containing product/service details and pricing.
/// </summary>
public class InvoiceLineItemRequest
{
    /// <summary>
    /// Gets or sets the line number for display ordering.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int LineNumber { get; set; }

    /// <summary>
    /// Gets or sets the item/product code (SKU).
    /// </summary>
    [StringLength(100)]
    public string? ItemCode { get; set; }

    /// <summary>
    /// Gets or sets the description of the item or service.
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity of items.
    /// </summary>
    [Range(0.01, (double)decimal.MaxValue)]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the unit price per item.
    /// </summary>
    [Range(0.0, (double)decimal.MaxValue)]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the discount percentage applied to this line item.
    /// </summary>
    [Range(0.0, 100.0)]
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Gets or sets the tax category (e.g., "VAT", "Exempt").
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TaxCategory { get; set; } = "VAT";

    /// <summary>
    /// Gets or sets the tax rate percentage (e.g., 7.00 for 7% VAT).
    /// </summary>
    [Range(0.0, 100.0)]
    public decimal TaxRate { get; set; } = 7.00m;
}
