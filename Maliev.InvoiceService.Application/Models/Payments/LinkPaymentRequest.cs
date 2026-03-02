using System.ComponentModel.DataAnnotations;

namespace Maliev.InvoiceService.Application.Models.Payments;

/// <summary>
/// Request model for linking an existing payment to a finalized invoice.
/// Supports partial or full payment allocation.
/// </summary>
public class LinkPaymentRequest
{
    /// <summary>
    /// Gets or sets the payment identifier (UUID) to link to the invoice.
    /// </summary>
    [Required]
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Gets or sets the amount to allocate from this payment to the invoice.
    /// Must not exceed the available payment balance or invoice outstanding amount.
    /// </summary>
    [Range(0.01, (double)decimal.MaxValue)]
    public decimal AllocatedAmount { get; set; }
}

/// <summary>
/// Request model for creating a new payment record in the system.
/// </summary>
public class CreatePaymentRequest
{
    /// <summary>
    /// Gets or sets the total payment amount received.
    /// </summary>
    [Range(0.01, (double)decimal.MaxValue)]
    public decimal PaymentAmount { get; set; }

    /// <summary>
    /// Gets or sets the date when the payment was received.
    /// Defaults to current UTC time.
    /// </summary>
    [Required]
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the payment method used (e.g., "BankTransfer", "Cash", "CreditCard", "Stripe").
    /// </summary>
    [Required]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional payment reference number (e.g., bank transaction ID, check number).
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Gets or sets optional notes or comments about the payment.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the user ID or name who recorded this payment.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string RecordedBy { get; set; } = string.Empty;
}

/// <summary>
/// Response model containing payment details after creation.
/// </summary>
public class PaymentResponse
{
    /// <summary>
    /// Gets or sets the unique payment identifier (UUID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the total payment amount received.
    /// </summary>
    public decimal PaymentAmount { get; set; }

    /// <summary>
    /// Gets or sets the date when the payment was received.
    /// </summary>
    public DateTime PaymentDate { get; set; }

    /// <summary>
    /// Gets or sets the payment method used (e.g., "BankTransfer", "Cash", "CreditCard", "Stripe").
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional payment reference number (e.g., bank transaction ID, check number).
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Gets or sets optional notes or comments about the payment.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the user ID or name who recorded this payment.
    /// </summary>
    public string RecordedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the payment record was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
