using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Maliev.InvoiceService.Api.Models.Audit;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Maliev.InvoiceService.Tests.Contract;

/// <summary>
/// Contract tests for Audit endpoints
/// T128 per tasks.md
/// </summary>
[Collection("Database Collection")]
public class AuditEndpointsTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly TestWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public AuditEndpointsTests(TestDatabaseFixture dbFixture)
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

    #region T128 - GET /invoices/v1/audit/invoices/{id}

    [Fact]
    public async Task GET_AuditInvoicesById_WithInvoiceHistory_Returns200OK_WithAuditTrail()
    {
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
        var createResponse = await _client.PostAsJsonAsync("/invoices/v1/invoices", createRequest);
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Finalize to create another audit entry
        await _client.PostAsJsonAsync($"/invoices/v1/invoices/{invoice!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        // Act
        var response = await _client.GetAsync($"/invoices/v1/audit/invoices/{invoice.Id}");

        // Assert - Contract verification
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var auditTrail = await response.Content.ReadFromJsonAsync<List<AuditLogResponse>>();
        auditTrail.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(2);

        // Verify audit entries contain expected fields
        auditTrail!.Should().Contain(a => a.Action == "Created");
        auditTrail.Should().Contain(a => a.Action == "Finalized");

        foreach (var entry in auditTrail)
        {
            entry.Id.Should().NotBeEmpty();
            entry.EntityType.Should().Be("Invoice");
            entry.EntityId.Should().Be(invoice.Id);
            entry.Action.Should().NotBeNullOrEmpty();
            entry.Timestamp.Should().NotBe(default);
        }
    }

    [Fact]
    public async Task GET_AuditInvoicesById_WithNonExistentInvoice_Returns404NotFound()
    {
        // Act
        var response = await _client.GetAsync($"/invoices/v1/audit/invoices/{Guid.NewGuid()}");

        // Assert - Contract verification
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
