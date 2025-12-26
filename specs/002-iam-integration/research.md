# Research: Permission-Based Authorization Migration

## Overview
This document outlines the research and technical decisions for migrating the InvoiceService to a fine-grained, permission-based authorization model integrated with the IAM service.

## Decisions

### 1. Role-to-Permission Mapping
- **Decision**: Map roles to permissions via IAM when only legacy roles are present in a token.
- **Rationale**: Ensures backward compatibility during the transition period. Users with existing tokens that only contain roles (e.g., "Manager") will still be able to perform actions if their role maps to the required permission.
- **Alternatives considered**:
    - **Deny access**: Rejected as it would break existing clients immediately upon enabling the feature flag.
    - **Dual check**: Rejected as it increases complexity and maintenance overhead of both roles and permissions.

### 2. Failure Handling (Startup)
- **Decision**: Fail-fast on IAM registration failure.
- **Rationale**: If the service cannot register its permissions and roles, it is effectively running in an undefined security state. Preventing startup ensures that we don't deploy a service that cannot correctly authorize users.
- **Alternatives considered**:
    - **Soft fail**: Rejected because it might lead to 403 Forbidden errors for all users if permissions aren't registered, which is harder to diagnose than a failed startup.

### 3. Permission Precedence
- **Decision**: Permissions take precedence over roles if both are present in the JWT.
- **Rationale**: Follows the principle of least privilege and moves the system toward the target state. Fine-grained permissions are more specific than broad roles.

### 4. Integration Pattern
- **Decision**: Use the `[RequirePermission]` attribute provided by `Maliev.Aspire.ServiceDefaults`.
- **Rationale**: Centralizes the authorization logic and ensures consistency across all microservices in the Maliev ecosystem.

## Dependencies

- **Maliev.Aspire.ServiceDefaults.IAM**: Provides the `RequirePermissionAttribute`, `IAMRegistrationService`, and `IAMClient`.
- **IAM Service**: Central service responsible for managing the permission registry and granting permissions to principals.

## Unresolved Items
- None. All critical ambiguities were resolved in the clarification session.
