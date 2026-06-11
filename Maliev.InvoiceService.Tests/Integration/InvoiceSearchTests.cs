using System.Net.Http.Json;
using Maliev.InvoiceService.Application.Models.Common;
using Maliev.InvoiceService.Application.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;

namespace Maliev.InvoiceService.Tests.Integration;

/// <summary>
/// Integration tests for invoice search with multiple filters
/// T118 per tasks.md
/// </summary>
public class InvoiceSearchTests : BaseIntegrationTest
{
    public InvoiceSearchTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region T118 - Invoice Search with Multiple Filters

    [Fact]
    public async Task SearchInvoices_FilterByStatus_ReturnsOnlyMatchingInvoices()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange - Create invoices with different statuses
        var customerId = Guid.NewGuid();

        // Create draft invoice
        await CreateInvoiceAsync(customerId, "Draft Customer 1", finalize: false);

        // Create finalized invoice
        var finalizedId = await CreateInvoiceAsync(customerId, "Finalized Customer", finalize: true);

        // Act
        var response = await Client.GetAsync("/invoice/v1/invoices?status=Finalized");

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
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        var targetCustomerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();

        await CreateInvoiceAsync(targetCustomerId, "Target Customer", finalize: false);
        await CreateInvoiceAsync(otherCustomerId, "Other Customer", finalize: false);

        // Act
        var response = await Client.GetAsync($"/invoice/v1/invoices?customerId={targetCustomerId}");

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
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange - Create multiple invoices
        var customerId = Guid.NewGuid();
        for (int i = 0; i < 5; i++)
        {
            await CreateInvoiceAsync(customerId, $"Customer {i}", finalize: false);
        }

        // Act
        var response = await Client.GetAsync("/invoice/v1/invoices?page=1&pageSize=2");

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
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        var targetCustomerId = Guid.NewGuid();

        await CreateInvoiceAsync(targetCustomerId, "Target Draft", finalize: false);
        await CreateInvoiceAsync(targetCustomerId, "Target Finalized", finalize: true);
        await CreateInvoiceAsync(Guid.NewGuid(), "Other Finalized", finalize: true);

        // Act - Filter by both status AND customerId
        var response = await Client.GetAsync($"/invoice/v1/invoices?status=Finalized&customerId={targetCustomerId}");

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

    [Fact]
    public async Task SearchInvoices_FilterByPoNumber_ReturnsOnlyMatchingInvoices()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        var customerId = Guid.NewGuid();
        var targetInvoiceId = await CreateInvoiceAsync(customerId, "Target PO Customer", finalize: false, poNumber: "PO-LIFECYCLE-001");
        await CreateInvoiceAsync(customerId, "Other PO Customer", finalize: false, poNumber: "PO-LIFECYCLE-002");

        // Act - Intranet order lifecycle lookup sends this exact query parameter.
        var response = await Client.GetAsync("/invoice/v1/invoices?poNumber=PO-LIFECYCLE-001&pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<InvoiceResponse>>();

        Assert.NotNull(result);
        Assert.NotNull(result!.Items);
        var invoice = Assert.Single(result.Items);
        Assert.Equal(targetInvoiceId, invoice.Id);
        Assert.Equal("PO-LIFECYCLE-001", invoice.PoNumber);
    }

    #endregion

    private async Task<Guid> CreateInvoiceAsync(Guid customerId, string customerName, bool finalize, string? poNumber = null)
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
            PoNumber = poNumber,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = 1000, TaxRate = 7 }
            }
        };

        var response = await Client.PostAsJsonAsync("/invoice/v1/invoices", request);
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();

        if (finalize)
        {
            await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/finalize",
                new FinalizeInvoiceRequest { FinalizedBy = "test-user" });
        }

        return invoice!.Id;
    }
}
