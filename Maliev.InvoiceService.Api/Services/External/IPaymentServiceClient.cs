namespace Maliev.InvoiceService.Api.Services.External;

/// <summary>
/// Client interface for interacting with the Payment Service API
/// to validate payment existence and retrieve payment details.
/// </summary>
public interface IPaymentServiceClient
{
    /// <summary>
    /// Retrieves payment details from the Payment Service by payment ID.
    /// </summary>
    /// <param name="paymentId">Unique payment identifier (UUID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response with status, amount, currency, and metadata</returns>
    /// <exception cref="HttpRequestException">When Payment Service is unavailable or returns error</exception>
    Task<ExternalPaymentResponse?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a payment exists and has "Succeeded" status in the Payment Service.
    /// </summary>
    /// <param name="paymentId">Unique payment identifier (UUID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if payment exists and status is "Succeeded", false otherwise</returns>
    /// <exception cref="HttpRequestException">When Payment Service is unavailable</exception>
    Task<bool> ValidatePaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Response DTO from Payment Service GET /payments/v1/payments/{id} endpoint.
/// Represents payment transaction data owned by Payment Service.
/// </summary>
public record ExternalPaymentResponse
{
    /// <summary>
    /// Unique payment identifier (UUID)
    /// </summary>
    public required Guid PaymentId { get; init; }

    /// <summary>
    /// Payment status (e.g., "Pending", "Succeeded", "Failed", "Refunded")
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Currency code (ISO 4217, e.g., "THB", "USD")
    /// </summary>
    public required string Currency { get; init; }

    /// <summary>
    /// Payment method (e.g., "Stripe", "BankTransfer", "Cash")
    /// </summary>
    public string? PaymentMethod { get; init; }

    /// <summary>
    /// Customer ID associated with payment
    /// </summary>
    public Guid? CustomerId { get; init; }

    /// <summary>
    /// Optional metadata (e.g., invoice IDs for auto-allocation)
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Payment creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; init; }
}
