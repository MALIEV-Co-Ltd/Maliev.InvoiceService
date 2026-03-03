using Maliev.InvoiceService.Application.Models.Invoices;
using Maliev.InvoiceService.Application.Models.Payments;
using Maliev.InvoiceService.Application.Models.Common;
using Maliev.InvoiceService.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;

namespace Maliev.InvoiceService.Tests.Integration;

public class InvoiceUpdateDeleteTests : BaseIntegrationTest
{
    public InvoiceUpdateDeleteTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task UpdateInvoice_WithValidData_ReturnsUpdated()
    {
        await CleanDatabaseAsync();

        var invoiceId = await CreateDraftInvoiceAsync();

        var updateRequest = new UpdateInvoiceRequest
        {
            CustomerName = "Updated Customer",
            CustomerTaxId = "9876543210987",
            BillingAddress = "456 New St, Bangkok",
            ShippingAddress = "456 New St, Bangkok",
            PoNumber = "PO-2026-001",
            DueDate = DateTime.UtcNow.Date.AddDays(45),
            PaymentTermsDays = 45,
            LateFeePercentage = 1.5m,
            RowVersion = await GetInvoiceRowVersionAsync(invoiceId),
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Updated Product", Quantity = 5, UnitPrice = 200, TaxRate = 7 }
            }
        };

        var response = await Client.PutAsJsonAsync($"/invoice/v1/invoices/{invoiceId}", updateRequest);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Update failed: {response.StatusCode} - {error}");
        }

        response.EnsureSuccessStatusCode();
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();

        Assert.NotNull(invoice);
        Assert.Equal("Updated Customer", invoice!.CustomerName);
        Assert.Equal("PO-2026-001", invoice.PoNumber);
    }

    [Fact]
    public async Task UpdateInvoice_NonDraftStatus_ReturnsBadRequest()
    {
        await CleanDatabaseAsync();

        var invoiceId = await CreateAndFinalizeInvoiceAsync(grandTotal: 1000m);

        var updateRequest = new UpdateInvoiceRequest
        {
            CustomerName = "Updated Customer",
            CustomerTaxId = "9876543210987",
            BillingAddress = "456 New St",
            RowVersion = await GetInvoiceRowVersionAsync(invoiceId),
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = 1000, TaxRate = 7 }
            }
        };

        var response = await Client.PutAsJsonAsync($"/invoice/v1/invoices/{invoiceId}", updateRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteInvoice_DraftStatus_ReturnsNoContent()
    {
        await CleanDatabaseAsync();

        var invoiceId = await CreateDraftInvoiceAsync();

        var response = await Client.DeleteAsync($"/invoice/v1/invoices/{invoiceId}");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteInvoice_FinalizedStatus_ReturnsBadRequest()
    {
        await CleanDatabaseAsync();

        var invoiceId = await CreateAndFinalizeInvoiceAsync(grandTotal: 1000m);

        var response = await Client.DeleteAsync($"/invoice/v1/invoices/{invoiceId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> CreateDraftInvoiceAsync()
    {
        var request = new CreateInvoiceRequest
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

        var response = await Client.PostAsJsonAsync("/invoice/v1/invoices", request);
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        return invoice!.Id;
    }

    private async Task<Guid> CreateAndFinalizeInvoiceAsync(decimal grandTotal)
    {
        var request = new CreateInvoiceRequest
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
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = grandTotal / 1.07m, TaxRate = 7 }
            }
        };

        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", request);
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        var finalizeResponse = await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        return invoice.Id;
    }

    private async Task<byte[]> GetInvoiceRowVersionAsync(Guid invoiceId)
    {
        var response = await Client.GetAsync($"/invoice/v1/invoices/{invoiceId}");
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        return invoice!.RowVersion;
    }
}

public class InvoiceExportTests : BaseIntegrationTest
{
    public InvoiceExportTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ExportInvoices_AsCsv_ReturnsCsvContent()
    {
        await CleanDatabaseAsync();

        await CreateDraftInvoiceAsync();
        await CreateDraftInvoiceAsync();

        var response = await Client.GetAsync("/invoice/v1/invoices/export?format=csv");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        Assert.NotEmpty(content);
        Assert.Contains("Id,InvoiceNumber,CustomerId", content);
    }

    [Fact]
    public async Task ExportInvoices_AsJson_ReturnsJsonContent()
    {
        await CleanDatabaseAsync();

        await CreateDraftInvoiceAsync();

        var response = await Client.GetAsync("/invoice/v1/invoices/export?format=json");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        Assert.NotEmpty(content);
        Assert.StartsWith("[", content.Trim());
    }

    [Fact]
    public async Task ExportInvoices_Limit1000_ReturnsResults()
    {
        await CleanDatabaseAsync();

        for (int i = 0; i < 5; i++)
        {
            await CreateDraftInvoiceAsync();
        }

        var response = await Client.GetAsync("/invoice/v1/invoices/export?format=json&pageSize=1000");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        Assert.NotNull(content);
    }

    private async Task CreateDraftInvoiceAsync()
    {
        var request = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Export Customer",
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

        await Client.PostAsJsonAsync("/invoice/v1/invoices", request);
    }
}

public class PaymentRetrievalTests : BaseIntegrationTest
{
    public PaymentRetrievalTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetPaymentById_ExistingPayment_ReturnsPayment()
    {
        await CleanDatabaseAsync();

        var paymentRequest = new CreatePaymentRequest
        {
            PaymentAmount = 1000m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Cash",
            RecordedBy = "cashier"
        };

        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/payments", paymentRequest);
        var payment = await createResponse.Content.ReadFromJsonAsync<PaymentResponse>();

        var getResponse = await Client.GetAsync($"/invoice/v1/payments/{payment!.Id}");

        getResponse.EnsureSuccessStatusCode();
        var retrievedPayment = await getResponse.Content.ReadFromJsonAsync<PaymentResponse>();

        Assert.NotNull(retrievedPayment);
        Assert.Equal(payment.Id, retrievedPayment!.Id);
        Assert.Equal(1000m, retrievedPayment.PaymentAmount);
    }

    [Fact]
    public async Task GetPaymentById_NonExistingPayment_ReturnsNotFound()
    {
        await CleanDatabaseAsync();

        var response = await Client.GetAsync($"/invoice/v1/payments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class FileReferenceRetrievalTests : BaseIntegrationTest
{
    public FileReferenceRetrievalTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetFileReferences_WithFiles_ReturnsFileList()
    {
        await CleanDatabaseAsync();

        var invoiceId = await CreateAndFinalizeInvoiceAsync(grandTotal: 1000m);

        var fileRequest = new RegisterFileRequest
        {
            FileType = "PDF",
            FileUrl = "https://storage.example.com/invoice.pdf",
            FileSizeBytes = 1024,
            GeneratedBy = "system"
        };

        await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoiceId}/files", fileRequest);

        var response = await Client.GetAsync($"/invoice/v1/invoices/{invoiceId}/files");

        response.EnsureSuccessStatusCode();
        var files = await response.Content.ReadFromJsonAsync<List<FileReferenceResponse>>();

        Assert.NotNull(files);
        Assert.Single(files);
    }

    [Fact]
    public async Task GetFileReferences_NoFiles_ReturnsEmptyList()
    {
        await CleanDatabaseAsync();

        var invoiceId = await CreateAndFinalizeInvoiceAsync(grandTotal: 1000m);

        var response = await Client.GetAsync($"/invoice/v1/invoices/{invoiceId}/files");

        response.EnsureSuccessStatusCode();
        var files = await response.Content.ReadFromJsonAsync<List<FileReferenceResponse>>();

        Assert.NotNull(files);
        Assert.Empty(files);
    }

    private async Task<Guid> CreateAndFinalizeInvoiceAsync(decimal grandTotal)
    {
        var request = new CreateInvoiceRequest
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
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = grandTotal / 1.07m, TaxRate = 7 }
            }
        };

        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", request);
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        return invoice.Id;
    }
}

public class PdfFileReferenceTests : BaseIntegrationTest
{
    public PdfFileReferenceTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task RegisterPdfFileReference_FinalizedInvoice_ReturnsNoContent()
    {
        await CleanDatabaseAsync();

        var invoiceId = await CreateAndFinalizedInvoiceForPdfAsync();

        var request = new RegisterPdfFileReferenceRequest
        {
            PdfFileReference = "https://storage.example.com/invoice.pdf"
        };

        var response = await Client.PatchAsJsonAsync($"/invoice/v1/invoices/{invoiceId}/pdf-reference", request);

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RegisterPdfFileReference_DraftInvoice_ReturnsBadRequest()
    {
        await CleanDatabaseAsync();

        var invoiceId = await CreateDraftInvoiceAsync();

        var request = new RegisterPdfFileReferenceRequest
        {
            PdfFileReference = "https://storage.example.com/invoice.pdf"
        };

        var response = await Client.PatchAsJsonAsync($"/invoice/v1/invoices/{invoiceId}/pdf-reference", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> CreateDraftInvoiceAsync()
    {
        var request = new CreateInvoiceRequest
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

        var response = await Client.PostAsJsonAsync("/invoice/v1/invoices", request);
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();
        return invoice!.Id;
    }

    private async Task<Guid> CreateAndFinalizedInvoiceForPdfAsync()
    {
        var request = new CreateInvoiceRequest
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

        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", request);
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        return invoice.Id;
    }
}

public class CurrencyConversionReportTests : BaseIntegrationTest
{
    public CurrencyConversionReportTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetCurrencyConversionReport_UsdInvoice_ReturnsReport()
    {
        await CleanDatabaseAsync();

        var request = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Test St",
            Currency = "USD",
            ManualExchangeRate = 35.5m,
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            PaymentTermsDays = 30,
            Lines = new List<InvoiceLineItemRequest>
            {
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = 100, TaxRate = 0 }
            }
        };

        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", request);
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        var response = await Client.GetAsync($"/invoice/v1/invoices/{invoice.Id}/currency-conversion");

        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        Assert.NotNull(report);
        Assert.Contains("OriginalCurrency", report.Keys);
        Assert.Contains("ExchangeRate", report.Keys);
    }

    [Fact]
    public async Task GetCurrencyConversionReport_ThbInvoice_ReturnsReport()
    {
        await CleanDatabaseAsync();

        var invoiceId = await CreateAndFinalizeInvoiceAsync(grandTotal: 1070m);

        var response = await Client.GetAsync($"/invoice/v1/invoices/{invoiceId}/currency-conversion");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var report = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(content);

        Assert.NotNull(report);
        var originalCurrency = report["OriginalCurrency"]?.ToString();
        Assert.Equal("THB", originalCurrency);
    }

    private async Task<Guid> CreateAndFinalizeInvoiceAsync(decimal grandTotal)
    {
        var request = new CreateInvoiceRequest
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
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = grandTotal / 1.07m, TaxRate = 7 }
            }
        };

        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", request);
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        return invoice.Id;
    }
}

public class AnalyticsSummaryTests : BaseIntegrationTest
{
    public AnalyticsSummaryTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAnalyticsSummary_WithData_ReturnsSummary()
    {
        await CleanDatabaseAsync();

        await CreateAndFinalizeInvoiceAsync(grandTotal: 1000m);
        await CreateAndFinalizeInvoiceAsync(grandTotal: 2000m);

        var response = await Client.GetAsync("/invoice/v1/analytics/summary");

        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        Assert.NotNull(summary);
        Assert.Contains("InvoiceCountsByStatus", summary.Keys);
    }

    [Fact]
    public async Task GetAnalyticsSummary_WithDateRange_ReturnsFilteredSummary()
    {
        await CleanDatabaseAsync();

        await CreateAndFinalizeInvoiceAsync(grandTotal: 1000m);

        var response = await Client.GetAsync("/invoice/v1/analytics/summary");

        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        Assert.NotNull(summary);
    }

    private async Task<Guid> CreateAndFinalizeInvoiceAsync(decimal grandTotal)
    {
        var request = new CreateInvoiceRequest
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
                new() { LineNumber = 1, Description = "Product", Quantity = 1, UnitPrice = grandTotal / 1.07m, TaxRate = 7 }
            }
        };

        var createResponse = await Client.PostAsJsonAsync("/invoice/v1/invoices", request);
        var invoice = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>();

        await Client.PostAsJsonAsync($"/invoice/v1/invoices/{invoice!.Id}/finalize",
            new FinalizeInvoiceRequest { FinalizedBy = "test-user" });

        return invoice.Id;
    }
}

public class BillingIdentityTests : BaseIntegrationTest
{
    public BillingIdentityTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateInvoice_CorporateBillingIdentity_ReturnsCreated()
    {
        await CleanDatabaseAsync();

        var request = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            BillingIdentityType = BillingIdentityType.Corporate,
            CustomerName = "Test Corp",
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

        var response = await Client.PostAsJsonAsync("/invoice/v1/invoices", request);

        response.EnsureSuccessStatusCode();
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceResponse>();

        Assert.NotNull(invoice);
    }
}
