using FluentAssertions;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;

namespace Maliev.InvoiceService.Tests.Integration;

[Collection("Database Collection")]
public class InvoiceFinalizationTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public InvoiceFinalizationTests(TestDatabaseFixture dbFixture)
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
    public async Task FinalizeInvoice_WithValidDraftInvoice_AssignsSequentialNumber()
    {
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

        var createResponse = await _client.PostAsJsonAsync("/invoices/v1/invoices", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var draftInvoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Act - Finalize invoice
        var finalizeRequest = new FinalizeInvoiceRequest { FinalizedBy = "test-user" };
        var finalizeResponse = await _client.PostAsJsonAsync($"/invoices/v1/invoices/{draftInvoice!.Id}/finalize", finalizeRequest);

        // Assert
        if (!finalizeResponse.IsSuccessStatusCode)
        {
            var errorContent = await finalizeResponse.Content.ReadAsStringAsync();
            throw new Exception($"Request failed with status {finalizeResponse.StatusCode}: {errorContent}");
        }
        finalizeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var finalizedInvoice = await finalizeResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        finalizedInvoice.Should().NotBeNull();
        finalizedInvoice!.Status.Should().Be("Finalized");
        finalizedInvoice.InvoiceNumber.Should().NotBeNullOrEmpty();
        finalizedInvoice.InvoiceNumber.Should().MatchRegex(@"^INV-\d{8}-\d{6}$");
        finalizedInvoice.FinalizedAt.Should().NotBeNull();
        finalizedInvoice.FinalizedBy.Should().Be("test-user");
    }

    [Fact]
    public async Task FinalizeInvoice_SecondInvoice_HasIncrementedSequenceNumber()
    {
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

        var response1 = await _client.PostAsJsonAsync("/invoices/v1/invoices", request1);
        var draft1 = await response1.Content.ReadFromJsonAsync<InvoiceResponse>();

        var finalizeResponse1 = await _client.PostAsJsonAsync($"/invoices/v1/invoices/{draft1!.Id}/finalize",
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

        var response2 = await _client.PostAsJsonAsync("/invoices/v1/invoices", request2);
        var draft2 = await response2.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Act
        var finalizeResponse2 = await _client.PostAsJsonAsync($"/invoices/v1/invoices/{draft2!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "user2" });

        // Assert
        finalizeResponse2.EnsureSuccessStatusCode();
        var finalized2 = await finalizeResponse2.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Extract sequence numbers
        finalized1!.InvoiceNumber.Should().NotBeNullOrEmpty();
        finalized2!.InvoiceNumber.Should().NotBeNullOrEmpty();

        var seq1 = int.Parse(finalized1.InvoiceNumber!.Split('-')[2]);
        var seq2 = int.Parse(finalized2.InvoiceNumber!.Split('-')[2]);

        seq2.Should().Be(seq1 + 1, "second invoice should have incremented sequence number");
    }

    [Fact]
    public async Task FinalizeInvoice_AlreadyFinalized_ReturnsConflict()
    {
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

        var createResponse = await _client.PostAsJsonAsync("/invoices/v1/invoices", createRequest);
        var draft = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        var finalizeRequest = new FinalizeInvoiceRequest { FinalizedBy = "user1" };
        await _client.PostAsJsonAsync($"/invoices/v1/invoices/{draft!.Id}/finalize", finalizeRequest);

        // Act - Try to finalize again
        var secondFinalizeResponse = await _client.PostAsJsonAsync($"/invoices/v1/invoices/{draft.Id}/finalize", finalizeRequest);

        // Assert
        secondFinalizeResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
