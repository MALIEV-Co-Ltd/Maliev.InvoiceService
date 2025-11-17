# Implementation Plan: Invoice Management Service

**Branch**: `001-invoice-service` | **Date**: 2025-11-11 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-invoice-service/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Create a comprehensive Invoice Management Service that serves as the single source of truth for all invoice-related data. The service handles invoice creation (from quotations or manual entry), invoice splitting, currency conversion, payment tracking, audit trails, and integration with PDF/Upload services. Built as a .NET 9 WebAPI microservice with PostgreSQL persistence, following MALIEV Co. Ltd.'s standardized architecture patterns including JWT authentication, role-based authorization, observability, and GitOps deployment.

## Technical Context

**Language/Version**: C# with .NET 10.0 SDK and ASP.NET Core 9.0
**Primary Dependencies**:
- Entity Framework Core 9.0.10 with Npgsql 9.0.4
- Serilog 8.0.2 for structured logging
- FluentValidation 11.3.0 for request validation
- Polly 8.5.0 with Microsoft.Extensions.Http.Resilience 9.0.0
- MassTransit 8.3.4 with RabbitMQ 7.0.0 (optional messaging)
- StackExchangeRedis 9.0.0 for distributed caching
- Prometheus.AspNetCore 8.2.1 for metrics
- Scalar 1.2.42 for API documentation

**Storage**: PostgreSQL 18 database with snake_case naming, EF Core migrations, optimistic concurrency via RowVersion
**Testing**: xUnit with FluentAssertions, Moq for mocking, Testcontainers for PostgreSQL integration tests, TestWebApplicationFactory pattern
**Target Platform**: Kubernetes (GKE) via ArgoCD GitOps, containerized with Docker (multi-stage build), non-root user execution
**Project Type**: Microservice WebAPI with three-project structure (Api, Data, Tests)
**Performance Goals**:
- <200ms p95 for cached invoice lookups
- <2s invoice finalization
- <1s search results (10k records paginated)
- 500+ concurrent read requests
- 95% of lookups from cache in <100ms

**Constraints**:
- Immutable finalized invoices (corrections via credit notes/amendments)
- Sequential invoice number generation with atomic database sequences
- 7-year audit log retention requirement
- Multi-currency support with fixed exchange rates at creation time
- No direct PDF rendering or file storage (delegated to separate services)
- 5-second timeout with 3 retries (exponential backoff) for external service calls

**Scale/Scope**:
- Support 500+ concurrent users
- Handle invoices with up to 50 line items
- Bulk export up to 1,000 invoices
- Search across 10,000+ invoice records
- Multi-currency invoicing with exchange rate persistence

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle III: Test-First Development (TDD)
- ✅ **PASS**: Tests will be written before implementation using TestWebApplicationFactory pattern
- ✅ **PASS**: PostgreSQL Testcontainers for real database behavior validation
- ✅ **PASS**: Contract tests for all API endpoints, integration tests for workflows, unit tests for validators and services
- ✅ **PASS**: Minimum 80% coverage for critical functionality

### Principle VI: Secrets Management
- ✅ **PASS**: All secrets via Google Secret Manager mounted at /mnt/secrets
- ✅ **PASS**: No secrets in source code, appsettings, or environment variables
- ✅ **PASS**: Connection strings follow ConnectionStrings__ServiceDbContext pattern
- ✅ **PASS**: JWT public key double base64-encoded from shared secrets

### Principle VII: Zero Warnings Policy
- ✅ **PASS**: TreatWarningsAsErrors enabled in all .csproj files
- ✅ **PASS**: Build must produce zero warnings (Debug and Release)
- ✅ **PASS**: CI/CD workflows fail on any warnings

### Principle VIII: Clean Artifacts
- ✅ **PASS**: Only project-specific files, no unused boilerplate
- ✅ **PASS**: .dockerignore excludes build artifacts, IDE files, specs
- ✅ **PASS**: .gitignore comprehensive with .vs/, bin/, obj/ exclusions

### Security Audit Requirements
- ✅ **PASS**: No production endpoints in public repository
- ✅ **PASS**: GitHub Actions workflows use mock service URLs (http://mock-service-name)
- ✅ **PASS**: appsettings.Development.json uses localhost only
- ✅ **PASS**: README uses placeholder values (<secret>, <password>)

### Architecture Compliance
- ✅ **PASS**: Clean Architecture pattern (Controllers → Services → Data)
- ✅ **PASS**: MANDATORY middleware pipeline order followed exactly
- ✅ **PASS**: Direct path prefixes in routes (NO UsePathBase per CRITICAL ROUTING LESSONS)
- ✅ **PASS**: Optimistic concurrency with manual RowVersion increment for PostgreSQL
- ✅ **PASS**: Audit trail via EF Core interceptor for 7-year retention
- ✅ **PASS**: Role-based authorization with operation-level permissions
- ✅ **PASS**: Global rate limiting (100 req/min per user/IP)

### Performance & Observability
- ✅ **PASS**: Prometheus metrics via UseHttpMetrics() + custom business metrics
- ✅ **PASS**: Correlation ID middleware for request tracking
- ✅ **PASS**: AsNoTracking() for all read-only queries
- ✅ **PASS**: Redis distributed cache with localhost fallback
- ✅ **PASS**: Custom health checks (Database, RabbitMQ, Redis, external services)

### CI/CD & Deployment
- ✅ **PASS**: Dockerfile in Api project with multi-stage build
- ✅ **PASS**: Three workflows (ci-develop.yml, ci-staging.yml, ci-main.yml)
- ✅ **PASS**: PostgreSQL 18 service container for tests
- ✅ **PASS**: GitOps via Kustomize + ArgoCD
- ✅ **PASS**: ServiceMonitor for Prometheus scraping

**Status**: ✅ ALL GATES PASSED - Proceed to Phase 0

## Project Structure

### Documentation (this feature)

```text
specs/001-invoice-service/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   ├── openapi.yaml     # OpenAPI 3.1 specification
│   ├── invoices.yaml    # Invoice endpoints contract
│   ├── payments.yaml    # Payment endpoints contract
│   └── audit.yaml       # Audit log endpoints contract
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
Maliev.InvoiceService/
├── .github/
│   └── workflows/
│       ├── ci-develop.yml
│       ├── ci-staging.yml
│       └── ci-main.yml
├── Maliev.InvoiceService.Api/
│   ├── Controllers/
│   │   ├── InvoicesController.cs
│   │   ├── PaymentsController.cs
│   │   ├── AuditController.cs
│   │   └── HealthController.cs
│   ├── Services/
│   │   ├── IInvoiceService.cs
│   │   ├── InvoiceService.cs
│   │   ├── IPaymentService.cs
│   │   ├── PaymentService.cs
│   │   ├── External/
│   │   │   ├── ICurrencyServiceClient.cs
│   │   │   ├── CurrencyServiceClient.cs
│   │   │   ├── IQuotationServiceClient.cs
│   │   │   └── QuotationServiceClient.cs
│   │   └── BackgroundServices/
│   │       └── AuditArchivalService.cs
│   ├── Models/
│   │   ├── Invoices/
│   │   │   ├── CreateInvoiceRequest.cs
│   │   │   ├── UpdateInvoiceRequest.cs
│   │   │   ├── FinalizeInvoiceRequest.cs
│   │   │   ├── CancelInvoiceRequest.cs
│   │   │   ├── SplitInvoiceRequest.cs
│   │   │   ├── InvoiceResponse.cs
│   │   │   └── InvoiceLineItemRequest.cs
│   │   ├── Payments/
│   │   │   ├── CreatePaymentRequest.cs
│   │   │   ├── PaymentResponse.cs
│   │   │   └── AllocatePaymentRequest.cs
│   │   ├── Audit/
│   │   │   └── AuditLogResponse.cs
│   │   └── Common/
│   │       ├── PaginatedResponse.cs
│   │       ├── ErrorResponse.cs
│   │       └── ExternalServiceOptions.cs
│   ├── Validators/
│   │   ├── CreateInvoiceRequestValidator.cs
│   │   ├── UpdateInvoiceRequestValidator.cs
│   │   ├── FinalizeInvoiceRequestValidator.cs
│   │   └── CreatePaymentRequestValidator.cs
│   ├── Middleware/
│   │   ├── ExceptionHandlingMiddleware.cs
│   │   ├── CorrelationIdMiddleware.cs
│   │   └── SecurityHeadersMiddleware.cs
│   ├── Program.cs
│   ├── Dockerfile
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Maliev.InvoiceService.Api.csproj
├── Maliev.InvoiceService.Data/
│   ├── Models/
│   │   ├── Invoice.cs
│   │   ├── InvoiceLine.cs
│   │   ├── Payment.cs
│   │   ├── AuditLog.cs
│   │   ├── WithholdingTax.cs
│   │   ├── PaymentTerm.cs
│   │   └── ExchangeRate.cs
│   ├── Configurations/
│   │   ├── InvoiceConfiguration.cs
│   │   ├── InvoiceLineConfiguration.cs
│   │   ├── PaymentConfiguration.cs
│   │   └── AuditLogConfiguration.cs
│   ├── Data/
│   │   ├── InvoiceDbContext.cs
│   │   ├── InvoiceDbContextFactory.cs
│   │   └── Interceptors/
│   │       ├── AuditLogInterceptor.cs
│   │       └── DatabaseMetricsInterceptor.cs
│   ├── Migrations/
│   └── Maliev.InvoiceService.Data.csproj
├── Maliev.InvoiceService.Tests/
│   ├── Contract/
│   │   ├── InvoiceEndpointsTests.cs
│   │   ├── PaymentEndpointsTests.cs
│   │   └── AuditEndpointsTests.cs
│   ├── Integration/
│   │   ├── InvoiceCreationTests.cs
│   │   ├── InvoiceSplittingTests.cs
│   │   ├── PaymentAllocationTests.cs
│   │   └── CurrencyConversionTests.cs
│   ├── Unit/
│   │   ├── Validators/
│   │   │   ├── CreateInvoiceRequestValidatorTests.cs
│   │   │   └── FinalizeInvoiceRequestValidatorTests.cs
│   │   └── Services/
│   │       ├── InvoiceServiceTests.cs
│   │       └── PaymentServiceTests.cs
│   ├── Fixtures/
│   │   ├── TestDatabaseFixture.cs
│   │   └── TestWebApplicationFactory.cs
│   ├── docker-compose.test.yml
│   └── Maliev.InvoiceService.Tests.csproj
├── .dockerignore
├── .gitignore
├── Maliev.InvoiceService.sln
└── README.md
```

**Structure Decision**: This follows the MALIEV Co. Ltd. standard three-project microservice pattern:
1. **Api project**: WebAPI controllers, services, models, validators, middleware, and Program.cs entry point
2. **Data project**: EF Core entities, configurations, DbContext, migrations, and interceptors
3. **Tests project**: Contract tests for API endpoints, integration tests for workflows, unit tests for validators/services, with TestDatabaseFixture and TestWebApplicationFactory patterns

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**Status**: No constitutional violations - all complexity is justified by requirements.

The implementation follows MALIEV Co. Ltd. standard patterns with no unnecessary abstraction layers. The three-project structure (Api, Data, Tests) is the company standard for microservices and is not considered additional complexity.

---

## Phase 1 Post-Design Constitution Re-Check

*Re-evaluated after completing data model, API contracts, and quickstart guide*

### Architecture & Design Validation
- ✅ **PASS**: Data model follows snake_case PostgreSQL naming conventions
- ✅ **PASS**: API contracts use OpenAPI 3.1 with comprehensive schemas
- ✅ **PASS**: All entities have proper indexes for query performance
- ✅ **PASS**: Optimistic concurrency via manual RowVersion increment (PostgreSQL-specific)
- ✅ **PASS**: Audit trail via EF Core interceptor (automatic, no missed events)
- ✅ **PASS**: Direct path prefixes in routes (NO UsePathBase per lessons learned)
- ✅ **PASS**: Scalar UI at `/invoices/scalar/v1` with proper configuration
- ✅ **PASS**: Health checks at `/invoices/liveness` and `/invoices/readiness`
- ✅ **PASS**: Metrics at `/invoices/metrics` for Prometheus

### Testing Strategy Validation
- ✅ **PASS**: TestDatabaseFixture with Testcontainers for real PostgreSQL
- ✅ **PASS**: TestWebApplicationFactory for API contract tests
- ✅ **PASS**: Three-tier testing: Contract, Integration, Unit
- ✅ **PASS**: FluentAssertions for readable test assertions
- ✅ **PASS**: Moq for external service mocking

### Performance & Scalability Validation
- ✅ **PASS**: AsNoTracking() for all read-only queries
- ✅ **PASS**: Redis distributed cache with fallback to in-memory
- ✅ **PASS**: Pagination with configurable page sizes (max 1000)
- ✅ **PASS**: Database sequences for atomic invoice number generation
- ✅ **PASS**: Composite indexes for multi-column queries

### Security & Compliance Validation
- ✅ **PASS**: JWT Bearer authentication with RSA public key validation
- ✅ **PASS**: Role-based authorization policies (Customer, Employee, Manager, Admin)
- ✅ **PASS**: Audit log with 7-year retention requirement
- ✅ **PASS**: Security headers middleware (X-Frame-Options, CSP, etc.)
- ✅ **PASS**: Rate limiting (100 req/min per user/IP)
- ✅ **PASS**: Correlation ID for distributed tracing

### External Dependencies Validation
- ✅ **PASS**: Polly v8 AddStandardResilienceHandler for Currency Service
- ✅ **PASS**: 5-second timeout with 3 retries and circuit breaker
- ✅ **PASS**: Typed HttpClient pattern for all external services
- ✅ **PASS**: Development fallback URLs for local testing

**Final Status**: ✅ ALL GATES PASSED - Design adheres to all constitutional requirements

---

## Phase 2: Task Generation (Next Command)

Phase 1 (planning) is now complete. To proceed with implementation:

```bash
# Generate actionable tasks from this plan
/speckit.tasks
```

This will create `tasks.md` with dependency-ordered implementation tasks based on the design artifacts produced in Phase 1.

---

**Planning Status**: ✅ COMPLETE
**Deliverables**:
- ✅ plan.md (this file) with Technical Context and Constitution Check
- ✅ research.md with 14 technical decisions documented
- ✅ data-model.md with complete entity definitions and EF Core configurations
- ✅ contracts/openapi.yaml with comprehensive API specification
- ✅ quickstart.md with step-by-step local development guide

**Next Steps**:
1. Run `/speckit.tasks` to generate implementation tasks
2. Review tasks.md for dependency order and estimates
3. Run `/speckit.implement` to begin TDD implementation
4. Follow red-green-refactor cycle: Tests → Fail → Implement → Pass
