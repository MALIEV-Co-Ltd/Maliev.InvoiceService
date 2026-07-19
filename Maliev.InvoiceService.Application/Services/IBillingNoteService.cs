using Maliev.InvoiceService.Application.Models.BillingNotes;

namespace Maliev.InvoiceService.Application.Services;

/// <summary>
/// Service interface for managing billing notes.
/// </summary>
public interface IBillingNoteService
{
    /// <summary>
    /// Creates a new billing note grouping multiple invoices.
    /// </summary>
    Task<BillingNoteResponse> CreateBillingNoteAsync(CreateBillingNoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a billing note by ID.
    /// </summary>
    Task<BillingNoteResponse?> GetBillingNoteByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a billing note's editable fields.
    /// </summary>
    Task<BillingNoteResponse> UpdateBillingNoteAsync(Guid id, UpdateBillingNoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transitions a billing note to a new status.
    /// </summary>
    Task<BillingNoteResponse> UpdateBillingNoteStatusAsync(Guid id, string newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a billing note by ID.
    /// </summary>
    Task DeleteBillingNoteAsync(Guid id, CancellationToken cancellationToken = default);
}
