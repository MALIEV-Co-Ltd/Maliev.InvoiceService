using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Maliev.InvoiceService.Api.Models.Common;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Maliev.InvoiceService.Tests.Contract;

/// <summary>
/// Contract tests verify API endpoint contracts (HTTP methods, status codes, response shapes)
/// These tests define the API surface and MUST be written FIRST per TDD principles
/// </summary>
[Collection("Database Collection")]
public class InvoiceEndpointsTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _dbFixture;
    private readonly TestWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public InvoiceEndpointsTests(TestDatabaseFixture dbFixture)
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

    #region T068 - POST /invoices/v1/invoices (create from quotation)

    [Fact]
    public async Task POST_Invoices_WithQuotationReference_Returns201Created_WithLocationHeader()
    {
        // Arrange
        var request = new CreateInvoiceRequest
        {
            QuotationReference = "QT-2025-00001",
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
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

        // Act
        var response = await _client.PostAsJsonAsync("/invoices/v1/invoices", request);

        // Assert - Contract verification
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.PathAndQuery.Should().Contain("/invoices/v1/invoices/");

        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        invoice.Should().NotBeNull();
        invoice!.Id.Should().NotBeEmpty();
        invoice.QuotationReference.Should().Be("QT-2025-00001");
        invoice.Status.Should().Be("Draft");
    }

    #endregion

    #region T069 - POST /invoices/v1/invoices/{id}/finalize

    [Fact]
    public async Task POST_InvoicesFinalize_WithDraftInvoice_Returns200OK_WithSequentialInvoiceNumber()
    {
        // Arrange - Create draft invoice first
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
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
        var draft = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Act
        var finalizeRequest = new FinalizeInvoiceRequest { FinalizedBy = "test-user" };
        var response = await _client.PostAsJsonAsync($"/invoices/v1/invoices/{draft!.Id}/finalize", finalizeRequest);

        // Assert - Contract verification
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var finalized = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        finalized.Should().NotBeNull();
        finalized!.Status.Should().Be("Finalized");
        finalized.InvoiceNumber.Should().NotBeNullOrEmpty();
        finalized.InvoiceNumber.Should().MatchRegex(@"^INV-\d{8}-\d{6}$");
        finalized.FinalizedAt.Should().NotBeNull();
        finalized.FinalizedBy.Should().Be("test-user");
    }

    #endregion

    #region T070 - GET /invoices/v1/invoices/{id}

    [Fact]
    public async Task GET_InvoicesById_WithExistingId_Returns200OK_WithFullInvoiceDetails()
    {
        // Arrange - Create invoice first
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
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
        var created = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        // Act
        var response = await _client.GetAsync($"/invoices/v1/invoices/{created!.Id}");

        // Assert - Contract verification
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        invoice.Should().NotBeNull();
        invoice!.Id.Should().Be(created.Id);
        invoice.Lines.Should().NotBeNull().And.HaveCount(1);
        invoice.Subtotal.Should().BeGreaterThan(0);
        invoice.GrandTotal.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GET_InvoicesById_WithNonExistentId_Returns404NotFound()
    {
        // Act
        var response = await _client.GetAsync($"/invoices/v1/invoices/{Guid.NewGuid()}");

        // Assert - Contract verification
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region T097 - POST /invoices/v1/invoices (create manual invoice)

    [Fact]
    public async Task POST_Invoices_WithoutQuotationReference_Returns201Created()
    {
        // Arrange
        var request = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Manual Customer",
            CustomerTaxId = "9876543210987",
            BillingAddress = "456 Manual St",
            Currency = "USD",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(45),
            PaymentTermsDays = 45,
            WithholdingTaxPercentage = 3m,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Service", Quantity = 5, UnitPrice = 200, TaxRate = 7, DiscountPercentage = 10 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/invoices/v1/invoices", request);

        // Assert - Contract verification
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        invoice.Should().NotBeNull();
        invoice!.QuotationReference.Should().BeNullOrEmpty();
        invoice.WithholdingTaxAmount.Should().BeGreaterThan(0);
        invoice.Currency.Should().Be("USD");
    }

    #endregion

    #region T105 - POST /invoices/v1/invoices/{id}/split

    [Fact]
    public async Task POST_InvoicesSplit_WithValidPercentages_Returns201Created_WithChildInvoices()
    {
        // Arrange - Create and finalize invoice
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product", Quantity = 10, UnitPrice = 1000, TaxRate = 7 }
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/invoices/v1/invoices", createRequest);
        var draft = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        await _client.PostAsJsonAsync($"/invoices/v1/invoices/{draft!.Id}/finalize", new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        // Act
        var splitRequest = new SplitInvoiceRequest
        {
            SplitRules = new List<InvoiceSplitRule>
            {
                new() { Percentage = 40m },
                new() { Percentage = 60m }
            }
        };
        var response = await _client.PostAsJsonAsync($"/invoices/v1/invoices/{draft.Id}/split", splitRequest);

        // Assert - Contract verification
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var children = await response.Content.ReadFromJsonAsync<List<InvoiceResponse>>();
        children.Should().NotBeNull().And.HaveCount(2);
        children![0].ParentInvoiceId.Should().Be(draft.Id);
        children![1].ParentInvoiceId.Should().Be(draft.Id);
    }

    [Fact]
    public async Task POST_InvoicesSplit_WithInvalidPercentages_Returns400BadRequest()
    {
        // Arrange - Create and finalize invoice
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
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
        var draft = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        await _client.PostAsJsonAsync($"/invoices/v1/invoices/{draft!.Id}/finalize", new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        // Act
        var splitRequest = new SplitInvoiceRequest
        {
            SplitRules = new List<InvoiceSplitRule>
            {
                new() { Percentage = 40m },
                new() { Percentage = 50m } // Only 90%
            }
        };
        var response = await _client.PostAsJsonAsync($"/invoices/v1/invoices/{draft.Id}/split", splitRequest);

        // Assert - Contract verification
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region T117 - GET /invoices/v1/invoices (with query parameters)

    [Fact]
    public async Task GET_Invoices_WithQueryParameters_Returns200OK_WithPaginatedResults()
    {
        // Arrange - Create some invoices
        for (int i = 0; i < 3; i++)
        {
            var request = new CreateInvoiceRequest
            {
                CustomerId = Guid.NewGuid(),
                CustomerName = $"Customer {i}",
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
            await _client.PostAsJsonAsync("/invoices/v1/invoices", request);
        }

        // Act
        var response = await _client.GetAsync("/invoices/v1/invoices?page=1&pageSize=2&status=Draft");

        // Assert - Contract verification
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<InvoiceResponse>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull().And.HaveCountLessThanOrEqualTo(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(3);
    }

    #endregion

    #region T141 - POST /invoices/v1/invoices/{id}/cancel

    [Fact]
    public async Task POST_InvoicesCancel_WithFinalizedInvoice_Returns200OK_WithCancelledStatus()
    {
        // Arrange - Create and finalize invoice
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
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
        var draft = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        await _client.PostAsJsonAsync($"/invoices/v1/invoices/{draft!.Id}/finalize", new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        // Act
        var cancelRequest = new CancelInvoiceRequest
        {
            CancelledBy = "admin-user",
            CancellationReason = "Customer requested cancellation"
        };
        var response = await _client.PostAsJsonAsync($"/invoices/v1/invoices/{draft.Id}/cancel", cancelRequest);

        // Assert - Contract verification
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancelled = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        cancelled.Should().NotBeNull();
        cancelled!.Status.Should().Be("Cancelled");
        cancelled.CancelledAt.Should().NotBeNull();
        cancelled.CancelledBy.Should().Be("admin-user");
        cancelled.CancellationReason.Should().Be("Customer requested cancellation");
    }

    #endregion

    #region T175 - GET /invoices/v1/invoices/{id} (PDF field verification)

    [Fact]
    public async Task GET_InvoicesById_FinalizedInvoice_ContainsAllPDFRequiredFields()
    {
        // Arrange - Create and finalize invoice
        var createRequest = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "PDF Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 PDF St, Bangkok 10100",
            ShippingAddress = "456 Ship St, Bangkok 10200",
            PoNumber = "PO-2025-001",
            Currency = "THB",
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new()
                {
                    LineNumber = 1,
                    ItemCode = "PROD-001",
                    Description = "Test Product",
                    Quantity = 2,
                    UnitPrice = 500,
                    TaxRate = 7,
                    TaxCategory = "Standard"
                }
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/invoices/v1/invoices", createRequest);
        var draft = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();
        await _client.PostAsJsonAsync($"/invoices/v1/invoices/{draft!.Id}/finalize", new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        // Act
        var response = await _client.GetAsync($"/invoices/v1/invoices/{draft.Id}");

        // Assert - Contract verification for PDF generation
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        invoice.Should().NotBeNull();

        // PDF Required Fields per spec
        invoice!.InvoiceNumber.Should().NotBeNullOrEmpty();
        invoice.CustomerName.Should().NotBeNullOrEmpty();
        invoice.CustomerTaxId.Should().NotBeNullOrEmpty();
        invoice.BillingAddress.Should().NotBeNullOrEmpty();
        invoice.IssueDate.Should().NotBe(default);
        invoice.DueDate.Should().NotBe(default);
        invoice.Lines.Should().NotBeNull().And.HaveCount(1);
        invoice.Lines.First().Description.Should().NotBeNullOrEmpty();
        invoice.Subtotal.Should().BeGreaterThan(0);
        invoice.TaxAmount.Should().BeGreaterThan(0);
        invoice.GrandTotal.Should().BeGreaterThan(0);
    }

    #endregion
}
