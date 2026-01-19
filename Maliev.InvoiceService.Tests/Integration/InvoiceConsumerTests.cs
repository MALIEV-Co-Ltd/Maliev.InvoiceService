using Moq;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Maliev.MessagingContracts.Generated;
using Maliev.InvoiceService.Api.Services.Consumers;
using Maliev.InvoiceService.Data.Data;
using Maliev.InvoiceService.Data.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Maliev.InvoiceService.Tests.Testing;
using Microsoft.Extensions.Logging;

namespace Maliev.InvoiceService.Api.Tests.Integration;

public class InvoiceConsumerTests : IClassFixture<BaseIntegrationTestFactory<Program, InvoiceDbContext>>
{
    private readonly BaseIntegrationTestFactory<Program, InvoiceDbContext> _factory;

    public InvoiceConsumerTests(BaseIntegrationTestFactory<Program, InvoiceDbContext> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Consume_FileDeletedEvent_ShouldRemoveFileReference()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<FileDeletedEventConsumer>>();

        var fileId = Guid.NewGuid();
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-DEL-1",
            CustomerId = Guid.NewGuid(),
            GrandTotal = 100,
            Currency = "USD",
            Status = "Draft",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Invoices.Add(invoice);

        var reference = new FileReference
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            FileUrl = $"http://storage.com/{fileId}",
            FileType = "application/pdf"
        };
        context.FileReferences.Add(reference);
        await context.SaveChangesAsync();

        var consumer = new FileDeletedEventConsumer(context, logger);
        var evt = new FileDeletedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: "FileDeletedEvent",
            MessageType: MessageType.Event,
            MessageVersion: "1.0",
            PublishedBy: "Upload",
            ConsumedBy: new[] { "Invoice" },
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: true,
            Payload: new FileDeletedEventPayload(
                FileId: fileId.ToString(),
                UploadId: "UP-1",
                ServiceId: "invoice-service",
                StoragePath: "invoices/file.pdf",
                DeletedAt: DateTimeOffset.UtcNow,
                DeletedBy: Guid.NewGuid().ToString(),
                Reason: "Cleanup"
            )
        );

        var mockContext = new Mock<ConsumeContext<FileDeletedEvent>>();
        mockContext.Setup(m => m.Message).Returns(evt);

        // Act
        await consumer.Consume(mockContext.Object);

        // Assert
        var remainingRefs = await context.FileReferences.Where(f => f.InvoiceId == invoice.Id).ToListAsync();
        Assert.Empty(remainingRefs);
    }
}
