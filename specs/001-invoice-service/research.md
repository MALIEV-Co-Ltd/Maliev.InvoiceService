# Research & Technical Decisions: Invoice Management Service

**Feature**: 001-invoice-service
**Date**: 2025-11-11
**Status**: Completed

## Overview

This document captures key technical decisions, design patterns, and architectural choices for implementing the Invoice Management Service. All decisions align with MALIEV Co. Ltd.'s standardized microservice architecture and address specific requirements from the feature specification.

---

## 1. Invoice Number Generation Strategy

### Decision
Use PostgreSQL sequences with explicit sequence definition in migration, combined with application-level transaction management for atomic invoice finalization.

### Rationale
- **FR-005 Requirement**: Atomic, guaranteed-unique invoice number generation
- PostgreSQL sequences provide database-level atomicity and uniqueness
- Sequence increments even on transaction rollback (prevents number reuse)
- Format: `INV-{year}-{sequence:00000}` (e.g., INV-2025-00001)
- Application code retrieves next sequence value within finalization transaction

### Implementation Pattern
```sql
-- Migration: Create sequence
CREATE SEQUENCE invoice_number_seq START WITH 1 INCREMENT BY 1 NO CYCLE;

-- C# Service Code
var sequenceValue = await _context.Database
    .ExecuteSqlRawAsync("SELECT nextval('invoice_number_seq')");
var invoiceNumber = $"INV-{DateTime.UtcNow.Year}-{sequenceValue:D5}";
invoice.InvoiceNumber = invoiceNumber;
invoice.Status = InvoiceStatus.Finalized;
invoice.FinalizedAt = DateTime.UtcNow;
invoice.FinalizedBy = userId;
await _context.SaveChangesAsync();
```

### Alternatives Considered
- **GUID-based invoice numbers**: Rejected because not human-readable and don't meet sequential numbering requirements for financial audits
- **Application-managed counter**: Rejected because requires distributed locking and doesn't guarantee atomicity under high concurrency
- **Database identity column**: Rejected because identity values can have gaps, and we need explicit control over formatting

---

## 2. Optimistic Concurrency with PostgreSQL RowVersion

### Decision
Manual RowVersion increment in DbContext.SaveChanges override using `ValueGeneratedNever()` configuration.

### Rationale
- **PostgreSQL bytea does NOT auto-increment** like SQL Server's rowversion
- Using `ValueGeneratedOnAddOrUpdate()` breaks concurrency detection in PostgreSQL
- Must manually increment RowVersion in `UpdateAuditFields()` method
- Prevents stale update conflicts and enforces last-write-wins detection

### Implementation Pattern
```csharp
// Entity Configuration
builder.Property(i => i.RowVersion)
    .HasColumnName("row_version")
    .IsRowVersion()
    .ValueGeneratedNever()  // CRITICAL for PostgreSQL
    .HasDefaultValueSql("'\\x0000000000000000'::bytea")
    .IsRequired();

// DbContext Override
private void UpdateAuditFields()
{
    var entries = ChangeTracker.Entries<Invoice>();
    var now = DateTime.UtcNow;

    foreach (var entry in entries)
    {
        if (entry.State == EntityState.Modified)
        {
            entry.Entity.UpdatedAt = now;
            UpdateRowVersion(entry.Entity);
            entry.Property(nameof(Invoice.RowVersion)).IsModified = true;
        }
    }
}

private static void UpdateRowVersion(Invoice entity)
{
    var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
    long versionNumber = currentVersion.Length >= 8
        ? BitConverter.ToInt64(currentVersion, 0)
        : 0;
    versionNumber++;
    entity.RowVersion = BitConverter.GetBytes(versionNumber);
}
```

### Alternatives Considered
- **ValueGeneratedOnAddOrUpdate**: Rejected - PostgreSQL doesn't auto-update bytea, causing false positives in concurrency tests
- **Timestamp column**: Rejected - not reliable for concurrency detection due to clock skew
- **Pessimistic locking**: Rejected - reduces throughput and introduces deadlock risk

---

## 3. Audit Trail Implementation

### Decision
EF Core interceptor (`AuditLogInterceptor`) with SaveChanges override to capture all state changes automatically.

### Rationale
- **FR-030 to FR-034, FR-056**: Comprehensive audit logging with 7-year retention
- Interceptor pattern ensures no audit events are missed
- Captures entity type, operation (Created, Updated, Deleted), timestamp, actor (user ID from ClaimsPrincipal)
- Stores changed fields as JSON for Update operations
- Separate `audit_logs` table with indexes on invoice_id and timestamp

### Implementation Pattern
```csharp
public class AuditLogInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        var userId = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";

        var auditEntries = new List<AuditLog>();
        foreach (var entry in context.ChangeTracker.Entries<Invoice>())
        {
            if (entry.State == EntityState.Added ||
                entry.State == EntityState.Modified ||
                entry.State == EntityState.Deleted)
            {
                auditEntries.Add(new AuditLog
                {
                    InvoiceId = entry.Entity.Id,
                    EventType = entry.State.ToString(),
                    Timestamp = DateTime.UtcNow,
                    ActorId = userId,
                    ChangedFields = entry.State == EntityState.Modified
                        ? JsonSerializer.Serialize(GetChangedFields(entry))
                        : null
                });
            }
        }

        context.Set<AuditLog>().AddRange(auditEntries);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

### Alternatives Considered
- **Manual audit logging in service methods**: Rejected - easy to miss events, requires discipline
- **Database triggers**: Rejected - harder to test, debugging complexity, actor identity not available
- **Event sourcing**: Rejected - unnecessary complexity for this domain; full event store overkill for audit requirements

---

## 4. Currency Service Integration with Polly v8 Resilience

### Decision
Typed HttpClient with Polly v8 `AddStandardResilienceHandler` for retry, circuit breaker, and timeout.

### Rationale
- **FR-021, FR-024, FR-055**: 5-second timeout, 3 retries with exponential backoff, circuit breaker
- Polly v8 uses `Microsoft.Extensions.Http.Resilience` package
- Standard resilience handler includes retry (exponential backoff + jitter), circuit breaker, and timeout in one configuration
- Fallback to manual rate entry or defer finalization if Currency Service unavailable after retries

### Implementation Pattern
```csharp
builder.Services.AddHttpClient<ICurrencyServiceClient, CurrencyServiceClient>(client =>
{
    var baseUrl = builder.Configuration["ExternalServices:CurrencyService:BaseUrl"]
        ?? throw new InvalidOperationException("Currency Service URL not configured");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddStandardResilienceHandler(options =>
{
    // Retry: 3 attempts, exponential backoff with jitter
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromSeconds(1);
    options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
    options.Retry.UseJitter = true;
    options.Retry.OnRetry = args =>
    {
        Log.Warning("Currency Service retry {Attempt} after {Delay}s",
            args.AttemptNumber, args.RetryDelay.TotalSeconds);
        return ValueTask.CompletedTask;
    };

    // Circuit Breaker: open after 5 consecutive failures, break for 30s
    options.CircuitBreaker.FailureRatio = 1.0;
    options.CircuitBreaker.MinimumThroughput = 5;
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
});
```

### Alternatives Considered
- **Synchronous retry with Thread.Sleep**: Rejected - blocks thread, poor performance
- **Manual retry logic without Polly**: Rejected - reinventing the wheel, error-prone
- **Polly v7 AddPolicyHandler**: Rejected - deprecated in favor of v8 standard resilience handler

---

## 5. Invoice Splitting and Reconciliation Logic

### Decision
Parent-child relationship with explicit foreign key, split algorithm distributes line items proportionally, rounding adjustment applied to last child.

### Rationale
- **FR-016 to FR-020**: Split invoice into children with reconciliation enforcement
- Parent invoice remains finalized and immutable after split
- Each child invoice references `parent_invoice_id` foreign key
- Split percentage (e.g., 40%, 60%) applied to each line item quantity and amount
- Rounding differences (e.g., 0.01 THB) added/subtracted from last child's final line item
- Validation: `SUM(child_invoice_totals) MUST EQUAL parent_invoice_total`

### Implementation Pattern
```csharp
public async Task<List<Invoice>> SplitInvoiceAsync(
    Guid parentInvoiceId,
    List<SplitPercentage> splits)
{
    var parent = await _context.Invoices
        .Include(i => i.Lines)
        .FirstOrDefaultAsync(i => i.Id == parentInvoiceId);

    if (parent.Status != InvoiceStatus.Finalized)
        throw new InvalidOperationException("Only finalized invoices can be split");

    var childInvoices = new List<Invoice>();
    decimal totalAllocated = 0;

    foreach (var split in splits)
    {
        var child = new Invoice
        {
            ParentInvoiceId = parent.Id,
            CustomerId = parent.CustomerId,
            Currency = parent.Currency,
            ExchangeRate = parent.ExchangeRate,
            Status = InvoiceStatus.Draft
        };

        foreach (var parentLine in parent.Lines)
        {
            var childLine = new InvoiceLine
            {
                ItemCode = parentLine.ItemCode,
                Description = parentLine.Description,
                Quantity = parentLine.Quantity * split.Percentage,
                UnitPrice = parentLine.UnitPrice,
                TaxRate = parentLine.TaxRate
            };
            child.Lines.Add(childLine);
        }

        child.CalculateTotals();
        totalAllocated += child.GrandTotal;
        childInvoices.Add(child);
    }

    // Apply rounding adjustment to last child
    var roundingDiff = parent.GrandTotal - totalAllocated;
    if (roundingDiff != 0)
    {
        var lastChild = childInvoices.Last();
        lastChild.GrandTotal += roundingDiff;
        lastChild.Lines.Last().Subtotal += roundingDiff;
    }

    await _context.Invoices.AddRangeAsync(childInvoices);
    await _context.SaveChangesAsync();
    return childInvoices;
}
```

### Alternatives Considered
- **Copy all parent fields to children**: Rejected - violates DRY, inconsistency risk
- **Equal amount distribution without rounding adjustment**: Rejected - fails reconciliation validation
- **Nested splits (split a child invoice)**: Deferred to future iteration - adds hierarchy complexity

---

## 6. Payment Allocation and Status Tracking

### Decision
Many-to-many relationship between Payments and Invoices with junction table `invoice_payments` storing allocation amounts.

### Rationale
- **FR-040 to FR-044**: Record payments, update status, allocate across multiple invoices
- Single payment can pay multiple invoices (e.g., customer pays $10,000 for 3 invoices)
- Invoice status transitions: Draft → Finalized → PartiallyPaid → FullyPaid
- Outstanding balance calculated as: `GrandTotal - SUM(allocated_payment_amounts)`
- Junction table stores payment_id, invoice_id, allocated_amount

### Implementation Pattern
```csharp
// Entity Model
public class InvoicePayment
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; }

    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; }

    public decimal AllocatedAmount { get; set; }
}

// Service Method
public async Task AllocatePaymentAsync(Guid paymentId, List<InvoiceAllocation> allocations)
{
    var payment = await _context.Payments.FindAsync(paymentId);
    var totalAllocated = allocations.Sum(a => a.Amount);

    if (totalAllocated > payment.Amount)
        throw new InvalidOperationException("Total allocation exceeds payment amount");

    foreach (var allocation in allocations)
    {
        var invoice = await _context.Invoices.FindAsync(allocation.InvoiceId);
        var invoicePayment = new InvoicePayment
        {
            PaymentId = paymentId,
            InvoiceId = allocation.InvoiceId,
            AllocatedAmount = allocation.Amount
        };
        _context.InvoicePayments.Add(invoicePayment);

        // Update invoice status
        var totalPaid = await _context.InvoicePayments
            .Where(ip => ip.InvoiceId == allocation.InvoiceId)
            .SumAsync(ip => ip.AllocatedAmount);

        invoice.Status = totalPaid >= invoice.GrandTotal
            ? InvoiceStatus.FullyPaid
            : InvoiceStatus.PartiallyPaid;
    }

    await _context.SaveChangesAsync();
}
```

### Alternatives Considered
- **One-to-many (Payment has many Invoices)**: Rejected - doesn't handle bulk payments correctly
- **Separate PaymentAllocation entity**: Considered equivalent to junction table approach
- **Store outstanding balance as column**: Rejected - derived value, can become stale

---

## 7. Search and Pagination Strategy

### Decision
LINQ query composition with `AsNoTracking()`, late materialization, and `PaginatedResponse<T>` wrapper.

### Rationale
- **FR-035 to FR-039**: Search with multiple filters, pagination, sorting, bulk export
- `AsNoTracking()` improves read-only query performance by ~30%
- Filters applied incrementally via `Where()` clauses before `Skip().Take()`
- Total count query separate from data query to optimize performance
- Default page size: 50, max page size: 1000 (for bulk export)

### Implementation Pattern
```csharp
public async Task<PaginatedResponse<InvoiceResponse>> SearchInvoicesAsync(
    InvoiceSearchRequest request)
{
    var query = _context.Invoices
        .AsNoTracking()
        .Where(i => !i.IsDeleted);

    // Apply filters incrementally
    if (!string.IsNullOrEmpty(request.CustomerName))
        query = query.Where(i => i.CustomerName.Contains(request.CustomerName));

    if (request.Status != null)
        query = query.Where(i => i.Status == request.Status);

    if (request.Currency != null)
        query = query.Where(i => i.Currency == request.Currency);

    if (request.IssueDateFrom != null)
        query = query.Where(i => i.IssueDate >= request.IssueDateFrom);

    if (request.IssueDateTo != null)
        query = query.Where(i => i.IssueDate <= request.IssueDateTo);

    // Exclude cancelled by default
    if (!request.IncludeCancelled)
        query = query.Where(i => i.Status != InvoiceStatus.Cancelled);

    // Count total before pagination
    var totalCount = await query.CountAsync();

    // Apply sorting
    query = request.SortBy switch
    {
        "issueDate" => query.OrderByDescending(i => i.IssueDate),
        "dueDate" => query.OrderBy(i => i.DueDate),
        "grandTotal" => query.OrderByDescending(i => i.GrandTotal),
        _ => query.OrderByDescending(i => i.InvoiceNumber)
    };

    // Apply pagination
    var invoices = await query
        .Skip(request.Page * request.PageSize)
        .Take(request.PageSize)
        .ToListAsync();

    return new PaginatedResponse<InvoiceResponse>
    {
        Items = invoices.Select(MapToResponse).ToList(),
        TotalCount = totalCount,
        Page = request.Page,
        PageSize = request.PageSize
    };
}
```

### Alternatives Considered
- **Load all records and filter in memory**: Rejected - poor performance at scale
- **Stored procedures for complex queries**: Rejected - reduces portability, harder to test
- **Elasticsearch for full-text search**: Deferred - overkill for initial version, can add later

---

## 8. Caching Strategy with Redis

### Decision
Distributed cache with StackExchangeRedis, cache-aside pattern, TTL-based expiration, explicit invalidation on mutations.

### Rationale
- **FR-049 to FR-052**: Cache frequently accessed data, 200ms p95 for cached queries
- Cache keys: `InvoiceService:invoice:{id}`, `InvoiceService:customer:{customerId}:invoices`
- Long TTL (24 hours) for finalized invoices (immutable)
- Short TTL (5 minutes) for draft invoices
- Invalidate on create, update, finalize, cancel operations
- Fallback to in-memory cache if Redis unavailable (Testing environment)

### Implementation Pattern
```csharp
public async Task<InvoiceResponse?> GetInvoiceCachedAsync(Guid id)
{
    var cacheKey = $"InvoiceService:invoice:{id}";
    var cached = await _cache.GetStringAsync(cacheKey);

    if (cached != null)
        return JsonSerializer.Deserialize<InvoiceResponse>(cached);

    var invoice = await _context.Invoices
        .AsNoTracking()
        .Include(i => i.Lines)
        .FirstOrDefaultAsync(i => i.Id == id);

    if (invoice == null)
        return null;

    var response = MapToResponse(invoice);
    var ttl = invoice.Status == InvoiceStatus.Finalized
        ? TimeSpan.FromHours(24)
        : TimeSpan.FromMinutes(5);

    await _cache.SetStringAsync(
        cacheKey,
        JsonSerializer.Serialize(response),
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }
    );

    return response;
}

public async Task InvalidateInvoiceCacheAsync(Guid id)
{
    await _cache.RemoveAsync($"InvoiceService:invoice:{id}");
    // Also invalidate customer-level cache
    var invoice = await _context.Invoices.FindAsync(id);
    if (invoice != null)
        await _cache.RemoveAsync($"InvoiceService:customer:{invoice.CustomerId}:invoices");
}
```

### Alternatives Considered
- **In-memory cache only (IMemoryCache)**: Rejected - doesn't scale across multiple service instances
- **Write-through cache**: Rejected - adds complexity, eventual consistency acceptable
- **Cache everything**: Rejected - memory waste, stale data risk for frequently updated records

---

## 9. Validation Strategy with FluentValidation

### Decision
Dedicated validator classes for each request DTO, registered via `AddValidatorsFromAssemblyContaining`, custom async validation rules for external dependencies.

### Rationale
- **FR-025 to FR-029**: Comprehensive validation before finalization
- Validators registered automatically via assembly scanning
- Custom rules: `CustomerExists`, `QuotationReferenceValid`, `WithholdingTaxCalculationValid`
- Async validation for external service checks (Currency Service rate existence)
- Validation errors return 400 Bad Request with structured error details

### Implementation Pattern
```csharp
public class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
{
    private readonly IQuotationServiceClient _quotationClient;

    public CreateInvoiceRequestValidator(IQuotationServiceClient quotationClient)
    {
        _quotationClient = quotationClient;

        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required");

        RuleFor(x => x.CustomerName)
            .NotEmpty().MaximumLength(500)
            .WithMessage("Customer name is required and must not exceed 500 characters");

        RuleFor(x => x.CustomerTaxId)
            .NotEmpty().Matches(@"^\d{13}$")
            .WithMessage("Customer Tax ID must be 13 digits");

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("Invoice must have at least one line item");

        RuleForEach(x => x.Lines).SetValidator(new InvoiceLineRequestValidator());

        RuleFor(x => x.QuotationReference)
            .MustAsync(async (ref, cancellation) =>
            {
                if (string.IsNullOrEmpty(ref)) return true;
                return await _quotationClient.QuotationExistsAsync(ref);
            })
            .WithMessage("Quotation reference does not exist");
    }
}

public class InvoiceLineRequestValidator : AbstractValidator<InvoiceLineItemRequest>
{
    public InvoiceLineRequestValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be positive");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative");

        RuleFor(x => x.Description)
            .NotEmpty().MaximumLength(1000);
    }
}
```

### Alternatives Considered
- **Data annotations**: Rejected - less flexible, harder to test, no async validation
- **Manual validation in service methods**: Rejected - violates single responsibility, repetitive
- **Guard clauses**: Rejected - acceptable for simple checks but verbose for complex rules

---

## 10. Routing Configuration (CRITICAL)

### Decision
Direct path prefixes in all route attributes (NO UsePathBase), explicit paths for OpenAPI, Scalar, health checks, and metrics.

### Rationale
- **CRITICAL ROUTING CONFIGURATION LESSONS**: UsePathBase causes mismatch with Scalar UI and OpenAPI endpoints
- Pattern: `/{servicename}/v{version}/{resource}` for API endpoints
- Pattern: `/invoices/scalar/v1` for Scalar UI
- Pattern: `/invoices/openapi/v1.json` for OpenAPI spec
- Pattern: `/invoices/liveness` and `/invoices/readiness` for health checks
- Pattern: `/invoices/metrics` for Prometheus scraping
- launchSettings.json launchUrl: `"invoices/scalar/v1"`

### Implementation Pattern
```csharp
// Controllers
[ApiController]
[ApiVersion("1.0")]
[Route("invoices/v{version:apiVersion}/invoices")]
public class InvoicesController : ControllerBase { }

[Route("invoices/v{version:apiVersion}/payments")]
public class PaymentsController : ControllerBase { }

[Route("invoices/v{version:apiVersion}/audit")]
public class AuditController : ControllerBase { }

// Program.cs
app.MapOpenApi("/invoices/openapi/{documentName}.json");

app.MapScalarApiReference(options =>
{
    options.WithTitle("Invoice Service API")
        .WithTheme(ScalarTheme.Purple)
        .WithEndpointPrefix("/invoices/scalar/{documentName}")
        .WithOpenApiRoutePattern("/invoices/openapi/{documentName}.json");
});

app.MapHealthChecks("/invoices/liveness", new HealthCheckOptions { ... });
app.MapHealthChecks("/invoices/readiness", new HealthCheckOptions { ... });
app.MapMetrics("/invoices/metrics");
```

### Alternatives Considered
- **UsePathBase("/invoices")**: Rejected - doesn't work with Scalar UI, causes routing mismatches
- **Separate route files**: Rejected - unnecessary abstraction for microservice
- **Minimal APIs**: Rejected - controller-based approach is company standard

---

## 11. Background Service for Audit Log Archival

### Decision
IHostedService implementation running daily to archive old audit logs to cold storage (future: Google Cloud Storage).

### Rationale
- **FR-056**: 7-year audit log retention requirement
- Daily background job checks for logs older than 1 year
- Move old logs to separate `audit_logs_archive` table or external storage
- Keep recent logs (< 1 year) in hot storage for fast queries
- Future enhancement: Export to Google Cloud Storage with lifecycle policy

### Implementation Pattern
```csharp
public class AuditArchivalService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditArchivalService> _logger;

    public AuditArchivalService(
        IServiceProvider serviceProvider,
        ILogger<AuditArchivalService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
                    var cutoffDate = DateTime.UtcNow.AddYears(-1);

                    var oldLogs = await context.AuditLogs
                        .Where(a => a.Timestamp < cutoffDate && !a.IsArchived)
                        .Take(1000)
                        .ToListAsync(stoppingToken);

                    if (oldLogs.Any())
                    {
                        // Mark as archived (future: export to GCS)
                        oldLogs.ForEach(log => log.IsArchived = true);
                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Archived {Count} audit logs", oldLogs.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during audit log archival");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}

// Registration
builder.Services.AddHostedService<AuditArchivalService>();
```

### Alternatives Considered
- **Manual archival script**: Rejected - requires operational discipline, error-prone
- **Delete old logs**: Rejected - violates 7-year retention requirement
- **Immediate export to GCS**: Deferred - adds complexity, can add in Phase 2

---

## 12. Testing Strategy

### Decision
Three-tier test strategy: Contract tests (API endpoints), Integration tests (workflows), Unit tests (validators/services), using Testcontainers for PostgreSQL.

### Rationale
- **Constitution Principle III**: Test-first development mandatory
- TestWebApplicationFactory for API endpoint testing
- TestDatabaseFixture with Testcontainers for isolated PostgreSQL per test class
- FluentAssertions for readable assertions
- Moq for external service mocking (Currency Service, Quotation Service)
- Target: 80%+ code coverage for critical paths

### Test Structure
```text
Maliev.InvoiceService.Tests/
├── Contract/               # API contract tests
│   ├── InvoiceEndpointsTests.cs
│   ├── PaymentEndpointsTests.cs
│   └── AuditEndpointsTests.cs
├── Integration/            # End-to-end workflow tests
│   ├── InvoiceCreationTests.cs
│   ├── InvoiceSplittingTests.cs
│   ├── PaymentAllocationTests.cs
│   └── CurrencyConversionTests.cs
├── Unit/                   # Unit tests for isolated logic
│   ├── Validators/
│   │   ├── CreateInvoiceRequestValidatorTests.cs
│   │   └── FinalizeInvoiceRequestValidatorTests.cs
│   └── Services/
│       ├── InvoiceServiceTests.cs
│       └── PaymentServiceTests.cs
└── Fixtures/
    ├── TestDatabaseFixture.cs
    └── TestWebApplicationFactory.cs
```

### Key Test Patterns
1. **Contract Tests**: Verify request/response contracts, status codes, validation errors
2. **Integration Tests**: Verify complete workflows (create → finalize → split → payment)
3. **Unit Tests**: Verify validators, business logic, calculations in isolation
4. **Database Cleanup**: `TestDatabaseFixture.ClearDatabaseAsync()` between tests
5. **Mock External Services**: `Mock<ICurrencyServiceClient>`, `Mock<IQuotationServiceClient>`

---

## 13. Observability and Monitoring

### Decision
Prometheus metrics via `prometheus-net`, custom business metrics, correlation ID middleware, structured logging with Serilog.

### Rationale
- **Constitution Principle**: Observability required for all services
- HTTP metrics auto-collected via `UseHttpMetrics()`
- Custom metrics: `invoices_created_total`, `invoices_finalized_total`, `invoice_split_operations_total`
- Database metrics via `DatabaseMetricsInterceptor`
- Correlation ID in all logs and response headers (X-Correlation-ID)
- Metrics endpoint at `/invoices/metrics` for Prometheus scraping

### Implementation Pattern
```csharp
// Custom Business Metrics
public class InvoiceMetrics
{
    private static readonly Counter InvoicesCreated = Metrics
        .CreateCounter("invoices_created_total", "Total invoices created");

    private static readonly Counter InvoicesFinalized = Metrics
        .CreateCounter("invoices_finalized_total", "Total invoices finalized");

    private static readonly Gauge ActiveInvoices = Metrics
        .CreateGauge("invoices_active_count", "Current number of active invoices");

    private static readonly Histogram InvoiceAmount = Metrics
        .CreateHistogram("invoice_amount_thb", "Invoice amounts in THB");

    public void RecordInvoiceCreated() => InvoicesCreated.Inc();
    public void RecordInvoiceFinalized(decimal amount)
    {
        InvoicesFinalized.Inc();
        InvoiceAmount.Observe((double)amount);
    }
}

// Correlation ID Middleware
public static class CorrelationIdMiddleware
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                ?? Guid.NewGuid().ToString();

            context.Response.Headers.TryAdd("X-Correlation-ID", correlationId);

            using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
            {
                await next();
            }
        });

        return app;
    }
}

// Force metrics initialization in Program.cs
RuntimeHelpers.RunClassConstructor(typeof(InvoiceMetrics).TypeHandle);
```

---

## 14. Authorization with Role-Based Access Control (RBAC)

### Decision
ASP.NET Core policy-based authorization with custom policies: Customer, Employee, Manager, Admin, EmployeeOrHigher.

### Rationale
- **FR-057 to FR-059**: Operation-level permissions (create, edit, finalize, cancel, view audit, record payments, split, export)
- JWT claims: `ClaimTypes.NameIdentifier`, `ClaimTypes.Role`, custom `userType` claim
- Standard policies registered in Program.cs
- Controllers decorated with `[Authorize(Policy = "Employee")]` or `[Authorize(Policy = "Admin")]`
- Health endpoints marked `[AllowAnonymous]`

### Implementation Pattern
```csharp
// Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Customer", policy =>
        policy.RequireClaim("userType", "customer"));

    options.AddPolicy("Employee", policy =>
        policy.RequireClaim("userType", "employee"));

    options.AddPolicy("Manager", policy =>
        policy.RequireRole("Manager"));

    options.AddPolicy("Admin", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("EmployeeOrHigher", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("userType", "employee") ||
            context.User.IsInRole("Manager") ||
            context.User.IsInRole("Admin")));
});

// Controller
[ApiController]
[Route("invoices/v{version:apiVersion}/invoices")]
[Authorize(Policy = "EmployeeOrHigher")]
public class InvoicesController : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "Employee")]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request) { }

    [HttpPost("{id}/finalize")]
    [Authorize(Policy = "Manager")]
    public async Task<IActionResult> FinalizeInvoice(Guid id) { }

    [HttpGet]
    [AllowAnonymous]  // Public endpoint for customers
    public async Task<IActionResult> SearchInvoices([FromQuery] InvoiceSearchRequest request) { }
}
```

---

## Summary of Key Technologies & Patterns

| Area | Technology/Pattern | Justification |
|------|-------------------|---------------|
| Invoice Number Generation | PostgreSQL Sequences | Atomic, guaranteed unique, sequential |
| Concurrency Control | Manual RowVersion increment | PostgreSQL bytea doesn't auto-increment |
| Audit Trail | EF Core Interceptor | Automatic, no missed events |
| External Services | Polly v8 AddStandardResilienceHandler | Retry, circuit breaker, timeout in one |
| Invoice Splitting | Parent-child FK, rounding adjustment | Reconciliation enforcement |
| Payment Allocation | Many-to-many junction table | Supports bulk payments |
| Search & Pagination | AsNoTracking + LINQ composition | ~30% performance improvement |
| Caching | Redis with cache-aside pattern | Distributed, scalable |
| Validation | FluentValidation with async rules | Flexible, testable |
| Routing | Direct path prefixes (NO UsePathBase) | Avoids Scalar UI mismatch |
| Background Jobs | IHostedService | Audit log archival |
| Testing | Testcontainers + TestWebApplicationFactory | Real database behavior |
| Observability | Prometheus + Correlation ID | Metrics + distributed tracing |
| Authorization | Policy-based RBAC | Operation-level permissions |

---

## Next Steps

1. **Phase 1: Design & Contracts**
   - Generate `data-model.md` with entity definitions
   - Generate OpenAPI contracts in `/contracts/`
   - Generate `quickstart.md` for local development
   - Update agent context with new technology decisions

2. **Phase 2: Task Generation** (NOT part of `/speckit.plan` command)
   - Generate `tasks.md` using `/speckit.tasks` command
   - Break down implementation into actionable tasks
   - Prioritize tasks based on user story priorities

3. **Phase 3: Implementation** (NOT part of `/speckit.plan` command)
   - Execute tasks using `/speckit.implement` command
   - Follow TDD approach: Tests → Fail → Implement → Pass
   - Verify constitution compliance at each step

---

**Status**: ✅ Research Complete - All technical decisions documented and justified
**Next Command**: Proceed to Phase 1 (data-model.md generation)
