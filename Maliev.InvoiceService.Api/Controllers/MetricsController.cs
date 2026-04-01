using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.InvoiceService.Api.Authorization;
using Maliev.InvoiceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maliev.InvoiceService.Api.Controllers;

/// <summary>
/// Lightweight business metrics for dashboards (overdue-count).
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("invoice/v{version:apiVersion}/metrics")]
public class MetricsController : ControllerBase
{
    private readonly InvoiceDbContext _db;
    private readonly ILogger<MetricsController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="MetricsController"/>.
    /// </summary>
    public MetricsController(InvoiceDbContext db, ILogger<MetricsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Get the count of overdue invoices.
    /// </summary>
    [HttpGet("overdue-count")]
    [RequirePermission(InvoicePermissions.ReportsAnalytics)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverdueCount(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var count = await _db.Invoices
            .Where(i => (i.Status == "Finalized" || i.Status == "PartiallyPaid") && i.DueDate < today)
            .CountAsync(cancellationToken);

        return Ok(new { count });
    }
}
