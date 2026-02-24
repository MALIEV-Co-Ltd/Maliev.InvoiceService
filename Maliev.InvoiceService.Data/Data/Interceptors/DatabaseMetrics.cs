using System.Diagnostics.Metrics;

namespace Maliev.InvoiceService.Data.Data.Interceptors;

/// <summary>
/// Database specific metrics for the Invoice Service.
/// </summary>
public static class DatabaseMetrics
{
    private static readonly Meter _meter = new("Maliev.InvoiceService.Database", "1.0.0");

    private static readonly Histogram<double> QueryDuration = _meter.CreateHistogram<double>(
        "invoice_db_query_duration_seconds",
        unit: "s",
        description: "Duration of database queries in seconds");

    private static readonly Counter<long> QueryTotal = _meter.CreateCounter<long>(
        "invoice_db_query_total",
        description: "Total number of database queries");

    public static void RecordQuery(string commandType, double durationSeconds, string status)
    {
        QueryDuration.Record(durationSeconds, new KeyValuePair<string, object?>("command_type", commandType));
        QueryTotal.Add(1,
            new KeyValuePair<string, object?>("command_type", commandType),
            new KeyValuePair<string, object?>("status", status));
    }
}
