# Implementation Plan: Permission-Based Authorization Migration

**Branch**: `002-iam-integration` | **Date**: 22 ธันวาคม 2568 | **Spec**: [specs/002-iam-integration/spec.md](spec.md)
**Input**: Feature specification from `/specs/002-iam-integration/spec.md`

## Summary
Migrate the InvoiceService from policy-based authorization to fine-grained permission-based authorization. The approach involves defining 21+ permission constants, 5 predefined roles, and implementing an `IAMRegistrationService` that synchronizes these definitions with a central IAM service on startup. Authorization in controllers will be updated to use the `[RequirePermission]` attribute, with a feature flag for safe rollout and fallback mechanisms.

## Technical Context

**Language/Version**: C# / .NET 10  
**Primary Dependencies**: ASP.NET Core, Maliev.Aspire.ServiceDefaults (IAM Client/Service)  
**Storage**: N/A (Authorization is handled via IAM service and JWT claims)  
**Testing**: xUnit, Testcontainers (for integration tests with IAM service)  
**Target Platform**: Linux (Docker/Kubernetes)  
**Project Type**: Single API Service with Data and Test projects  
**Performance Goals**: Authorization overhead < 1ms per request (verify in integration tests)
**Constraints**: Zero downtime during migration; Must support legacy tokens during transition  
**Scale/Scope**: 21 Permissions, 5 Roles, 4 Controllers

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] Service Autonomy: Permission definitions are owned by the service.
- [x] Explicit Contracts: API-permission mapping documented in `contracts/api-permissions.md`.
- [x] Test-First Development: Integration tests will be updated before/during implementation.
- [x] Real Infrastructure Testing: Using real IAM service for integration testing.
- [x] Auditability: Both successful and denied authorization attempts will be logged.
- [x] Zero Warnings Policy: Build must remain warning-free.
- [x] No AutoMapper: Using explicit mapping for role-to-permission fallback if needed.

## Project Structure

### Documentation (this feature)

```text
specs/002-iam-integration/
├── plan.md              # This file
├── research.md          # Research on failure modes and precedence
├── data-model.md        # Definition of Permissions and Roles
├── quickstart.md        # Local development setup instructions
├── contracts/
│   └── api-permissions.md # Endpoint to Permission mapping
└── checklists/
    └── requirements.md  # Spec quality checklist
```

### Source Code (repository root)

```text
Maliev.InvoiceService.Api/
├── Authorization/
│   ├── InvoicePermissions.cs
│   └── InvoicePredefinedRoles.cs
├── Controllers/
│   ├── InvoicesController.cs
│   ├── InvoiceSegmentsController.cs
│   ├── PaymentsController.cs
│   └── AuditController.cs
├── Services/
│   └── InvoiceIAMRegistrationService.cs
└── Program.cs

Maliev.InvoiceService.Tests/
├── Integration/
│   ├── InvoicesControllerTests.cs
│   └── ...
```

**Structure Decision**: Following the flat structure mandated by the constitution (Projects at root). Added an `Authorization` folder in the API project for constants. Standardized Role IDs to `roles.invoice.*` format and aligned registration models with `Maliev.Aspire.ServiceDefaults.IAM`.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |