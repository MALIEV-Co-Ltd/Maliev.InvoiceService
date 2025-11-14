using Prometheus;

namespace Maliev.InvoiceService.Api.Services;

/// <summary>
/// Prometheus metrics for monitoring invoice operations and business analytics.
/// Metrics are exposed at /invoices/metrics endpoint for scraping by Prometheus.
/// </summary>
public static class InvoiceMetrics
{
    private static readonly Counter InvoicesCreatedTotal = Metrics.CreateCounter(
        "invoices_created_total",
        "Total number of invoices created",
        new CounterConfiguration
        {
            LabelNames = new[] { "status" }
        });

    private static readonly Counter InvoicesFinalizedTotal = Metrics.CreateCounter(
        "invoices_finalized_total",
        "Total number of invoices finalized");

    private static readonly Counter InvoiceSplitOperationsTotal = Metrics.CreateCounter(
        "invoice_split_operations_total",
        "Total number of invoice split operations",
        new CounterConfiguration
        {
            LabelNames = new[] { "result" }
        });

    private static readonly Gauge InvoicesActiveCount = Metrics.CreateGauge(
        "invoices_active_count",
        "Current number of active (non-cancelled, non-deleted) invoices",
        new GaugeConfiguration
        {
            LabelNames = new[] { "status" }
        });

    private static readonly Histogram InvoiceAmountThb = Metrics.CreateHistogram(
        "invoice_amount_thb",
        "Distribution of invoice amounts in THB",
        new HistogramConfiguration
        {
            Buckets = new[] { 1000.0, 5000.0, 10000.0, 25000.0, 50000.0, 100000.0, 250000.0, 500000.0, 1000000.0 }
        });

    /// <summary>
    /// Records the creation of an invoice with its initial status.
    /// </summary>
    /// <param name="status">Invoice status (e.g., "Draft", "Finalized").</param>
    public static void RecordInvoiceCreated(string status)
    {
        InvoicesCreatedTotal.WithLabels(status).Inc();
    }

    /// <summary>
    /// Records an invoice being finalized (transitioned from Draft to Finalized status).
    /// </summary>
    public static void RecordInvoiceFinalized()
    {
        InvoicesFinalizedTotal.Inc();
    }

    /// <summary>
    /// Records an invoice split operation and its outcome.
    /// </summary>
    /// <param name="success">True if split was successful, false otherwise.</param>
    public static void RecordInvoiceSplitOperation(bool success)
    {
        InvoiceSplitOperationsTotal.WithLabels(success ? "success" : "failure").Inc();
    }

    /// <summary>
    /// Sets the current count of active invoices for a specific status.
    /// </summary>
    /// <param name="status">Invoice status (e.g., "Draft", "Finalized", "PartiallyPaid", "Paid").</param>
    /// <param name="count">Current count of invoices with this status.</param>
    public static void SetActiveInvoiceCount(string status, int count)
    {
        InvoicesActiveCount.WithLabels(status).Set(count);
    }

    /// <summary>
    /// Records an invoice amount in THB for distribution analysis.
    /// </summary>
    /// <param name="amountThb">Invoice total amount in Thai Baht.</param>
    public static void RecordInvoiceAmount(decimal amountThb)
    {
        InvoiceAmountThb.Observe((double)amountThb);
    }
}
