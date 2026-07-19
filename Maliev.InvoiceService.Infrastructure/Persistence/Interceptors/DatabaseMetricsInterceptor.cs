using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Diagnostics;

namespace Maliev.InvoiceService.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core command interceptor that captures database query metrics for Prometheus.
/// Tracks query execution time, command types, and error rates.
/// </summary>
public class DatabaseMetricsInterceptor : DbCommandInterceptor
{
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

        DatabaseMetrics.RecordQuery(commandType, durationSeconds, status);

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
