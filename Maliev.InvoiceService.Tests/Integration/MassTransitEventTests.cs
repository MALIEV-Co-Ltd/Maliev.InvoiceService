using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.InvoiceService.Application.Models.Invoices;
using Maliev.InvoiceService.Application.Models.Payments;
using Maliev.InvoiceService.Application.Services;
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
    private static readonly CancellationToken NoWaitSnapshotToken = new(canceled: true);

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

            Assert.True(await WaitForOutboundCountAsync<InvoiceCreatedEvent>(
                    harness,
                    message => message.Payload.InvoiceId == invoice.Id,
                    expectedCount: 1),
                "InvoiceCreatedEvent should be published");

            var @event = Assert.Single(SnapshotOutboundMessages<InvoiceCreatedEvent>(
                harness,
                message => message.Payload.InvoiceId == invoice.Id));
            Assert.Equal("InvoiceCreatedEvent", @event.MessageName);
            Assert.Equal("InvoiceService", @event.PublishedBy);
            Assert.Equal(MessageType.Event, @event.MessageType);
            Assert.NotNull(@event.Payload);
            Assert.Equal(invoice.Id, @event.Payload.InvoiceId);
            Assert.NotNull(@event.Payload.InvoiceNumber);
            Assert.Equal("THB", @event.Payload.Currency);
            Assert.Equal(customerId, @event.Payload.CustomerId);

            Assert.True(await WaitForOutboundCountAsync<SearchDocumentUpsertedEvent>(
                    harness,
                    message => message.Payload.ResourceId == invoice.Id.ToString(),
                    expectedCount: 1),
                "SearchDocumentUpsertedEvent should be published");

            var searchMessage = Assert.Single(SnapshotOutboundMessages<SearchDocumentUpsertedEvent>(
                harness,
                message => message.Payload.ResourceId == invoice.Id.ToString()));
            Assert.Equal("InvoiceService", searchMessage.Payload.SourceService);
            Assert.Equal("invoice", searchMessage.Payload.ResourceType);
            Assert.Equal("invoice.invoices.read", searchMessage.Payload.RequiredPermission);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task SearchReindexRequestedConsumer_ShouldDeliverReplayedDocumentsOutsideEfOutbox()
    {
        await _factory.CleanDatabaseAsync();

        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        var consumerHarness = harness.GetConsumerHarness<SearchReindexRequestedConsumer>();
        await harness.Start();

        try
        {
            using var createResponse = await _client.PostAsJsonAsync("/invoice/v1/invoices", new CreateInvoiceRequest
            {
                CustomerId = Guid.NewGuid(),
                Currency = "THB",
                DueDate = DateTime.UtcNow.AddDays(30),
                Lines =
                [
                    new InvoiceLineItemRequest
                    {
                        LineNumber = 1,
                        Description = "Search reindex regression",
                        Quantity = 1,
                        UnitPrice = 100m,
                        TaxRate = 7m
                    }
                ],
                CustomerName = "Search Reindex Customer",
                CustomerTaxId = "1234567890123",
                BillingAddress = "123 Search Road"
            });
            createResponse.EnsureSuccessStatusCode();
            var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
            Assert.NotNull(invoice);

            Assert.True(await WaitForOutboundCountAsync<SearchDocumentUpsertedEvent>(
                harness,
                message => message.Payload.ResourceId == invoice.Id.ToString(),
                expectedCount: 1));

            var reindexCommand = new SearchReindexRequestedCommand(
                MessageId: Guid.NewGuid(),
                MessageName: nameof(SearchReindexRequestedCommand),
                MessageType: MessageType.Command,
                MessageVersion: "1.0.0",
                PublishedBy: "SearchService",
                ConsumedBy: ["InvoiceService"],
                CorrelationId: Guid.NewGuid(),
                CausationId: null,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                IsPublic: false,
                Payload: new SearchReindexRequestedCommandPayload(
                    SourceService: "InvoiceService",
                    RequestedBy: "integration-test",
                    RequestedAtUtc: DateTimeOffset.UtcNow));

            await harness.Bus.Publish(reindexCommand);

            Assert.True(await WaitForConsumedCountAsync<SearchReindexRequestedCommand>(
                consumerHarness.Consumed,
                message => message.MessageId == reindexCommand.MessageId,
                expectedCount: 1));
            Assert.True(await WaitForOutboundCountAsync<SearchDocumentUpsertedEvent>(
                harness,
                message => message.Payload.ResourceId == invoice.Id.ToString(),
                expectedCount: 2));
            Assert.Empty(harness.Published.Select<Fault<SearchReindexRequestedCommand>>(NoWaitSnapshotToken));
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

            Assert.True(await WaitForOutboundCountAsync<InvoicePaymentReceivedEvent>(
                    harness,
                    message => message.Payload.InvoiceId == createdInvoice.Id,
                    expectedCount: 1),
                "InvoicePaymentReceivedEvent should be published");

            var @event = Assert.Single(SnapshotOutboundMessages<InvoicePaymentReceivedEvent>(
                harness,
                message => message.Payload.InvoiceId == createdInvoice.Id));
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
            Assert.True(await WaitForConsumedCountAsync<PaymentCompletedEvent>(
                consumerHarness.Consumed,
                message => message.Payload.PaymentId == firstPaymentId,
                expectedCount: 1));
            Assert.True(await WaitForConsumedCountAsync<PaymentCompletedEvent>(
                consumerHarness.Consumed,
                message => message.Payload.PaymentId == secondPaymentId,
                expectedCount: 1));

            var allocations = await WaitForInvoiceAllocationsAsync(invoice.Id, expectedCount: 2);

            Assert.Equal(2, allocations.Count);
            Assert.Equal(invoice.GrandTotal, allocations.Sum(allocation => allocation.AllocatedAmount));

            await using var verificationScope = _factory.Services.CreateAsyncScope();
            var db = verificationScope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
            var paidInvoice = await db.Invoices.AsNoTracking().SingleAsync(candidate => candidate.Id == invoice.Id);
            Assert.Equal("FullyPaid", paidInvoice.Status);

            Assert.True(await WaitForOutboundCountAsync<InvoicePaymentReceivedEvent>(
                harness,
                message => message.Payload.InvoiceId == invoice.Id,
                expectedCount: 2));
            Assert.True(await WaitForOutboundCountAsync<InvoiceFullyPaidEvent>(
                harness,
                message => message.Payload.InvoiceId == invoice.Id,
                expectedCount: 1));

            var allocationEffects = SnapshotOutboundMessages<InvoicePaymentReceivedEvent>(
                harness,
                message => message.Payload.InvoiceId == invoice.Id);
            Assert.Equal(2, allocationEffects.Count);
            Assert.Equal(
                invoice.GrandTotal,
                allocationEffects.Sum(effect => (decimal)effect.Payload.AllocatedAmount));
            Assert.Single(allocationEffects, effect => effect.Payload.PaymentId == firstPaymentId);
            Assert.Single(allocationEffects, effect => effect.Payload.PaymentId == secondPaymentId);

            var fullyPaidEffects = SnapshotOutboundMessages<InvoiceFullyPaidEvent>(
                harness,
                message => message.Payload.InvoiceId == invoice.Id);
            Assert.Single(fullyPaidEffects);
            Assert.Empty(harness.Published.Select<Fault<PaymentCompletedEvent>>(NoWaitSnapshotToken));
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task PaymentCompletedEventConsumer_ConcurrentDuplicateDelivery_ShouldApplyPaymentBudgetOnceAcrossInvoices()
    {
        await _factory.CleanDatabaseAsync();
        var orderNumber = "ORD-MULTI-INVOICE-DUPLICATE";
        var customerId = Guid.NewGuid();
        var invoiceIds = new List<Guid>();

        for (var index = 0; index < 4; index++)
        {
            using var createResponse = await _client.PostAsJsonAsync("/invoice/v1/invoices", new CreateInvoiceRequest
            {
                CustomerId = customerId,
                Currency = "THB",
                DueDate = DateTime.UtcNow.AddDays(30),
                PoNumber = orderNumber,
                Lines =
                [
                    new InvoiceLineItemRequest
                    {
                        LineNumber = 1,
                        Description = $"Duplicate delivery invoice {index + 1}",
                        Quantity = 1,
                        UnitPrice = 500.00m,
                        TaxRate = 0
                    }
                ],
                CustomerName = "Multi-invoice Customer",
                CustomerTaxId = "1234567890123",
                BillingAddress = "123 Test St"
            });
            createResponse.EnsureSuccessStatusCode();
            var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
            Assert.NotNull(invoice);
            invoiceIds.Add(invoice.Id);

            using var finalizeResponse = await _client.PostAsJsonAsync(
                $"/invoice/v1/invoices/{invoice.Id}/finalize",
                new FinalizeInvoiceRequest { FinalizedBy = "test-admin" });
            finalizeResponse.EnsureSuccessStatusCode();
        }

        var paymentId = Guid.NewGuid();
        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
            db.Payments.Add(new Payment
            {
                Id = paymentId,
                PaymentAmount = 1000.00m,
                PaymentDate = DateTime.UtcNow.Date,
                PaymentMethod = "omise",
                ReferenceNumber = orderNumber,
                Notes = "Pre-recorded payment for duplicate delivery regression",
                RecordedBy = "PaymentService",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var paymentEvent = new PaymentCompletedEvent(
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
                CustomerId: customerId.ToString(),
                PaymentId: paymentId,
                Amount: 1000.00,
                Currency: "THB")
            {
                ProviderName = "omise"
            });

        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        var consumerHarness = harness.GetConsumerHarness<PaymentCompletedEventConsumer>();
        await harness.Start();

        try
        {
            await Task.WhenAll(
                harness.Bus.Publish(paymentEvent),
                harness.Bus.Publish(paymentEvent));

            Assert.True(await WaitForConsumedCountAsync<PaymentCompletedEvent>(
                consumerHarness.Consumed,
                message => message.Payload.PaymentId == paymentId,
                expectedCount: 2));

            var allocations = await WaitForPaymentAllocationsAsync(paymentId, expectedCount: 2);

            Assert.Equal(2, allocations.Count);
            Assert.Equal(1000.00m, allocations.Sum(allocation => allocation.AllocatedAmount));
            Assert.Equal(2, allocations.Select(allocation => allocation.InvoiceId).Distinct().Count());
            Assert.All(allocations, allocation => Assert.Contains(allocation.InvoiceId, invoiceIds));
            var affectedInvoiceIds = allocations
                .Select(allocation => allocation.InvoiceId)
                .ToHashSet();

            await using var verificationScope = _factory.Services.CreateAsyncScope();
            var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
            var persistedInvoices = await verificationDb.Invoices
                .AsNoTracking()
                .Where(candidate => invoiceIds.Contains(candidate.Id))
                .ToListAsync();
            Assert.Equal(2, persistedInvoices.Count(candidate =>
                affectedInvoiceIds.Contains(candidate.Id) &&
                candidate.Status == "FullyPaid"));
            Assert.Equal(2, persistedInvoices.Count(candidate =>
                !affectedInvoiceIds.Contains(candidate.Id) &&
                candidate.Status == "Finalized"));

            var paymentAudits = await verificationDb.AuditLogs
                .AsNoTracking()
                .Where(audit =>
                    audit.EventType == "PaymentLinked" &&
                    affectedInvoiceIds.Contains(audit.InvoiceId))
                .ToListAsync();
            Assert.Equal(
                2,
                paymentAudits.Count(audit => audit.ChangedFields?.Contains(
                        paymentId.ToString(),
                        StringComparison.OrdinalIgnoreCase)
                    == true));

            var affectedResourceIds = affectedInvoiceIds
                .Select(affectedInvoiceId => affectedInvoiceId.ToString())
                .ToHashSet(StringComparer.Ordinal);
            Assert.True(await WaitForOutboundCountAsync<InvoicePaymentReceivedEvent>(
                harness,
                message => message.Payload.PaymentId == paymentId,
                expectedCount: 2));
            Assert.True(await WaitForOutboundCountAsync<PaymentAllocatedEvent>(
                harness,
                message => message.Payload.PaymentId == paymentId,
                expectedCount: 2));
            Assert.True(await WaitForOutboundCountAsync<InvoiceFullyPaidEvent>(
                harness,
                message => message.Payload.LastPaymentId == paymentId,
                expectedCount: 2));
            Assert.True(await WaitForOutboundCountAsync<SearchDocumentUpsertedEvent>(
                harness,
                message =>
                    affectedResourceIds.Contains(message.Payload.ResourceId) &&
                    message.Payload.Status == "FullyPaid",
                expectedCount: 2));

            var allocationEffects = SnapshotOutboundMessages<InvoicePaymentReceivedEvent>(
                harness,
                message => message.Payload.PaymentId == paymentId);
            Assert.Equal(2, allocationEffects.Count);
            Assert.Equal(
                1000.00m,
                allocationEffects.Sum(effect => (decimal)effect.Payload.AllocatedAmount));
            Assert.Equal(
                2,
                allocationEffects.Select(effect => effect.Payload.InvoiceId).Distinct().Count());
            Assert.Equal(
                2,
                allocationEffects.Select(effect => effect.MessageId).Distinct().Count());

            var paymentAllocatedEffects = SnapshotOutboundMessages<PaymentAllocatedEvent>(
                harness,
                message => message.Payload.PaymentId == paymentId);
            Assert.Equal(2, paymentAllocatedEffects.Count);
            Assert.Equal(
                1000.00m,
                paymentAllocatedEffects.Sum(effect => (decimal)effect.Payload.AllocatedAmount));
            Assert.Equal(
                2,
                paymentAllocatedEffects.Select(effect => effect.Payload.InvoiceId).Distinct().Count());

            var fullyPaidEffects = SnapshotOutboundMessages<InvoiceFullyPaidEvent>(
                harness,
                message => message.Payload.LastPaymentId == paymentId);
            Assert.Equal(2, fullyPaidEffects.Count);
            Assert.Equal(2, fullyPaidEffects.Select(effect => effect.Payload.InvoiceId).Distinct().Count());

            var allocationSearchEffects = SnapshotOutboundMessages<SearchDocumentUpsertedEvent>(
                harness,
                message =>
                    Guid.TryParse(message.Payload.ResourceId, out var resourceId) &&
                    affectedInvoiceIds.Contains(resourceId) &&
                    message.Payload.Status == "FullyPaid");
            Assert.Equal(2, allocationSearchEffects.Count);
            Assert.Equal(
                2,
                allocationSearchEffects.Select(effect => effect.Payload.ResourceId).Distinct().Count());
            Assert.Empty(harness.Published.Select<Fault<PaymentCompletedEvent>>(NoWaitSnapshotToken));
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task AllocatePaymentAsync_ConcurrentInvoices_ShouldNotExceedPersistedPaymentAmount()
    {
        await _factory.CleanDatabaseAsync();
        var customerId = Guid.NewGuid();
        var invoiceIds = new List<Guid>();

        for (var index = 0; index < 2; index++)
        {
            using var createResponse = await _client.PostAsJsonAsync("/invoice/v1/invoices", new CreateInvoiceRequest
            {
                CustomerId = customerId,
                Currency = "THB",
                DueDate = DateTime.UtcNow.AddDays(30),
                Lines =
                [
                    new InvoiceLineItemRequest
                    {
                        LineNumber = 1,
                        Description = $"Direct allocation invoice {index + 1}",
                        Quantity = 1,
                        UnitPrice = 500.00m,
                        TaxRate = 0
                    }
                ],
                CustomerName = "Direct Allocation Customer",
                CustomerTaxId = "1234567890123",
                BillingAddress = "123 Test St"
            });
            createResponse.EnsureSuccessStatusCode();
            var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
            Assert.NotNull(invoice);
            invoiceIds.Add(invoice.Id);

            using var finalizeResponse = await _client.PostAsJsonAsync(
                $"/invoice/v1/invoices/{invoice.Id}/finalize",
                new FinalizeInvoiceRequest { FinalizedBy = "test-admin" });
            finalizeResponse.EnsureSuccessStatusCode();
        }

        var paymentId = Guid.NewGuid();
        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
            db.Payments.Add(new Payment
            {
                Id = paymentId,
                PaymentAmount = 500.00m,
                PaymentDate = DateTime.UtcNow.Date,
                PaymentMethod = "BankTransfer",
                ReferenceNumber = "DIRECT-ALLOCATION",
                RecordedBy = "test-user",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            async Task<Exception?> TryAllocateAsync(Guid invoiceId)
            {
                await using var scope = _factory.Services.CreateAsyncScope();
                var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
                try
                {
                    await invoiceService.AllocatePaymentAsync(
                        invoiceId,
                        paymentId,
                        500.00m,
                        "test-user");
                    return null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }

            var results = await Task.WhenAll(
                    TryAllocateAsync(invoiceIds[0]),
                    TryAllocateAsync(invoiceIds[1]))
                .WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Equal(1, results.Count(result => result is null));
            var rejectedAllocation = Assert.Single(results, result => result is not null);
            var validationError = Assert.IsType<InvalidOperationException>(rejectedAllocation);
            Assert.Contains("remaining payment amount", validationError.Message, StringComparison.OrdinalIgnoreCase);

            await using var verificationScope = _factory.Services.CreateAsyncScope();
            var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
            var allocations = await verificationDb.InvoicePaymentAllocations
                .AsNoTracking()
                .Where(allocation => allocation.PaymentId == paymentId)
                .ToListAsync();
            Assert.Single(allocations);
            Assert.Equal(500.00m, allocations.Sum(allocation => allocation.AllocatedAmount));

            var persistedInvoices = await verificationDb.Invoices
                .AsNoTracking()
                .Where(invoice => invoiceIds.Contains(invoice.Id))
                .ToListAsync();
            Assert.Single(persistedInvoices, invoice => invoice.Status == "FullyPaid");
            Assert.Single(persistedInvoices, invoice => invoice.Status == "Finalized");

            Assert.True(await WaitForOutboundCountAsync<InvoicePaymentReceivedEvent>(
                harness,
                message => message.Payload.PaymentId == paymentId,
                expectedCount: 1));

            var allocationEffects = SnapshotOutboundMessages<InvoicePaymentReceivedEvent>(
                harness,
                message => message.Payload.PaymentId == paymentId);
            Assert.Single(allocationEffects);
            Assert.Equal(500.00, allocationEffects[0].Payload.AllocatedAmount);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task PaymentCompletedEventConsumer_AllocationInsertFault_ShouldRollbackAllocationAuditAndOutbox()
    {
        await _factory.CleanDatabaseAsync();
        var orderNumber = "ORD-ALLOCATION-ROLLBACK";
        var customerId = Guid.NewGuid();
        using var createResponse = await _client.PostAsJsonAsync("/invoice/v1/invoices", new CreateInvoiceRequest
        {
            CustomerId = customerId,
            Currency = "THB",
            DueDate = DateTime.UtcNow.AddDays(30),
            PoNumber = orderNumber,
            Lines =
            [
                new InvoiceLineItemRequest
                {
                    LineNumber = 1,
                    Description = "Allocation rollback invoice",
                    Quantity = 1,
                    UnitPrice = 500.00m,
                    TaxRate = 0
                }
            ],
            CustomerName = "Rollback Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St"
        });
        createResponse.EnsureSuccessStatusCode();
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(invoice);

        using var finalizeResponse = await _client.PostAsJsonAsync(
            $"/invoice/v1/invoices/{invoice.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-admin" });
        finalizeResponse.EnsureSuccessStatusCode();

        var paymentId = Guid.NewGuid();
        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
            db.Payments.Add(new Payment
            {
                Id = paymentId,
                PaymentAmount = 500.00m,
                PaymentDate = DateTime.UtcNow.Date,
                PaymentMethod = "omise",
                ReferenceNumber = orderNumber,
                Notes = "Pre-recorded payment for rollback regression",
                RecordedBy = "PaymentService",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE OR REPLACE FUNCTION test_fail_invoice_allocation_insert()
                RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'forced invoice allocation failure';
                END;
                $$ LANGUAGE plpgsql;

                DROP TRIGGER IF EXISTS test_fail_invoice_allocation_insert ON invoice_payment_allocations;
                CREATE TRIGGER test_fail_invoice_allocation_insert
                BEFORE INSERT ON invoice_payment_allocations
                FOR EACH ROW EXECUTE FUNCTION test_fail_invoice_allocation_insert();
                """);

            var triggerEnabled = await db.Database
                .SqlQueryRaw<string>(
                    """
                    SELECT trigger.tgenabled::text AS "Value"
                    FROM pg_trigger AS trigger
                    WHERE trigger.tgname = 'test_fail_invoice_allocation_insert'
                    """)
                .SingleAsync();
            Assert.Equal("O", triggerEnabled);

            await using var triggerProbeTransaction = await db.Database.BeginTransactionAsync();
            var probeTimestamp = DateTime.UtcNow;
            var triggerProbeException = await Assert.ThrowsAsync<Npgsql.PostgresException>(() =>
                db.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                    INSERT INTO invoice_payment_allocations
                        (invoice_id, payment_id, allocated_amount, allocation_date, allocation_status, allocated_by, created_at)
                    VALUES
                        ({invoice.Id}, {paymentId}, {1m}, {probeTimestamp}, {"Confirmed"}, {"trigger-probe"}, {probeTimestamp})
                    """));
            Assert.Equal(Npgsql.PostgresErrorCodes.RaiseException, triggerProbeException.SqlState);
            await triggerProbeTransaction.RollbackAsync();
        }

        var paymentEvent = new PaymentCompletedEvent(
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
                CustomerId: customerId.ToString(),
                PaymentId: paymentId,
                Amount: 500.00,
                Currency: "THB")
            {
                ProviderName = "omise"
            });

        var harness = _factory.Services.GetRequiredService<ITestHarness>();
        var consumerHarness = harness.GetConsumerHarness<PaymentCompletedEventConsumer>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(paymentEvent);

            Assert.True(await WaitForConsumedCountAsync<PaymentCompletedEvent>(
                consumerHarness.Consumed,
                message => message.Payload.PaymentId == paymentId,
                expectedCount: 1));

            var failedDelivery = Assert.Single(
                consumerHarness.Consumed.Select<PaymentCompletedEvent>(NoWaitSnapshotToken),
                consumed => consumed.Context.Message.Payload.PaymentId == paymentId);
            var postgresFailure = EnumerateExceptionChain(failedDelivery.Exception)
                .OfType<Npgsql.PostgresException>()
                .FirstOrDefault(exception =>
                    exception.SqlState == Npgsql.PostgresErrorCodes.RaiseException &&
                    exception.MessageText.Contains(
                        "forced invoice allocation failure",
                        StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(postgresFailure);

            Assert.True(
                await WaitForPublishedCountAsync<Fault<PaymentCompletedEvent>>(
                    harness,
                    fault => fault.Message.Payload.PaymentId == paymentId,
                    expectedCount: 1),
                "The injected PostgreSQL allocation failure should fault the consumer");

            Assert.Empty(SnapshotOutboundMessages<InvoicePaymentReceivedEvent>(
                harness,
                message => message.Payload.PaymentId == paymentId));
            Assert.Empty(SnapshotOutboundMessages<PaymentAllocatedEvent>(
                harness,
                message => message.Payload.PaymentId == paymentId));
            Assert.Empty(SnapshotOutboundMessages<InvoiceFullyPaidEvent>(
                harness,
                message => message.Payload.LastPaymentId == paymentId));
            Assert.Empty(SnapshotOutboundMessages<SearchDocumentUpsertedEvent>(
                harness,
                message =>
                    message.Payload.ResourceId == invoice.Id.ToString() &&
                    message.Payload.Status == "FullyPaid"));

            await using var verificationScope = _factory.Services.CreateAsyncScope();
            var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InvoiceDbContext>();

            var allocationPersisted = await verificationDb.InvoicePaymentAllocations
                .AsNoTracking()
                .AnyAsync(allocation => allocation.PaymentId == paymentId);
            Assert.False(allocationPersisted, "Allocation row persisted despite enabled trigger");
            Assert.False(await verificationDb.AuditLogs
                .AsNoTracking()
                .AnyAsync(audit =>
                    audit.InvoiceId == invoice.Id &&
                    audit.EventType == "PaymentLinked"));

            var persistedInvoice = await verificationDb.Invoices
                .AsNoTracking()
                .SingleAsync(candidate => candidate.Id == invoice.Id);
            Assert.Equal("Finalized", persistedInvoice.Status);

            var paymentToken = paymentId.ToString();
            Assert.False(await verificationDb
                .Set<MassTransit.EntityFrameworkCoreIntegration.OutboxMessage>()
                .AsNoTracking()
                .AnyAsync(message => message.Body.Contains(paymentToken)));
        }
        finally
        {
            await using var cleanupScope = _factory.Services.CreateAsyncScope();
            var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
            await cleanupDb.Database.ExecuteSqlRawAsync(
                """
                DROP TRIGGER IF EXISTS test_fail_invoice_allocation_insert ON invoice_payment_allocations;
                DROP FUNCTION IF EXISTS test_fail_invoice_allocation_insert();
                """);
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

            Assert.True(await WaitForOutboundCountAsync<InvoicePaymentReceivedEvent>(
                    harness,
                    message => message.Payload.InvoiceId == createdInvoice.Id,
                    expectedCount: 1),
                "InvoicePaymentReceivedEvent should be published");
            Assert.True(await WaitForOutboundCountAsync<InvoiceFullyPaidEvent>(
                    harness,
                    message => message.Payload.InvoiceId == createdInvoice.Id,
                    expectedCount: 1),
                "InvoiceFullyPaidEvent should be published when invoice is fully paid");

            var @event = Assert.Single(SnapshotOutboundMessages<InvoiceFullyPaidEvent>(
                harness,
                message => message.Payload.InvoiceId == createdInvoice.Id));
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

            Assert.True(await WaitForOutboundCountAsync<InvoiceCancelledEvent>(
                    harness,
                    message => message.Payload.InvoiceId == createdInvoice.Id,
                    expectedCount: 1),
                "InvoiceCancelledEvent should be published");

            var @event = Assert.Single(SnapshotOutboundMessages<InvoiceCancelledEvent>(
                harness,
                message => message.Payload.InvoiceId == createdInvoice.Id));
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

            Assert.True(await WaitForOutboundCountAsync<InvoiceGeneratedEvent>(
                    harness,
                    message => message.Payload.InvoiceId == createdInvoice.Id,
                    expectedCount: 1),
                "InvoiceGeneratedEvent should be published");

            var @event = Assert.Single(SnapshotOutboundMessages<InvoiceGeneratedEvent>(
                harness,
                message => message.Payload.InvoiceId == createdInvoice.Id));
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
            Assert.True(await WaitForConsumedCountAsync<PdfGenerationCompletedEvent>(
                    consumerHarness.Consumed,
                    message => message.Payload.ReferenceId == createdInvoice.Id.ToString(),
                    expectedCount: 1),
                "PdfGenerationCompletedEvent should be consumed");

            var updatedInvoice = await WaitForInvoicePdfAsync(createdInvoice.Id);

            // Assert - Verify invoice has PDF URL
            Assert.NotNull(updatedInvoice);
            Assert.NotNull(updatedInvoice.PdfFileReference);
            Assert.Contains(pdfUrl, updatedInvoice.PdfFileReference);
            Assert.Empty(harness.Published.Select<Fault<PdfGenerationCompletedEvent>>(NoWaitSnapshotToken));

            // Verify the outboxed InvoiceGeneratedEvent reached a transport consumer.
            Assert.True(await WaitForOutboundCountAsync<InvoiceGeneratedEvent>(
                    harness,
                    message => message.Payload.InvoiceId == createdInvoice.Id,
                    expectedCount: 1),
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

            Assert.True(await WaitForConsumedCountAsync<PaymentCompletedEvent>(
                consumerHarness.Consumed,
                message => message.Payload.PaymentId == paymentId,
                expectedCount: 2));
            var allocations = await WaitForPaymentAllocationsAsync(paymentId, expectedCount: 1);
            Assert.Empty(harness.Published.Select<Fault<PaymentCompletedEvent>>(NoWaitSnapshotToken));

            var payment = await WaitForPaymentAsync(paymentId);
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

            var allocation = Assert.Single(allocations);
            Assert.Equal(invoice.Id, allocation.InvoiceId);
            Assert.Equal(paidAmount, allocation.AllocatedAmount);

            var paidInvoice = await db.Invoices
                .AsNoTracking()
                .SingleAsync(i => i.Id == invoice.Id);
            Assert.Equal("FullyPaid", paidInvoice.Status);

            var paymentReceivedPublished = await WaitForOutboundCountAsync<InvoicePaymentReceivedEvent>(
                harness,
                message =>
                    message.Payload.InvoiceId == invoice.Id &&
                    message.Payload.PaymentId == paymentId &&
                    message.Payload.AllocatedAmount == (double)paidAmount,
                expectedCount: 1);
            Assert.True(
                paymentReceivedPublished,
                await DescribeOutboxStateAsync(paymentId));
            Assert.True(await WaitForOutboundCountAsync<InvoiceFullyPaidEvent>(
                harness,
                message =>
                    message.Payload.InvoiceId == invoice.Id &&
                    message.Payload.LastPaymentId == paymentId,
                expectedCount: 1));

            var allocationEffects = SnapshotOutboundMessages<InvoicePaymentReceivedEvent>(
                harness,
                message =>
                    message.Payload.InvoiceId == invoice.Id &&
                    message.Payload.PaymentId == paymentId);
            Assert.Single(allocationEffects);

            var fullyPaidEffects = SnapshotOutboundMessages<InvoiceFullyPaidEvent>(
                harness,
                message =>
                    message.Payload.InvoiceId == invoice.Id &&
                    message.Payload.LastPaymentId == paymentId);
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
            Assert.True(await WaitForConsumedCountAsync<PaymentCompletedEvent>(
                consumerHarness.Consumed,
                message => message.Payload.PaymentId == followUpPaymentId,
                expectedCount: 1));
            Assert.Empty(harness.Published.Select<Fault<PaymentCompletedEvent>>(NoWaitSnapshotToken));
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

            Assert.True(await WaitForConsumedCountAsync<OrderPaidEvent>(
                    consumerHarness.Consumed,
                    message => message.Payload.PaymentId == paymentId,
                    expectedCount: 2),
                "OrderPaidEvent should be consumed");

            var payment = await WaitForPaymentAsync(paymentId);
            Assert.Empty(harness.Published.Select<Fault<OrderPaidEvent>>(NoWaitSnapshotToken));

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

    private async Task<List<InvoicePaymentAllocation>> WaitForPaymentAllocationsAsync(
        Guid paymentId,
        int expectedCount)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        List<InvoicePaymentAllocation> allocations;

        do
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
            allocations = await db.InvoicePaymentAllocations
                .AsNoTracking()
                .Where(allocation =>
                    allocation.PaymentId == paymentId &&
                    allocation.AllocationStatus == "Confirmed")
                .ToListAsync();

            if (allocations.Count >= expectedCount)
            {
                return allocations;
            }
        }
        while (DateTime.UtcNow < deadline && await timer.WaitForNextTickAsync());

        return allocations;
    }

    private static async Task<bool> WaitForConsumedCountAsync<T>(
        IReceivedMessageList consumed,
        Func<T, bool> predicate,
        int expectedCount)
        where T : class
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

        while (true)
        {
            var count = consumed
                .Select<T>(NoWaitSnapshotToken)
                .Count(message => predicate(message.Context.Message));
            if (count >= expectedCount)
            {
                return true;
            }

            if (DateTime.UtcNow >= deadline || !await timer.WaitForNextTickAsync())
            {
                return false;
            }
        }
    }

    private static async Task<bool> WaitForPublishedCountAsync<T>(
        ITestHarness harness,
        Func<T, bool> predicate,
        int expectedCount)
        where T : class
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

        while (true)
        {
            var count = harness.Published
                .Select<T>(NoWaitSnapshotToken)
                .Count(message => predicate(message.Context.Message));
            if (count >= expectedCount)
            {
                return true;
            }

            if (DateTime.UtcNow >= deadline || !await timer.WaitForNextTickAsync())
            {
                return false;
            }
        }
    }

    private static async Task<bool> WaitForOutboundCountAsync<T>(
        ITestHarness harness,
        Func<T, bool> predicate,
        int expectedCount)
        where T : class
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

        while (true)
        {
            var consumedCount = harness.Consumed
                .Select<T>(NoWaitSnapshotToken)
                .Count(message => predicate(message.Context.Message));
            if (consumedCount >= expectedCount)
            {
                return true;
            }

            if (DateTime.UtcNow >= deadline || !await timer.WaitForNextTickAsync())
            {
                return false;
            }
        }
    }

    private static IReadOnlyList<T> SnapshotOutboundMessages<T>(
        ITestHarness harness,
        Func<T, bool> predicate)
        where T : class
    {
        return harness.Consumed
            .Select<T>(NoWaitSnapshotToken)
            .Select(message => message.Context.Message)
            .Where(predicate)
            .ToList();
    }

    private async Task<string> DescribeOutboxStateAsync(Guid paymentId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
        var paymentToken = paymentId.ToString();
        var messages = await db.Set<MassTransit.EntityFrameworkCoreIntegration.OutboxMessage>()
            .AsNoTracking()
            .Where(message => message.Body.Contains(paymentToken))
            .Select(message => new
            {
                message.SequenceNumber,
                message.OutboxId,
                message.InboxMessageId,
                message.DestinationAddress
            })
            .ToListAsync();
        var states = await db.Set<MassTransit.EntityFrameworkCoreIntegration.OutboxState>()
            .AsNoTracking()
            .Select(state => new
            {
                state.OutboxId,
                state.Delivered,
                state.LastSequenceNumber
            })
            .ToListAsync();

        return $"Expected payment event was not published. Matching outbox messages: " +
               $"{JsonSerializer.Serialize(messages)}; outbox states: {JsonSerializer.Serialize(states)}";
    }

    private static IEnumerable<Exception> EnumerateExceptionChain(Exception? exception)
    {
        if (exception == null)
        {
            yield break;
        }

        yield return exception;

        if (exception is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                foreach (var nestedException in EnumerateExceptionChain(innerException))
                {
                    yield return nestedException;
                }
            }
        }
        else if (exception.InnerException != null)
        {
            foreach (var nestedException in EnumerateExceptionChain(exception.InnerException))
            {
                yield return nestedException;
            }
        }
    }
}
