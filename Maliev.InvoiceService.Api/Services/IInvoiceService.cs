using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Api.Models.Common;
using Maliev.InvoiceService.Api.Models.Payments;
using Maliev.InvoiceService.Api.Models.Audit;

namespace Maliev.InvoiceService.Api.Services;

/// <summary>
/// Service interface for managing invoices, payments, file references, and audit trails.
/// Provides business logic for invoice lifecycle management with external service integration.
/// </summary>
public interface IInvoiceService
{
    // Invoice operations

    /// <summary>
    /// Creates a new draft invoice, optionally generating from a quotation reference.
    /// Performs currency conversion, withholding tax calculation, and line item validation.
    /// </summary>
    /// <param name="request">Invoice creation request with customer details and line items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created invoice with Draft status.</returns>
    Task<InvoiceResponse> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an invoice by its unique identifier.
    /// </summary>
    /// <param name="id">Invoice ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Invoice details if found, or null if not found.</returns>
    Task<InvoiceResponse?> GetInvoiceByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of invoices with optional filtering by status and customer.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="status">Optional status filter (e.g., "Draft", "Finalized", "Paid").</param>
    /// <param name="customerId">Optional customer ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated response containing invoices and pagination metadata.</returns>
    Task<PaginatedResponse<InvoiceResponse>> GetPaginatedInvoicesAsync(int page, int pageSize, string? status = null, Guid? customerId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches invoices using comprehensive filtering, sorting, and full-text search capabilities.
    /// </summary>
    /// <param name="request">Search request with filters, date ranges, sorting options, and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated response containing matching invoices.</returns>
    Task<PaginatedResponse<InvoiceResponse>> SearchInvoicesAsync(InvoiceSearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalizes a draft invoice, assigning a sequential invoice number and making it immutable.
    /// Only invoices in Draft status can be finalized.
    /// </summary>
    /// <param name="id">Invoice ID.</param>
    /// <param name="finalizedBy">User performing the finalization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The finalized invoice with assigned invoice number.</returns>
    /// <exception cref="InvalidOperationException">If invoice is not in Draft status.</exception>
    Task<InvoiceResponse> FinalizeInvoiceAsync(Guid id, string finalizedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an invoice with a specified reason.
    /// Cancelled invoices cannot be modified or finalized.
    /// </summary>
    /// <param name="id">Invoice ID.</param>
    /// <param name="cancelledBy">User performing the cancellation.</param>
    /// <param name="reason">Reason for cancellation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cancelled invoice.</returns>
    Task<InvoiceResponse> CancelInvoiceAsync(Guid id, string cancelledBy, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a draft invoice with new details. Only Draft invoices can be updated.
    /// </summary>
    /// <param name="id">Invoice ID.</param>
    /// <param name="request">Updated invoice details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated invoice.</returns>
    /// <exception cref="InvalidOperationException">If invoice is not in Draft status.</exception>
    Task<InvoiceResponse> UpdateInvoiceAsync(Guid id, CreateInvoiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a draft invoice. Only Draft invoices can be deleted.
    /// </summary>
    /// <param name="id">Invoice ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">If invoice is not in Draft status.</exception>
    Task DeleteInvoiceAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Splits a finalized invoice into multiple new invoices based on percentage allocations.
    /// The original invoice is cancelled and child invoices retain references to the parent.
    /// </summary>
    /// <param name="id">Invoice ID to split.</param>
    /// <param name="request">Split request with percentage rules for each new invoice.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of newly created split invoices.</returns>
    /// <exception cref="InvalidOperationException">If invoice is not in Finalized status or split percentages don't sum to 100%.</exception>
    Task<List<InvoiceResponse>> SplitInvoiceAsync(Guid id, SplitInvoiceRequest request, CancellationToken cancellationToken = default);

    // Export operations

    /// <summary>
    /// Exports invoices matching the search criteria to a specified format (CSV or Excel).
    /// </summary>
    /// <param name="request">Search criteria for invoices to export.</param>
    /// <param name="format">Export format ("csv" or "xlsx").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Base64-encoded file content.</returns>
    Task<string> ExportInvoicesAsync(InvoiceSearchRequest request, string format, CancellationToken cancellationToken = default);

    // File operations

    /// <summary>
    /// Registers a file reference (e.g., PDF, attachment) for an invoice.
    /// </summary>
    /// <param name="invoiceId">Invoice ID.</param>
    /// <param name="request">File registration request with file path and metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registered file reference details.</returns>
    Task<FileReferenceResponse> RegisterFileAsync(Guid invoiceId, RegisterFileRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all file references associated with an invoice.
    /// </summary>
    /// <param name="invoiceId">Invoice ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of file references.</returns>
    Task<List<FileReferenceResponse>> GetFileReferencesAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a PDF file reference for an invoice (typically from PdfService integration).
    /// </summary>
    /// <param name="invoiceId">Invoice ID.</param>
    /// <param name="pdfFileReference">File path or reference to the PDF document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RegisterPdfFileReferenceAsync(Guid invoiceId, string pdfFileReference, CancellationToken cancellationToken = default);

    // Audit operations

    /// <summary>
    /// Retrieves the complete audit trail for an invoice, showing all changes and actions.
    /// </summary>
    /// <param name="invoiceId">Invoice ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit log entries ordered by timestamp.</returns>
    Task<List<AuditLogResponse>> GetAuditTrailAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    // Payment operations

    /// <summary>
    /// Creates a new payment record.
    /// </summary>
    /// <param name="request">Payment creation request with amount, method, and date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created payment details.</returns>
    Task<PaymentResponse> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a payment by its unique identifier.
    /// </summary>
    /// <param name="id">Payment ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payment details if found, or null if not found.</returns>
    Task<PaymentResponse?> GetPaymentByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Links a payment to an invoice and automatically updates invoice status based on payment allocation.
    /// Invoice status transitions: Finalized → PartiallyPaid → Paid.
    /// </summary>
    /// <param name="invoiceId">Invoice ID.</param>
    /// <param name="request">Payment linking request with payment ID and allocated amount.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated invoice with new payment status.</returns>
    Task<InvoiceResponse> LinkPaymentAsync(Guid invoiceId, LinkPaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Allocates a payment to an invoice (internal method used by MassTransit consumer for auto-allocation).
    /// Creates an InvoicePaymentAllocation record and updates invoice status.
    /// </summary>
    /// <param name="invoiceId">Invoice ID.</param>
    /// <param name="paymentId">Payment ID.</param>
    /// <param name="allocatedAmount">Amount to allocate from the payment.</param>
    /// <param name="allocatedBy">User or system performing the allocation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AllocatePaymentAsync(Guid invoiceId, Guid paymentId, decimal allocatedAmount, string allocatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the outstanding balance for an invoice (TotalWithWHT - sum of allocated payments).
    /// </summary>
    /// <param name="invoiceId">Invoice ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Outstanding balance amount.</returns>
    Task<decimal> CalculateOutstandingBalanceAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    // Currency conversion reporting

    /// <summary>
    /// Retrieves currency conversion details for an invoice, including exchange rates and converted amounts.
    /// </summary>
    /// <param name="invoiceId">Invoice ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary containing conversion metadata (rate, source currency, target currency).</returns>
    Task<Dictionary<string, object>> GetCurrencyConversionReportAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    // Analytics and reporting

    /// <summary>
    /// Retrieves analytics summary including invoice counts, revenue totals, and status distributions
    /// for a specified date range.
    /// </summary>
    /// <param name="fromDate">Start date for analytics (default: 30 days ago).</param>
    /// <param name="toDate">End date for analytics (default: today).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary containing analytics metrics and KPIs.</returns>
    Task<Dictionary<string, object>> GetAnalyticsSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
}
