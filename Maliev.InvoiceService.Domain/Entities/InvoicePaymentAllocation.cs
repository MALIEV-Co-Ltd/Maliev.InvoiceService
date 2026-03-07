namespace Maliev.InvoiceService.Domain.Entities;

/// <summary>
/// Tracks allocation of payments (from Payment Service) to invoices.
/// This table stores references to payment IDs from the Payment Service and does not own payment transaction data.
/// Payment IDs reference Payment Service WITHOUT foreign key constraints (loose coupling).
/// </summary>
public class InvoicePaymentAllocation
{
    /// <summary>
    /// Foreign key to invoice (composite PK)
    /// </summary>
    public Guid InvoiceId { get; set; }

    /// <summary>
    /// Payment ID from Payment Service (composite PK, NO FK constraint)
    /// </summary>
    public Guid PaymentId { get; set; }

    /// <summary>
    /// Amount of payment allocated to this invoice
    /// </summary>
    public decimal AllocatedAmount { get; set; }

    /// <summary>
    /// Allocation date
    /// </summary>
    public DateTime AllocationDate { get; set; }

    /// <summary>
    /// Allocation status: "Confirmed", "Reversed"
    /// </summary>
    public string AllocationStatus { get; set; } = "Confirmed";

    /// <summary>
    /// User who allocated the payment (user ID or "system")
    /// </summary>
    public string AllocatedBy { get; set; } = "system";

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Invoice Invoice { get; set; } = null!;
}
