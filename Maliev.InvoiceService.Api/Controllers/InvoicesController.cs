using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maliev.InvoiceService.Api.Services;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Api.Models.Common;

namespace Maliev.InvoiceService.Api.Controllers;

/// <summary>
/// Controller for managing invoice operations including creation, retrieval, finalization, splitting, and cancellation.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("invoice/v{version:apiVersion}/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<InvoicesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvoicesController"/> class.
    /// </summary>
    /// <param name="invoiceService">The invoice service for business logic operations.</param>
    /// <param name="logger">The logger instance for this controller.</param>
    public InvoicesController(IInvoiceService invoiceService, ILogger<InvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    private string CorrelationId => HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

    /// <summary>
    /// Creates a new draft invoice from a quotation or manually.
    /// </summary>
    /// <param name="request">The invoice creation request containing customer and line item details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created invoice details.</returns>
    /// <response code="201">Invoice created successfully.</response>
    /// <response code="400">Invalid request data.</response>
    [HttpPost]
    [Authorize(Policy = "EmployeeOrHigher")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvoiceResponse>> CreateInvoice([FromBody] CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{CorrelationId}] Creating invoice for customer {CustomerId}", CorrelationId, request.CustomerId);

        var invoice = await _invoiceService.CreateInvoiceAsync(request, cancellationToken);

        _logger.LogInformation("[{CorrelationId}] Created invoice {InvoiceId}", CorrelationId, invoice.Id);

        return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id, version = "1" }, invoice);
    }

    /// <summary>
    /// Retrieves a specific invoice by its unique identifier.
    /// </summary>
    /// <param name="id">The invoice unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The invoice details.</returns>
    /// <response code="200">Invoice found and returned.</response>
    /// <response code="404">Invoice not found.</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceResponse>> GetInvoice(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("[{CorrelationId}] Retrieving invoice {InvoiceId}", CorrelationId, id);

        var invoice = await _invoiceService.GetInvoiceByIdAsync(id, cancellationToken);

        if (invoice == null)
        {
            _logger.LogWarning("[{CorrelationId}] Invoice {InvoiceId} not found", CorrelationId, id);
            return NotFound();
        }

        return Ok(invoice);
    }

    /// <summary>
    /// Searches and retrieves a paginated list of invoices with optional filtering.
    /// </summary>
    /// <param name="request">Search criteria including pagination, status filter, and customer ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of invoices matching the search criteria.</returns>
    /// <response code="200">Invoices retrieved successfully.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<InvoiceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetInvoices([FromQuery] InvoiceSearchRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[{CorrelationId}] Searching invoices with filters", CorrelationId);

        var result = await _invoiceService.SearchInvoicesAsync(request, cancellationToken);

        _logger.LogDebug("[{CorrelationId}] Found {TotalCount} invoices matching search criteria", CorrelationId, result.TotalCount);

        return Ok(result);
    }

    /// <summary>
    /// Exports invoices to CSV or JSON format based on search criteria.
    /// </summary>
    /// <param name="request">Search criteria for filtering invoices to export.</param>
    /// <param name="format">Export format: either 'csv' or 'json' (default: json).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>File download with the exported invoice data.</returns>
    /// <response code="200">Invoices exported successfully.</response>
    /// <response code="400">Invalid format specified.</response>
    [HttpGet("export")]
    [Authorize(Policy = "EmployeeOrHigher")]
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
    /// Finalizes a draft invoice, assigning it a sequential invoice number and changing its status to Finalized.
    /// </summary>
    /// <param name="id">The invoice unique identifier.</param>
    /// <param name="request">Finalization request containing the user who finalized the invoice.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The finalized invoice details with assigned invoice number.</returns>
    /// <response code="200">Invoice finalized successfully.</response>
    /// <response code="404">Invoice not found.</response>
    /// <response code="400">Invoice cannot be finalized (e.g., not in Draft status).</response>
    [HttpPost("{id:guid}/finalize")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvoiceResponse>> FinalizeInvoice(Guid id, [FromBody] FinalizeInvoiceRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{CorrelationId}] Finalizing invoice {InvoiceId} by {User}", CorrelationId, id, request.FinalizedBy);

        var invoice = await _invoiceService.FinalizeInvoiceAsync(id, request.FinalizedBy, cancellationToken);

        _logger.LogInformation("[{CorrelationId}] Finalized invoice {InvoiceId} with number {InvoiceNumber}",
            CorrelationId, invoice.Id, invoice.InvoiceNumber);

        return Ok(invoice);
    }

    /// <summary>
    /// Cancels an invoice by changing its status to Cancelled.
    /// </summary>
    /// <param name="id">The invoice unique identifier.</param>
    /// <param name="request">Cancellation request containing the user who cancelled and the reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cancelled invoice details.</returns>
    /// <response code="200">Invoice cancelled successfully.</response>
    /// <response code="404">Invoice not found.</response>
    /// <response code="409">Invoice cannot be cancelled (e.g., already finalized).</response>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<InvoiceResponse>> CancelInvoice(Guid id, [FromBody] CancelInvoiceRequest request, CancellationToken cancellationToken)
    {
        try
        {
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
    /// Updates a draft invoice with new details. Only draft invoices can be updated.
    /// </summary>
    /// <param name="id">The invoice unique identifier.</param>
    /// <param name="request">Updated invoice details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated invoice details.</returns>
    /// <response code="200">Invoice updated successfully.</response>
    /// <response code="404">Invoice not found.</response>
    /// <response code="400">Invoice cannot be updated (e.g., not in Draft status).</response>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InvoiceResponse>> UpdateInvoice(Guid id, [FromBody] CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        try
        {
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
    /// Deletes a draft invoice. Only draft invoices can be deleted.
    /// </summary>
    /// <param name="id">The invoice unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on successful deletion.</returns>
    /// <response code="204">Invoice deleted successfully.</response>
    /// <response code="404">Invoice not found.</response>
    /// <response code="400">Invoice cannot be deleted (e.g., not in Draft status).</response>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteInvoice(Guid id, CancellationToken cancellationToken)
    {
        try
        {
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
    /// Splits a finalized invoice into multiple child invoices based on percentage rules.
    /// </summary>
    /// <param name="id">The invoice unique identifier to split.</param>
    /// <param name="request">Split request containing percentage-based split rules for each child invoice.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of newly created child invoices.</returns>
    /// <response code="201">Invoice split successfully, child invoices created.</response>
    /// <response code="404">Invoice not found.</response>
    /// <response code="400">Invalid split request (e.g., percentages don't sum to 100%, or invoice cannot be split).</response>
    [HttpPost("{id:guid}/split")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType(typeof(List<InvoiceResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<InvoiceResponse>>> SplitInvoice(Guid id, [FromBody] SplitInvoiceRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{CorrelationId}] Splitting invoice {InvoiceId} into {Count} parts", CorrelationId, id, request.SplitRules.Count);

        var childInvoices = await _invoiceService.SplitInvoiceAsync(id, request, cancellationToken);

        _logger.LogInformation("[{CorrelationId}] Successfully split invoice {InvoiceId}", CorrelationId, id);

        return StatusCode(StatusCodes.Status201Created, childInvoices);
    }

    /// <summary>
    /// Registers a file reference for an invoice. Links an uploaded file to the invoice.
    /// </summary>
    /// <param name="id">The invoice unique identifier.</param>
    /// <param name="request">File reference details including file ID and metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registered file reference details.</returns>
    /// <response code="201">File reference registered successfully.</response>
    /// <response code="404">Invoice not found.</response>
    /// <response code="409">Conflict - file reference already exists or invalid operation.</response>
    [HttpPost("{id:guid}/files")]
    [Authorize(Policy = "EmployeeOrHigher")]
    [ProducesResponseType(typeof(FileReferenceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileReferenceResponse>> RegisterFile(Guid id, [FromBody] RegisterFileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var fileReference = await _invoiceService.RegisterFileAsync(id, request, cancellationToken);
            return CreatedAtAction(nameof(GetFiles), new { id, version = "1" }, fileReference);
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
    /// Retrieves all file references associated with an invoice.
    /// </summary>
    /// <param name="id">The invoice unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of file references for the invoice.</returns>
    /// <response code="200">File references retrieved successfully.</response>
    [HttpGet("{id:guid}/files")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<FileReferenceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FileReferenceResponse>>> GetFiles(Guid id, CancellationToken cancellationToken)
    {
        var files = await _invoiceService.GetFileReferencesAsync(id, cancellationToken);
        return Ok(files);
    }

    /// <summary>
    /// Retrieves currency conversion report for an invoice showing amounts in different currencies.
    /// </summary>
    /// <param name="id">The invoice unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Currency conversion report with amounts in various currencies.</returns>
    /// <response code="200">Currency conversion report retrieved successfully.</response>
    /// <response code="404">Invoice not found.</response>
    [HttpGet("{id:guid}/currency-conversion")]
    [Authorize(Policy = "EmployeeOrHigher")]
    [ProducesResponseType(typeof(Dictionary<string, object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Dictionary<string, object>>> GetCurrencyConversionReport(Guid id, CancellationToken cancellationToken)
    {
        try
        {
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
    /// Retrieves analytics summary for invoices within a specified date range.
    /// </summary>
    /// <param name="fromDate">Start date for the analytics period (default: 12 months ago).</param>
    /// <param name="toDate">End date for the analytics period (default: current date).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analytics summary including revenue, invoice counts, and trends.</returns>
    /// <response code="200">Analytics summary retrieved successfully.</response>
    [HttpGet("~/invoices/v{version:apiVersion}/analytics/summary")]
    [Authorize(Policy = "Manager")]
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
    /// Internal endpoint for Upload Service to register PDF file reference after PDF generation.
    /// No authorization required - this is for service-to-service communication.
    /// </summary>
    [HttpPatch("{id:guid}/pdf-reference")]
    [AllowAnonymous]
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
