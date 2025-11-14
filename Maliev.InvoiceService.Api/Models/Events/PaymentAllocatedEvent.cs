namespace Maliev.InvoiceService.Api.Models.Events;

/// <summary>
/// Event published by Invoice Service when a payment is allocated to an invoice.
/// Consumed by Financial Service for accounting and revenue recognition.
///
/// Exchange: maliev.invoices
/// Routing Key: payment.allocated
/// </summary>
public record PaymentAllocatedEvent
{
    /// <summary>
    /// Invoice ID (UUID) that received the payment allocation
    /// </summary>
    public required Guid InvoiceId { get; init; }

    /// <summary>
    /// Invoice number (e.g., "INV-2025-00001")
    /// </summary>
    public required string InvoiceNumber { get; init; }

    /// <summary>
    /// Payment ID (UUID) from Payment Service
    /// </summary>
    public required Guid PaymentId { get; init; }

    /// <summary>
    /// Amount allocated from payment to this invoice
    /// </summary>
    public required decimal AllocatedAmount { get; init; }

    /// <summary>
    /// Currency code (ISO 4217, e.g., "THB", "USD")
    /// </summary>
    public required string Currency { get; init; }

    /// <summary>
    /// Customer ID associated with the invoice
    /// </summary>
    public required Guid CustomerId { get; init; }

    /// <summary>
    /// Updated invoice status after allocation ("PartiallyPaid" or "FullyPaid")
    /// </summary>
    public required string InvoiceStatus { get; init; }

    /// <summary>
    /// Outstanding balance remaining on invoice after allocation
    /// </summary>
    public required decimal OutstandingBalance { get; init; }

    /// <summary>
    /// User ID or "system" who performed the allocation
    /// </summary>
    public required string AllocatedBy { get; init; }

    /// <summary>
    /// Timestamp when allocation was created (UTC)
    /// </summary>
    public DateTime AllocationDate { get; init; }

    /// <summary>
    /// Timestamp when event was published (UTC)
    /// </summary>
    public DateTime EventTimestamp { get; init; }
}
