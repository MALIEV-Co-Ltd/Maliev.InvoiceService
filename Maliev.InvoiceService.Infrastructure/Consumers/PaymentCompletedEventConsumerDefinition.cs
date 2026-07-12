using Maliev.InvoiceService.Infrastructure.Persistence;
using MassTransit;

namespace Maliev.InvoiceService.Infrastructure.Consumers;

/// <summary>
/// Applies inbox deduplication and a transactional consumer outbox to payment completion handling.
/// </summary>
public sealed class PaymentCompletedEventConsumerDefinition : ConsumerDefinition<PaymentCompletedEventConsumer>
{
    /// <inheritdoc />
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<PaymentCompletedEventConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseEntityFrameworkOutbox<InvoiceDbContext>(context);
    }
}
