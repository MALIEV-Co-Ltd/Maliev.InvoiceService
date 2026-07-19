namespace Maliev.InvoiceService.Domain.Entities;

/// <summary>
/// Represents a billing note (Thai: ใบวางบิล) which groups one or more invoices for collection.
/// </summary>
public class BillingNote
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique billing note number (e.g., BN-20250101-0001).
    /// </summary>
    public string? BillingNoteNumber { get; set; }

    /// <summary>
    /// Customer identifier.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Customer name snapshot.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Customer tax ID snapshot.
    /// </summary>
    public string CustomerTaxId { get; set; } = string.Empty;

    /// <summary>
    /// Billing address snapshot.
    /// </summary>
    public string BillingAddress { get; set; } = string.Empty;

    /// <summary>
    /// Date issued.
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Due date for payment.
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Payment terms in days.
    /// </summary>
    public int PaymentTermsDays { get; set; }

    /// <summary>
    /// Total amount collected in this billing note.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string Currency { get; set; } = "THB";

    /// <summary>
    /// Status (Draft, Issued, PartiallyPaid, FullyPaid, Cancelled).
    /// </summary>
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// Optional notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Linked invoices.
    /// </summary>
    public ICollection<BillingNoteInvoice> BillingNoteInvoices { get; set; } = new List<BillingNoteInvoice>();
}
