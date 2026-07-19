using System.ComponentModel.DataAnnotations;

namespace Maliev.InvoiceService.Application.Models.BillingNotes;

/// <summary>
/// Request model for creating a billing note.
/// </summary>
public class CreateBillingNoteRequest
{
    /// <summary>
    /// Customer identifier.
    /// </summary>
    [Required]
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Issue date of the billing note.
    /// </summary>
    [Required]
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Payment due date.
    /// </summary>
    [Required]
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Payment terms in days.
    /// </summary>
    public int PaymentTermsDays { get; set; } = 30;

    /// <summary>
    /// Optional notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// List of invoice IDs to include in the billing note.
    /// </summary>
    [Required]
    public List<Guid> InvoiceIds { get; set; } = new();
}
