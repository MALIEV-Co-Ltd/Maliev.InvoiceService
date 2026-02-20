using System.ComponentModel.DataAnnotations;

namespace Maliev.InvoiceService.Data.Models;

/// <summary>
/// Reference entity for payment terms (e.g., Net 30, COD).
/// </summary>
public class CreditTerm
{
    /// <summary>
    /// Unique code (PK), e.g., "NET30".
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the term.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Number of days for payment due.
    /// </summary>
    public int Days { get; set; }

    /// <summary>
    /// Whether the term is active for selection.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
