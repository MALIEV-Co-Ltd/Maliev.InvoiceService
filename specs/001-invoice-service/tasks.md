# Tasks: Invoice Management Service

**Input**: Design documents from `/specs/001-invoice-service/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml, quickstart.md

**Tests**: Tests are included per TDD approach mandated by Constitution Principle III

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Based on MALIEV Co. Ltd. standard three-project microservice structure:
- **Api project**: `Maliev.InvoiceService.Api/`
- **Data project**: `Maliev.InvoiceService.Data/`
- **Tests project**: `Maliev.InvoiceService.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create solution structure with three projects: Maliev.InvoiceService.sln, Maliev.InvoiceService.Api, Maliev.InvoiceService.Data, Maliev.InvoiceService.Tests
- [X] T002 Add NuGet packages to Api project: Microsoft.AspNetCore.OpenApi 9.0.0, Microsoft.AspNetCore.Authentication.JwtBearer 9.0.8, Serilog.AspNetCore 8.0.2, FluentValidation 11.3.0, Asp.Versioning.Http 8.1.0, Scalar.AspNetCore 1.2.42, Prometheus.AspNetCore 8.2.1, StackExchange.Redis 9.0.0, MassTransit 8.3.4, MassTransit.RabbitMQ 8.3.4
- [X] T003 [P] Add NuGet packages to Data project: Microsoft.EntityFrameworkCore 9.0.10, Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4, Microsoft.EntityFrameworkCore.Tools 9.0.10
- [X] T004 [P] Add NuGet packages to Tests project: xUnit 2.6.6, FluentAssertions 8.6.0, Moq 4.20.72, Microsoft.AspNetCore.Mvc.Testing 9.0.0, Testcontainers 3.7.0, Testcontainers.PostgreSql 3.7.0
- [X] T005 [P] Create .dockerignore in repository root per CLAUDE.md template
- [X] T006 [P] Create .gitignore in repository root with .NET exclusions (.vs/, bin/, obj/, *.user)
- [X] T007 [P] Create Dockerfile in Maliev.InvoiceService.Api/ with multi-stage build per CLAUDE.md template
- [X] T008 [P] Create docker-compose.dev.yml for local PostgreSQL and Redis
- [X] T009 [P] Create docker-compose.test.yml for test PostgreSQL
- [X] T010 Configure TreatWarningsAsErrors in all .csproj files (zero warnings policy)
- [X] T011 [P] Create appsettings.json in Api project with placeholder configuration structure
- [X] T012 [P] Create appsettings.Development.json in Api project with localhost defaults

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Database Foundation

- [X] T013 Create InvoiceDbContext in Maliev.InvoiceService.Data/Data/InvoiceDbContext.cs with DbSet properties
- [X] T014 Create InvoiceDbContextFactory in Maliev.InvoiceService.Data/Data/InvoiceDbContextFactory.cs implementing IDesignTimeDbContextFactory (required for migrations)
- [X] T015 [P] Create AuditLogInterceptor in Maliev.InvoiceService.Data/Data/Interceptors/AuditLogInterceptor.cs for automatic audit trail
- [X] T016 [P] Create DatabaseMetricsInterceptor in Maliev.InvoiceService.Data/Data/Interceptors/DatabaseMetricsInterceptor.cs for Prometheus metrics

### Base Entities and Configurations

- [X] T017 Create Invoice entity in Maliev.InvoiceService.Data/Models/Invoice.cs with all properties from data-model.md
- [X] T018 [P] Create InvoiceLine entity in Maliev.InvoiceService.Data/Models/InvoiceLine.cs
- [X] T019 [P] **REMOVED** - Payment entity moved to Payment Service (architectural change)
- [X] T020 [P] Create InvoicePaymentAllocation entity in Maliev.InvoiceService.Data/Models/InvoicePaymentAllocation.cs (stores payment_id reference, NO FK)
- [X] T021 [P] Create AuditLog entity in Maliev.InvoiceService.Data/Models/AuditLog.cs
- [X] T022 Create InvoiceConfiguration in Maliev.InvoiceService.Data/Configurations/InvoiceConfiguration.cs implementing IEntityTypeConfiguration
- [X] T023 [P] Create InvoiceLineConfiguration in Maliev.InvoiceService.Data/Configurations/InvoiceLineConfiguration.cs
- [X] T024 [P] Create InvoicePaymentAllocationConfiguration in Maliev.InvoiceService.Data/Configurations/InvoicePaymentAllocationConfiguration.cs
- [X] T025 [P] Create AuditLogConfiguration in Maliev.InvoiceService.Data/Configurations/AuditLogConfiguration.cs
- [X] T026 Apply configurations in InvoiceDbContext.OnModelCreating using ApplyConfiguration
- [X] T027 Override SaveChangesAsync in InvoiceDbContext to implement manual RowVersion increment for PostgreSQL optimistic concurrency

### Initial Migration

- [X] T028 Create initial migration: dotnet ef migrations add InitialCreate --project Maliev.InvoiceService.Data
- [X] T029 Add invoice_number_seq sequence creation to migration Up() method
- [X] T029a [P] Create SQL script files in Maliev.InvoiceService.Data/Migrations/Scripts/ for reusable trigger DDL (update_updated_at_column.sql, prevent_finalized_deletion.sql) - can run in parallel with T028-T029
- [X] T030 Add database trigger update_updated_at_column() to migration Up() method per data-model.md lines 383-403 (references T029a SQL scripts)
- [X] T030a Add database trigger prevent_finalized_deletion() to migration Up() method per data-model.md lines 406-423 (depends on T029a SQL scripts, can run in parallel with T030)
- [X] T030b Apply triggers to tables: updated_at trigger on invoices and invoice_lines; prevent_finalized_deletion trigger on invoices (depends on T030 and T030a completion)

### Program.cs Core Configuration

- [X] T031 Create Program.cs in Maliev.InvoiceService.Api/ with Serilog configuration (console JSON only)
- [X] T032 Add Google Secret Manager configuration loading from /mnt/secrets in Program.cs
- [X] T033 Add DbContext registration with Npgsql and interceptors in Program.cs
- [X] T034 Add Memory Cache registration (simple AddMemoryCache without SizeLimit) in Program.cs
- [X] T035 Add Redis distributed cache registration with fallback to in-memory in Program.cs
- [X] T036 Add Health Checks registration with custom DatabaseHealthCheck in Program.cs
- [X] T037 Add API versioning configuration (Asp.Versioning) in Program.cs
- [X] T038 Add Scalar UI configuration (development only) in Program.cs
- [X] T038a Configure Scalar UI route at /invoices/scalar/v1 with OpenAPI route pattern /invoices/openapi/{documentName}.json in Program.cs per research.md lines 597-606

### JWT Authentication and Authorization

- [X] T039 Implement JWT Bearer authentication with double base64-encoded RSA public key in Program.cs per CLAUDE.md pattern
- [X] T040 Add authorization policies in Program.cs: Customer, Employee, Manager, Admin, EmployeeOrHigher per research.md
- [X] T041 Create TestAuthHandler in Maliev.InvoiceService.Tests/Fixtures/TestAuthHandler.cs returning Admin claims for testing

### Middleware and Pipeline

- [X] T042 [P] Create ExceptionHandlingMiddleware in Maliev.InvoiceService.Api/Middleware/ExceptionHandlingMiddleware.cs
- [X] T043 [P] Create CorrelationIdMiddleware in Maliev.InvoiceService.Api/Middleware/CorrelationIdMiddleware.cs
- [X] T044 [P] Create SecurityHeadersMiddleware in Maliev.InvoiceService.Api/Middleware/SecurityHeadersMiddleware.cs
- [X] T045 Configure middleware pipeline in Program.cs in EXACT mandatory order per CLAUDE.md (ExceptionHandling, CorrelationId, SecurityHeaders, HSTS, ResponseCompression, OpenAPI/Scalar, HTTPS, CORS, HttpMetrics, RateLimiter, Authentication, Authorization, HealthChecks, Metrics, Controllers)

### Rate Limiting

- [X] T046 Add global rate limiter configuration in Program.cs (100 req/min per user/IP) per CLAUDE.md pattern

### Common Models and Validators

- [X] T047 [P] Create PaginatedResponse<T> in Maliev.InvoiceService.Api/Models/Common/PaginatedResponse.cs
- [X] T048 [P] Create ErrorResponse in Maliev.InvoiceService.Api/Models/Common/ErrorResponse.cs
- [X] T049 [P] Create ExternalServiceOptions in Maliev.InvoiceService.Api/Models/Common/ExternalServiceOptions.cs

### Test Infrastructure

- [X] T050 Create TestDatabaseFixture in Maliev.InvoiceService.Tests/Fixtures/TestDatabaseFixture.cs with Testcontainers pattern per CLAUDE.md
- [X] T051 Create TestWebApplicationFactory in Maliev.InvoiceService.Tests/Fixtures/TestWebApplicationFactory.cs with TestAuthHandler
- [X] T052 Create DatabaseCollectionFixture in Maliev.InvoiceService.Tests/Fixtures/DatabaseCollectionFixture.cs with DisableParallelization attribute
- [X] T053 Implement ClearDatabaseAsync method in TestDatabaseFixture with PostgreSQL connection pool clearing per CLAUDE.md pattern

### External Service Clients Setup

- [X] T054 [P] Create ICurrencyServiceClient interface in Maliev.InvoiceService.Api/Services/External/ICurrencyServiceClient.cs
- [X] T055 [P] Create IQuotationServiceClient interface in Maliev.InvoiceService.Api/Services/External/IQuotationServiceClient.cs
- [X] T056 Implement CurrencyServiceClient in Maliev.InvoiceService.Api/Services/External/CurrencyServiceClient.cs with Polly v8 AddStandardResilienceHandler per research.md
- [X] T057 Implement QuotationServiceClient in Maliev.InvoiceService.Api/Services/External/QuotationServiceClient.cs with Polly v8 AddStandardResilienceHandler
- [X] T058 Register typed HttpClients for external services in Program.cs with 5-second timeout, 3 retries, circuit breaker

### Metrics and Observability

- [X] T059 [P] Create InvoiceMetrics class in Maliev.InvoiceService.Api/Services/InvoiceMetrics.cs with Counters (invoices_created_total, invoices_finalized_total, invoice_split_operations_total), Gauge (invoices_active_count), and Histogram (invoice_amount_thb) per research.md lines 754-774
- [X] T060 Add Prometheus HTTP metrics (UseHttpMetrics) in Program.cs middleware pipeline
- [X] T061 Map metrics endpoint at /invoices/metrics in Program.cs
- [X] T062 Force metrics initialization in Program.cs using RuntimeHelpers.RunClassConstructor per CLAUDE.md

### Health Checks

- [X] T063 [P] Create DatabaseHealthCheck in Maliev.InvoiceService.Api/Services/HealthChecks/DatabaseHealthCheck.cs
- [X] T064 [P] Create RedisHealthCheck in Maliev.InvoiceService.Api/Services/HealthChecks/RedisHealthCheck.cs
- [X] T065 Map liveness endpoint at /invoices/liveness in Program.cs
- [X] T066 Map readiness endpoint at /invoices/readiness with custom health checks in Program.cs

### launchSettings.json

- [X] T067 Create launchSettings.json in Maliev.InvoiceService.Api/Properties/launchSettings.json with launchUrl="invoices/scalar/v1" per CLAUDE.md

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Create Invoice from Quotation (Priority: P1) 🎯 MVP

**Goal**: Enable financial administrators to create invoices from approved quotations with pre-populated data, including currency conversion via Currency Service, and finalize invoices with unique sequential invoice numbers.

**Independent Test**: Create a quotation, generate an invoice from it, verify all fields are correctly populated, change currency to trigger exchange rate fetch, finalize the invoice, and verify it is immutable with an assigned invoice number.

### Tests for User Story 1 (TDD Approach)

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T068 [P] [US1] Contract test for POST /invoices/v1/invoices (create invoice from quotation) in Maliev.InvoiceService.Tests/Contract/InvoiceEndpointsTests.cs
- [X] T069 [P] [US1] Contract test for POST /invoices/v1/invoices/{id}/finalize in Maliev.InvoiceService.Tests/Contract/InvoiceEndpointsTests.cs
- [X] T070 [P] [US1] Contract test for GET /invoices/v1/invoices/{id} in Maliev.InvoiceService.Tests/Contract/InvoiceEndpointsTests.cs
- [X] T071 [P] [US1] Integration test for create invoice from quotation workflow in Maliev.InvoiceService.Tests/Integration/InvoiceCreationTests.cs
- [X] T072 [P] [US1] Integration test for currency conversion workflow in Maliev.InvoiceService.Tests/Integration/CurrencyConversionTests.cs
- [X] T073 [P] [US1] Unit test for CreateInvoiceRequestValidator in Maliev.InvoiceService.Tests/Unit/Validators/CreateInvoiceRequestValidatorTests.cs
- [X] T074 [P] [US1] Unit test for FinalizeInvoiceRequestValidator in Maliev.InvoiceService.Tests/Unit/Validators/FinalizeInvoiceRequestValidatorTests.cs

### Implementation for User Story 1

#### DTOs and Validators

- [X] T075 [P] [US1] Create CreateInvoiceRequest in Maliev.InvoiceService.Api/Models/Invoices/CreateInvoiceRequest.cs
- [X] T076 [P] [US1] Create InvoiceLineItemRequest in Maliev.InvoiceService.Api/Models/Invoices/InvoiceLineItemRequest.cs
- [X] T077 [P] [US1] Create InvoiceResponse in Maliev.InvoiceService.Api/Models/Invoices/InvoiceResponse.cs
- [X] T078 [P] [US1] Create InvoiceLineResponse in Maliev.InvoiceService.Api/Models/Invoices/InvoiceLineResponse.cs
- [X] T079 [US1] Create CreateInvoiceRequestValidator in Maliev.InvoiceService.Api/Validators/CreateInvoiceRequestValidator.cs with FluentValidation rules including async QuotationReferenceValid check
- [X] T080 [US1] Create InvoiceLineRequestValidator in Maliev.InvoiceService.Api/Validators/InvoiceLineRequestValidator.cs
- [X] T081 [US1] Register validators in Program.cs using AddValidatorsFromAssemblyContaining

#### Service Layer

- [X] T082 [US1] Create IInvoiceService interface in Maliev.InvoiceService.Api/Services/IInvoiceService.cs with CreateInvoiceAsync, GetInvoiceByIdAsync, FinalizeInvoiceAsync methods
- [X] T083 [US1] Create InvoiceService class skeleton in Maliev.InvoiceService.Api/Services/InvoiceService.cs with constructor dependencies (InvoiceDbContext, ILogger, IDistributedCache, ICurrencyServiceClient, InvoiceMetrics) - no method implementation yet
- [X] T084 [US1] Implement CreateInvoiceAsync method in InvoiceService with quotation reference handling and Currency Service integration (depends on T082, T083)
- [X] T085 [US1] Implement GetInvoiceByIdAsync method with AsNoTracking() for performance (depends on T082)
- [X] T086 [US1] Implement FinalizeInvoiceAsync method with PostgreSQL sequence-based invoice number generation per research.md (depends on T082)
- [X] T087 [US1] Implement CalculateTotals method for line items (quantity * unit_price * (1 - discount/100) * (1 + tax_rate/100))
- [X] T088 [US1] Add Redis caching in GetInvoiceByIdAsync with 24-hour TTL for finalized invoices per research.md
- [X] T089 [US1] Add cache invalidation in CreateInvoiceAsync and FinalizeInvoiceAsync
- [X] T090 [US1] Add InvoiceMetrics recording in FinalizeInvoiceAsync (invoices_finalized_total counter)
- [X] T091 [US1] Register InvoiceService in Program.cs with scoped lifetime

#### Controller

- [X] T092 [US1] Create InvoicesController in Maliev.InvoiceService.Api/Controllers/InvoicesController.cs with ApiController, ApiVersion("1.0"), and Route("invoices/v{version:apiVersion}/invoices") attributes per CLAUDE.md
- [X] T093 [US1] Implement POST /invoices/v1/invoices endpoint with [Authorize(Policy = "Employee")] returning 201 Created with Location header
- [X] T094 [US1] Implement GET /invoices/v1/invoices/{id} endpoint with caching support
- [X] T095 [US1] Implement POST /invoices/v1/invoices/{id}/finalize endpoint with [Authorize(Policy = "Manager")] and validation
- [X] T096 [US1] Add structured logging with correlation ID in all controller actions

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently. User can create invoices from quotations, fetch exchange rates, and finalize invoices with unique numbers.

---

## Phase 4: User Story 2 - Create Invoice Manually (Priority: P1)

**Goal**: Enable financial administrators to create invoices without quotation references, entering all data manually, with comprehensive validation and finalization.

**Independent Test**: Create an invoice from scratch without any quotation reference, entering all fields manually, verify validation errors for incomplete data, complete all fields, finalize, and verify invoice number assignment.

### Tests for User Story 2 (TDD Approach)

- [X] T097 [P] [US2] Contract test for POST /invoices/v1/invoices (create manual invoice) in Maliev.InvoiceService.Tests/Contract/InvoiceEndpointsTests.cs
- [X] T098 [P] [US2] Integration test for manual invoice creation with validation in Maliev.InvoiceService.Tests/Integration/InvoiceCreationTests.cs
- [X] T099 [P] [US2] Unit test for withholding tax calculation in Maliev.InvoiceService.Tests/Unit/Services/InvoiceServiceTests.cs

### Implementation for User Story 2

- [X] T100 [US2] Extend CreateInvoiceRequestValidator with withholding tax validation rules in Maliev.InvoiceService.Api/Validators/CreateInvoiceRequestValidator.cs
- [X] T101 [US2] Add CalculateWithholdingTax method in InvoiceService for withholding tax type and percentage handling in Maliev.InvoiceService.Api/Services/InvoiceService.cs
- [X] T102 [US2] Update CalculateTotals to include withholding tax in grand total calculation (subtotal + tax - withholding) in Maliev.InvoiceService.Api/Services/InvoiceService.cs
- [X] T103 [US2] Add validation in FinalizeInvoiceAsync to enforce all mandatory fields (customer tax ID, line descriptions, payment terms) per FR-025 to FR-029
- [X] T104 [US2] Add InvoiceMetrics recording in CreateInvoiceAsync (invoices_created_total counter)

**Checkpoint**: User Stories 1 AND 2 should both work independently. User can create invoices either from quotations or manually.

---

## Phase 5: User Story 3 - Split Invoice into Multiple Child Invoices (Priority: P2)

**Goal**: Enable financial administrators to split finalized invoices into multiple child invoices using percentage-based splits, with automatic reconciliation validation.

**Independent Test**: Create a finalized invoice, split it into 2-3 child invoices using percentage rules (e.g., 40%, 60%), verify each child references the parent, totals reconcile, and line items are proportionally distributed.

### Tests for User Story 3 (TDD Approach)

- [X] T105 [P] [US3] Contract test for POST /invoices/v1/invoices/{id}/split in Maliev.InvoiceService.Tests/Contract/InvoiceEndpointsTests.cs
- [X] T106 [P] [US3] Integration test for invoice splitting workflow in Maliev.InvoiceService.Tests/Integration/InvoiceSplittingTests.cs
- [X] T107 [P] [US3] Unit test for split reconciliation logic in Maliev.InvoiceService.Tests/Unit/Services/InvoiceServiceTests.cs

### Implementation for User Story 3

- [X] T108 [P] [US3] Create SplitInvoiceRequest in Maliev.InvoiceService.Api/Models/Invoices/SplitInvoiceRequest.cs
- [X] T109 [US3] Create SplitInvoiceRequestValidator in Maliev.InvoiceService.Api/Validators/SplitInvoiceRequestValidator.cs (percentages sum to 100)
- [X] T110 [US3] Add SplitInvoiceAsync method to IInvoiceService interface
- [X] T111 [US3] Implement SplitInvoiceAsync in InvoiceService with parent-child relationship and proportional line item distribution per research.md
- [X] T112 [US3] Implement rounding adjustment logic (apply to last child invoice) to ensure exact reconciliation
- [X] T113 [US3] Implement reconciliation validation (sum of child totals = parent total)
- [X] T114 [US3] Implement POST /invoices/v1/invoices/{id}/split endpoint in InvoicesController with [Authorize(Policy = "Manager")]
- [X] T115 [US3] Add audit log entry for Split event
- [X] T116 [US3] Add InvoiceMetrics recording for split operations

**Checkpoint**: User Stories 1, 2, AND 3 should all work independently. User can split invoices into staged billing.

---

## Phase 6: User Story 4 - Search and Retrieve Invoices (Priority: P2)

**Goal**: Enable users to search and filter invoices with multiple criteria (customer, status, currency, date ranges) with pagination and bulk export.

**Independent Test**: Create several invoices with varying attributes (different customers, statuses, currencies, dates), perform searches with various filter combinations, verify results are accurate and paginated correctly, test bulk export.

### Tests for User Story 4 (TDD Approach)

- [X] T117 [P] [US4] Contract test for GET /invoices/v1/invoices with query parameters in Maliev.InvoiceService.Tests/Contract/InvoiceEndpointsTests.cs
- [X] T118 [P] [US4] Integration test for invoice search with multiple filters in Maliev.InvoiceService.Tests/Integration/InvoiceSearchTests.cs
- [X] T119 [P] [US4] Unit test for search filter logic in Maliev.InvoiceService.Tests/Unit/Services/InvoiceServiceTests.cs

### Implementation for User Story 4

- [X] T120 [P] [US4] Create InvoiceSearchRequest in Maliev.InvoiceService.Api/Models/Invoices/InvoiceSearchRequest.cs with filter properties (customerName, status, currency, date ranges, pagination)
- [X] T121 [US4] Add SearchInvoicesAsync method to IInvoiceService interface returning PaginatedResponse<InvoiceResponse>
- [X] T122 [US4] Implement SearchInvoicesAsync in InvoiceService with AsNoTracking(), incremental Where() clauses, separate count query, and pagination per research.md
- [X] T123 [US4] Implement sorting logic (issue date, due date, grand total, invoice number)
- [X] T124 [US4] Implement cancelled invoice exclusion by default (unless includeCancelled filter is true)
- [X] T125 [US4] Update GET /invoices/v1/invoices endpoint in InvoicesController to call SearchInvoicesAsync
- [X] T126 [US4] Add Redis caching for search results with 5-minute TTL
- [X] T127 [US4] Implement bulk export functionality (CSV, JSON) for up to 1,000 invoices - CSV columns match InvoiceResponse DTO properties in order; JSON output is array of InvoiceResponse objects

**Checkpoint**: User can efficiently search and retrieve invoices using multiple filters with pagination.

---

## Phase 7: User Story 5 - Audit Trail and Immutability Enforcement (Priority: P2)

**Goal**: Enable auditors to review complete invoice history with all events (creation, edits, finalization, cancellation), enforce immutability of finalized invoices, and capture all modification attempts.

**Independent Test**: Create, edit, and finalize invoices, attempt unauthorized modifications, review audit log to verify all actions are captured with correct timestamps and actor identities.

### Tests for User Story 5 (TDD Approach)

- [X] T128 [P] [US5] Contract test for GET /invoices/v1/audit/invoices/{id} in Maliev.InvoiceService.Tests/Contract/AuditEndpointsTests.cs
- [X] T129 [P] [US5] Integration test for audit trail capture in Maliev.InvoiceService.Tests/Integration/AuditTrailTests.cs
- [X] T130 [P] [US5] Unit test for immutability enforcement in Maliev.InvoiceService.Tests/Unit/Services/InvoiceServiceTests.cs

### Implementation for User Story 5

- [X] T131 [P] [US5] Create AuditLogResponse in Maliev.InvoiceService.Api/Models/Audit/AuditLogResponse.cs
- [X] T132 [US5] Create AuditController in Maliev.InvoiceService.Api/Controllers/AuditController.cs with Route("invoices/v{version:apiVersion}/audit")
- [X] T133 [US5] Implement GET /invoices/v1/audit/invoices/{id} endpoint with [Authorize(Policy = "EmployeeOrHigher")]
- [X] T134 [US5] Verify AuditLogInterceptor captures Create, Update, Finalize events automatically
- [X] T135 [US5] Add immutability enforcement in InvoiceService UpdateInvoiceAsync method (reject if status is Finalized/Cancelled)
- [X] T136 [US5] Add audit log entry for attempted modifications to finalized invoices
- [X] T137 [US5] Add FilteredAuditLogsAsync method to InvoiceService excluding archived logs (is_archived = false)
- [X] T138 [US5] Create AuditArchivalService in Maliev.InvoiceService.Api/Services/BackgroundServices/AuditArchivalService.cs implementing BackgroundService
- [X] T139 [US5] Implement ExecuteAsync in AuditArchivalService to mark logs older than 1 year as archived (daily execution) per research.md
- [X] T140 [US5] Register AuditArchivalService in Program.cs using AddHostedService

**Checkpoint**: Complete audit trail is available for all invoices with 7-year retention compliance.

---

## Phase 8: User Story 6 - Invoice Cancellation (Priority: P3)

**Goal**: Enable financial administrators to cancel finalized invoices with a mandatory reason, update status, log the cancellation, and exclude from default searches.

**Independent Test**: Finalize an invoice, cancel it with a reason, verify status changes to Cancelled, audit log is updated, and invoice is excluded from active searches.

### Tests for User Story 6 (TDD Approach)

- [X] T141 [P] [US6] Contract test for POST /invoices/v1/invoices/{id}/cancel in Maliev.InvoiceService.Tests/Contract/InvoiceEndpointsTests.cs
- [X] T142 [P] [US6] Integration test for cancellation workflow in Maliev.InvoiceService.Tests/Integration/InvoiceCancellationTests.cs

### Implementation for User Story 6

- [X] T143 [P] [US6] Create CancelInvoiceRequest in Maliev.InvoiceService.Api/Models/Invoices/CancelInvoiceRequest.cs with reason property
- [X] T144 [US6] Create CancelInvoiceRequestValidator in Maliev.InvoiceService.Api/Validators/CancelInvoiceRequestValidator.cs (reason min 10 chars)
- [X] T145 [US6] Add CancelInvoiceAsync method to IInvoiceService interface
- [X] T146 [US6] Implement CancelInvoiceAsync in InvoiceService with status update, audit log, cache invalidation
- [X] T147 [US6] Implement POST /invoices/v1/invoices/{id}/cancel endpoint in InvoicesController with [Authorize(Policy = "Manager")]
- [X] T148 [US6] Verify SearchInvoicesAsync excludes cancelled invoices by default (already implemented in US4)

**Checkpoint**: Users can cancel invoices with proper audit trail.

---

## Phase 9: User Story 7 - Currency Conversion and Rate Storage (Priority: P3)

**Goal**: Enable multi-currency invoicing with automatic exchange rate fetching from Currency Service, rate persistence, and fallback handling.

**Independent Test**: Create invoices in multiple currencies (THB, USD, EUR), verify exchange rates are fetched and stored, confirm totals use stored rates even after Currency Service rates change.

### Tests for User Story 7 (TDD Approach)

- [X] T149 [P] [US7] Integration test for currency conversion with Currency Service unavailable in Maliev.InvoiceService.Tests/Integration/CurrencyConversionTests.cs
- [X] T150 [P] [US7] Unit test for exchange rate storage in Maliev.InvoiceService.Tests/Unit/Services/InvoiceServiceTests.cs

### Implementation for User Story 7

- [X] T151 [US7] Add GetExchangeRateAsync method to ICurrencyServiceClient
- [X] T152 [US7] Implement GetExchangeRateAsync in CurrencyServiceClient with Polly retry and circuit breaker
- [X] T153 [US7] Update CreateInvoiceAsync in InvoiceService to detect currency mismatch and call GetExchangeRateAsync
- [X] T154 [US7] Implement fallback rate handling (manual entry or defer finalization) when Currency Service is unavailable per FR-024
- [X] T155 [US7] Store exchange_rate and exchange_rate_source in Invoice entity
- [X] T156 [US7] Add currency conversion reporting methods to InvoiceService (original amount and converted amount)

**Checkpoint**: Multi-currency invoicing is fully functional with exchange rate persistence.

---

## Phase 10: User Story 8 - Allocate Payments to Invoices (Priority: P3)

**Goal**: Enable financial administrators to allocate payment references from Payment Service to invoices, updating invoice status and outstanding balance. Integrate with Payment Service via API validation and RabbitMQ event-driven auto-allocation.

n**Architecture Note**: Payment processing is owned by Payment Service. Invoice Service only tracks payment allocation references (NO FK constraints). Payment validation via Payment Service API required before allocation.

**Independent Test**: Mock Payment Service API and RabbitMQ events, create finalized invoices, allocate payment references, verify status updates, balance calculations, event publishing, and audit history.

### Tests for User Story 8 (TDD Approach)

- [X] T157 [P] [US8] Contract test for POST /invoices/v1/payments in Maliev.InvoiceService.Tests/Contract/PaymentEndpointsTests.cs
- [X] T158 [P] [US8] Contract test for GET /invoices/v1/payments/{id} in Maliev.InvoiceService.Tests/Contract/PaymentEndpointsTests.cs
- [X] T159 [P] [US8] Integration test for payment allocation workflow in Maliev.InvoiceService.Tests/Integration/PaymentAllocationTests.cs
- [X] T160 [P] [US8] Unit test for payment allocation validation in Maliev.InvoiceService.Tests/Unit/Services/PaymentServiceTests.cs

### Implementation for User Story 8

- [X] T161 [P] [US8] Create CreatePaymentRequest in Maliev.InvoiceService.Api/Models/Payments/CreatePaymentRequest.cs with allocations array
- [X] T162 [P] [US8] Create PaymentResponse in Maliev.InvoiceService.Api/Models/Payments/PaymentResponse.cs
- [X] T163 [US8] Create CreatePaymentRequestValidator in Maliev.InvoiceService.Api/Validators/CreatePaymentRequestValidator.cs (total allocation <= payment amount)
- [X] T164 [US8] Create IPaymentServiceClient interface in Maliev.InvoiceService.Api/Services/External/IPaymentServiceClient.cs for Payment Service API calls
- [X] T165 [US8] Implement PaymentServiceClient in Maliev.InvoiceService.Api/Services/External/PaymentServiceClient.cs with GetPaymentAsync and ValidatePaymentAsync
- [X] T166 [US8] Create PaymentSucceededEvent in Maliev.InvoiceService.Api/Models/Events/PaymentSucceededEvent.cs matching Payment Service schema
- [X] T167 [US8] Create PaymentAllocatedEvent in Maliev.InvoiceService.Api/Models/Events/PaymentAllocatedEvent.cs for Financial Service
- [X] T168 [US8] Create PaymentSucceededConsumer in Maliev.InvoiceService.Api/Services/Consumers/PaymentSucceededConsumer.cs implementing IConsumer<PaymentSucceededEvent>
- [X] T169 [US8] Implement AllocatePaymentAsync method in InvoiceService with Payment Service API validation and allocation logic
- [X] T170 [US8] Update invoice status to PartiallyPaid/FullyPaid based on InvoicePaymentAllocation records and set paid_at timestamp
- [X] T171 [US8] Implement CalculateOutstandingBalance method calculating grand_total - SUM(confirmed allocations)
- [X] T172 [US8] Configure MassTransit with RabbitMQ in Program.cs (subscribe to maliev.payments exchange, payment.succeeded routing key)
- [X] T173 [US8] Register PaymentServiceClient as typed HttpClient with Polly resilience handler in Program.cs
- [X] T174 [US8] Create PaymentsController in Maliev.InvoiceService.Api/Controllers/PaymentsController.cs with Route("invoices/v{version:apiVersion}/payments")
- [X] T175 [US8] Implement POST /invoices/v1/payments endpoint with [Authorize(Policy = "Employee")]
- [X] T176 [US8] Implement GET /invoices/v1/payments/{id} endpoint
- [X] T177 [US8] Add audit log entry for PaymentLinked event
- [X] T178 [US8] Add cache invalidation for invoices when payments are recorded

**Checkpoint**: Payment tracking and allocation is fully functional with invoice status updates.

---

## Phase 11: User Story 9 - Provide Data for PDF Generation and File Storage (Priority: P3)

**Goal**: Enable PDF Service to retrieve invoice data for rendering, and Upload Service to register PDF file references with invoices.

**Independent Test**: Mock PDF Service and Upload Service, query Invoice Service for invoice data, verify response format includes all required fields, simulate file reference registration.

### Tests for User Story 9 (TDD Approach)

- [X] T179 [P] [US9] Contract test for GET /invoices/v1/invoices/{id} verifying PDF-required fields in Maliev.InvoiceService.Tests/Contract/InvoiceEndpointsTests.cs
- [ ] T180 [P] [US9] Integration test for file reference registration in Maliev.InvoiceService.Tests/Integration/FileReferenceTests.cs

### Implementation for User Story 9

- [X] T181 [US9] Add pdf_file_reference property to Invoice entity in Maliev.InvoiceService.Data/Models/Invoice.cs
- [X] T186 [US9] Create UpdateInvoiceRequest in Maliev.InvoiceService.Api/Models/Invoices/UpdateInvoiceRequest.cs with rowVersion for optimistic concurrency
- [X] T183 [US9] Create UpdateInvoiceRequestValidator in Maliev.InvoiceService.Api/Validators/UpdateInvoiceRequestValidator.cs
- [X] T184 [US9] Add UpdateInvoiceAsync method to IInvoiceService interface
- [X] T185 [US9] Implement UpdateInvoiceAsync in InvoiceService with draft invoice update only (enforce immutability) and RowVersion validation
- [X] T186 [US9] Implement PUT /invoices/v1/invoices/{id} endpoint in InvoicesController
- [X] T187 [US9] Verify GET /invoices/v1/invoices/{id} returns all PDF-required fields (invoice number, customer details, line items, totals, taxes, payment terms, cancelled status)
- [X] T188 [US9] Add RegisterPdfFileReferenceAsync method to InvoiceService for Upload Service callback
- [X] T189 [US9] Implement PATCH /invoices/v1/invoices/{id}/pdf-reference endpoint (internal use only, no authorization required)

**Checkpoint**: PDF Service and Upload Service integration points are functional.

---

## Phase 12: Polish & Cross-Cutting Concerns

**Purpose**: Final improvements affecting multiple user stories, documentation, and deployment configuration

### Update Operations

- [X] T186 [P] Implement DELETE /invoices/v1/invoices/{id} endpoint for draft invoice soft deletion in InvoicesController (depends on UpdateInvoiceAsync)

### Bulk Operations and Export

- [X] T187 [P] Implement GET /invoices/v1/invoices/export endpoint for bulk CSV/JSON export in InvoicesController (depends on SearchInvoicesAsync)

### Analytics and BI Integration

- [X] T187a [P] Create GET /invoices/v1/analytics/summary endpoint in InvoicesController per FR-048 (invoice counts by status, total invoiced amounts, withholding tax totals, receivable aging, payment delays)

### Deferred Features Note

**Note**: FR-053 and FR-054 (Event Notifications via MassTransit/RabbitMQ) are marked as optional requirements and are deferred to a future release. Infrastructure is mentioned in plan.md but no implementation tasks are included in this phase.

### GitHub Actions CI/CD Workflows

- [X] T188 [P] Create .github/workflows/ci-develop.yml with PostgreSQL service container, test execution, Docker build, GitOps update per CLAUDE.md
- [X] T189 [P] Create .github/workflows/ci-staging.yml with staging environment configuration
- [X] T190 [P] Create .github/workflows/ci-main.yml with production environment configuration

### GitOps Deployment Manifests

- [X] T191 [P] Create maliev-gitops/3-apps/maliev-invoice-service/base/deployment.yaml with envFrom secretRef per CLAUDE.md
- [X] T192 [P] Create maliev-gitops/3-apps/maliev-invoice-service/base/service.yaml with app label
- [X] T193 [P] Create maliev-gitops/3-apps/maliev-invoice-service/base/servicemonitor.yaml for Prometheus scraping
- [X] T194 [P] Create maliev-gitops/3-apps/maliev-invoice-service/base/kustomization.yaml
- [X] T195 [P] Create maliev-gitops/3-apps/maliev-invoice-service/overlays/development/kustomization.yaml
- [X] T196 [P] Create maliev-gitops/3-apps/maliev-invoice-service/overlays/staging/kustomization.yaml
- [X] T197 [P] Create maliev-gitops/3-apps/maliev-invoice-service/overlays/production/kustomization.yaml

### Documentation

- [X] T198 [P] Create comprehensive README.md in repository root per CLAUDE.md template with all 13 required sections
- [ ] T199 [P] Verify quickstart.md works end-to-end by following all steps

### Validation and Cleanup

- [X] T200 Verify all tests pass: dotnet test Maliev.InvoiceService.sln --verbosity normal
- [X] T201 Verify zero build warnings: dotnet build Maliev.InvoiceService.sln --configuration Release
- [ ] T202 [P] Run Scalar UI validation in browser at https://localhost:5001/invoices/scalar/v1
- [ ] T203 [P] Verify all health checks return Healthy status
- [ ] T204 [P] Verify Prometheus metrics endpoint at http://localhost:5000/invoices/metrics
- [ ] T205 Code cleanup and removal of unused using statements
- [ ] T206 Verify Constitution compliance checklist (all gates passed)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-11)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P1 → P2 → P2 → P2 → P3 → P3 → P3 → P3)
- **Polish (Phase 12)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - Extends US1 validators and calculations but independently testable
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Requires US1 finalization to exist but is independently testable
- **User Story 4 (P2)**: Can start after Foundational (Phase 2) - Works with invoices from US1/US2 but independently testable
- **User Story 5 (P2)**: Can start after Foundational (Phase 2) - Audit infrastructure already in place from Phase 2, adds retrieval endpoints
- **User Story 6 (P3)**: Can start after Foundational (Phase 2) - Independent cancellation workflow
- **User Story 7 (P3)**: Can start after Foundational (Phase 2) - Currency Service integration already in US1, adds fallback handling
- **User Story 8 (P3)**: Can start after Foundational (Phase 2) - Independent payment workflow
- **User Story 9 (P3)**: Depends on US1 and US2 (invoice data must exist) - Adds PDF integration points

### Within Each User Story

- Tests (TDD approach) MUST be written and FAIL before implementation
- DTOs and validators before services
- Services before controllers
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel (T002-T004, T005-T007, T008-T009, T011-T012)
- All Foundational tasks marked [P] within each subsection can run in parallel
- Once Foundational phase completes, all user stories can start in parallel (if team capacity allows)
- All tests for a user story marked [P] can run in parallel
- DTOs, validators, and entities within a story marked [P] can run in parallel
- Different user stories can be worked on in parallel by different team members

---

## Parallel Example: User Story 1

```bash
# Write all tests for User Story 1 together (TDD approach - tests FIRST):
Task T068: "Contract test for POST /invoices/v1/invoices (create invoice from quotation)"
Task T069: "Contract test for POST /invoices/v1/invoices/{id}/finalize"
Task T070: "Contract test for GET /invoices/v1/invoices/{id}"
Task T071: "Integration test for create invoice from quotation workflow"
Task T072: "Integration test for currency conversion workflow"
Task T073: "Unit test for CreateInvoiceRequestValidator"
Task T074: "Unit test for FinalizeInvoiceRequestValidator"

# Verify all tests FAIL (no implementation yet)

# Launch all DTOs for User Story 1 together:
Task T075: "Create CreateInvoiceRequest"
Task T076: "Create InvoiceLineItemRequest"
Task T077: "Create InvoiceResponse"
Task T078: "Create InvoiceLineResponse"

# Then implement validators, service, controller sequentially (depend on DTOs)
```

---

## Implementation Strategy

### MVP First (User Stories 1 and 2 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (Create Invoice from Quotation)
4. Complete Phase 4: User Story 2 (Create Invoice Manually)
5. **STOP and VALIDATE**: Test both stories independently
6. Deploy/demo MVP (covers both primary invoice creation workflows)

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo
3. Add User Story 2 → Test independently → Deploy/Demo (MVP!)
4. Add User Story 3 → Test independently → Deploy/Demo (invoice splitting)
5. Add User Story 4 → Test independently → Deploy/Demo (search)
6. Add User Story 5 → Test independently → Deploy/Demo (audit trail)
7. Add User Stories 6-9 → Test independently → Deploy/Demo (additional features)
8. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together (critical path)
2. Once Foundational is done:
   - Developer A: User Story 1 (create from quotation)
   - Developer B: User Story 2 (create manually)
   - Developer C: User Story 4 (search)
3. Then:
   - Developer A: User Story 3 (split)
   - Developer B: User Story 5 (audit)
   - Developer C: User Story 8 (payments)
4. Stories complete and integrate independently

---

## Notes

- **Task ID Pattern**: Sequential numbering (T001, T002, ...) with alpha suffixes for related sub-tasks (T029a, T030a, T030b). Alpha suffixes indicate tasks that are closely related to their parent task and typically involve the same file or feature area.
- [P] tasks = different files, no dependencies, can run in parallel
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- TDD approach: Verify tests fail before implementing (Constitution Principle III)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Zero warnings policy enforced: dotnet build must produce zero warnings
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence

---

**Total Tasks**: 211 (updated after remediation: +5 tasks for SQL scripts, Scalar route, analytics endpoint)
**MVP Scope (US1 + US2)**: Tasks T001-T104 (107 tasks including T029a, T030a, T030b, T038a)
**Parallel Opportunities**: 73 tasks marked [P] can run in parallel within their phase
**Independent User Stories**: 9 stories, each testable independently after Foundational phase
**Suggested First Iteration**: Phase 1 (Setup) + Phase 2 (Foundational) + Phase 3 (US1) + Phase 4 (US2) = MVP

**Test-First Approach**: 37 test tasks (Contract, Integration, Unit) to be written BEFORE implementation per TDD

**Format Validation**: ✅ All 206 tasks follow checklist format with checkbox, ID, [P] marker (where applicable), [Story] label (where applicable), and exact file paths
