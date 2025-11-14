namespace Maliev.InvoiceService.Api.Models.Events;

/// <summary>
/// Event published by Payment Service when a payment succeeds.
/// Consumed by Invoice Service via RabbitMQ for automatic payment allocation.
///
/// Exchange: maliev.payments
/// Routing Key: payment.succeeded
/// </summary>
public record PaymentSucceededEvent
{
    /// <summary>
    /// Unique payment identifier (UUID) from Payment Service
    /// </summary>
    public required Guid PaymentId { get; init; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Currency code (ISO 4217, e.g., "THB", "USD")
    /// </summary>
    public required string Currency { get; init; }

    /// <summary>
    /// Customer ID associated with the payment
    /// </summary>
    public required Guid CustomerId { get; init; }

    /// <summary>
    /// Payment method used (e.g., "Stripe", "BankTransfer", "Cash")
    /// </summary>
    public string? PaymentMethod { get; init; }

    /// <summary>
    /// Optional metadata containing invoice IDs for auto-allocation.
    /// Example: { "invoice_ids": "inv-001,inv-002", "allocation_mode": "auto" }
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Timestamp when payment succeeded (UTC)
    /// </summary>
    public DateTime SucceededAt { get; init; }

    /// <summary>
    /// Timestamp when event was published (UTC)
    /// </summary>
    public DateTime EventTimestamp { get; init; }
}
