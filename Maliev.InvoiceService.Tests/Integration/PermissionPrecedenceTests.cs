using System.Net;
using System.Net.Http.Json;
using Maliev.InvoiceService.Api.Authorization;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;
using Maliev.InvoiceService.Tests.Testing;
using System.Security.Claims;

namespace Maliev.InvoiceService.Tests.Integration;

public class PermissionPrecedenceTests : BaseIntegrationTest
{
    public PermissionPrecedenceTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Request_WithConflictingRoleAndPermission_ShouldHonorPermission()
    {
        // Scenario: User has 'Manager' role (which traditionally allows finalize) 
        // BUT explicit 'invoice.invoices.finalize' permission is NOT granted.
        // Expected: Forbidden (since permissions take precedence and roles are ignored for [RequirePermission])

        // Arrange
        await CleanDatabaseAsync();
        
        // Create an invoice first using admin client
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

        // Create client with 'Manager' role claim but ONLY 'read' permission
        var claims = new Dictionary<string, string>
        {
            [ClaimTypes.Role] = "Manager",
            ["permissions"] = InvoicePermissions.InvoicesRead
        };
        
        var token = Factory.CreateTestJwtToken("test-user", null, claims);
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        // Act
        var response = await client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/finalize", new FinalizeInvoiceRequest { FinalizedBy = "test" });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
