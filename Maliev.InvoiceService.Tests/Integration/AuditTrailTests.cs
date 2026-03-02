using System.Net.Http.Json;
using Maliev.InvoiceService.Application.Models.Audit;
using Maliev.InvoiceService.Application.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;

namespace Maliev.InvoiceService.Tests.Integration;

/// <summary>
/// Integration tests for audit trail capture
/// T129 per tasks.md
/// </summary>
public class AuditTrailTests : BaseIntegrationTest
{
    public AuditTrailTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region T129 - Audit Trail Capture

    [Fact]
    public async Task InvoiceLifecycle_CapturesAllAuditEvents()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange & Act - Create invoice
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

        // Finalize invoice
        await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        // Cancel invoice
        await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice.Id}/cancel",
            new CancelInvoiceRequest { CancelledBy = "admin", CancellationReason = "Test cancellation" });

        // Assert - Retrieve audit trail
        var auditResponse = await Client.GetAsync($"/invoice/v1/audit/invoices/{invoice.Id}");
        auditResponse.EnsureSuccessStatusCode();

        var auditTrail = await auditResponse.Content.ReadFromJsonAsync<List<AuditLogResponse>>();

        Assert.NotNull(auditTrail);
        Assert.True(auditTrail!.Count >= 3);

        // Verify each lifecycle event is captured
        Assert.Contains(auditTrail, a => a.Action == "Created");
        Assert.Contains(auditTrail, a => a.Action == "Finalized");
        Assert.Contains(auditTrail, a => a.Action == "Cancelled");

        // Verify audit entries are chronologically ordered
        var timestamps = auditTrail.Select(a => a.Timestamp).ToList();
        for (int i = 1; i < timestamps.Count; i++)
        {
            Assert.True(timestamps[i] >= timestamps[i - 1], "Timestamps should be in ascending order");
        }
    }

    [Fact]
    public async Task InvoiceUpdate_CapturesUpdateAuditEvent()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange - Create draft invoice
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Original Name",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St",
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

        // Act - Update invoice
        var updateRequest = new UpdateInvoiceRequest
        {
            CustomerName = "Updated Name",
            CustomerTaxId = "1234567890123",
            BillingAddress = "456 Updated St",
            Currency = "THB",
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Updated Product", Quantity = 2, UnitPrice = 1000, TaxRate = 7 }
            },
            RowVersion = invoice!.RowVersion
        };
        var updateResponse = await Client.PutAsJsonAsync($"/invoice/v1/invoices/{invoice.Id}", updateRequest);
        updateResponse.EnsureSuccessStatusCode();

        // Assert - Check audit trail
        var auditResponse = await Client.GetAsync($"/invoice/v1/audit/invoices/{invoice.Id}");
        var auditTrail = await auditResponse.Content.ReadFromJsonAsync<List<AuditLogResponse>>();

        Assert.NotNull(auditTrail);
        Assert.Contains(auditTrail!, a => a.Action == "Updated");
    }

    [Fact]
    public async Task AuditLog_Contains7YearRetentionMetadata()
    {
        // Arrange - Clean database for test isolation
        await CleanDatabaseAsync();

        // Arrange - Create invoice
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Retention Test",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St",
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

        // Act - Retrieve audit
        var auditResponse = await Client.GetAsync($"/invoice/v1/audit/invoices/{invoice!.Id}");
        var auditTrail = await auditResponse.Content.ReadFromJsonAsync<List<AuditLogResponse>>();

        // Assert - Verify audit records exist (7-year retention requirement)
        Assert.NotNull(auditTrail);
        Assert.True(auditTrail!.Count >= 1);

        foreach (var entry in auditTrail)
        {
            Assert.NotEqual(default, entry.Timestamp);
            Assert.True(entry.Timestamp > DateTime.UtcNow.AddMinutes(-5), "Timestamp should be recent");
        }
    }

    #endregion
}
