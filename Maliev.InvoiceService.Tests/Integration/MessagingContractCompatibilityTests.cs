using System.Text.Json;
using Maliev.MessagingContracts.Contracts.Orders;
using Maliev.MessagingContracts.Contracts.Payments;

namespace Maliev.InvoiceService.Tests.Integration;

public sealed class MessagingContractCompatibilityTests
{
    [Fact]
    public void PaymentCompletedPayload_ProviderOmitted_PreservesHistoricalV1WireShape()
    {
        var payload = new PaymentCompletedEventPayload(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-1001",
            CustomerId: Guid.NewGuid().ToString(),
            PaymentId: Guid.NewGuid(),
            Amount: 1250,
            Currency: "THB");

        var json = JsonSerializer.Serialize(payload);
        using var document = JsonDocument.Parse(json);

        Assert.Equal(string.Empty, payload.ProviderName);
        Assert.True(document.RootElement.TryGetProperty("providerName", out var providerName));
        Assert.Equal(string.Empty, providerName.GetString());
    }

    [Fact]
    public void OrderPaidPayload_ProviderOmitted_PreservesHistoricalV1WireShape()
    {
        var payload = new OrderPaidEventPayload(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-1002",
            PaymentId: Guid.NewGuid(),
            PaidAmount: 2500,
            Currency: "THB",
            PaidAt: DateTimeOffset.UtcNow);

        var json = JsonSerializer.Serialize(payload);
        using var document = JsonDocument.Parse(json);

        Assert.Equal(string.Empty, payload.ProviderName);
        Assert.True(document.RootElement.TryGetProperty("providerName", out var providerName));
        Assert.Equal(string.Empty, providerName.GetString());
    }
}
