using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace Maliev.InvoiceService.Tests.Integration;

public class InvoiceFinalizationTests : BaseIntegrationTest
{
    public InvoiceFinalizationTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task FinalizeInvoice_WithValidDraftInvoice_AssignsSequentialNumber()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange - Create draft invoice
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St, Bangkok",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new()
                {
                    LineNumber = 1,
                    Description = "Test Product",
                    Quantity = 1,
                    UnitPrice = 1000,
                    TaxRate = 7
                }
            }
        };

        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var draftInvoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Act - Finalize invoice
        var finalizeRequest = new FinalizeInvoiceRequest { FinalizedBy = "test-user" };
        var finalizeResponse = await Client.PostAsJsonAsync($"/invoice/v1/invoices/{draftInvoice!.Id}/finalize", finalizeRequest);

        // Assert
        if (!finalizeResponse.IsSuccessStatusCode)
        {
            var errorContent = await finalizeResponse.Content.ReadAsStringAsync();
            throw new Exception($"Request failed with status {finalizeResponse.StatusCode}: {errorContent}");
        }
        Assert.Equal(HttpStatusCode.OK, finalizeResponse.StatusCode);
        var finalizedInvoice = await finalizeResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        Assert.NotNull(finalizedInvoice);
        Assert.Equal("Finalized", finalizedInvoice!.Status);
        Assert.False(string.IsNullOrEmpty(finalizedInvoice.InvoiceNumber));
        Assert.Matches(@"^(INV|TAX)-\d{8}-\d{6}$", finalizedInvoice.InvoiceNumber);
        Assert.NotNull(finalizedInvoice.FinalizedAt);
        Assert.Equal("test-user", finalizedInvoice.FinalizedBy);
    }

    [Fact]
    public async Task FinalizeInvoice_SecondInvoice_HasIncrementedSequenceNumber()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange - Create and finalize first invoice
        var request1 = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Customer 1",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product 1", Quantity = 1, UnitPrice = 100, TaxRate = 7 }
            }
        };

        var response1 = await Client.PostAsJsonAsync("/invoice/v1/invoices", request1);
        var draft1 = await response1.Content.ReadFromJsonAsync<InvoiceResponse>();

        var finalizeResponse1 = await Client.PostAsJsonAsync($"/invoice/v1/invoices/{draft1!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "user1" });
        var finalized1 = await finalizeResponse1.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Create and finalize second invoice
        var request2 = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Customer 2",
            CustomerTaxId = "1234567890123",
            BillingAddress = "456 Test St",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product 2", Quantity = 1, UnitPrice = 200, TaxRate = 7 }
            }
        };

        var response2 = await Client.PostAsJsonAsync("/invoice/v1/invoices", request2);
        var draft2 = await response2.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Act
        var finalizeResponse2 = await Client.PostAsJsonAsync($"/invoice/v1/invoices/{draft2!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "user2" });

        // Assert
        finalizeResponse2.EnsureSuccessStatusCode();
        var finalized2 = await finalizeResponse2.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Extract sequence numbers
        Assert.False(string.IsNullOrEmpty(finalized1!.InvoiceNumber));
        Assert.False(string.IsNullOrEmpty(finalized2!.InvoiceNumber));

        var seq1 = int.Parse(finalized1.InvoiceNumber!.Split('-')[2]);
        var seq2 = int.Parse(finalized2.InvoiceNumber!.Split('-')[2]);

        Assert.Equal(seq1 + 1, seq2); // second invoice should have incremented sequence number
    }

    [Fact]
    public async Task FinalizeInvoice_AlreadyFinalized_ReturnsConflict()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange - Create and finalize invoice
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
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = 100, TaxRate = 7 }
            }
        };

        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        var draft = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        var finalizeRequest = new FinalizeInvoiceRequest { FinalizedBy = "user1" };
        await Client.PostAsJsonAsync($"/invoice/v1/invoices/{draft!.Id}/finalize", finalizeRequest);

        // Act - Try to finalize again
        var secondFinalizeResponse = await Client.PostAsJsonAsync($"/invoice/v1/invoices/{draft.Id}/finalize", finalizeRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, secondFinalizeResponse.StatusCode);
    }
}
