using Maliev.InvoiceService.Infrastructure.Persistence;
using MassTransit;

namespace Maliev.InvoiceService.Infrastructure.Consumers;

/// <summary>
/// Applies inbox deduplication and a transactional consumer outbox to PDF registration.
/// </summary>
public sealed class PdfGenerationCompletedEventConsumerDefinition : ConsumerDefinition<PdfGenerationCompletedEventConsumer>
{
    /// <inheritdoc />
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<PdfGenerationCompletedEventConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseEntityFrameworkOutbox<InvoiceDbContext>(context);
    }
}
