using MassTransit;
using Maliev.InvoiceService.Api.Models.Events;
using Maliev.InvoiceService.Api.Services.External;

namespace Maliev.InvoiceService.Api.Services.Consumers;

/// <summary>
/// MassTransit consumer for PaymentSucceededEvent from Payment Service.
/// Automatically allocates payments to invoices when metadata contains invoice IDs.
///
/// Queue: invoice-service-payment-succeeded
/// Exchange: maliev.payments
/// Routing Key: payment.succeeded
/// </summary>
public class PaymentSucceededConsumer : IConsumer<PaymentSucceededEvent>
{
    private readonly IInvoiceService _invoiceService;
    private readonly IPaymentServiceClient _paymentServiceClient;
    private readonly ILogger<PaymentSucceededConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentSucceededConsumer"/> class.
    /// </summary>
    /// <param name="invoiceService">Invoice service for payment allocation operations.</param>
    /// <param name="paymentServiceClient">Payment service client for validating payments.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="publishEndpoint">MassTransit publish endpoint for publishing events.</param>
    public PaymentSucceededConsumer(
        IInvoiceService invoiceService,
        IPaymentServiceClient paymentServiceClient,
        ILogger<PaymentSucceededConsumer> logger,
        IPublishEndpoint publishEndpoint)
    {
        _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
        _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
    }

    /// <summary>
    /// Consumes a PaymentSucceededEvent and automatically allocates the payment to specified invoices.
    /// Extracts invoice IDs from metadata, validates payment status, and allocates amounts proportionally.
    /// </summary>
    /// <param name="context">MassTransit consume context containing the PaymentSucceededEvent message.</param>
    public async Task Consume(ConsumeContext<PaymentSucceededEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Received PaymentSucceededEvent: PaymentId={PaymentId}, Amount={Amount} {Currency}, CustomerId={CustomerId}",
            message.PaymentId, message.Amount, message.Currency, message.CustomerId);

        try
        {
            // Extract invoice IDs from metadata
            if (message.Metadata == null || !message.Metadata.ContainsKey("invoice_ids"))
            {
                _logger.LogInformation(
                    "No invoice IDs in metadata for auto-allocation. Skipping. PaymentId={PaymentId}",
                    message.PaymentId);
                return;
            }

            var invoiceIdsString = message.Metadata["invoice_ids"];
            var invoiceIds = ParseInvoiceIds(invoiceIdsString);

            if (invoiceIds.Count == 0)
            {
                _logger.LogWarning(
                    "Invalid invoice_ids format in metadata. Expected comma-separated GUIDs. PaymentId={PaymentId}, Value={Value}",
                    message.PaymentId, invoiceIdsString);
                return;
            }

            _logger.LogInformation(
                "Auto-allocating payment to {Count} invoice(s): PaymentId={PaymentId}, InvoiceIds={InvoiceIds}",
                invoiceIds.Count, message.PaymentId, string.Join(", ", invoiceIds));

            // Validate payment exists and has Succeeded status
            var isValid = await _paymentServiceClient.ValidatePaymentAsync(message.PaymentId, context.CancellationToken);
            if (!isValid)
            {
                _logger.LogError(
                    "Payment validation failed during auto-allocation. PaymentId={PaymentId}",
                    message.PaymentId);
                throw new InvalidOperationException($"Payment validation failed for PaymentId={message.PaymentId}");
            }

            // Allocate payment to each invoice
            decimal remainingAmount = message.Amount;
            foreach (var invoiceId in invoiceIds)
            {
                try
                {
                    if (remainingAmount <= 0)
                    {
                        _logger.LogWarning(
                            "No remaining payment amount for allocation. PaymentId={PaymentId}, InvoiceId={InvoiceId}",
                            message.PaymentId, invoiceId);
                        break;
                    }

                    // Get invoice to calculate allocation amount
                    var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId, context.CancellationToken);
                    if (invoice == null)
                    {
                        _logger.LogWarning(
                            "Invoice not found for auto-allocation. PaymentId={PaymentId}, InvoiceId={InvoiceId}",
                            message.PaymentId, invoiceId);
                        continue;
                    }

                    // Calculate outstanding balance
                    var outstandingBalance = await CalculateOutstandingBalanceAsync(invoiceId, context.CancellationToken);
                    var allocationAmount = Math.Min(remainingAmount, outstandingBalance);

                    if (allocationAmount <= 0)
                    {
                        _logger.LogInformation(
                            "Invoice has no outstanding balance. Skipping allocation. PaymentId={PaymentId}, InvoiceId={InvoiceId}",
                            message.PaymentId, invoiceId);
                        continue;
                    }

                    // Allocate payment
                    await AllocatePaymentToInvoiceAsync(
                        invoiceId,
                        message.PaymentId,
                        allocationAmount,
                        "system", // Auto-allocation actor
                        context.CancellationToken);

                    remainingAmount -= allocationAmount;

                    _logger.LogInformation(
                        "Successfully allocated payment to invoice: PaymentId={PaymentId}, InvoiceId={InvoiceId}, Amount={Amount}, Remaining={Remaining}",
                        message.PaymentId, invoiceId, allocationAmount, remainingAmount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error allocating payment to invoice: PaymentId={PaymentId}, InvoiceId={InvoiceId}",
                        message.PaymentId, invoiceId);
                    // Continue with next invoice instead of failing entire batch
                }
            }

            _logger.LogInformation(
                "Completed auto-allocation for PaymentSucceededEvent: PaymentId={PaymentId}, AllocatedAmount={AllocatedAmount}, RemainingAmount={RemainingAmount}",
                message.PaymentId, message.Amount - remainingAmount, remainingAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PaymentSucceededEvent: PaymentId={PaymentId}", message.PaymentId);
            throw; // Rethrow for MassTransit retry/error handling
        }
    }

    private List<Guid> ParseInvoiceIds(string invoiceIdsString)
    {
        var invoiceIds = new List<Guid>();

        if (string.IsNullOrWhiteSpace(invoiceIdsString))
        {
            return invoiceIds;
        }

        var parts = invoiceIdsString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            if (Guid.TryParse(part, out var invoiceId))
            {
                invoiceIds.Add(invoiceId);
            }
            else
            {
                _logger.LogWarning("Invalid GUID format in invoice_ids: {Value}", part);
            }
        }

        return invoiceIds;
    }

    private async Task<decimal> CalculateOutstandingBalanceAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        return await _invoiceService.CalculateOutstandingBalanceAsync(invoiceId, cancellationToken);
    }

    private async Task AllocatePaymentToInvoiceAsync(
        Guid invoiceId,
        Guid paymentId,
        decimal allocatedAmount,
        string allocatedBy,
        CancellationToken cancellationToken)
    {
        await _invoiceService.AllocatePaymentAsync(invoiceId, paymentId, allocatedAmount, allocatedBy, cancellationToken);
    }
}
