using System.Net.Http.Json;
using FluentAssertions;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Maliev.InvoiceService.Tests.Integration;

/// <summary>
/// Integration tests for invoice cancellation workflow
/// T142 per tasks.md
/// </summary>
[Collection("Database Collection")]
public class InvoiceCancellationTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly TestWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public InvoiceCancellationTests(TestDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new TestWebApplicationFactory(_dbFixture);
    }

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _dbFixture.ClearDatabaseAsync();
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    #region T142 - Cancellation Workflow

    [Fact]
    public async Task CancelInvoice_FinalizedInvoice_UpdatesStatusAndRecordsDetails()
    {
        // Arrange - Create and finalize invoice
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Cancellation Test",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = 1000, TaxRate = 7 }
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/invoices/v1/invoices", createRequest);
        var draft = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        await _client.PostAsJsonAsync($"/invoices/v1/invoices/{draft!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        // Act - Cancel invoice
        var cancelRequest = new CancelInvoiceRequest
        {
            CancelledBy = "admin-user",
            CancellationReason = "Customer requested cancellation due to duplicate order"
        };
        var cancelResponse = await _client.PostAsJsonAsync($"/invoices/v1/invoices/{draft.Id}/cancel", cancelRequest);

        // Assert
        cancelResponse.EnsureSuccessStatusCode();
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        cancelled.Should().NotBeNull();
        cancelled!.Status.Should().Be("Cancelled");
        cancelled.CancelledAt.Should().NotBeNull();
        cancelled.CancelledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        cancelled.CancelledBy.Should().Be("admin-user");
        cancelled.CancellationReason.Should().Be("Customer requested cancellation due to duplicate order");

        // Invoice number should remain unchanged
        cancelled.InvoiceNumber.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CancelInvoice_DraftInvoice_ThrowsConflict()
    {
        // Arrange - Create draft invoice (not finalized)
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Draft Test",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = 1000, TaxRate = 7 }
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/invoices/v1/invoices", createRequest);
        var draft = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Act - Try to cancel draft invoice
        var cancelRequest = new CancelInvoiceRequest
        {
            CancelledBy = "admin-user",
            CancellationReason = "Test"
        };
        var cancelResponse = await _client.PostAsJsonAsync($"/invoices/v1/invoices/{draft!.Id}/cancel", cancelRequest);

        // Assert - Should fail because only finalized invoices can be cancelled
        cancelResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CancelInvoice_WithPayments_StillAllowsCancellation()
    {
        // Arrange - Create, finalize, and add payment to invoice
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Payment Test",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = 1000, TaxRate = 7 }
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/invoices/v1/invoices", createRequest);
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        await _client.PostAsJsonAsync($"/invoices/v1/invoices/{invoice!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        // Add payment
        var paymentResponse = await _client.PostAsJsonAsync("/invoices/v1/payments", new
        {
            PaymentAmount = 500m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Cash",
            RecordedBy = "cashier"
        });
        var payment = await paymentResponse.Content.ReadFromJsonAsync<dynamic>();

        // Act - Cancel invoice with payment
        var cancelRequest = new CancelInvoiceRequest
        {
            CancelledBy = "manager",
            CancellationReason = "Cancellation despite payment received - will issue refund"
        };
        var cancelResponse = await _client.PostAsJsonAsync($"/invoices/v1/invoices/{invoice.Id}/cancel", cancelRequest);

        // Assert - Should allow cancellation (business rule: cancellation allowed, refund will be processed separately)
        cancelResponse.EnsureSuccessStatusCode();
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        cancelled!.Status.Should().Be("Cancelled");
    }

    #endregion
}
