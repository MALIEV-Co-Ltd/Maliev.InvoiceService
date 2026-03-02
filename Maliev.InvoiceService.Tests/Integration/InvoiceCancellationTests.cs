using System.Net.Http.Json;
using Maliev.InvoiceService.Application.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;

namespace Maliev.InvoiceService.Tests.Integration;

/// <summary>
/// Integration tests for invoice cancellation workflow
/// T142 per tasks.md
/// </summary>
public class InvoiceCancellationTests : BaseIntegrationTest
{
    public InvoiceCancellationTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region T142 - Cancellation Workflow

    [Fact]
    public async Task CancelInvoice_FinalizedInvoice_UpdatesStatusAndRecordsDetails()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

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
        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        var draft = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        await Client.PostAsJsonAsync($"/invoice/v1/invoices/{draft!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        // Act - Cancel invoice
        var cancelRequest = new CancelInvoiceRequest
        {
            CancelledBy = "admin-user",
            CancellationReason = "Customer requested cancellation due to duplicate order"
        };
        var cancelResponse = await Client.PostAsJsonAsync($"/invoice/v1/invoices/{draft.Id}/cancel", cancelRequest);

        // Assert
        cancelResponse.EnsureSuccessStatusCode();
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        Assert.NotNull(cancelled);
        Assert.Equal("Cancelled", cancelled!.Status);
        Assert.NotNull(cancelled.CancelledAt);
        Assert.True(cancelled.CancelledAt!.Value <= DateTime.UtcNow.AddMinutes(1) && cancelled.CancelledAt.Value >= DateTime.UtcNow.AddMinutes(-1));
        Assert.Equal("admin-user", cancelled.CancelledBy);
        Assert.Equal("Customer requested cancellation due to duplicate order", cancelled.CancellationReason);

        // Invoice number should remain unchanged
        Assert.False(string.IsNullOrEmpty(cancelled.InvoiceNumber));
    }

    [Fact]
    public async Task CancelInvoice_DraftInvoice_ThrowsConflict()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

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
        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        var draft = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Act - Try to cancel draft invoice
        var cancelRequest = new CancelInvoiceRequest
        {
            CancelledBy = "admin-user",
            CancellationReason = "Test"
        };
        var cancelResponse = await Client.PostAsJsonAsync($"/invoice/v1/invoices/{draft!.Id}/cancel", cancelRequest);

        // Assert - Should fail because only finalized invoices can be cancelled
        Assert.Equal(System.Net.HttpStatusCode.Conflict, cancelResponse.StatusCode);
    }

    [Fact]
    public async Task CancelInvoice_WithPayments_StillAllowsCancellation()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

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
        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        // Add payment
        var paymentResponse = await Client.PostAsJsonAsync("/invoice/v1/payments", new
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
        var cancelResponse = await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice.Id}/cancel", cancelRequest);

        // Assert - Should allow cancellation (business rule: cancellation allowed, refund will be processed separately)
        cancelResponse.EnsureSuccessStatusCode();
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.Equal("Cancelled", cancelled!.Status);
    }

    #endregion
}
