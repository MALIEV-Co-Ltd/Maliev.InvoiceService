using System.Net;
using System.Net.Http.Json;
using Maliev.InvoiceService.Application.Models.Invoices;
using Maliev.InvoiceService.Application.Models.Payments;
using Maliev.InvoiceService.Api.Authorization;
using Maliev.InvoiceService.Domain.Entities;
using Maliev.InvoiceService.Infrastructure.Consumers;
using Maliev.InvoiceService.Tests.Fixtures;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Maliev.MessagingContracts;
using Maliev.MessagingContracts.Contracts.Invoices;
using Maliev.MessagingContracts.Contracts.Payments;
using Maliev.MessagingContracts.Contracts.Orders;
using Maliev.MessagingContracts.Contracts.Pdf;
using Maliev.MessagingContracts.Contracts.Search;
using Maliev.InvoiceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maliev.InvoiceService.Tests.Integration;

/// <summary>
/// Integration tests for MassTransit event publishing and consuming in InvoiceService.
/// Uses its own TestWebApplicationFactory instance to avoid test interference.
/// </summary>
public class MassTransitEventTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MassTransitEventTests()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateAuthenticatedClient(
            userId: "test-admin",
            roles: ["Admin"],
            permissions: InvoicePermissions.All);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task CreateInvoice_ShouldPublishInvoiceCreatedEvent()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var customerId = Guid.NewGuid();
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = customerId,
            Currency = "THB",
            DueDate = DateTime.UtcNow.AddDays(30),
            Lines = new List<InvoiceLineItemRequest>
            {
                new InvoiceLineItemRequest
                {
                    LineNumber = 1,
                    Description = "Test Product",
                    Quantity = 2,
                    UnitPrice = 100.00m,
                    TaxRate = 7
                }
            },
            CustomerName = "Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St"
        };

        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            // Act
            HttpResponseMessage response = await _client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
            Assert.NotNull(invoice);

            // Verify InvoiceCreatedEvent was published
            Assert.True(await harness.Published.Any<InvoiceCreatedEvent>(),
                "InvoiceCreatedEvent should be published");

            var publishedMessage = harness.Published.Select<InvoiceCreatedEvent>()
                .FirstOrDefault(x => x.Context.Message.Payload.InvoiceId == invoice.Id);
            Assert.NotNull(publishedMessage);

            var @event = publishedMessage.Context.Message;
            Assert.Equal("InvoiceCreatedEvent", @event.MessageName);
            Assert.Equal("InvoiceService", @event.PublishedBy);
            Assert.Equal(MessageType.Event, @event.MessageType);
            Assert.NotNull(@event.Payload);
            Assert.Equal(invoice.Id, @event.Payload.InvoiceId);
            Assert.NotNull(@event.Payload.InvoiceNumber);
            Assert.Equal("THB", @event.Payload.Currency);
            Assert.Equal(customerId, @event.Payload.CustomerId);

            Assert.True(await harness.Published.Any<SearchDocumentUpsertedEvent>(),
                "SearchDocumentUpsertedEvent should be published");

            var searchMessage = harness.Published.Select<SearchDocumentUpsertedEvent>()
                .FirstOrDefault(x => x.Context.Message.Payload.ResourceId == invoice.Id.ToString());
            Assert.NotNull(searchMessage);
            Assert.Equal("InvoiceService", searchMessage.Context.Message.Payload.SourceService);
            Assert.Equal("invoice", searchMessage.Context.Message.Payload.ResourceType);
            Assert.Equal("invoice.invoices.read", searchMessage.Context.Message.Payload.RequiredPermission);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task AllocatePayment_ShouldPublishInvoicePaymentReceivedEvent()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        // First create an invoice
        var customerId = Guid.NewGuid();
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = customerId,
            Currency = "THB",
            DueDate = DateTime.UtcNow.AddDays(30),
            Lines = new List<InvoiceLineItemRequest>
            {
                new InvoiceLineItemRequest
                {
                    LineNumber = 1,
                    Description = "Test Item",
                    Quantity = 1,
                    UnitPrice = 1000.00m,
                    TaxRate = 7
                }
            },
            CustomerName = "Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St"
        };

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        var createdInvoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(createdInvoice);

        // Finalize the invoice first
        await _client.PostAsJsonAsync($"/invoice/v1/invoices/{createdInvoice.Id}/finalize", new { FinalizedBy = "test-admin" });

        // Create a payment first
        var paymentResponse = await _client.PostAsJsonAsync("/invoice/v1/payments", new CreatePaymentRequest
        {
            PaymentAmount = 500.00m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "BankTransfer",
            RecordedBy = "test-user"
        });
        var createdPayment = await paymentResponse.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(createdPayment);

        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            // Act - Link payment
            var paymentId = createdPayment.Id;
            var linkRequest = new { PaymentId = paymentId, AllocatedAmount = 500.00m };
            HttpResponseMessage allocateResponse = await _client.PostAsJsonAsync(
                $"/invoice/v1/payments/invoices/{createdInvoice.Id}/link",
                linkRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, allocateResponse.StatusCode);

            // Verify InvoicePaymentReceivedEvent was published
            Assert.True(await harness.Published.Any<InvoicePaymentReceivedEvent>(),
                "InvoicePaymentReceivedEvent should be published");

            var publishedMessage = harness.Published.Select<InvoicePaymentReceivedEvent>()
                .FirstOrDefault(x => x.Context.Message.Payload.InvoiceId == createdInvoice.Id);
            Assert.NotNull(publishedMessage);

            var @event = publishedMessage.Context.Message;
            Assert.Equal("InvoicePaymentReceivedEvent", @event.MessageName);
            Assert.NotNull(@event.Payload);
            Assert.Equal(createdInvoice.Id, @event.Payload.InvoiceId);
            Assert.Equal(paymentId, @event.Payload.PaymentId);
            Assert.Equal(500.00, @event.Payload.AllocatedAmount);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task PaymentCompletedEventConsumer_DistinctConcurrentPayments_ShouldNotOverAllocateInvoice()
    {
        await _factory.CleanDatabaseAsync();
        var orderNumber = "ORD-CONCURRENT-ALLOCATIONS";
        var createResponse = await _client.PostAsJsonAsync("/invoice/v1/invoices", new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            Currency = "THB",
            DueDate = DateTime.UtcNow.AddDays(30),
            PoNumber = orderNumber,
            Lines =
            [
                new InvoiceLineItemRequest
                {
                    LineNumber = 1,
                    Description = "Concurrent allocation test item",
                    Quantity = 1,
                    UnitPrice = 1000.00m,
                    TaxRate = 0
                }
            ],
            CustomerName = "Concurrent Allocation Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St"
        });
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(invoice);

        using var finalizeResponse = await _client.PostAsJsonAsync(
            $"/invoice/v1/invoices/{invoice.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-admin" });
        finalizeResponse.EnsureSuccessStatusCode();

        var firstPaymentId = Guid.NewGuid();
        var secondPaymentId = Guid.NewGuid();
        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        var consumerHarness = harness.GetConsumerHarness<PaymentCompletedEventConsumer>();
        await harness.Start();

        try
        {
            PaymentCompletedEvent CreatePaymentEvent(Guid paymentId) => new(
                MessageId: Guid.NewGuid(),
                MessageName: nameof(PaymentCompletedEvent),
                MessageType: MessageType.Event,
                MessageVersion: "1.0.0",
                PublishedBy: "PaymentService",
                ConsumedBy: ["InvoiceService", "OrderService"],
                CorrelationId: Guid.NewGuid(),
                CausationId: null,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                IsPublic: false,
                Payload: new PaymentCompletedEventPayload(
                    OrderId: Guid.NewGuid(),
                    OrderNumber: orderNumber,
                    CustomerId: Guid.NewGuid().ToString(),
                    PaymentId: paymentId,
                    Amount: 700.00,
                    Currency: "THB")
                {
                    ProviderName = "omise"
                });

            await Task.WhenAll(
                harness.Bus.Publish(CreatePaymentEvent(firstPaymentId)),
                harness.Bus.Publish(CreatePaymentEvent(secondPaymentId)));

            Assert.NotNull(await WaitForPaymentAsync(firstPaymentId));
            Assert.NotNull(await WaitForPaymentAsync(secondPaymentId));
            Assert.True(await consumerHarness.Consumed.Any<PaymentCompletedEvent>(consumed =>
                consumed.Context.Message.Payload.PaymentId == firstPaymentId));
            Assert.True(await consumerHarness.Consumed.Any<PaymentCompletedEvent>(consumed =>
                consumed.Context.Message.Payload.PaymentId == secondPaymentId));

            var allocations = await WaitForInvoiceAllocationsAsync(invoice.Id, expectedCount: 2);

            Assert.Equal(2, allocations.Count);
            Assert.Equal(invoice.GrandTotal, allocations.Sum(allocation => allocation.AllocatedAmount));

            await using var verificationScope = _factory.Services.CreateAsyncScope();
            var db = verificationScope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
            var paidInvoice = await db.Invoices.AsNoTracking().SingleAsync(candidate => candidate.Id == invoice.Id);
            Assert.Equal("FullyPaid", paidInvoice.Status);

            var allocationEffects = harness.Published
                .Select<InvoicePaymentReceivedEvent>()
                .Where(published => published.Context.Message.Payload.InvoiceId == invoice.Id)
                .ToList();
            Assert.Equal(2, allocationEffects.Count);
            Assert.Equal(
                invoice.GrandTotal,
                allocationEffects.Sum(effect => (decimal)effect.Context.Message.Payload.AllocatedAmount));
            Assert.Single(allocationEffects, effect => effect.Context.Message.Payload.PaymentId == firstPaymentId);
            Assert.Single(allocationEffects, effect => effect.Context.Message.Payload.PaymentId == secondPaymentId);

            var fullyPaidEffects = harness.Published
                .Select<InvoiceFullyPaidEvent>()
                .Where(published => published.Context.Message.Payload.InvoiceId == invoice.Id)
                .ToList();
            Assert.Single(fullyPaidEffects);
            Assert.Empty(harness.Published.Select<Fault<PaymentCompletedEvent>>());
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task AllocatePaymentToFullyPay_ShouldPublishInvoiceFullyPaidEvent()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var customerId = Guid.NewGuid();
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = customerId,
            Currency = "THB",
            DueDate = DateTime.UtcNow.AddDays(30),
            Lines = new List<InvoiceLineItemRequest>
            {
                new InvoiceLineItemRequest
                {
                    LineNumber = 1,
                    Description = "Small Item",
                    Quantity = 1,
                    UnitPrice = 100.00m,
                    TaxRate = 7
                }
            },
            CustomerName = "Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St"
        };

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        var createdInvoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(createdInvoice);

        // Finalize the invoice
        await _client.PostAsJsonAsync($"/invoice/v1/invoices/{createdInvoice.Id}/finalize", new { FinalizedBy = "test-admin" });

        // Create a payment first
        var paymentResponse = await _client.PostAsJsonAsync("/invoice/v1/payments", new CreatePaymentRequest
        {
            PaymentAmount = 107.00m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "BankTransfer",
            RecordedBy = "test-user"
        });
        var createdPayment = await paymentResponse.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(createdPayment);

        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            // Act - Fully pay the invoice
            var paymentId = createdPayment.Id;
            var linkRequest = new { PaymentId = paymentId, AllocatedAmount = 107.00m };
            HttpResponseMessage allocateResponse = await _client.PostAsJsonAsync(
                $"/invoice/v1/payments/invoices/{createdInvoice.Id}/link",
                linkRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, allocateResponse.StatusCode);

            // Verify both events were published
            Assert.True(await harness.Published.Any<InvoicePaymentReceivedEvent>(),
                "InvoicePaymentReceivedEvent should be published");
            Assert.True(await harness.Published.Any<InvoiceFullyPaidEvent>(),
                "InvoiceFullyPaidEvent should be published when invoice is fully paid");

            var fullyPaidMessage = harness.Published.Select<InvoiceFullyPaidEvent>().FirstOrDefault();
            Assert.NotNull(fullyPaidMessage);

            var @event = fullyPaidMessage.Context.Message;
            Assert.Equal("InvoiceFullyPaidEvent", @event.MessageName);
            Assert.NotNull(@event.Payload);
            Assert.Equal(createdInvoice.Id, @event.Payload.InvoiceId);
            Assert.Equal(customerId, @event.Payload.CustomerId);
            Assert.Equal(paymentId, @event.Payload.LastPaymentId);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task CancelInvoice_ShouldPublishInvoiceCancelledEvent()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var customerId = Guid.NewGuid();
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = customerId,
            Currency = "THB",
            DueDate = DateTime.UtcNow.AddDays(30),
            Lines = new List<InvoiceLineItemRequest>
            {
                new InvoiceLineItemRequest
                {
                    LineNumber = 1,
                    Description = "Test Product",
                    Quantity = 1,
                    UnitPrice = 500.00m,
                    TaxRate = 7
                }
            },
            CustomerName = "Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St"
        };

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        var createdInvoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(createdInvoice);

        // Finalize the invoice
        await _client.PostAsJsonAsync($"/invoice/v1/invoices/{createdInvoice.Id}/finalize", new { FinalizedBy = "test-admin" });

        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            // Act - Cancel invoice
            var cancelRequest = new { CancelledBy = "test-admin", CancellationReason = "Test cancellation" };
            HttpResponseMessage cancelResponse = await _client.PostAsJsonAsync(
                $"/invoice/v1/invoices/{createdInvoice.Id}/cancel",
                cancelRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

            // Verify InvoiceCancelledEvent was published
            Assert.True(await harness.Published.Any<InvoiceCancelledEvent>(),
                "InvoiceCancelledEvent should be published");

            var publishedMessage = harness.Published.Select<InvoiceCancelledEvent>().FirstOrDefault();
            Assert.NotNull(publishedMessage);

            var @event = publishedMessage.Context.Message;
            Assert.Equal("InvoiceCancelledEvent", @event.MessageName);
            Assert.NotNull(@event.Payload);
            Assert.Equal(createdInvoice.Id, @event.Payload.InvoiceId);
            Assert.Equal(customerId, @event.Payload.CustomerId);
            Assert.Equal("Test cancellation", @event.Payload.CancellationReason);
            Assert.False(@event.Payload.RefundRequired); // No payments made
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task RegisterPdfFileReference_ShouldPublishInvoiceGeneratedEvent()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var customerId = Guid.NewGuid();
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = customerId,
            Currency = "THB",
            DueDate = DateTime.UtcNow.AddDays(30),
            Lines = new List<InvoiceLineItemRequest>
            {
                new InvoiceLineItemRequest
                {
                    LineNumber = 1,
                    Description = "Test Product",
                    Quantity = 1,
                    UnitPrice = 250.00m,
                    TaxRate = 7
                }
            },
            CustomerName = "Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St"
        };

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        var createdInvoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(createdInvoice);

        // Finalize the invoice
        await _client.PostAsJsonAsync($"/invoice/v1/invoices/{createdInvoice.Id}/finalize", new { FinalizedBy = "test-admin" });

        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            // Act - Register PDF file reference (simulates PDF generation completion)
            var pdfUrl = $"https://storage.example.com/invoices/{createdInvoice.Id}.pdf";
            var request = new HttpRequestMessage(HttpMethod.Patch, $"/invoice/v1/invoices/{createdInvoice.Id}/pdf-reference");
            request.Content = JsonContent.Create(new { PdfFileReference = pdfUrl });
            HttpResponseMessage pdfResponse = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, pdfResponse.StatusCode);

            // Verify InvoiceGeneratedEvent was published
            Assert.True(await harness.Published.Any<InvoiceGeneratedEvent>(),
                "InvoiceGeneratedEvent should be published");

            var publishedMessage = harness.Published.Select<InvoiceGeneratedEvent>()
                .FirstOrDefault(x => x.Context.Message.Payload.InvoiceId == createdInvoice.Id);
            Assert.NotNull(publishedMessage);

            var @event = publishedMessage.Context.Message;
            Assert.Equal("InvoiceGeneratedEvent", @event.MessageName);
            Assert.NotNull(@event.Payload);
            Assert.Equal(createdInvoice.Id, @event.Payload.InvoiceId);
            Assert.Contains(pdfUrl, @event.Payload.PdfUrl);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task PdfGenerationCompletedEventConsumer_ShouldRegisterPdfUrl()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var customerId = Guid.NewGuid();
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = customerId,
            Currency = "THB",
            DueDate = DateTime.UtcNow.AddDays(30),
            Lines = new List<InvoiceLineItemRequest>
            {
                new InvoiceLineItemRequest
                {
                    LineNumber = 1,
                    Description = "Test Product for PDF",
                    Quantity = 1,
                    UnitPrice = 300.00m,
                    TaxRate = 7
                }
            },
            CustomerName = "Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St"
        };

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdInvoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(createdInvoice);

        // Finalize the invoice
        var finalizeResponse = await _client.PostAsJsonAsync(
            $"/invoice/v1/invoices/{createdInvoice.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-admin" });
        finalizeResponse.EnsureSuccessStatusCode();
        var finalizedInvoice = await finalizeResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(finalizedInvoice);
        Assert.Equal("Finalized", finalizedInvoice.Status);

        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        var consumerHarness = harness.GetConsumerHarness<PdfGenerationCompletedEventConsumer>();
        await harness.Start();

        try
        {
            // Act - Publish PdfGenerationCompletedEvent
            var pdfUrl = $"https://storage.example.com/pdfs/{createdInvoice.Id}.pdf";
            var pdfEvent = new PdfGenerationCompletedEvent(
                MessageId: Guid.NewGuid(),
                MessageName: "PdfGenerationCompletedEvent",
                MessageType: MessageType.Event,
                MessageVersion: "1.0.0",
                PublishedBy: "PdfService",
                ConsumedBy: ["InvoiceService"],
                CorrelationId: Guid.NewGuid(),
                CausationId: null,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                IsPublic: false,
                Payload: new PdfGenerationCompletedEventPayload(
                    RequestId: Guid.NewGuid().ToString(),
                    ReferenceId: createdInvoice.Id.ToString(),
                    DocumentType: "Invoice",
                    StorageUrl: pdfUrl,
                    CompletedAt: DateTimeOffset.UtcNow
                )
            );

            await harness.Bus.Publish(pdfEvent);

            // Wait for this specific consumer delivery before inspecting its durable result.
            Assert.True(await consumerHarness.Consumed.Any<PdfGenerationCompletedEvent>(consumed =>
                    consumed.Context.Message.Payload.ReferenceId == createdInvoice.Id.ToString()),
                "PdfGenerationCompletedEvent should be consumed");

            var updatedInvoice = await WaitForInvoicePdfAsync(createdInvoice.Id);

            // Assert - Verify invoice has PDF URL
            Assert.NotNull(updatedInvoice);
            Assert.NotNull(updatedInvoice.PdfFileReference);
            Assert.Contains(pdfUrl, updatedInvoice.PdfFileReference);
            Assert.Empty(harness.Published.Select<Fault<PdfGenerationCompletedEvent>>());

            // Verify InvoiceGeneratedEvent was published as a result
            Assert.True(await harness.Published.Any<InvoiceGeneratedEvent>(published =>
                    published.Context.Message.Payload.InvoiceId == createdInvoice.Id),
                "InvoiceGeneratedEvent should be published after PDF registration");
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task PaymentCompletedEventConsumer_ShouldRecordPaymentIdempotently()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var orderNumber = "ORD-12345";
        var paidAmount = 1500.00m;
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Paid Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Payment Road",
            Currency = "THB",
            DueDate = DateTime.UtcNow.AddDays(30),
            PoNumber = orderNumber,
            Lines =
            [
                new InvoiceLineItemRequest
                {
                    LineNumber = 1,
                    Description = "Paid order line",
                    Quantity = 1,
                    UnitPrice = 1500.00m,
                    TaxRate = 0
                }
            ]
        };

        var createResponse = await _client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(invoice);

        var finalizeResponse = await _client.PostAsync($"/invoice/v1/invoices/{invoice.Id}/finalize", null);
        finalizeResponse.EnsureSuccessStatusCode();

        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        var consumerHarness = harness.GetConsumerHarness<PaymentCompletedEventConsumer>();
        await harness.Start();

        try
        {
            // Act - Publish PaymentCompletedEvent
            var paymentEvent = new PaymentCompletedEvent(
                MessageId: Guid.NewGuid(),
                MessageName: "PaymentCompletedEvent",
                MessageType: MessageType.Event,
                MessageVersion: "1.0.0",
                PublishedBy: "PaymentService",
                ConsumedBy: ["InvoiceService", "OrderService"],
                CorrelationId: Guid.NewGuid(),
                CausationId: null,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                IsPublic: false,
                Payload: new PaymentCompletedEventPayload(
                    OrderId: orderId,
                    OrderNumber: orderNumber,
                    CustomerId: Guid.NewGuid().ToString(),
                    PaymentId: paymentId,
                    Amount: (double)paidAmount,
                    Currency: "THB"
                )
                {
                    ProviderName = "omise"
                }
            );

            await Task.WhenAll(
                harness.Bus.Publish(paymentEvent),
                harness.Bus.Publish(paymentEvent));

            var payment = await WaitForPaymentAsync(paymentId);

            var deliveryDeadline = DateTime.UtcNow.AddSeconds(5);
            using var deliveryTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
            var consumedDeliveryCount = 0;
            while (consumedDeliveryCount < 2 && DateTime.UtcNow < deliveryDeadline)
            {
                consumedDeliveryCount = consumerHarness.Consumed
                    .Select<PaymentCompletedEvent>()
                    .Count(consumed => consumed.Context.Message.Payload.PaymentId == paymentId);
                if (consumedDeliveryCount < 2)
                {
                    _ = await deliveryTimer.WaitForNextTickAsync();
                }
            }

            Assert.Equal(2, consumedDeliveryCount);
            Assert.Empty(harness.Published.Select<Fault<PaymentCompletedEvent>>());

            Assert.NotNull(payment);
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
            var paymentCount = await db.Payments.AsNoTracking().CountAsync(p => p.Id == paymentId);
            Assert.Equal(1, paymentCount);
            Assert.Equal(paidAmount, payment.PaymentAmount);
            Assert.Equal("omise", payment.PaymentMethod);
            Assert.Equal(orderNumber, payment.ReferenceNumber);
            Assert.Contains(orderId.ToString(), payment.Notes);
            Assert.Equal("PaymentService", payment.RecordedBy);

            var allocation = await db.InvoicePaymentAllocations
                .AsNoTracking()
                .SingleOrDefaultAsync(a => a.InvoiceId == invoice.Id && a.PaymentId == paymentId);
            Assert.NotNull(allocation);
            Assert.Equal(paidAmount, allocation.AllocatedAmount);

            var paidInvoice = await db.Invoices
                .AsNoTracking()
                .SingleAsync(i => i.Id == invoice.Id);
            Assert.Equal("FullyPaid", paidInvoice.Status);

            Assert.True(await harness.Published.Any<InvoicePaymentReceivedEvent>(published =>
                published.Context.Message.Payload.InvoiceId == invoice.Id &&
                published.Context.Message.Payload.PaymentId == paymentId &&
                published.Context.Message.Payload.AllocatedAmount == (double)paidAmount));

            var allocationEffects = harness.Published
                .Select<InvoicePaymentReceivedEvent>()
                .Where(published =>
                    published.Context.Message.Payload.InvoiceId == invoice.Id &&
                    published.Context.Message.Payload.PaymentId == paymentId)
                .ToList();
            Assert.Single(allocationEffects);

            var fullyPaidEffects = harness.Published
                .Select<InvoiceFullyPaidEvent>()
                .Where(published =>
                    published.Context.Message.Payload.InvoiceId == invoice.Id &&
                    published.Context.Message.Payload.LastPaymentId == paymentId)
                .ToList();
            Assert.Single(fullyPaidEffects);

            var followUpPaymentId = Guid.NewGuid();
            var followUpEvent = paymentEvent with
            {
                MessageId = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(),
                Payload = paymentEvent.Payload with
                {
                    OrderId = Guid.NewGuid(),
                    OrderNumber = "ORD-FOLLOW-UP",
                    PaymentId = followUpPaymentId,
                    Amount = 1
                }
            };

            await harness.Bus.Publish(followUpEvent);

            Assert.NotNull(await WaitForPaymentAsync(followUpPaymentId));
            Assert.True(await consumerHarness.Consumed.Any<PaymentCompletedEvent>(consumed =>
                consumed.Context.Message.Payload.PaymentId == followUpPaymentId));
            Assert.Empty(harness.Published.Select<Fault<PaymentCompletedEvent>>());
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task OrderPaidEventConsumer_ShouldRecordOrderPayment()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var paidAmount = 2000.00m;
        var orderNumber = "ORD-67890";

        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        var consumerHarness = harness.GetConsumerHarness<OrderPaidEventConsumer>();
        await harness.Start();

        try
        {
            // Act - Publish OrderPaidEvent
            var orderPaidEvent = new OrderPaidEvent(
                MessageId: Guid.NewGuid(),
                MessageName: "OrderPaidEvent",
                MessageType: MessageType.Event,
                MessageVersion: "1.0.0",
                PublishedBy: "OrderService",
                ConsumedBy: ["InvoiceService", "MaterialService"],
                CorrelationId: Guid.NewGuid(),
                CausationId: null,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                IsPublic: false,
                Payload: new OrderPaidEventPayload(
                    OrderId: orderId,
                    OrderNumber: orderNumber,
                    PaymentId: paymentId,
                    PaidAmount: (double)paidAmount,
                    Currency: "THB",
                    PaidAt: DateTimeOffset.UtcNow
                )
                {
                    ProviderName = "omise"
                }
            );

            await harness.Bus.Publish(orderPaidEvent);
            await harness.Bus.Publish(orderPaidEvent);

            // Wait for consumer to process
            Assert.True(await consumerHarness.Consumed.Any<OrderPaidEvent>(consumed =>
                    consumed.Context.Message.Payload.PaymentId == paymentId),
                "OrderPaidEvent should be consumed");

            var payment = await WaitForPaymentAsync(paymentId);
            Assert.Empty(harness.Published.Select<Fault<OrderPaidEvent>>());

            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
            var paymentCount = await db.Payments.AsNoTracking().CountAsync(p => p.Id == paymentId);

            Assert.NotNull(payment);
            Assert.Equal(1, paymentCount);
            Assert.Equal(paidAmount, payment.PaymentAmount);
            Assert.Equal("omise", payment.PaymentMethod);
            Assert.Equal(orderNumber, payment.ReferenceNumber);
            Assert.Contains(orderId.ToString(), payment.Notes);
            Assert.Equal("OrderService", payment.RecordedBy);
        }
        finally
        {
            await harness.Stop();
        }
    }

    private async Task<Payment?> WaitForPaymentAsync(Guid paymentId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

        while (true)
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
            var payment = await db.Payments.AsNoTracking().SingleOrDefaultAsync(p => p.Id == paymentId);
            if (payment != null)
            {
                return payment;
            }

            if (DateTime.UtcNow >= deadline || !await timer.WaitForNextTickAsync())
            {
                return null;
            }
        }
    }

    private async Task<InvoiceResponse?> WaitForInvoicePdfAsync(Guid invoiceId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

        while (true)
        {
            using var getResponse = await _client.GetAsync($"/invoice/v1/invoices/{invoiceId}");
            var invoice = await getResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
            if (!string.IsNullOrWhiteSpace(invoice?.PdfFileReference))
            {
                return invoice;
            }

            if (DateTime.UtcNow >= deadline || !await timer.WaitForNextTickAsync())
            {
                return invoice;
            }
        }
    }

    private async Task<List<InvoicePaymentAllocation>> WaitForInvoiceAllocationsAsync(
        Guid invoiceId,
        int expectedCount)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        List<InvoicePaymentAllocation> allocations;

        do
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
            allocations = await db.InvoicePaymentAllocations
                .AsNoTracking()
                .Where(allocation => allocation.InvoiceId == invoiceId)
                .ToListAsync();

            if (allocations.Count >= expectedCount)
            {
                return allocations;
            }
        }
        while (DateTime.UtcNow < deadline && await timer.WaitForNextTickAsync());

        return allocations;
    }
}
