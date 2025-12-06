using System.Net.Http.Json;
using Maliev.InvoiceService.Api.Models.Common;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Maliev.InvoiceService.Tests.Integration;

/// <summary>
/// Integration tests for invoice search with multiple filters
/// T118 per tasks.md
/// </summary>
[Collection("Database Collection")]
public class InvoiceSearchTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly TestWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public InvoiceSearchTests(TestDatabaseFixture dbFixture)
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

    #region T118 - Invoice Search with Multiple Filters

    [Fact]
    public async Task SearchInvoices_FilterByStatus_ReturnsOnlyMatchingInvoices()
    {
        // Arrange - Create invoices with different statuses
        var customerId = Guid.NewGuid();

        // Create draft invoice
        await CreateInvoiceAsync(customerId, "Draft Customer 1", finalize: false);

        // Create finalized invoice
        var finalizedId = await CreateInvoiceAsync(customerId, "Finalized Customer", finalize: true);

        // Act
        var response = await _client.GetAsync("/invoices/v1/invoices?status=Finalized");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<InvoiceResponse>>();

        Assert.NotNull(result);
        Assert.NotNull(result!.Items);
        Assert.All(result.Items, i => Assert.Equal("Finalized", i.Status));
        Assert.Contains(result.Items, i => i.Id == finalizedId);
    }

    [Fact]
    public async Task SearchInvoices_FilterByCustomerId_ReturnsOnlyCustomerInvoices()
    {
        // Arrange
        var targetCustomerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();

        await CreateInvoiceAsync(targetCustomerId, "Target Customer", finalize: false);
        await CreateInvoiceAsync(otherCustomerId, "Other Customer", finalize: false);

        // Act
        var response = await _client.GetAsync($"/invoices/v1/invoices?customerId={targetCustomerId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<InvoiceResponse>>();

        Assert.NotNull(result);
        Assert.NotNull(result!.Items);
        Assert.All(result.Items, i => Assert.Equal(targetCustomerId, i.CustomerId));
    }

    [Fact]
    public async Task SearchInvoices_WithPagination_ReturnsCorrectPageSize()
    {
        // Arrange - Create multiple invoices
        var customerId = Guid.NewGuid();
        for (int i = 0; i < 5; i++)
        {
            await CreateInvoiceAsync(customerId, $"Customer {i}", finalize: false);
        }

        // Act
        var response = await _client.GetAsync("/invoices/v1/invoices?page=1&pageSize=2");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<InvoiceResponse>>();

        Assert.NotNull(result);
        Assert.NotNull(result!.Items);
        Assert.True(result.Items.Count() <= 2);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.True(result.TotalCount >= 5);
    }

    [Fact]
    public async Task SearchInvoices_CombinedFilters_ReturnsIntersectionOfFilters()
    {
        // Arrange
        var targetCustomerId = Guid.NewGuid();

        await CreateInvoiceAsync(targetCustomerId, "Target Draft", finalize: false);
        await CreateInvoiceAsync(targetCustomerId, "Target Finalized", finalize: true);
        await CreateInvoiceAsync(Guid.NewGuid(), "Other Finalized", finalize: true);

        // Act - Filter by both status AND customerId
        var response = await _client.GetAsync($"/invoices/v1/invoices?status=Finalized&customerId={targetCustomerId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<InvoiceResponse>>();

        Assert.NotNull(result);
        Assert.NotNull(result!.Items);
        Assert.All(result.Items, i => 
        {
            Assert.Equal("Finalized", i.Status);
            Assert.Equal(targetCustomerId, i.CustomerId);
        });
    }

    #endregion

    private async Task<Guid> CreateInvoiceAsync(Guid customerId, string customerName, bool finalize)
    {
        var request = new CreateInvoiceRequest
        {
            CustomerId = customerId,
            CustomerName = customerName,
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

        var response = await _client.PostAsJsonAsync("/invoices/v1/invoices", request);
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();

        if (finalize)
        {
            await _client.PostAsJsonAsync($"/invoices/v1/invoices/{invoice!.Id}/finalize",
                new FinalizeInvoiceRequest { FinalizedBy = "test-user" });
        }

        return invoice!.Id;
    }
}
