namespace Maliev.InvoiceService.Application.Models.BillingNotes;

/// <summary>
/// Response model for a billing note.
/// </summary>
public class BillingNoteResponse
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Billing note number.
    /// </summary>
    public string? BillingNoteNumber { get; set; }

    /// <summary>
    /// Customer ID.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Customer name.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Issue date.
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Due date.
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Total amount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Included invoice IDs.
    /// </summary>
    public List<Guid> InvoiceIds { get; set; } = new();
}
