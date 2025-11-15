using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Diagnostics;

namespace Maliev.InvoiceService.Data.Data.Interceptors;

/// <summary>
/// EF Core command interceptor that captures database query metrics for Prometheus.
/// Tracks query execution time, command types, and error rates.
/// </summary>
public class DatabaseMetricsInterceptor : DbCommandInterceptor
{
    // TODO: Initialize Prometheus metrics in Program.cs
    // private static readonly Histogram QueryDuration = Metrics.CreateHistogram(
    //     "invoice_db_query_duration_seconds",
    //     "Duration of database queries in seconds",
    //     new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(0.001, 2, 10) }
    // );
    //
    // private static readonly Counter QueryCount = Metrics.CreateCounter(
    //     "invoice_db_query_total",
    //     "Total number of database queries",
    //     new CounterConfiguration { LabelNames = new[] { "command_type", "status" } }
    // );

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        RecordMetrics(command, eventData.Duration.TotalSeconds, "success");
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        RecordMetrics(command, eventData.Duration.TotalSeconds, "success");
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override void CommandFailed(
        DbCommand command,
        CommandErrorEventData eventData)
    {
        RecordMetrics(command, eventData.Duration.TotalSeconds, "error");
        base.CommandFailed(command, eventData);
    }

    public override Task CommandFailedAsync(
        DbCommand command,
        CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        RecordMetrics(command, eventData.Duration.TotalSeconds, "error");
        return base.CommandFailedAsync(command, eventData, cancellationToken);
    }

    private void RecordMetrics(DbCommand command, double durationSeconds, string status)
    {
        var commandType = GetCommandType(command.CommandText);

        // TODO: Uncomment when metrics are initialized
        // QueryDuration.Observe(durationSeconds);
        // QueryCount.WithLabels(commandType, status).Inc();

        // For now, just log to debug
        Debug.WriteLine($"DB Query: {commandType} - {durationSeconds:F3}s - {status}");
    }

    private string GetCommandType(string commandText)
    {
        if (string.IsNullOrWhiteSpace(commandText))
            return "unknown";

        var normalized = commandText.TrimStart().ToUpperInvariant();

        if (normalized.StartsWith("SELECT")) return "SELECT";
        if (normalized.StartsWith("INSERT")) return "INSERT";
        if (normalized.StartsWith("UPDATE")) return "UPDATE";
        if (normalized.StartsWith("DELETE")) return "DELETE";

        return "other";
    }
}
