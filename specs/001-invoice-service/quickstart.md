# Quickstart: Invoice Management Service

**Feature**: 001-invoice-service
**Date**: 2025-11-11

This guide provides step-by-step instructions for setting up and running the Invoice Management Service locally for development and testing.

---

## Prerequisites

### Required Software

- **.NET 10.0 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Docker Desktop**: [Download](https://www.docker.com/products/docker-desktop) (for PostgreSQL and Redis)
- **Git**: [Download](https://git-scm.com/downloads)
- **Visual Studio 2022** or **Visual Studio Code**: IDE with C# support

### Optional Software

- **Postman** or **Insomnia**: For API testing
- **kubectl**: For Kubernetes deployment (production)
- **pgAdmin**: PostgreSQL GUI client

---

## Step 1: Clone the Repository

```bash
git clone https://github.com/MALIEV-Co-Ltd/maliev-invoice-service.git
cd maliev-invoice-service
git checkout 001-invoice-service
```

---

## Step 2: Start Local PostgreSQL Database

### Using Docker Compose (Recommended)

Create `docker-compose.dev.yml`:

```yaml
version: '3.8'

services:
  postgres-dev:
    image: postgres:18
    container_name: invoice-postgres-dev
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: dev_password
      POSTGRES_DB: invoice_dev_db
    ports:
      - "5432:5432"
    volumes:
      - invoice-postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis-dev:
    image: redis:7-alpine
    container_name: invoice-redis-dev
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  invoice-postgres-data:
```

Start services:

```bash
docker-compose -f docker-compose.dev.yml up -d
```

Verify services are running:

```bash
docker-compose -f docker-compose.dev.yml ps
```

Expected output:
```
NAME                   IMAGE           STATUS
invoice-postgres-dev   postgres:18     Up (healthy)
invoice-redis-dev      redis:7-alpine  Up (healthy)
```

---

## Step 3: Configure Environment Variables

### Windows PowerShell

```powershell
# Database connection
$env:ConnectionStrings__InvoiceDbContext = "Server=localhost;Port=5432;Database=invoice_dev_db;Username=postgres;Password=dev_password;"

# Redis
$env:Redis__ConnectionString = "localhost:6379"
$env:Redis__Enabled = "true"

# RabbitMQ (Optional - disable for local dev)
$env:RabbitMq__Enabled = "false"

# JWT (Development keys - DO NOT use in production)
$env:Jwt__Issuer = "https://dev.api.maliev.com/auth"
$env:Jwt__Audience = "https://dev.api.maliev.com"
$env:Jwt__PublicKey = "LS0tLS1CRUdJTiBQVUJMSUMgS0VZLS0tLS0KTUlJQklqQU5CZ2txaGtpRzl3MEJBUUVGQUFPQ0FROEFNSUlCQ2dLQ0FRRUFxLy9iZ2RvQ3NMdTkyckFnc2lCQQpYbUJGdDQ5bGxhZXB4S1ROK2RLZjhRRCtsdmFQbVo4K1hiWUt4MzJqRE9OYjh6THhRb1UrVzVKaThIRkd0cFBoCmxVZ2RYY0FLZWlQQTFuVGRvMjJXUGVyRVlRdGpjNm9QUnRLRUZPT09PdmtEUUttT1hpR3BrV3JpVGhDOHZoSngKV3NkclBBOFNTcm9WdmJYY2E4OE0vL3B4TWxuTzhMejlQSzQxTFN2YzBGaEh6QmZTdzZob29VT0FYdmlJVkswMApFS0l5Y2lxd3FlYTFjdzBUY1ZGM1ZDVDdHRFJaU3E1N3hzaWdhVjFjYXVnZ1d0SmJYQU9xOXJXdjY2RndqY1FQCnAyNnhDMGFvcENPUzV0S1hwL0NacWtrL1pVNUQycE1Pdk52UFU0cU5RWWFabW1KL1ZSUFVSaUV2T3VyOWhrREEKTFFJREFRQUIKLS0tLS1FTkQgUFVCTElDIEtFWS0tLS0t"

# CORS
$env:CORS_ALLOWED_ORIGINS = "http://localhost:3000,http://localhost:5173"

# External Services (mock for local dev)
$env:ExternalServices__CurrencyService__BaseUrl = "http://localhost:8081"
$env:ExternalServices__CurrencyService__TimeoutSeconds = "5"
$env:ExternalServices__QuotationService__BaseUrl = "http://localhost:8082"
$env:ExternalServices__QuotationService__TimeoutSeconds = "5"
```

### Linux/macOS Bash

```bash
# Database connection
export ConnectionStrings__InvoiceDbContext="Server=localhost;Port=5432;Database=invoice_dev_db;Username=postgres;Password=dev_password;"

# Redis
export Redis__ConnectionString="localhost:6379"
export Redis__Enabled="true"

# RabbitMQ (Optional - disable for local dev)
export RabbitMq__Enabled="false"

# JWT (Development keys - DO NOT use in production)
export Jwt__Issuer="https://dev.api.maliev.com/auth"
export Jwt__Audience="https://dev.api.maliev.com"
export Jwt__PublicKey="LS0tLS1CRUdJTiBQVUJMSUMgS0VZLS0tLS0KTUlJQklqQU5CZ2txaGtpRzl3MEJBUUVGQUFPQ0FROEFNSUlCQ2dLQ0FRRUFxLy9iZ2RvQ3NMdTkyckFnc2lCQQpYbUJGdDQ5bGxhZXB4S1ROK2RLZjhRRCtsdmFQbVo4K1hiWUt4MzJqRE9OYjh6THhRb1UrVzVKaThIRkd0cFBoCmxVZ2RYY0FLZWlQQTFuVGRvMjJXUGVyRVlRdGpjNm9QUnRLRUZPT09PdmtEUUttT1hpR3BrV3JpVGhDOHZoSngKV3NkclBBOFNTcm9WdmJYY2E4OE0vL3B4TWxuTzhMejlQSzQxTFN2YzBGaEh6QmZTdzZob29VT0FYdmlJVkswMApFS0l5Y2lxd3FlYTFjdzBUY1ZGM1ZDVDdHRFJaU3E1N3hzaWdhVjFjYXVnZ1d0SmJYQU9xOXJXdjY2RndqY1FQCnAyNnhDMGFvcENPUzV0S1hwL0NacWtrL1pVNUQycE1Pdk52UFU0cU5RWWFabW1KL1ZSUFVSaUV2T3VyOWhrREEKTFFJREFRQUIKLS0tLS1FTkQgUFVCTElDIEtFWS0tLS0t"

# CORS
export CORS_ALLOWED_ORIGINS="http://localhost:3000,http://localhost:5173"

# External Services (mock for local dev)
export ExternalServices__CurrencyService__BaseUrl="http://localhost:8081"
export ExternalServices__CurrencyService__TimeoutSeconds="5"
export ExternalServices__QuotationService__BaseUrl="http://localhost:8082"
export ExternalServices__QuotationService__TimeoutSeconds="5"
```

---

## Step 4: Apply Database Migrations

Ensure PostgreSQL is running and environment variable is set:

```bash
# Build the solution first
dotnet build Maliev.InvoiceService.sln

# Apply migrations (creates tables, indexes, sequences)
dotnet ef database update --project Maliev.InvoiceService.Data --startup-project Maliev.InvoiceService.Api

# Verify migration
dotnet ef migrations list --project Maliev.InvoiceService.Data --startup-project Maliev.InvoiceService.Api
```

Expected output:
```
Build succeeded.
Applying migration '20250111000001_InitialCreate'.
Done.
```

---

## Step 5: Run the Application

### Option A: Using .NET CLI

```bash
cd Maliev.InvoiceService.Api
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### Option B: Using Visual Studio

1. Open `Maliev.InvoiceService.sln` in Visual Studio 2022
2. Set `Maliev.InvoiceService.Api` as the startup project
3. Press `F5` (Run with debugging) or `Ctrl+F5` (Run without debugging)
4. Browser will automatically open to `https://localhost:5001/invoices/scalar/v1` (Scalar UI)

### Option C: Using Visual Studio Code

1. Open repository folder in VS Code
2. Install **C# Dev Kit** extension
3. Press `F5` to launch with debugger
4. Navigate to `https://localhost:5001/invoices/scalar/v1` in browser

---

## Step 6: Verify Service is Running

### Health Checks

```bash
# Liveness (simple health check)
curl http://localhost:5000/invoices/liveness

# Expected response:
# {"status":"Healthy","service":"Invoice Service"}

# Readiness (checks database, Redis)
curl http://localhost:5000/invoices/readiness

# Expected response:
# {"status":"Healthy","checks":{"database":"Healthy","redis":"Healthy"}}
```

### Metrics Endpoint

```bash
curl http://localhost:5000/invoices/metrics
```

Expected output (Prometheus format):
```
# HELP process_cpu_seconds_total Total user and system CPU time spent in seconds.
# TYPE process_cpu_seconds_total counter
process_cpu_seconds_total 0.03
...
```

---

## Step 7: Explore the API with Scalar UI

Navigate to: **https://localhost:5001/invoices/scalar/v1**

Scalar UI provides:
- Interactive API documentation
- "Try It Out" functionality with example requests
- Request/response examples
- Schema validation
- Authentication token input

### Example: Create Invoice

1. Navigate to **POST /invoices/v1/invoices**
2. Click "Try It Out"
3. Use example request body:

```json
{
  "customerId": "123e4567-e89b-12d3-a456-426614174000",
  "customerName": "ACME Corporation Ltd.",
  "customerTaxId": "1234567890123",
  "billingAddress": "123 Main Street, Bangkok, Thailand 10110",
  "currency": "THB",
  "issueDate": "2025-01-11",
  "dueDate": "2025-02-10",
  "paymentTermsDays": 30,
  "lines": [
    {
      "lineNumber": 1,
      "itemCode": "PROD-001",
      "description": "3D Printed Prototype Model",
      "quantity": 5,
      "unitPrice": 2500.00,
      "discountPercentage": 0,
      "taxCategory": "VAT",
      "taxRate": 7.00
    }
  ]
}
```

4. Click "Send Request"
5. Verify response: `201 Created` with invoice details

---

## Step 8: Run Tests

### Prerequisites for Tests

Start test database:

```bash
docker-compose -f docker-compose.test.yml up -d
```

Set test environment variable:

```powershell
# Windows PowerShell
$env:ConnectionStrings__InvoiceDbContext = "Host=localhost;Port=5432;Database=test_db;Username=postgres;Password=postgres;"

# Linux/macOS
export ConnectionStrings__InvoiceDbContext="Host=localhost;Port=5432;Database=test_db;Username=postgres;Password=postgres;"
```

### Run All Tests

```bash
dotnet test Maliev.InvoiceService.sln --verbosity normal
```

Expected output:
```
Test run for Maliev.InvoiceService.Tests.dll (.NET 10.0)
Microsoft (R) Test Execution Command Line Tool Version 17.9.0

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:   150, Skipped:     0, Total:   150, Duration: 15 s
```

### Run Specific Test Categories

```bash
# Contract tests only
dotnet test --filter "Category=Contract"

# Integration tests only
dotnet test --filter "Category=Integration"

# Unit tests only
dotnet test --filter "Category=Unit"
```

---

## Step 9: Sample Workflow

### Create, Finalize, and Split an Invoice

```bash
# 1. Create draft invoice
curl -X POST http://localhost:5000/invoices/v1/invoices \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <your-jwt-token>" \
  -d '{
    "customerId": "123e4567-e89b-12d3-a456-426614174000",
    "customerName": "Test Customer Ltd.",
    "customerTaxId": "1234567890123",
    "billingAddress": "123 Test Street, Bangkok 10110",
    "currency": "THB",
    "issueDate": "2025-01-11",
    "dueDate": "2025-02-10",
    "lines": [
      {
        "lineNumber": 1,
        "description": "Product A",
        "quantity": 10,
        "unitPrice": 5000.00,
        "taxCategory": "VAT",
        "taxRate": 7.00
      }
    ]
  }'

# Response: 201 Created with invoice ID (e.g., abc-123-def)

# 2. Finalize invoice (assigns invoice number, locks record)
curl -X POST http://localhost:5000/invoices/v1/invoices/abc-123-def/finalize \
  -H "Authorization: Bearer <your-jwt-token>"

# Response: 200 OK with invoice number INV-2025-00001

# 3. Split invoice into 2 child invoices (40% and 60%)
curl -X POST http://localhost:5000/invoices/v1/invoices/abc-123-def/split \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <your-jwt-token>" \
  -d '{
    "splits": [
      {"percentage": 40.0},
      {"percentage": 60.0}
    ]
  }'

# Response: 201 Created with 2 child invoice IDs

# 4. Get audit trail
curl http://localhost:5000/invoices/v1/audit/invoices/abc-123-def \
  -H "Authorization: Bearer <your-jwt-token>"

# Response: Complete audit trail with all events
```

---

## Troubleshooting

### Issue: "Unable to connect to PostgreSQL"

**Solution**:
1. Verify PostgreSQL is running: `docker ps | grep postgres`
2. Check connection string environment variable
3. Test connection: `psql -h localhost -U postgres -d invoice_dev_db`
4. Check PostgreSQL logs: `docker logs invoice-postgres-dev`

### Issue: "Migration fails with 'Database does not exist'"

**Solution**:
```bash
# Manually create database
docker exec -it invoice-postgres-dev psql -U postgres -c "CREATE DATABASE invoice_dev_db;"

# Retry migration
dotnet ef database update --project Maliev.InvoiceService.Data
```

### Issue: "Tests fail with 'Table does not exist'"

**Solution**:
1. Ensure test database is running: `docker-compose -f docker-compose.test.yml up -d`
2. Set correct environment variable for tests
3. TestDatabaseFixture auto-applies migrations, but verify:
   ```bash
   docker exec -it postgres-test psql -U postgres -d test_db -c "\dt"
   ```

### Issue: "Scalar UI shows 'Failed to load OpenAPI spec'"

**Solution**:
1. Ensure service is running in Development environment
2. Check `ASPNETCORE_ENVIRONMENT=Development`
3. Verify route: `https://localhost:5001/invoices/scalar/v1`
4. Check browser console for errors

### Issue: "Redis connection fails"

**Solution**:
```bash
# Verify Redis is running
docker ps | grep redis

# Test Redis connection
docker exec -it invoice-redis-dev redis-cli ping
# Expected: PONG

# Disable Redis for local dev if not needed
$env:Redis__Enabled = "false"
```

---

## Development Workflow Best Practices

### 1. Branch Strategy

```bash
# Always work on feature branches
git checkout -b feature/your-feature-name

# Keep main and develop branches clean
git fetch origin
git rebase origin/develop
```

### 2. Code Changes

1. Make code changes
2. Run tests: `dotnet test`
3. Check for warnings: `dotnet build`
4. Format code: `dotnet format`

### 3. Database Schema Changes

```bash
# Create new migration
dotnet ef migrations add MigrationName --project Maliev.InvoiceService.Data

# Review generated migration file
# Edit migration if needed (e.g., add indexes, triggers)

# Apply migration locally
dotnet ef database update --project Maliev.InvoiceService.Data

# Test migration rollback
dotnet ef database update PreviousMigration --project Maliev.InvoiceService.Data
dotnet ef database update --project Maliev.InvoiceService.Data
```

### 4. Debugging

- Use Visual Studio or VS Code debugger with breakpoints
- Check logs: Serilog outputs to console in JSON format
- Monitor metrics: `curl http://localhost:5000/invoices/metrics | grep invoice`
- Trace requests: Check `X-Correlation-ID` header in responses

---

## Next Steps

1. **Review API Contracts**: See `/specs/001-invoice-service/contracts/openapi.yaml`
2. **Review Data Model**: See `/specs/001-invoice-service/data-model.md`
3. **Read Research Document**: See `/specs/001-invoice-service/research.md`
4. **Explore Tests**: Browse `/Maliev.InvoiceService.Tests/` directory
5. **Configure IDE**: Set up launch configurations for debugging
6. **Install pgAdmin**: GUI tool for inspecting PostgreSQL database

---

## Additional Resources

- **OpenAPI Specification**: `/specs/001-invoice-service/contracts/openapi.yaml`
- **Scalar API Documentation**: `https://localhost:5001/invoices/scalar/v1`
- **Prometheus Metrics**: `http://localhost:5000/invoices/metrics`
- **Health Checks**: `http://localhost:5000/invoices/liveness` and `/invoices/readiness`
- **.NET 9 Documentation**: [https://learn.microsoft.com/en-us/dotnet/](https://learn.microsoft.com/en-us/dotnet/)
- **Entity Framework Core**: [https://learn.microsoft.com/en-us/ef/core/](https://learn.microsoft.com/en-us/ef/core/)
- **FluentValidation**: [https://docs.fluentvalidation.net/](https://docs.fluentvalidation.net/)
- **Polly**: [https://www.thepollyproject.org/](https://www.thepollyproject.org/)

---

**Status**: ✅ Quickstart Guide Complete
**Last Updated**: 2025-01-11
