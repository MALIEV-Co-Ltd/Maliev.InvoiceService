using FluentAssertions;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;

namespace Maliev.InvoiceService.Tests.Integration;

[Collection("Database Collection")]
public class InvoiceSplitTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public InvoiceSplitTests(TestDatabaseFixture dbFixture)
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
    public async Task SplitInvoice_Into50_50_CreatesProportionalInvoices()
    {
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

        var splitResponse = await _client.PostAsJsonAsync($"/invoices/v1/invoices/{invoiceId}/split", splitRequest);

        // Assert
        if (!splitResponse.IsSuccessStatusCode)
        {
            var errorContent = await splitResponse.Content.ReadAsStringAsync();
            throw new Exception($"Request failed with status {splitResponse.StatusCode}: {errorContent}");
        }
        splitResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var childInvoices = await splitResponse.Content.ReadFromJsonAsync<List<InvoiceResponse>>();

        childInvoices.Should().NotBeNull();
        childInvoices.Should().HaveCount(2);

        // Each child should have 50% of the parent amounts
        foreach (var child in childInvoices!)
        {
            child.Subtotal.Should().Be(500m); // 50% of 1000
            child.TaxAmount.Should().Be(35m); // 50% of 70
            child.GrandTotal.Should().Be(535m); // 50% of 1070
            child.ParentInvoiceId.Should().Be(invoiceId);
            child.Status.Should().Be("Finalized");
        }
    }

    [Fact]
    public async Task SplitInvoice_Into70_30_CreatesCorrectProportions()
    {
        // Arrange
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

        var splitResponse = await _client.PostAsJsonAsync($"/invoices/v1/invoices/{invoiceId}/split", splitRequest);

        // Assert
        splitResponse.EnsureSuccessStatusCode();
        var childInvoices = await splitResponse.Content.ReadFromJsonAsync<List<InvoiceResponse>>();

        childInvoices.Should().HaveCount(2);

        var largerChild = childInvoices![0];
        largerChild.Subtotal.Should().Be(700m); // 70% of 1000
        largerChild.GrandTotal.Should().Be(749m); // 70% of 1070

        var smallerChild = childInvoices[1];
        smallerChild.Subtotal.Should().Be(300m); // 30% of 1000
        smallerChild.GrandTotal.Should().Be(321m); // 30% of 1070
    }

    [Fact]
    public async Task SplitInvoice_WithInvalidPercentages_ReturnsBadRequest()
    {
        // Arrange
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

        var splitResponse = await _client.PostAsJsonAsync($"/invoices/v1/invoices/{invoiceId}/split", splitRequest);

        // Assert
        splitResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SplitInvoice_DraftInvoice_ReturnsConflict()
    {
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

        var createResponse = await _client.PostAsJsonAsync("/invoices/v1/invoices", createRequest);
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

        var splitResponse = await _client.PostAsJsonAsync($"/invoices/v1/invoices/{draft!.Id}/split", splitRequest);

        // Assert
        splitResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
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

        var createResponse = await _client.PostAsJsonAsync("/invoices/v1/invoices", createRequest);
        var draft = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        var finalizeResponse = await _client.PostAsJsonAsync($"/invoices/v1/invoices/{draft!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });
        var finalized = await finalizeResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        return finalized!.Id;
    }
}
