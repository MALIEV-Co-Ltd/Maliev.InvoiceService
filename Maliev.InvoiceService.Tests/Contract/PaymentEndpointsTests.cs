using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Api.Models.Payments;
using Maliev.InvoiceService.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Maliev.InvoiceService.Tests.Contract;

/// <summary>
/// Contract tests for Payment endpoints
/// T157, T158 per tasks.md
/// </summary>
[Collection("Database Collection")]
public class PaymentEndpointsTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly TestWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public PaymentEndpointsTests(TestDatabaseFixture dbFixture)
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

    #region T157 - POST /invoices/v1/payments

    [Fact]
    public async Task POST_Payments_WithValidData_Returns201Created_WithLocationHeader()
    {
        // Arrange
        var request = new CreatePaymentRequest
        {
            PaymentAmount = 5000m,
            PaymentDate = DateTime.UtcNow.Date,
            PaymentMethod = "Bank Transfer",
            ReferenceNumber = "TXN-2025-001",
            Notes = "Payment for services",
            RecordedBy = "cashier-1"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/invoices/v1/payments", request);

        // Assert - Contract verification
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.PathAndQuery.Should().Contain("/invoices/v1/payments/");

        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        payment.Should().NotBeNull();
        payment!.Id.Should().NotBeEmpty();
        payment.PaymentAmount.Should().Be(5000m);
        payment.PaymentMethod.Should().Be("Bank Transfer");
        payment.ReferenceNumber.Should().Be("TXN-2025-001");
        payment.RecordedBy.Should().Be("cashier-1");
    }

    #endregion

    #region T158 - GET /invoices/v1/payments/{id}

    [Fact]
    public async Task GET_PaymentsById_WithExistingId_Returns200OK_WithPaymentDetails()
    {
        // Arrange - Create payment first
        var createRequest = new CreatePaymentRequest
        {
            PaymentAmount = 3000m,
            PaymentDate = DateTime.UtcNow.Date,
            PaymentMethod = "Cash",
            ReferenceNumber = "CASH-001",
            RecordedBy = "cashier-2"
        };
        var createResponse = await _client.PostAsJsonAsync("/invoices/v1/payments", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<PaymentResponse>();

        // Act
        var response = await _client.GetAsync($"/invoices/v1/payments/{created!.Id}");

        // Assert - Contract verification
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        payment.Should().NotBeNull();
        payment!.Id.Should().Be(created.Id);
        payment.PaymentAmount.Should().Be(3000m);
        payment.PaymentMethod.Should().Be("Cash");
    }

    [Fact]
    public async Task GET_PaymentsById_WithNonExistentId_Returns404NotFound()
    {
        // Act
        var response = await _client.GetAsync($"/invoices/v1/payments/{Guid.NewGuid()}");

        // Assert - Contract verification
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
