namespace Maliev.InvoiceService.Data.Models;

/// <summary>
/// Represents a payment received from a customer.
/// Can be allocated to one or more invoices through the InvoicePayment junction table.
/// </summary>
public class Payment
{
    /// <summary>
    /// Primary key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Total payment amount
    /// </summary>
    public decimal PaymentAmount { get; set; }

    /// <summary>
    /// Date payment was received
    /// </summary>
    public DateTime PaymentDate { get; set; }

    /// <summary>
    /// Payment method (BankTransfer, Check, CreditCard, Cash)
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// Bank reference or check number
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Additional payment notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// User ID who recorded the payment
    /// </summary>
    public string RecordedBy { get; set; } = string.Empty;

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // No navigation properties - InvoicePaymentAllocation is loosely coupled without FK constraint
}
