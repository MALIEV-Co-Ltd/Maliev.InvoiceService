namespace Maliev.InvoiceService.Application.Models.Audit;

/// <summary>
/// Response containing audit log entry details
/// </summary>
public class AuditLogResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for the audit log entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the type of entity that was audited (e.g., "Invoice", "Payment").
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the entity that was audited.
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Gets or sets the action performed on the entity (e.g., "Created", "Updated", "Deleted").
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the action was performed.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who performed the action.
    /// </summary>
    public string? PerformedBy { get; set; }

    /// <summary>
    /// Gets or sets the JSON representation of changes made to the entity.
    /// </summary>
    public string? Changes { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the action was performed.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent string of the client that performed the action.
    /// </summary>
    public string? UserAgent { get; set; }
}
