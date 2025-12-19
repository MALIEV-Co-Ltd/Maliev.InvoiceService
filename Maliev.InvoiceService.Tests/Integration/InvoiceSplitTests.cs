using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;

namespace Maliev.InvoiceService.Tests.Integration;

public class InvoiceSplitTests : BaseIntegrationTest
{
    public InvoiceSplitTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task SplitInvoice_Into50_50_CreatesProportionalInvoices()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange - Create and finalize invoice
        var invoiceId = await CreateAndFinalizeInvoiceAsync();

        // Act - Split invoice 50/50
        var splitRequest = new SplitInvoiceRequest
        {
            SplitRules = new List<InvoiceSplitRule>
            {
                new() { Percentage = 50m, Notes = "First half" },
                new() { Percentage = 50m, Notes = "Second half" }
            }
        };

        var splitResponse = await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoiceId}/split", splitRequest);

        // Assert
        if (!splitResponse.IsSuccessStatusCode)
        {
            var errorContent = await splitResponse.Content.ReadAsStringAsync();
            throw new Exception($"Request failed with status {splitResponse.StatusCode}: {errorContent}");
        }
        Assert.Equal(HttpStatusCode.Created, splitResponse.StatusCode);
        var childInvoices = await splitResponse.Content.ReadFromJsonAsync<List<InvoiceResponse>>();

        Assert.NotNull(childInvoices);
        Assert.Equal(2, childInvoices!.Count);

        // Each child should have 50% of the parent amounts
        foreach (var child in childInvoices)
        {
            Assert.Equal(500m, child.Subtotal); // 50% of 1000
            Assert.Equal(35m, child.TaxAmount); // 50% of 70
            Assert.Equal(535m, child.GrandTotal); // 50% of 1070
            Assert.Equal(invoiceId, child.ParentInvoiceId);
            Assert.Equal("Finalized", child.Status);
        }
    }

    [Fact]
    public async Task SplitInvoice_Into70_30_CreatesCorrectProportions()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        var invoiceId = await CreateAndFinalizeInvoiceAsync();

        // Act
        var splitRequest = new SplitInvoiceRequest
        {
            SplitRules = new List<InvoiceSplitRule>
            {
                new() { Percentage = 70m, Notes = "Larger portion" },
                new() { Percentage = 30m, Notes = "Smaller portion" }
            }
        };

        var splitResponse = await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoiceId}/split", splitRequest);

        // Assert
        splitResponse.EnsureSuccessStatusCode();
        var childInvoices = await splitResponse.Content.ReadFromJsonAsync<List<InvoiceResponse>>();

        Assert.Equal(2, childInvoices!.Count);

        var largerChild = childInvoices[0];
        Assert.Equal(700m, largerChild.Subtotal); // 70% of 1000
        Assert.Equal(749m, largerChild.GrandTotal); // 70% of 1070

        var smallerChild = childInvoices[1];
        Assert.Equal(300m, smallerChild.Subtotal); // 30% of 1000
        Assert.Equal(321m, smallerChild.GrandTotal); // 30% of 1070
    }

    [Fact]
    public async Task SplitInvoice_WithInvalidPercentages_ReturnsBadRequest()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        var invoiceId = await CreateAndFinalizeInvoiceAsync();

        // Act - Try to split with percentages that don't sum to 100
        var splitRequest = new SplitInvoiceRequest
        {
            SplitRules = new List<InvoiceSplitRule>
            {
                new() { Percentage = 60m },
                new() { Percentage = 30m } // Only 90% total
            }
        };

        var splitResponse = await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoiceId}/split", splitRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, splitResponse.StatusCode);
    }

    [Fact]
    public async Task SplitInvoice_DraftInvoice_ReturnsConflict()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange - Create draft invoice (don't finalize)
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
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = 1000, TaxRate = 7 }
            }
        };

        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        var draft = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Act - Try to split draft invoice
        var splitRequest = new SplitInvoiceRequest
        {
            SplitRules = new List<InvoiceSplitRule>
            {
                new() { Percentage = 50m },
                new() { Percentage = 50m }
            }
        };

        var splitResponse = await Client.PostAsJsonAsync($"/invoice/v1/invoices/{draft!.Id}/split", splitRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, splitResponse.StatusCode);
    }

    private async Task<Guid> CreateAndFinalizeInvoiceAsync()
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
                    UnitPrice = 1000,
                    TaxRate = 7
                }
            }
        };

        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        var draft = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        var finalizeResponse = await Client.PostAsJsonAsync($"/invoice/v1/invoices/{draft!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });
        var finalized = await finalizeResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        return finalized!.Id;
    }
}
