namespace Maliev.InvoiceService.Api.Models.Invoices;

/// <summary>
/// Request model for searching and filtering invoices with pagination.
/// </summary>
public class InvoiceSearchRequest
{
    /// <summary>
    /// Gets or sets the page number (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; } = 20;

    // Filter properties

    /// <summary>
    /// Gets or sets the customer name filter (partial match).
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Gets or sets the customer ID filter (exact match).
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the invoice status filter (e.g., "Draft", "Finalized", "Cancelled").
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the currency code filter (e.g., "THB", "USD").
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Gets or sets the invoice number filter (partial match).
    /// </summary>
    public string? InvoiceNumber { get; set; }

    // Date range filters

    /// <summary>
    /// Gets or sets the minimum issue date for filtering.
    /// </summary>
    public DateTime? IssueDateFrom { get; set; }

    /// <summary>
    /// Gets or sets the maximum issue date for filtering.
    /// </summary>
    public DateTime? IssueDateTo { get; set; }

    /// <summary>
    /// Gets or sets the minimum due date for filtering.
    /// </summary>
    public DateTime? DueDateFrom { get; set; }

    /// <summary>
    /// Gets or sets the maximum due date for filtering.
    /// </summary>
    public DateTime? DueDateTo { get; set; }

    // Amount range filters

    /// <summary>
    /// Gets or sets the minimum grand total amount for filtering.
    /// </summary>
    public decimal? GrandTotalFrom { get; set; }

    /// <summary>
    /// Gets or sets the maximum grand total amount for filtering.
    /// </summary>
    public decimal? GrandTotalTo { get; set; }

    // Sorting

    /// <summary>
    /// Gets or sets the field to sort by (e.g., "CreatedAt", "DueDate", "GrandTotal").
    /// </summary>
    public string SortBy { get; set; } = "CreatedAt";

    /// <summary>
    /// Gets or sets the sort order ("Asc" or "Desc").
    /// </summary>
    public string SortOrder { get; set; } = "Desc";

    // Special filters

    /// <summary>
    /// Gets or sets a value indicating whether to include cancelled invoices in results.
    /// </summary>
    public bool IncludeCancelled { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to return only overdue invoices.
    /// </summary>
    public bool OnlyOverdue { get; set; } = false;

    // Parent/Child relationship filters

    /// <summary>
    /// Gets or sets the parent invoice ID for filtering child invoices.
    /// </summary>
    public Guid? ParentInvoiceId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to exclude split parent invoices from results.
    /// </summary>
    public bool ExcludeSplitParents { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to return only root invoices (no parent).
    /// </summary>
    public bool OnlyRootInvoices { get; set; } = false;
}
