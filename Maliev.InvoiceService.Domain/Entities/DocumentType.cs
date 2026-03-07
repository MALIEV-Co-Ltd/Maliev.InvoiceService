namespace Maliev.InvoiceService.Domain.Entities;

/// <summary>
/// Defines the type of billing document.
/// </summary>
public enum DocumentType
{
    /// <summary>
    /// Legal VAT document (Bai Kam Kub Pa See).
    /// </summary>
    TaxInvoice = 1,

    /// <summary>
    /// Non-VAT invoice or debit note (Bai Chang Nee).
    /// </summary>
    Invoice = 2,

    /// <summary>
    /// Credit Note (Bai Lod Nee) - reduces a previous tax invoice.
    /// </summary>
    CreditNote = 3,

    /// <summary>
    /// Debit Note (Bai Perm Nee) - increases a previous tax invoice.
    /// </summary>
    DebitNote = 4
}
