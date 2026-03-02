using Maliev.InvoiceService.Application.Services;
using Maliev.MessagingContracts.Contracts.Payments;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Maliev.InvoiceService.Infrastructure.Consumers;

/// <summary>
/// MassTransit consumer for PaymentCompletedEvent from Payment Service.
/// Automatically allocates payments to invoices for the order.
/// </summary>
public partial class PaymentCompletedEventConsumer : IConsumer<PaymentCompletedEvent>
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<PaymentCompletedEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentCompletedEventConsumer"/> class.
    /// </summary>
    public PaymentCompletedEventConsumer(IInvoiceService invoiceService, ILogger<PaymentCompletedEventConsumer> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Consumes the PaymentCompletedEvent.
    /// </summary>
    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var @event = context.Message;
        var payload = @event.Payload;

        Log.ConsumingPaymentCompletedEvent(_logger, payload.PaymentId, payload.OrderId);

        try
        {
            // PaymentCompletedEvent received - payment allocation should be done
            // via direct API calls to /allocate endpoint with invoice ID
            // This consumer serves as a notification/audit trail
            Log.PaymentCompletedForOrder(_logger, payload.PaymentId, payload.OrderId, payload.Amount, payload.Currency);
        }
        catch (Exception ex)
        {
            Log.ErrorAllocatingPayment(_logger, ex, payload.PaymentId, payload.OrderId);
            throw; // Re-throw to trigger MassTransit retry
        }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Consuming PaymentCompletedEvent for PaymentId: {PaymentId}, OrderId: {OrderId}")]
        public static partial void ConsumingPaymentCompletedEvent(ILogger logger, Guid paymentId, Guid orderId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Payment completed for Order - PaymentId: {PaymentId}, OrderId: {OrderId}, Amount: {Amount} {Currency}")]
        public static partial void PaymentCompletedForOrder(ILogger logger, Guid paymentId, Guid orderId, double amount, string currency);

        [LoggerMessage(Level = LogLevel.Error, Message = "Error processing PaymentCompletedEvent - PaymentId: {PaymentId}, OrderId: {OrderId}")]
        public static partial void ErrorAllocatingPayment(ILogger logger, Exception ex, Guid paymentId, Guid orderId);
    }
}
