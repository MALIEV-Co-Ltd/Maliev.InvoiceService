# Feature Specification: Permission-Based Authorization Migration

**Feature Branch**: `002-iam-integration`  
**Created**: 22 ธันวาคม 2568  
**Status**: Draft  
**Input**: User description: "use the content from invoice-specify.md for specifications"

## Clarifications

### Session 2025-12-22
- Q: How should the system handle startup if the IAM service is unavailable? → A: Fail fast: Stop service startup if IAM registration fails.
- Q: How are critical operations handled if the permission check fails due to a network timeout? → A: Fallback: Revert to legacy policy check if permission check fails.
- Q: How does the system handle tokens that have both old roles and new permissions? → A: Permissions take precedence: If permission check exists, role is ignored.
- Q: What should the system do if only legacy roles are present in a token and the feature is ON? → A: Map: System should map roles to permissions via IAM to ensure backward compatibility.
- Q: Should authorization decisions (success/denial) be logged to the service's Audit system? → A: Yes: Log both successful and denied permission checks.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Secure Endpoint Access (Priority: P1)

As an API consumer, I want my requests to be authorized based on fine-grained permissions rather than broad roles, so that the principle of least privilege is enforced.

**Why this priority**: Core security requirement for the migration. It ensures that the system transitions to a more secure and flexible authorization model.

**Independent Test**: Can be fully tested by attempting to access a specific endpoint (e.g., Create Invoice) with a token containing the required permission (`invoice.invoices.create`) and verifying success, then attempting with a token lacking that permission and verifying failure.

**Acceptance Scenarios**:

1. **Given** a user has the `invoice.invoices.create` permission, **When** they call the `POST /invoices` endpoint, **Then** the request is authorized and processed.
2. **Given** a user does NOT have the `invoice.invoices.create` permission, **When** they call the `POST /invoices` endpoint, **Then** the system returns a 403 Forbidden response.

---

### User Story 2 - Automated Permission Registration (Priority: P1)

As a system administrator, I want the InvoiceService to automatically register its required permissions and predefined roles with the IAM service on startup, so that I don't have to manually configure them in the identity system.

**Why this priority**: Critical for operational efficiency and ensuring the IAM service stays in sync with the application's authorization requirements.

**Independent Test**: Can be tested by starting the InvoiceService and checking the IAM service logs or API to verify that 21 permissions and 5 roles have been registered.

**Acceptance Scenarios**:

1. **Given** the InvoiceService is starting up, **When** the registration service executes, **Then** it sends a registration request to the IAM service containing all 21 defined permissions.
2. **Given** the InvoiceService is starting up, **When** the registration service executes, **Then** it sends a registration request to the IAM service containing the 5 predefined roles (Admin, Manager, Creator, Viewer, Accountant).

---

### User Story 3 - Service-to-Service Authorization (Priority: P2)

As a developer of a related service (e.g., UploadService), I want to call InvoiceService endpoints using a service account with specific permissions, so that automated processes can interact securely with the invoice system.

**Why this priority**: Enables secure automation and integration with other microservices without requiring user intervention.

**Independent Test**: Can be tested by simulating a call from the UploadService using a service account token that has the `invoice.files.register` permission and verifying the PDF registration succeeds on the `PATCH /invoice/v1/invoices/{id}/pdf-reference` endpoint.

**Acceptance Scenarios**:

1. **Given** a service account has the `invoice.files.register` permission, **When** it calls the `PATCH /invoice/v1/invoices/{id}/pdf-reference` endpoint, **Then** the request is authorized.

---

### User Story 4 - Safe Feature Rollout (Priority: P1)

As an operator, I want to be able to toggle the permission-based authorization feature via a configuration flag, so that I can safely rollback to policy-based authorization if issues are detected in production.

**Why this priority**: Essential for risk mitigation during the transition from the old authorization model to the new one.

**Independent Test**: Can be tested by setting `PermissionBasedAuthEnabled` to `false` and verifying that the system uses the legacy `Authorize(Policy)` checks.

**Acceptance Scenarios**:

1. **Given** `PermissionBasedAuthEnabled` is set to `false`, **When** a request is made, **Then** the system evaluates traditional policies like `Manager` or `EmployeeOrHigher`.
2. **Given** `PermissionBasedAuthEnabled` is set to `true`, **When** a request is made, **Then** the system evaluates fine-grained permissions like `invoice.invoices.finalize`.

---

### Edge Cases

- **IAM Service Unavailable (Startup)**: System MUST fail fast and stop startup if the IAM service is unreachable during the registration phase to prevent running in an insecure state.
- **Token with Mixed Scopes**: If a token contains both legacy roles and new permissions, permissions MUST take precedence and roles are ignored for authorization decisions.
- **Legacy Tokens (Roles Only)**: If a token contains only legacy roles and the feature is ON, the system MUST attempt to map these roles (e.g., "Manager" -> "roles.invoice.manager") to their corresponding permissions by querying the IAM service to maintain backward compatibility.
- **Critical Permission Failure (Timeout)**: System MUST fail-closed (deny access) by default. If the feature flag is ON and a timeout occurs, the system may short-circuit to legacy policy-based authorization only if the legacy policy check succeeds.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST define 21 distinct permission constants in a centralized `InvoicePermissions` class.
- **FR-002**: System MUST define 5 predefined roles using the enterprise format `roles.invoice.{name}` (Admin, Manager, Creator, Viewer, Accountant).
- **FR-003**: System MUST implement an `IAMRegistrationService` that transmits permissions and roles to the IAM service on application startup.
- **FR-004**: System MUST update all controller endpoints (Invoices, Segments, Splits, Files, Reports) to use the `[RequirePermission]` attribute instead of `[Authorize(Policy)]`.
- **FR-005**: System MUST support a configuration flag `Features:PermissionBasedAuthEnabled` to toggle between the new and old authorization models.
- **FR-006**: System MUST integrate the IAM Client and Registration Service into the `Program.cs` startup sequence.
- **FR-007**: System MUST provide updated integration tests that verify both successful authorization with correct permissions and forbidden responses for missing permissions.
- **FR-008**: System MUST log all successful and denied permission-based authorization decisions to the service's audit log for compliance and security auditing.

### Key Entities *(include if feature involves data)*

- **Permission**: A unique string identifier (e.g., `invoice.invoices.create`) representing a specific action on a resource.
- **Role**: A collection of permissions (e.g., `invoice-manager`) that can be assigned to users or service accounts.
- **IAM Registration**: The payload containing all permissions and roles that the service identifies as its own and synchronizes with the central IAM service.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of target endpoints are decorated with `[RequirePermission]` attributes.
- **SC-002**: Zero `[Authorize(Policy)]` attributes remain in the production code path for Invoice and related controllers.
- **SC-003**: Service successfully registers 21 permissions and 5 roles with the IAM service during the startup phase.
- **SC-004**: All integration tests for authorized and unauthorized access pass with 100% reliability.
- **SC-005**: Feature flag transition (ON/OFF) executes without requiring code changes, only configuration updates.
- **SC-006**: OpenAPI documentation correctly reflects the required permission for each endpoint.
- **SC-007**: Audit logs capture 100% of permission-based authorization events with relevant metadata (user, resource, permission, outcome).
