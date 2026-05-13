using Maliev.InvoiceService.Application.Models.Audit;
using Maliev.InvoiceService.Application.Models.Common;
using Maliev.InvoiceService.Application.Models.Customers;
using Maliev.InvoiceService.Application.Models.Invoices;
using Maliev.InvoiceService.Application.Models.Payments;
using Maliev.InvoiceService.Application.Services;
using Maliev.InvoiceService.Application.Services.External;
using Maliev.InvoiceService.Infrastructure.Persistence;
using Maliev.InvoiceService.Infrastructure.Search;
using Maliev.InvoiceService.Domain.Entities;
using Maliev.MessagingContracts;
using Maliev.MessagingContracts.Contracts.Invoices;
using Maliev.MessagingContracts.Contracts.Payments;
using Maliev.MessagingContracts.Contracts.Pdf;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace Maliev.InvoiceService.Infrastructure.Services;

/// <summary>
/// Service implementation for managing invoices, payments, file references, and audit trails.
/// Provides comprehensive business logic for invoice lifecycle management with:
/// - Currency conversion via CurrencyService
/// - Quotation-to-invoice generation via QuotationService
/// - Withholding tax (WHT) calculations
/// - Payment allocation and status tracking
/// - Redis distributed caching for performance
/// - Automatic audit logging via EF Core interceptors
/// - PostgreSQL full-text search and JSON operators
/// </summary>
public class InvoiceService : IInvoiceService
{
    private readonly InvoiceDbContext _context;
    private readonly ILogger<InvoiceService> _logger;
    private readonly IDistributedCache _cache;
    private readonly ICurrencyServiceClient _currencyClient;
    private readonly IQuotationServiceClient _quotationClient;
    private readonly IPaymentServiceClient _paymentClient;
    private readonly ICustomerServiceClient _customerClient;
    private readonly IPublishEndpoint _publishEndpoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvoiceService"/> class.
    /// </summary>
    /// <param name="context">Database context for invoice data access.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="cache">Distributed cache for performance optimization.</param>
    /// <param name="currencyClient">Client for retrieving exchange rates from Currency Service.</param>
    /// <param name="quotationClient">Client for retrieving quotation data from Quotation Service.</param>
    /// <param name="paymentClient">Client for retrieving payment details from Payment Service.</param>
    /// <param name="customerClient">Client for retrieving customer data from Customer Service.</param>
    /// <param name="publishEndpoint">MassTransit publish endpoint for publishing events.</param>
    public InvoiceService(
        InvoiceDbContext context,
        ILogger<InvoiceService> logger,
        IDistributedCache cache,
        ICurrencyServiceClient currencyClient,
        IQuotationServiceClient quotationClient,
        IPaymentServiceClient paymentClient,
        ICustomerServiceClient customerClient,
        IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
        _currencyClient = currencyClient;
        _quotationClient = quotationClient;
        _paymentClient = paymentClient;
        _customerClient = customerClient;
        _publishEndpoint = publishEndpoint;
    }

    /// <inheritdoc/>
    public async Task<InvoiceResponse> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        // If quotation reference is provided, fetch quotation data
        if (!string.IsNullOrEmpty(request.QuotationReference))
        {
            try
            {
                var quotation = await _quotationClient.GetQuotationAsync(request.QuotationReference, cancellationToken);
                if (quotation != null)
                {
                    _logger.LogInformation("Populating invoice from quotation {QuotationReference}", request.QuotationReference);
                    // Pre-populate from quotation if fields are empty
                    request.CustomerId = quotation.CustomerId;
                    request.CustomerName = quotation.CustomerName;
                    request.CustomerTaxId = quotation.CustomerTaxId;
                    request.BillingAddress = quotation.BillingAddress;
                    request.ShippingAddress ??= quotation.ShippingAddress;
                    request.Currency = quotation.Currency;
                    request.PaymentTermsDays = quotation.PaymentTermsDays;

                    // Map quotation lines if request has no lines
                    if (request.Lines.Count == 0)
                    {
                        request.Lines = quotation.Lines.Select(ql => new InvoiceLineItemRequest
                        {
                            LineNumber = ql.LineNumber,
                            ItemCode = ql.ItemCode,
                            Description = ql.Description,
                            Quantity = ql.Quantity,
                            UnitPrice = ql.UnitPrice,
                            DiscountPercentage = ql.DiscountPercentage,
                            TaxCategory = ql.TaxCategory,
                            TaxRate = ql.TaxRate
                        }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch quotation {QuotationReference}", request.QuotationReference);
            }
        }

        // Validate billing identity and fetch customer data
        var customer = await _customerClient.GetCustomerByIdAsync(request.CustomerId, cancellationToken);
        if (customer == null)
        {
            throw new ValidationException($"Customer {request.CustomerId} not found");
        }

        string billingName;
        string billingTaxId;

        if (request.BillingIdentityType == BillingIdentityType.Personal)
        {
            if (string.IsNullOrEmpty(customer.ThaiNationalIdMasked))
            {
                throw new ValidationException("Customer does not have a Thai National ID. Cannot create invoice with personal billing identity.");
            }

            billingName = $"{customer.FirstName} {customer.LastName}";
            // Note: We use the masked ID from the API response since full ID is not exposed for security
            // The actual Thai National ID will be fetched from CustomerService for PDF generation
            billingTaxId = "PersonalID"; // Placeholder - will be resolved during PDF generation
            _logger.LogInformation("Creating invoice with Personal billing identity for customer {CustomerId}", request.CustomerId);
        }
        else // Corporate
        {
            if (customer.CompanyId == null || string.IsNullOrEmpty(customer.CompanyName))
            {
                throw new ValidationException("Customer does not have a linked company. Cannot create invoice with corporate billing identity.");
            }

            billingName = customer.CompanyName;
            billingTaxId = request.CustomerTaxId; // Company tax ID should come from request or be fetched
            _logger.LogInformation("Creating invoice with Corporate billing identity for customer {CustomerId}", request.CustomerId);
        }

        // Override request values with validated billing identity
        request.CustomerName = billingName;
        request.CustomerTaxId = billingTaxId;

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            QuotationReference = request.QuotationReference,
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            CustomerTaxId = request.CustomerTaxId,
            BillingAddress = request.BillingAddress,
            ShippingAddress = request.ShippingAddress,
            PoNumber = request.PoNumber,
            DocumentType = request.DocumentType,
            Currency = request.Currency,
            IssueDate = request.IssueDate.Date,
            DueDate = request.DueDate.Date,
            PaymentTermsDays = request.PaymentTermsDays,
            CreditTermCode = request.CreditTermCode,
            LateFeePercentage = request.LateFeePercentage,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // T154: Handle exchange rate - use manual rate if provided, otherwise fetch from service
        if (invoice.Currency != "THB")
        {
            if (request.ManualExchangeRate.HasValue)
            {
                // Use manually provided exchange rate
                invoice.ExchangeRate = request.ManualExchangeRate.Value;
                invoice.ExchangeRateSource = "Manual Entry";
                _logger.LogInformation("Using manual exchange rate {Rate} for {FromCurrency} to THB",
                    invoice.ExchangeRate, invoice.Currency);
            }
            else
            {
                // Fetch from currency service
                try
                {
                    invoice.ExchangeRate = await _currencyClient.GetExchangeRateAsync(
                        invoice.Currency, "THB", DateTime.UtcNow, cancellationToken);
                    invoice.ExchangeRateSource = "Currency Service";
                    _logger.LogInformation("Retrieved exchange rate {Rate} for {FromCurrency} to THB",
                        invoice.ExchangeRate, invoice.Currency);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch exchange rate for {Currency}. Invoice can still be created but will need exchange rate before finalization.", invoice.Currency);
                    // Invoice can still be created without exchange rate for now
                    // It will be required during finalization
                }
            }
        }

        decimal subtotal = 0;
        decimal totalTax = 0;

        foreach (var lineRequest in request.Lines)
        {
            var lineSubtotal = (lineRequest.Quantity * lineRequest.UnitPrice) * (1 - lineRequest.DiscountPercentage / 100);
            var lineTax = lineSubtotal * (lineRequest.TaxRate / 100);

            var line = new InvoiceLine
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                LineNumber = lineRequest.LineNumber,
                ItemCode = lineRequest.ItemCode,
                Description = lineRequest.Description,
                Quantity = lineRequest.Quantity,
                UnitPrice = lineRequest.UnitPrice,
                DiscountPercentage = lineRequest.DiscountPercentage,
                TaxCategory = lineRequest.TaxCategory,
                TaxRate = lineRequest.TaxRate,
                LineSubtotal = lineSubtotal,
                TaxAmount = lineTax,
                LineTotal = lineSubtotal + lineTax,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            subtotal += lineSubtotal;
            totalTax += lineTax;

            invoice.Lines.Add(line);
        }

        invoice.Subtotal = subtotal;
        invoice.TaxAmount = totalTax;

        // Calculate withholding tax
        invoice.WithholdingTaxAmount = CalculateWithholdingTax(subtotal, totalTax, request.WithholdingTaxPercentage);

        invoice.GrandTotal = subtotal + totalTax - invoice.WithholdingTaxAmount;

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        // Record metrics
        InvoiceMetrics.RecordInvoiceCreated("Draft");

        // Invalidate cache (in case customer-specific lists are cached)
        await _cache.RemoveAsync($"invoice:{invoice.Id}", cancellationToken);

        _logger.LogInformation("Created invoice {InvoiceId} for customer {CustomerId}", invoice.Id, invoice.CustomerId);

        // Publish InvoiceCreatedEvent
        await _publishEndpoint.Publish(new InvoiceCreatedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(InvoiceCreatedEvent),
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "InvoiceService",
            ConsumedBy: new List<string> { "NotificationService", "AnalyticsService" },
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new InvoiceCreatedEventPayload(
                InvoiceId: invoice.Id,
                InvoiceNumber: invoice.InvoiceNumber ?? "DRAFT",
                OrderId: null, // Will be set by consumers if linked
                OrderNumber: null,
                CustomerId: invoice.CustomerId,
                TotalAmount: (double)invoice.GrandTotal,
                Currency: invoice.Currency,
                DueDate: invoice.DueDate != default ? new DateTimeOffset(invoice.DueDate, TimeSpan.Zero) : null,
                CreatedAt: new DateTimeOffset(invoice.CreatedAt, TimeSpan.Zero)
            )
        ), cancellationToken);

        await _publishEndpoint.Publish(
            InvoiceSearchDocumentMapper.ToUpsertEvent(invoice, DateTimeOffset.UtcNow),
            cancellationToken);

        return MapToResponse(invoice);
    }

    /// <inheritdoc/>
    public async Task<InvoiceResponse?> GetInvoiceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Try to get from cache first
        var cacheKey = $"invoice:{id}";
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (!string.IsNullOrEmpty(cachedData))
        {
            _logger.LogDebug("Retrieved invoice {InvoiceId} from cache", id);
            return JsonSerializer.Deserialize<InvoiceResponse>(cachedData);
        }

        var invoice = await _context.Invoices
            .Include(i => i.Lines)
            .Include(i => i.ChildInvoices) // Include child invoices
            .Include(i => i.AuditLogs)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);

        if (invoice == null)
            return null;

        var response = MapToResponse(invoice);

        // Cache finalized invoices for 24 hours (immutable)
        if (invoice.Status == "Finalized")
        {
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            };

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(response),
                cacheOptions,
                cancellationToken);

            _logger.LogDebug("Cached finalized invoice {InvoiceId} for 24 hours", id);
        }

        return response;
    }

    /// <inheritdoc/>
    public async Task<PaginatedResponse<InvoiceResponse>> GetPaginatedInvoicesAsync(
        int page, int pageSize, string? status = null, Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Invoices
            .Include(i => i.Lines)
            .Where(i => !i.IsDeleted)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(i => i.Status == status);

        if (customerId.HasValue)
            query = query.Where(i => i.CustomerId == customerId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var invoices = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<InvoiceResponse>
        {
            Items = invoices.Select(MapToResponse),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc/>
    public async Task<PaginatedResponse<InvoiceResponse>> SearchInvoicesAsync(InvoiceSearchRequest request, CancellationToken cancellationToken = default)
    {
        return await SearchInvoicesAsync(request, new InvoiceAccessScope(string.Empty, RestrictToCreatedInvoices: false), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PaginatedResponse<InvoiceResponse>> SearchInvoicesAsync(
        InvoiceSearchRequest request,
        InvoiceAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        // Try to get from cache first (5-minute TTL)
        var cacheKey = $"invoice:search:{accessScope.CacheKey}:{JsonSerializer.Serialize(request)}";
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (!string.IsNullOrEmpty(cachedData))
        {
            _logger.LogDebug("Retrieved search results from cache for key {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<PaginatedResponse<InvoiceResponse>>(cachedData)!;
        }

        var query = _context.Invoices
            .Include(i => i.Lines)
            .Where(i => !i.IsDeleted)
            .AsNoTracking();

        query = ApplyAccessScope(query, accessScope);

        // Exclude cancelled invoices by default (T124)
        if (!request.IncludeCancelled)
        {
            query = query.Where(i => i.Status != "Cancelled");
        }

        // Apply filters
        if (!string.IsNullOrEmpty(request.CustomerName))
        {
            query = query.Where(i => i.CustomerName.Contains(request.CustomerName));
        }

        if (request.CustomerId.HasValue)
        {
            query = query.Where(i => i.CustomerId == request.CustomerId.Value);
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            query = query.Where(i => i.Status == request.Status);
        }

        if (!string.IsNullOrEmpty(request.Currency))
        {
            query = query.Where(i => i.Currency == request.Currency);
        }

        if (!string.IsNullOrEmpty(request.InvoiceNumber))
        {
            query = query.Where(i => i.InvoiceNumber != null && i.InvoiceNumber.Contains(request.InvoiceNumber));
        }

        // Date range filters
        if (request.IssueDateFrom.HasValue)
        {
            query = query.Where(i => i.IssueDate >= request.IssueDateFrom.Value);
        }

        if (request.IssueDateTo.HasValue)
        {
            query = query.Where(i => i.IssueDate <= request.IssueDateTo.Value);
        }

        if (request.DueDateFrom.HasValue)
        {
            query = query.Where(i => i.DueDate >= request.DueDateFrom.Value);
        }

        if (request.DueDateTo.HasValue)
        {
            query = query.Where(i => i.DueDate <= request.DueDateTo.Value);
        }

        // Amount range filters
        if (request.GrandTotalFrom.HasValue)
        {
            query = query.Where(i => i.GrandTotal >= request.GrandTotalFrom.Value);
        }

        if (request.GrandTotalTo.HasValue)
        {
            query = query.Where(i => i.GrandTotal <= request.GrandTotalTo.Value);
        }

        // Overdue filter
        if (request.OnlyOverdue)
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(i => i.DueDate < today && i.Status != "Paid" && i.Status != "Cancelled");
        }

        // Parent/Child filters
        if (request.ParentInvoiceId.HasValue)
        {
            query = query.Where(i => i.ParentInvoiceId == request.ParentInvoiceId.Value);
        }

        if (request.ExcludeSplitParents)
        {
            // Assuming "Split" status indicates a parent that has been split
            query = query.Where(i => i.Status != "Split");
        }

        if (request.OnlyRootInvoices)
        {
            query = query.Where(i => i.ParentInvoiceId == null);
        }

        // Get total count before sorting and pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting (T123)
        query = request.SortBy.ToLower() switch
        {
            "issuedate" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(i => i.IssueDate)
                : query.OrderByDescending(i => i.IssueDate),
            "duedate" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(i => i.DueDate)
                : query.OrderByDescending(i => i.DueDate),
            "grandtotal" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(i => i.GrandTotal)
                : query.OrderByDescending(i => i.GrandTotal),
            "invoicenumber" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(i => i.InvoiceNumber)
                : query.OrderByDescending(i => i.InvoiceNumber),
            "customername" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(i => i.CustomerName)
                : query.OrderByDescending(i => i.CustomerName),
            _ => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(i => i.CreatedAt)
                : query.OrderByDescending(i => i.CreatedAt)
        };

        // Apply pagination
        var invoices = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var result = new PaginatedResponse<InvoiceResponse>
        {
            Items = invoices.Select(MapToResponse),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        // Cache the results for 5 minutes (T126)
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(result),
            cacheOptions,
            cancellationToken);

        _logger.LogDebug("Cached search results for key {CacheKey} with 5-minute TTL", cacheKey);

        return result;
    }

    private static IQueryable<Invoice> ApplyAccessScope(IQueryable<Invoice> query, InvoiceAccessScope accessScope)
    {
        if (!accessScope.RestrictToCreatedInvoices)
        {
            return query;
        }

        if (string.IsNullOrWhiteSpace(accessScope.PrincipalId))
        {
            return query.Where(_ => false);
        }

        return query.Where(i => i.AuditLogs.Any(a =>
            a.EventType == "Created" &&
            a.ActorId == accessScope.PrincipalId));
    }

    /// <inheritdoc/>
    public async Task<string> ExportInvoicesAsync(InvoiceSearchRequest request, string format, CancellationToken cancellationToken = default)
    {
        // Limit export to 1,000 invoices
        var exportRequest = new InvoiceSearchRequest
        {
            Page = request.Page,
            PageSize = Math.Min(request.PageSize, 1000),
            CustomerName = request.CustomerName,
            CustomerId = request.CustomerId,
            Status = request.Status,
            Currency = request.Currency,
            InvoiceNumber = request.InvoiceNumber,
            IssueDateFrom = request.IssueDateFrom,
            IssueDateTo = request.IssueDateTo,
            DueDateFrom = request.DueDateFrom,
            DueDateTo = request.DueDateTo,
            GrandTotalFrom = request.GrandTotalFrom,
            GrandTotalTo = request.GrandTotalTo,
            SortBy = request.SortBy,
            SortOrder = request.SortOrder,
            IncludeCancelled = request.IncludeCancelled,
            OnlyOverdue = request.OnlyOverdue
        };

        var query = _context.Invoices
            .Include(i => i.Lines)
            .Where(i => !i.IsDeleted)
            .AsNoTracking();

        // Apply same filters as SearchInvoicesAsync
        if (!exportRequest.IncludeCancelled)
        {
            query = query.Where(i => i.Status != "Cancelled");
        }

        if (!string.IsNullOrEmpty(exportRequest.CustomerName))
        {
            query = query.Where(i => i.CustomerName.Contains(exportRequest.CustomerName));
        }

        if (exportRequest.CustomerId.HasValue)
        {
            query = query.Where(i => i.CustomerId == exportRequest.CustomerId.Value);
        }

        if (!string.IsNullOrEmpty(exportRequest.Status))
        {
            query = query.Where(i => i.Status == exportRequest.Status);
        }

        if (!string.IsNullOrEmpty(exportRequest.Currency))
        {
            query = query.Where(i => i.Currency == exportRequest.Currency);
        }

        if (!string.IsNullOrEmpty(exportRequest.InvoiceNumber))
        {
            query = query.Where(i => i.InvoiceNumber != null && i.InvoiceNumber.Contains(exportRequest.InvoiceNumber));
        }

        if (exportRequest.IssueDateFrom.HasValue)
        {
            query = query.Where(i => i.IssueDate >= exportRequest.IssueDateFrom.Value);
        }

        if (exportRequest.IssueDateTo.HasValue)
        {
            query = query.Where(i => i.IssueDate <= exportRequest.IssueDateTo.Value);
        }

        if (exportRequest.DueDateFrom.HasValue)
        {
            query = query.Where(i => i.DueDate >= exportRequest.DueDateFrom.Value);
        }

        if (exportRequest.DueDateTo.HasValue)
        {
            query = query.Where(i => i.DueDate <= exportRequest.DueDateTo.Value);
        }

        if (exportRequest.GrandTotalFrom.HasValue)
        {
            query = query.Where(i => i.GrandTotal >= exportRequest.GrandTotalFrom.Value);
        }

        if (exportRequest.GrandTotalTo.HasValue)
        {
            query = query.Where(i => i.GrandTotal <= exportRequest.GrandTotalTo.Value);
        }

        if (exportRequest.OnlyOverdue)
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(i => i.DueDate < today && i.Status != "Paid" && i.Status != "Cancelled");
        }

        // Apply sorting
        query = exportRequest.SortBy.ToLower() switch
        {
            "issuedate" => exportRequest.SortOrder.ToLower() == "asc"
                ? query.OrderBy(i => i.IssueDate)
                : query.OrderByDescending(i => i.IssueDate),
            "duedate" => exportRequest.SortOrder.ToLower() == "asc"
                ? query.OrderBy(i => i.DueDate)
                : query.OrderByDescending(i => i.DueDate),
            "grandtotal" => exportRequest.SortOrder.ToLower() == "asc"
                ? query.OrderBy(i => i.GrandTotal)
                : query.OrderByDescending(i => i.GrandTotal),
            "invoicenumber" => exportRequest.SortOrder.ToLower() == "asc"
                ? query.OrderBy(i => i.InvoiceNumber)
                : query.OrderByDescending(i => i.InvoiceNumber),
            _ => exportRequest.SortOrder.ToLower() == "asc"
                ? query.OrderBy(i => i.CreatedAt)
                : query.OrderByDescending(i => i.CreatedAt)
        };

        var invoices = await query
            .Take(1000) // Hard limit to 1,000 invoices
            .ToListAsync(cancellationToken);

        var invoiceResponses = invoices.Select(MapToResponse).ToList();

        if (format.ToLower() == "csv")
        {
            return GenerateCsv(invoiceResponses);
        }
        else // JSON
        {
            return JsonSerializer.Serialize(invoiceResponses, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }

    private string GenerateCsv(List<InvoiceResponse> invoices)
    {
        var csv = new StringBuilder();

        // Header row matching InvoiceResponse DTO properties in order
        csv.AppendLine("Id,InvoiceNumber,CustomerId,CustomerName,CustomerTaxId,BillingAddress,ShippingAddress,Currency,IssueDate,DueDate,PaymentTermsDays,Status,Subtotal,TaxAmount,WithholdingTaxAmount,GrandTotal,ExchangeRate,ExchangeRateSource,QuotationReference,PoNumber,FinalizedAt,FinalizedBy,CancelledAt,CancelledBy,CancellationReason,CreatedAt,UpdatedAt,LineCount");

        // Data rows
        foreach (var invoice in invoices)
        {
            csv.AppendLine($"{invoice.Id}," +
                $"\"{invoice.InvoiceNumber}\"," +
                $"{invoice.CustomerId}," +
                $"\"{CsvEscape(invoice.CustomerName)}\"," +
                $"\"{CsvEscape(invoice.CustomerTaxId)}\"," +
                $"\"{CsvEscape(invoice.BillingAddress)}\"," +
                $"\"{CsvEscape(invoice.ShippingAddress)}\"," +
                $"{invoice.Currency}," +
                $"{invoice.IssueDate:yyyy-MM-dd}," +
                $"{invoice.DueDate:yyyy-MM-dd}," +
                $"{invoice.PaymentTermsDays}," +
                $"{invoice.Status}," +
                $"{invoice.Subtotal}," +
                $"{invoice.TaxAmount}," +
                $"{invoice.WithholdingTaxAmount}," +
                $"{invoice.GrandTotal}," +
                $"{invoice.ExchangeRate}," +
                $"\"{CsvEscape(invoice.ExchangeRateSource)}\"," +
                $"\"{CsvEscape(invoice.QuotationReference)}\"," +
                $"\"{CsvEscape(invoice.PoNumber)}\"," +
                $"{invoice.FinalizedAt:yyyy-MM-dd HH:mm:ss}," +
                $"\"{CsvEscape(invoice.FinalizedBy)}\"," +
                $"{invoice.CancelledAt:yyyy-MM-dd HH:mm:ss}," +
                $"\"{CsvEscape(invoice.CancelledBy)}\"," +
                $"\"{CsvEscape(invoice.CancellationReason)}\"," +
                $"{invoice.CreatedAt:yyyy-MM-dd HH:mm:ss}," +
                $"{invoice.UpdatedAt:yyyy-MM-dd HH:mm:ss}," +
                $"{invoice.Lines.Count}");
        }

        return csv.ToString();
    }

    private string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Escape quotes by doubling them
        return value.Replace("\"", "\"\"");
    }

    /// <inheritdoc/>
    public async Task<InvoiceResponse> FinalizeInvoiceAsync(Guid id, string finalizedBy, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        // Check idempotency key if provided
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existingKey = await _context.IdempotencyKeys
                .FirstOrDefaultAsync(k => k.Key == idempotencyKey && k.Operation == "FinalizeInvoice", cancellationToken);

            if (existingKey != null)
            {
                if (existingKey.ExpiresAt < DateTime.UtcNow)
                {
                    // Key expired, delete it and proceed with new request
                    _context.IdempotencyKeys.Remove(existingKey);
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Expired idempotency key {Key} for invoice finalization removed", idempotencyKey);
                }
                else
                {
                    // Return cached result
                    _logger.LogInformation("Idempotency key {Key} found for invoice {InvoiceId}, returning cached result", idempotencyKey, id);
                    var cachedResponse = JsonSerializer.Deserialize<InvoiceResponse>(existingKey.Response);
                    return cachedResponse ?? throw new InvalidOperationException("Failed to deserialize cached idempotency response");
                }
            }
        }

        var invoice = await _context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException($"Invoice {id} not found");

        if (invoice.Status != "Draft")
            throw new InvalidOperationException($"Invoice {id} is not in Draft status");

        // Validate mandatory fields before finalization
        ValidateMandatoryFields(invoice);

        // T154: Ensure exchange rate exists for foreign currency invoices
        if (invoice.Currency != "THB" && !invoice.ExchangeRate.HasValue)
        {
            _logger.LogInformation("Invoice {InvoiceId} is missing exchange rate, attempting to fetch from currency service", id);
            try
            {
                invoice.ExchangeRate = await _currencyClient.GetExchangeRateAsync(
                    invoice.Currency, "THB", DateTime.UtcNow, cancellationToken);
                invoice.ExchangeRateSource = "Currency Service (Auto-filled at finalization)";
                _logger.LogInformation("Retrieved exchange rate {Rate} for {FromCurrency} to THB during finalization",
                    invoice.ExchangeRate, invoice.Currency);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch exchange rate for {Currency} during finalization", invoice.Currency);
                throw new InvalidOperationException($"Cannot finalize invoice with {invoice.Currency} currency without exchange rate. Please provide manual exchange rate or try again when currency service is available.");
            }
        }

        // Generate sequential invoice number using database sequence
        // NOTE: Gap Risk Documentation
        // =============================
        // This implementation uses PostgreSQL's nextval() which increments the sequence IMMEDIATELY,
        // not at transaction commit. This means:
        //
        // Gap Scenarios:
        // 1. If SaveChangesAsync() fails after nextval(), the sequence number is consumed but never used
        // 2. Concurrent transactions will get different sequence numbers even if one rolls back
        // 3. Application crashes between nextval() and commit will create gaps
        //
        // Thai Tax Law Compliance:
        // According to Thai Revenue Code Section 86/4, tax invoices must have sequential numbers,
        // but the law does NOT prohibit gaps. Gaps are acceptable as long as:
        // - Numbers are sequential when issued (monotonically increasing)
        // - No duplicate invoice numbers exist
        // - Audit trail explains missing numbers (rollback, cancellation, etc.)
        //
        // Current implementation is COMPLIANT because:
        // - Invoice numbers are always increasing
        // - No duplicates are possible (sequence guarantees uniqueness)
        // - AuditLogs table tracks all finalization attempts (including failures)
        //
        // Alternative Implementation (Gap-Free):
        // If business requirements change to prohibit gaps, implement optimistic locking:
        // 1. Create invoice_number_lock table with single row
        // 2. SELECT ... FOR UPDATE on lock row before nextval()
        // 3. This serializes all invoice finalization (reduces throughput)
        // 4. Only acceptable if gap-free requirement is mandatory
        //
        // Decision: Current implementation is ACCEPTABLE for Thai tax compliance.
        // Reviewed: 2025-12-26

        // Manual Connection Management Documentation
        // =========================================
        // We manually open/close the database connection for raw SQL execution.
        // This is CORRECT and REQUIRED because:
        //
        // 1. EF Core does NOT auto-manage connections for raw SQL commands outside SaveChanges
        // 2. The connection must be open before ExecuteScalarAsync() is called
        // 3. Finally block ensures connection is closed even if command fails
        // 4. Using existing DbContext connection (not creating new connection)
        //
        // Execution Strategy Compatibility:
        // - EF Core execution strategies (retry policies) work at SaveChanges level
        // - Raw SQL commands executed here are idempotent (nextval is atomic)
        // - Connection is properly closed in finally block preventing connection leaks
        //
        // Alternative Considered: Let EF Core manage connection
        // - Not possible for raw SQL outside of SaveChanges/ExecuteSqlRaw
        // - Connection would not be open when ExecuteScalarAsync() is called
        //
        // Reviewed: 2025-12-26 - Manual connection management is CORRECT for raw SQL
        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT nextval('invoice_number_seq')";

        if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
        {
            await _context.Database.OpenConnectionAsync(cancellationToken);
        }

        object? result;
        try
        {
            result = await command.ExecuteScalarAsync(cancellationToken);
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }

        var nextSeq = Convert.ToInt64(result);

        var prefix = invoice.DocumentType switch
        {
            DocumentType.TaxInvoice => "TAX",
            DocumentType.Invoice => "INV",
            DocumentType.CreditNote => "CN",
            DocumentType.DebitNote => "DN",
            _ => "INV"
        };

        invoice.InvoiceNumber = $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{nextSeq:D6}";
        invoice.Status = "Finalized";
        invoice.FinalizedAt = DateTime.UtcNow;
        invoice.FinalizedBy = finalizedBy;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Record metrics
        InvoiceMetrics.RecordInvoiceFinalized();

        // Record invoice amount in THB
        var amountInThb = invoice.Currency == "THB"
            ? invoice.GrandTotal
            : invoice.GrandTotal * (invoice.ExchangeRate ?? 1m);
        InvoiceMetrics.RecordInvoiceAmount(amountInThb);

        // Cache the finalized invoice (24 hours)
        var response = MapToResponse(invoice);
        var cacheKey = $"invoice:{id}";
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        };
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(response),
            cacheOptions,
            cancellationToken);

        _logger.LogInformation("Finalized invoice {InvoiceId} with invoice number {InvoiceNumber}", invoice.Id, invoice.InvoiceNumber);

        // Store idempotency key if provided
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var idempotencyRecord = new IdempotencyKey
            {
                Key = idempotencyKey,
                Operation = "FinalizeInvoice",
                ResourceId = invoice.Id,
                Response = JsonSerializer.Serialize(response),
                StatusCode = 200,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _context.IdempotencyKeys.Add(idempotencyRecord);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Stored idempotency key {Key} for invoice {InvoiceId}", idempotencyKey, invoice.Id);
        }

        await _publishEndpoint.Publish(
            InvoiceSearchDocumentMapper.ToUpsertEvent(invoice, DateTimeOffset.UtcNow),
            cancellationToken);

        return response;
    }

    /// <inheritdoc/>
    public async Task<InvoiceResponse> CancelInvoiceAsync(Guid id, string cancelledBy, string reason, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException($"Invoice {id} not found");

        if (invoice.Status != "Finalized")
            throw new InvalidOperationException($"Cannot cancel invoice in {invoice.Status} status. Only finalized invoices can be cancelled.");

        invoice.Status = "Cancelled";
        invoice.CancelledAt = DateTime.UtcNow;
        invoice.CancelledBy = cancelledBy;
        invoice.CancellationReason = reason;
        invoice.UpdatedAt = DateTime.UtcNow;

        // Check if invoice had payments (refund may be required)
        var hadPayments = await _context.InvoicePaymentAllocations
            .AnyAsync(ipa => ipa.InvoiceId == id, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cancelled invoice {InvoiceId}", invoice.Id);

        // Publish InvoiceCancelledEvent
        Guid.TryParse(cancelledBy, out var cancelledByIdParsed);
        await _publishEndpoint.Publish(new InvoiceCancelledEvent(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(InvoiceCancelledEvent),
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "InvoiceService",
            ConsumedBy: new List<string> { "PaymentService", "NotificationService", "AnalyticsService" },
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new InvoiceCancelledEventPayload(
                InvoiceId: id,
                InvoiceNumber: invoice.InvoiceNumber ?? "UNKNOWN",
                CustomerId: invoice.CustomerId,
                CancelledBy: cancelledByIdParsed,
                CancelledAt: new DateTimeOffset(invoice.CancelledAt.Value, TimeSpan.Zero),
                CancellationReason: reason,
                RefundRequired: hadPayments
            )
        ), cancellationToken);

        await _publishEndpoint.Publish(
            InvoiceSearchDocumentMapper.ToUpsertEvent(invoice, DateTimeOffset.UtcNow),
            cancellationToken);

        return MapToResponse(invoice);
    }

    /// <inheritdoc/>
    public async Task<InvoiceResponse> UpdateInvoiceAsync(Guid id, UpdateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        // T135-T136: Load invoice and check immutability FIRST before any modifications
        var invoiceCheck = await _context.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException($"Invoice {id} not found");

        if (invoiceCheck.Status != "Draft")
        {
            _logger.LogWarning("Attempt to modify {Status} invoice {InvoiceId} rejected. Modifications only allowed for Draft invoices.",
                invoiceCheck.Status, invoiceCheck.Id);

            // Log the attempted modification in audit trail
            await _context.AuditLogs.AddAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceCheck.Id,
                EventType = "UpdateAttemptRejected",
                ChangedFields = JsonSerializer.Serialize(new { message = $"Attempted to modify {invoiceCheck.Status} invoice" }),
                ActorId = "System",
                Timestamp = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            throw new InvalidOperationException($"Cannot update invoice {id} in {invoiceCheck.Status} status. Only Draft invoices can be modified.");
        }

        // Load the invoice with lines for update
        var invoice = await _context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException($"Invoice {id} not found");

        // Delete existing lines (works with both relational and InMemory providers)
        _context.InvoiceLines.RemoveRange(invoice.Lines);

        // Update invoice properties
        invoice.CustomerName = request.CustomerName;
        invoice.CustomerTaxId = request.CustomerTaxId;
        invoice.BillingAddress = request.BillingAddress;
        invoice.ShippingAddress = request.ShippingAddress;
        invoice.PoNumber = request.PoNumber;
        invoice.DueDate = request.DueDate;
        invoice.PaymentTermsDays = request.PaymentTermsDays;
        invoice.LateFeePercentage = request.LateFeePercentage;
        invoice.UpdatedAt = DateTime.UtcNow;

        // T154: Update exchange rate if manual rate is provided
        if (request.ManualExchangeRate.HasValue && invoice.Currency != "THB")
        {
            invoice.ExchangeRate = request.ManualExchangeRate.Value;
            invoice.ExchangeRateSource = "Manual Entry (Updated)";
            _logger.LogInformation("Updated manual exchange rate to {Rate} for invoice {InvoiceId}",
                invoice.ExchangeRate, invoice.Id);
        }

        decimal subtotal = 0;
        decimal totalTax = 0;

        var newLines = new List<InvoiceLine>();

        foreach (var lineRequest in request.Lines)
        {
            var lineSubtotal = (lineRequest.Quantity * lineRequest.UnitPrice) * (1 - lineRequest.DiscountPercentage / 100);
            var lineTax = lineSubtotal * (lineRequest.TaxRate / 100);

            var line = new InvoiceLine
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                LineNumber = lineRequest.LineNumber,
                ItemCode = lineRequest.ItemCode,
                Description = lineRequest.Description,
                Quantity = lineRequest.Quantity,
                UnitPrice = lineRequest.UnitPrice,
                DiscountPercentage = lineRequest.DiscountPercentage,
                TaxCategory = lineRequest.TaxCategory,
                TaxRate = lineRequest.TaxRate,
                LineSubtotal = lineSubtotal,
                TaxAmount = lineTax,
                LineTotal = lineSubtotal + lineTax,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            subtotal += lineSubtotal;
            totalTax += lineTax;

            newLines.Add(line);
        }

        invoice.Subtotal = subtotal;
        invoice.TaxAmount = totalTax;

        // Calculate withholding tax
        invoice.WithholdingTaxAmount = CalculateWithholdingTax(subtotal, totalTax, request.WithholdingTaxPercentage);

        invoice.GrandTotal = subtotal + totalTax - invoice.WithholdingTaxAmount;

        // Add new lines to context
        _context.InvoiceLines.AddRange(newLines);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated invoice {InvoiceId}", invoice.Id);

        // Reload with lines for response
        invoice = await _context.Invoices
            .Include(i => i.Lines)
            .AsNoTracking()
            .FirstAsync(i => i.Id == id, cancellationToken);

        await _publishEndpoint.Publish(
            InvoiceSearchDocumentMapper.ToUpsertEvent(invoice, DateTimeOffset.UtcNow),
            cancellationToken);

        return MapToResponse(invoice);
    }

    /// <inheritdoc/>
    public async Task DeleteInvoiceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Invoice {id} not found");

        if (invoice.Status != "Draft")
            throw new InvalidOperationException($"Cannot delete invoice {id} in {invoice.Status} status");

        invoice.IsDeleted = true;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Soft deleted invoice {InvoiceId}", invoice.Id);

        await _publishEndpoint.Publish(
            InvoiceSearchDocumentMapper.ToDeletedEvent(invoice.Id, DateTimeOffset.UtcNow),
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<InvoiceResponse>> SplitInvoiceAsync(Guid id, SplitInvoiceRequest request, string splitBy, CancellationToken cancellationToken = default)
    {
        var parentInvoice = await _context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException($"Invoice {id} not found");

        if (parentInvoice.Status == "Split")
            throw new InvalidOperationException("Invoice has already been split. You cannot split an invoice more than once.");

        if (parentInvoice.Status != "Finalized")
            throw new InvalidOperationException($"Only finalized invoices can be split. Current status: {parentInvoice.Status}");

        // Validate split rules
        var totalPercentage = request.SplitRules.Sum(r => r.Percentage);
        if (Math.Abs(totalPercentage - 100) > 0.01m)
            throw new ArgumentException($"Split percentages must sum to 100%, got {totalPercentage}%", nameof(request));

        var childInvoices = new List<Invoice>();

        // Execution strategy for raw SQL sequence generation
        if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
        {
            await _context.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            foreach (var rule in request.SplitRules)
            {
                // Get next sequence value for child invoice number
                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT nextval('invoice_number_seq')";
                var result = await command.ExecuteScalarAsync(cancellationToken);
                var nextSeq = Convert.ToInt64(result);

                var childInvoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    ParentInvoiceId = parentInvoice.Id,
                    CustomerId = parentInvoice.CustomerId,
                    CustomerName = parentInvoice.CustomerName,
                    CustomerTaxId = parentInvoice.CustomerTaxId,
                    BillingAddress = parentInvoice.BillingAddress,
                    ShippingAddress = parentInvoice.ShippingAddress,
                    QuotationReference = parentInvoice.QuotationReference,
                    PoNumber = parentInvoice.PoNumber,
                    Currency = parentInvoice.Currency,
                    ExchangeRate = parentInvoice.ExchangeRate,
                    ExchangeRateSource = parentInvoice.ExchangeRateSource,
                    IssueDate = parentInvoice.IssueDate,
                    DueDate = parentInvoice.DueDate, // Can be overridden if rules allowed different due dates, but currently inherits
                    PaymentTermsDays = parentInvoice.PaymentTermsDays,
                    LateFeePercentage = parentInvoice.LateFeePercentage,
                    InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{nextSeq:D6}",
                    Status = "Finalized", // Child invoices start as Finalized
                    FinalizedAt = DateTime.UtcNow,
                    FinalizedBy = splitBy, // Use the actual user
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var ratio = rule.Percentage / 100m;

                // Split lines proportionally
                foreach (var parentLine in parentInvoice.Lines)
                {
                    var childLine = new InvoiceLine
                    {
                        Id = Guid.NewGuid(),
                        InvoiceId = childInvoice.Id,
                        LineNumber = parentLine.LineNumber,
                        ItemCode = parentLine.ItemCode,
                        Description = $"{parentLine.Description} (Split {rule.Percentage}%)",
                        Quantity = Math.Round(parentLine.Quantity * ratio, 4),
                        UnitPrice = parentLine.UnitPrice,
                        DiscountPercentage = parentLine.DiscountPercentage,
                        TaxCategory = parentLine.TaxCategory,
                        TaxRate = parentLine.TaxRate,
                        LineSubtotal = Math.Round(parentLine.LineSubtotal * ratio, 2),
                        TaxAmount = Math.Round(parentLine.TaxAmount * ratio, 2),
                        LineTotal = Math.Round(parentLine.LineTotal * ratio, 2),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    childInvoice.Lines.Add(childLine);
                }

                childInvoice.Subtotal = childInvoice.Lines.Sum(l => l.LineSubtotal);
                childInvoice.TaxAmount = childInvoice.Lines.Sum(l => l.TaxAmount);
                childInvoice.WithholdingTaxAmount = Math.Round(parentInvoice.WithholdingTaxAmount * ratio, 2);
                childInvoice.GrandTotal = childInvoice.Subtotal + childInvoice.TaxAmount - childInvoice.WithholdingTaxAmount;

                childInvoices.Add(childInvoice);
            }
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }

        // Add child invoices to context
        _context.Invoices.AddRange(childInvoices);

        // Update parent status
        parentInvoice.Status = "Split";
        parentInvoice.UpdatedAt = DateTime.UtcNow;

        // Add audit logs
        var parentAudit = new AuditLog
        {
            Id = Guid.NewGuid(),
            InvoiceId = parentInvoice.Id,
            EventType = "Split",
            ChangedFields = JsonSerializer.Serialize(new
            {
                ChildInvoiceIds = childInvoices.Select(c => c.Id).ToList(),
                Reason = request.Reason
            }),
            ActorId = splitBy,
            Timestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.AuditLogs.Add(parentAudit);

        foreach (var child in childInvoices)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                InvoiceId = child.Id,
                EventType = "Created",
                ChangedFields = JsonSerializer.Serialize(new
                {
                    Reason = $"Split from {parentInvoice.InvoiceNumber} ({child.GrandTotal / parentInvoice.GrandTotal * 100:F2}%)",
                    ParentInvoiceId = parentInvoice.Id
                }),
                ActorId = splitBy,
                Timestamp = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync($"invoice:{parentInvoice.Id}", cancellationToken);

        _logger.LogInformation("Split invoice {ParentId} into {Count} children by {User}", parentInvoice.Id, childInvoices.Count, splitBy);

        // Publish InvoiceSplitEvent
        var evt = new InvoiceSplitEvent(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(InvoiceSplitEvent),
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "InvoiceService",
            ConsumedBy: new List<string> { "AccountingService", "NotificationService", "AnalyticsService" },
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new InvoiceSplitEventPayload(
                ParentInvoiceId: parentInvoice.Id,
                ParentInvoiceNumber: parentInvoice.InvoiceNumber ?? "UNKNOWN",
                ChildInvoices: childInvoices.Select(c => new InvoiceSplitEventPayloadChildInvoicesItem(
                    Id: c.Id,
                    InvoiceNumber: c.InvoiceNumber ?? "UNKNOWN",
                    GrandTotal: (double)c.GrandTotal,
                    Percentage: (double)(childInvoices.IndexOf(c) < request.SplitRules.Count ? request.SplitRules[childInvoices.IndexOf(c)].Percentage : 0)
                )).ToList(),
                SplitBy: splitBy,
                SplitAt: DateTimeOffset.UtcNow,
                Reason: request.Reason ?? "Manual Split"
            )
        );

        await _publishEndpoint.Publish(evt, cancellationToken);

        await _publishEndpoint.Publish(
            InvoiceSearchDocumentMapper.ToUpsertEvent(parentInvoice, DateTimeOffset.UtcNow),
            cancellationToken);

        foreach (var childInvoice in childInvoices)
        {
            await _publishEndpoint.Publish(
                InvoiceSearchDocumentMapper.ToUpsertEvent(childInvoice, DateTimeOffset.UtcNow),
                cancellationToken);
        }

        return childInvoices.Select(MapToResponse).ToList();
    }



    /// <inheritdoc/>
    public async Task<PaymentResponse> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            PaymentAmount = request.PaymentAmount,
            PaymentDate = request.PaymentDate.Date,
            PaymentMethod = request.PaymentMethod,
            ReferenceNumber = request.ReferenceNumber,
            Notes = request.Notes,
            RecordedBy = request.RecordedBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created payment {PaymentId} for amount {Amount}", payment.Id, payment.PaymentAmount);

        return new PaymentResponse
        {
            Id = payment.Id,
            PaymentAmount = payment.PaymentAmount,
            PaymentDate = payment.PaymentDate,
            PaymentMethod = payment.PaymentMethod,
            ReferenceNumber = payment.ReferenceNumber,
            Notes = payment.Notes,
            RecordedBy = payment.RecordedBy,
            CreatedAt = payment.CreatedAt
        };
    }

    /// <inheritdoc/>
    public async Task<InvoiceResponse> LinkPaymentAsync(Guid invoiceId, LinkPaymentRequest request, CancellationToken cancellationToken = default)
    {
        // Delegate to AllocatePaymentAsync for payment allocation
        await AllocatePaymentAsync(invoiceId, request.PaymentId, request.AllocatedAmount, "api", cancellationToken);

        // Reload invoice to get updated status
        var invoice = await _context.Invoices
            .Include(i => i.Lines)
            .Include(i => i.InvoicePaymentAllocations)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && !i.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found");

        _logger.LogInformation("Linked payment {PaymentId} to invoice {InvoiceId} via API, new status: {Status}",
            request.PaymentId, invoiceId, invoice.Status);

        return MapToResponse(invoice);
    }

    /// <inheritdoc/>
    public async Task AllocatePaymentAsync(Guid invoiceId, Guid paymentId, decimal allocatedAmount, string allocatedBy, CancellationToken cancellationToken = default)
    {
        // Validate invoice exists and is finalized
        var invoice = await _context.Invoices
            .Include(i => i.InvoicePaymentAllocations)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && !i.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found");

        if (invoice.Status == "Draft" || invoice.Status == "Cancelled" || invoice.Status == "Split")
        {
            throw new InvalidOperationException($"Cannot allocate payment to invoice with status {invoice.Status}");
        }

        // Check for duplicate allocation (composite PK prevents duplicates at DB level)
        if (invoice.InvoicePaymentAllocations.Any(ipa => ipa.PaymentId == paymentId))
        {
            _logger.LogWarning("Payment {PaymentId} already allocated to invoice {InvoiceId}. Skipping duplicate.", paymentId, invoiceId);
            return; // Idempotency: Skip duplicate allocation
        }

        // Validate allocated amount doesn't exceed outstanding balance
        var outstandingBalance = await CalculateOutstandingBalanceAsync(invoiceId, cancellationToken);
        if (allocatedAmount > outstandingBalance)
        {
            throw new InvalidOperationException($"Allocated amount {allocatedAmount} exceeds outstanding balance {outstandingBalance}");
        }

        // Create allocation record
        var allocation = new InvoicePaymentAllocation
        {
            InvoiceId = invoiceId,
            PaymentId = paymentId,
            AllocatedAmount = allocatedAmount,
            AllocationDate = DateTime.UtcNow,
            AllocationStatus = "Confirmed",
            AllocatedBy = allocatedBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.InvoicePaymentAllocations.Add(allocation);

        // Update invoice status (T170)
        var newOutstandingBalance = outstandingBalance - allocatedAmount;
        if (newOutstandingBalance == 0)
        {
            invoice.Status = "FullyPaid";
            _logger.LogInformation("Invoice {InvoiceId} fully paid with payment {PaymentId}", invoiceId, paymentId);
        }
        else if (newOutstandingBalance < invoice.GrandTotal)
        {
            invoice.Status = "PartiallyPaid";
            _logger.LogInformation("Invoice {InvoiceId} partially paid: {Paid}/{Total}",
                invoiceId, invoice.GrandTotal - newOutstandingBalance, invoice.GrandTotal);
        }

        invoice.UpdatedAt = DateTime.UtcNow;

        // Save changes
        await _context.SaveChangesAsync(cancellationToken);

        // Cache invalidation (T178)
        await _cache.RemoveAsync($"invoice:{invoiceId}", cancellationToken);

        _logger.LogInformation(
            "Allocated payment to invoice: PaymentId={PaymentId}, InvoiceId={InvoiceId}, Amount={Amount}, NewStatus={Status}, OutstandingBalance={OutstandingBalance}",
            paymentId, invoiceId, allocatedAmount, invoice.Status, newOutstandingBalance);

        // Publish InvoicePaymentReceivedEvent
        Guid.TryParse(allocatedBy, out var allocatedByIdParsed);
        await _publishEndpoint.Publish(new InvoicePaymentReceivedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(InvoicePaymentReceivedEvent),
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "InvoiceService",
            ConsumedBy: new List<string> { "NotificationService", "AnalyticsService" },
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new InvoicePaymentReceivedEventPayload(
                InvoiceId: invoiceId,
                InvoiceNumber: invoice.InvoiceNumber ?? "UNKNOWN",
                PaymentId: paymentId,
                AllocatedAmount: (double)allocatedAmount,
                Currency: invoice.Currency,
                RemainingBalance: (double)newOutstandingBalance,
                AllocatedAt: new DateTimeOffset(allocation.AllocationDate, TimeSpan.Zero),
                AllocatedBy: allocatedByIdParsed
            )
        ), cancellationToken);

        // Publish InvoiceFullyPaidEvent if invoice is fully paid
        if (invoice.Status == "FullyPaid")
        {
            await _publishEndpoint.Publish(new InvoiceFullyPaidEvent(
                MessageId: Guid.NewGuid(),
                MessageName: nameof(InvoiceFullyPaidEvent),
                MessageType: MessageType.Event,
                MessageVersion: "1.0.0",
                PublishedBy: "InvoiceService",
                ConsumedBy: new List<string> { "NotificationService", "AnalyticsService" },
                CorrelationId: Guid.NewGuid(),
                CausationId: null,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                IsPublic: false,
                Payload: new InvoiceFullyPaidEventPayload(
                    InvoiceId: invoiceId,
                    InvoiceNumber: invoice.InvoiceNumber ?? "UNKNOWN",
                    CustomerId: invoice.CustomerId,
                    TotalAmount: (double)invoice.GrandTotal,
                    Currency: invoice.Currency,
                    LastPaymentId: paymentId,
                    FullyPaidAt: DateTimeOffset.UtcNow
                )
            ), cancellationToken);
        }

        // Add audit log entry for PaymentLinked event (T177)
        var changedFields = new
        {
            PaymentId = paymentId,
            AllocatedAmount = allocatedAmount,
            AllocatedBy = allocatedBy,
            PreviousStatus = invoice.Status == "FullyPaid" ? "PartiallyPaid" : (invoice.Status == "PartiallyPaid" ? "Finalized" : invoice.Status),
            NewStatus = invoice.Status,
            OutstandingBalance = newOutstandingBalance
        };

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            EventType = "PaymentLinked",
            ChangedFields = JsonSerializer.Serialize(changedFields),
            ActorId = allocatedBy,
            Timestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        // Publish PaymentAllocatedEvent for Financial Service
        await _publishEndpoint.Publish(new PaymentAllocatedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(PaymentAllocatedEvent),
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "InvoiceService",
            ConsumedBy: new List<string> { "FinancialService" },
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new PaymentAllocatedEventPayload(
                InvoiceId: invoiceId,
                InvoiceNumber: invoice.InvoiceNumber ?? "UNKNOWN",
                PaymentId: paymentId,
                AllocatedAmount: (double)allocatedAmount,
                Currency: invoice.Currency,
                CustomerId: invoice.CustomerId,
                InvoiceStatus: invoice.Status,
                OutstandingBalance: (double)newOutstandingBalance,
                AllocatedBy: allocatedBy,
                AllocatedAt: new DateTimeOffset(allocation.AllocationDate, TimeSpan.Zero)
            )
        ), cancellationToken);

        await _publishEndpoint.Publish(
            InvoiceSearchDocumentMapper.ToUpsertEvent(invoice, DateTimeOffset.UtcNow),
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<decimal> CalculateOutstandingBalanceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .Include(i => i.InvoicePaymentAllocations)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && !i.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found");

        // Sum all confirmed allocations
        var totalAllocated = invoice.InvoicePaymentAllocations
            .Where(ipa => ipa.AllocationStatus == "Confirmed")
            .Sum(ipa => ipa.AllocatedAmount);

        var outstandingBalance = invoice.GrandTotal - totalAllocated;

        _logger.LogDebug(
            "Calculated outstanding balance for invoice {InvoiceId}: GrandTotal={GrandTotal}, Allocated={Allocated}, Outstanding={Outstanding}",
            invoiceId, invoice.GrandTotal, totalAllocated, outstandingBalance);

        return outstandingBalance >= 0 ? outstandingBalance : 0;
    }

    private static void ValidateMandatoryFields(Invoice invoice)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(invoice.CustomerTaxId))
            errors.Add("Customer Tax ID is required for finalization");

        if (string.IsNullOrWhiteSpace(invoice.CustomerName))
            errors.Add("Customer Name is required for finalization");

        if (string.IsNullOrWhiteSpace(invoice.BillingAddress))
            errors.Add("Billing Address is required for finalization");

        if (invoice.PaymentTermsDays <= 0)
            errors.Add("Payment Terms Days must be greater than 0 for finalization");

        if (!invoice.Lines.Any())
            errors.Add("At least one line item is required for finalization");

        foreach (var line in invoice.Lines)
        {
            if (string.IsNullOrWhiteSpace(line.Description))
                errors.Add($"Line {line.LineNumber}: Description is required for finalization");

            if (line.Quantity <= 0)
                errors.Add($"Line {line.LineNumber}: Quantity must be greater than 0");

            if (line.UnitPrice < 0)
                errors.Add($"Line {line.LineNumber}: Unit Price must be non-negative");
        }

        if (errors.Any())
        {
            throw new InvalidOperationException($"Invoice cannot be finalized: {string.Join("; ", errors)}");
        }
    }

    private static decimal CalculateWithholdingTax(decimal subtotal, decimal taxAmount, decimal withholdingTaxPercentage)
    {
        if (withholdingTaxPercentage <= 0)
            return 0m;

        // Withholding tax is calculated on the subtotal (before VAT) in Thai regulations
        var withholdingTaxAmount = subtotal * (withholdingTaxPercentage / 100m);
        return Math.Round(withholdingTaxAmount, 2);
    }

    private static InvoiceResponse MapToResponse(Invoice invoice)
    {
        return new InvoiceResponse
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            ParentInvoiceId = invoice.ParentInvoiceId,
            CustomerId = invoice.CustomerId,
            CustomerName = invoice.CustomerName,
            CustomerTaxId = invoice.CustomerTaxId,
            BillingAddress = invoice.BillingAddress,
            ShippingAddress = invoice.ShippingAddress,
            QuotationReference = invoice.QuotationReference,
            PoNumber = invoice.PoNumber,
            Status = invoice.Status,
            Currency = invoice.Currency,
            ExchangeRate = invoice.ExchangeRate,
            ExchangeRateSource = invoice.ExchangeRateSource,
            Subtotal = invoice.Subtotal,
            TaxAmount = invoice.TaxAmount,
            WithholdingTaxAmount = invoice.WithholdingTaxAmount,
            GrandTotal = invoice.GrandTotal,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            PaymentTermsDays = invoice.PaymentTermsDays,
            LateFeePercentage = invoice.LateFeePercentage,
            FinalizedAt = invoice.FinalizedAt,
            FinalizedBy = invoice.FinalizedBy,
            CancelledAt = invoice.CancelledAt,
            CancelledBy = invoice.CancelledBy,
            CancellationReason = invoice.CancellationReason,
            PdfFileReference = invoice.PdfFileReference,
            CreatedAt = invoice.CreatedAt,
            CreatedBy = GetCreatedBy(invoice),
            UpdatedAt = invoice.UpdatedAt,
            Lines = invoice.Lines.Select(l => new InvoiceLineResponse
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                ItemCode = l.ItemCode,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                DiscountPercentage = l.DiscountPercentage,
                TaxCategory = l.TaxCategory,
                TaxRate = l.TaxRate,
                LineSubtotal = l.LineSubtotal,
                TaxAmount = l.TaxAmount,
                LineTotal = l.LineTotal
            }).ToList(),
            ChildInvoiceSummaries = invoice.ChildInvoices.Select(c => new ChildInvoiceSummary
            {
                Id = c.Id,
                InvoiceNumber = c.InvoiceNumber,
                GrandTotal = c.GrandTotal,
                Status = c.Status,
                DueDate = c.DueDate
            }).ToList()
        };
    }

    private static string? GetCreatedBy(Invoice invoice)
    {
        return invoice.AuditLogs
            .Where(a => a.EventType == "Created")
            .OrderBy(a => a.Timestamp)
            .Select(a => a.ActorId)
            .FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task<FileReferenceResponse> RegisterFileAsync(Guid invoiceId, RegisterFileRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices.FindAsync(new object[] { invoiceId }, cancellationToken);
        if (invoice == null)
            throw new KeyNotFoundException($"Invoice {invoiceId} not found");

        if (invoice.Status != "Finalized")
            throw new InvalidOperationException($"Cannot register file for invoice in {invoice.Status} status. Invoice must be finalized.");

        var fileReference = new FileReference
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            FileType = request.FileType,
            FileUrl = request.FileUrl,
            FileSizeBytes = request.FileSizeBytes,
            GeneratedBy = request.GeneratedBy,
            Checksum = request.Checksum,
            CreatedAt = DateTime.UtcNow
        };

        _context.FileReferences.Add(fileReference);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Registered {FileType} file reference for invoice {InvoiceId}", request.FileType, invoiceId);

        return new FileReferenceResponse
        {
            Id = fileReference.Id,
            InvoiceId = fileReference.InvoiceId,
            FileType = fileReference.FileType,
            FileUrl = fileReference.FileUrl,
            FileSizeBytes = fileReference.FileSizeBytes,
            GeneratedBy = fileReference.GeneratedBy,
            Checksum = fileReference.Checksum,
            CreatedAt = fileReference.CreatedAt
        };
    }

    /// <inheritdoc/>
    public async Task<List<FileReferenceResponse>> GetFileReferencesAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var files = await _context.FileReferences
            .Where(f => f.InvoiceId == invoiceId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

        return files.Select(f => new FileReferenceResponse
        {
            Id = f.Id,
            InvoiceId = f.InvoiceId,
            FileType = f.FileType,
            FileUrl = f.FileUrl,
            FileSizeBytes = f.FileSizeBytes,
            GeneratedBy = f.GeneratedBy,
            Checksum = f.Checksum,
            CreatedAt = f.CreatedAt
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task RegisterPdfFileReferenceAsync(Guid invoiceId, string pdfFileReference, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && !i.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found");

        if (invoice.Status == "Draft")
            throw new InvalidOperationException("Cannot register PDF file reference for draft invoice. Invoice must be finalized first.");

        // Update PDF file reference
        invoice.PdfFileReference = pdfFileReference;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cache.RemoveAsync($"invoice:{invoiceId}", cancellationToken);

        _logger.LogInformation("Registered PDF file reference for invoice {InvoiceId}: {PdfFileReference}", invoiceId, pdfFileReference);

        // Publish InvoiceGeneratedEvent
        await _publishEndpoint.Publish(new InvoiceGeneratedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(InvoiceGeneratedEvent),
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "InvoiceService",
            ConsumedBy: new List<string> { "NotificationService" },
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new InvoiceGeneratedEventPayload(
                InvoiceId: invoiceId,
                InvoiceNumber: invoice.InvoiceNumber ?? "UNKNOWN",
                PdfUrl: pdfFileReference,
                GeneratedAt: DateTimeOffset.UtcNow
            )
        ), cancellationToken);

        // Audit log
        var changedFields = new
        {
            PdfFileReference = pdfFileReference
        };

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            EventType = "PdfFileReferenceRegistered",
            ChangedFields = JsonSerializer.Serialize(changedFields),
            ActorId = "UploadService",
            Timestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await _publishEndpoint.Publish(
            InvoiceSearchDocumentMapper.ToUpsertEvent(invoice, DateTimeOffset.UtcNow),
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuditLogResponse>> GetAuditTrailAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        // Check if invoice exists
        var invoiceExists = await _context.Invoices.AnyAsync(i => i.Id == invoiceId, cancellationToken);
        if (!invoiceExists)
            throw new KeyNotFoundException($"Invoice {invoiceId} not found");

        var auditLogs = await _context.AuditLogs
            .Where(a => a.InvoiceId == invoiceId)
            .OrderBy(a => a.Timestamp)
            .ToListAsync(cancellationToken);

        return auditLogs.Select(a => new AuditLogResponse
        {
            Id = a.Id,
            EntityType = "Invoice",
            EntityId = a.InvoiceId,
            Action = a.EventType,
            Timestamp = a.Timestamp,
            PerformedBy = a.ActorId,
            Changes = a.ChangedFields,
            IpAddress = null,
            UserAgent = null
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task<PaymentResponse?> GetPaymentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var payment = await _context.Payments.FindAsync(new object[] { id }, cancellationToken);
        if (payment == null)
            return null;

        return new PaymentResponse
        {
            Id = payment.Id,
            PaymentAmount = payment.PaymentAmount,
            PaymentDate = payment.PaymentDate,
            PaymentMethod = payment.PaymentMethod,
            ReferenceNumber = payment.ReferenceNumber,
            Notes = payment.Notes,
            RecordedBy = payment.RecordedBy,
            CreatedAt = payment.CreatedAt
        };
    }

    // T156: Currency conversion reporting
    /// <inheritdoc/>
    public async Task<Dictionary<string, object>> GetCurrencyConversionReportAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invoiceId && !i.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found");

        var report = new Dictionary<string, object>
        {
            ["InvoiceId"] = invoice.Id,
            ["InvoiceNumber"] = invoice.InvoiceNumber ?? "Not assigned",
            ["OriginalCurrency"] = invoice.Currency,
            ["OriginalAmount"] = invoice.GrandTotal,
            ["TargetCurrency"] = "THB",
            ["ExchangeRate"] = invoice.ExchangeRate ?? 1m,
            ["ExchangeRateSource"] = invoice.ExchangeRateSource ?? "Default (1:1)",
            ["ConvertedAmount"] = invoice.GrandTotal * (invoice.ExchangeRate ?? 1m),
            ["IssueDate"] = invoice.IssueDate,
            ["Status"] = invoice.Status
        };

        // Add historical rate comparison if available
        if (invoice.Currency != "THB" && invoice.ExchangeRate.HasValue)
        {
            try
            {
                var currentRate = await _currencyClient.GetExchangeRateAsync(invoice.Currency, "THB", null, cancellationToken);
                var rateDifference = currentRate - invoice.ExchangeRate.Value;
                var percentageChange = (rateDifference / invoice.ExchangeRate.Value) * 100;

                report["CurrentExchangeRate"] = currentRate;
                report["RateDifference"] = rateDifference;
                report["PercentageChange"] = percentageChange;
                report["ValueDifferenceIfConvertedToday"] = invoice.GrandTotal * rateDifference;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch current exchange rate for currency {Currency}", invoice.Currency);
                report["CurrentExchangeRateError"] = "Unable to fetch current rate";
            }
        }

        return report;
    }

    // T187a: Analytics and reporting
    /// <inheritdoc/>
    public async Task<Dictionary<string, object>> GetAnalyticsSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var from = fromDate ?? DateTime.UtcNow.AddMonths(-12);
        var to = toDate ?? DateTime.UtcNow;

        var invoicesQuery = _context.Invoices
            .Where(i => !i.IsDeleted && i.CreatedAt >= from && i.CreatedAt <= to)
            .AsNoTracking();

        // Invoice counts by status
        var statusCounts = await invoicesQuery
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        // Total invoiced amounts
        var totalInvoiced = await invoicesQuery
            .Where(i => i.Status == "Finalized" || i.Status == "Paid" || i.Status == "PartiallyPaid")
            .SumAsync(i => i.GrandTotal * (i.ExchangeRate ?? 1m), cancellationToken);

        // Withholding tax totals
        var totalWithholdingTax = await invoicesQuery
            .SumAsync(i => i.WithholdingTaxAmount * (i.ExchangeRate ?? 1m), cancellationToken);

        // Receivable aging (outstanding invoices grouped by age)
        var today = DateTime.UtcNow.Date;
        var overdueInvoices = await invoicesQuery
            .Where(i => (i.Status == "Finalized" || i.Status == "PartiallyPaid") && i.DueDate < today)
            .ToListAsync(cancellationToken);

        var agingBuckets = new Dictionary<string, object>
        {
            ["0-30 days"] = overdueInvoices.Count(i => (today - i.DueDate).TotalDays <= 30),
            ["31-60 days"] = overdueInvoices.Count(i => (today - i.DueDate).TotalDays > 30 && (today - i.DueDate).TotalDays <= 60),
            ["61-90 days"] = overdueInvoices.Count(i => (today - i.DueDate).TotalDays > 60 && (today - i.DueDate).TotalDays <= 90),
            ["90+ days"] = overdueInvoices.Count(i => (today - i.DueDate).TotalDays > 90)
        };

        // Payment delays (average days between due date and payment date)
        var paidInvoices = await invoicesQuery
            .Where(i => i.Status == "Paid" || i.Status == "FullyPaid")
            .Include(i => i.InvoicePaymentAllocations)
            .ToListAsync(cancellationToken);

        var paymentDelays = new List<double>();
        foreach (var invoice in paidInvoices)
        {
            // Get the date of the last payment allocation as the "paid date"
            var lastAllocation = invoice.InvoicePaymentAllocations
                .OrderByDescending(a => a.AllocationDate)
                .FirstOrDefault();

            if (lastAllocation != null)
            {
                var delay = (lastAllocation.AllocationDate.Date - invoice.DueDate.Date).TotalDays;
                paymentDelays.Add(delay);
            }
        }

        var averagePaymentDelay = paymentDelays.Any() ? paymentDelays.Average() : 0;

        // Outstanding balance
        var outstandingBalance = await invoicesQuery
            .Where(i => i.Status == "Finalized" || i.Status == "PartiallyPaid")
            .SumAsync(i => i.GrandTotal * (i.ExchangeRate ?? 1m), cancellationToken);

        var summary = new Dictionary<string, object>
        {
            ["Period"] = new { From = from, To = to },
            ["InvoiceCountsByStatus"] = statusCounts,
            ["TotalInvoicedAmountTHB"] = totalInvoiced,
            ["TotalWithholdingTaxTHB"] = totalWithholdingTax,
            ["OutstandingBalanceTHB"] = outstandingBalance,
            ["ReceivableAging"] = agingBuckets,
            ["AveragePaymentDelayDays"] = averagePaymentDelay,
            ["OverdueInvoiceCount"] = overdueInvoices.Count,
            ["OverdueAmountTHB"] = overdueInvoices.Sum(i => i.GrandTotal * (i.ExchangeRate ?? 1m))
        };

        return summary;
    }
}
