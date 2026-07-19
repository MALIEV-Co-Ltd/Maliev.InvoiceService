using Maliev.InvoiceService.Infrastructure.Persistence;
using MassTransit;

namespace Maliev.InvoiceService.Infrastructure.Consumers;

/// <summary>
/// Applies inbox deduplication and a transactional consumer outbox to paid-order handling.
/// </summary>
public sealed class OrderPaidEventConsumerDefinition : ConsumerDefinition<OrderPaidEventConsumer>
{
    /// <inheritdoc />
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<OrderPaidEventConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseEntityFrameworkOutbox<InvoiceDbContext>(context);
    }
}
