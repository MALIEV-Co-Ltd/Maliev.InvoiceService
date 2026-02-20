namespace Maliev.InvoiceService.Data.Models;

/// <summary>
/// Junction entity linking a billing note to an invoice.
/// </summary>
public class BillingNoteInvoice
{
    /// <summary>
    /// Foreign key to BillingNote.
    /// </summary>
    public Guid BillingNoteId { get; set; }

    /// <summary>
    /// Foreign key to Invoice.
    /// </summary>
    public Guid InvoiceId { get; set; }

    /// <summary>
    /// Amount from the invoice included in this billing note.
    /// </summary>
    public decimal IncludedAmount { get; set; }

    // Navigation properties
    public BillingNote BillingNote { get; set; } = null!;
    public Invoice Invoice { get; set; } = null!;
}
