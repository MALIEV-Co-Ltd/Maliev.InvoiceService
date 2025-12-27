using System.Net;
using System.Net.Http.Json;
using Maliev.InvoiceService.Api.Authorization;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;
using Maliev.InvoiceService.Tests.Testing;
using System.Security.Claims;
using Xunit.Abstractions;

namespace Maliev.InvoiceService.Tests.Integration;

public class LegacyRoleMappingTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public LegacyRoleMappingTests(TestWebApplicationFactory factory, ITestOutputHelper testOutputHelper) : base(factory)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact(Skip = "Role-to-permission mapping not implemented - permission-based auth is always enforced")]
    public async Task Request_WithLegacyRoleOnly_ShouldBeMappedToPermissions()
    {
        // Scenario: User has 'Manager' role but NO permissions claim.
        // Expected: Success (if mapping is implemented)

        // Arrange
        await CleanDatabaseAsync();

        // Create an invoice first
        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            Lines = new List<InvoiceLineItemRequest> { new() { LineNumber = 1, Description = "Item", Quantity = 1, UnitPrice = 100, TaxCategory = "VAT" } }
        });

        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            var error = await createResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create invoice: {createResponse.StatusCode}. Error: {error}");
        }

        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Create client with ONLY 'Manager' role claim
        var claims = new Dictionary<string, string>
        {
            [ClaimTypes.Role] = "Manager"
        };

        var token = Factory.CreateTestJwtToken(userId: "test-user", additionalClaims: claims);
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        // Act
        var response = await client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/finalize", new FinalizeInvoiceRequest { FinalizedBy = "test" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var finalInvoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.Equal("Finalized", finalInvoice!.Status);
    }
}
