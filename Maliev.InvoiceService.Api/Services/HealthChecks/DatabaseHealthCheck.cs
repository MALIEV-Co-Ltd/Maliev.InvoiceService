using Maliev.InvoiceService.Data.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Maliev.InvoiceService.Api.Services.HealthChecks;

/// <summary>
/// Health check implementation for PostgreSQL database connectivity and basic query functionality.
/// Used by Kubernetes readiness probes to determine if the service can handle requests.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for creating scoped DbContext instances.</param>
    /// <param name="logger">Logger for health check diagnostics.</param>
    public DatabaseHealthCheck(IServiceProvider serviceProvider, ILogger<DatabaseHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Checks the health of the PostgreSQL database by verifying connectivity and executing a simple query.
    /// </summary>
    /// <param name="context">Health check context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Healthy if database is accessible and responsive, Unhealthy otherwise.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();

            await dbContext.Database.CanConnectAsync(cancellationToken);

            var invoiceCount = await dbContext.Invoices.CountAsync(cancellationToken);

            return HealthCheckResult.Healthy($"Database is healthy. Invoice count: {invoiceCount}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database is unhealthy", ex);
        }
    }
}
