using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Maliev.InvoiceService.Api.Services.HealthChecks;

/// <summary>
/// Health check implementation for Redis distributed cache connectivity.
/// Returns Degraded status instead of Unhealthy when Redis is unavailable,
/// as the service falls back to in-memory caching.
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisHealthCheck"/> class.
    /// </summary>
    /// <param name="cache">Distributed cache instance (Redis or in-memory fallback).</param>
    /// <param name="logger">Logger for health check diagnostics.</param>
    public RedisHealthCheck(IDistributedCache cache, ILogger<RedisHealthCheck> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Checks the health of Redis by performing a write-read-delete cycle with a test key.
    /// </summary>
    /// <param name="context">Health check context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Healthy if Redis is accessible and responsive.
    /// Degraded if Redis is unavailable (service falls back to in-memory caching).
    /// </returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var testKey = $"health_check_{Guid.NewGuid()}";
            var testValue = new byte[] { 1, 2, 3 };

            await _cache.SetAsync(testKey, testValue, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
            }, cancellationToken);

            var retrievedValue = await _cache.GetAsync(testKey, cancellationToken);

            if (retrievedValue == null || !retrievedValue.SequenceEqual(testValue))
            {
                return HealthCheckResult.Degraded("Redis cache returned unexpected value");
            }

            await _cache.RemoveAsync(testKey, cancellationToken);

            return HealthCheckResult.Healthy("Redis cache is healthy");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis health check failed - service may be using in-memory fallback");
            return HealthCheckResult.Degraded("Redis cache unavailable (using in-memory fallback)", ex);
        }
    }
}
