# Agentic Coding Guidelines - Maliev.InvoiceService

This document provides essential instructions for AI agents operating in this repository.

## Build, Test & Lint Commands

All commands run from within this service directory (`B:\maliev\Maliev.InvoiceService`).

```powershell
# Build (treats warnings as errors — all must be fixed)
dotnet build Maliev.InvoiceService.slnx

# Run all tests
dotnet test Maliev.InvoiceService.slnx --verbosity normal

# Run a single test method
dotnet test --filter "FullyQualifiedName~ClassName.MethodName_StateUnderTest_ExpectedBehavior"

# Run all tests in a class
dotnet test --filter "FullyQualifiedName~ClassName"

# Run with code coverage
dotnet test Maliev.InvoiceService.slnx --collect:"XPlat Code Coverage"

# Format check
dotnet format Maliev.InvoiceService.slnx

# EF Core migrations (Infrastructure/Data project only)
dotnet ef migrations add <Name> --project Maliev.InvoiceService.Infrastructure --startup-project Maliev.InvoiceService.Infrastructure
```

---

## Code Style & Conventions

### Workspace Structure
```
Maliev.InvoiceService/
├── Maliev.InvoiceService.Api/           # Controllers, Consumers, Middleware
├── Maliev.InvoiceService.Application/   # Use cases, DTOs, Interfaces, Handlers
├── Maliev.InvoiceService.Domain/        # Entities, value objects, domain interfaces
├── Maliev.InvoiceService.Infrastructure/ # EF Core DbContext, repositories, HTTP clients
├── Maliev.InvoiceService.Tests/         # Unit + Integration tests (xUnit)
├── specs/                               # API specifications and business rules
├── Directory.Build.props                # Central package versioning
└── Maliev.InvoiceService.slnx          # Solution file (.slnx preferred over .sln)
```

### C# Naming & Formatting
- **Namespaces**: File-scoped (`namespace Maliev.InvoiceService.Api.Services;`)
- **Classes/Methods/Properties**: `PascalCase`
- **Private fields**: `_camelCase` (underscore prefix)
- **Parameters/locals**: `camelCase`
- **Async methods**: Suffix with `Async` (e.g., `FinalizeInvoiceAsync`)
- **Interfaces**: Prefix with `I` (e.g., `IInvoiceService`)
- **Permissions**: GCP-style `{domain}.{plural-resource}.{action}` as `public const string` in a `Permissions` static class
  - Valid: `invoice.invoices.create`, `invoice.invoices.finalize`
  - Invalid: `invoice.invoice.create` (singular), `invoice.finalize` (missing resource)
- **XML docs**: Required on ALL public methods and properties
- **Nullable**: Enabled (`<Nullable>enable</Nullable>`). Use `?` explicitly
- **Imports**: System first, then third-party, then local. Alphabetize within groups. Remove unused `using`
- **Braces**: Allman style (new line) for methods and control structures. Expression-bodied for properties/accessors
- **Indentation**: 4 spaces, LF line endings, UTF-8, trim trailing whitespace

### C# Patterns
- **DI**: Constructor injection with `private readonly` fields
- **Controllers**: `[ApiController]`, `[ApiVersion("1")]`, `[Route("invoice/v{version:apiVersion}")]`
- **Logging**: `ILogger<T>` with structured placeholders (never interpolate): `_logger.LogInformation("Processing {InvoiceId}", invoiceId)`
- **Error handling**: Global exception middleware. Return `ProblemDetails` / `ErrorResponse` DTOs. Never expose stack traces
- **JSON**: Check existing conventions in this service for naming policy
- **Manual mapping**: Static extension methods (`ToDto()`, `ToEntity()`). AutoMapper is banned
- **Validation**: `System.ComponentModel.DataAnnotations` on DTOs. FluentValidation is banned

---

## Banned Libraries (Build Will Fail)

| Banned | Use Instead |
|--------|-------------|
| AutoMapper | Manual mapping extensions |
| FluentValidation | DataAnnotations or manual validation |
| FluentAssertions | Standard xUnit `Assert.*` |
| Swashbuckle/Swagger | Scalar (at `/invoice/scalar`) |
| InMemoryDatabase (EF Core) | Testcontainers with real PostgreSQL |

---

## Error Handling & Reliability

### Exception Strategy
- Use specific exceptions: `KeyNotFoundException` (404), `InvalidOperationException` (400/409), `ArgumentException` (400).
- Controllers should catch these and return appropriate `ActionResult` (e.g., `NotFound()`, `BadRequest()`, `Conflict()`).
- Log exceptions with `ILogger` including relevant context (e.g., `InvoiceId`).
- Use `ProblemDetails` for structured error responses in the API.

### Idempotency
- Critical mutations (e.g., `FinalizeInvoice`) must support idempotency.
- Use the `Idempotency-Key` HTTP header.
- Check for existing keys in the `IdempotencyKeys` table before executing logic.
- Idempotency records should be stored with the response status and payload.

### Database Compliance
- **Invoice Numbers**: Sequential numbering (INV-YYYYMMDD-XXXXXX) is backed by PostgreSQL sequences.
- **Gaps**: Gaps in numbering are acceptable per Thai Tax Law, but duplicates are not.
- **Manual Connections**: Use `_context.Database.GetDbConnection()` for raw SQL (like sequences) but manage state carefully (open/close manually in `finally` blocks).
- **Audit Interceptors**: EF Core interceptors automatically handle `CreatedAt` and `UpdatedAt` timestamps.

---

## Testing Rules

- **Framework**: xUnit with standard `Assert` (`Assert.Equal`, `Assert.NotNull`, etc.)
- **Naming**: `MethodName_StateUnderTest_ExpectedBehavior` or `HTTP_METHOD_Path_Scenario_ExpectedStatus`
- **Coverage**: Minimum 80% per service
- **Integration tests**: `BaseIntegrationTestFactory<TProgram, TDbContext>` with Testcontainers (PostgreSQL, Redis, RabbitMQ). Never InMemoryDatabase
- **System tests** (Tier 3): `AspireTestFixture` with `[Collection("AspireDomainTests")]` — shared AppHost, never one per class
- **Eventual consistency**: Use `TestHelpers.WaitForAsync`. Never `Task.Delay`
- **MassTransit consumers**: Must have consumer tests using `AddMassTransitTestHarness()`

### Testing Strategy (4-Tier Pyramid Context)

This service's tests cover **Tier 1 (Unit)** and **Tier 2 (Service Integration)** of the Maliev testing pyramid:

| Tier | What to Test | Infrastructure |
|------|-------------|---------------|
| **Unit** | Business logic, domain models, service methods with mocked dependencies | None (mocks only) |
| **Service Integration** | API endpoints, database persistence, permission enforcement, input validation | `BaseIntegrationTestFactory` + Testcontainers (Postgres/Redis/RabbitMQ) |

**Tier 3 (System Integration)** — cross-service workflows and event chains — is tested in `Maliev.Aspire.Tests/`.

#### Key Rules
- Use `BaseIntegrationTestFactory<TProgram, TDbContext>` for integration tests (real Testcontainers, never InMemoryDatabase)
- Every MassTransit consumer MUST have a consumer test using `services.AddMassTransitTestHarness()`
- Test naming: `MethodName_StateUnderTest_ExpectedBehavior`
- Minimum 80% code coverage
- Use `[Fact]` for single cases, `[Theory]` for parameterized tests

> Full ecosystem test strategy: `Maliev.Aspire.Tests/TEST_PLAN.md`

---

## Mandatory Rules

- **`TreatWarningsAsErrors = true`**: Zero warnings allowed. No suppression
- **`[RequirePermission("domain.resources.action")]`**: On all endpoints, not plain `[Authorize]`
- **API versioning**: All routes versioned (`v1/`)
- **Service prefix**: Routes prefixed with service domain (e.g., `/invoice`)
- **Scalar docs**: Configured at `/invoice/scalar`
- **Secrets**: Never hardcoded. Use GCP Secret Manager or environment variables
- **Async/await**: All the way down. Pass `CancellationToken`
- **EF Core Design package**: Only in Infrastructure project, never in Api
- **PostgreSQL xmin**: Shadow property only — `entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion()`. Never add entity property
- **Temporary files**: Generate in `/temp` folder, clean up afterwards

---

## Security & Secrets
- **No Secrets**: Never commit API keys, connection strings, or credentials. Use `UserSecrets` for local dev.
- **Environment Variables**: Use `IConfiguration` with environment variable overrides in production.
- **Permissions**: Use `[RequirePermission(InvoicePermissions.Name)]` on controller actions. Permissions follow the pattern `invoice.invoices.action`.
- **Creator role scope**: `roles.invoice.creator` is own-invoice scoped through `InvoiceAccessGuard` and `InvoiceAccessScope`. Any endpoint that accepts an invoice ID or returns invoice search results must preserve this guard and must include caller scope in any cache key.
- **Downstream owner contract**: Keep `InvoiceResponse.CreatedBy` populated from the creation audit entry. ReceiptService depends on it for creator-scoped receipt creation.
- **Cross-boundary DTOs**: Before changing controller requests, BFF callers, service clients, or MassTransit invoice/payment/PDF events, verify DTOs, JSON property names, and wire-shape tests on both sides of the boundary.

---

## Deployment & CI/CD
- **Pre-commit Hooks**: Run `dotnet format` and `verify build` before every commit.
- **Health Checks**: API must expose `/liveness` and `/readiness` endpoints.
- **Metrics**: Use `OpenTelemetry` for custom metrics (see `InvoiceMetrics.cs`).

---

## Git Rules

- Each `Maliev.*` folder is an independent git repo. `cd` into it before git commands
- **Commit early and often** after every meaningful unit of work. Do not accumulate changes
- **Never use `git checkout` to restore files** — commit first, then `git revert` or `git reset --soft`
- Feature branches merged to `develop` via PR. Do not push without being asked

---

## Database & EF Core — Mandatory Rules

### EF Core Design Package
- `Microsoft.EntityFrameworkCore.Design` MUST NOT be in Api projects
- It belongs ONLY in the Infrastructure (or Data) project where migrations live
- Migration commands must target Infrastructure as both project and startup-project:
  ```
  dotnet ef migrations add <Name> --project Maliev.InvoiceService.Infrastructure --startup-project Maliev.InvoiceService.Infrastructure
  ```

### PostgreSQL xmin Concurrency — Mandatory Pattern
Use shadow property ONLY. Never add a Xmin/xmin property to domain entities.
```csharp
entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion();
```
- Never use `UseXminAsConcurrencyToken()` (removed in Npgsql EF v7)
- Never use entity property `public uint Xmin { get; set; }` or `public uint xmin { get; set; }`
- Never use `.Ignore(e => e.Xmin)` — remove the entity property instead
