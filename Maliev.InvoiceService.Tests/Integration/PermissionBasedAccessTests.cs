using System.Net;
using System.Net.Http.Json;
using Maliev.InvoiceService.Api.Authorization;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;
using Maliev.InvoiceService.Tests.Testing;

using Xunit.Abstractions;

namespace Maliev.InvoiceService.Tests.Integration;

public class PermissionBasedAccessTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public PermissionBasedAccessTests(TestWebApplicationFactory factory, ITestOutputHelper testOutputHelper) : base(factory)
    {
        _testOutputHelper = testOutputHelper;
    }

    private CreateInvoiceRequest CreateValidRequest() => new()
    {
        CustomerId = Guid.NewGuid(),
        CustomerName = "Test Customer",
        CustomerTaxId = "1234567890123",
        BillingAddress = "123 Test St",
        Currency = "THB",
        IssueDate = DateTime.UtcNow.Date,
        DueDate = DateTime.UtcNow.Date.AddDays(30),
        Lines = new List<InvoiceLineItemRequest>
        {
            new() { LineNumber = 1, Description = "Item", Quantity = 1, UnitPrice = 100, TaxCategory = "VAT" }
        }
    };

    [Fact]
    public async Task CreateInvoice_WithCreatePermission_ReturnsCreated()
    {
        // Arrange
        await CleanDatabaseAsync();
        var client = Factory.CreateClient().WithTestAuth(Factory, InvoicePermissions.InvoicesCreate);
        var request = CreateValidRequest();

        // Act
        var response = await client.PostAsJsonAsync("/invoice/v1/invoices", request);

        // Assert
        var error = response.StatusCode != HttpStatusCode.Created ? await response.Content.ReadAsStringAsync() : "";
        Assert.True(response.StatusCode == HttpStatusCode.Created, $"Expected Created but got {response.StatusCode}. Error: {error}");
    }

    [Fact]
    public async Task CreateInvoice_WithoutCreatePermission_ReturnsForbidden()
    {
        // Arrange
        await CleanDatabaseAsync();
        // Only give read permission
        var client = Factory.CreateClient().WithTestAuth(Factory, InvoicePermissions.InvoicesRead);
        var request = CreateValidRequest();

        // Act
        var response = await client.PostAsJsonAsync("/invoice/v1/invoices", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task FinalizeInvoice_WithoutFinalizePermission_ReturnsForbidden()
    {
        // Arrange
        await CleanDatabaseAsync();
        // Create an invoice first using admin client
        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", CreateValidRequest());
        createResponse.EnsureSuccessStatusCode();
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Create client with only read/create permission but NO finalize
        var client = Factory.CreateClient().WithTestAuth(Factory, InvoicePermissions.InvoicesRead, InvoicePermissions.InvoicesCreate);

        // Act
        var response = await client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/finalize", new FinalizeInvoiceRequest { FinalizedBy = "test" });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditTrail_WithoutReadPermission_ReturnsForbidden()
    {
        // Arrange
        await CleanDatabaseAsync();
        var invoiceId = Guid.NewGuid();
        // Create client with ONLY create permission but NO read
        var client = Factory.CreateClient().WithTestAuth(Factory, InvoicePermissions.InvoicesCreate);

        // Act
        var response = await client.GetAsync($"/invoice/v1/audit/invoices/{invoiceId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}