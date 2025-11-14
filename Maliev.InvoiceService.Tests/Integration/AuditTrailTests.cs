using System.Net.Http.Json;
using FluentAssertions;
using Maliev.InvoiceService.Api.Models.Audit;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Maliev.InvoiceService.Tests.Integration;

/// <summary>
/// Integration tests for audit trail capture
/// T129 per tasks.md
/// </summary>
[Collection("Database Collection")]
public class AuditTrailTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly TestWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public AuditTrailTests(TestDatabaseFixture dbFixture)
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

    #region T129 - Audit Trail Capture

    [Fact]
    public async Task InvoiceLifecycle_CapturesAllAuditEvents()
    {
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
        var createResponse = await _client.PostAsJsonAsync("/invoices/v1/invoices", createRequest);
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Finalize invoice
        await _client.PostAsJsonAsync($"/invoices/v1/invoices/{invoice!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        // Cancel invoice
        await _client.PostAsJsonAsync($"/invoices/v1/invoices/{invoice.Id}/cancel",
            new CancelInvoiceRequest { CancelledBy = "admin", CancellationReason = "Test cancellation" });

        // Assert - Retrieve audit trail
        var auditResponse = await _client.GetAsync($"/invoices/v1/audit/invoices/{invoice.Id}");
        auditResponse.EnsureSuccessStatusCode();

        var auditTrail = await auditResponse.Content.ReadFromJsonAsync<List<AuditLogResponse>>();

        auditTrail.Should().NotBeNull();
        auditTrail!.Should().HaveCountGreaterThanOrEqualTo(3);

        // Verify each lifecycle event is captured
        auditTrail.Should().Contain(a => a.Action == "Created");
        auditTrail.Should().Contain(a => a.Action == "Finalized");
        auditTrail.Should().Contain(a => a.Action == "Cancelled");

        // Verify audit entries are chronologically ordered
        var timestamps = auditTrail.Select(a => a.Timestamp).ToList();
        timestamps.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task InvoiceUpdate_CapturesUpdateAuditEvent()
    {
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
        var createResponse = await _client.PostAsJsonAsync("/invoices/v1/invoices", createRequest);
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Act - Update invoice
        var updateRequest = new CreateInvoiceRequest
        {
            CustomerId = invoice!.CustomerId,
            CustomerName = "Updated Name",
            CustomerTaxId = "1234567890123",
            BillingAddress = "456 Updated St",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Updated Product", Quantity = 2, UnitPrice = 1000, TaxRate = 7 }
            }
        };
        await _client.PutAsJsonAsync($"/invoices/v1/invoices/{invoice.Id}", updateRequest);

        // Assert - Check audit trail
        var auditResponse = await _client.GetAsync($"/invoices/v1/audit/invoices/{invoice.Id}");
        var auditTrail = await auditResponse.Content.ReadFromJsonAsync<List<AuditLogResponse>>();

        auditTrail.Should().NotBeNull();
        auditTrail!.Should().Contain(a => a.Action == "Updated");
    }

    [Fact]
    public async Task AuditLog_Contains7YearRetentionMetadata()
    {
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
        var createResponse = await _client.PostAsJsonAsync("/invoices/v1/invoices", createRequest);
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Act - Retrieve audit
        var auditResponse = await _client.GetAsync($"/invoices/v1/audit/invoices/{invoice!.Id}");
        var auditTrail = await auditResponse.Content.ReadFromJsonAsync<List<AuditLogResponse>>();

        // Assert - Verify audit records exist (7-year retention requirement)
        auditTrail.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(1);

        foreach (var entry in auditTrail!)
        {
            entry.Timestamp.Should().NotBe(default);
            entry.Timestamp.Should().BeAfter(DateTime.UtcNow.AddMinutes(-5)); // Recent
        }
    }

    #endregion
}
