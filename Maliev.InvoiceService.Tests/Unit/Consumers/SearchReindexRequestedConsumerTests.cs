using Maliev.InvoiceService.Infrastructure.Consumers;
using Maliev.MessagingContracts.Contracts.Search;
using Maliev.MessagingContracts.Contracts.Shared;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.InvoiceService.Tests.Unit.Consumers;

/// <summary>
/// Unit tests for invoice search reindex message handling.
/// </summary>
public sealed class SearchReindexRequestedConsumerTests
{
    /// <summary>
    /// Ensures malformed reindex commands are ignored before database access.
    /// </summary>
    [Fact]
    public async Task Consume_WithoutPayload_IsIgnored()
    {
        var bus = new Mock<IBus>();
        var consumer = new SearchReindexRequestedConsumer(
            null!,
            bus.Object,
            Mock.Of<ILogger<SearchReindexRequestedConsumer>>());

        await consumer.Consume(CreateContext(CreateCommand(null)).Object);

        bus.Verify(
            publisher => publisher.Publish(
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static SearchReindexRequestedCommand CreateCommand(SearchReindexRequestedCommandPayload? payload)
    {
        return new SearchReindexRequestedCommand(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(SearchReindexRequestedCommand),
            MessageType: MessageType.Command,
            MessageVersion: "1.0",
            PublishedBy: "SearchService",
            ConsumedBy: ["InvoiceService"],
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: payload!);
    }

    private static Mock<ConsumeContext<T>> CreateContext<T>(T message)
        where T : class
    {
        var context = new Mock<ConsumeContext<T>>();
        context.Setup(c => c.Message).Returns(message);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return context;
    }
}
