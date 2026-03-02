using System.ComponentModel.DataAnnotations;

namespace Maliev.InvoiceService.Application.Models.BillingNotes;

/// <summary>
/// Request model for updating a billing note status.
/// </summary>
public class UpdateBillingNoteStatusRequest
{
    /// <summary>
    /// New status (Issued, PartiallyPaid, FullyPaid, Cancelled).
    /// </summary>
    [Required]
    public required string Status { get; set; }
}
