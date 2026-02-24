namespace Maliev.InvoiceService.Data.Models;

/// <summary>
/// Comprehensive audit trail for all invoice lifecycle events.
/// Retained for minimum 7 years per regulatory requirement.
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Primary key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to invoice
    /// </summary>
    public Guid InvoiceId { get; set; }

    /// <summary>
    /// Event type (Created, Updated, Finalized, Cancelled, PaymentLinked, Split)
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Event timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// User ID or system identifier
    /// </summary>
    public string ActorId { get; set; } = string.Empty;

    /// <summary>
    /// JSON object of changed field names and new values (for Update events)
    /// Example: {"customer_name": "Old Corp → New Corp", "grand_total": "50000 → 55000"}
    /// </summary>
    public string? ChangedFields { get; set; }

    /// <summary>
    /// Reason for action (e.g., cancellation reason)
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Flag indicating if log has been archived to cold storage
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Invoice Invoice { get; set; } = null!;
}
