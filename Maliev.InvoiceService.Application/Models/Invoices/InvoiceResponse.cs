namespace Maliev.InvoiceService.Application.Models.Invoices;

/// <summary>
/// Response model containing complete invoice details including header, line items, and audit information.
/// </summary>
public class InvoiceResponse
{
    /// <summary>
    /// Gets or sets the unique invoice identifier (UUID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the sequential invoice number (e.g., "INV-2025-00001").
    /// Null for draft invoices; assigned when finalized.
    /// </summary>
    public string? InvoiceNumber { get; set; }

    /// <summary>
    /// Gets or sets the parent invoice ID if this invoice was created via split operation.
    /// Null for original invoices.
    /// </summary>
    public Guid? ParentInvoiceId { get; set; }

    /// <summary>
    /// Gets or sets the customer identifier (UUID) from Customer Service.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the customer name as it appears on the invoice.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer tax identification number (e.g., VAT number, TIN).
    /// </summary>
    public string CustomerTaxId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the billing address for the invoice.
    /// </summary>
    public string BillingAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional shipping address if different from billing address.
    /// </summary>
    public string? ShippingAddress { get; set; }

    /// <summary>
    /// Gets or sets the optional quotation reference number if invoice was generated from a quotation.
    /// </summary>
    public string? QuotationReference { get; set; }

    /// <summary>
    /// Gets or sets the optional customer purchase order number for reference.
    /// </summary>
    public string? PoNumber { get; set; }

    /// <summary>
    /// Gets or sets the current invoice status (e.g., "Draft", "Finalized", "PartiallyPaid", "FullyPaid", "Cancelled").
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the invoice currency code (ISO 4217, e.g., "THB", "USD").
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exchange rate used for currency conversion.
    /// Null if invoice is in base currency (THB).
    /// </summary>
    public decimal? ExchangeRate { get; set; }

    /// <summary>
    /// Gets or sets the source of the exchange rate (e.g., "CurrencyService", "Manual").
    /// </summary>
    public string? ExchangeRateSource { get; set; }

    /// <summary>
    /// Gets or sets the subtotal amount before taxes (sum of all line items).
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Gets or sets the total tax amount (VAT).
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Gets or sets the withholding tax (WHT) amount deducted from grand total.
    /// </summary>
    public decimal WithholdingTaxAmount { get; set; }

    /// <summary>
    /// Gets or sets the grand total amount (Subtotal + TaxAmount - WithholdingTaxAmount).
    /// </summary>
    public decimal GrandTotal { get; set; }

    /// <summary>
    /// Gets or sets the total confirmed payment amount allocated to this invoice.
    /// </summary>
    public decimal PaidAmount { get; set; }

    /// <summary>
    /// Gets or sets the remaining confirmed outstanding balance for this invoice.
    /// </summary>
    public decimal OutstandingBalance { get; set; }

    /// <summary>
    /// Gets or sets the invoice issue date.
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Gets or sets the payment due date.
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Gets or sets the payment terms in days (e.g., 30 for Net 30).
    /// </summary>
    public int PaymentTermsDays { get; set; }

    /// <summary>
    /// Gets or sets the optional late fee percentage applied to overdue invoices.
    /// </summary>
    public decimal? LateFeePercentage { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the invoice was finalized (UTC).
    /// Null for draft invoices.
    /// </summary>
    public DateTime? FinalizedAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID or name who finalized the invoice.
    /// Null for draft invoices.
    /// </summary>
    public string? FinalizedBy { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the invoice was cancelled (UTC).
    /// Null for non-cancelled invoices.
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID or name who cancelled the invoice.
    /// Null for non-cancelled invoices.
    /// </summary>
    public string? CancelledBy { get; set; }

    /// <summary>
    /// Gets or sets the reason for invoice cancellation.
    /// Null for non-cancelled invoices.
    /// </summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Gets or sets the file reference ID for the generated PDF invoice.
    /// Null if PDF has not been generated yet.
    /// </summary>
    public string? PdfFileReference { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the invoice was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the principal that created the invoice, when available from the audit trail.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the invoice was last updated (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the list of invoice line items.
    /// </summary>
    public List<InvoiceLineResponse> Lines { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of child invoices created via splitting.
    /// </summary>
    public List<ChildInvoiceSummary> ChildInvoiceSummaries { get; set; } = new();
}

/// <summary>
/// Response model representing a single line item on an invoice.
/// </summary>
public class InvoiceLineResponse
{
    /// <summary>
    /// Gets or sets the unique line item identifier (UUID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the sequential line number within the invoice (1-based).
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Gets or sets the optional item/product code (SKU).
    /// </summary>
    public string? ItemCode { get; set; }

    /// <summary>
    /// Gets or sets the line item description.
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
    /// Gets or sets the discount percentage applied to this line (0-100).
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Gets or sets the tax category (e.g., "Standard", "Reduced", "Zero").
    /// </summary>
    public string TaxCategory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tax rate as a decimal (e.g., 0.07 for 7% VAT).
    /// </summary>
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Gets or sets the line subtotal before tax (Quantity * UnitPrice * (1 - DiscountPercentage/100)).
    /// </summary>
    public decimal LineSubtotal { get; set; }

    /// <summary>
    /// Gets or sets the tax amount for this line item.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Gets or sets the line total including tax (LineSubtotal + TaxAmount).
    /// </summary>
    public decimal LineTotal { get; set; }
}
