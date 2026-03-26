# Agentic Coding Guidelines - Maliev.InvoiceService

This document provides essential instructions for AI agents operating in this repository.

## 🛠 Build and Development

### Core Commands
- **Build Solution**: `dotnet build Maliev.InvoiceService.slnx`
- **Run API**: `dotnet run --project Maliev.InvoiceService.Api`
- **Run All Tests**: `dotnet test Maliev.InvoiceService.slnx --verbosity normal`
- **Format Code**: `dotnet format Maliev.InvoiceService.slnx`
- **Update Database**: `dotnet ef database update --project Maliev.InvoiceService.Infrastructure --startup-project Maliev.InvoiceService.Infrastructure`
- **Add Migration**: `dotnet ef migrations add <Name> --project Maliev.InvoiceService.Infrastructure --startup-project Maliev.InvoiceService.Infrastructure`

### Running Tests
- **Single Test**: `dotnet test --filter "Fully.Qualified.Namespace.ClassName.TestMethodName"`
- **Tests in Class**: `dotnet test --filter "ClassName"`
- **Integration Tests**: Require Docker/Testcontainers. Ensure Docker is running.
- **Test Results**: Coverage reports are stored in `coverage/`.

---

## 📏 Code Style and Conventions

### Architecture & Libraries
- **No AutoMapper**: Perform explicit manual mapping between Entities and DTOs.
- **No FluentValidation**: Use standard `System.ComponentModel.DataAnnotations` (e.g., `[Required]`, `[StringLength]`).
- **No FluentAssertions**: Use standard xUnit `Assert` methods (e.g., `Assert.Equal()`).
- **Entity Framework**: Use EF Core 10.x. All integration tests MUST use `Testcontainers` with PostgreSQL 18.

### C# Language Guidelines
- **Language Version**: C# 13 (.NET 10.0).
- **Namespaces**: Use file-scoped namespaces: `namespace Maliev.InvoiceService.Api.Services;`.
- **Async/Await**: All asynchronous methods must have the `Async` suffix and accept a `CancellationToken`. Always use `ConfigureAwait(false)` in library/data projects, though less critical in ASP.NET Core 10.
- **Documentation**: XML documentation is MANDATORY for all public classes, methods, and properties.
  - Example: `/// <summary>Brief description</summary>`
- **Warnings**: `TreatWarningsAsErrors` is enabled. Code must compile without warnings.

### Naming Conventions
- **Classes/Interfaces**: `PascalCase`. Interfaces prefixed with `I` (e.g., `IInvoiceService`).
- **Methods**: `PascalCase`.
- **Properties**: `PascalCase`.
- **Private Fields**: `_camelCase` with underscore prefix (e.g., `_invoiceContext`).
- **Local Variables**: `camelCase`. Use `var` when the type is obvious from the right-hand side (e.g., `var list = new List<string>();`).
- **Constants**: `PascalCase` or `UPPER_SNAKE_CASE` depending on context (prefer `PascalCase` for public constants).

### Formatting
- **Indentation**: 4 spaces.
- **Braces**: Allman style (braces on new lines).
- **Line Length**: Aim for < 120 characters where possible.
- **Imports**: Organize using statements: System first, then Microsoft, then Third-party, then Internal.

---

## 🛡 Error Handling & Reliability

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

## 🏗 Project Structure

- `Maliev.InvoiceService.Api`: ASP.NET Core API controllers, DTOs, and business services.
- `Maliev.InvoiceService.Data`: EF Core DbContext, Migrations, and Database Models.
- `Maliev.InvoiceService.Tests`: Unit, Integration, and Contract tests.
- `specs/`: Contains API specifications and business rules.

---

## 🔒 Security & Secrets
- **No Secrets**: Never commit API keys, connection strings, or credentials. Use `UserSecrets` for local dev.
- **Environment Variables**: Use `IConfiguration` with environment variable overrides in production.
- **Permissions**: Use `[RequirePermission(InvoicePermissions.Name)]` on controller actions. Permissions follow the pattern `service.resource.action`.


---

## 🧪 Testing Guidelines
- **Unit Tests**: Mock external dependencies using `Moq`.
- **Integration Tests**: Use `BaseIntegrationTest` which handles the Testcontainers lifecycle.
- **Data Integrity**: Verify `RowVersion` for optimistic concurrency in updates.
- **Rounding**: Reconcile rounding errors in financial calculations (e.g., invoice splitting).

---

## 🚀 Deployment & CI/CD
- **Pre-commit Hooks**: Run `dotnet format` and `verify build` before every commit.
- **Health Checks**: API must expose `/liveness` and `/readiness` endpoints.
- **Metrics**: Use `OpenTelemetry` for custom metrics (see `InvoiceMetrics.cs`).


## Git & Version Control — Mandatory Rules

### 🚨 CRITICAL: Always Commit Code Changes (Non-Negotiable)
- **You MUST commit your changes to the local repository after completing any meaningful unit of work.**
- **Never accumulate uncommitted changes.** Do not wait until end of session or until something breaks.
- **Commit early and often** — if a change is meaningful (even a small fix or refactor), commit it.
- **You do NOT need to push to remote** — local commits are sufficient to protect against accidental loss.
- **If you are unsure whether to commit, commit anyway.** Extra commits are harmless; lost work is irreversible.
- This rule applies even if you are just "testing" or "exploring" — use git branches to isolate experimental work and commit those changes too.

### 🚨 CRITICAL: Never Use `git checkout` to Restore Broken Files
- **NEVER use `git checkout` to restore or recover files.** This operation discards uncommitted changes permanently and will result in data loss.
- **To undo/recover from broken files: first commit your current changes, then use `git revert` or `git reset --soft` to safely undo.**

## Database & EF Core — Mandatory Rules

### EF Core Design Package
- ❌ `Microsoft.EntityFrameworkCore.Design` MUST NOT be in Api projects
- ✅ It belongs ONLY in the Infrastructure (or Data) project where migrations live
- Migration commands must target Infrastructure as both project and startup-project (since EF Core Design package is in Infrastructure):
  ```
  dotnet ef migrations add <Name> --project Maliev.<Domain>Service.Infrastructure --startup-project Maliev.<Domain>Service.Infrastructure
  ```

### PostgreSQL xmin Concurrency — Mandatory Pattern
Use shadow property ONLY. Never add a Xmin/xmin property to domain entities.
```csharp
entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion();
```
- ❌ Never use `UseXminAsConcurrencyToken()` (removed in Npgsql EF v7)
- ❌ Never use entity property `public uint Xmin { get; set; }` or `public uint xmin { get; set; }`
- ❌ Never use `.Ignore(e => e.Xmin)` — remove the entity property instead
