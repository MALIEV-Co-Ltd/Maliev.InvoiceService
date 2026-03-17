using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.InvoiceService.Api.Authorization;
using Maliev.InvoiceService.Application.Models.Invoices;
using Maliev.InvoiceService.Application.Models.Payments;
using Maliev.InvoiceService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.InvoiceService.Api.Controllers;

/// <summary>
/// Controller for managing payment operations including creation, retrieval, and invoice linking.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("invoice/v{version:apiVersion}/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<PaymentsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentsController"/> class.
    /// </summary>
    /// <param name="invoiceService">The invoice service for payment operations.</param>
    /// <param name="logger">The logger instance for this controller.</param>
    public PaymentsController(IInvoiceService invoiceService, ILogger<PaymentsController> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new payment record.
    /// </summary>
    /// <param name="request">Payment creation request containing amount, date, and payment method details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created payment details.</returns>
    /// <response code="201">Payment created successfully.</response>
    [HttpPost]
    [RequirePermission(InvoicePermissions.InvoicesUpdate)]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<PaymentResponse>> CreatePayment([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var payment = await _invoiceService.CreatePaymentAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPayment), new { id = payment.Id, version = "1" }, payment);
    }

    /// <summary>
    /// Retrieves a specific payment by its unique identifier.
    /// </summary>
    /// <param name="id">The payment unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The payment details.</returns>
    /// <response code="200">Payment found and returned.</response>
    /// <response code="404">Payment not found.</response>
    [HttpGet("{id:guid}")]
    [RequirePermission(InvoicePermissions.InvoicesRead)]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResponse>> GetPayment(Guid id, CancellationToken cancellationToken)
    {
        var payment = await _invoiceService.GetPaymentByIdAsync(id, cancellationToken);
        if (payment == null)
            return NotFound();

        return Ok(payment);
    }

    /// <summary>
    /// Links an existing payment to an invoice, allocating a specific amount to the invoice.
    /// </summary>
    /// <param name="invoiceId">The invoice unique identifier to link the payment to.</param>
    /// <param name="request">Link payment request containing payment ID and allocated amount.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated invoice details showing the payment allocation.</returns>
    /// <response code="200">Payment linked to invoice successfully.</response>
    /// <response code="404">Invoice or payment not found.</response>
    [HttpPost("invoices/{invoiceId:guid}/link")]
    [RequirePermission(InvoicePermissions.InvoicesUpdate)]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> LinkPaymentToInvoice(Guid invoiceId, [FromBody] LinkPaymentRequest request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.LinkPaymentAsync(invoiceId, request, cancellationToken);
        return Ok(invoice);
    }
}
