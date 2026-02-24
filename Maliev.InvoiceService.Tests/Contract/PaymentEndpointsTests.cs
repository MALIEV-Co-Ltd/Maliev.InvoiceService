using System.Net;
using System.Net.Http.Json;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Api.Models.Payments;
using Maliev.InvoiceService.Tests.Fixtures;

namespace Maliev.InvoiceService.Tests.Contract;

/// <summary>
/// Contract tests for Payment endpoints
/// T157, T158 per tasks.md
/// </summary>
public class PaymentEndpointsTests : BaseContractTest
{
    public PaymentEndpointsTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region T157 - POST /invoice/v1/payments

    [Fact]
    public async Task POST_Payments_WithValidData_Returns201Created_WithLocationHeader()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();
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
        var response = await Client.PostAsJsonAsync("/invoice/v1/payments", request);

        // Assert - Contract verification
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/invoice/v1/payments/", response.Headers.Location!.PathAndQuery);

        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(payment);
        Assert.NotEqual(Guid.Empty, payment!.Id);
        Assert.Equal(5000m, payment.PaymentAmount);
        Assert.Equal("Bank Transfer", payment.PaymentMethod);
        Assert.Equal("TXN-2025-001", payment.ReferenceNumber);
        Assert.Equal("cashier-1", payment.RecordedBy);
    }

    #endregion

    #region T158 - GET /invoice/v1/payments/{id}

    [Fact]
    public async Task GET_PaymentsById_WithExistingId_Returns200OK_WithPaymentDetails()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Create payment first
        var createRequest = new CreatePaymentRequest
        {
            PaymentAmount = 3000m,
            PaymentDate = DateTime.UtcNow.Date,
            PaymentMethod = "Cash",
            ReferenceNumber = "CASH-001",
            RecordedBy = "cashier-2"
        };
        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/payments", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<PaymentResponse>();

        // Act
        var response = await Client.GetAsync($"/invoice/v1/payments/{created!.Id}");

        // Assert - Contract verification
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(payment);
        Assert.Equal(created.Id, payment!.Id);
        Assert.Equal(3000m, payment.PaymentAmount);
        Assert.Equal("Cash", payment.PaymentMethod);
    }

    [Fact]
    public async Task GET_PaymentsById_WithNonExistentId_Returns404NotFound()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Act
        var response = await Client.GetAsync($"/invoice/v1/payments/{Guid.NewGuid()}");

        // Assert - Contract verification
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}
