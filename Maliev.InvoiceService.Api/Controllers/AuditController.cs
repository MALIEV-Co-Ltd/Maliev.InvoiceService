using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.InvoiceService.Api.Authorization;
using Maliev.InvoiceService.Application.Models.Audit;
using Maliev.InvoiceService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.InvoiceService.Api.Controllers;

/// <summary>
/// Controller for retrieving audit trail information for invoices and related entities.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("invoice/v{version:apiVersion}/audit")]
public class AuditController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly InvoiceAccessGuard _accessGuard;
    private readonly ILogger<AuditController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditController"/> class.
    /// </summary>
    /// <param name="invoiceService">The invoice service for retrieving audit data.</param>
    /// <param name="accessGuard">Invoice object-scope access guard.</param>
    /// <param name="logger">The logger instance for this controller.</param>
    public AuditController(
        IInvoiceService invoiceService,
        InvoiceAccessGuard accessGuard,
        ILogger<AuditController> logger)
    {
        _invoiceService = invoiceService;
        _accessGuard = accessGuard;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the complete audit trail for a specific invoice, showing all changes and operations.
    /// </summary>
    /// <param name="id">The invoice unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A chronological list of audit log entries for the invoice.</returns>
    /// <response code="200">Audit trail retrieved successfully.</response>
    /// <response code="404">Invoice not found.</response>
    [HttpGet("invoices/{id:guid}")]
    [RequirePermission(InvoicePermissions.InvoicesRead)]
    [ProducesResponseType(typeof(List<AuditLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<AuditLogResponse>>> GetInvoiceAuditTrail(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Retrieving audit trail for invoice {InvoiceId}", id);

            var decision = await _accessGuard.CheckInvoiceAsync(id, User, cancellationToken);
            if (decision == InvoiceAccessDecision.NotFound) return NotFound();
            if (decision == InvoiceAccessDecision.Forbidden) return Forbid();

            var auditTrail = await _invoiceService.GetAuditTrailAsync(id, cancellationToken);

            return Ok(auditTrail);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
