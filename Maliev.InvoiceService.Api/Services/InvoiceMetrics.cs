using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Maliev.InvoiceService.Api.Services;

/// <summary>
/// OpenTelemetry metrics for monitoring invoice operations and business analytics.
/// Instruments are created with System.Diagnostics.Metrics (compatible with OpenTelemetry).
/// </summary>
public static class InvoiceMetrics
{
    private static readonly Meter _meter = new("Maliev.InvoiceService.Metrics", "1.0.0");

    private static readonly Counter<long> InvoicesCreatedTotal = _meter.CreateCounter<long>(
        "invoices_created_total",
        description: "Total number of invoices created");

    private static readonly Counter<long> InvoicesFinalizedTotal = _meter.CreateCounter<long>(
        "invoices_finalized_total",
        description: "Total number of invoices finalized");

    private static readonly Counter<long> InvoiceSplitOperationsTotal = _meter.CreateCounter<long>(
        "invoice_split_operations_total",
        description: "Total number of invoice split operations");

    // Store per-status active invoice counts so the observable gauge can report per-status values.
    private static readonly ConcurrentDictionary<string, long> _invoicesActiveCounts = new();

    private static readonly ObservableGauge<long> InvoicesActiveCount = _meter.CreateObservableGauge<long>(
        "invoices_active_count",
        ObserveActiveCounts,
        description: "Current number of active (non-cancelled, non-deleted) invoices");

    private static readonly Histogram<double> InvoiceAmountThb = _meter.CreateHistogram<double>(
        "invoice_amount_thb",
        unit: "THB",
        description: "Distribution of invoice amounts in THB");

    /// <summary>
    /// Records the creation of an invoice with its initial status.
    /// </summary>
    /// <param name="status">Invoice status (e.g., "Draft", "Finalized").</param>
    public static void RecordInvoiceCreated(string status)
    {
        InvoicesCreatedTotal.Add(
            1,
            new[] { new KeyValuePair<string, object?>("status", status) });
    }

    /// <summary>
    /// Records an invoice being finalized (transitioned from Draft to Finalized status).
    /// </summary>
    public static void RecordInvoiceFinalized()
    {
        InvoicesFinalizedTotal.Add(1);
    }

    /// <summary>
    /// Records an invoice split operation and its outcome.
    /// </summary>
    /// <param name="success">True if split was successful, false otherwise.</param>
    public static void RecordInvoiceSplitOperation(bool success)
    {
        InvoiceSplitOperationsTotal.Add(
            1,
            new[] { new KeyValuePair<string, object?>("result", success ? "success" : "failure") });
    }

    /// <summary>
    /// Sets the current count of active invoices for a specific status.
    /// </summary>
    /// <param name="status">Invoice status (e.g., "Draft", "Finalized", "PartiallyPaid", "Paid").</param>
    /// <param name="count">Current count of invoices with this status.</param>
    public static void SetActiveInvoiceCount(string status, int count)
    {
        _invoicesActiveCounts.AddOrUpdate(status, count, (_, __) => count);
    }

    /// <summary>
    /// Records an invoice amount in THB for distribution analysis.
    /// </summary>
    /// <param name="amountThb">Invoice total amount in Thai Baht.</param>
    public static void RecordInvoiceAmount(decimal amountThb)
    {
        InvoiceAmountThb.Record((double)amountThb);
    }

    private static IEnumerable<Measurement<long>> ObserveActiveCounts()
    {
        // Produce one Measurement per status, each tagged with the status label.
        foreach (var kvp in _invoicesActiveCounts.ToArray())
        {
            yield return new Measurement<long>(
                kvp.Value,
                new[] { new KeyValuePair<string, object?>("status", kvp.Key) });
        }
    }
}
