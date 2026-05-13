using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.InvoiceService.Api.Authorization;
using Maliev.InvoiceService.Application.Models.Common;
using Maliev.InvoiceService.Application.Models.Invoices;
using Maliev.InvoiceService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.InvoiceService.Api.Controllers;

/// <summary>
/// Controller for managing invoice operations including creation, retrieval, finalization, splitting, and cancellation.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("invoice/v{version:apiVersion}/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly InvoiceAccessGuard _accessGuard;
    private readonly ILogger<InvoicesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvoicesController"/> class.
    /// </summary>
    /// <param name="invoiceService">The invoice service for business logic operations.</param>
    /// <param name="accessGuard">Invoice object-scope access guard.</param>
    /// <param name="logger">The logger instance for this controller.</param>
    public InvoicesController(
        IInvoiceService invoiceService,
        InvoiceAccessGuard accessGuard,
        ILogger<InvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _accessGuard = accessGuard;
        _logger = logger;
    }

    private string CorrelationId => HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

    private async Task<InvoiceAccessDecision> CheckInvoiceAccessAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _accessGuard.CheckInvoiceAsync(id, User, cancellationToken);
    }

    /// <summary>
    /// Creates a new draft invoice.
    /// </summary>
    /// <remarks>
    /// Invoices can be created from a Quotation or manually. Newly created invoices are always in `Draft` status.
    /// </remarks>
    /// <param name="request">The invoice creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created invoice details.</returns>
    /// <response code="201">Invoice created successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="403">If user lacks `invoice.invoices.create` permission.</response>
    [HttpPost]
    [RequirePermission(InvoicePermissions.InvoicesCreate)]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvoiceResponse>> CreateInvoice([FromBody] CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{CorrelationId}] Creating invoice for customer {CustomerId}", CorrelationId, request.CustomerId);

        var invoice = await _invoiceService.CreateInvoiceAsync(request, cancellationToken);
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

        _logger.LogInformation("[{CorrelationId}] Created invoice {InvoiceId}", CorrelationId, invoice.Id);

        return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id, version = apiVersion }, invoice);
    }

    /// <summary>
    /// Retrieves a specific invoice.
    /// </summary>
    /// <remarks>
    /// Fetches full invoice details including line items, tax components, and status.
    /// </remarks>
    /// <param name="id">The invoice unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The invoice details.</returns>
    /// <response code="200">Invoice found and returned.</response>
    /// <response code="403">If user lacks `invoice.invoices.read` permission.</response>
    /// <response code="404">Invoice not found.</response>
    [HttpGet("{id:guid}")]
    [RequirePermission(InvoicePermissions.InvoicesRead)]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceResponse>> GetInvoice(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("[{CorrelationId}] Retrieving invoice {InvoiceId}", CorrelationId, id);

        var accessDecision = await CheckInvoiceAccessAsync(id, cancellationToken);
        if (accessDecision == InvoiceAccessDecision.NotFound) return NotFound();
        if (accessDecision == InvoiceAccessDecision.Forbidden) return Forbid();

        var invoice = await _invoiceService.GetInvoiceByIdAsync(id, cancellationToken);

        if (invoice == null)
        {
            _logger.LogWarning("[{CorrelationId}] Invoice {InvoiceId} not found", CorrelationId, id);
            return NotFound();
        }

        return Ok(invoice);
    }

    /// <summary>
    /// Searches and retrieves invoices.
    /// </summary>
    /// <remarks>
    /// Supports filtering by customer, status, and date range. Results are paginated.
    /// </remarks>
    /// <param name="request">Search criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of invoices.</returns>
    /// <response code="200">Invoices retrieved successfully.</response>
    /// <response code="403">If user lacks `invoice.invoices.read` permission.</response>
    [HttpGet]
    [RequirePermission(InvoicePermissions.InvoicesRead)]
    [ProducesResponseType(typeof(PaginatedResponse<InvoiceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetInvoices([FromQuery] InvoiceSearchRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[{CorrelationId}] Searching invoices with filters", CorrelationId);

        var result = await _invoiceService.SearchInvoicesAsync(request, _accessGuard.GetScope(User), cancellationToken);

        _logger.LogDebug("[{CorrelationId}] Found {TotalCount} invoices matching search criteria", CorrelationId, result.TotalCount);

        return Ok(result);
    }

    /// <summary>
    /// Exports invoices to CSV or JSON.
    /// </summary>
    /// <remarks>
    /// Useful for accounting system imports or detailed reporting.
    /// </remarks>
    /// <param name="request">Search criteria for filtering export data.</param>
    /// <param name="format">Export format: 'csv' or 'json'.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>File download.</returns>
    /// <response code="200">Invoices exported successfully.</response>
    /// <response code="403">If user lacks `invoice.invoices.export` permission.</response>
    [HttpGet("export")]
    [RequirePermission(InvoicePermissions.InvoicesExport)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportInvoices([FromQuery] InvoiceSearchRequest request, [FromQuery] string format = "json", CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[{CorrelationId}] Exporting invoices in {Format} format", CorrelationId, format);

        if (format.ToLower() != "csv" && format.ToLower() != "json")
        {
            return BadRequest(new { message = "Format must be either 'csv' or 'json'" });
        }

        var exportData = await _invoiceService.ExportInvoicesAsync(request, format, cancellationToken);

        var contentType = format.ToLower() == "csv" ? "text/csv" : "application/json";
        var fileName = $"invoices_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{format.ToLower()}";

        _logger.LogInformation("[{CorrelationId}] Successfully exported invoices to {Format}", CorrelationId, format);

        return File(System.Text.Encoding.UTF8.GetBytes(exportData), contentType, fileName);
    }

    /// <summary>
    /// Finalizes a draft invoice.
    /// </summary>
    /// <remarks>
    /// **CRITICAL:** Assigns a permanent, sequential invoice number. The invoice becomes immutable for most fields.
    /// Supports idempotency via the Idempotency-Key header to prevent duplicate finalization on network retries.
    /// </remarks>
    /// <param name="id">The invoice unique identifier.</param>
    /// <param name="request">Finalization details.</param>
    /// <param name="idempotencyKey">Optional idempotency key to prevent duplicate operations (recommended).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The finalized invoice details.</returns>
    /// <response code="200">Invoice finalized successfully.</response>
    /// <response code="403">If user lacks `invoice.invoices.finalize` permission.</response>
    /// <response code="409">If invoice is not in Draft status.</response>
    [HttpPost("{id:guid}/finalize")]
    [RequirePermission(InvoicePermissions.InvoicesFinalize, IsCritical = true)]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvoiceResponse>> FinalizeInvoice(
        Guid id,
        [FromBody] FinalizeInvoiceRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{CorrelationId}] Finalizing invoice {InvoiceId} by {User}", CorrelationId, id, request.FinalizedBy);

        try
        {
            var accessDecision = await CheckInvoiceAccessAsync(id, cancellationToken);
            if (accessDecision == InvoiceAccessDecision.NotFound) return NotFound();
            if (accessDecision == InvoiceAccessDecision.Forbidden) return Forbid();

            var invoice = await _invoiceService.FinalizeInvoiceAsync(id, request.FinalizedBy, idempotencyKey, cancellationToken);

            _logger.LogInformation("[{CorrelationId}] Finalized invoice {InvoiceId} with number {InvoiceNumber}",
                CorrelationId, invoice.Id, invoice.InvoiceNumber);

            return Ok(invoice);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Invoice {id} not found" });
        }
        catch (InvalidOperationException ex)
        {
            // Return 409 Conflict for state-based issues (e.g., already finalized)
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancels an invoice.
    /// </summary>
    /// <remarks>
    /// **CRITICAL:** Voids the invoice. Typically only allowed if no payments have been recorded.
    /// </remarks>
    /// <param name="id">The invoice unique identifier.</param>
    /// <param name="request">Cancellation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cancelled invoice details.</returns>
    /// <response code="200">Invoice cancelled successfully.</response>
    /// <response code="403">If user lacks `invoice.invoices.void` permission.</response>
    /// <response code="409">If invoice cannot be cancelled.</response>
    [HttpPost("{id:guid}/cancel")]
    [RequirePermission(InvoicePermissions.InvoicesVoid, IsCritical = true)]
    public async Task<ActionResult<InvoiceResponse>> CancelInvoice(Guid id, [FromBody] CancelInvoiceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var accessDecision = await CheckInvoiceAccessAsync(id, cancellationToken);
            if (accessDecision == InvoiceAccessDecision.NotFound) return NotFound();
            if (accessDecision == InvoiceAccessDecision.Forbidden) return Forbid();

            var invoice = await _invoiceService.CancelInvoiceAsync(id, request.CancelledBy, request.CancellationReason, cancellationToken);
            return Ok(invoice);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling invoice {InvoiceId}", id);
            return StatusCode(500, new { message = "Error cancelling invoice", details = ex.Message });
        }
    }

    /// <summary>
    /// Updates a draft invoice.
    /// </summary>
    /// <remarks>
    /// Only invoices in `Draft` status can be modified.
    /// </remarks>
    /// <param name="id">The invoice identifier.</param>
    /// <param name="request">Updated details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated invoice details.</returns>
    /// <response code="200">Invoice updated successfully.</response>
    /// <response code="403">If user lacks `invoice.invoices.update` permission.</response>
    [HttpPut("{id:guid}")]
    [RequirePermission(InvoicePermissions.InvoicesUpdate)]
    public async Task<ActionResult<InvoiceResponse>> UpdateInvoice(Guid id, [FromBody] UpdateInvoiceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var accessDecision = await CheckInvoiceAccessAsync(id, cancellationToken);
            if (accessDecision == InvoiceAccessDecision.NotFound) return NotFound();
            if (accessDecision == InvoiceAccessDecision.Forbidden) return Forbid();

            var invoice = await _invoiceService.UpdateInvoiceAsync(id, request, cancellationToken);
            return Ok(invoice);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice {InvoiceId}", id);
            return StatusCode(500, new { message = "Error updating invoice", details = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a draft invoice.
    /// </summary>
    /// <remarks>
    /// Permanently removes a draft. Finalized invoices cannot be deleted (must be voided).
    /// </remarks>
    /// <param name="id">The invoice identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Invoice deleted successfully.</response>
    /// <response code="403">If user lacks `invoice.invoices.delete` permission.</response>
    [HttpDelete("{id:guid}")]
    [RequirePermission(InvoicePermissions.InvoicesDelete)]
    public async Task<ActionResult> DeleteInvoice(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var accessDecision = await CheckInvoiceAccessAsync(id, cancellationToken);
            if (accessDecision == InvoiceAccessDecision.NotFound) return NotFound();
            if (accessDecision == InvoiceAccessDecision.Forbidden) return Forbid();

            await _invoiceService.DeleteInvoiceAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice {InvoiceId}", id);
            return StatusCode(500, new { message = "Error deleting invoice", details = ex.Message });
        }
    }

    /// <summary>
    /// Splits a finalized invoice.
    /// </summary>
    /// <remarks>
    /// Used for partial billing scenarios. The original invoice is marked as split and new child invoices are created.
    /// </remarks>
    /// <param name="id">The invoice to split.</param>
    /// <param name="request">Split rules (percentages).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of new child invoices.</returns>
    /// <response code="201">Invoice split successfully.</response>
    /// <response code="403">If user lacks `invoice.splits.create` permission.</response>
    [HttpPost("{id:guid}/split")]
    [RequirePermission(InvoicePermissions.SplitsCreate)]
    [ProducesResponseType(typeof(List<InvoiceResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<List<InvoiceResponse>>> SplitInvoice(Guid id, [FromBody] SplitInvoiceRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{CorrelationId}] Splitting invoice {InvoiceId} into {Count} parts", CorrelationId, id, request.SplitRules.Count);

        // Extract user ID from claims or fallback to request property if we added it (we didn't add SplitBy to request yet, but we could)
        // For now, let's use the sub claim which is standard.
        var splitBy = User.FindFirst("sub")?.Value ?? User.FindFirst("uid")?.Value ?? "unknown-user";

        try
        {
            var accessDecision = await CheckInvoiceAccessAsync(id, cancellationToken);
            if (accessDecision == InvoiceAccessDecision.NotFound) return NotFound();
            if (accessDecision == InvoiceAccessDecision.Forbidden) return Forbid();

            var childInvoices = await _invoiceService.SplitInvoiceAsync(id, request, splitBy, cancellationToken);

            _logger.LogInformation("[{CorrelationId}] Successfully split invoice {InvoiceId}", CorrelationId, id);

            return StatusCode(StatusCodes.Status201Created, childInvoices);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Invoice {id} not found" });
        }
        catch (InvalidOperationException ex)
        {
            // Return 409 Conflict for state-based issues (e.g., not finalized, already split)
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            // Return 400 BadRequest for validation errors (e.g., percentages don't sum to 100%)
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Registers an attachment for an invoice.
    /// </summary>
    /// <remarks>
    /// Links a file already uploaded to the `Upload Service` to this invoice.
    /// </remarks>
    /// <param name="id">The invoice identifier.</param>
    /// <param name="request">File ID and metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registered file reference.</returns>
    /// <response code="201">File linked successfully.</response>
    /// <response code="403">If user lacks `invoice.files.upload` permission.</response>
    [HttpPost("{id:guid}/files")]
    [RequirePermission(InvoicePermissions.FilesUpload)]
    [ProducesResponseType(typeof(FileReferenceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileReferenceResponse>> RegisterFile(Guid id, [FromBody] RegisterFileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var accessDecision = await CheckInvoiceAccessAsync(id, cancellationToken);
            if (accessDecision == InvoiceAccessDecision.NotFound) return NotFound();
            if (accessDecision == InvoiceAccessDecision.Forbidden) return Forbid();

            var fileReference = await _invoiceService.RegisterFileAsync(id, request, cancellationToken);
            var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
            return CreatedAtAction(nameof(GetFiles), new { id, version = apiVersion }, fileReference);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves all file attachments for an invoice.
    /// </summary>
    /// <remarks>
    /// Returns metadata about all files linked to the specified invoice.
    /// </remarks>
    /// <param name="id">The invoice identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of file references.</returns>
    /// <response code="200">Files retrieved successfully.</response>
    /// <response code="403">If user lacks `invoice.files.read` permission.</response>
    [HttpGet("{id:guid}/files")]
    [RequirePermission(InvoicePermissions.FilesRead)]
    [ProducesResponseType(typeof(List<FileReferenceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FileReferenceResponse>>> GetFiles(Guid id, CancellationToken cancellationToken)
    {
        var accessDecision = await CheckInvoiceAccessAsync(id, cancellationToken);
        if (accessDecision == InvoiceAccessDecision.NotFound) return NotFound();
        if (accessDecision == InvoiceAccessDecision.Forbidden) return Forbid();

        var files = await _invoiceService.GetFileReferencesAsync(id, cancellationToken);
        return Ok(files);
    }

    /// <summary>
    /// Generates a currency conversion report.
    /// </summary>
    /// <remarks>
    /// Shows the invoice totals converted into multiple system-supported currencies using current rates.
    /// </remarks>
    /// <param name="id">The invoice identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Currency conversion data.</returns>
    /// <response code="200">Report generated successfully.</response>
    /// <response code="403">If user lacks `invoice.reports.currency` permission.</response>
    [HttpGet("{id:guid}/currency-conversion")]
    [RequirePermission(InvoicePermissions.ReportsCurrency)]
    [ProducesResponseType(typeof(Dictionary<string, object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Dictionary<string, object>>> GetCurrencyConversionReport(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var accessDecision = await CheckInvoiceAccessAsync(id, cancellationToken);
            if (accessDecision == InvoiceAccessDecision.NotFound) return NotFound();
            if (accessDecision == InvoiceAccessDecision.Forbidden) return Forbid();

            _logger.LogInformation("[{CorrelationId}] Retrieving currency conversion report for invoice {InvoiceId}", CorrelationId, id);

            var report = await _invoiceService.GetCurrencyConversionReportAsync(id, cancellationToken);

            return Ok(report);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Retrieves global invoice analytics.
    /// </summary>
    /// <remarks>
    /// Provides aggregate data on revenue, status distribution, and volume trends.
    /// </remarks>
    /// <param name="fromDate">Optional start date.</param>
    /// <param name="toDate">Optional end date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analytics summary object.</returns>
    /// <response code="200">Analytics retrieved successfully.</response>
    /// <response code="403">If user lacks `invoice.reports.analytics` permission.</response>
    [HttpGet("~/invoice/v{version:apiVersion}/analytics/summary")]
    [RequirePermission(InvoicePermissions.ReportsAnalytics)]
    [ProducesResponseType(typeof(Dictionary<string, object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, object>>> GetAnalyticsSummary(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[{CorrelationId}] Retrieving analytics summary for period {FromDate} to {ToDate}",
            CorrelationId, fromDate ?? DateTime.UtcNow.AddMonths(-12), toDate ?? DateTime.UtcNow);

        var summary = await _invoiceService.GetAnalyticsSummaryAsync(fromDate, toDate, cancellationToken);

        return Ok(summary);
    }

    /// <summary>
    /// Internal: Links a generated PDF to the invoice.
    /// </summary>
    /// <remarks>
    /// **Internal Only:** Used by the system after PDF generation completes.
    /// </remarks>
    [HttpPatch("{id:guid}/pdf-reference")]
    [RequirePermission(InvoicePermissions.FilesRegister)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterPdfFileReference(
        Guid id,
        [FromBody] RegisterPdfFileReferenceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[{CorrelationId}] Registering PDF file reference for invoice {InvoiceId}: {PdfFileReference}",
                CorrelationId, id, request.PdfFileReference);

            await _invoiceService.RegisterPdfFileReferenceAsync(id, request.PdfFileReference, cancellationToken);

            _logger.LogInformation("[{CorrelationId}] Successfully registered PDF file reference for invoice {InvoiceId}",
                CorrelationId, id);

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering PDF file reference for invoice {InvoiceId}", id);
            return StatusCode(500, new { message = "Error registering PDF file reference", details = ex.Message });
        }
    }
}
