# Maliev Invoice Management Service

Comprehensive microservice for managing invoices, quotations, payments, and audit trails. Built with .NET 10.0, PostgreSQL 18, and following MALIEV Co. Ltd.'s standardized architecture patterns.

## Status

**Current Implementation**: MVP Foundation Complete (Phases 1-3)

### ✅ Phase 1: Setup (T001-T012) - COMPLETE
- ✅ Three-project solution structure (Api, Data, Tests)
- ✅ NuGet packages installed (EF Core 9.0.10, Npgsql 9.0.4, Serilog 8.0.3, etc.)
- ✅ Docker files (.dockerignore, Dockerfile, docker-compose.dev.yml, docker-compose.test.yml)
- ✅ TreatWarningsAsErrors enabled (zero warnings policy)
- ✅ appsettings.json and appsettings.Development.json configured

### ✅ Phase 2: Foundational Infrastructure (T013-T067) - COMPLETE
- ✅ **Database Models**: Invoice, InvoiceLine, Payment, InvoicePayment, AuditLog
- ✅ **EF Core Configurations**: All entities with PostgreSQL snake_case naming, indexes, relationships
- ✅ **InvoiceDbContext**: Manual RowVersion increment for PostgreSQL optimistic concurrency
- ✅ **Initial Migration**: Includes sequences (invoice_number_seq) and triggers (updated_at, prevent_finalized_deletion)
- ✅ **Interceptors**: AuditLogInterceptor (automatic audit logging), DatabaseMetricsInterceptor
- ✅ **Program.cs**: Serilog, DbContext, Redis cache, JWT auth, health checks, rate limiting
- ✅ **Common Models**: PaginatedResponse, ErrorResponse

### ✅ Phase 3: User Story 1 - Create Invoice from Quotation (T068-T096) - CORE COMPLETE
- ✅ **DTOs**: CreateInvoiceRequest, InvoiceResponse, InvoiceLineItemRequest
- ✅ **Service**: IInvoiceService interface and implementation with:
  - CreateInvoiceAsync (with line items and calculations)
  - GetInvoiceByIdAsync
  - GetPaginatedInvoicesAsync (with filtering)
  - FinalizeInvoiceAsync (sequential invoice number generation)
  - CancelInvoiceAsync
  - UpdateInvoiceAsync
  - DeleteInvoiceAsync (soft delete)
- ✅ **Controller**: InvoicesController with full CRUD + finalize + cancel operations
- ✅ **API Versioning**: v1.0 with URL segment versioning

### ⏸️ Phase 4-12: Pending Implementation (T097-T206)
- Phase 4: US2 - Manual Invoice Creation (T097-T104)
- Phase 5: US3 - Invoice Splitting (T105-T116)
- Phase 6: US4 - Search and Retrieve (T117-T127)
- Phase 7: US5 - Audit Trail (T128-T140)
- Phase 8: US6 - Cancellation (T141-T148)
- Phase 9: US7 - Currency Conversion (T149-T156)
- Phase 10: US8 - Link Payments (T157-T174)
- Phase 11: US9 - PDF Integration (T175-T185)
- Phase 12: Polish & Cross-Cutting (T186-T206) - CI/CD, GitOps, documentation

## Architecture

### Technology Stack
- **Framework**: .NET 10.0 with ASP.NET Core WebAPI
- **Database**: PostgreSQL 18 with EF Core 9.0.10 (forward-compatible)
- **Database Provider**: Npgsql 9.0.4 (stable, .NET 10 compatible)
- **Caching**: Redis distributed cache with in-memory fallback
- **Logging**: Serilog 8.0.3 (JSON console output)
- **Authentication**: JWT Bearer with RSA public key validation
- **API Documentation**: OpenAPI 3.1 with Scalar UI
- **Testing**: xUnit, FluentAssertions, Moq, Testcontainers 3.10.0

### Project Structure
```
Maliev.InvoiceService/
├── Maliev.InvoiceService.Api/          # WebAPI project
│   ├── Controllers/                     # API controllers
│   ├── Services/                        # Business logic services
│   ├── Models/                          # DTOs (Invoices, Payments, etc.)
│   ├── Program.cs                       # Application entry point
│   └── appsettings.json
├── Maliev.InvoiceService.Data/          # Data access layer
│   ├── Models/                          # EF Core entities
│   ├── Configurations/                  # Fluent API configurations
│   ├── Data/                            # DbContext and interceptors
│   └── Migrations/                      # EF Core migrations
└── Maliev.InvoiceService.Tests/         # Test project
    ├── Contract/                        # API endpoint tests
    ├── Integration/                     # Workflow tests
    └── Unit/                            # Service/validator tests
```

### Database Schema

**Entities**:
- `invoices`: Core invoice entity with status, amounts, dates
- `invoice_lines`: Line items with quantities, prices, tax calculations
- `payments`: Payment records
- `invoice_payments`: Junction table for payment allocations
- `audit_logs`: Comprehensive audit trail (7-year retention)

**Key Features**:
- PostgreSQL sequence `invoice_number_seq` for sequential numbering (INV-2025-00001)
- Triggers for `updated_at` timestamp management
- Trigger to prevent deletion of finalized invoices
- Optimistic concurrency with manual RowVersion increment (PostgreSQL-specific)
- Automatic audit logging via EF Core interceptor

## Quick Start

### Prerequisites
- .NET 10.0 SDK
- Docker (for PostgreSQL and Redis)
- PostgreSQL 18 (or use Docker Compose)

### Running Locally

1. **Start PostgreSQL and Redis**:
```bash
docker-compose -f docker-compose.dev.yml up -d
```

2. **Apply database migrations**:
```bash
export InvoiceDbContext="Server=localhost;Port=5432;Database=invoice_db;User Id=postgres;Password=postgres_dev_password;"
dotnet ef database update --project Maliev.InvoiceService.Data --startup-project Maliev.InvoiceService.Api
```

3. **Run the application**:
```bash
dotnet run --project Maliev.InvoiceService.Api
```

4. **Access the API**:
- OpenAPI: http://localhost:5000/openapi/v1.json
- Health checks:
  - Liveness: http://localhost:5000/invoices/liveness
  - Readiness: http://localhost:5000/invoices/readiness

### Example API Usage

**Create a draft invoice**:
```bash
curl -X POST http://localhost:5000/invoice/v1/invoices \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "00000000-0000-0000-0000-000000000001",
    "customerName": "Acme Corporation",
    "customerTaxId": "1234567890",
    "billingAddress": "123 Main St, Bangkok, Thailand",
    "currency": "THB",
    "issueDate": "2025-11-11",
    "dueDate": "2025-12-11",
    "paymentTermsDays": 30,
    "lines": [
      {
        "lineNumber": 1,
        "description": "3D Printing Service",
        "quantity": 10,
        "unitPrice": 1000.00,
        "discountPercentage": 0,
        "taxRate": 7.00
      }
    ]
  }'
```

**Finalize invoice** (assigns sequential invoice number):
```bash
curl -X POST http://localhost:5000/invoice/v1/invoices/{id}/finalize
```

**Get paginated invoices**:
```bash
curl http://localhost:5000/invoice/v1/invoices?page=1&pageSize=20&status=Draft
```

## Configuration

### Required Environment Variables (Production)

The service expects secrets to be mounted at `/mnt/secrets` (Google Secret Manager pattern):
- `InvoiceDbContext`: PostgreSQL connection string
- `Redis__Configuration`: Redis connection string
- `JwtSettings__PublicKeyBase64`: Double base64-encoded RSA public key

### Development Settings

See `appsettings.Development.json` for localhost defaults.

## Key Design Decisions

1. **PostgreSQL-Specific Patterns**:
   - Manual RowVersion increment (bytea doesn't auto-increment like SQL Server)
   - Snake_case naming convention
   - Database sequences for atomic invoice numbering

2. **Audit Trail**:
   - EF Core SaveChanges interceptor for automatic audit logging
   - Captures Created, Updated, Finalized, Cancelled events
   - 7-year retention requirement

3. **Immutable Finalized Invoices**:
   - Database trigger prevents deletion of finalized invoices
   - Corrections must be made via credit notes/amendments (future feature)

4. **API Versioning**:
   - URL segment versioning (`/invoice/v1/...`)
   - Direct path prefixes (NO UsePathBase per MALIEV routing lessons)

## Testing

```bash
# Run all tests
dotnet test Maliev.InvoiceService.Tests

# Run with coverage
dotnet test Maliev.InvoiceService.Tests --collect:"XPlat Code Coverage"
```

## Build and Deploy

```bash
# Build solution
dotnet build Maliev.InvoiceService.sln

# Build Docker image
docker build -t maliev-invoice-service:latest -f Maliev.InvoiceService.Api/Dockerfile .

# Run in Docker
docker run -p 8080:8080 \
  -e InvoiceDbContext="Server=host.docker.internal;Port=5432;Database=invoice_db;..." \
  maliev-invoice-service:latest
```

## Next Steps

To complete the MVP, implement:
1. US2: Manual invoice creation (reuses existing infrastructure)
2. CI/CD workflows (ci-develop.yml, ci-staging.yml, ci-main.yml)
3. GitOps manifests (Kustomize + ArgoCD)
4. Integration tests with Testcontainers
5. US3-US9: Remaining user stories (splitting, payments, PDF, etc.)

## License

Copyright © 2025 MALIEV Co. Ltd. All rights reserved.
