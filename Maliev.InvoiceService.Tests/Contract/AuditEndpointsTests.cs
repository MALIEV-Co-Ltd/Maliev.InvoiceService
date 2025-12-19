using System.Net;
using System.Net.Http.Json;
using Maliev.InvoiceService.Api.Models.Audit;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;

namespace Maliev.InvoiceService.Tests.Contract;

/// <summary>
/// Contract tests for Audit endpoints
/// T128 per tasks.md
/// </summary>
public class AuditEndpointsTests : BaseContractTest
{
    public AuditEndpointsTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region T128 - GET /invoice/v1/audit/invoices/{id}

    [Fact]
    public async Task GET_AuditInvoicesById_WithInvoiceHistory_Returns200OK_WithAuditTrail()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange - Create invoice, finalize it, cancel it to generate audit trail
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Audit Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Audit St",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = 1000, TaxRate = 7 }
            }
        };
        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", createRequest);
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Finalize to create another audit entry
        await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        // Act
        var response = await Client.GetAsync($"/invoice/v1/audit/invoices/{invoice.Id}");

        // Assert - Contract verification
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auditTrail = await response.Content.ReadFromJsonAsync<List<AuditLogResponse>>();
        Assert.NotNull(auditTrail);
        Assert.True(auditTrail!.Count >= 2);

        // Verify audit entries contain expected fields
        Assert.Contains(auditTrail, a => a.Action == "Created");
        Assert.Contains(auditTrail, a => a.Action == "Finalized");

        foreach (var entry in auditTrail)
        {
            Assert.NotEqual(Guid.Empty, entry.Id);
            Assert.Equal("Invoice", entry.EntityType);
            Assert.Equal(invoice.Id, entry.EntityId);
            Assert.False(string.IsNullOrEmpty(entry.Action));
            Assert.NotEqual(default, entry.Timestamp);
        }
    }

    [Fact]
    public async Task GET_AuditInvoicesById_WithNonExistentInvoice_Returns404NotFound()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Act
        var response = await Client.GetAsync($"/invoice/v1/audit/invoices/{Guid.NewGuid()}");

        // Assert - Contract verification
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}
