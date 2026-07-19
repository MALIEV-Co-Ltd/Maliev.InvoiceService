# Tasks: Permission-Based Authorization Migration

**Input**: Design documents from `/specs/002-iam-integration/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and constants definition

- [x] T001 Create Authorization directory in `Maliev.InvoiceService.Api/Authorization`
- [x] T002 Create 21 permission constants in `Maliev.InvoiceService.Api/Authorization/InvoicePermissions.cs`
- [x] T003 [P] Create predefined role registrations (roles.invoice.* format) in `Maliev.InvoiceService.Api/Authorization/InvoicePredefinedRoles.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core IAM integration infrastructure

- [x] T004 Configure IAM Client in `Maliev.InvoiceService.Api/Program.cs`
- [x] T005 [P] Add IAM service configuration in `Maliev.InvoiceService.Api/appsettings.json`
- [x] T006 Create registration service skeleton in `Maliev.InvoiceService.Api/Services/InvoiceIAMRegistrationService.cs`
- [x] T007 Register `InvoiceIAMRegistrationService` as a hosted service in `Maliev.InvoiceService.Api/Program.cs`
- [x] T008 [P] Add `Features:PermissionBasedAuthEnabled` flag in `Maliev.InvoiceService.Api/appsettings.json`
- [x] T009 Create `InvoiceSegmentsController.cs` in `Maliev.InvoiceService.Api/Controllers/`

---

## Phase 3: User Story 2 - Automated Permission Registration (Priority: P1)

**Goal**: Automatically register 21 permissions and 5 roles with the IAM service on startup.

**Independent Test**: Start the InvoiceService and verify that 21 permissions and 5 roles are registered in the IAM service registry.

### Tests for User Story 2 (TDD)
- [x] T010 [US2] Write integration test to verify IAM registration call on startup in `Maliev.InvoiceService.Tests/Integration/IAMRegistrationTests.cs`

### Implementation for User Story 2
- [x] T011 [US2] Implement `GetPermissions` mapping for 21 permissions in `Maliev.InvoiceService.Api/Services/InvoiceIAMRegistrationService.cs`
- [x] T012 [US2] Implement `GetPredefinedRoles` using `roles.invoice.*` format in `Maliev.InvoiceService.Api/Services/InvoiceIAMRegistrationService.cs`
- [x] T013 [US2] Implement fail-fast startup logic for IAM registration in `Maliev.InvoiceService.Api/Services/InvoiceIAMRegistrationService.cs`

---

## Phase 4: User Story 1 - Secure Endpoint Access (Priority: P1) 🎯 MVP

**Goal**: Protect API endpoints using fine-grained permissions instead of broad roles.

**Independent Test**: Call `POST /invoices` with a token having `invoice.invoices.create` (Success) and without it (403 Forbidden).

### Tests for User Story 1 (TDD)
- [x] T014 [US1] Update `InvoicesController` integration tests to use `.WithTestAuth(permission)` in `Maliev.InvoiceService.Tests/Integration/InvoicesControllerTests.cs`
- [x] T015 [US1] Update `AuditController` integration tests to use `.WithTestAuth(permission)` in `Maliev.InvoiceService.Tests/Integration/AuditTrailTests.cs`
- [x] T016 [US1] Add test for permission precedence (permissions > roles) in `Maliev.InvoiceService.Tests/Integration/PermissionPrecedenceTests.cs`
- [x] T017 [US1] Add test for legacy role-to-permission mapping (e.g. "Manager" -> "roles.invoice.manager") in `Maliev.InvoiceService.Tests/Integration/LegacyRoleMappingTests.cs`

### Implementation for User Story 1
- [x] T018 [P] [US1] Update `InvoicesController.cs` methods with `[RequirePermission]` attributes
- [x] T019 [P] [US1] Update `AuditController.cs` methods with `[RequirePermission]` attributes
- [x] T020 [P] [US1] Update `PaymentsController.cs` methods with `[RequirePermission]` attributes
- [x] T021 [P] [US1] Update `InvoiceSegmentsController.cs` methods with `[RequirePermission]` attributes
- [x] T022 [US1] Remove legacy `[Authorize(Policy)]` attributes from all controllers
- [x] T023 [US1] Remove old authorization policy definitions from `Maliev.InvoiceService.Api/Program.cs`

---

## Phase 5: User Story 4 - Safe Feature Rollout (Priority: P1)

**Goal**: Support toggling between new and old auth models and provide fallbacks.

**Independent Test**: Toggle `PermissionBasedAuthEnabled` to `false` and verify legacy policy-based authorization still works.

### Tests for User Story 4 (TDD)
- [x] T024 [US4] Add integration test for feature flag toggle behavior in `Maliev.InvoiceService.Tests/Integration/FeatureFlagAuthTests.cs`

### Implementation for User Story 4
- [x] T025 [US4] Implement feature flag check in the authorization handler/middleware logic in `Maliev.InvoiceService.Api/Program.cs`
- [x] T026 [US4] Implement fail-closed timeout logic with legacy policy short-circuit (enabled by default when feature ON) in `Maliev.InvoiceService.Api/Program.cs`
- [ ] T033 Verify authorization overhead is < 1ms in integration tests
- [x] T034 Implement logging for successful permission checks in `Maliev.Aspire.ServiceDefaults.Authorization.RequirePermissionAttribute` (using IsCritical=true for high-value operations)

---

## Phase 6: User Story 3 - Service-to-Service Authorization (Priority: P2)

**Goal**: Secure service-to-service endpoints using service account permissions.

**Independent Test**: Simulate a call from UploadService with a token having `invoice.files.register` and verify success.

### Tests for User Story 3 (TDD)
- [x] T027 [US3] Add integration test for service account authorization in `Maliev.InvoiceService.Tests/Integration/ServiceAccountAuthTests.cs`

### Implementation for User Story 3
- [x] T028 [US3] Update `RegisterPdfFileReference` endpoint with `[RequirePermission(InvoicePermissions.FilesRegister)]` in `Maliev.InvoiceService.Api/Controllers/InvoicesController.cs`

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements and compliance

- [x] T029 [P] [FR-008] Implement authorization decision logging to the audit system in `Maliev.InvoiceService.Api/Middleware/ExceptionHandlingMiddleware.cs`
- [x] T030 [P] [SC-006] Update OpenAPI documentation with `SecurityRequirement` for each endpoint to reflect permission strings
- [x] T031 [P] Documentation updates in `README.md` regarding new permission-based authorization
- [ ] T032 Run `quickstart.md` validation in the staging environment

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1.
- **User Stories (Phase 3-6)**: All depend on Foundational (Phase 2).
  - US2 (Registration) is P1 and provides the registry for US1.
  - US1 (Endpoints) is the main functional change.
  - US4 (Rollout) is P1 risk mitigation.
  - US3 (Service-to-Service) is P2.

### Parallel Opportunities

- Constants in Phase 1 (T002, T003) can be created in parallel.
- Controller updates in Phase 4 (T018-T021) can be done in parallel.
- Polish tasks (T029-T031) can run in parallel.

---

## Implementation Strategy

### MVP First (User Story 1 & 2)

1. Complete Setup and Foundational.
2. Implement US2 (Registration) to populate the IAM system.
3. Implement US1 (Endpoints) to enforce security.
4. **VALIDATE**: Ensure core endpoints are protected by permissions.

### Incremental Delivery

1. Foundation + US2 → Service registers permissions.
2. US1 + US4 → Permissions enforced with safe toggle.
3. US3 → Secure inter-service communication.
4. Polish → Audit trails and documentation.