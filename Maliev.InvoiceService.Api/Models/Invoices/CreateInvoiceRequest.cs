namespace Maliev.InvoiceService.Api.Models.Invoices;

/// <summary>
/// Request model for creating a new draft invoice.
/// </summary>
public class CreateInvoiceRequest
{
    /// <summary>
    /// Gets or sets the quotation reference number if this invoice is generated from a quotation.
    /// </summary>
    public string? QuotationReference { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the customer.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the customer's name.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer's tax identification number.
    /// </summary>
    public string CustomerTaxId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the billing address for the invoice.
    /// </summary>
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
    public string Currency { get; set; } = "THB";

    /// <summary>
    /// Gets or sets the invoice issue date.
    /// </summary>
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the payment due date.
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Gets or sets the number of days for payment terms (e.g., Net 30).
    /// </summary>
    public int PaymentTermsDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the late fee percentage applied to overdue invoices.
    /// </summary>
    public decimal? LateFeePercentage { get; set; }

    /// <summary>
    /// Gets or sets the withholding tax percentage (0, 1, 2, 3, or 5 percent).
    /// </summary>
    public decimal WithholdingTaxPercentage { get; set; } = 0m;

    /// <summary>
    /// Gets or sets a manual exchange rate override (if not using automatic currency conversion).
    /// </summary>
    public decimal? ManualExchangeRate { get; set; }

    /// <summary>
    /// Gets or sets the list of line items for the invoice.
    /// </summary>
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
    public int LineNumber { get; set; }

    /// <summary>
    /// Gets or sets the item/product code (SKU).
    /// </summary>
    public string? ItemCode { get; set; }

    /// <summary>
    /// Gets or sets the description of the item or service.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity of items.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the unit price per item.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the discount percentage applied to this line item.
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Gets or sets the tax category (e.g., "VAT", "Exempt").
    /// </summary>
    public string TaxCategory { get; set; } = "VAT";

    /// <summary>
    /// Gets or sets the tax rate percentage (e.g., 7.00 for 7% VAT).
    /// </summary>
    public decimal TaxRate { get; set; } = 7.00m;
}
