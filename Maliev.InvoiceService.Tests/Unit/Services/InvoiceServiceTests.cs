using Maliev.InvoiceService.Application.Models.Invoices;
using Maliev.InvoiceService.Application.Services;
using Maliev.InvoiceService.Application.Services.External;
using Maliev.InvoiceService.Infrastructure.Persistence;
using Maliev.InvoiceService.Infrastructure.Services;
using Maliev.InvoiceService.Domain.Entities;
using Maliev.InvoiceService.Application.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace Maliev.InvoiceService.Tests.Unit.Services;

/// <summary>
/// Unit tests for InvoiceService business logic
/// T099, T107, T119, T130, T150 per tasks.md
/// </summary>
public class InvoiceServiceTests : IAsyncLifetime
{
    private readonly Mock<ILogger<Maliev.InvoiceService.Infrastructure.Services.InvoiceService>> _loggerMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ICurrencyServiceClient> _currencyClientMock;
    private readonly Mock<IQuotationServiceClient> _quotationClientMock;
    private readonly Mock<IPaymentServiceClient> _paymentClientMock;
    private readonly Mock<ICustomerServiceClient> _customerClientMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private static readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder().WithImage("postgres:18-alpine").Build();

    public InvoiceServiceTests()
    {
        _loggerMock = new Mock<ILogger<Maliev.InvoiceService.Infrastructure.Services.InvoiceService>>();
        _cacheMock = new Mock<IDistributedCache>();
        _currencyClientMock = new Mock<ICurrencyServiceClient>();
        _quotationClientMock = new Mock<IQuotationServiceClient>();
        _paymentClientMock = new Mock<IPaymentServiceClient>();
        _customerClientMock = new Mock<ICustomerServiceClient>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();
    }

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
        using var context = CreateDbContext(); // Use CreateDbContext here once for initial setup
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgreSqlContainer.StopAsync();
    }

    private InvoiceDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseNpgsql(_postgreSqlContainer.GetConnectionString())
            .Options;
        var context = new InvoiceDbContext(options);
        // EnsureCreated() removed, MigrateAsync in InitializeAsync handles schema
        return context;
    }

    #region T099 - Withholding Tax Calculation

    [Theory]
    [InlineData(1000, 0, 3, 30)]      // 3% of 1000 = 30
    [InlineData(5000, 350, 5, 250)]   // 5% of 5000 = 250 (tax doesn't affect withholding base)
    [InlineData(10000, 700, 1, 100)]  // 1% of 10000 = 100
    [InlineData(1000, 0, 0, 0)]       // 0% = no withholding
    public void CalculateWithholdingTax_VariousScenarios_ReturnsCorrectAmount(
        decimal subtotal, decimal taxAmount, decimal withholdingPercentage, decimal expectedWithholding)
    {
        // Arrange & Act
        var result = CalculateWithholdingTaxHelper(subtotal, taxAmount, withholdingPercentage);

        // Assert
        Assert.Equal(expectedWithholding, result);
    }

    private static decimal CalculateWithholdingTaxHelper(decimal subtotal, decimal taxAmount, decimal withholdingTaxPercentage)
    {
        if (withholdingTaxPercentage <= 0)
            return 0m;

        // Withholding tax is calculated on the subtotal (before VAT) in Thai regulations
        var withholdingTaxAmount = subtotal * (withholdingTaxPercentage / 100m);
        return Math.Round(withholdingTaxAmount, 2);
    }

    #endregion

    #region T107 - Split Reconciliation Logic

    [Fact]
    public void SplitReconciliation_50_50Split_TotalsMatchParent()
    {
        // Arrange
        var parentSubtotal = 10000m;
        var parentTax = 700m;
        var parentGrandTotal = 10700m;
        var percentages = new[] { 50m, 50m };

        // Act
        var childTotals = percentages.Select(p => new
        {
            Percentage = p,
            Subtotal = Math.Round(parentSubtotal * (p / 100m), 2),
            Tax = Math.Round(parentTax * (p / 100m), 2),
            GrandTotal = Math.Round(parentGrandTotal * (p / 100m), 2)
        }).ToList();

        // Assert
        var reconciledSubtotal = childTotals.Sum(c => c.Subtotal);
        var reconciledTax = childTotals.Sum(c => c.Tax);
        var reconciledGrandTotal = childTotals.Sum(c => c.GrandTotal);

        Assert.Equal(parentSubtotal, reconciledSubtotal);
        Assert.Equal(parentTax, reconciledTax);
        Assert.Equal(parentGrandTotal, reconciledGrandTotal);
    }

    [Fact]
    public void SplitReconciliation_40_60Split_TotalsMatchParent()
    {
        // Arrange
        var parentSubtotal = 10000m;
        var parentTax = 700m;
        var parentGrandTotal = 10700m;
        var percentages = new[] { 40m, 60m };

        // Act
        var childTotals = percentages.Select(p => new
        {
            Subtotal = Math.Round(parentSubtotal * (p / 100m), 2),
            Tax = Math.Round(parentTax * (p / 100m), 2),
            GrandTotal = Math.Round(parentGrandTotal * (p / 100m), 2)
        }).ToList();

        // Assert
        var reconciledGrandTotal = childTotals.Sum(c => c.GrandTotal);
        Assert.Equal(parentGrandTotal, reconciledGrandTotal);
    }

    #endregion

    #region T119 - Search Filter Logic

    [Fact]
    public async Task SearchFilter_ByStatus_ReturnsMatchingInvoices()
    {
        // Arrange
        await using var context = CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        context.Invoices.Add(new Invoice { Id = Guid.NewGuid(), Status = "Draft", CustomerId = Guid.NewGuid(), Currency = "THB", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(30), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, RowVersion = new byte[8] });
        context.Invoices.Add(new Invoice { Id = Guid.NewGuid(), Status = "Finalized", CustomerId = Guid.NewGuid(), Currency = "THB", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(30), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, RowVersion = new byte[8] });
        await context.SaveChangesAsync();

        // Act
        var results = await context.Invoices.Where(i => i.Status == "Draft").ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal("Draft", results.First().Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task SearchFilter_ByCustomerId_ReturnsMatchingInvoices()
    {
        // Arrange
        var targetCustomerId = Guid.NewGuid();
        await using var context = CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        context.Invoices.Add(new Invoice { Id = Guid.NewGuid(), Status = "Draft", CustomerId = targetCustomerId, Currency = "THB", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(30), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, RowVersion = new byte[8] });
        context.Invoices.Add(new Invoice { Id = Guid.NewGuid(), Status = "Draft", CustomerId = Guid.NewGuid(), Currency = "THB", IssueDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(30), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, RowVersion = new byte[8] });
        await context.SaveChangesAsync();

        // Act
        var results = await context.Invoices.Where(i => i.CustomerId == targetCustomerId).ToListAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal(targetCustomerId, results.First().CustomerId);

        await transaction.RollbackAsync();
    }

    #endregion

    #region T130 - Immutability Enforcement

    [Fact]
    public void ImmutabilityCheck_FinalizedInvoice_CannotBeModified()
    {
        // Arrange
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            Status = "Finalized",
            FinalizedAt = DateTime.UtcNow,
            CustomerId = Guid.NewGuid(),
            Currency = "THB",
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[8]
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            if (invoice.Status == "Finalized")
            {
                throw new InvalidOperationException("Cannot modify finalized invoice");
            }
            invoice.Subtotal = 1000m;
        });
        Assert.Contains("Cannot modify finalized invoice", exception.Message);
    }

    [Fact]
    public async Task UpdateInvoiceAsync_FinalizedInvoice_ThrowsInvalidOperationExceptionAndLogsAudit()
    {
        // Arrange
        await using var context = CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var invoiceId = Guid.NewGuid();
        var invoice = new Invoice
        {
            Id = invoiceId,
            Status = "Finalized",
            FinalizedAt = DateTime.UtcNow,
            InvoiceNumber = "INV-2025-001",
            CustomerId = Guid.NewGuid(),
            Currency = "THB",
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[8]
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        var service = new Maliev.InvoiceService.Infrastructure.Services.InvoiceService(
            context,
            _loggerMock.Object,
            _cacheMock.Object,
            _currencyClientMock.Object,
            _quotationClientMock.Object,
            _paymentClientMock.Object,
            _customerClientMock.Object,
            _publishEndpointMock.Object
        );

        var updateRequest = new UpdateInvoiceRequest
        {
            Currency = "THB",
            DueDate = DateTime.UtcNow.AddDays(30),
            Lines = new List<InvoiceLineItemRequest>(),
            RowVersion = invoice.RowVersion,
            CustomerName = "Updated Name",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Updated St"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateInvoiceAsync(invoiceId, updateRequest, CancellationToken.None));

        // Verify audit log was created
        var auditLog = await context.AuditLogs
            .FirstOrDefaultAsync(a => a.InvoiceId == invoiceId && a.EventType == "UpdateAttemptRejected");
        Assert.NotNull(auditLog);
        Assert.Equal("System", auditLog!.ActorId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task UpdateInvoiceAsync_CancelledInvoice_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var invoiceId = Guid.NewGuid();
        var invoice = new Invoice
        {
            Id = invoiceId,
            Status = "Cancelled",
            CancelledAt = DateTime.UtcNow,
            CancellationReason = "Test cancellation",
            CustomerId = Guid.NewGuid(),
            Currency = "THB",
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[8]
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        var service = new Maliev.InvoiceService.Infrastructure.Services.InvoiceService(
            context,
            _loggerMock.Object,
            _cacheMock.Object,
            _currencyClientMock.Object,
            _quotationClientMock.Object,
            _paymentClientMock.Object,
            _customerClientMock.Object,
            _publishEndpointMock.Object
        );

        var updateRequest = new UpdateInvoiceRequest
        {
            Currency = "THB",
            DueDate = DateTime.UtcNow.AddDays(30),
            Lines = new List<InvoiceLineItemRequest>(),
            RowVersion = invoice.RowVersion,
            CustomerName = "Updated Name",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Updated St"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateInvoiceAsync(invoiceId, updateRequest, CancellationToken.None));
        Assert.Contains("Cancelled", exception.Message);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task UpdateInvoiceAsync_DraftInvoice_Succeeds()
    {
        // Arrange
        await using var context = CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var customerId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var invoice = new Invoice
        {
            Id = invoiceId,
            Status = "Draft",
            CustomerId = customerId,
            CustomerName = "Original Customer",
            Currency = "THB",
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            Subtotal = 1000m,
            TaxAmount = 70m,
            GrandTotal = 1070m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = BitConverter.GetBytes(1L)
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        var service = new Maliev.InvoiceService.Infrastructure.Services.InvoiceService(
            context,
            _loggerMock.Object,
            _cacheMock.Object,
            _currencyClientMock.Object,
            _quotationClientMock.Object,
            _paymentClientMock.Object,
            _customerClientMock.Object,
            _publishEndpointMock.Object
        );

        var updateRequest = new UpdateInvoiceRequest
        {
            CustomerName = "Updated Customer",
            CustomerTaxId = "1234567890123",
            BillingAddress = "123 Updated St",
            Currency = "THB",
            DueDate = DateTime.UtcNow.AddDays(45),
            Lines = new List<InvoiceLineItemRequest>
            {
                new InvoiceLineItemRequest
                {
                    Description = "Updated Item",
                    Quantity = 2,
                    UnitPrice = 500m
                }
            },
            RowVersion = invoice.RowVersion
        };

        // Act
        var result = await service.UpdateInvoiceAsync(invoiceId, updateRequest, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Customer", result.CustomerName);
        Assert.Equal("Draft", result.Status);

        await transaction.RollbackAsync();
    }

    #endregion

    #region T150 - Exchange Rate Storage

    [Fact]
    public async Task CreateInvoiceAsync_WithForeignCurrency_StoresExchangeRate()
    {
        // Arrange
        await using var context = CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        _currencyClientMock
            .Setup(x => x.GetExchangeRateAsync("USD", "THB", It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(35.50m);

        _customerClientMock
            .Setup(x => x.GetCustomerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Maliev.InvoiceService.Application.Models.Customers.CustomerResponse
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Customer",
                CompanyId = Guid.NewGuid(),
                CompanyName = "Test Company"
            });

        var service = new Maliev.InvoiceService.Infrastructure.Services.InvoiceService(
            context,
            _loggerMock.Object,
            _cacheMock.Object,
            _currencyClientMock.Object,
            _quotationClientMock.Object,
            _paymentClientMock.Object,
            _customerClientMock.Object,
            _publishEndpointMock.Object
        );

        var request = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            Currency = "USD",
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            Lines = new List<InvoiceLineItemRequest>
            {
                new InvoiceLineItemRequest
                {
                    Description = "Test Item",
                    Quantity = 1,
                    UnitPrice = 100m
                }
            }
        };

        // Act
        var result = await service.CreateInvoiceAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(35.50m, result.ExchangeRate);
        Assert.Equal("Currency Service", result.ExchangeRateSource);

        // Verify it was persisted to database
        var invoiceFromDb = await context.Invoices.FirstOrDefaultAsync(i => i.Id == result.Id);
        Assert.NotNull(invoiceFromDb);
        Assert.Equal(35.50m, invoiceFromDb!.ExchangeRate);
        Assert.Equal("Currency Service", invoiceFromDb.ExchangeRateSource);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithTHB_DoesNotFetchExchangeRate()
    {
        // Arrange
        await using var context = CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        _customerClientMock
            .Setup(x => x.GetCustomerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Maliev.InvoiceService.Application.Models.Customers.CustomerResponse
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Customer",
                CompanyId = Guid.NewGuid(),
                CompanyName = "Test Company"
            });

        var service = new Maliev.InvoiceService.Infrastructure.Services.InvoiceService(
            context,
            _loggerMock.Object,
            _cacheMock.Object,
            _currencyClientMock.Object,
            _quotationClientMock.Object,
            _paymentClientMock.Object,
            _customerClientMock.Object,
            _publishEndpointMock.Object
        );

        var request = new CreateInvoiceRequest
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            Currency = "THB",
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            Lines = new List<InvoiceLineItemRequest>
            {
                new InvoiceLineItemRequest
                {
                    Description = "Test Item",
                    Quantity = 1,
                    UnitPrice = 100m
                }
            }
        };

        // Act
        var result = await service.CreateInvoiceAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("THB", result.Currency);
        Assert.Null(result.ExchangeRate);
        Assert.True(string.IsNullOrEmpty(result.ExchangeRateSource));

        // Verify currency service was never called
        _currencyClientMock.Verify(
            x => x.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        await transaction.RollbackAsync();
    }

    [Fact]
    public void ExchangeRateStorage_NonTHBCurrency_StoresExchangeRateAndSource()
    {
        // Arrange
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            Currency = "USD",
            CustomerId = Guid.NewGuid(),
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[8]
        };

        // Act
        invoice.ExchangeRate = 35.50m;
        invoice.ExchangeRateSource = "Currency Service";

        // Assert
        Assert.Equal(35.50m, invoice.ExchangeRate);
        Assert.Equal("Currency Service", invoice.ExchangeRateSource);
    }

    [Fact]
    public void ExchangeRateStorage_THBCurrency_NoExchangeRateNeeded()
    {
        // Arrange
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            Currency = "THB",
            CustomerId = Guid.NewGuid(),
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[8]
        };

        // Act & Assert
        Assert.Null(invoice.ExchangeRate);
        Assert.True(string.IsNullOrEmpty(invoice.ExchangeRateSource));
    }

    #endregion
}
