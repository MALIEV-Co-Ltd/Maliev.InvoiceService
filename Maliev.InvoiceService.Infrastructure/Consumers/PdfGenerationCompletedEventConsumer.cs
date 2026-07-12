using Maliev.InvoiceService.Application.Services;
using Maliev.MessagingContracts.Contracts.Pdf;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Maliev.InvoiceService.Infrastructure.Consumers;

/// <summary>
/// Consumes PdfGenerationCompletedEvent to update invoice with generated PDF URL.
/// </summary>
public partial class PdfGenerationCompletedEventConsumer : IConsumer<PdfGenerationCompletedEvent>
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<PdfGenerationCompletedEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfGenerationCompletedEventConsumer"/> class.
    /// </summary>
    public PdfGenerationCompletedEventConsumer(IInvoiceService invoiceService, ILogger<PdfGenerationCompletedEventConsumer> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Consumes the PdfGenerationCompletedEvent.
    /// </summary>
    public async Task Consume(ConsumeContext<PdfGenerationCompletedEvent> context)
    {
        var @event = context.Message;
        var payload = @event.Payload;

        Log.ConsumingPdfGenerationCompletedEvent(_logger, payload.RequestId);

        try
        {
            // ReferenceId should be the invoice ID
            // DocumentType helps identify the type (e.g., "Invoice", "Receipt", etc.)
            if (payload.DocumentType != "Invoice")
            {
                Log.SkippingNonInvoiceDocument(_logger, payload.DocumentType, payload.ReferenceId);
                return;
            }

            // Parse ReferenceId (which is the invoice ID as string) to Guid
            if (!Guid.TryParse(payload.ReferenceId, out var invoiceId))
            {
                Log.InvalidInvoiceId(_logger, payload.ReferenceId);
                return;
            }

            // Update invoice with PDF URL
            await _invoiceService.RegisterPdfFileReferenceAsync(
                invoiceId,
                payload.StorageUrl,
                context.CancellationToken);

            Log.PdfUrlRegisteredToInvoice(_logger, payload.ReferenceId);
        }
        catch (Exception ex)
        {
            Log.ErrorRegisteringPdfUrl(_logger, ex, payload.RequestId);
            throw; // Re-throw to trigger MassTransit retry
        }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Consuming PdfGenerationCompletedEvent for RequestId: {RequestId}")]
        public static partial void ConsumingPdfGenerationCompletedEvent(ILogger logger, string requestId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Skipping non-invoice document: DocumentType={DocumentType}, ReferenceId={ReferenceId}")]
        public static partial void SkippingNonInvoiceDocument(ILogger logger, string documentType, string referenceId);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid invoice ID in ReferenceId: {ReferenceId}")]
        public static partial void InvalidInvoiceId(ILogger logger, string referenceId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Registered PDF URL to invoice {InvoiceId}")]
        public static partial void PdfUrlRegisteredToInvoice(ILogger logger, string invoiceId);

        [LoggerMessage(Level = LogLevel.Error, Message = "Error registering PDF URL for RequestId {RequestId}")]
        public static partial void ErrorRegisteringPdfUrl(ILogger logger, Exception ex, string requestId);
    }
}
