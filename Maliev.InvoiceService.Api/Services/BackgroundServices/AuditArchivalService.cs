using Maliev.InvoiceService.Data.Data;
using Microsoft.EntityFrameworkCore;

namespace Maliev.InvoiceService.Api.Services.BackgroundServices;

/// <summary>
/// Background service that marks audit logs older than 1 year as archived.
/// Runs daily to maintain audit trail performance while meeting 7-year retention requirements.
/// </summary>
public class AuditArchivalService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditArchivalService> _logger;
    private readonly TimeSpan _executionInterval = TimeSpan.FromDays(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditArchivalService"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for creating scoped DbContext instances.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public AuditArchivalService(
        IServiceProvider serviceProvider,
        ILogger<AuditArchivalService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Executes the audit archival background task, running daily to mark old audit logs as archived.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token triggered when the application is shutting down.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Audit Archival Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ArchiveOldAuditLogsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while archiving audit logs");
            }

            // Wait for next execution
            await Task.Delay(_executionInterval, stoppingToken);
        }

        _logger.LogInformation("Audit Archival Service stopped");
    }

    private async Task ArchiveOldAuditLogsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();

        var archiveThresholdDate = DateTime.UtcNow.AddYears(-1);

        _logger.LogInformation("Starting audit log archival for logs older than {ThresholdDate}", archiveThresholdDate);

        // Find logs older than 1 year that are not yet archived
        var logsToArchive = await context.AuditLogs
            .Where(a => a.CreatedAt < archiveThresholdDate && !a.IsArchived)
            .ToListAsync(cancellationToken);

        if (logsToArchive.Count == 0)
        {
            _logger.LogInformation("No audit logs require archiving");
            return;
        }

        // Mark logs as archived
        foreach (var log in logsToArchive)
        {
            log.IsArchived = true;
        }

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully archived {Count} audit logs", logsToArchive.Count);
    }
}
