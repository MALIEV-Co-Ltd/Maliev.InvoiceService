using Maliev.MessagingContracts.Generated;
using Maliev.MessagingContracts.Contracts.Orders;
using MassTransit;

namespace Maliev.InvoiceService.Api.Services.Consumers;

/// <summary>
/// Consumes OrderPaidEvent to automatically create invoice for paid order.
/// </summary>
public partial class OrderPaidEventConsumer : IConsumer<OrderPaidEvent>
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<OrderPaidEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderPaidEventConsumer"/> class.
    /// </summary>
    public OrderPaidEventConsumer(IInvoiceService invoiceService, ILogger<OrderPaidEventConsumer> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Consumes the OrderPaidEvent.
    /// </summary>
    public async Task Consume(ConsumeContext<OrderPaidEvent> context)
    {
        var @event = context.Message;
        var payload = @event.Payload;

        Log.ConsumingOrderPaidEvent(_logger, payload.OrderId, payload.OrderNumber);

        try
        {
            // OrderPaidEvent doesn't contain enough info to auto-create invoice
            // This consumer serves as a placeholder for future enhancement
            // When needed, call OrderService API to get full order details
            Log.OrderPaidEventReceived(_logger, payload.OrderId, payload.OrderNumber, payload.PaidAmount, payload.Currency);
        }
        catch (Exception ex)
        {
            Log.ErrorCreatingInvoice(_logger, ex, payload.OrderId);
            throw; // Re-throw to trigger MassTransit retry
        }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Consuming OrderPaidEvent for OrderId: {OrderId}, OrderNumber: {OrderNumber}")]
        public static partial void ConsumingOrderPaidEvent(ILogger logger, Guid orderId, string orderNumber);

        [LoggerMessage(Level = LogLevel.Information, Message = "OrderPaidEvent received - OrderId: {OrderId}, OrderNumber: {OrderNumber}, Amount: {Amount} {Currency}")]
        public static partial void OrderPaidEventReceived(ILogger logger, Guid orderId, string orderNumber, double amount, string currency);

        [LoggerMessage(Level = LogLevel.Error, Message = "Error processing OrderPaidEvent for OrderId {OrderId}")]
        public static partial void ErrorCreatingInvoice(ILogger logger, Exception ex, Guid orderId);
    }
}
