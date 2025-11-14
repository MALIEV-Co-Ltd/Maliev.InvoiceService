namespace Maliev.InvoiceService.Api.Services.External;

/// <summary>
/// Client interface for retrieving quotation data from the Quotation Service.
/// </summary>
public interface IQuotationServiceClient
{
    /// <summary>
    /// Retrieves a quotation by its reference number.
    /// </summary>
    /// <param name="quotationReference">Unique quotation reference number (e.g., "QUO-2025-001").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The quotation data if found, or null if not found.</returns>
    /// <exception cref="HttpRequestException">When Quotation Service is unavailable or returns an error.</exception>
    Task<QuotationDto?> GetQuotationAsync(string quotationReference, CancellationToken cancellationToken = default);
}

/// <summary>
/// Response DTO from Quotation Service containing quotation details.
/// </summary>
public class QuotationDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the quotation.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the quotation reference number.
    /// </summary>
    public string QuotationNumber { get; set; } = string.Empty;

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
    /// Gets or sets the billing address for the quotation.
    /// </summary>
    public string BillingAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the shipping address (optional, may differ from billing address).
    /// </summary>
    public string? ShippingAddress { get; set; }

    /// <summary>
    /// Gets or sets the currency code (ISO 4217 format, e.g., "THB", "USD").
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quotation issue date.
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Gets or sets the date until which the quotation is valid.
    /// </summary>
    public DateTime ValidUntil { get; set; }

    /// <summary>
    /// Gets or sets the number of days for payment terms (e.g., Net 30).
    /// </summary>
    public int PaymentTermsDays { get; set; }

    /// <summary>
    /// Gets or sets the list of line items in the quotation.
    /// </summary>
    public List<QuotationLineDto> Lines { get; set; } = new();
}

/// <summary>
/// Represents a line item in a quotation.
/// </summary>
public class QuotationLineDto
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
    public string TaxCategory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tax rate percentage (e.g., 7.00 for 7% VAT).
    /// </summary>
    public decimal TaxRate { get; set; }
}
