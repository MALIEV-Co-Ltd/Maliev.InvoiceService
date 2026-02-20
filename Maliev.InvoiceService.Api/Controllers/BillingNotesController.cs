using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.InvoiceService.Api.Authorization;
using Maliev.InvoiceService.Api.Models.BillingNotes;
using Maliev.InvoiceService.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.InvoiceService.Api.Controllers;

/// <summary>
/// Controller for managing billing notes (Thai ใบวางบิล).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("invoice/v{version:apiVersion}/billing-notes")]
public class BillingNotesController : ControllerBase
{
    private readonly IBillingNoteService _billingNoteService;
    private readonly ILogger<BillingNotesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BillingNotesController"/> class.
    /// </summary>
    /// <param name="billingNoteService">Service for billing note operations.</param>
    /// <param name="logger">Logger instance.</param>
    public BillingNotesController(IBillingNoteService billingNoteService, ILogger<BillingNotesController> logger)
    {
        _billingNoteService = billingNoteService;
        _logger = logger;
    }

    private string CorrelationId => HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

    /// <summary>
    /// Creates a new billing note.
    /// </summary>
    /// <param name="request">Creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created billing note.</returns>
    [HttpPost]
    [RequirePermission(InvoicePermissions.BillingNotesCreate)] // Ensure this permission exists or reuse existing
    [ProducesResponseType(typeof(BillingNoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BillingNoteResponse>> CreateBillingNote(
        [FromBody] CreateBillingNoteRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{CorrelationId}] Creating billing note for customer {CustomerId}", CorrelationId, request.CustomerId);

        try
        {
            var billingNote = await _billingNoteService.CreateBillingNoteAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetBillingNote), new { id = billingNote.Id, version = "1" }, billingNote);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a billing note by ID.
    /// </summary>
    /// <param name="id">Billing note ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Billing note details.</returns>
    [HttpGet("{id:guid}")]
    [RequirePermission(InvoicePermissions.BillingNotesRead)]
    [ProducesResponseType(typeof(BillingNoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BillingNoteResponse>> GetBillingNote(Guid id, CancellationToken cancellationToken)
    {
        var billingNote = await _billingNoteService.GetBillingNoteByIdAsync(id, cancellationToken);
        if (billingNote == null)
            return NotFound();

        return Ok(billingNote);
    }

    /// <summary>
    /// Updates a billing note's editable fields.
    /// </summary>
    /// <param name="id">Billing note ID.</param>
    /// <param name="request">Updated fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated billing note.</returns>
    [HttpPut("{id:guid}")]
    [RequirePermission(InvoicePermissions.BillingNotesUpdate)]
    [ProducesResponseType(typeof(BillingNoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BillingNoteResponse>> UpdateBillingNote(
        Guid id,
        [FromBody] UpdateBillingNoteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var billingNote = await _billingNoteService.UpdateBillingNoteAsync(id, request, cancellationToken);
            return Ok(billingNote);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Transitions a billing note to a new status.
    /// </summary>
    /// <param name="id">Billing note ID.</param>
    /// <param name="request">Status transition request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated billing note.</returns>
    [HttpPatch("{id:guid}/status")]
    [RequirePermission(InvoicePermissions.BillingNotesUpdate)]
    [ProducesResponseType(typeof(BillingNoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BillingNoteResponse>> UpdateBillingNoteStatus(
        Guid id,
        [FromBody] UpdateBillingNoteStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var billingNote = await _billingNoteService.UpdateBillingNoteStatusAsync(id, request.Status, cancellationToken);
            return Ok(billingNote);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a billing note.
    /// </summary>
    /// <param name="id">Billing note ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{id:guid}")]
    [RequirePermission(InvoicePermissions.BillingNotesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBillingNote(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _billingNoteService.DeleteBillingNoteAsync(id, cancellationToken);
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
    }
}
