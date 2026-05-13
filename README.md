# Maliev Invoice Service

[![Build Status](https://img.shields.io/badge/Build-Passing-success)](https://github.com/ORGANIZATION/Maliev.InvoiceService)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Database](https://img.shields.io/badge/Database-PostgreSQL%2018-blue)](https://www.postgresql.org/)

Comprehensive microservice for managing sales invoices, quotations, and commercial payments.

**Role in MALIEV Architecture**: The primary financial gateway for revenue tracking. It orchestrates the transformation of quotations into invoices, tracks payment allocations, and maintains the authoritative commercial audit trail for the entire platform.

---

## 🏗️ Architecture & Tech Stack

- **Framework**: ASP.NET Core 10.0 (C# 13)
- **Database**: PostgreSQL 18 with Entity Framework Core 10.x
- **Distributed Cache**: Redis 7.x (High-speed invoice caching)
- **Messaging**: RabbitMQ via MassTransit
- **API Documentation**: OpenAPI 3.1 + Scalar UI
- **Observability**: OpenTelemetry (Metrics, Traces, Logging)

---

## ⚖️ Constitution Rules

This service strictly adheres to the platform development mandates:

### Banned Libraries
To maintain high performance and low complexity, the following are **NOT** used:
- ❌ **AutoMapper**: Explicit manual mapping only.
- ❌ **FluentValidation**: Standard Data Annotations (`[Required]`, `[EmailAddress]`) only.
- ❌ **FluentAssertions**: Standard xUnit `Assert` methods only.
- ❌ **In-memory Test DB**: All integration tests use **Testcontainers** with real PostgreSQL 18.

### Mandatory Practices
- ✅ **TreatWarningsAsErrors**: Enabled in all `.csproj` files.
- ✅ **XML Documentation**: Required on all public methods and properties.
- ✅ **No Secrets in Code**: All sensitive configuration injected via environment variables.
- ✅ **No Test Config in Program.cs**: Test configuration in test fixtures only.
- ✅ **IAM Integration**: Self-registers permissions with the IAM Service using GCP-style naming: `{service}.{resource}.{action}`.

---

## ✨ Key Features

- **Sequential Number Generation**: Guaranteed atomic invoice numbering (INV-YYYY-XXXXX) backed by database sequences.
- **Payment Reconciliation**: Precision allocation of payments to multiple invoices with dynamic status tracking.
- **Audit-First Design**: Automatic, immutable audit logging via interceptors for all commercial mutations.
- **Anti-Tamper Logic**: Database-level protection preventing the deletion or modification of finalized financial documents.
- **Multi-Currency Support**: Fully integrated with the platform's exchange rate providers for international invoicing.
- **Creator Ownership Scope**: `roles.invoice.creator` can create, read, update, split, and attach files only for invoices whose creation audit entry belongs to that principal.

---

## 🚀 Quick Start

### Prerequisites
- .NET 10.0 SDK
- Docker Desktop (for infrastructure)
- PostgreSQL 18 (Alpine)

### Local Development Setup

1. **Clone the repository**
```bash
git clone https://github.com/ORGANIZATION/Maliev.InvoiceService.git
cd Maliev.InvoiceService
```

2. **Spin up Infrastructure**
```bash
docker run --name invoice-db -e POSTGRES_PASSWORD=YOUR_PASSWORD -p 5432:5432 -d postgres:18-alpine
docker run --name invoice-redis -p 6379:6379 -d redis:7-alpine
```

3. **Configure Environment**
```powershell
# Windows PowerShell
$env:ConnectionStrings__InvoiceDbContext="YOUR_POSTGRES_CONNECTION_STRING"
$env:ConnectionStrings__Cache="YOUR_REDIS_CONNECTION_STRING"
```

4. **Apply Migrations & Run**
```bash
dotnet ef database update --project Maliev.InvoiceService.Infrastructure --startup-project Maliev.InvoiceService.Infrastructure
dotnet run --project Maliev.InvoiceService.Api
```

The service will be available at `http://localhost:5000/invoices`. Access the interactive documentation at `http://localhost:5000/invoices/scalar`.

---

## 📡 API Endpoints

All endpoints are prefixed with `/invoice/v1/`.

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/invoices` | Create a draft invoice |
| GET | `/invoices/{id}` | Retrieve detailed invoice details |
| POST | `/invoices/{id}/finalize` | Commit and finalize a draft invoice |
| POST | `/payments` | Record and allocate a payment |

---

## Authorization Model

- All controller actions use `[RequirePermission]` with `invoice.*` permission strings.
- `roles.invoice.admin`, `roles.invoice.manager`, and `roles.invoice.accountant` are unrestricted financial roles for their granted actions.
- `roles.invoice.creator` is intentionally object-scoped. Detail, search, update, delete, split, file, audit, currency-report, and payment-link routes check the invoice's `Created` audit log actor before returning or mutating data.
- Search cache keys include the caller's invoice access scope. Do not add new invoice search caches without including the effective caller scope when results are object-filtered.
- `InvoiceResponse.CreatedBy` is populated from the invoice creation audit entry so downstream services, including ReceiptService, can enforce their own creator-scoped workflows without guessing.
- Service-to-service PDF reference registration uses `invoice.files.register` and remains reserved for trusted internal service accounts.

---

## 🏥 Health & Monitoring

Standardized health probes for Kubernetes orchestration:
- **Liveness**: `GET /invoices/liveness`
- **Readiness**: `GET /invoices/readiness` (Checks DB and Redis connectivity)
- **Metrics**: `GET /invoices/metrics` (Prometheus format)

---

## 🧪 Testing

We prioritize reliable tests over mock-heavy unit tests.

```bash
# Run all tests using Testcontainers
dotnet test --verbosity normal
```

- **Integration Tests**: Use real PostgreSQL 18 containers.
- **Contract Tests**: Ensure API stability for consumers.

---

## 📦 Deployment

Infrastructure management is handled via GitOps patterns.

- **Docker Image**: `REGION-docker.pkg.dev/PROJECT_ID/REPOSITORY/maliev-invoice-service:{sha}`
- **Environments**: Development, Staging, Production

---

## 📄 License

Proprietary - © 2025 MALIEV Co., Ltd. All rights reserved.
