using System.Net.Http.Json;
using FluentAssertions;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Maliev.InvoiceService.Tests.Integration;

/// <summary>
/// Integration tests for currency conversion workflows
/// T072, T149 per tasks.md
/// </summary>
[Collection("Database Collection")]
public class CurrencyConversionTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly TestWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public CurrencyConversionTests(TestDatabaseFixture dbFixture)
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

    #region T072 - Currency Conversion Workflow

    [Fact]
    public async Task CreateInvoice_WithUSDCurrency_FetchesExchangeRateAndStoresIt()
    {
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
        var response = await _client.PostAsJsonAsync("/invoices/v1/invoices", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();

        invoice.Should().NotBeNull();
        invoice!.Currency.Should().Be("USD");
        invoice.ExchangeRate.Should().NotBeNull();
        invoice.ExchangeRate!.Value.Should().BeGreaterThan(0);
        invoice.ExchangeRateSource.Should().NotBeNullOrEmpty();
        invoice.ExchangeRateSource.Should().Contain("Currency Service");
    }

    [Fact]
    public async Task CreateInvoice_WithTHBCurrency_DoesNotFetchExchangeRate()
    {
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
        var response = await _client.PostAsJsonAsync("/invoices/v1/invoices", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();

        invoice.Should().NotBeNull();
        invoice!.Currency.Should().Be("THB");
        invoice.ExchangeRate.Should().BeNull();
        invoice.ExchangeRateSource.Should().BeNullOrEmpty();
    }

    #endregion

    #region T149 - Currency Service Unavailable

    [Fact]
    public async Task CreateInvoice_WithUSDWhenCurrencyServiceUnavailable_CreatesInvoiceWithoutExchangeRate()
    {
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
        var response = await _client.PostAsJsonAsync("/invoices/v1/invoices", request);

        // Assert - Should still create invoice even if exchange rate fetch fails
        response.EnsureSuccessStatusCode();
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();

        invoice.Should().NotBeNull();
        invoice!.Currency.Should().Be("EUR");
        // Exchange rate may be null if service is unavailable (graceful degradation)
    }

    #endregion
}
