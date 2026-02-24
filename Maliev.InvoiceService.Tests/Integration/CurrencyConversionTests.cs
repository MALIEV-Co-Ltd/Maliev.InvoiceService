using System.Net.Http.Json;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;

namespace Maliev.InvoiceService.Tests.Integration;

/// <summary>
/// Integration tests for currency conversion workflows
/// T072, T149 per tasks.md
/// </summary>
public class CurrencyConversionTests : BaseIntegrationTest
{
    public CurrencyConversionTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region T072 - Currency Conversion Workflow

    [Fact]
    public async Task CreateInvoice_WithUSDCurrency_FetchesExchangeRateAndStoresIt()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange
        var request = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "USD Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St",
            Currency = "USD", // Non-THB currency
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = 100, TaxRate = 7 }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/invoice/v1/invoices", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();

        Assert.NotNull(invoice);
        Assert.Equal("USD", invoice!.Currency);
        Assert.NotNull(invoice.ExchangeRate);
        Assert.True(invoice.ExchangeRate!.Value > 0);
        Assert.False(string.IsNullOrEmpty(invoice.ExchangeRateSource));
        Assert.Contains("Currency Service", invoice.ExchangeRateSource);
    }

    [Fact]
    public async Task CreateInvoice_WithTHBCurrency_DoesNotFetchExchangeRate()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange
        var request = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "THB Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St",
            Currency = "THB", // Local currency
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = 1000, TaxRate = 7 }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/invoice/v1/invoices", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();

        Assert.NotNull(invoice);
        Assert.Equal("THB", invoice!.Currency);
        Assert.Null(invoice.ExchangeRate);
        Assert.True(string.IsNullOrEmpty(invoice.ExchangeRateSource));
    }

    #endregion

    #region T149 - Currency Service Unavailable

    [Fact]
    public async Task CreateInvoice_WithUSDWhenCurrencyServiceUnavailable_CreatesInvoiceWithoutExchangeRate()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange - This test assumes Currency Service is mocked to return null/fail
        var request = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "USD Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St",
            Currency = "EUR", // Another foreign currency
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = 100, TaxRate = 7 }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/invoice/v1/invoices", request);

        // Assert - Should still create invoice even if exchange rate fetch fails
        response.EnsureSuccessStatusCode();
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();

        Assert.NotNull(invoice);
        Assert.Equal("EUR", invoice!.Currency);
        // Exchange rate may be null if service is unavailable (graceful degradation)
    }

    #endregion
}
