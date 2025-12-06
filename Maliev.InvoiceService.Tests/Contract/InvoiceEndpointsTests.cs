using System.Net;
using System.Net.Http.Json;
using Maliev.InvoiceService.Api.Models.Common;
using Maliev.InvoiceService.Api.Models.Invoices;
using Maliev.InvoiceService.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.RegularExpressions;

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
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/invoices/v1/invoices/", response.Headers.Location!.PathAndQuery);

        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(invoice);
        Assert.NotEqual(Guid.Empty, invoice!.Id);
        Assert.Equal("QT-2025-00001", invoice.QuotationReference);
        Assert.Equal("Draft", invoice.Status);
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
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var finalized = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(finalized);
        Assert.Equal("Finalized", finalized!.Status);
        Assert.False(string.IsNullOrEmpty(finalized.InvoiceNumber));
        Assert.Matches(@"^INV-\d{8}-\d{6}$", finalized.InvoiceNumber);
        Assert.NotNull(finalized.FinalizedAt);
        Assert.Equal("test-user", finalized.FinalizedBy);
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
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(invoice);
        Assert.Equal(created.Id, invoice!.Id);
        Assert.NotNull(invoice.Lines);
        Assert.Single(invoice.Lines);
        Assert.True(invoice.Subtotal > 0);
        Assert.True(invoice.GrandTotal > 0);
    }

    [Fact]
    public async Task GET_InvoicesById_WithNonExistentId_Returns404NotFound()
    {
        // Act
        var response = await _client.GetAsync($"/invoices/v1/invoices/{Guid.NewGuid()}");

        // Assert - Contract verification
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(invoice);
        Assert.True(string.IsNullOrEmpty(invoice!.QuotationReference));
        Assert.True(invoice.WithholdingTaxAmount > 0);
        Assert.Equal("USD", invoice.Currency);
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
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var children = await response.Content.ReadFromJsonAsync<List<InvoiceResponse>>();
        Assert.NotNull(children);
        Assert.Equal(2, children!.Count);
        Assert.Equal(draft.Id, children[0].ParentInvoiceId);
        Assert.Equal(draft.Id, children[1].ParentInvoiceId);
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
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<InvoiceResponse>>();
        Assert.NotNull(result);
        Assert.NotNull(result!.Items);
        Assert.True(result.Items.Count() <= 2);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.True(result.TotalCount >= 3);
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
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cancelled = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(cancelled);
        Assert.Equal("Cancelled", cancelled!.Status);
        Assert.NotNull(cancelled.CancelledAt);
        Assert.Equal("admin-user", cancelled.CancelledBy);
        Assert.Equal("Customer requested cancellation", cancelled.CancellationReason);
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
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        Assert.NotNull(invoice);

        // PDF Required Fields per spec
        Assert.False(string.IsNullOrEmpty(invoice!.InvoiceNumber));
        Assert.False(string.IsNullOrEmpty(invoice.CustomerName));
        Assert.False(string.IsNullOrEmpty(invoice.CustomerTaxId));
        Assert.False(string.IsNullOrEmpty(invoice.BillingAddress));
        Assert.NotEqual(default, invoice.IssueDate);
        Assert.NotEqual(default, invoice.DueDate);
        Assert.NotNull(invoice.Lines);
        Assert.Single(invoice.Lines);
        Assert.False(string.IsNullOrEmpty(invoice.Lines.First().Description));
        Assert.True(invoice.Subtotal > 0);
        Assert.True(invoice.TaxAmount > 0);
        Assert.True(invoice.GrandTotal > 0);
    }

    #endregion
}
