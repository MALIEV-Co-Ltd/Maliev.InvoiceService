using FluentAssertions;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Api.Models.Payments;
using Maliev.InvoiceService.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;

namespace Maliev.InvoiceService.Tests.Integration;

[Collection("Database Collection")]
public class PaymentLinkingTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PaymentLinkingTests(TestDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new TestWebApplicationFactory(_dbFixture);
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _dbFixture.ClearDatabaseAsync();
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task LinkPayment_ToFinalizedInvoice_UpdatesStatusToPartiallyPaid()
    {
        // Arrange - Create and finalize invoice
        var invoiceId = await CreateAndFinalizeInvoiceAsync(grandTotal: 1070m);

        // Create payment
        var paymentRequest = new CreatePaymentRequest
        {
            PaymentAmount = 500m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Bank Transfer",
            RecordedBy = "cashier"
        };
        var paymentResponse = await _client.PostAsJsonAsync("/invoices/v1/payments", paymentRequest);
        var payment = await paymentResponse.Content.ReadFromJsonAsync<PaymentResponse>();

        // Act - Link partial payment to invoice
        var linkRequest = new LinkPaymentRequest
        {
            PaymentId = payment!.Id,
            AllocatedAmount = 500m
        };
        var linkResponse = await _client.PostAsJsonAsync($"/invoices/v1/payments/invoices/{invoiceId}/link", linkRequest);

        // Assert
        if (!linkResponse.IsSuccessStatusCode)
        {
            var errorContent = await linkResponse.Content.ReadAsStringAsync();
            throw new Exception($"Request failed with status {linkResponse.StatusCode}: {errorContent}");
        }
        linkResponse.EnsureSuccessStatusCode();
        var updatedInvoice = await linkResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        updatedInvoice.Should().NotBeNull();
        updatedInvoice!.Status.Should().Be("PartiallyPaid");
    }

    [Fact]
    public async Task LinkPayment_FullAmount_UpdatesStatusToPaid()
    {
        // Arrange - Create and finalize invoice
        var invoiceId = await CreateAndFinalizeInvoiceAsync(grandTotal: 1070m);

        // Create payment
        var paymentRequest = new CreatePaymentRequest
        {
            PaymentAmount = 1070m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Cash",
            RecordedBy = "cashier"
        };
        var paymentResponse = await _client.PostAsJsonAsync("/invoices/v1/payments", paymentRequest);
        var payment = await paymentResponse.Content.ReadFromJsonAsync<PaymentResponse>();

        // Act - Link full payment
        var linkRequest = new LinkPaymentRequest
        {
            PaymentId = payment!.Id,
            AllocatedAmount = 1070m
        };
        var linkResponse = await _client.PostAsJsonAsync($"/invoices/v1/payments/invoices/{invoiceId}/link", linkRequest);

        // Assert
        linkResponse.EnsureSuccessStatusCode();
        var updatedInvoice = await linkResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        updatedInvoice.Should().NotBeNull();
        updatedInvoice!.Status.Should().Be("FullyPaid");
    }

    [Fact]
    public async Task LinkMultiplePayments_TotalingFullAmount_UpdatesStatusToPaid()
    {
        // Arrange - Create and finalize invoice
        var invoiceId = await CreateAndFinalizeInvoiceAsync(grandTotal: 1000m);

        // Create first payment
        var payment1Response = await _client.PostAsJsonAsync("/invoices/v1/payments", new CreatePaymentRequest
        {
            PaymentAmount = 600m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Bank Transfer",
            RecordedBy = "cashier"
        });
        var payment1 = await payment1Response.Content.ReadFromJsonAsync<PaymentResponse>();

        // Link first payment
        await _client.PostAsJsonAsync($"/invoices/v1/payments/invoices/{invoiceId}/link", new LinkPaymentRequest
        {
            PaymentId = payment1!.Id,
            AllocatedAmount = 600m
        });

        // Create second payment
        var payment2Response = await _client.PostAsJsonAsync("/invoices/v1/payments", new CreatePaymentRequest
        {
            PaymentAmount = 400m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Cash",
            RecordedBy = "cashier"
        });
        var payment2 = await payment2Response.Content.ReadFromJsonAsync<PaymentResponse>();

        // Act - Link second payment
        var linkResponse = await _client.PostAsJsonAsync($"/invoices/v1/payments/invoices/{invoiceId}/link", new LinkPaymentRequest
        {
            PaymentId = payment2!.Id,
            AllocatedAmount = 400m
        });

        // Assert
        linkResponse.EnsureSuccessStatusCode();
        var updatedInvoice = await linkResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        updatedInvoice.Should().NotBeNull();
        updatedInvoice!.Status.Should().Be("FullyPaid");
    }

    private async Task<Guid> CreateAndFinalizeInvoiceAsync(decimal grandTotal)
    {
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new()
                {
                    LineNumber = 1,
                    Description = "Product",
                    Quantity = 1,
                    UnitPrice = grandTotal / 1.07m, // Calculate to get desired grand total
                    TaxRate = 7
                }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/invoices/v1/invoices", createRequest);
        if (!createResponse.IsSuccessStatusCode)
        {
            var error = await createResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create invoice: {createResponse.StatusCode} - {error}");
        }
        var draft = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        var finalizeResponse = await _client.PostAsJsonAsync($"/invoices/v1/invoices/{draft!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });
        if (!finalizeResponse.IsSuccessStatusCode)
        {
            var error = await finalizeResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to finalize invoice: {finalizeResponse.StatusCode} - {error}");
        }
        var finalized = await finalizeResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        if (finalized == null || finalized.Id == Guid.Empty)
        {
            throw new Exception("Finalized invoice has null or empty ID");
        }

        return finalized.Id;
    }
}
