using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;

namespace Maliev.InvoiceService.Tests.Integration;

public class InvoiceCreationTests : BaseIntegrationTest
{
    public InvoiceCreationTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateInvoice_WithValidData_ReturnsCreated()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();
        // Arrange
        var request = new CreateInvoiceRequest
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
                    Quantity = 2,
                    UnitPrice = 100,
                    TaxRate = 7
                }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/invoice/v1/invoices", request);

        // Assert
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Request failed with status {response.StatusCode}: {errorContent}");
        }
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(invoice);
        Assert.Equal("Test Customer", invoice!.CustomerName);
        Assert.Equal("Draft", invoice.Status);
        Assert.Single(invoice.Lines);
    }

    [Fact]
    public async Task CreateInvoice_WithWithholdingTax_CalculatesCorrectly()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();
        var request = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St, Bangkok",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            WithholdingTaxPercentage = 3m,
            Lines = new List<InvoiceLineItemRequest>
            {
                new()
                {
                    LineNumber = 1,
                    Description = "Service",
                    Quantity = 1,
                    UnitPrice = 1000,
                    TaxRate = 7
                }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/invoice/v1/invoices", request);

        // Assert
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Request failed with status {response.StatusCode}: {errorContent}");
        }
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(invoice);
        Assert.Equal(1000m, invoice!.Subtotal);
        Assert.Equal(70m, invoice.TaxAmount); // 7% of 1000
        Assert.Equal(30m, invoice.WithholdingTaxAmount); // 3% of 1000
        Assert.Equal(1040m, invoice.GrandTotal); // 1000 + 70 - 30
    }
}
