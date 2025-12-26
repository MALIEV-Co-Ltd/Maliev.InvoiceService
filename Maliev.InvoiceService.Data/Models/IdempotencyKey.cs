namespace Maliev.InvoiceService.Data.Models;

/// <summary>
/// Tracks idempotency keys to prevent duplicate processing of critical operations.
/// Used primarily for invoice finalization to prevent duplicate invoice numbers on network retries.
/// </summary>
public class IdempotencyKey
{
    /// <summary>
    /// The idempotency key provided by the client (typically a UUID)
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The operation/endpoint this key is for (e.g., "FinalizeInvoice")
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// The resource ID this operation affected (e.g., Invoice ID)
    /// </summary>
    public Guid ResourceId { get; set; }

    /// <summary>
    /// Serialized JSON response from the original request
    /// </summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code from the original request
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// When this idempotency key was first created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Expiration time for this key (typically 24 hours from creation)
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
