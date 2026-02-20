using Maliev.InvoiceService.Api.Models.BillingNotes;
using Maliev.InvoiceService.Data.Data;
using Maliev.InvoiceService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Maliev.InvoiceService.Api.Services;

/// <summary>
/// Service implementation for managing billing notes.
/// </summary>
public class BillingNoteService : IBillingNoteService
{
    private readonly InvoiceDbContext _context;
    private readonly ILogger<BillingNoteService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BillingNoteService"/> class.
    /// </summary>
    /// <param name="context">Database context.</param>
    /// <param name="logger">Logger instance.</param>
    public BillingNoteService(InvoiceDbContext context, ILogger<BillingNoteService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<BillingNoteResponse> CreateBillingNoteAsync(CreateBillingNoteRequest request, CancellationToken cancellationToken = default)
    {
        // Validate invoices
        var invoices = await _context.Invoices
            .Where(i => request.InvoiceIds.Contains(i.Id) && !i.IsDeleted)
            .ToListAsync(cancellationToken);

        if (invoices.Count != request.InvoiceIds.Count)
            throw new KeyNotFoundException("One or more invoices not found");

        // Verify all invoices belong to the customer
        if (invoices.Any(i => i.CustomerId != request.CustomerId))
            throw new InvalidOperationException("All invoices must belong to the specified customer");

        // Verify invoices are Finalized or PartiallyPaid
        if (invoices.Any(i => i.Status == "Draft" || i.Status == "Cancelled"))
            throw new InvalidOperationException("Only Finalized or PartiallyPaid invoices can be included in a billing note");

        // Generate billing note number
        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT nextval('invoice_number_seq')"; // Reuse invoice seq or create new one?
        // Usually separate sequence for Billing Notes.
        // I'll assume we reuse or I should have created 'billing_note_seq'.
        // For safety, I'll use invoice_number_seq but prefix with BN.
        if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
            await _context.Database.OpenConnectionAsync(cancellationToken);

        long nextSeq;
        try { nextSeq = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)); }
        finally { await _context.Database.CloseConnectionAsync(); }

        var billingNote = new BillingNote
        {
            Id = Guid.NewGuid(),
            BillingNoteNumber = $"BN-{DateTime.UtcNow:yyyyMMdd}-{nextSeq:D6}",
            CustomerId = request.CustomerId,
            CustomerName = invoices.First().CustomerName, // Snapshot
            CustomerTaxId = invoices.First().CustomerTaxId,
            BillingAddress = invoices.First().BillingAddress,
            IssueDate = request.IssueDate,
            DueDate = request.DueDate,
            PaymentTermsDays = request.PaymentTermsDays,
            Notes = request.Notes,
            Status = "Issued",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TotalAmount = invoices.Sum(i => i.GrandTotal) // Simplified: Sum of GrandTotals. Should ideally handle partial/remaining amounts.
        };

        foreach (var invoice in invoices)
        {
            billingNote.BillingNoteInvoices.Add(new BillingNoteInvoice
            {
                InvoiceId = invoice.Id,
                IncludedAmount = invoice.GrandTotal // Full amount for now
            });
        }

        _context.BillingNotes.Add(billingNote);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created billing note {BillingNoteId} with number {BillingNoteNumber}", billingNote.Id, billingNote.BillingNoteNumber);

        return MapToResponse(billingNote);
    }

    /// <inheritdoc/>
    public async Task<BillingNoteResponse?> GetBillingNoteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var bn = await _context.BillingNotes
            .Include(b => b.BillingNoteInvoices)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (bn == null) return null;

        return MapToResponse(bn);
    }

    /// <inheritdoc/>
    public async Task<BillingNoteResponse> UpdateBillingNoteAsync(Guid id, UpdateBillingNoteRequest request, CancellationToken cancellationToken = default)
    {
        var bn = await _context.BillingNotes
            .Include(b => b.BillingNoteInvoices)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Billing note {id} not found");

        if (bn.Status == "Cancelled")
            throw new InvalidOperationException("Cannot update a cancelled billing note");

        if (request.DueDate.HasValue)
            bn.DueDate = request.DueDate.Value;

        if (request.Notes != null)
            bn.Notes = request.Notes;

        bn.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponse(bn);
    }

    /// <inheritdoc/>
    public async Task<BillingNoteResponse> UpdateBillingNoteStatusAsync(Guid id, string newStatus, CancellationToken cancellationToken = default)
    {
        var allowedStatuses = new[] { "Issued", "PartiallyPaid", "FullyPaid", "Cancelled" };
        if (!allowedStatuses.Contains(newStatus))
            throw new InvalidOperationException($"Invalid status '{newStatus}'. Allowed values: {string.Join(", ", allowedStatuses)}");

        var bn = await _context.BillingNotes
            .Include(b => b.BillingNoteInvoices)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Billing note {id} not found");

        if (bn.Status == "Cancelled")
            throw new InvalidOperationException("Cannot transition a cancelled billing note");

        bn.Status = newStatus;
        bn.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponse(bn);
    }

    /// <inheritdoc/>
    public async Task DeleteBillingNoteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var bn = await _context.BillingNotes
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Billing note {id} not found");

        if (bn.Status != "Draft" && bn.Status != "Issued")
            throw new InvalidOperationException("Only Draft or Issued billing notes can be deleted");

        _context.BillingNotes.Remove(bn);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static BillingNoteResponse MapToResponse(BillingNote bn)
    {
        return new BillingNoteResponse
        {
            Id = bn.Id,
            BillingNoteNumber = bn.BillingNoteNumber,
            CustomerId = bn.CustomerId,
            CustomerName = bn.CustomerName,
            IssueDate = bn.IssueDate,
            DueDate = bn.DueDate,
            TotalAmount = bn.TotalAmount,
            Status = bn.Status,
            InvoiceIds = bn.BillingNoteInvoices.Select(i => i.InvoiceId).ToList()
        };
    }
}
