namespace Maliev.InvoiceService.Api.Models.BillingNotes;

/// <summary>
/// Request model for updating a billing note.
/// </summary>
public class UpdateBillingNoteRequest
{
    /// <summary>
    /// Updated payment due date.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Updated optional notes.
    /// </summary>
    public string? Notes { get; set; }
}
